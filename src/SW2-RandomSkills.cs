using System.Text;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Translation;
using Microsoft.Extensions.Logging;

namespace SW2_RandomSkills;

[PluginMetadata(Id = "SW2_RandomSkills", Version = "2.0.0", Name = "SW2-RandomSkills", Author = "T3Marius", Description = "Random skills with roll animation")]
public sealed class SW2_RandomSkills(ISwiftlyCore core) : BasePlugin(core)
{
    private ServiceProvider? _provider;

    public static new ISwiftlyCore Core { get; private set; } = null!;
    public SkillsConfig Config { get; set; } = new();

    private readonly SkillManager _skillManager = new();
    private readonly Dictionary<int, ActiveSkillData> _activeSkills = new();
    private readonly Dictionary<int, GameButtonFlags> _currentButtonStates = new();
    private readonly Dictionary<int, GameButtonFlags> _previousButtonStates = new();
    private readonly Dictionary<int, PlayerRollData> _playerRolls = new();

    private ILocalizer? _localizer;
    private readonly Random _random = new();

    private class RollConfig
    {
        public int SkillsToShow { get; set; } = 3;
        public float InitialSpeed { get; set; } = 0.05f;
        public float SlowdownRate { get; set; } = 0.75f;
        public float MinSpeed { get; set; } = 0.2f;
        public int TotalRolls { get; set; } = 12;
    }

    public override void Load(bool hotReload)
    {
        Core = base.Core;
        _localizer = Core.Localizer;

        Core.Configuration.InitializeJsonWithModel<SkillsConfig>("config.jsonc", "RandomSkills")
            .Configure(builder => builder.AddJsonFile("config.jsonc", optional: false, reloadOnChange: true));

        ServiceCollection services = new();
        services.AddSwiftly(Core)
                .AddOptionsWithValidateOnStart<SkillsConfig>()
                .BindConfiguration("RandomSkills");

        _provider = services.BuildServiceProvider();
        Config = _provider.GetRequiredService<IOptionsMonitor<SkillsConfig>>().CurrentValue;

        if (Config.SkillsRaw != null)
        {
            var skillsDict = new Dictionary<SkillType, SkillConfig>();
            foreach (var kvp in Config.SkillsRaw)
            {
                if (Enum.TryParse<SkillType>(kvp.Key, out var skillType))
                {
                    skillsDict[skillType] = kvp.Value;
                }
            }
            Config.Skills = skillsDict;
        }

        Core.Event.OnClientKeyStateChanged += OnClientKeyStateChanged;
        Core.Event.OnTick += OnTick;

        Core.GameEvent.HookPost<EventPlayerSpawn>(OnPlayerSpawn);
        Core.GameEvent.HookPre<EventPlayerDeath>(OnPlayerDeath);
        Core.GameEvent.HookPre<EventPlayerDisconnect>(OnPlayerDisconnect);

        _skillManager.RegisterSkill(new SwapPositionSkill(_localizer, Core));
        _skillManager.RegisterSkill(new SpeedBoostSkill(_localizer, Core));
        _skillManager.RegisterSkill(new ExtraDamageSkill(_localizer, Core));
        _skillManager.RegisterSkill(new RandomHealthSkill(_localizer, Core));
        _skillManager.RegisterSkill(new TeleportSkill(_localizer, Core));
        _skillManager.RegisterSkill(new TankSkill(_localizer, Core));
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect e)
    {
        if (e.UserIdPlayer == null)
            return HookResult.Continue;

        RemovePlayerSkill(e.UserIdPlayer);
        _playerRolls.Remove(e.UserIdPlayer.PlayerID);
        return HookResult.Continue;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn e)
    {
        IPlayer? player = e.UserIdPlayer;
        if (player == null || !player.IsValid || player.IsFakeClient)
            return HookResult.Continue;

        var gameRules = Core.EntitySystem.GetGameRules();
        if (gameRules?.WarmupPeriod == true)
            return HookResult.Continue;

        StartSkillRollAnimation(player);
        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath e)
    {
        IPlayer? player = e.UserIdPlayer;
        if (player == null || !player.IsValid || player.IsFakeClient)
            return HookResult.Continue;

        RemovePlayerSkill(player);
        _playerRolls.Remove(player.PlayerID);
        return HookResult.Continue;
    }

    private void StartSkillRollAnimation(IPlayer player)
    {
        player.PlaySound(Config.Sounds.OpenSound.Name, Config.Sounds.OpenSound.Volume);

        if (_activeSkills.ContainsKey(player.PlayerID))
        {
            var oldSkill = _activeSkills[player.PlayerID];
            oldSkill.Skill.Remove(player);
            _activeSkills.Remove(player.PlayerID);
        }

        var availableSkills = Config.Skills
            .Where(kvp => kvp.Value.Weight > 0 && kvp.Key != SkillType.None)
            .Select(kvp => kvp.Key)
            .ToList();

        if (availableSkills.Count == 0)
        {
            AssignRandomSkill(player);
            return;
        }

        var rollData = new PlayerRollData
        {
            PlayerId = player.PlayerID,
            AvailableSkills = availableSkills,
            CurrentIndex = _random.Next(availableSkills.Count),
            RollsRemaining = new RollConfig().TotalRolls,
            CurrentSpeed = new RollConfig().InitialSpeed,
            IsRolling = true,
            NextRollTime = DateTime.Now.AddSeconds(new RollConfig().InitialSpeed)
        };

        _playerRolls[player.PlayerID] = rollData;
        ShowRollAnimationFrame(player, rollData);
    }

    private void ShowRollAnimationFrame(IPlayer player, PlayerRollData rollData)
    {
        var config = new RollConfig();
        var skillsToShow = new List<SkillType>();
        int startIndex = rollData.CurrentIndex - 1;

        for (int i = 0; i < config.SkillsToShow; i++)
        {
            int index = (startIndex + i + rollData.AvailableSkills.Count) % rollData.AvailableSkills.Count;
            skillsToShow.Add(rollData.AvailableSkills[index]);
        }

        string html = CreateRollHTML(skillsToShow);
        player.SendCenterHTML(html);
        player.PlaySound(Config.Sounds.ScrollSound.Name, Config.Sounds.ScrollSound.Volume);
    }

    private string CreateRollHTML(List<SkillType> skills)
    {
        var builder = new StringBuilder();
        var skillNames = skills.Select(s => GetSkillDisplayName(s)).ToList();

        builder.AppendLine("<div align='center'>");
        builder.AppendLine(_localizer!["html.roll_title"]);
        builder.AppendLine("<br>");
        builder.AppendLine($"<font color='#888888' class='fontSize-sm'>{skillNames[0]}</font>");
        builder.AppendLine("<br>");
        builder.AppendLine($"<font color='#ffff00' class='fontSize-m'><b>&gt; {skillNames[1]} &lt;</b></font>");
        builder.AppendLine("<br>");
        builder.AppendLine($"<font color='#888888' class='fontSize-sm'>{skillNames[2]}</font>");
        builder.AppendLine($"<br>{_localizer!["html.roll_rolling"]}");
        builder.AppendLine("</div>");

        return builder.ToString();
    }

    private void ShowFinalSkillResult(IPlayer player, SkillType skillType)
    {
        var skill = _skillManager.CreateSkillInstance(skillType, _localizer!, Core);
        if (skill == null) return;

        if (!Config.Skills.TryGetValue(skillType, out var skillConfig))
            skillConfig = new SkillConfig();

        skill.Initialize(skillConfig);

        player.PlaySound(Config.Sounds.DisplaySound.Name, Config.Sounds.DisplaySound.Volume);

        string html = CreateFinalResultHTML(skill, skillType);
        player.SendCenterHTML(html);

        Core.Scheduler.DelayBySeconds(5, () =>
        {
            if (player.IsValid)
            {
                _activeSkills[player.PlayerID] = new ActiveSkillData
                {
                    Skill = skill,
                    SkillType = skillType,
                    Config = skillConfig
                };

                if (Config.EnableSkillMessages && _localizer != null)
                {
                    ShowSkillMessage(player, skill, skillConfig);
                }

                skill.Apply(player);

                Core.Scheduler.DelayBySeconds(3, () =>
                {
                    if (player.IsValid)
                    {
                        player.SendCenterHTML("");
                    }
                });
            }
        });
    }

    private string CreateFinalResultHTML(BaseSkill skill, SkillType skillType)
    {
        var builder = new StringBuilder();
        string skillName = GetSkillDisplayName(skillType);

        builder.AppendLine("<div align='center'>");
        builder.AppendLine(_localizer!["html.roll_title"]);
        builder.AppendLine("<br>");
        builder.AppendLine($"<font color='#00ff00' class='fontSize-sm'><b>&gt; {skillName} &lt;</b></font>");
        builder.AppendLine("<br>");
        builder.AppendLine($"<font color='#cccccc' class='fontSize-s'>ⓘ {skill.Description}</font>");
        builder.AppendLine("</div>");

        return builder.ToString();
    }

    private string GetSkillDisplayName(SkillType skillType)
    {
        return skillType switch
        {
            SkillType.SwapPosition => "Swap Position",
            SkillType.SpeedBoost => "Speed Boost",
            SkillType.Jetpack => "Jetpack",
            SkillType.Teleport => "Teleport",
            SkillType.Shield => "Shield",
            SkillType.ExtraDamage => "Extra Damage",
            SkillType.Tank => "Tank",
            _ => skillType.ToString()
        };
    }

    private void ProcessRollAnimations(float deltaTime)
    {
        var now = DateTime.Now;
        var playersToRemove = new List<int>();

        foreach (var kvp in _playerRolls.ToList())
        {
            var playerId = kvp.Key;
            var rollData = kvp.Value;

            if (!rollData.IsRolling) continue;

            var player = Core.PlayerManager.GetPlayer(playerId);
            if (player == null || !player.IsValid)
            {
                playersToRemove.Add(playerId);
                continue;
            }

            if (now >= rollData.NextRollTime)
            {
                rollData.RollsRemaining--;

                if (rollData.RollsRemaining <= 0)
                {
                    FinalizeSkillRoll(player, rollData);
                    playersToRemove.Add(playerId);
                }
                else
                {
                    rollData.CurrentIndex = (rollData.CurrentIndex + 1) % rollData.AvailableSkills.Count;
                    var config = new RollConfig();
                    rollData.CurrentSpeed = Math.Max(config.MinSpeed, rollData.CurrentSpeed * config.SlowdownRate);
                    rollData.NextRollTime = now.AddSeconds(rollData.CurrentSpeed);
                    ShowRollAnimationFrame(player, rollData);
                }
            }
        }

        foreach (var playerId in playersToRemove)
        {
            _playerRolls.Remove(playerId);
        }
    }

    private void FinalizeSkillRoll(IPlayer player, PlayerRollData rollData)
    {
        int finalIndex = rollData.CurrentIndex;
        var finalSkillType = rollData.AvailableSkills[finalIndex];
        rollData.FinalSkill = finalSkillType;
        rollData.IsRolling = false;
        ShowFinalSkillResult(player, finalSkillType);
    }

    private void AssignRandomSkill(IPlayer player)
    {
        if (_activeSkills.ContainsKey(player.PlayerID))
        {
            var oldSkill = _activeSkills[player.PlayerID];
            oldSkill.Skill.Remove(player);
            _activeSkills.Remove(player.PlayerID);
        }

        var skillType = _skillManager.GetRandomSkill(Config);
        if (skillType == SkillType.None) return;

        var skill = _skillManager.CreateSkillInstance(skillType, _localizer!, Core);
        if (skill == null) return;

        if (!Config.Skills.TryGetValue(skillType, out var skillConfig))
            skillConfig = new SkillConfig();

        skill.Initialize(skillConfig);

        _activeSkills[player.PlayerID] = new ActiveSkillData
        {
            Skill = skill,
            SkillType = skillType,
            Config = skillConfig
        };

        if (Config.EnableSkillMessages && _localizer != null)
        {
            ShowSkillMessage(player, skill, skillConfig);
        }

        skill.Apply(player);
    }

    private void ShowSkillMessage(IPlayer player, BaseSkill skill, SkillConfig config)
    {
        string prefix = _localizer!["prefix"];
        string assignedMsg = _localizer["skill_assigned", skill.Name];

        player.SendChat($"{prefix}{assignedMsg}");
        player.SendChat($"{prefix}{skill.Description}");

        if (!string.IsNullOrEmpty(config.ActivationKey))
        {
            string keyName = config.ActivationKey.ToUpper();
            string activateMsg = _localizer["activate_key", keyName];
            player.SendCenterHTML($"<div align='center'>🔘 {activateMsg}</div>");
        }
    }

    private void OnTick()
    {
        float deltaTime = 0.016f;
        ProcessRollAnimations(deltaTime);

        foreach (var playerId in _activeSkills.Keys.ToList())
        {
            var player = Core.PlayerManager.GetPlayer(playerId);
            if (player == null || !player.IsValid)
            {
                CleanupPlayer(playerId);
                continue;
            }

            var skillData = _activeSkills[playerId];
            skillData.Skill.OnTick(player, deltaTime);

            if (!string.IsNullOrEmpty(skillData.Config.ActivationKey) &&
                KeyHelper.TryParseConfigKey(skillData.Config.ActivationKey, out var button))
            {
                DetectHeldButtons(player, playerId, button);
            }
        }
    }

    private void OnClientKeyStateChanged(IOnClientKeyStateChangedEvent e)
    {
        IPlayer? player = Core.PlayerManager.GetPlayer(e.PlayerId);
        if (player == null || !player.IsValid || !player.IsAlive)
            return;

        GameButtonFlags? mappedButton = e.Key switch
        {
            KeyKind.W => GameButtonFlags.W,
            KeyKind.S => GameButtonFlags.S,
            KeyKind.A => GameButtonFlags.A,
            KeyKind.D => GameButtonFlags.D,
            KeyKind.F => GameButtonFlags.F,
            KeyKind.Space => GameButtonFlags.Space,
            KeyKind.Ctrl => GameButtonFlags.Ctrl,
            KeyKind.Mouse1 => GameButtonFlags.Mouse2,
            KeyKind.Mouse2 => GameButtonFlags.Mouse1,
            KeyKind.E => GameButtonFlags.E,
            KeyKind.R => GameButtonFlags.R,
            KeyKind.Shift => GameButtonFlags.Shift,
            _ => null
        };

        if (mappedButton.HasValue)
        {
            UpdateButtonState(player.PlayerID, mappedButton.Value, e.Pressed);
            if (_activeSkills.TryGetValue(player.PlayerID, out var skillData))
            {
                ProcessKeyEventForSkill(player, skillData, mappedButton.Value, e.Pressed);
            }
        }
    }

    private void UpdateButtonState(int playerId, GameButtonFlags key, bool pressed)
    {
        if (!_previousButtonStates.ContainsKey(playerId))
            _previousButtonStates[playerId] = GameButtonFlags.None;

        _previousButtonStates[playerId] = _currentButtonStates.GetValueOrDefault(playerId);

        if (!_currentButtonStates.ContainsKey(playerId))
            _currentButtonStates[playerId] = GameButtonFlags.None;

        if (pressed)
            _currentButtonStates[playerId] |= key;
        else
            _currentButtonStates[playerId] &= ~key;
    }

    private void DetectHeldButtons(IPlayer player, int playerId, GameButtonFlags activationButton)
    {
        if (!_currentButtonStates.TryGetValue(playerId, out var currentState))
            return;

        if ((currentState & activationButton) != GameButtonFlags.None)
        {
            if (_activeSkills.TryGetValue(playerId, out var skillData))
            {
                skillData.Skill.OnActivationButtonHeld(player);
            }
        }
    }

    private void ProcessKeyEventForSkill(IPlayer player, ActiveSkillData skillData, GameButtonFlags button, bool pressed)
    {
        var skill = skillData.Skill;

        if (pressed)
            skill.OnButtonPressed(player, button);
        else
            skill.OnButtonReleased(player, button);

        if (!string.IsNullOrEmpty(skillData.Config.ActivationKey) &&
            KeyHelper.TryParseConfigKey(skillData.Config.ActivationKey, out var activationButton) &&
            button == activationButton)
        {
            if (pressed)
                skill.OnActivationButtonPressed(player);
            else
                skill.OnActivationButtonReleased(player);
        }
    }

    private void RemovePlayerSkill(IPlayer player)
    {
        if (_activeSkills.TryGetValue(player.PlayerID, out var skillData))
        {
            skillData.Skill.Remove(player);
            _activeSkills.Remove(player.PlayerID);
        }

        CleanupPlayer(player.PlayerID);
    }

    private void CleanupPlayer(int playerId)
    {
        if (_activeSkills.ContainsKey(playerId))
            _activeSkills.Remove(playerId);
        if (_currentButtonStates.ContainsKey(playerId))
            _currentButtonStates.Remove(playerId);
        if (_previousButtonStates.ContainsKey(playerId))
            _previousButtonStates.Remove(playerId);
        if (_playerRolls.ContainsKey(playerId))
            _playerRolls.Remove(playerId);
    }

    public override void Unload()
    {
        Core.Event.OnTick -= OnTick;
        Core.Event.OnClientKeyStateChanged -= OnClientKeyStateChanged;
    }
}

public class ActiveSkillData
{
    public BaseSkill Skill { get; set; } = null!;
    public SkillType SkillType { get; set; }
    public SkillConfig Config { get; set; } = new();
    public DateTime LastActivation { get; set; } = DateTime.MinValue;
}

public class PlayerRollData
{
    public List<SkillType> AvailableSkills { get; set; } = new();
    public int CurrentIndex { get; set; } = 0;
    public int RollsRemaining { get; set; } = 0;
    public float CurrentSpeed { get; set; } = 0.1f;
    public SkillType? FinalSkill { get; set; }
    public DateTime NextRollTime { get; set; } = DateTime.Now;
    public bool IsRolling { get; set; } = true;
    public int PlayerId { get; set; }
}

public interface IActivatableSkill
{
    void OnActivate(IPlayer player);
}
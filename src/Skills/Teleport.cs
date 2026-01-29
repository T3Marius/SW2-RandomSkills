using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Translation;

namespace SW2_RandomSkills;

public class TeleportSkill(ILocalizer localizer, ISwiftlyCore core) : BaseSkill(localizer, core)
{
    public override SkillType Type => SkillType.Teleport;
    public override string Name => "Teleport";
    public override void Apply(IPlayer player) { }
    public override void Initialize(SkillConfig config)
    {
        base.Initialize(config);
    }
    protected override string GetDefaultDescription()
    {
        return $"Teleports you behind a random opponent! Press {GetActivationKey()} to use it.";
    }
    protected override object[] GetDescriptionParameters()
    {
        return new object[] { GetActivationKey() };
    }
    public override void OnActivationButtonPressed(IPlayer player)
    {
        if (IsOnCooldown())
        {
            player.SendCenter(Localizer["skill_cooldown", $"{GetCooldownLeft():F1}"]);
            return;
        }
        if (Core.EntitySystem.GetGameRules()?.FreezePeriod == true)
        {
            player.SendCenter(Localizer["teleport.freeze_time"]);
            return;
        }
        IPlayer? target;

        var targets = Core.PlayerManager.GetAlive().Where(p => p.Controller.Team != player.Controller.Team).ToList();
        target = targets[new Random().Next(targets.Count)];

        if (target == null)
        {
            player.SendCenter(Localizer["teleport.no_enemy_found"]);
            return;
        }

        StartCooldown();
        TeleportBehind(player, target);

        player.SendChat(Localizer["prefix"] + Localizer["teleport.teleported_behind", target.GetPlayerName()]);
    }
    private void TeleportBehind(IPlayer p1, IPlayer p2)
    {
        if (p1.PlayerPawn == null || p2.PlayerPawn == null)
            return;

        var pos = p2.PlayerPawn.CBodyComponent?.SceneNode?.AbsOrigin;
        var vel = p2.PlayerPawn.AbsVelocity;
        var angles = p2.PlayerPawn.EyeAngles;

        if (pos == null)
            return;

        float radians = (float)(angles.Y * Math.PI / 180f);
        float offsetX = (float)Math.Cos(radians) * 100f;
        float offsetY = (float)Math.Sin(radians) * 100f;

        Vector behindPos = new Vector(
            pos.Value.X - offsetX,
            pos.Value.Y - offsetY,
            pos.Value.Z
        );

        p1.Teleport(behindPos, angles, vel);
    }


}
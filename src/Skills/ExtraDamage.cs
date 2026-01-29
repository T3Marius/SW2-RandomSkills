using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Translation;

namespace SW2_RandomSkills;

public class ExtraDamageSkill(ILocalizer localizer, ISwiftlyCore core) : BaseSkill(localizer, core)
{
    public override SkillType Type => SkillType.ExtraDamage;
    public override string Name => "Extra Damage";
    private int _extraDamage;
    private IPlayer? _player;
    protected override string GetDefaultDescription()
    {
        return $"You deal extra damage by {_extraDamage}";
    }
    protected override object[] GetDescriptionParameters()
    {
        return new object[] { _extraDamage };
    }
    public override void Initialize(SkillConfig config)
    {
        base.Initialize(config);
        _extraDamage = GetParameter("ExtraDamage", 90);
    }
    public override void Apply(IPlayer player)
    {
        _player = player;
        Core.Event.OnEntityTakeDamage += OnTakeDamage;
    }
    public override void Remove(IPlayer player)
    {
        Core.Event.OnEntityTakeDamage -= OnTakeDamage;
        _player = null;
    }
    private void OnTakeDamage(IOnEntityTakeDamageEvent e)
    {
        if (_player == null)
        {
            Core.Logger.LogInformation("Skill player is null");
            return;
        }

        var attacker = e.Info.AttackerInfo.AttackerPawn.Value;
        if (attacker == null)
        {
            Core.Logger.LogInformation("Attacker value is null.");
            return;
        }

        var controller = attacker.Controller.Value;
        if (controller == null)
        {
            Core.Logger.LogInformation("Controller value is null.");
            return;
        }

        if (controller.ToPlayer()?.PlayerID == _player.PlayerID)
        {
            e.Info.Damage += _extraDamage;
            return;
        }
    }
}
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Translation;

namespace SW2_RandomSkills;

public class SpeedBoostSkill(ILocalizer localizer, ISwiftlyCore core) : BaseSkill(localizer, core)
{
    public override SkillType Type => SkillType.SpeedBoost;
    public override string Name => "Speed Boost";
    private float _speedDelay;
    private float _speedBoost;
    private float _defaultSpeed;
    protected override string GetDefaultDescription()
    {
        if (_speedDelay > 0)
            return $"After {_speedDelay}s delay, move {_speedBoost}x faster!";
        return $"Move {_speedBoost}x faster!";
    }
    protected override object[] GetDescriptionParameters()
    {
        return new object[] { _speedBoost, _speedDelay };
    }
    public override void Initialize(SkillConfig config)
    {
        base.Initialize(config);
        _speedDelay = GetParameter("SpeedDelay", 2f);
        _speedBoost = GetParameter("SpeedBoost", 1.5f);
    }
    public override void Apply(IPlayer player)
    {
        if (player.PlayerPawn == null)
            return;

        Core.Scheduler.NextWorldUpdate(() =>
        {
            if (_speedDelay > 0)
            {
                Core.Scheduler.DelayBySeconds(_speedDelay, () =>
                {
                    player.SetSpeed(player.PlayerPawn.VelocityModifier + _speedBoost);
                });
            }
            else
            {
                player.SetSpeed(player.PlayerPawn.VelocityModifier + _speedBoost);
            }
        });
    }
    public override void Remove(IPlayer player)
    {
        if (player.PlayerPawn == null)
            return;

        player.PlayerPawn.VelocityModifier = _defaultSpeed;
        player.PlayerPawn.VelocityModifierUpdated();
    }
}
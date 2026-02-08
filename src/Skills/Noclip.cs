using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Translation;

namespace SW2_RandomSkills;

public class NoclipSkill(ILocalizer localizer, ISwiftlyCore core) : BaseSkill(localizer, core)
{
    public override SkillType Type => SkillType.Noclip;
    public override string Name => "Noclip";
    protected override string GetDefaultDescription()
    {
        return $"Enables Noclip for {_noClipDuration} seconds when pressing {Config.ActivationKey}. Cooldown: {Config.Cooldown}";
    }
    protected override object[] GetDescriptionParameters()
    {
        return new object[] { _noClipDuration, Config.ActivationKey, Config.Cooldown };
    }
    private int _noClipDuration;
    private CancellationTokenSource? _noClipTimer;
    public override void Apply(IPlayer player) { }
    public override void Initialize(SkillConfig config)
    {
        base.Initialize(config);
        _noClipDuration = GetParameter("NoclipDuration", 5);
    }
    public override void Remove(IPlayer player)
    {
        _noClipTimer?.Cancel();
        _noClipTimer = null;
    }
    public override void OnActivationButtonPressed(IPlayer player)
    {
        if (IsOnCooldown())
        {
            player.SendChat(Localizer["prefix"] + Localizer["skill_cooldown", GetCooldownLeft().ToString("0")]);
            return;
        }

        int duration = _noClipDuration;

        ChangeMoveType(player, MoveType_t.MOVETYPE_NOCLIP);
        _noClipTimer = Core.Scheduler.RepeatBySeconds(1.0f, () =>
        {
            duration--;
            if (duration > 0)
            {
                player.SendCenterHTML(Localizer["noclip.duration", duration.ToString("0")]);
            }
            else
            {
                player.SendCenterHTML("");
                ChangeMoveType(player, MoveType_t.MOVETYPE_WALK);
                _noClipDuration = GetParameter("NoclipDuration", 5);
                _noClipTimer?.Cancel();
                _noClipTimer = null;
            }
        });
        StartCooldown();
    }
    private void ChangeMoveType(IPlayer player, MoveType_t moveType)
    {
        var playerPawn = player.PlayerPawn;
        if (playerPawn == null)
            return;

        playerPawn.MoveType = moveType;
        playerPawn.ActualMoveType = moveType;
        playerPawn.MoveTypeUpdated();
    }

}
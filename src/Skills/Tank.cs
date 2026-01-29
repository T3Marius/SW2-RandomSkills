using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Helpers;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Translation;
using ChatColors = SwiftlyS2.Shared.Helper.ChatColors;

namespace SW2_RandomSkills;

public class TankSkill(ILocalizer localizer, ISwiftlyCore core) : BaseSkill(localizer, core)
{
    public override SkillType Type => SkillType.Tank;
    public override string Name => "Tank";
    protected override string GetDefaultDescription()
    {
        return $"You're a {ChatColors.Red}TANK{ChatColors.Default}! You get {_tankHealth} HP + NEGEV + UNLIMITED AMMO AND LOWER SPEED!";
    }
    protected override object[] GetDescriptionParameters()
    {
        return new object[] { _tankHealth };
    }
    private int _tankHealth;
    private float _tankSpeed;
    private IPlayer? _player;
    public override void Apply(IPlayer player)
    {
        _player = player;

        player.SetHealth(_tankHealth);
        player.SetSpeed(_tankSpeed);
        player.SetAmmo(999, 999);

        player.Pawn?.ItemServices?.RemoveItems();
        player.Pawn?.ItemServices?.GiveItem<CBaseEntity>("weapon_negev");

        Core.GameEvent.HookPre<EventWeaponFire>(OnWeaponFire);
        Core.Event.OnItemServicesCanAcquireHook += CanAcquire;
    }
    public override void Initialize(SkillConfig config)
    {
        base.Initialize(config);
        _tankHealth = GetParameter("TankHealth", 500);
        _tankSpeed = GetParameter("TankSpeed", 0.5f);
    }
    private HookResult OnWeaponFire(EventWeaponFire e)
    {
        IPlayer? player = e.UserIdPlayer;
        if (player == null || player.PlayerID != _player?.PlayerID)
            return HookResult.Continue;

        player.SetAmmo(999, 999);

        return HookResult.Continue;
    }
    private void CanAcquire(IOnItemServicesCanAcquireHookEvent e)
    {
        var econItemView = e.EconItemView;
        var player = e.ItemServices.Pawn.ToPlayer();
        if (player == null)
            return;

        if (player.PlayerID != _player?.PlayerID)
            return;

        if (econItemView.ItemDefinitionIndex != (ushort)ItemDefinitionIndex.Negev)
        {
            player.SendChat(Localizer["prefix"] + Localizer["tank.only_negev"]);
            e.SetAcquireResult(AcquireResult.NotAllowedByProhibition);
        }
    }
    public override void Remove(IPlayer player)
    {
        Core.GameEvent.UnhookPre<EventWeaponFire>();
        Core.Event.OnItemServicesCanAcquireHook -= CanAcquire;
        player.SetHealth(100);
        player.SetSpeed(1.0f);
    }
}
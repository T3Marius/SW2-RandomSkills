using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Translation;

namespace SW2_RandomSkills;

public class SwapPositionSkill(ILocalizer localizer, ISwiftlyCore core) : BaseSkill(localizer, core)
{
    public override SkillType Type => SkillType.SwapPosition;
    public override string Name => "Swap Position";
    protected override string GetDefaultDescription()
    {
        return "Press F to swap positions with nearest enemy";
    }

    private float _maxRange;
    protected override object[] GetDescriptionParameters()
    {
        return new object[] { _maxRange };
    }

    public override void Initialize(SkillConfig config)
    {
        base.Initialize(config);
        _maxRange = GetParameter("MaxRange", 2000.0f);
    }
    public override void Apply(IPlayer player) { }
    public override void OnActivationButtonPressed(IPlayer player)
    {
        if (IsOnCooldown())
        {
            player.SendCenter(Localizer["skill_cooldown", $"{GetCooldownLeft():F1}"]);
            return;
        }

        IPlayer? target = null;
        if (_maxRange > 0)
        {
            target = FindNearestEnemy(player);
        }
        else
        {
            var targets = Core.PlayerManager.GetAlive().Where(p => p.Controller.Team != player.Controller.Team).ToList();
            target = targets[new Random().Next(targets.Count)];
        }

        if (target == null)
        {
            player.SendCenter(Localizer["swap_position.no_enemy_radius", _maxRange]);
            return;
        }

        SwapPositions(player, target);
        StartCooldown();


        player.SendCenter(Localizer["swap_position.changed", target.GetPlayerName()]);
        target.SendCenter(Localizer["swap_position.changed", player.GetPlayerName()]);

    }
    private void SwapPositions(IPlayer p1, IPlayer p2)
    {
        if (p1.Pawn == null || p2.Pawn == null || p1.PlayerPawn == null || p2.PlayerPawn == null)
            return;

        var pos1 = p1.Pawn.CBodyComponent?.SceneNode?.AbsOrigin;
        var pos2 = p2.Pawn.CBodyComponent?.SceneNode?.AbsOrigin;

        if (pos1 == null || pos2 == null) return;

        var vel1 = p1.Pawn.AbsVelocity;
        var vel2 = p2.Pawn.AbsVelocity;

        p1.Pawn.Teleport(pos2, p1.PlayerPawn.EyeAngles, vel2);
        p2.Pawn.Teleport(pos1, p2.PlayerPawn.EyeAngles, vel1);
    }
    private IPlayer? FindNearestEnemy(IPlayer player)
    {
        var enemies = Core.PlayerManager.GetAlive()
            .Where(p => p.Controller.Team != player.Controller.Team)
            .ToList();

        if (enemies.Count == 0) return null;

        IPlayer? nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            var dist = CalculateDistance(player, enemy);
            if (dist < _maxRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = enemy;
            }
        }
        return nearest;
    }
    private float CalculateDistance(IPlayer p1, IPlayer p2)
    {
        if (p1.Pawn == null || p2.Pawn == null)
            return float.MaxValue;

        var pos1 = p1.Pawn.CBodyComponent?.SceneNode?.AbsOrigin;
        var pos2 = p2.Pawn.CBodyComponent?.SceneNode?.AbsOrigin;

        if (pos1 == null || pos2 == null) return float.MaxValue;

        return (float)Math.Sqrt(
            Math.Pow(pos1.Value.X - pos2.Value.X, 2) +
            Math.Pow(pos1.Value.Y - pos2.Value.Y, 2) +
            Math.Pow(pos1.Value.Z - pos2.Value.Z, 2)
        );
    }

}
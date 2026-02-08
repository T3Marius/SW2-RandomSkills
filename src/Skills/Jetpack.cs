using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Translation;

namespace SW2_RandomSkills;

public class JetpackSkill(ILocalizer localizer, ISwiftlyCore core) : BaseSkill(localizer, core)
{
    public override SkillType Type => SkillType.Jetpack;
    public override string Name => "Jetpack";

    private IPlayer? _player;

    private float _maxFuel;
    private float _fuelDrainPerSecond;
    private float _fuelRefillPerSecond;
    private float _refillDelay;
    private float _liftVelocity;
    private float _maxVerticalSpeed;
    private float _hudUpdateInterval;
    private int _fuelBarSegments;
    private string _fuelFillChar = "\u2588";
    private string _fuelEmptyChar = "\u2588";

    private float _currentFuel;
    private float _refillDelayRemaining;
    private float _hudAccumulator;
    private bool _activationHeld;
    private bool _outOfFuelNotified;

    protected override string GetDefaultDescription()
    {
        return $"Hold {Config.ActivationKey} to fly. Fuel refills after {_refillDelay:0.##}s.";
    }

    protected override object[] GetDescriptionParameters()
    {
        return new object[] { Config.ActivationKey, _maxFuel, _refillDelay };
    }

    public override void Initialize(SkillConfig config)
    {
        base.Initialize(config);

        _maxFuel = Math.Max(1f, GetParameter("MaxFuel", 100f));
        _fuelDrainPerSecond = Math.Max(0.01f, GetParameter("FuelDrainPerSecond", 25f));
        _fuelRefillPerSecond = Math.Max(0.01f, GetParameter("FuelRefillPerSecond", 15f));
        _refillDelay = Math.Max(0f, GetParameter("RefillTimer", GetParameter("RefillDelay", 1.5f)));
        _liftVelocity = GetParameter("LiftVelocity", 280f);
        _maxVerticalSpeed = Math.Max(50f, GetParameter("MaxVerticalSpeed", 320f));
        _hudUpdateInterval = Math.Max(0.05f, GetParameter("HudUpdateInterval", 0.1f));
        _fuelBarSegments = Math.Max(5, GetParameter("FuelBarSegments", 18));

        string fillChar = GetParameter("FuelFillChar", GetParameter("FuelBarFillChar", "\u2588"));
        string emptyChar = GetParameter("FuelEmptyChar", GetParameter("FuelBarEmptyChar", "\u2588"));

        _fuelFillChar = string.IsNullOrWhiteSpace(fillChar) ? "\u2588" : fillChar;
        _fuelEmptyChar = string.IsNullOrWhiteSpace(emptyChar) ? "\u2588" : emptyChar;

        _currentFuel = _maxFuel;
        _refillDelayRemaining = 0f;
        _hudAccumulator = 0f;
        _activationHeld = false;
        _outOfFuelNotified = false;
    }

    public override void Apply(IPlayer player)
    {
        _player = player;
        _currentFuel = _maxFuel;
        _refillDelayRemaining = 0f;
        _hudAccumulator = _hudUpdateInterval;
        _activationHeld = false;
        _outOfFuelNotified = false;
    }

    public override void OnActivationButtonHeld(IPlayer player)
    {
        base.OnActivationButtonHeld(player);

        _activationHeld = true;
        if (_currentFuel <= 0f)
        {
            return;
        }

        ApplyLift(player);
    }

    public override void OnActivationButtonReleased(IPlayer player)
    {
        base.OnActivationButtonReleased(player);
        _activationHeld = false;
    }

    public override void OnTick(IPlayer player, float deltaTime)
    {
        bool heldThisTick = _activationHeld;
        _activationHeld = false;

        if (heldThisTick)
        {
            if (_currentFuel > 0f)
            {
                _currentFuel = Math.Max(0f, _currentFuel - (_fuelDrainPerSecond * deltaTime));
                _refillDelayRemaining = _refillDelay;

                if (_currentFuel <= 0f && !_outOfFuelNotified)
                {
                    player.SendChat(Localizer["prefix"] + Localizer["jetpack.out_of_fuel", Config.ActivationKey.ToUpper()]);
                    _outOfFuelNotified = true;
                }
            }
            else
            {
                _refillDelayRemaining = _refillDelay;
            }
        }
        else
        {
            if (_refillDelayRemaining > 0f)
            {
                _refillDelayRemaining = Math.Max(0f, _refillDelayRemaining - deltaTime);
            }
            else
            {
                _currentFuel = Math.Min(_maxFuel, _currentFuel + (_fuelRefillPerSecond * deltaTime));
                if (_currentFuel > 0f)
                {
                    _outOfFuelNotified = false;
                }
            }
        }

        UpdateFuelHud(player, deltaTime, heldThisTick);
    }

    public override void Remove(IPlayer player)
    {
        if (_player != null && _player.IsValid)
        {
            _player.SendCenterHTML("");
        }

        _activationHeld = false;
        _player = null;
    }

    private void ApplyLift(IPlayer player)
    {
        if (player.Pawn == null)
        {
            return;
        }

        var velocity = player.Pawn.AbsVelocity;
        float boostedZ = Math.Min(_maxVerticalSpeed, Math.Max(velocity.Z, _liftVelocity));
        Vector newVelocity = new Vector(velocity.X, velocity.Y, boostedZ);


        player.Pawn.Teleport(null, null, newVelocity);
    }

    private void UpdateFuelHud(IPlayer player, float deltaTime, bool heldThisTick)
    {
        _hudAccumulator += deltaTime;
        if (_hudAccumulator < _hudUpdateInterval)
        {
            return;
        }

        _hudAccumulator = 0f;
        float fuelPercent = (_currentFuel / _maxFuel) * 100f;

        int filled = (int)MathF.Round((_currentFuel / _maxFuel) * _fuelBarSegments);
        filled = Math.Clamp(filled, 0, _fuelBarSegments);
        int empty = _fuelBarSegments - filled;

        string filledBar = string.Concat(Enumerable.Repeat(_fuelFillChar, filled));
        string emptyBar = string.Concat(Enumerable.Repeat(_fuelEmptyChar, empty));
        string fuelColor = GetFuelColor(fuelPercent);

        string statusText;
        string statusColor;

        if (heldThisTick && _currentFuel > 0f)
        {
            statusText = Localizer["jetpack.status.boosting"];
            statusColor = "#67e8f9";
        }
        else if (_currentFuel <= 0f)
        {
            statusText = Localizer["jetpack.status.empty"];
            statusColor = "#ef4444";
        }
        else if (_refillDelayRemaining > 0f)
        {
            statusText = Localizer["jetpack.status.refill_wait", _refillDelayRemaining.ToString("0.0")];
            statusColor = "#9ca3af";
        }
        else if (_currentFuel >= _maxFuel - 0.01f)
        {
            statusText = Localizer["jetpack.status.full"];
            statusColor = "#22c55e";
        }
        else
        {
            statusText = Localizer["jetpack.status.refilling"];
            statusColor = "#f59e0b";
        }

        player.SendCenterHTML(Localizer["jetpack.fuel_hud", fuelPercent.ToString("0"), filledBar, emptyBar, fuelColor, statusColor, statusText]);
    }

    private static string GetFuelColor(float fuelPercent)
    {
        if (fuelPercent <= 25f)
        {
            return "#ef4444";
        }

        if (fuelPercent <= 60f)
        {
            return "#f59e0b";
        }

        return "#22c55e";
    }
}

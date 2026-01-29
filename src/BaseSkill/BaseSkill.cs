using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Translation;

namespace SW2_RandomSkills;

public abstract class BaseSkill
{
    protected readonly ILocalizer Localizer = null!;
    protected readonly ISwiftlyCore Core = null!;
    protected BaseSkill(ILocalizer localizer, ISwiftlyCore core)
    {
        Localizer = localizer;
        Core = core;
    }
    public abstract SkillType Type { get; }
    public abstract string Name { get; }
    public virtual string Description
    {
        get
        {
            string skillKey = Name.ToLower().Replace(" ", "_");
            string key = $"{skillKey}.description";

            if (Localizer != null)
            {
                try
                {
                    return GetLocalizedDescription(key);
                }
                catch
                {
                    return GetDefaultDescription();
                }
            }

            return GetDefaultDescription();
        }
    }
    protected virtual object[] GetDescriptionParameters()
    {
        return Array.Empty<object>();
    }

    protected string GetLocalizedDescription(string key)
    {
        try
        {
            var parameters = GetDescriptionParameters();

            if (parameters.Length > 0)
            {
                try
                {
                    string result = Localizer![key, parameters];
                    return result;
                }
                catch (Exception)
                {
                    return Localizer![key];
                }
            }

            string simpleResult = Localizer![key];
            return simpleResult;
        }
        catch (Exception)
        {
            return GetDefaultDescription();
        }
    }
    protected abstract string GetDefaultDescription();
    protected DateTime LastActivation { get; set; } = DateTime.MinValue;
    protected SkillConfig Config { get; private set; } = new();
    protected string GetActivationKey()
    {
        return Config.ActivationKey ?? "";
    }

    public virtual void Initialize(SkillConfig config)
    {
        Config = config;
    }
    public virtual void OnButtonPressed(IPlayer player, GameButtonFlags button) { }
    public virtual void OnButtonReleased(IPlayer player, GameButtonFlags button) { }
    public virtual void OnButtonHeld(IPlayer player, GameButtonFlags button) { }

    public virtual void OnActivationButtonPressed(IPlayer player) { }
    public virtual void OnActivationButtonReleased(IPlayer player) { }
    public virtual void OnActivationButtonHeld(IPlayer player) { }

    public virtual void OnTick(IPlayer player, float deltaTime) { }
    protected bool IsOnCooldown()
    {
        if (Config.Cooldown <= 0) return false;
        return (DateTime.Now - LastActivation).TotalSeconds < Config.Cooldown;
    }
    protected float GetCooldownLeft()
    {
        if (Config.Cooldown <= 0) return 0;
        var elapsed = (float)(DateTime.Now - LastActivation).TotalSeconds;
        return Math.Max(0, Config.Cooldown - elapsed);
    }
    protected void StartCooldown()
    {
        LastActivation = DateTime.Now;
    }
    public abstract void Apply(IPlayer player);
    public virtual void Remove(IPlayer player) { }

    protected T GetParameter<T>(string key, T defaultValue)
    {
        if (Config.Parameters.TryGetValue(key, out object? value))
        {
            if (value == null)
                return defaultValue;

            try
            {
                if (value is T typedValue)
                    return typedValue;

                return (T)Convert.ChangeType(value, typeof(T))!;
            }
            catch (Exception ex)
            {
                SW2_RandomSkills.Core.Logger.LogError($"Failed to convert parameter '{key}' from {value.GetType().Name} to {typeof(T).Name}: {ex.Message}. Using default value.");
                return defaultValue;
            }
        }
        return defaultValue;
    }
}
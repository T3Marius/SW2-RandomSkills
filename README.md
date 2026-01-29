<div align="center">
  <img src="https://pan.samyyc.dev/s/VYmMXE" />
  <h2><strong>SW2-RandomSkills</strong></h2>
  <h3>Gives each player a random skill every round. Configurable via config.</h3>
</div>

<p align="center">
  <img src="https://img.shields.io/github/actions/workflow/status/T3Marius/SW2-RandomSkills/release.yml" alt="Build Status">
  <img src="https://img.shields.io/github/downloads/T3Marius/SW2-RandomSkills/total" alt="Downloads">
  <img src="https://img.shields.io/github/stars/T3Marius/SW2-RandomSkills?style=flat&logo=github" alt="Stars">
  <img src="https://img.shields.io/github/license/T3Marius/SW2-RandomSkills" alt="License">
  <img src="https://img.shields.io/github/v/release/T3Marius/SW2-RandomSkills" alt="Latest Release">
</p>

# Creating A Skill
- In order to create a new skill, do this:
1. Clone the repository
2. Go into src\skills\
3. Create a file with your skill name e.g Jetpack.cs
4. Here's a skill example:

## Info
- You already have acces to Core and Localizer from the BaseSkill class.
- You also have overrides voids like OnButtonReleased, OnButtonPressed, etc.
- To learn more just check the other skils i've made.

```c#
namespace SW2_RandomSkills;

public class RandomHealthSkill(ILocalizer localizer, ISwiftlyCore core) : BaseSkill(localizer, core)
{
    public override SkillType Type => SkillType.RandomHealth; // Set the skill type
    public override string Name => "Random Health"; // Skill Name, this will be shown in rolling
    private Random _random = new();
    private string _randomHealthValues = null!; // we will get this variable value from config.
    protected override string GetDefaultDescription()
    {
        return "Gives you a random amount of health!";
    }
    public override void Initialize(SkillConfig config)
    {
        base.Initialize(config);
        _randomHealthValues = GetParameter("RandomHealthValues", "100,125,200,255,300"); // get the variable values from config and set default values as a fallback.
    }
    public override void Apply(IPlayer player) // this is called OnPlayerSpawn
    {
        string[] healths = _randomHealthValues.Split(",", StringSplitOptions.RemoveEmptyEntries);

        string randomHealthSelected = healths[_random.Next(healths.Count())];

        if (!int.TryParse(randomHealthSelected, out int randomHealth))
        {
            SW2_RandomSkills.Core.Logger.LogInformation($"Invalid random health value at Random Health skill. Please enter an integer.");
            return;
        }

        player.SetHealth(randomHealth);

        player.SendChat(Localizer["prefix"] + Localizer["random_health.given", randomHealth]);

    }
}
```

- After you created your own skill, go into src/SW2-RandomSkills.cs and register it like this OnLoad:
```c#
_skillManager.RegisterSkill(new RandomHealthSkill(_localizer, Core));
```

# BaseSkill Class
- ## This is the base skill class. You can see each feature you can use it in your own skill class from here.
```c#
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
```

# Config
```json
{
  "RandomSkills": {
    "EnableSkillMessages": true,
    "Sounds": {
      "ScrollSound": {
        "Name": "UI.CrateItemScroll",
        "Volume": 0.5
      },
      "OpenSound": {
        "Name": "UI.CrateOpen",
        "Volume": 0.5
      },
      "DisplaySound": {
        "Name": "UI.CrateDisplay",
        "Volume": 0.5
      }
    },
    "Skills": {
      "SwapPosition": {
        "Weight": 25,
        "Cooldown": 30.0,
        "ActivationKey": "F",
        "Parameters": {
          "MaxRange": 2000.0,
          "SwapParticleEffect": "effect.vpcf"
        }
      },
      "SpeedBoost": {
        "Weight": 25,
        "Cooldown": 0,
        "ActivationKey": "",
        "Parameters": {
          "SpeedDelay": 2,
          "SpeedBoost": 1.5
        }
      },
      "ExtraDamage": {
        "Weight": 25,
        "Cooldown": 0,
        "ActivationKey": "",
        "Parameters": {
          "ExtraDamage": 30
        }
      },
      "Teleport": {
        "Weight": 25,
        "Cooldown": 30,
        "ActivationKey": "F"
      },
      "RandomHealth": {
        "Weight": 25,
        "Cooldown": 0,
        "ActivationKey": "",
        "Parameters": {
          "RandomHealthValues": "105,125,200,255,300"
        }
      },
      "Tank": {
        "Weight": 25,
        "Cooldown": 0,
        "ActivationKey": "",
        "Parameters": {
        "TankHealth": 500,
	      "TankSpeed": 0.5
        }
      }
    }
  }
}
```
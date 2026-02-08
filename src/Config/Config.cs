using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace SW2_RandomSkills;

public sealed class SkillsConfig
{
    public bool EnableSkillMessages { get; set; } = true;
    public RollConfig RollSettings { get; set; } = new();
    public SoundsConfig Sounds { get; set; } = new();
    [ConfigurationKeyName("Skills")]
    public Dictionary<string, SkillConfig>? SkillsRaw { get; set; }

    [JsonIgnore]
    public Dictionary<SkillType, SkillConfig> Skills { get; set; } = new();
}
public class SoundsConfig
{
    public ScrollSound ScrollSound { get; set; } = new();
    public OpenSound OpenSound { get; set; } = new();
    public DisplaySound DisplaySound { get; set; } = new();
}
public class ScrollSound
{
    public string Name { get; set; } = "UI.CrateItemScroll";
    public float Volume { get; set; } = 0.5f;
}
public class OpenSound
{
    public string Name { get; set; } = "UI.CrateOpen";
    public float Volume { get; set; } = 0.5f;
}
public class DisplaySound
{
    public string Name { get; set; } = "UI.CrateDisplay";
    public float Volume { get; set; } = 0.5f;
}

public class SkillConfig
{
    public int Weight { get; set; } = 10;
    public float Cooldown { get; set; } = 0.0f;
    public string ActivationKey { get; set; } = "";
    public Dictionary<string, object> Parameters { get; set; } = new();
}
public class RollConfig
{
    public int SkillsToShow { get; set; } = 3;
    public float InitialSpeed { get; set; } = 0.05f;
    public float SlowdownRate { get; set; } = 0.75f;
    public float MinSpeed { get; set; } = 0.2f;
    public int TotalRolls { get; set; } = 25;
}
public enum SkillType
{
    None,
    Tank,
    SpeedBoost,
    ExtraDamage,
    RandomHealth,
    Shield,
    SwapPosition,
    Jetpack,
    Teleport,
    Noclip
}
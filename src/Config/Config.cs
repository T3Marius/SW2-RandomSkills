using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Binder;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace SW2_RandomSkills;

public sealed class SkillsConfig
{
    public bool EnableSkillMessages { get; set; } = true;
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
    Teleport
}
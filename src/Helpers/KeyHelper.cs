
using SwiftlyS2.Shared.Events;

namespace SW2_RandomSkills;

public static class KeyHelper
{
    private static readonly Dictionary<string, GameButtonFlags> _keyMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        ["F"] = GameButtonFlags.F,
        ["CTRL"] = GameButtonFlags.Ctrl,
        ["SPACE"] = GameButtonFlags.Space,
        ["E"] = GameButtonFlags.E,
        ["R"] = GameButtonFlags.R,
        ["MOUSE1"] = GameButtonFlags.Mouse1,
        ["MOUSE2"] = GameButtonFlags.Mouse2,
        ["SHIFT"] = GameButtonFlags.Shift,
        ["TAB"] = GameButtonFlags.Tab,
        ["W"] = GameButtonFlags.W,
        ["S"] = GameButtonFlags.S,
        ["A"] = GameButtonFlags.A,
        ["D"] = GameButtonFlags.D,
        ["ESC"] = GameButtonFlags.Esc,
    };

    public static bool TryParseConfigKey(string configKey, out GameButtonFlags button)
    {
        // Mapping direct din șirul de configurare (ex: "F", "CTRL")
        return configKey.ToUpper() switch
        {
            "F" => SetValue(out button, GameButtonFlags.F),
            "CTRL" => SetValue(out button, GameButtonFlags.Ctrl),
            "SPACE" => SetValue(out button, GameButtonFlags.Space),
            "E" => SetValue(out button, GameButtonFlags.E),
            "R" => SetValue(out button, GameButtonFlags.R),
            "MOUSE1" => SetValue(out button, GameButtonFlags.Mouse1),
            "MOUSE2" => SetValue(out button, GameButtonFlags.Mouse2),
            "SHIFT" => SetValue(out button, GameButtonFlags.Shift),
            _ => SetValue(out button, GameButtonFlags.None)
        };
    }
    private static bool SetValue(out GameButtonFlags button, GameButtonFlags value)
    {
        button = value;
        return button != GameButtonFlags.None;
    }

    public static string GetKeyName(GameButtonFlags button)
    {
        return button switch
        {
            GameButtonFlags.F => "F",
            GameButtonFlags.Ctrl => "CTRL",
            GameButtonFlags.Space => "SPACE",
            GameButtonFlags.E => "E",
            GameButtonFlags.R => "R",
            GameButtonFlags.Mouse1 => "MOUSE1",
            GameButtonFlags.Mouse2 => "MOUSE2",
            GameButtonFlags.Shift => "SHIFT",
            GameButtonFlags.Tab => "TAB",
            GameButtonFlags.W => "W",
            GameButtonFlags.S => "S",
            GameButtonFlags.A => "A",
            GameButtonFlags.D => "D",
            GameButtonFlags.Esc => "ESC",
            _ => button.ToString()
        };
    }
}
using Microsoft.UI.Xaml.Media;

namespace Patch;

/// <summary>
/// Color and brush parsing utilities.
/// Supports named colors, hex (#RRGGBB, #AARRGGBB), and direct Color values.
/// </summary>
public static class BrushHelper
{
    /// <summary>
    /// Parses a color string into a SolidColorBrush.
    /// Supports named colors (red, green, blue, white, black, gray, lightgray, transparent)
    /// and hex codes (#RRGGBB or #AARRGGBB).
    /// </summary>
    public static SolidColorBrush Parse(string color)
    {
        var c = color.ToLowerInvariant() switch
        {
            "red" => Windows.UI.Color.FromArgb(255, 255, 0, 0),
            "green" => Windows.UI.Color.FromArgb(255, 0, 128, 0),
            "blue" => Windows.UI.Color.FromArgb(255, 0, 0, 255),
            "white" => Windows.UI.Color.FromArgb(255, 255, 255, 255),
            "black" => Windows.UI.Color.FromArgb(255, 0, 0, 0),
            "gray" or "grey" => Windows.UI.Color.FromArgb(255, 128, 128, 128),
            "lightgray" or "lightgrey" => Windows.UI.Color.FromArgb(255, 211, 211, 211),
            "transparent" => Windows.UI.Color.FromArgb(0, 0, 0, 0),
            _ when color.StartsWith('#') => ParseHex(color),
            _ => Windows.UI.Color.FromArgb(255, 128, 128, 128),
        };
        return new SolidColorBrush(c);
    }

    internal static Windows.UI.Color ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
            return Windows.UI.Color.FromArgb(255,
                byte.Parse(hex[0..2], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber));
        if (hex.Length == 8)
            return Windows.UI.Color.FromArgb(
                byte.Parse(hex[0..2], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex[6..8], System.Globalization.NumberStyles.HexNumber));
        return Windows.UI.Color.FromArgb(255, 128, 128, 128);
    }
}

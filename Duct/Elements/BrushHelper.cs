using System.Collections.Concurrent;
using Microsoft.UI.Xaml.Media;

namespace Duct;

/// <summary>
/// Color and brush parsing utilities.
/// Supports named colors, hex (#RRGGBB, #AARRGGBB), and direct Color values.
/// Brushes are cached by color string to avoid creating heavyweight DependencyObjects every render.
/// </summary>
public static class BrushHelper
{
    private static readonly ConcurrentDictionary<string, SolidColorBrush> _cache = new();

    /// <summary>
    /// Parses a color string into a SolidColorBrush.
    /// Supports named colors (red, green, blue, white, black, gray, lightgray, transparent)
    /// and hex codes (#RRGGBB or #AARRGGBB).
    /// Results are cached — repeated calls with the same string return the same brush instance.
    /// </summary>
    public static SolidColorBrush Parse(string color)
    {
        return _cache.GetOrAdd(color, static c =>
        {
            var parsed = c.ToLowerInvariant() switch
            {
                "red" => Windows.UI.Color.FromArgb(255, 255, 0, 0),
                "green" => Windows.UI.Color.FromArgb(255, 0, 128, 0),
                "blue" => Windows.UI.Color.FromArgb(255, 0, 0, 255),
                "white" => Windows.UI.Color.FromArgb(255, 255, 255, 255),
                "black" => Windows.UI.Color.FromArgb(255, 0, 0, 0),
                "gray" or "grey" => Windows.UI.Color.FromArgb(255, 128, 128, 128),
                "lightgray" or "lightgrey" => Windows.UI.Color.FromArgb(255, 211, 211, 211),
                "transparent" => Windows.UI.Color.FromArgb(0, 0, 0, 0),
                _ when c.StartsWith('#') => ParseHex(c),
                _ => Windows.UI.Color.FromArgb(255, 128, 128, 128),
            };
            return new SolidColorBrush(parsed);
        });
    }

    internal static Windows.UI.Color ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6
            && byte.TryParse(hex[0..2], System.Globalization.NumberStyles.HexNumber, null, out var r6)
            && byte.TryParse(hex[2..4], System.Globalization.NumberStyles.HexNumber, null, out var g6)
            && byte.TryParse(hex[4..6], System.Globalization.NumberStyles.HexNumber, null, out var b6))
        {
            return Windows.UI.Color.FromArgb(255, r6, g6, b6);
        }
        if (hex.Length == 8
            && byte.TryParse(hex[0..2], System.Globalization.NumberStyles.HexNumber, null, out var a8)
            && byte.TryParse(hex[2..4], System.Globalization.NumberStyles.HexNumber, null, out var r8)
            && byte.TryParse(hex[4..6], System.Globalization.NumberStyles.HexNumber, null, out var g8)
            && byte.TryParse(hex[6..8], System.Globalization.NumberStyles.HexNumber, null, out var b8))
        {
            return Windows.UI.Color.FromArgb(a8, r8, g8, b8);
        }
        // Fallback to gray for malformed hex, consistent with named color fallback
        return Windows.UI.Color.FromArgb(255, 128, 128, 128);
    }
}

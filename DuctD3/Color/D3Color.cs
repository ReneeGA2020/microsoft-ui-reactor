// Port of d3-color — ISC License, Copyright 2010-2023 Mike Bostock
// Simplified for chart usage: parse CSS color strings, manipulate RGB/HSL

namespace Duct.D3;

/// <summary>
/// CSS color utilities. Parses hex, rgb(), hsl(), and named colors.
/// Provides brighter/darker manipulation for chart theming.
/// </summary>
public readonly struct D3Color
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public double Opacity { get; }

    public D3Color(byte r, byte g, byte b, double opacity = 1.0)
    {
        R = r; G = g; B = b; Opacity = Math.Clamp(opacity, 0, 1);
    }

    public D3Color Brighter(double k = 1)
    {
        double factor = Math.Pow(1.0 / 0.7, k);
        return new D3Color(
            ClampByte(R * factor),
            ClampByte(G * factor),
            ClampByte(B * factor),
            Opacity);
    }

    public D3Color Darker(double k = 1)
    {
        double factor = Math.Pow(0.7, k);
        return new D3Color(
            ClampByte(R * factor),
            ClampByte(G * factor),
            ClampByte(B * factor),
            Opacity);
    }

    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";
    public string ToRgb() => Opacity < 1 ? $"rgba({R}, {G}, {B}, {Opacity})" : $"rgb({R}, {G}, {B})";
    public override string ToString() => ToRgb();

    private static byte ClampByte(double v) => (byte)Math.Clamp(Math.Round(v), 0, 255);

    // ── Parsing ────────────────────────────────────────────────────────

    public static D3Color Parse(string format)
    {
        format = format.Trim().ToLowerInvariant();

        // Hex
        if (format.StartsWith('#'))
        {
            string hex = format[1..];
            if (hex.Length == 3)
                return new D3Color(
                    (byte)(Convert.ToByte(hex[0..1], 16) * 17),
                    (byte)(Convert.ToByte(hex[1..2], 16) * 17),
                    (byte)(Convert.ToByte(hex[2..3], 16) * 17));
            if (hex.Length == 6)
                return new D3Color(
                    Convert.ToByte(hex[0..2], 16),
                    Convert.ToByte(hex[2..4], 16),
                    Convert.ToByte(hex[4..6], 16));
        }

        // Named colors (common chart colors)
        if (NamedColors.TryGetValue(format, out var named))
            return named;

        return new D3Color(0, 0, 0);
    }

    // ── Predefined palettes for charting ───────────────────────────────

    /// <summary>D3's category10 color scheme — 10 distinct colors for categorical data.</summary>
    public static readonly D3Color[] Category10 =
    [
        Parse("#1f77b4"), Parse("#ff7f0e"), Parse("#2ca02c"), Parse("#d62728"), Parse("#9467bd"),
        Parse("#8c564b"), Parse("#e377c2"), Parse("#7f7f7f"), Parse("#bcbd22"), Parse("#17becf"),
    ];

    /// <summary>Tableau10 color scheme — another popular 10-color categorical palette.</summary>
    public static readonly D3Color[] Tableau10 =
    [
        Parse("#4e79a7"), Parse("#f28e2b"), Parse("#e15759"), Parse("#76b7b2"), Parse("#59a14f"),
        Parse("#edc948"), Parse("#b07aa1"), Parse("#ff9da7"), Parse("#9c755f"), Parse("#bab0ac"),
    ];

    private static readonly Dictionary<string, D3Color> NamedColors = new()
    {
        ["red"] = new(255, 0, 0),
        ["green"] = new(0, 128, 0),
        ["blue"] = new(0, 0, 255),
        ["white"] = new(255, 255, 255),
        ["black"] = new(0, 0, 0),
        ["orange"] = new(255, 165, 0),
        ["yellow"] = new(255, 255, 0),
        ["purple"] = new(128, 0, 128),
        ["gray"] = new(128, 128, 128),
        ["grey"] = new(128, 128, 128),
        ["steelblue"] = new(70, 130, 180),
        ["tomato"] = new(255, 99, 71),
        ["coral"] = new(255, 127, 80),
        ["teal"] = new(0, 128, 128),
        ["navy"] = new(0, 0, 128),
        ["gold"] = new(255, 215, 0),
        ["crimson"] = new(220, 20, 60),
        ["darkgreen"] = new(0, 100, 0),
        ["dodgerblue"] = new(30, 144, 255),
        ["transparent"] = new(0, 0, 0, 0),
    };
}

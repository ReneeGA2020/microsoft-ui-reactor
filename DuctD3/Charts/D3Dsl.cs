// Declarative D3 drawing DSL for Duct's virtual tree.
// Replaces imperative G.AddRect/AddLine/AddEllipse/AddText/MakePath patterns
// with composable Duct Elements that work with the reconciler.
//
// Usage: using static Duct.D3.Charts.D3;

using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using static Duct.UI;

namespace Duct.D3.Charts;

/// <summary>
/// Static factory methods for declarative D3 chart drawing.
/// Import with: using static Duct.D3.Charts.D3;
/// </summary>
public static class D3
{
    // ── Color helpers ───────────────────────────────────────────────────

    public static readonly D3Color[] Palette = D3Color.Category10;

    public static SolidColorBrush Brush(string color)
    {
        var c = D3Color.Parse(color);
        return new SolidColorBrush(Windows.UI.Color.FromArgb((byte)(c.Opacity * 255), c.R, c.G, c.B));
    }

    public static SolidColorBrush Brush(D3Color c)
        => new(Windows.UI.Color.FromArgb((byte)(c.Opacity * 255), c.R, c.G, c.B));

    public static SolidColorBrush Brush(D3Color c, double opacity)
        => new(Windows.UI.Color.FromArgb((byte)(opacity * 255), c.R, c.G, c.B));

    public static SolidColorBrush Brush(string color, double opacity)
        => Brush(D3Color.Parse(color), opacity);

    public static SolidColorBrush Gray(byte v, byte alpha = 255)
        => new(Windows.UI.Color.FromArgb(alpha, v, v, v));

    public static string Fmt(double v) =>
        Math.Abs(v) >= 1e6 ? (v / 1e6).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "M" :
        Math.Abs(v) >= 1e3 ? (v / 1e3).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "k" :
        v == Math.Floor(v) ? v.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) : v.ToString("G4", System.Globalization.CultureInfo.InvariantCulture);

    // ── Canvas ──────────────────────────────────────────────────────────

    /// <summary>Creates a Canvas element with the given dimensions and children.</summary>
    public static CanvasElement D3Canvas(double width, double height, params Element?[] children) =>
        Canvas(children) with
        {
            Width = width,
            Height = height,
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
        };

    // ── Primitive shapes ────────────────────────────────────────────────

    /// <summary>Creates a positioned rectangle on a Canvas.</summary>
    public static RectangleElement D3Rect(double x, double y, double width, double height) =>
        new RectangleElement()
            .Width(Math.Max(0, width)).Height(Math.Max(0, height))
            .Canvas(x, y);

    /// <summary>Creates a circle (ellipse) positioned at center (cx, cy) with radius r.</summary>
    public static EllipseElement D3Circle(double cx, double cy, double r) =>
        new EllipseElement()
            .Width(r * 2).Height(r * 2)
            .Canvas(cx - r, cy - r);

    /// <summary>Creates a line between two points.</summary>
    public static LineElement D3Line(double x1, double y1, double x2, double y2) =>
        new() { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 };

    /// <summary>Creates a path from SVG path data string.  Accepts null pathData gracefully (renders nothing).</summary>
    public static PathElement D3Path(string? pathData, Brush? stroke = null, Brush? fill = null, double strokeWidth = 1.5) =>
        new()
        {
            Data = pathData != null ? PathDataParser.Parse(pathData) : null,
            PathDataString = pathData,
            Stroke = stroke,
            Fill = fill,
            StrokeThickness = strokeWidth,
        };

    /// <summary>Creates a path from SVG path data with a translate transform.  Accepts null pathData gracefully (renders nothing).</summary>
    public static PathElement D3PathTranslated(string? pathData, double translateX, double translateY, Brush? stroke = null, Brush? fill = null, double strokeWidth = 1.5) =>
        new()
        {
            Data = pathData != null ? PathDataParser.Parse(pathData) : null,
            PathDataString = pathData,
            Stroke = stroke,
            Fill = fill,
            StrokeThickness = strokeWidth,
            RenderTransform = new TranslateTransform { X = translateX, Y = translateY },
        };

    // ── Text ────────────────────────────────────────────────────────────

    /// <summary>Creates a positioned text label on a Canvas.</summary>
    public static TextElement D3Text(double x, double y, string text, double fontSize = 10, Brush? foreground = null) =>
        Text(text)
            .FontSize(fontSize)
            .Foreground(foreground ?? Gray(100))
            .Canvas(x, y);

    /// <summary>Creates a positioned text label with right alignment and explicit width (for Y axis labels).</summary>
    public static TextElement D3TextRight(double x, double y, string text, double width, double fontSize = 10, Brush? foreground = null) =>
        Text(text)
            .FontSize(fontSize)
            .Foreground(foreground ?? Gray(100))
            .Width(width)
            .TextAlignment(TextAlignment.Right)
            .Canvas(x, y);

    /// <summary>Creates a positioned text label with center alignment and explicit width.</summary>
    public static TextElement D3TextCenter(double x, double y, string text, double width, double fontSize = 10, Brush? foreground = null) =>
        Text(text)
            .FontSize(fontSize)
            .Foreground(foreground ?? Gray(100))
            .Width(width)
            .TextAlignment(TextAlignment.Center)
            .Canvas(x, y);

    // ── Generator helpers (functional one-shot wrappers) ──────────────

    /// <summary>Creates a line path element directly from data, collapsing LineGenerator + Generate + D3Path into one expression.</summary>
    public static PathElement D3LinePath<T>(IReadOnlyList<T> data, Func<T, double> x, Func<T, double> y,
        Brush? stroke = null, double strokeWidth = 1.5,
        CurveFactory? curve = null, Func<T, int, bool>? defined = null)
    {
        var gen = LineGenerator.Create(x, y);
        if (curve != null) gen.SetCurve(curve);
        if (defined != null) gen.SetDefined(defined);
        return D3Path(gen.Generate(data), stroke: stroke, strokeWidth: strokeWidth);
    }

    /// <summary>Creates an area path element directly from data, collapsing AreaGenerator + Generate + D3Path into one expression.</summary>
    public static PathElement D3AreaPath<T>(IReadOnlyList<T> data, Func<T, double> x, Func<T, double> y0, Func<T, double> y1,
        Brush? fill = null, Brush? stroke = null, double strokeWidth = 1.5)
    {
        var gen = AreaGenerator.Create(x, y0, y1);
        return D3Path(gen.Generate(data), stroke: stroke, fill: fill, strokeWidth: strokeWidth);
    }

    /// <summary>Creates an arc sector path element at (cx, cy), collapsing ArcGenerator + Generate + D3PathTranslated into one expression.</summary>
    public static PathElement D3ArcPath(double startAngle, double endAngle, double cx, double cy,
        double innerRadius = 0, double outerRadius = 100,
        double padAngle = 0, Brush? fill = null, Brush? stroke = null, double strokeWidth = 1.5)
    {
        var pathData = new ArcGenerator()
            .SetInnerRadius(innerRadius)
            .SetOuterRadius(outerRadius)
            .Generate(startAngle, endAngle, padAngle);
        return D3PathTranslated(pathData, cx, cy, stroke: stroke, fill: fill, strokeWidth: strokeWidth);
    }

    /// <summary>Creates pie/donut slice elements directly from data, collapsing PieGenerator + ArcGenerator + iteration into one expression.</summary>
    public static Element[] D3Pie<T>(IReadOnlyList<T> data, Func<T, double> value, double cx, double cy,
        double outerRadius = 150, double innerRadius = 0,
        double padAngle = 0, bool sort = true,
        Brush? stroke = null, double strokeWidth = 1.5)
    {
        var arcs = PieGenerator.Generate(data, value, sort, padAngle);
        var arc = new ArcGenerator().SetOuterRadius(outerRadius).SetInnerRadius(innerRadius);
        return arcs.Select((a, i) =>
            (Element)D3PathTranslated(arc.Generate(a), cx, cy,
                fill: Brush(Palette[i % Palette.Length]),
                stroke: stroke,
                strokeWidth: strokeWidth)
        ).ToArray();
    }

    // ── Composite chart helpers ─────────────────────────────────────────

    /// <summary>Creates a vertical bezier tree link path between two points (parent to child).</summary>
    public static PathElement D3Link(double x1, double y1, double x2, double y2, Brush? stroke = null, double strokeWidth = 1.5)
    {
        double my = (y1 + y2) / 2;
        var pb = new PathBuilder(3);
        pb.MoveTo(x1, y1);
        pb.BezierCurveTo(x1, my, x2, my, x2, y2);
        return D3Path(pb.ToString(), stroke, null, strokeWidth);
    }

    /// <summary>Creates a legend as rect+text pairs laid out vertically.</summary>
    public static Element[] D3Legend(double x, double y, IEnumerable<(string label, SolidColorBrush color)> items, double fontSize = 11)
    {
        return items.SelectMany((item, i) => new Element[]
        {
            D3Rect(x, y + i * 22, 14, 14) with { Fill = item.color, RadiusX = 2, RadiusY = 2 },
            D3Text(x + 20, y + i * 22, item.label, fontSize, Gray(60)),
        }).ToArray();
    }


    /// <summary>Creates X and Y axis lines with tick labels as a flat array of Elements.</summary>
    public static Element[] D3Axes(LinearScale xs, LinearScale ys,
        double left, double top, double width, double height, int xTicks = 6, int yTicks = 5)
    {
        var ab = Gray(100, 180);
        double bot = top + height;
        var elements = new List<Element>
        {
            D3Line(left, bot, left + width, bot) with { Stroke = ab, StrokeThickness = 1 },
            D3Line(left, top, left, bot) with { Stroke = ab, StrokeThickness = 1 },
        };

        foreach (var t in xs.Ticks(xTicks))
            elements.Add(D3Text(xs.Map(t) - 12, bot + 4, Fmt(t), 10, ab));

        foreach (var t in ys.Ticks(yTicks))
            elements.Add(D3TextRight(0, ys.Map(t) - 7, Fmt(t), left - 6, 10, ab));

        return elements.ToArray();
    }

    /// <summary>Creates horizontal grid lines as a flat array of Elements.</summary>
    public static Element[] D3Grid(LinearScale ys, double left, double width, int ticks = 5)
    {
        var gb = Gray(128, 40);
        return ys.Ticks(ticks)
            .Select(t => (Element)(D3Line(left, ys.Map(t), left + width, ys.Map(t)) with { Stroke = gb, StrokeThickness = 1 }))
            .ToArray();
    }
}

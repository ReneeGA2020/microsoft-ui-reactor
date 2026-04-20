using Microsoft.UI.Reactor.Charting.D3;
// Declarative D3 drawing DSL for Reactor's virtual tree.
// Replaces imperative G.AddRect/AddLine/AddEllipse/AddText/MakePath patterns
// with composable Reactor Elements that work with the reconciler.
//
// Usage: using static Microsoft.UI.Reactor.Charting.D3Dsl;

using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.Charting;

/// <summary>
/// Static factory methods for declarative D3 chart drawing.
/// Import with: using static Microsoft.UI.Reactor.Charting.D3Dsl;
/// </summary>
public static class D3Dsl
{
    // ── Color helpers ───────────────────────────────────────────────────

    public static readonly IReadOnlyList<D3Color> Palette = D3Color.Category10;

    public static SolidColorBrush Brush(string color)
    {
        var c = D3Color.Parse(color);
        return new SolidColorBrush(global::Windows.UI.Color.FromArgb((byte)(c.Opacity * 255), c.R, c.G, c.B));
    }

    public static SolidColorBrush Brush(D3Color c)
        => new(global::Windows.UI.Color.FromArgb((byte)(c.Opacity * 255), c.R, c.G, c.B));

    public static SolidColorBrush Brush(D3Color c, double opacity)
        => new(global::Windows.UI.Color.FromArgb((byte)(opacity * 255), c.R, c.G, c.B));

    public static SolidColorBrush Brush(string color, double opacity)
        => Brush(D3Color.Parse(color), opacity);

    public static SolidColorBrush Gray(byte v, byte alpha = 255)
        => new(global::Windows.UI.Color.FromArgb(alpha, v, v, v));

    // ── Theme-aware chart chrome brushes ──────────────────────────────
    //
    // Charts draw axes, gridlines, labels and titles that need enough contrast
    // on both light and dark surfaces. Host applications set IsDarkTheme once
    // per render (e.g. from the app's light/dark toggle) and all chart helpers
    // — and any user code using ChartForeground/ChartAxis/etc. — pick the right
    // brush automatically.

    [ThreadStatic] private static bool _isDarkTheme;

    public static bool IsDarkTheme
    {
        get => _isDarkTheme;
        set => _isDarkTheme = value;
    }

    /// <summary>Primary text on a chart surface — titles, strong labels.</summary>
    public static SolidColorBrush ChartForeground =>
        _isDarkTheme ? Gray(235) : Gray(40);

    /// <summary>Secondary text — tick labels, subtle annotations, legend labels.</summary>
    public static SolidColorBrush ChartMutedForeground =>
        _isDarkTheme ? Gray(190) : Gray(90);

    /// <summary>Axis lines + their tick labels.</summary>
    public static SolidColorBrush ChartAxis =>
        _isDarkTheme ? Gray(190, 200) : Gray(100, 180);

    /// <summary>Subtle horizontal/vertical gridlines behind the plot.</summary>
    public static SolidColorBrush ChartGrid =>
        _isDarkTheme ? Gray(200, 50) : Gray(128, 50);

    /// <summary>Solid fill matching the chart's surrounding card — use for gap strokes between colored slices (pie / sunburst / icicle).</summary>
    public static SolidColorBrush ChartSurface =>
        _isDarkTheme ? Gray(32) : Gray(255);

    /// <summary>Translucent surface — for layered ridge fills or separators that blend with the card.</summary>
    public static SolidColorBrush ChartSurfaceAlpha(byte alpha) =>
        _isDarkTheme ? Gray(32, alpha) : Gray(255, alpha);

    /// <summary>Slightly elevated neutral surface — non-leaf tree fills, alternating rows.</summary>
    public static SolidColorBrush ChartSubtleFill =>
        _isDarkTheme ? Gray(60) : Gray(225);

    /// <summary>Subtle neutral stroke — baselines, separators, non-accented borders.</summary>
    public static SolidColorBrush ChartSubtleStroke =>
        _isDarkTheme ? Gray(90) : Gray(185);

    public static string Fmt(double v) =>
        Math.Abs(v) >= 1e6 ? (v / 1e6).ToString("0.#", global::System.Globalization.CultureInfo.InvariantCulture) + "M" :
        Math.Abs(v) >= 1e3 ? (v / 1e3).ToString("0.#", global::System.Globalization.CultureInfo.InvariantCulture) + "k" :
        v == Math.Floor(v) ? v.ToString("F0", global::System.Globalization.CultureInfo.InvariantCulture) : v.ToString("G4", global::System.Globalization.CultureInfo.InvariantCulture);

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
    public static TextBlockElement Text(double x, double y, string text, double fontSize = 10, Brush? foreground = null) =>
        TextBlock(text)
            .FontSize(fontSize)
            .Foreground(foreground ?? ChartMutedForeground)
            .Canvas(x, y);

    /// <summary>Creates a positioned text label with right alignment and explicit width (for Y axis labels).</summary>
    public static TextBlockElement TextRight(double x, double y, string text, double width, double fontSize = 10, Brush? foreground = null) =>
        TextBlock(text)
            .FontSize(fontSize)
            .Foreground(foreground ?? ChartMutedForeground)
            .Width(width)
            .TextAlignment(TextAlignment.Right)
            .Canvas(x, y);

    /// <summary>Creates a positioned text label with center alignment and explicit width.</summary>
    public static TextBlockElement TextCenter(double x, double y, string text, double width, double fontSize = 10, Brush? foreground = null) =>
        TextBlock(text)
            .FontSize(fontSize)
            .Foreground(foreground ?? ChartMutedForeground)
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
                fill: Brush(Palette[i % Palette.Count]),
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
            Text(x + 20, y + i * 22, item.label, fontSize, ChartMutedForeground),
        }).ToArray();
    }


    /// <summary>Creates X and Y axis lines with tick labels as a flat array of Elements.</summary>
    public static Element[] D3Axes(LinearScale xs, LinearScale ys,
        double left, double top, double width, double height, int xTicks = 6, int yTicks = 5)
    {
        var ab = ChartAxis;
        double bot = top + height;
        var elements = new List<Element>
        {
            D3Line(left, bot, left + width, bot) with { Stroke = ab, StrokeThickness = 1 },
            D3Line(left, top, left, bot) with { Stroke = ab, StrokeThickness = 1 },
        };

        foreach (var t in xs.Ticks(xTicks))
            elements.Add(Text(xs.Map(t) - 12, bot + 4, Fmt(t), 10, ab));

        foreach (var t in ys.Ticks(yTicks))
            elements.Add(TextRight(0, ys.Map(t) - 7, Fmt(t), left - 6, 10, ab));

        return elements.ToArray();
    }

    /// <summary>Creates horizontal grid lines as a flat array of Elements.</summary>
    public static Element[] D3Grid(LinearScale ys, double left, double width, int ticks = 5)
    {
        var gb = ChartGrid;
        return ys.Ticks(ticks)
            .Select(t => (Element)(D3Line(left, ys.Map(t), left + width, ys.Map(t)) with { Stroke = gb, StrokeThickness = 1 }))
            .ToArray();
    }
}

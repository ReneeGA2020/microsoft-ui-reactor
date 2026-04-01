// Shared rendering helpers for gallery samples

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;
using Duct.D3;
using Duct.D3.Charts;

namespace DuctD3.Gallery;

/// <summary>
/// Base class for all gallery samples. Each sample provides a title,
/// description, source code snippet, and a Render method that builds the chart canvas.
/// </summary>
public abstract class GallerySample
{
    public abstract string Title { get; }
    public abstract string Description { get; }
    public abstract string Category { get; }
    public abstract string SourceCode { get; }
    public abstract FrameworkElement Render();
}

/// <summary>Shared helpers used by all samples to render charts on a Canvas.</summary>
public static class G
{
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

    public static SolidColorBrush Gray(byte v, byte a = 255)
        => new(Windows.UI.Color.FromArgb(a, v, v, v));

    public static Geometry ParsePath(string pathData) => PathDataParser.Parse(pathData);

    public static WinShapes.Path MakePath(string? pathData, Brush? stroke = null, Brush? fill = null, double strokeWidth = 1.5)
    {
        if (pathData == null) return new WinShapes.Path();
        return new WinShapes.Path
        {
            Data = ParsePath(pathData),
            Stroke = stroke,
            Fill = fill,
            StrokeThickness = strokeWidth,
        };
    }

    public static void AddLine(Canvas c, double x1, double y1, double x2, double y2, Brush stroke, double width = 1)
    {
        c.Children.Add(new WinShapes.Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = stroke, StrokeThickness = width });
    }

    public static void AddEllipse(Canvas c, double cx, double cy, double r, Brush fill, Brush? stroke = null, double strokeWidth = 1)
    {
        var e = new WinShapes.Ellipse { Width = r * 2, Height = r * 2, Fill = fill };
        if (stroke != null) { e.Stroke = stroke; e.StrokeThickness = strokeWidth; }
        Canvas.SetLeft(e, cx - r);
        Canvas.SetTop(e, cy - r);
        c.Children.Add(e);
    }

    public static void AddRect(Canvas c, double x, double y, double w, double h, Brush fill, double rx = 0)
    {
        var r = new WinShapes.Rectangle { Width = Math.Max(0, w), Height = Math.Max(0, h), Fill = fill, RadiusX = rx, RadiusY = rx };
        Canvas.SetLeft(r, x);
        Canvas.SetTop(r, y);
        c.Children.Add(r);
    }

    public static void AddText(Canvas c, double x, double y, string text, double fontSize = 10, Brush? foreground = null, TextAlignment align = TextAlignment.Left, double? width = null)
    {
        var tb = new TextBlock { Text = text, FontSize = fontSize, Foreground = foreground ?? Gray(100) };
        if (align != TextAlignment.Left) tb.TextAlignment = align;
        if (width.HasValue) tb.Width = width.Value;
        Canvas.SetLeft(tb, x);
        Canvas.SetTop(tb, y);
        c.Children.Add(tb);
    }

    public static string Fmt(double v) =>
        Math.Abs(v) >= 1e6 ? $"{v / 1e6:0.#}M" :
        Math.Abs(v) >= 1e3 ? $"{v / 1e3:0.#}k" :
        v == Math.Floor(v) ? v.ToString("F0") : v.ToString("G4");

    /// <summary>Draws X and Y axes with tick labels.</summary>
    public static void DrawAxes(Canvas c, LinearScale xs, LinearScale ys,
        double left, double top, double width, double height, int xTicks = 6, int yTicks = 5)
    {
        var ab = Gray(100, 180);
        double bot = top + height;
        AddLine(c, left, bot, left + width, bot, ab);
        AddLine(c, left, top, left, bot, ab);
        foreach (var t in xs.Ticks(xTicks)) AddText(c, xs.Map(t) - 12, bot + 4, Fmt(t), 10, ab);
        foreach (var t in ys.Ticks(yTicks)) AddText(c, 0, ys.Map(t) - 7, Fmt(t), 10, ab, TextAlignment.Right, left - 6);
    }

    /// <summary>Draws horizontal grid lines.</summary>
    public static void DrawGrid(Canvas c, LinearScale ys, double left, double width, int ticks = 5)
    {
        var gb = Gray(128, 40);
        foreach (var t in ys.Ticks(ticks)) AddLine(c, left, ys.Map(t), left + width, ys.Map(t), gb);
    }
}

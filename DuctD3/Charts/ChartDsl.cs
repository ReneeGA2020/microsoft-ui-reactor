// Ergonomic Duct chart DSL — high-level chart components for Duct's declarative model
// Usage: using static Duct.D3.Charts.ChartDsl;

using Duct.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace Duct.D3.Charts;

/// <summary>
/// Static factory methods that integrate D3 charting into Duct's declarative DSL.
/// Import with: using static Duct.D3.Charts.ChartDsl;
/// </summary>
public static partial class ChartDsl
{
    public static ChartElement<T> LineChart<T>(IReadOnlyList<T> data, Func<T, double> x, Func<T, double> y) =>
        new() { Data = data, XAccessor = x, YAccessor = y, ChartType = ChartType.Line };

    public static ChartElement<T> BarChart<T>(IReadOnlyList<T> data, Func<T, double> x, Func<T, double> y) =>
        new() { Data = data, XAccessor = x, YAccessor = y, ChartType = ChartType.Bar };

    public static ChartElement<T> AreaChart<T>(IReadOnlyList<T> data, Func<T, double> x, Func<T, double> y) =>
        new() { Data = data, XAccessor = x, YAccessor = y, ChartType = ChartType.Area };

    public static PieChartElement<T> PieChart<T>(IReadOnlyList<T> data, Func<T, double> value, Func<T, string>? label = null) =>
        new() { Data = data, ValueAccessor = value, LabelAccessor = label };
}

public enum ChartType { Line, Bar, Area }

// ════════════════════════════════════════════════════════════════════════════
//  ChartElement — Line / Bar / Area
// ════════════════════════════════════════════════════════════════════════════

public sealed class ChartElement<T>
{
    internal IReadOnlyList<T> Data { get; init; } = [];
    internal Func<T, double> XAccessor { get; init; } = _ => 0;
    internal Func<T, double> YAccessor { get; init; } = _ => 0;
    internal ChartType ChartType { get; init; }

    private double _width = 400, _height = 300;
    private double _marginTop = 20, _marginRight = 20, _marginBottom = 30, _marginLeft = 40;
    private string _stroke = "#4285f4", _fill = "#4285f4";
    private double _strokeWidth = 2, _fillOpacity = 0.3;
    private bool _showAxes = true, _showGrid = true;
    private Action<ChartHandle>? _onReady;

    public ChartElement<T> Width(double w) { _width = w; return this; }
    public ChartElement<T> Height(double h) { _height = h; return this; }
    public ChartElement<T> Margin(double top, double right, double bottom, double left) { _marginTop = top; _marginRight = right; _marginBottom = bottom; _marginLeft = left; return this; }
    public ChartElement<T> Stroke(string color) { _stroke = color; return this; }
    public ChartElement<T> Fill(string color) { _fill = color; return this; }
    public ChartElement<T> StrokeWidth(double w) { _strokeWidth = w; return this; }
    public ChartElement<T> FillOpacity(double o) { _fillOpacity = o; return this; }
    public ChartElement<T> ShowAxes(bool show) { _showAxes = show; return this; }
    public ChartElement<T> ShowGrid(bool show) { _showGrid = show; return this; }

    /// <summary>
    /// Called after the chart is rendered. The handle exposes a Redraw method
    /// that re-renders the chart with new data without recreating the Canvas.
    /// </summary>
    public ChartElement<T> OnReady(Action<ChartHandle> callback) { _onReady = callback; return this; }

    public Element ToElement() => new XamlHostElement(BuildCanvas, UpdateCanvas) { TypeKey = $"DuctD3Chart_{ChartType}" };
    public static implicit operator Element(ChartElement<T> chart) => chart.ToElement();

    private FrameworkElement BuildCanvas()
    {
        var canvas = new Canvas { Width = _width, Height = _height, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) };
        FullRender(canvas, Data);
        _onReady?.Invoke(new ChartHandle(canvas, data => FullRender(canvas, (IReadOnlyList<T>)data)));
        return canvas;
    }

    private void UpdateCanvas(FrameworkElement fe)
    {
        if (fe is Canvas canvas)
        {
            FullRender(canvas, Data);
            _onReady?.Invoke(new ChartHandle(canvas, data => FullRender(canvas, (IReadOnlyList<T>)data)));
        }
    }

    private void FullRender(Canvas canvas, IReadOnlyList<T> data)
    {
        canvas.Children.Clear();
        if (data.Count == 0) return;

        double plotLeft = _marginLeft, plotTop = _marginTop;
        double plotWidth = _width - _marginLeft - _marginRight;
        double plotHeight = _height - _marginTop - _marginBottom;

        var (xMin, xMax) = D3Extent.Extent(data, XAccessor);
        var (yMin, yMax) = D3Extent.Extent(data, YAccessor);
        var xScale = new LinearScale([xMin, xMax], [plotLeft, plotLeft + plotWidth]).Nice();
        var yScale = new LinearScale([yMin, yMax], [plotTop + plotHeight, plotTop]).Nice();

        if (_showGrid) RenderGrid(canvas, yScale, plotLeft, plotWidth);

        switch (ChartType)
        {
            case ChartType.Line: RenderLine(canvas, data, xScale, yScale); break;
            case ChartType.Bar: RenderBars(canvas, data, xScale, yScale, plotTop, plotWidth, plotHeight); break;
            case ChartType.Area: RenderArea(canvas, data, xScale, yScale, plotTop, plotHeight); break;
        }

        if (_showAxes) RenderAxes(canvas, xScale, yScale, plotLeft, plotTop, plotWidth, plotHeight);
    }

    private void RenderLine(Canvas c, IReadOnlyList<T> data, LinearScale xs, LinearScale ys)
    {
        var gen = LineGenerator.Create<T>(d => xs.Map(XAccessor(d)), d => ys.Map(YAccessor(d)));
        string? pd = gen.Generate(data);
        if (pd != null) c.Children.Add(new WinShapes.Path { Data = ParsePathData(pd), Stroke = ColorToBrush(_stroke), StrokeThickness = _strokeWidth });
    }

    private void RenderArea(Canvas c, IReadOnlyList<T> data, LinearScale xs, LinearScale ys, double plotTop, double plotHeight)
    {
        double baseline = plotTop + plotHeight;
        var gen = AreaGenerator.Create<T>(d => xs.Map(XAccessor(d)), _ => baseline, d => ys.Map(YAccessor(d)));
        string? pd = gen.Generate(data);
        if (pd != null) { var f = ColorToBrush(_fill); f.Opacity = _fillOpacity; c.Children.Add(new WinShapes.Path { Data = ParsePathData(pd), Fill = f }); }
        RenderLine(c, data, xs, ys);
    }

    private void RenderBars(Canvas c, IReadOnlyList<T> data, LinearScale xs, LinearScale ys, double plotTop, double plotWidth, double plotHeight)
    {
        double barW = Math.Max(1, plotWidth / data.Count * 0.8);
        double baseline = plotTop + plotHeight;
        for (int i = 0; i < data.Count; i++)
        {
            double cx = xs.Map(XAccessor(data[i])), cy = ys.Map(YAccessor(data[i]));
            var r = new WinShapes.Rectangle { Width = barW, Height = Math.Max(0, baseline - cy), Fill = ColorToBrush(_fill), RadiusX = 2, RadiusY = 2 };
            Canvas.SetLeft(r, cx - barW / 2); Canvas.SetTop(r, cy);
            c.Children.Add(r);
        }
    }

    private static void RenderGrid(Canvas c, LinearScale ys, double plotLeft, double plotWidth)
    {
        var b = new SolidColorBrush(Windows.UI.Color.FromArgb(40, 128, 128, 128));
        foreach (var t in ys.Ticks(5)) { double y = ys.Map(t); c.Children.Add(new WinShapes.Line { X1 = plotLeft, Y1 = y, X2 = plotLeft + plotWidth, Y2 = y, Stroke = b, StrokeThickness = 1 }); }
    }

    private static void RenderAxes(Canvas c, LinearScale xs, LinearScale ys, double plotLeft, double plotTop, double plotWidth, double plotHeight)
    {
        var ab = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 100, 100, 100));
        double bot = plotTop + plotHeight;
        c.Children.Add(new WinShapes.Line { X1 = plotLeft, Y1 = bot, X2 = plotLeft + plotWidth, Y2 = bot, Stroke = ab, StrokeThickness = 1 });
        c.Children.Add(new WinShapes.Line { X1 = plotLeft, Y1 = plotTop, X2 = plotLeft, Y2 = bot, Stroke = ab, StrokeThickness = 1 });
        foreach (var t in xs.Ticks(6)) { double x = xs.Map(t); var l = new TextBlock { Text = Fmt(t), FontSize = 10, Foreground = ab }; Canvas.SetLeft(l, x - 12); Canvas.SetTop(l, bot + 4); c.Children.Add(l); }
        foreach (var t in ys.Ticks(5)) { double y = ys.Map(t); var l = new TextBlock { Text = Fmt(t), FontSize = 10, Foreground = ab, TextAlignment = TextAlignment.Right, Width = plotLeft - 6 }; Canvas.SetLeft(l, 0); Canvas.SetTop(l, y - 7); c.Children.Add(l); }
    }

    private static string Fmt(double v) => Math.Abs(v) >= 1e6 ? $"{v / 1e6:0.#}M" : Math.Abs(v) >= 1e3 ? $"{v / 1e3:0.#}k" : v == Math.Floor(v) ? v.ToString("F0") : v.ToString("G4");

    internal static SolidColorBrush ColorToBrush(string color) { var c = D3Color.Parse(color); return new SolidColorBrush(Windows.UI.Color.FromArgb((byte)(c.Opacity * 255), c.R, c.G, c.B)); }
    internal static Geometry ParsePathData(string pathData) => PathDataParser.Parse(pathData);
}

/// <summary>
/// Handle returned by OnReady — lets callers push new data into the chart without
/// recreating the Canvas or triggering a Duct re-render.
/// </summary>
public sealed class ChartHandle
{
    private readonly Canvas _canvas;
    private readonly Action<object> _redraw;

    internal ChartHandle(Canvas canvas, Action<object> redraw) { _canvas = canvas; _redraw = redraw; }

    public Canvas Canvas => _canvas;

    /// <summary>Re-renders the chart with new data. Call from DispatcherQueue.</summary>
    public void Redraw<T>(IReadOnlyList<T> data) => _redraw(data);
}

// ════════════════════════════════════════════════════════════════════════════
//  PieChartElement
// ════════════════════════════════════════════════════════════════════════════

public sealed class PieChartElement<T>
{
    internal IReadOnlyList<T> Data { get; init; } = [];
    internal Func<T, double> ValueAccessor { get; init; } = _ => 0;
    internal Func<T, string>? LabelAccessor { get; init; }

    private double _width = 300, _height = 300;
    private double _innerRadius = 0, _padAngle = 0.02;
    private D3Color[]? _colorPalette;
    private Action<PieChartHandle>? _onReady;

    public PieChartElement<T> Width(double w) { _width = w; return this; }
    public PieChartElement<T> Height(double h) { _height = h; return this; }
    public PieChartElement<T> InnerRadius(double r) { _innerRadius = r; return this; }
    public PieChartElement<T> PadAngle(double a) { _padAngle = a; return this; }
    public PieChartElement<T> SetColors(params D3Color[] colors) { _colorPalette = colors; return this; }
    public PieChartElement<T> OnReady(Action<PieChartHandle> callback) { _onReady = callback; return this; }

    public Element ToElement() => new XamlHostElement(BuildCanvas, UpdateCanvas) { TypeKey = "DuctD3Pie" };
    public static implicit operator Element(PieChartElement<T> chart) => chart.ToElement();

    private FrameworkElement BuildCanvas()
    {
        var canvas = new Canvas { Width = _width, Height = _height };
        FullRender(canvas, Data);
        _onReady?.Invoke(new PieChartHandle(canvas, data => FullRender(canvas, (IReadOnlyList<T>)data)));
        return canvas;
    }

    private void UpdateCanvas(FrameworkElement fe)
    {
        if (fe is Canvas canvas)
        {
            FullRender(canvas, Data);
            _onReady?.Invoke(new PieChartHandle(canvas, data => FullRender(canvas, (IReadOnlyList<T>)data)));
        }
    }

    private void FullRender(Canvas canvas, IReadOnlyList<T> data)
    {
        canvas.Children.Clear();
        if (data.Count == 0) return;

        var palette = _colorPalette ?? D3Color.Category10;
        double cx = _width / 2, cy = _height / 2;
        double outerRadius = Math.Min(cx, cy) - 10;

        var pieGen = PieGenerator.Create<T>(ValueAccessor).SetPadAngle(_padAngle);
        var arcs = pieGen.Generate(data);
        var arcGen = new ArcGenerator().SetInnerRadius(_innerRadius).SetOuterRadius(outerRadius);

        foreach (var arc in arcs)
        {
            string? pd = arcGen.Generate(arc);
            if (pd == null) continue;
            var color = palette[arc.Index % palette.Length];
            var brush = new SolidColorBrush(Windows.UI.Color.FromArgb((byte)(color.Opacity * 255), color.R, color.G, color.B));
            var path = new WinShapes.Path { Fill = brush, Stroke = new SolidColorBrush(Microsoft.UI.Colors.White), StrokeThickness = 1, RenderTransform = new TranslateTransform { X = cx, Y = cy } };
            path.Data = ChartElement<T>.ParsePathData(pd);
            canvas.Children.Add(path);

            if (LabelAccessor != null)
            {
                var (lx, ly) = arcGen.Centroid(arc.StartAngle, arc.EndAngle);
                var label = new TextBlock { Text = LabelAccessor(arc.Data), FontSize = 11, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White) };
                Canvas.SetLeft(label, cx + lx - 10); Canvas.SetTop(label, cy + ly - 7);
                canvas.Children.Add(label);
            }
        }
    }
}

public sealed class PieChartHandle
{
    private readonly Canvas _canvas;
    private readonly Action<object> _redraw;
    internal PieChartHandle(Canvas canvas, Action<object> redraw) { _canvas = canvas; _redraw = redraw; }
    public Canvas Canvas => _canvas;
    public void Redraw<T>(IReadOnlyList<T> data) => _redraw(data);
}

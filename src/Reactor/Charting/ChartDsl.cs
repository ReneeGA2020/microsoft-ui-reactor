using Microsoft.UI.Reactor.Charting.Accessibility;
using Microsoft.UI.Reactor.Charting.D3;
// Ergonomic Reactor chart DSL — high-level chart components for Reactor's declarative model
// Usage: using static Microsoft.UI.Reactor.Charting.ChartDsl;

using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Reactor.Charting.D3Dsl;

namespace Microsoft.UI.Reactor.Charting;

/// <summary>
/// Static factory methods that integrate D3 charting into Reactor's declarative DSL.
/// Import with: using static Microsoft.UI.Reactor.Charting.ChartDsl;
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

public sealed class ChartElement<T> : IChartAccessibilityData
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
    private Action<ChartHandle<T>>? _onReady;

    // Accessibility fields
    private string? _title;
    private string? _description;
    private string[]? _seriesNames;
    private Func<T, int, string>? _dataLabel;
    private string? _xUnits, _yUnits;
    private string? _xAxisLabel, _yAxisLabel;

    public ChartElement<T> Width(double w) { _width = w; return this; }
    public ChartElement<T> Height(double h) { _height = h; return this; }
    public ChartElement<T> Margin(double top, double right, double bottom, double left) { _marginTop = top; _marginRight = right; _marginBottom = bottom; _marginLeft = left; return this; }
    public ChartElement<T> Stroke(string color) { _stroke = color; return this; }
    public ChartElement<T> Fill(string color) { _fill = color; return this; }
    public ChartElement<T> StrokeWidth(double w) { _strokeWidth = w; return this; }
    public ChartElement<T> FillOpacity(double o) { _fillOpacity = o; return this; }
    public ChartElement<T> ShowAxes(bool show) { _showAxes = show; return this; }
    public ChartElement<T> ShowGrid(bool show) { _showGrid = show; return this; }

    // ── Accessibility modifiers ──────────────────────────────────────

    /// <summary>Sets visible title + accessible name for the chart.</summary>
    public ChartElement<T> Title(string title) { _title = title; return this; }

    /// <summary>Overrides auto-generated accessible description/summary.</summary>
    public ChartElement<T> Description(string description) { _description = description; return this; }

    /// <summary>Sets the series name (for single-series charts).</summary>
    public ChartElement<T> SeriesName(string name) { _seriesNames = [name]; return this; }

    /// <summary>Sets names for multiple series.</summary>
    public ChartElement<T> SeriesNames(params string[] names) { _seriesNames = names; return this; }

    /// <summary>Per-point label override. Receives the data item and its index.</summary>
    public ChartElement<T> DataLabel(Func<T, int, string> labeller) { _dataLabel = labeller; return this; }

    /// <summary>Axis unit annotations (e.g., "months", "USD").</summary>
    public ChartElement<T> Units(string? xUnits = null, string? yUnits = null) { _xUnits = xUnits; _yUnits = yUnits; return this; }

    /// <summary>Explicit axis name.</summary>
    public ChartElement<T> AxisLabel(ChartAxisType axis, string label)
    {
        if (axis == ChartAxisType.X) _xAxisLabel = label;
        else _yAxisLabel = label;
        return this;
    }

    /// <summary>
    /// Called after the chart Canvas is mounted. The handle exposes the Canvas for
    /// escape-hatch scenarios. Prefer state-driven re-renders for data updates.
    /// </summary>
    public ChartElement<T> OnReady(Action<ChartHandle<T>> callback) { _onReady = callback; return this; }

    public Element ToElement() => BuildElement(Data);
    public static implicit operator Element(ChartElement<T> chart) => chart.ToElement();

    private Element BuildElement(IReadOnlyList<T> data)
    {
        if (data.Count == 0)
            return D3Canvas(_width, _height);

        double plotLeft = _marginLeft, plotTop = _marginTop;
        double plotWidth = _width - _marginLeft - _marginRight;
        double plotHeight = _height - _marginTop - _marginBottom;

        var (xMin, xMax) = D3Extent.Extent(data, XAccessor);
        var (yMin, yMax) = D3Extent.Extent(data, YAccessor);
        var xScale = new LinearScale([xMin, xMax], [plotLeft, plotLeft + plotWidth]).Nice();
        var yScale = new LinearScale([yMin, yMax], [plotTop + plotHeight, plotTop]).Nice();

        var canvas = D3Canvas(_width, _height,
            [.. _showGrid ? D3Grid(yScale, plotLeft, plotWidth) : [],
             .. RenderData(data, xScale, yScale, plotLeft, plotTop, plotWidth, plotHeight),
             .. _showAxes ? D3Axes(xScale, yScale, plotLeft, plotTop, plotWidth, plotHeight) : []]);

        if (_onReady is { } cb)
            canvas = canvas.Set(c => cb(new ChartHandle<T>(c)));

        return canvas;
    }

    private Element[] RenderData(IReadOnlyList<T> data, LinearScale xScale, LinearScale yScale,
        double plotLeft, double plotTop, double plotWidth, double plotHeight)
    {
        return ChartType switch
        {
            ChartType.Line => RenderLine(data, xScale, yScale),
            ChartType.Bar => RenderBars(data, xScale, yScale, plotTop, plotWidth, plotHeight),
            ChartType.Area => RenderArea(data, xScale, yScale, plotTop, plotHeight),
            _ => [],
        };
    }

    private Element[] RenderLine(IReadOnlyList<T> data, LinearScale xScale, LinearScale yScale)
    {
        return [D3LinePath(data,
            d => xScale.Map(XAccessor(d)),
            d => yScale.Map(YAccessor(d)),
            stroke: Brush(_stroke), strokeWidth: _strokeWidth)];
    }

    private Element[] RenderArea(IReadOnlyList<T> data, LinearScale xScale, LinearScale yScale,
        double plotTop, double plotHeight)
    {
        double baseline = plotTop + plotHeight;
        return [
            D3AreaPath(data,
                d => xScale.Map(XAccessor(d)),
                _ => baseline,
                d => yScale.Map(YAccessor(d)),
                fill: Brush(_fill, _fillOpacity)),
            D3LinePath(data,
                d => xScale.Map(XAccessor(d)),
                d => yScale.Map(YAccessor(d)),
                stroke: Brush(_stroke), strokeWidth: _strokeWidth),
        ];
    }

    private Element[] RenderBars(IReadOnlyList<T> data, LinearScale xScale, LinearScale yScale,
        double plotTop, double plotWidth, double plotHeight)
    {
        double barW = Math.Max(1, plotWidth / data.Count * 0.8);
        double baseline = plotTop + plotHeight;
        var fillBrush = Brush(_fill);
        return data.Select((d, i) =>
        {
            double cx = xScale.Map(XAccessor(d)), cy = yScale.Map(YAccessor(d));
            return (Element)(D3Rect(cx - barW / 2, cy, barW, Math.Max(0, baseline - cy))
                with { Fill = fillBrush, RadiusX = 2, RadiusY = 2, Key = $"bar-{i}" });
        }).ToArray();
    }

    internal static SolidColorBrush ColorToBrush(string color) { var c = D3Color.Parse(color); return new SolidColorBrush(global::Windows.UI.Color.FromArgb((byte)(c.Opacity * 255), c.R, c.G, c.B)); }
    internal static Geometry ParsePathData(string pathData) => PathDataParser.Parse(pathData);

    // ── IChartAccessibilityData ──────────────────────────────────────

    string? IChartAccessibilityData.Name => _title;
    string? IChartAccessibilityData.Description => _description;

    IReadOnlyList<ChartSeriesDescriptor> IChartAccessibilityData.Series
    {
        get
        {
            if (Data.Count == 0) return [];

            var seriesName = _seriesNames?.Length > 0 ? _seriesNames[0] : "Series 1";

            var points = Data.Select((d, i) =>
            {
                var xVal = XAccessor(d);
                var yVal = YAccessor(d);
                var xLabel = xVal.ToString("G");
                string? label = _dataLabel?.Invoke(d, i);
                return new ChartPointDescriptor(xLabel, yVal, label);
            }).ToArray();

            return [new ChartSeriesDescriptor(seriesName, points)];
        }
    }

    IReadOnlyList<ChartAxisDescriptor> IChartAccessibilityData.Axes
    {
        get
        {
            if (Data.Count == 0) return [];

            var (xMin, xMax) = D3Extent.Extent(Data, XAccessor);
            var (yMin, yMax) = D3Extent.Extent(Data, YAccessor);

            return [
                new ChartAxisDescriptor(ChartAxisType.X, _xAxisLabel, xMin, xMax, _xUnits),
                new ChartAxisDescriptor(ChartAxisType.Y, _yAxisLabel, yMin, yMax, _yUnits),
            ];
        }
    }

    ChartViewport? IChartAccessibilityData.Viewport => null;
}

/// <summary>
/// Handle returned by OnReady — exposes the underlying Canvas for escape-hatch scenarios.
/// </summary>
public sealed class ChartHandle<T>
{
    private readonly Microsoft.UI.Xaml.Controls.Canvas _canvas;

    internal ChartHandle(Microsoft.UI.Xaml.Controls.Canvas canvas) { _canvas = canvas; }

    public Microsoft.UI.Xaml.Controls.Canvas Canvas => _canvas;

    /// <summary>Re-renders the chart with new data. Prefer state-driven re-renders instead.</summary>
    [Obsolete("Use state-driven re-renders (e.g. setData(newData)) instead of ChartHandle.Redraw. " +
              "Charts are now native Reactor elements that diff efficiently.")]
    public void Redraw(IReadOnlyList<T> data) { }
}

// ════════════════════════════════════════════════════════════════════════════
//  PieChartElement
// ════════════════════════════════════════════════════════════════════════════

public sealed class PieChartElement<T> : IChartAccessibilityData
{
    internal IReadOnlyList<T> Data { get; init; } = [];
    internal Func<T, double> ValueAccessor { get; init; } = _ => 0;
    internal Func<T, string>? LabelAccessor { get; init; }

    private double _width = 300, _height = 300;
    private double _innerRadius = 0, _padAngle = 0.02;
    private IReadOnlyList<D3Color>? _colorPalette;
    private Action<PieChartHandle<T>>? _onReady;

    // Accessibility fields
    private string? _title;
    private string? _description;
    private string[]? _seriesNames;
    private Func<T, int, string>? _dataLabel;

    public PieChartElement<T> Width(double w) { _width = w; return this; }
    public PieChartElement<T> Height(double h) { _height = h; return this; }
    public PieChartElement<T> InnerRadius(double r) { _innerRadius = r; return this; }
    public PieChartElement<T> PadAngle(double a) { _padAngle = a; return this; }
    public PieChartElement<T> SetColors(params D3Color[] colors) { _colorPalette = Array.AsReadOnly(colors); return this; }
    public PieChartElement<T> OnReady(Action<PieChartHandle<T>> callback) { _onReady = callback; return this; }

    /// <summary>Sets visible title + accessible name for the chart.</summary>
    public PieChartElement<T> Title(string title) { _title = title; return this; }

    /// <summary>Overrides auto-generated accessible description/summary.</summary>
    public PieChartElement<T> Description(string description) { _description = description; return this; }

    /// <summary>Sets names for pie slices (mapped to series in accessibility).</summary>
    public PieChartElement<T> SeriesNames(params string[] names) { _seriesNames = names; return this; }

    /// <summary>Per-slice label override.</summary>
    public PieChartElement<T> DataLabel(Func<T, int, string> labeller) { _dataLabel = labeller; return this; }

    public Element ToElement() => BuildElement(Data);
    public static implicit operator Element(PieChartElement<T> chart) => chart.ToElement();

    private Element BuildElement(IReadOnlyList<T> data)
    {
        if (data.Count == 0)
            return D3Canvas(_width, _height);

        var palette = _colorPalette ?? D3Color.Category10;
        double cx = _width / 2, cy = _height / 2;
        double outerRadius = Math.Min(cx, cy) - 10;

        var whiteBrush = new SolidColorBrush(Microsoft.UI.Colors.White);

        var canvas = D3Canvas(_width, _height,
            [.. D3Pie(data, ValueAccessor, cx, cy, outerRadius, _innerRadius, _padAngle,
                    stroke: whiteBrush),
             .. LabelAccessor != null ? RenderLabels(data, cx, cy, outerRadius) : []]);

        if (_onReady is { } cb)
            canvas = canvas.Set(c => cb(new PieChartHandle<T>(c)));

        return canvas;
    }

    private Element[] RenderLabels(IReadOnlyList<T> data, double cx, double cy, double outerRadius)
    {
        var pieGen = PieGenerator.Create<T>(ValueAccessor).SetPadAngle(_padAngle);
        var arcs = pieGen.Generate(data);
        var arcGen = new ArcGenerator().SetInnerRadius(_innerRadius).SetOuterRadius(outerRadius);
        var whiteBrush = new SolidColorBrush(Microsoft.UI.Colors.White);

        return arcs.Select(arc =>
        {
            var (lx, ly) = arcGen.Centroid(arc.StartAngle, arc.EndAngle);
            return (Element)D3Dsl.Text(cx + lx - 10, cy + ly - 7, LabelAccessor!(arc.Data), 11, whiteBrush);
        }).ToArray();
    }

    // ── IChartAccessibilityData ──────────────────────────────────────

    string? IChartAccessibilityData.Name => _title;
    string? IChartAccessibilityData.Description => _description;

    IReadOnlyList<ChartSeriesDescriptor> IChartAccessibilityData.Series
    {
        get
        {
            if (Data.Count == 0) return [];

            // Pie charts expose each slice as a point in a single "Slices" series
            var points = Data.Select((d, i) =>
            {
                var value = ValueAccessor(d);
                var label = LabelAccessor?.Invoke(d) ?? $"Slice {i + 1}";
                string? customLabel = _dataLabel?.Invoke(d, i);
                return new ChartPointDescriptor(label, value, customLabel);
            }).ToArray();

            var seriesName = _seriesNames?.Length > 0 ? _seriesNames[0] : "Slices";
            return [new ChartSeriesDescriptor(seriesName, points)];
        }
    }

    IReadOnlyList<ChartAxisDescriptor> IChartAccessibilityData.Axes => [];
    ChartViewport? IChartAccessibilityData.Viewport => null;
}

/// <summary>
/// Handle returned by OnReady — exposes the underlying Canvas for escape-hatch scenarios.
/// </summary>
public sealed class PieChartHandle<T>
{
    private readonly Microsoft.UI.Xaml.Controls.Canvas _canvas;
    internal PieChartHandle(Microsoft.UI.Xaml.Controls.Canvas canvas) { _canvas = canvas; }
    public Microsoft.UI.Xaml.Controls.Canvas Canvas => _canvas;

    /// <summary>Re-renders the chart with new data. Prefer state-driven re-renders instead.</summary>
    [Obsolete("Use state-driven re-renders (e.g. setData(newData)) instead of PieChartHandle.Redraw. " +
              "Charts are now native Reactor elements that diff efficiently.")]
    public void Redraw(IReadOnlyList<T> data) { }
}

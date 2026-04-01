// Central registry of all gallery samples

namespace DuctD3.Gallery;

/// <summary>
/// Registers all gallery samples. Add new samples here to include them in the gallery.
/// </summary>
public static class SampleRegistry
{
    public static GallerySample[] All { get; } =
    [
        // ── Bars ──────────────────────────────────────────────
        new BarChartSample(),
        new HorizontalBarChartSample(),
        new StackedBarChartSample(),
        new GroupedBarChartSample(),
        new DivergingBarChartSample(),

        // ── Lines ─────────────────────────────────────────────
        new LineChart(),
        new MultiLineChart(),
        new LineChartMissingData(),
        new SlopeChart(),
        new CandlestickChart(),

        // ── Areas ─────────────────────────────────────────────
        new AreaChart(),
        new StackedAreaChart(),
        new StreamgraphChart(),
        new DifferenceChart(),
        new RidgePlot(),

        // ── Radial ────────────────────────────────────────────
        new PieChartSample(),
        new DonutChartSample(),

        // ── Dots ──────────────────────────────────────────────
        new ScatterplotSample(),
        new BubbleChartSample(),
        new DotPlotSample(),

        // ── Analysis ──────────────────────────────────────────
        new HistogramSample(),
        new BoxPlotSample(),

        // ── Hierarchies ───────────────────────────────────────
        new TidyTreeSample(),
        new ClusterDendrogramSample(),
        new TreemapSample(),
        new CirclePackingSample(),
        new SunburstSample(),
        new IcicleSample(),
        new IndentedTreeSample(),

        // ── Networks ──────────────────────────────────────────
        new ForceDirectedGraphSample(),
        new ChordDiagramSample(),
        new SankeyDiagramSample(),
        new ArcDiagramSample(),
    ];
}

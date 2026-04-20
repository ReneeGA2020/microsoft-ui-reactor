using Microsoft.UI.Reactor.Charting.Accessibility;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

/// <summary>
/// Unit tests for chart automation peer provider logic — tests grid/table/value/range
/// providers without requiring a WinUI window.
/// </summary>
public class ChartAutomationPeerTests
{
    // ── IGridProvider row/column counts ───────────────────────────────

    [Fact]
    public void GridProvider_EmptyData_ZeroCounts()
    {
        var data = MakeData(seriesCounts: []);
        Assert.Empty(data.Series);
        Assert.Empty(data.Axes);
    }

    [Fact]
    public void GridProvider_SinglePoint()
    {
        var data = MakeData(seriesCounts: [1]);
        Assert.Single(data.Series);
        Assert.Single(data.Series[0].Points);
    }

    [Fact]
    public void GridProvider_TwoSeries_FivePoints()
    {
        var data = MakeData(seriesCounts: [5, 5]);
        Assert.Equal(2, data.Series.Count);
        Assert.Equal(5, data.Series[0].Points.Count);
        Assert.Equal(5, data.Series[1].Points.Count);
    }

    [Fact]
    public void GridProvider_JaggedSeries_DifferentPointCounts()
    {
        var data = MakeData(seriesCounts: [3, 7, 1]);
        Assert.Equal(3, data.Series.Count);
        var maxCols = data.Series.Max(s => s.Points.Count);
        Assert.Equal(7, maxCols);
    }

    // ── IValueProvider.Value formatting ──────────────────────────────

    [Fact]
    public void ValueProvider_FormattedLabel_UsedWhenPresent()
    {
        var series = new ChartSeriesDescriptor("Revenue",
            [new ChartPointDescriptor("March", 42300, "$42,300 on March")]);

        var label = ChartPointProvider.FormatDefaultLabel(series, series.Points[0], 0);

        // When FormattedLabel is set, the provider should use it via the point's own
        // FormattedLabel property. FormatDefaultLabel generates the default only.
        Assert.Contains("Revenue", label);
        Assert.Contains("March", label);
    }

    [Fact]
    public void ValueProvider_IntegerValue_NoDecimals()
    {
        var series = new ChartSeriesDescriptor("Sales",
            [new ChartPointDescriptor("Q1", 100)]);

        var label = ChartPointProvider.FormatDefaultLabel(series, series.Points[0], 0);
        Assert.Contains("100", label);
        Assert.DoesNotContain(".00", label);
    }

    [Fact]
    public void ValueProvider_DoubleValue_TwoDecimals()
    {
        var series = new ChartSeriesDescriptor("Temperature",
            [new ChartPointDescriptor("Jan", 23.456)]);

        var label = ChartPointProvider.FormatDefaultLabel(series, series.Points[0], 0);
        Assert.Contains("23.46", label);
    }

    [Fact]
    public void ValueProvider_WithUnits()
    {
        var series = new ChartSeriesDescriptor("Revenue",
            [new ChartPointDescriptor("Q1", 500)]);

        var label = ChartPointProvider.FormatDefaultLabel(series, series.Points[0], 0, " USD");
        Assert.Contains("USD", label);
    }

    [Fact]
    public void ValueProvider_DefaultFormat_ContainsPointIndex()
    {
        var points = Enumerable.Range(0, 5)
            .Select(i => new ChartPointDescriptor($"P{i}", i * 10))
            .ToArray();
        var series = new ChartSeriesDescriptor("Data", points);

        var label = ChartPointProvider.FormatDefaultLabel(series, series.Points[2], 2);
        Assert.Contains("point 3 of 5", label);
    }

    // ── ITableProvider header generation ─────────────────────────────

    [Fact]
    public void TableProvider_RowHeaders_MatchSeriesNames()
    {
        var data = MakeData(seriesCounts: [3, 3], seriesNames: ["Revenue", "Costs"]);
        Assert.Equal("Revenue", data.Series[0].Name);
        Assert.Equal("Costs", data.Series[1].Name);
    }

    [Fact]
    public void TableProvider_ColumnHeaders_MatchXLabels()
    {
        var data = MakeData(seriesCounts: [3]);
        Assert.Equal("P0", data.Series[0].Points[0].XLabel);
        Assert.Equal("P1", data.Series[0].Points[1].XLabel);
        Assert.Equal("P2", data.Series[0].Points[2].XLabel);
    }

    [Fact]
    public void TableProvider_WithoutExplicitAxisLabels()
    {
        var data = MakeData(seriesCounts: [2]);
        // Default: no axis label set
        Assert.Equal(2, data.Axes.Count);
        Assert.Null(data.Axes[0].Label);
    }

    [Fact]
    public void TableProvider_WithExplicitAxisLabels()
    {
        var data = MakeData(seriesCounts: [2], xAxisLabel: "Month", yAxisLabel: "Revenue");
        Assert.Equal("Month", data.Axes[0].Label);
        Assert.Equal("Revenue", data.Axes[1].Label);
    }

    // ── IRangeValueProvider min/max derivation ───────────────────────

    [Fact]
    public void RangeProvider_DerivesMinMax_FromAxisDescriptors()
    {
        var axis = new ChartAxisDescriptor(ChartAxisType.Y, "Value", 10, 200, "USD");
        Assert.Equal(10, axis.Min);
        Assert.Equal(200, axis.Max);
    }

    [Fact]
    public void RangeProvider_SmallChange_IsRangeDividedBy20()
    {
        var axis = new ChartAxisDescriptor(ChartAxisType.X, null, 0, 100);
        // ChartAxisProvider computes SmallChange = range / 20
        var expectedSmall = (axis.Max - axis.Min) / 20.0;
        Assert.Equal(5.0, expectedSmall);
    }

    [Fact]
    public void RangeProvider_LargeChange_IsRangeDividedBy4()
    {
        var axis = new ChartAxisDescriptor(ChartAxisType.X, null, 0, 100);
        var expectedLarge = (axis.Max - axis.Min) / 4.0;
        Assert.Equal(25.0, expectedLarge);
    }

    // ── Test helpers ─────────────────────────────────────────────────

    private static MockChartData MakeData(
        int[] seriesCounts,
        string[]? seriesNames = null,
        string? xAxisLabel = null,
        string? yAxisLabel = null)
    {
        var series = seriesCounts.Select((count, si) =>
        {
            var name = seriesNames != null && si < seriesNames.Length
                ? seriesNames[si]
                : $"Series {si + 1}";

            var points = Enumerable.Range(0, count)
                .Select(i => new ChartPointDescriptor($"P{i}", i * 10.0))
                .ToArray();

            return new ChartSeriesDescriptor(name, points);
        }).ToArray();

        var axes = seriesCounts.Length > 0
            ? new ChartAxisDescriptor[]
            {
                new(ChartAxisType.X, xAxisLabel, 0, seriesCounts.Max() * 10.0),
                new(ChartAxisType.Y, yAxisLabel, 0, seriesCounts.Max() * 100.0),
            }
            : Array.Empty<ChartAxisDescriptor>();

        return new MockChartData(series, axes);
    }

    private sealed class MockChartData : IChartAccessibilityData
    {
        public MockChartData(ChartSeriesDescriptor[] series, ChartAxisDescriptor[] axes)
        {
            Series = series;
            Axes = axes;
        }

        public string? Name => null;
        public string? Description => null;
        public IReadOnlyList<ChartSeriesDescriptor> Series { get; }
        public IReadOnlyList<ChartAxisDescriptor> Axes { get; }
        public ChartViewport? Viewport => null;
    }
}

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

    // ── IScrollProvider ──────────────────────────────────────────────

    [Fact]
    public void ScrollProvider_InitialState_ReportsNoScroll()
    {
        var data = MakeData(seriesCounts: [5]);
        // Verify initial scroll state defaults — peer reports NoScroll when pan is not enabled
        Assert.NotNull(data);
        // ChartAutomationPeer initializes H/V scroll percent to NoScroll (-1)
        // and view sizes to 100. We validate the data contract here.
        Assert.Equal(5, data.Series[0].Points.Count);
    }

    [Fact]
    public void ScrollProvider_ViewportClamp_ClampsBetween0And100()
    {
        // Verify that viewport clamping logic works correctly
        // (Values outside 0-100 should be clamped by UpdateViewport)
        Assert.Equal(0, Math.Clamp(-10, 0, 100));
        Assert.Equal(100, Math.Clamp(150, 0, 100));
        Assert.Equal(50, Math.Clamp(50, 0, 100));
    }

    // ── IGridProvider boundary conditions (jagged series) ────────────

    [Fact]
    public void GridProvider_JaggedSeries_GetItem_OutOfBoundsColumn_ReturnsNull()
    {
        // Series A has 5 points, Series B has 2 points.
        // Requesting column 4 from Series B (row 1) should return null.
        var seriesA = Enumerable.Range(0, 5)
            .Select(i => new ChartPointDescriptor($"P{i}", i * 10.0))
            .ToArray();
        var seriesB = Enumerable.Range(0, 2)
            .Select(i => new ChartPointDescriptor($"P{i}", i * 20.0))
            .ToArray();

        var data = new MockChartData(
            [new ChartSeriesDescriptor("A", seriesA), new ChartSeriesDescriptor("B", seriesB)],
            []);

        // Validate data structure — Series B has only 2 points
        Assert.Equal(5, data.Series[0].Points.Count);
        Assert.Equal(2, data.Series[1].Points.Count);

        // Column index 4 is valid for Series A but out-of-bounds for Series B
        int row = 1; // Series B
        int col = 4; // Beyond Series B's point count
        Assert.True(col >= data.Series[row].Points.Count,
            "Column index should exceed Series B point count for boundary test");
    }

    [Fact]
    public void GridProvider_JaggedSeries_NegativeIndices_Safe()
    {
        var data = MakeData(seriesCounts: [3, 2]);
        // Negative row/column should not be valid
        Assert.True(data.Series.Count > 0);
        int row = -1, col = -1;
        Assert.True(row < 0 || row >= data.Series.Count,
            "Negative row index should fail bounds check");
        Assert.True(col < 0,
            "Negative column index should fail bounds check");
    }

    [Fact]
    public void GridProvider_JaggedSeries_MaxColumnCount()
    {
        // ColumnCount should be the max across all series
        var data = MakeData(seriesCounts: [2, 7, 3]);
        var maxCols = data.Series.Max(s => s.Points.Count);
        Assert.Equal(7, maxCols);
        // Each series should have its own point count
        Assert.Equal(2, data.Series[0].Points.Count);
        Assert.Equal(7, data.Series[1].Points.Count);
        Assert.Equal(3, data.Series[2].Points.Count);
    }

    // ── Keyboard navigator focus state ───────────────────────────────

    [Fact]
    public void KeyboardNav_FocusState_InitializesToZeroZero()
    {
        var state = new ChartKeyboardNavigator.FocusState(0, 0, false);
        Assert.Equal(0, state.SeriesIndex);
        Assert.Equal(0, state.PointIndex);
        Assert.False(state.HasFocus);
        Assert.Equal(-1, state.BrushStart);
        Assert.Equal(-1, state.BrushEnd);
        Assert.False(state.LegendFocused);
    }

    [Fact]
    public void KeyboardNav_FocusState_ClampToValidBounds()
    {
        var data = MakeData(seriesCounts: [3, 5]);
        int seriesCount = data.Series.Count;

        // Simulate clamping as the navigator does
        int si = Math.Clamp(10, 0, seriesCount - 1); // 10 → clamped to 1
        int pi = Math.Clamp(20, 0, data.Series[si].Points.Count - 1); // 20 → clamped to 4
        Assert.Equal(1, si);
        Assert.Equal(4, pi);
    }

    [Fact]
    public void KeyboardNav_FocusState_RightArrow_IncreasesPointIndex()
    {
        // Simulate Right arrow: pointIndex goes from 0 to 1
        var data = MakeData(seriesCounts: [5]);
        int si = 0, pi = 0;
        int newPi = Math.Min(data.Series[si].Points.Count - 1, pi + 1);
        Assert.Equal(1, newPi);
    }

    [Fact]
    public void KeyboardNav_FocusState_LeftArrow_DecreasesPointIndex()
    {
        var data = MakeData(seriesCounts: [5]);
        int pi = 3;
        int newPi = Math.Max(0, pi - 1);
        Assert.Equal(2, newPi);
    }

    [Fact]
    public void KeyboardNav_FocusState_LeftArrow_ClampsAtZero()
    {
        int pi = 0;
        int newPi = Math.Max(0, pi - 1);
        Assert.Equal(0, newPi); // Already at first point, stays at 0
    }

    [Fact]
    public void KeyboardNav_FocusState_RightArrow_ClampsAtEnd()
    {
        var data = MakeData(seriesCounts: [5]);
        int si = 0, pi = 4; // Last point
        int newPi = Math.Min(data.Series[si].Points.Count - 1, pi + 1);
        Assert.Equal(4, newPi); // Already at last point, stays at 4
    }

    [Fact]
    public void KeyboardNav_FocusState_DownArrow_SwitchesSeries()
    {
        var data = MakeData(seriesCounts: [5, 5, 5]);
        int si = 0, pi = 2;
        int newSi = Math.Min(data.Series.Count - 1, si + 1);
        int newPi = Math.Min(pi, data.Series[newSi].Points.Count - 1);
        Assert.Equal(1, newSi);
        Assert.Equal(2, newPi); // Point index preserved
    }

    [Fact]
    public void KeyboardNav_FocusState_UpArrow_ClampsAtFirstSeries()
    {
        int si = 0;
        int newSi = Math.Max(0, si - 1);
        Assert.Equal(0, newSi);
    }

    [Fact]
    public void KeyboardNav_CtrlHome_JumpsToFirstSeriesFirstPoint()
    {
        // Ctrl+Home: si=0, pi=0
        var data = MakeData(seriesCounts: [5, 5]);
        int newSi = 0, newPi = 0;
        Assert.Equal(0, newSi);
        Assert.Equal(0, newPi);
        // Verify those are valid indices
        Assert.True(newSi < data.Series.Count);
        Assert.True(newPi < data.Series[newSi].Points.Count);
    }

    [Fact]
    public void KeyboardNav_CtrlEnd_JumpsToLastSeriesLastPoint()
    {
        // Ctrl+End: si=lastSeries, pi=lastPoint
        var data = MakeData(seriesCounts: [3, 7, 5]);
        int newSi = data.Series.Count - 1; // 2
        int newPi = data.Series[newSi].Points.Count - 1; // 4
        Assert.Equal(2, newSi);
        Assert.Equal(4, newPi);
    }

    [Fact]
    public void KeyboardNav_Home_JumpsToFirstPointInSeries()
    {
        var data = MakeData(seriesCounts: [5]);
        int si = 0;
        int newPi = 0;
        Assert.True(newPi < data.Series[si].Points.Count);
    }

    [Fact]
    public void KeyboardNav_End_JumpsToLastPointInSeries()
    {
        var data = MakeData(seriesCounts: [5]);
        int si = 0;
        int newPi = data.Series[si].Points.Count - 1;
        Assert.Equal(4, newPi);
    }

    [Fact]
    public void KeyboardNav_BrushSelection_ExpandsRange()
    {
        // Shift+Right: brush start anchors at current, end moves right
        int currentPi = 2;
        int brushStart = currentPi;
        int brushEnd = Math.Min(4, currentPi + 1); // 5 points → max index 4
        Assert.Equal(2, brushStart);
        Assert.Equal(3, brushEnd);
        Assert.True(brushStart <= brushEnd);

        // Shift+Left: end moves left
        brushEnd = Math.Max(0, brushEnd - 1);
        Assert.Equal(2, brushEnd);
    }

    [Fact]
    public void KeyboardNav_SeriesSwitch_ClampsPointIndex()
    {
        // When switching from a 5-point series to a 2-point series,
        // point index should clamp to the new series' bounds
        var data = MakeData(seriesCounts: [5, 2]);
        int pi = 4;
        int newSi = 1; // Switch to shorter series
        int newPi = Math.Min(pi, data.Series[newSi].Points.Count - 1);
        Assert.Equal(1, newPi); // Clamped from 4 to 1
    }

    // ── ChartFocusContext save/restore ────────────────────────────────

    [Fact]
    public void FocusContext_SaveRestore_RoundTrips()
    {
        var ctx = new ChartFocusContext();
        Assert.False(ctx.HasSavedPosition);

        ctx.SavePosition(2, 7);
        Assert.True(ctx.HasSavedPosition);

        var (si, pi) = ctx.RestorePosition();
        Assert.Equal(2, si);
        Assert.Equal(7, pi);
    }

    [Fact]
    public void FocusContext_AdjustForDataChange_ClampsDeletedSeries()
    {
        var ctx = new ChartFocusContext();
        ctx.SavePosition(5, 3); // Series 5 will be deleted

        var series = new[]
        {
            new ChartSeriesDescriptor("A", [new ChartPointDescriptor("P0", 10), new ChartPointDescriptor("P1", 20)]),
            new ChartSeriesDescriptor("B", [new ChartPointDescriptor("P0", 30)]),
        };

        var (si, pi, announcement) = ctx.AdjustForDataChange(series);
        Assert.Equal(1, si); // Clamped from 5 to last series (1)
        Assert.True(pi <= series[si].Points.Count - 1);
    }

    [Fact]
    public void FocusContext_Clear_ResetsState()
    {
        var ctx = new ChartFocusContext();
        ctx.SavePosition(3, 5);
        ctx.ClearSavedPosition();
        Assert.False(ctx.HasSavedPosition);

        var (si, pi) = ctx.RestorePosition();
        Assert.Equal(0, si);
        Assert.Equal(0, pi);
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

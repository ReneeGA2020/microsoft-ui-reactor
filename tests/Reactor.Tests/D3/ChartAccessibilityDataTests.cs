using Microsoft.UI.Reactor.Charting.Accessibility;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

/// <summary>
/// Tests for chart accessibility descriptor records — record equality,
/// with-expressions, and edge cases.
/// </summary>
public class ChartAccessibilityDataTests
{
    // ── ChartPointDescriptor ─────────────────────────────────────────

    [Fact]
    public void PointDescriptor_EqualityByValue()
    {
        var a = new ChartPointDescriptor("March", 42.5, "$42.50 on March");
        var b = new ChartPointDescriptor("March", 42.5, "$42.50 on March");
        Assert.Equal(a, b);
    }

    [Fact]
    public void PointDescriptor_InequalityOnDifferentValues()
    {
        var a = new ChartPointDescriptor("March", 42.5);
        var b = new ChartPointDescriptor("March", 43.0);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void PointDescriptor_WithExpression()
    {
        var original = new ChartPointDescriptor("March", 42.5);
        var modified = original with { YValue = 99.0 };
        Assert.Equal(42.5, original.YValue);
        Assert.Equal(99.0, modified.YValue);
        Assert.Equal("March", modified.XLabel);
    }

    [Fact]
    public void PointDescriptor_FormattedLabelNullByDefault()
    {
        var point = new ChartPointDescriptor("Q1", 100);
        Assert.Null(point.FormattedLabel);
    }

    // ── ChartSeriesDescriptor ────────────────────────────────────────

    [Fact]
    public void SeriesDescriptor_EqualityByValue()
    {
        var points = new[] { new ChartPointDescriptor("A", 1), new ChartPointDescriptor("B", 2) };
        var a = new ChartSeriesDescriptor("Revenue", points);
        var b = new ChartSeriesDescriptor("Revenue", points);
        Assert.Equal(a, b);
    }

    [Fact]
    public void SeriesDescriptor_InequalityOnDifferentName()
    {
        var points = Array.Empty<ChartPointDescriptor>();
        var a = new ChartSeriesDescriptor("Series A", points);
        var b = new ChartSeriesDescriptor("Series B", points);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void SeriesDescriptor_WithExpression()
    {
        var points = new[] { new ChartPointDescriptor("A", 1) };
        var original = new ChartSeriesDescriptor("Original", points);
        var modified = original with { Name = "Modified" };
        Assert.Equal("Original", original.Name);
        Assert.Equal("Modified", modified.Name);
        Assert.Same(points, modified.Points);
    }

    // ── ChartAxisDescriptor ──────────────────────────────────────────

    [Fact]
    public void AxisDescriptor_EqualityByValue()
    {
        var a = new ChartAxisDescriptor(ChartAxisType.X, "Time", 0, 100, "months");
        var b = new ChartAxisDescriptor(ChartAxisType.X, "Time", 0, 100, "months");
        Assert.Equal(a, b);
    }

    [Fact]
    public void AxisDescriptor_InequalityOnDifferentType()
    {
        var a = new ChartAxisDescriptor(ChartAxisType.X, "Time", 0, 100);
        var b = new ChartAxisDescriptor(ChartAxisType.Y, "Time", 0, 100);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void AxisDescriptor_UnitsNullByDefault()
    {
        var axis = new ChartAxisDescriptor(ChartAxisType.Y, "Value", 0, 50);
        Assert.Null(axis.Units);
    }

    [Fact]
    public void AxisDescriptor_WithExpression()
    {
        var original = new ChartAxisDescriptor(ChartAxisType.X, "Time", 0, 100);
        var modified = original with { Max = 200, Units = "ms" };
        Assert.Equal(100, original.Max);
        Assert.Null(original.Units);
        Assert.Equal(200, modified.Max);
        Assert.Equal("ms", modified.Units);
    }

    // ── ChartViewport ────────────────────────────────────────────────

    [Fact]
    public void Viewport_EqualityByValue()
    {
        var a = new ChartViewport(0, 100, 0, 50);
        var b = new ChartViewport(0, 100, 0, 50);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Viewport_WithExpression()
    {
        var original = new ChartViewport(0, 100, 0, 50);
        var zoomed = original with { XMin = 20, XMax = 80 };
        Assert.Equal(0, original.XMin);
        Assert.Equal(20, zoomed.XMin);
        Assert.Equal(80, zoomed.XMax);
        Assert.Equal(0, zoomed.YMin);
    }
}

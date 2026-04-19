// Port of d3-array statistics tests (min, max, sum, mean, median, quantile, variance, deviation, cumsum)
// Validates D3Statistics matches d3-array v3 behavior.

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class StatisticsTests
{
    private const double Tol = 1e-10;

    // ─── Min ─────────────────────────────────────────────────────────

    [Fact]
    public void Min_ReturnsSmallest()
    {
        Assert.Equal(1.0, D3Statistics.Min([3, 1, 2]));
    }

    [Fact]
    public void Min_IgnoresNaN()
    {
        Assert.Equal(1.0, D3Statistics.Min([double.NaN, 1, 2]));
    }

    [Fact]
    public void Min_Empty_ReturnsNaN()
    {
        Assert.True(double.IsNaN(D3Statistics.Min(Array.Empty<double>())));
    }

    [Fact]
    public void Min_AllNaN_ReturnsNaN()
    {
        Assert.True(double.IsNaN(D3Statistics.Min([double.NaN, double.NaN])));
    }

    [Fact]
    public void Min_Negative()
    {
        Assert.Equal(-3.0, D3Statistics.Min([-1, -2, -3]));
    }

    // ─── Max ─────────────────────────────────────────────────────────

    [Fact]
    public void Max_ReturnsLargest()
    {
        Assert.Equal(3.0, D3Statistics.Max([3, 1, 2]));
    }

    [Fact]
    public void Max_IgnoresNaN()
    {
        Assert.Equal(2.0, D3Statistics.Max([double.NaN, 1, 2]));
    }

    [Fact]
    public void Max_Empty_ReturnsNaN()
    {
        Assert.True(double.IsNaN(D3Statistics.Max(Array.Empty<double>())));
    }

    // ─── Sum ─────────────────────────────────────────────────────────

    [Fact]
    public void Sum_ReturnsTotal()
    {
        Assert.Equal(6.0, D3Statistics.Sum([1, 2, 3]));
    }

    [Fact]
    public void Sum_IgnoresNaN()
    {
        Assert.Equal(3.0, D3Statistics.Sum([1, double.NaN, 2]));
    }

    [Fact]
    public void Sum_Empty_ReturnsZero()
    {
        Assert.Equal(0.0, D3Statistics.Sum(Array.Empty<double>()));
    }

    // ─── Mean ────────────────────────────────────────────────────────

    [Fact]
    public void Mean_ReturnsAverage()
    {
        Assert.Equal(2.0, D3Statistics.Mean([1, 2, 3]));
    }

    [Fact]
    public void Mean_IgnoresNaN()
    {
        Assert.Equal(2.0, D3Statistics.Mean([1, double.NaN, 3]));
    }

    [Fact]
    public void Mean_Empty_ReturnsNaN()
    {
        Assert.True(double.IsNaN(D3Statistics.Mean(Array.Empty<double>())));
    }

    // ─── Median ──────────────────────────────────────────────────────

    [Fact]
    public void Median_OddCount()
    {
        Assert.Equal(2.0, D3Statistics.Median([3, 1, 2]));
    }

    [Fact]
    public void Median_EvenCount()
    {
        Assert.Equal(2.5, D3Statistics.Median([1, 2, 3, 4]));
    }

    // ─── Quantile ────────────────────────────────────────────────────

    [Fact]
    public void Quantile_Sorted_0()
    {
        Assert.Equal(0.0, D3Statistics.QuantileSorted([0, 10, 30], 0));
    }

    [Fact]
    public void Quantile_Sorted_1()
    {
        Assert.Equal(30.0, D3Statistics.QuantileSorted([0, 10, 30], 1));
    }

    [Fact]
    public void Quantile_Sorted_Half()
    {
        Assert.Equal(10.0, D3Statistics.QuantileSorted([0, 10, 30], 0.5));
    }

    [Fact]
    public void Quantile_Sorted_Quarter()
    {
        Assert.Equal(5.0, D3Statistics.QuantileSorted([0, 10, 30], 0.25));
    }

    // ─── Variance / Deviation ────────────────────────────────────────

    [Fact]
    public void Variance_ComputableFromValues()
    {
        Assert.Equal(1.0, D3Statistics.Variance([1, 2, 3]), Tol);
    }

    [Fact]
    public void Variance_SingleValue_ReturnsNaN()
    {
        Assert.True(double.IsNaN(D3Statistics.Variance([1])));
    }

    [Fact]
    public void Deviation_ComputableFromValues()
    {
        Assert.Equal(1.0, D3Statistics.Deviation([1, 2, 3]), Tol);
    }

    // ─── CumSum ──────────────────────────────────────────────────────

    [Fact]
    public void CumSum_ReturnsRunningTotal()
    {
        Assert.Equal(new[] { 1.0, 3.0, 6.0 }, D3Statistics.CumSum([1, 2, 3]));
    }
}

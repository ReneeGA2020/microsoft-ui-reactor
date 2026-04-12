// Tests for D3Extent — covers Extent and Extent<T> with accessor,
// NaN handling, empty input, and mixed values.

using Xunit;

namespace Duct.D3.Tests;

public class ExtentExtendedTests
{
    private const double Tol = 1e-10;

    // ─── Extent(IEnumerable<double>) ────────────────────────────────

    [Fact]
    public void Extent_ReturnsMinMax()
    {
        var (min, max) = D3Extent.Extent([3, 1, 4, 1, 5, 9]);
        Assert.Equal(1.0, min);
        Assert.Equal(9.0, max);
    }

    [Fact]
    public void Extent_SingleElement()
    {
        var (min, max) = D3Extent.Extent([42.0]);
        Assert.Equal(42.0, min);
        Assert.Equal(42.0, max);
    }

    [Fact]
    public void Extent_NegativeValues()
    {
        var (min, max) = D3Extent.Extent([-5, -1, -3]);
        Assert.Equal(-5.0, min);
        Assert.Equal(-1.0, max);
    }

    [Fact]
    public void Extent_IgnoresNaN()
    {
        var (min, max) = D3Extent.Extent([double.NaN, 2, double.NaN, 8, 3]);
        Assert.Equal(2.0, min);
        Assert.Equal(8.0, max);
    }

    [Fact]
    public void Extent_AllNaN_ReturnsNaN()
    {
        var (min, max) = D3Extent.Extent([double.NaN, double.NaN]);
        Assert.True(double.IsNaN(min));
        Assert.True(double.IsNaN(max));
    }

    [Fact]
    public void Extent_Empty_ReturnsNaN()
    {
        var (min, max) = D3Extent.Extent(Array.Empty<double>());
        Assert.True(double.IsNaN(min));
        Assert.True(double.IsNaN(max));
    }

    // ─── Extent<T>(IEnumerable<T>, Func<T, double>) ────────────────

    [Fact]
    public void Extent_Generic_WithAccessor()
    {
        var items = new[] { ("a", 3.0), ("b", 1.0), ("c", 5.0) };
        var (min, max) = D3Extent.Extent(items, x => x.Item2);
        Assert.Equal(1.0, min);
        Assert.Equal(5.0, max);
    }

    [Fact]
    public void Extent_Generic_IgnoresNaN()
    {
        var items = new[] { ("a", double.NaN), ("b", 2.0), ("c", 8.0) };
        var (min, max) = D3Extent.Extent(items, x => x.Item2);
        Assert.Equal(2.0, min);
        Assert.Equal(8.0, max);
    }

    [Fact]
    public void Extent_Generic_AllNaN_ReturnsNaN()
    {
        var items = new[] { ("a", double.NaN), ("b", double.NaN) };
        var (min, max) = D3Extent.Extent(items, x => x.Item2);
        Assert.True(double.IsNaN(min));
        Assert.True(double.IsNaN(max));
    }

    [Fact]
    public void Extent_Generic_Empty_ReturnsNaN()
    {
        var items = Array.Empty<(string, double)>();
        var (min, max) = D3Extent.Extent(items, x => x.Item2);
        Assert.True(double.IsNaN(min));
        Assert.True(double.IsNaN(max));
    }
}

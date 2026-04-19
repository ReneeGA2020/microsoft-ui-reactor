// Port of d3-array extent-test.js, bisect-test.js, tickIncrement-test.js

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class ExtentTests
{
    [Fact]
    public void Extent_Single_Value()
    {
        Assert.Equal((1.0, 1.0), D3Extent.Extent([1]));
    }

    [Fact]
    public void Extent_Multiple_Values()
    {
        Assert.Equal((1.0, 5.0), D3Extent.Extent([5, 1, 2, 3, 4]));
        Assert.Equal((3.0, 20.0), D3Extent.Extent([20, 3]));
        Assert.Equal((3.0, 20.0), D3Extent.Extent([3, 20]));
    }

    [Fact]
    public void Extent_Ignores_NaN()
    {
        Assert.Equal((1.0, 5.0), D3Extent.Extent([double.NaN, 1, 2, 3, 4, 5]));
        Assert.Equal((1.0, 5.0), D3Extent.Extent([1, 2, 3, 4, 5, double.NaN]));
        Assert.Equal((-5.0, -1.0), D3Extent.Extent([-1, -3, -5, double.NaN]));
    }

    [Fact]
    public void Extent_Empty_Returns_NaN()
    {
        var (min, max) = D3Extent.Extent(Array.Empty<double>());
        Assert.True(double.IsNaN(min));
        Assert.True(double.IsNaN(max));
    }

    [Fact]
    public void Extent_All_NaN_Returns_NaN()
    {
        var (min, max) = D3Extent.Extent([double.NaN, double.NaN]);
        Assert.True(double.IsNaN(min));
        Assert.True(double.IsNaN(max));
    }

    [Fact]
    public void Extent_Accessor_Overload()
    {
        var items = new[] { new Box(5), new Box(1), new Box(2), new Box(3), new Box(4) };
        Assert.Equal((1.0, 5.0), D3Extent.Extent(items, x => x.V));
    }

    [Fact]
    public void Extent_Accessor_Ignores_NaN()
    {
        var items = new[] { new Box(double.NaN), new Box(1), new Box(5) };
        Assert.Equal((1.0, 5.0), D3Extent.Extent(items, x => x.V));
    }

    private record Box(double V);
}

public class BisectTests
{
    [Fact]
    public void BisectLeft_Exact_Match_Returns_First_Index()
    {
        var numbers = new double[] { 1, 2, 3 };
        Assert.Equal(0, D3Bisect.BisectLeft(numbers, 1));
        Assert.Equal(1, D3Bisect.BisectLeft(numbers, 2));
        Assert.Equal(2, D3Bisect.BisectLeft(numbers, 3));
    }

    [Fact]
    public void BisectLeft_Duplicates_Returns_First()
    {
        var numbers = new double[] { 1, 2, 2, 3 };
        Assert.Equal(0, D3Bisect.BisectLeft(numbers, 1));
        Assert.Equal(1, D3Bisect.BisectLeft(numbers, 2));
        Assert.Equal(3, D3Bisect.BisectLeft(numbers, 3));
    }

    [Fact]
    public void BisectLeft_Empty_Returns_Zero()
    {
        Assert.Equal(0, D3Bisect.BisectLeft(Array.Empty<double>(), 1));
    }

    [Fact]
    public void BisectLeft_Non_Exact_Returns_Insertion_Point()
    {
        var numbers = new double[] { 1, 2, 3 };
        Assert.Equal(0, D3Bisect.BisectLeft(numbers, 0.5));
        Assert.Equal(1, D3Bisect.BisectLeft(numbers, 1.5));
        Assert.Equal(2, D3Bisect.BisectLeft(numbers, 2.5));
        Assert.Equal(3, D3Bisect.BisectLeft(numbers, 3.5));
    }

    [Fact]
    public void BisectLeft_Observes_Lo_Bound()
    {
        var numbers = new double[] { 1, 2, 3, 4, 5 };
        Assert.Equal(2, D3Bisect.BisectLeft(numbers, 0, 2));
        Assert.Equal(2, D3Bisect.BisectLeft(numbers, 2, 2));
        Assert.Equal(3, D3Bisect.BisectLeft(numbers, 4, 2));
        Assert.Equal(4, D3Bisect.BisectLeft(numbers, 5, 2));
        Assert.Equal(5, D3Bisect.BisectLeft(numbers, 6, 2));
    }

    [Fact]
    public void BisectLeft_Observes_Lo_Hi_Bounds()
    {
        var numbers = new double[] { 1, 2, 3, 4, 5 };
        Assert.Equal(2, D3Bisect.BisectLeft(numbers, 2, 2, 3));
        Assert.Equal(3, D3Bisect.BisectLeft(numbers, 4, 2, 3));
        Assert.Equal(3, D3Bisect.BisectLeft(numbers, 6, 2, 3));
    }

    [Fact]
    public void BisectLeft_NaN_Returns_Hi()
    {
        var numbers = new double[] { 1, 2, 3 };
        Assert.Equal(3, D3Bisect.BisectLeft(numbers, double.NaN));
    }

    [Fact]
    public void BisectRight_Exact_Match_Returns_Index_After()
    {
        var numbers = new double[] { 1, 2, 3 };
        Assert.Equal(1, D3Bisect.BisectRight(numbers, 1));
        Assert.Equal(2, D3Bisect.BisectRight(numbers, 2));
        Assert.Equal(3, D3Bisect.BisectRight(numbers, 3));
    }

    [Fact]
    public void BisectRight_Duplicates_Returns_Index_After_Last()
    {
        var numbers = new double[] { 1, 2, 2, 3 };
        Assert.Equal(1, D3Bisect.BisectRight(numbers, 1));
        Assert.Equal(3, D3Bisect.BisectRight(numbers, 2));
        Assert.Equal(4, D3Bisect.BisectRight(numbers, 3));
    }

    [Fact]
    public void BisectRight_Empty_Returns_Zero()
    {
        Assert.Equal(0, D3Bisect.BisectRight(Array.Empty<double>(), 1));
    }

    [Fact]
    public void BisectRight_Non_Exact_Returns_Insertion_Point()
    {
        var numbers = new double[] { 1, 2, 3 };
        Assert.Equal(0, D3Bisect.BisectRight(numbers, 0.5));
        Assert.Equal(1, D3Bisect.BisectRight(numbers, 1.5));
        Assert.Equal(2, D3Bisect.BisectRight(numbers, 2.5));
        Assert.Equal(3, D3Bisect.BisectRight(numbers, 3.5));
    }

    [Fact]
    public void BisectRight_Observes_Lo_Hi_Bounds()
    {
        var numbers = new double[] { 1, 2, 3, 4, 5 };
        Assert.Equal(2, D3Bisect.BisectRight(numbers, 2, 2, 3));
        Assert.Equal(3, D3Bisect.BisectRight(numbers, 3, 2, 3));
        Assert.Equal(3, D3Bisect.BisectRight(numbers, 6, 2, 3));
    }

    [Fact]
    public void BisectCenter_Picks_Closest()
    {
        var numbers = new double[] { 1, 2, 3, 4, 5 };
        // Closer to 2 than 3.
        Assert.Equal(1, D3Bisect.BisectCenter(numbers, 2.1));
        // Closer to 3 than 2.
        Assert.Equal(2, D3Bisect.BisectCenter(numbers, 2.9));
    }

    [Fact]
    public void BisectCenter_Empty_Returns_Zero()
    {
        Assert.Equal(0, D3Bisect.BisectCenter(Array.Empty<double>(), 1));
    }
}

public class TickIncrementTests
{
    [Fact]
    public void TickIncrement_Zero_To_One()
    {
        // count=10 → step ≈ 0.1 → factor=1, power=-1 → inc = -10
        Assert.Equal(-10, D3Ticks.TickIncrement(0, 1, 10));
        Assert.Equal(-10, D3Ticks.TickIncrement(0, 1, 9));
        Assert.Equal(-10, D3Ticks.TickIncrement(0, 1, 8));
        Assert.Equal(-5, D3Ticks.TickIncrement(0, 1, 7));
        Assert.Equal(-5, D3Ticks.TickIncrement(0, 1, 5));
        Assert.Equal(-2, D3Ticks.TickIncrement(0, 1, 3));
        Assert.Equal(-2, D3Ticks.TickIncrement(0, 1, 2));
        Assert.Equal(1, D3Ticks.TickIncrement(0, 1, 1));
    }

    [Fact]
    public void TickIncrement_Zero_To_Ten()
    {
        Assert.Equal(1, D3Ticks.TickIncrement(0, 10, 10));
        Assert.Equal(1, D3Ticks.TickIncrement(0, 10, 8));
        Assert.Equal(2, D3Ticks.TickIncrement(0, 10, 7));
        Assert.Equal(2, D3Ticks.TickIncrement(0, 10, 4));
        Assert.Equal(5, D3Ticks.TickIncrement(0, 10, 3));
        Assert.Equal(5, D3Ticks.TickIncrement(0, 10, 2));
        Assert.Equal(10, D3Ticks.TickIncrement(0, 10, 1));
    }

    [Fact]
    public void TickIncrement_Negative_To_Positive()
    {
        Assert.Equal(2, D3Ticks.TickIncrement(-10, 10, 10));
        Assert.Equal(2, D3Ticks.TickIncrement(-10, 10, 8));
        Assert.Equal(2, D3Ticks.TickIncrement(-10, 10, 7));
        Assert.Equal(5, D3Ticks.TickIncrement(-10, 10, 6));
        Assert.Equal(5, D3Ticks.TickIncrement(-10, 10, 4));
        Assert.Equal(5, D3Ticks.TickIncrement(-10, 10, 3));
        Assert.Equal(10, D3Ticks.TickIncrement(-10, 10, 2));
        Assert.Equal(20, D3Ticks.TickIncrement(-10, 10, 1));
    }
}

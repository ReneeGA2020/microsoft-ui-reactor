// Port of d3-array/range tests

using Xunit;

namespace Duct.D3.Tests;

public class RangeTests
{
    [Fact]
    public void Range_Stop_Only()
    {
        Assert.Equal(new[] { 0.0, 1.0, 2.0, 3.0, 4.0 }, D3Range.Range(5));
    }

    [Fact]
    public void Range_StartStop()
    {
        Assert.Equal(new[] { 2.0, 3.0, 4.0 }, D3Range.Range(2, 5));
    }

    [Fact]
    public void Range_StartStopStep()
    {
        Assert.Equal(new[] { 0.0, 0.5, 1.0, 1.5, 2.0 }, D3Range.Range(0, 2.5, 0.5));
    }

    [Fact]
    public void Range_NegativeStep()
    {
        Assert.Equal(new[] { 5.0, 4.0, 3.0 }, D3Range.Range(5, 2, -1));
    }

    [Fact]
    public void Range_Empty()
    {
        Assert.Empty(D3Range.Range(0));
        Assert.Empty(D3Range.Range(5, 5));
    }
}

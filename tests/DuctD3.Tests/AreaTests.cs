// Port of d3-shape/test/area-test.js

using Xunit;

namespace Duct.D3.Tests;

public class AreaTests
{
    [Fact]
    public void Area_Default_GeneratesPath()
    {
        var a = AreaGenerator.FromArrays();
        var data = new[] { new[] { 0.0, 1.0 }, new[] { 2.0, 3.0 }, new[] { 4.0, 5.0 } };
        // Top line: M0,1 L2,3 L4,5  then bottom line reversed: L4,0 L2,0 L0,0 Z
        Assert.Equal("M0,1L2,3L4,5L4,0L2,0L0,0Z", a.Generate(data));
    }

    [Fact]
    public void Area_CustomAccessors()
    {
        var data = new[]
        {
            new { X = 0.0, Y = 5.0 },
            new { X = 10.0, Y = 15.0 },
            new { X = 20.0, Y = 10.0 },
        };
        var a = AreaGenerator.Create<dynamic>(d => (double)d.X, d => (double)d.Y);
        // Top: M0,5 L10,15 L20,10  Bottom: L20,0 L10,0 L0,0 Z
        Assert.Equal("M0,5L10,15L20,10L20,0L10,0L0,0Z", a.Generate(data));
    }

    [Fact]
    public void Area_EmptyData_ReturnsNull()
    {
        var a = AreaGenerator.FromArrays();
        Assert.Null(a.Generate(Array.Empty<double[]>()));
    }
}

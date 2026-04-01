// Tests for radial shape generators

using Xunit;

namespace Duct.D3.Tests;

public class RadialLineTests
{
    [Fact]
    public void RadialLine_GeneratesPath()
    {
        var gen = RadialLineGenerator.Create();
        var data = new (double angle, double radius)[]
        {
            (0, 100), (Math.PI / 2, 100), (Math.PI, 100), (3 * Math.PI / 2, 100)
        };

        var path = gen.Generate(data);
        Assert.NotNull(path);
        Assert.StartsWith("M", path);
    }

    [Fact]
    public void RadialLine_CircleAt360()
    {
        var gen = RadialLineGenerator.Create();
        int n = 36;
        var data = Enumerable.Range(0, n)
            .Select(i => (angle: 2 * Math.PI * i / n, radius: 50.0))
            .ToArray();

        var path = gen.Generate(data);
        Assert.NotNull(path);
        Assert.Contains("L", path); // Multiple line segments
    }

    [Fact]
    public void RadialLine_WithCurve()
    {
        var gen = RadialLineGenerator.Create().SetCurve(D3Curve.Cardinal);
        var data = new (double angle, double radius)[]
        {
            (0, 100), (1, 80), (2, 120), (3, 90)
        };

        var path = gen.Generate(data);
        Assert.NotNull(path);
        Assert.Contains("C", path); // Cubic bezier from cardinal
    }

    [Fact]
    public void RadialLine_CustomAccessors()
    {
        var gen = RadialLineGenerator.Create<(double a, double r)>(
            d => d.a, d => d.r);
        var data = new (double a, double r)[] { (0, 50), (1, 60), (2, 70) };

        var path = gen.Generate(data);
        Assert.NotNull(path);
    }
}

public class RadialAreaTests
{
    [Fact]
    public void RadialArea_GeneratesClosedPath()
    {
        var gen = RadialAreaGenerator.Create<(double angle, double value)>(
            d => d.angle, _ => 50, d => d.value);

        var data = new (double angle, double value)[]
        {
            (0, 100), (Math.PI / 2, 80), (Math.PI, 120), (3 * Math.PI / 2, 90)
        };

        var path = gen.Generate(data);
        Assert.NotNull(path);
        Assert.Contains("Z", path); // Closed path
    }
}

public class RadialLinkTests
{
    [Fact]
    public void RadialLink_GeneratesQuadratic()
    {
        var gen = RadialLinkGenerator.Create<(double angle, double radius)>(
            d => d.angle, d => d.radius);

        var path = gen.Generate(
            (source: (0.0, 50.0), target: (Math.PI, 100.0)));

        Assert.NotNull(path);
        Assert.Contains("Q", path); // Quadratic bezier
    }
}

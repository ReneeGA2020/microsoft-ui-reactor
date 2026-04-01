// Tests for symbol and link generators

using Xunit;

namespace Duct.D3.Tests;

public class SymbolTests
{
    [Fact]
    public void Circle_GeneratesArcPath()
    {
        var gen = SymbolGenerator.Create();
        var path = gen.Generate(null);
        Assert.NotNull(path);
        Assert.Contains("A", path); // Arc command
    }

    [Fact]
    public void AllSymbols_GeneratePaths()
    {
        foreach (var type in D3Symbol.All)
        {
            var gen = SymbolGenerator.Create<object?>(type, 64);
            var path = gen.Generate(null);
            Assert.NotNull(path);
            Assert.True(path.Length > 0);
        }
    }

    [Fact]
    public void Square_GeneratesRect()
    {
        var gen = SymbolGenerator.Create<object?>(D3Symbol.Square, 100);
        var path = gen.Generate(null);
        Assert.NotNull(path);
        Assert.Contains("M", path);
    }

    [Fact]
    public void CustomSize()
    {
        var gen = SymbolGenerator.Create<object?>(D3Symbol.Circle, 256);
        var path = gen.Generate(null);
        Assert.NotNull(path);
    }
}

public class LinkTests
{
    [Fact]
    public void Vertical_GeneratesCubicBezier()
    {
        var link = LinkGenerator.Vertical<(double x, double y)>(
            n => n.x, n => n.y);
        var path = link.Generate((source: (0.0, 0.0), target: (10.0, 10.0)));
        Assert.NotNull(path);
        Assert.Contains("C", path); // Cubic bezier
    }

    [Fact]
    public void Horizontal_GeneratesCubicBezier()
    {
        var link = LinkGenerator.Horizontal<(double x, double y)>(
            n => n.x, n => n.y);
        var path = link.Generate((source: (0.0, 0.0), target: (10.0, 10.0)));
        Assert.NotNull(path);
        Assert.Contains("C", path);
    }
}

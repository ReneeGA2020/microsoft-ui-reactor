using Xunit;

namespace Duct.D3.Tests;

public class ArcTests
{
    private const double Tolerance = 1e-6;

    // ── Defaults ───────────────────────────────────────────────────────

    [Fact]
    public void Generate_DefaultRadii_ProducesPath()
    {
        var arc = new ArcGenerator();
        var path = arc.Generate(0, Math.PI / 2);
        Assert.NotNull(path);
        Assert.Contains("M", path);
    }

    // ── Degenerate arc (radius ~ 0) ───────────────────────────────────

    [Fact]
    public void Generate_DegenerateRadius_ReturnsMoveTo()
    {
        var arc = new ArcGenerator()
            .SetInnerRadius(0)
            .SetOuterRadius(0);
        var path = arc.Generate(0, Math.PI / 2);
        Assert.NotNull(path);
        Assert.StartsWith("M0", path);
    }

    // ── Full circle ────────────────────────────────────────────────────

    [Fact]
    public void Generate_FullCircle_ProducesFullArc()
    {
        var arc = new ArcGenerator()
            .SetInnerRadius(0)
            .SetOuterRadius(100);
        var path = arc.Generate(0, 2 * Math.PI);
        Assert.NotNull(path);
        Assert.Contains("A", path); // arc command
    }

    [Fact]
    public void Generate_FullCircleWithInner_ProducesTwoArcs()
    {
        var arc = new ArcGenerator()
            .SetInnerRadius(50)
            .SetOuterRadius(100);
        var path = arc.Generate(0, 2 * Math.PI);
        Assert.NotNull(path);
        // Two arc commands for outer and inner ring
        int arcCount = 0;
        foreach (char c in path)
            if (c == 'A') arcCount++;
        Assert.True(arcCount >= 2);
    }

    // ── Normal arc without inner radius → pie slice (LineTo 0,0) ──────

    [Fact]
    public void Generate_NormalArcNoInner_ContainsLineTo()
    {
        var arc = new ArcGenerator()
            .SetInnerRadius(0)
            .SetOuterRadius(100);
        var path = arc.Generate(0, Math.PI / 2);
        Assert.NotNull(path);
        Assert.Contains("L", path); // LineTo(0,0)
    }

    // ── Normal arc with inner radius → wedge with inner arc ───────────

    [Fact]
    public void Generate_NormalArcWithInner_ContainsInnerArc()
    {
        var arc = new ArcGenerator()
            .SetInnerRadius(50)
            .SetOuterRadius(100);
        var path = arc.Generate(0, Math.PI / 2);
        Assert.NotNull(path);
        // Should have at least two arc commands (outer + inner)
        int arcCount = 0;
        foreach (char c in path)
            if (c == 'A') arcCount++;
        Assert.True(arcCount >= 2);
    }

    // ── Pad angle ──────────────────────────────────────────────────────

    [Fact]
    public void Generate_PadAngle_ShrinksArc()
    {
        var arc = new ArcGenerator()
            .SetInnerRadius(0)
            .SetOuterRadius(100);
        var noPad = arc.Generate(0, Math.PI / 2, padAngle: 0);
        var withPad = arc.Generate(0, Math.PI / 2, padAngle: 0.1);
        Assert.NotNull(noPad);
        Assert.NotNull(withPad);
        // Padded arc should be different (shorter angular span)
        Assert.NotEqual(noPad, withPad);
    }

    // ── Centroid (instance) ────────────────────────────────────────────

    [Fact]
    public void Centroid_Instance_ReturnsMidpoint()
    {
        var arc = new ArcGenerator()
            .SetInnerRadius(0)
            .SetOuterRadius(100);
        var (x, y) = arc.Centroid(0, Math.PI);
        // Midpoint angle = PI/2 - PI/2 = 0 → cos(0)*50 = 50, sin(0)*50 = 0
        Assert.Equal(50.0, x, Tolerance);
        Assert.Equal(0.0, y, Tolerance);
    }

    // ── Centroid (static) ──────────────────────────────────────────────

    [Fact]
    public void Centroid_Static_ReturnsMidpoint()
    {
        var (x, y) = ArcGenerator.Centroid(0, Math.PI, innerRadius: 0, outerRadius: 100);
        Assert.Equal(50.0, x, Tolerance);
        Assert.Equal(0.0, y, Tolerance);
    }

    // ── Fluent setters ─────────────────────────────────────────────────

    [Fact]
    public void FluentSetters_ReturnSameInstance()
    {
        var arc = new ArcGenerator();
        var result = arc.SetInnerRadius(10).SetOuterRadius(200).SetCornerRadius(0).SetDigits(5);
        Assert.Same(arc, result);
    }

    // ── Explicit radius overrides ────────────────────────────────────────

    [Fact]
    public void Generate_ExplicitRadii_OverridesDefaults()
    {
        var arc = new ArcGenerator()
            .SetInnerRadius(0)
            .SetOuterRadius(50);
        // Pass explicit radii that differ from defaults
        var pathDefault = arc.Generate(0, Math.PI / 2);
        var pathOverride = arc.Generate(0, Math.PI / 2, innerRadius: 20, outerRadius: 200);
        Assert.NotNull(pathDefault);
        Assert.NotNull(pathOverride);
        Assert.NotEqual(pathDefault, pathOverride);
    }

    // ── PieArc<T> overload ─────────────────────────────────────────────

    [Fact]
    public void Generate_PieArc_DelegatesToAngleOverload()
    {
        var arc = new ArcGenerator()
            .SetInnerRadius(0)
            .SetOuterRadius(100);
        var pieArc = new PieArc<string>("slice", 1.0, 0, 0, Math.PI / 2, 0);
        var fromPie = arc.Generate(pieArc);
        var fromAngles = arc.Generate(0, Math.PI / 2);
        Assert.Equal(fromAngles, fromPie);
    }
}

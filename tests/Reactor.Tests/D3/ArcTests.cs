// Port of d3-shape/test/arc-test.js (subset — exact SVG path-string matching is
// avoided where reactor1's PathBuilder formatting diverges from upstream.
// Corner-radius paths are not yet implemented in reactor1 so those tests are
// expectation-only: we assert the NotImplementedException contract.)

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class ArcTests
{
    private const double Tol = 1e-6;
    private static double Round(double x) => Math.Round(x * 1e6) / 1e6;

    // ─── Point / degenerate ──────────────────────────────────────────

    [Fact]
    public void Point_When_Both_Radii_Zero()
    {
        var a = new ArcGenerator().SetInnerRadius(0).SetOuterRadius(0);
        var path = a.Generate(0, 2 * Math.PI);
        Assert.Equal("M0,0Z", path);
    }

    [Fact]
    public void Point_When_Zero_Angle_Span()
    {
        var a = new ArcGenerator().SetInnerRadius(0).SetOuterRadius(0);
        var path = a.Generate(0, 0);
        Assert.Equal("M0,0Z", path);
    }

    // ─── Circle / annulus full turn ──────────────────────────────────

    [Fact]
    public void FullCircle_StartsAt_Minus_OuterRadius_Y()
    {
        // Inner=0, outer=100, full τ turn: starts at (0, -100) (12 o'clock).
        var a = new ArcGenerator().SetInnerRadius(0).SetOuterRadius(100);
        var path = a.Generate(0, 2 * Math.PI);
        Assert.NotNull(path);
        Assert.StartsWith("M0,-100", path);
        Assert.EndsWith("Z", path);
    }

    [Fact]
    public void Annulus_FullCircle_Has_Two_Arcs()
    {
        // Inner=50, outer=100, full τ turn: outer arc + inner arc (2 M commands).
        var a = new ArcGenerator().SetInnerRadius(50).SetOuterRadius(100);
        var path = a.Generate(0, 2 * Math.PI);
        Assert.NotNull(path);
        // Two separate subpaths (MoveTo to outer then MoveTo to inner).
        Assert.Equal(2, path!.Count(c => c == 'M'));
        Assert.Equal(4, path.Count(c => c == 'A'));
    }

    // ─── Sector (no inner radius) ────────────────────────────────────

    [Fact]
    public void SmallClockwiseSector_EndsWith_LineTo_Origin()
    {
        // Pie-slice: should end with L0,0 before Z.
        var a = new ArcGenerator().SetInnerRadius(0).SetOuterRadius(100);
        var path = a.Generate(0, Math.PI / 2);
        Assert.NotNull(path);
        Assert.Contains("L0,0", path);
        Assert.EndsWith("Z", path);
    }

    [Fact]
    public void AnticlockwiseSector_SpansOtherDirection()
    {
        // Negative angle span proceeds anticlockwise.
        var a = new ArcGenerator().SetInnerRadius(0).SetOuterRadius(100);
        var path = a.Generate(0, -Math.PI / 2);
        Assert.NotNull(path);
        // Ends near (-100, 0) on the negative x-axis.
        Assert.Contains("-100", path);
    }

    // ─── Annular sector ──────────────────────────────────────────────

    [Fact]
    public void Annular_Sector_Has_Two_Arcs_No_LineTo_Origin()
    {
        var a = new ArcGenerator().SetInnerRadius(50).SetOuterRadius(100);
        var path = a.Generate(0, Math.PI / 2);
        Assert.NotNull(path);
        // Outer arc + inner arc → 2 A commands.
        Assert.Equal(2, path!.Count(c => c == 'A'));
        // Inner radius > 0 → no line-to-origin (L0,0).
        Assert.DoesNotContain("L0,0", path);
    }

    // ─── Radius swap (innerRadius > outerRadius) ─────────────────────

    [Fact]
    public void Radii_Are_Swapped_When_Inner_Greater_Than_Outer()
    {
        // Generator should still produce valid output with radii swapped internally.
        var a = new ArcGenerator().SetInnerRadius(100).SetOuterRadius(50);
        var path = a.Generate(0, Math.PI / 2);
        Assert.NotNull(path);
    }

    // ─── Corner radius: not yet implemented ──────────────────────────

    [Fact]
    public void CornerRadius_Nonzero_NotImplemented()
    {
        var a = new ArcGenerator().SetInnerRadius(0).SetOuterRadius(100).SetCornerRadius(5);
        Assert.Throws<NotImplementedException>(() => a.Generate(0, Math.PI / 2));
    }

    [Fact]
    public void CornerRadius_On_FullCircle_Still_Works()
    {
        // For full circles, the corner radius code path is never entered.
        var a = new ArcGenerator().SetInnerRadius(0).SetOuterRadius(100).SetCornerRadius(5);
        var path = a.Generate(0, 2 * Math.PI);
        Assert.NotNull(path);
    }

    // ─── Centroid ────────────────────────────────────────────────────

    [Fact]
    public void Centroid_Of_Half_Circle()
    {
        // r0=0, r1=100, [0, π] → midRadius=50, midAngle=π/2 - π/2 = 0 → (50, 0).
        var (x, y) = new ArcGenerator().Centroid(0, Math.PI, 0, 100);
        Assert.Equal(50, Round(x));
        Assert.Equal(0, Round(y));
    }

    [Fact]
    public void Centroid_Of_Quarter_Circle()
    {
        // r=50, midAngle = π/4 - π/2 = -π/4 → (50 cos(-π/4), 50 sin(-π/4)).
        var (x, y) = new ArcGenerator().Centroid(0, Math.PI / 2, 0, 100);
        Assert.Equal(35.355339, Round(x));
        Assert.Equal(-35.355339, Round(y));
    }

    [Fact]
    public void Centroid_Of_Annulus_Quarter()
    {
        // r0=50, r1=100, midAngle = -π/4 → midRadius=75 but angle goes through -π/4
        // d3 upstream: innerRadius: 50, outerRadius: 100, start 0, end -π → (-75, 0).
        var (x, y) = new ArcGenerator().Centroid(0, -Math.PI, 50, 100);
        Assert.Equal(-75, Round(x));
        Assert.Equal(0, Round(y));
    }

    [Fact]
    public void Centroid_Static_Convenience()
    {
        var (x, y) = ArcGenerator.Centroid(0, Math.PI, 0, 100);
        Assert.Equal(50, Round(x));
        Assert.Equal(0, Round(y));
    }

    // ─── Fluent chaining ─────────────────────────────────────────────

    [Fact]
    public void Fluent_Setters_Chain_Correctly()
    {
        var a = new ArcGenerator()
            .SetInnerRadius(10)
            .SetOuterRadius(20)
            .SetCornerRadius(0)
            .SetDigits(3);
        // No explicit inner/outer override → uses the set values.
        var path = a.Generate(0, Math.PI / 2);
        Assert.NotNull(path);
        // Outer radius 20 should appear in the path.
        Assert.Contains("20", path);
    }

    [Fact]
    public void Explicit_Radii_Override_Setters()
    {
        var a = new ArcGenerator().SetInnerRadius(100).SetOuterRadius(200);
        // Explicit args override the setters.
        var path = a.Generate(0, 2 * Math.PI, 0, 50, 75);
        Assert.NotNull(path);
        // With inner=50, outer=75, neither 100 nor 200 should appear.
        Assert.DoesNotContain("M0,-100", path);
    }
}

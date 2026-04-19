// Tests for d3-shape curve implementations

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class CurveTests
{
    private static string? GenerateWithCurve(CurveFactory curve, (double x, double y)[] data)
    {
        var line = LineGenerator.Create().SetCurve(curve);
        return line.Generate(data);
    }

    [Fact]
    public void CurveLinear_StraightLines()
    {
        var path = GenerateWithCurve(D3Curve.Linear,
            [(0, 0), (10, 10), (20, 0)]);
        Assert.NotNull(path);
        Assert.StartsWith("M", path);
        Assert.Contains("L", path);
    }

    [Fact]
    public void CurveStep_StepFunction()
    {
        var path = GenerateWithCurve(D3Curve.Step,
            [(0, 0), (10, 10), (20, 0)]);
        Assert.NotNull(path);
        Assert.Contains("L", path);
    }

    [Fact]
    public void CurveStepBefore_StepFunction()
    {
        var path = GenerateWithCurve(D3Curve.StepBefore,
            [(0, 0), (10, 10), (20, 0)]);
        Assert.NotNull(path);
    }

    [Fact]
    public void CurveStepAfter_StepFunction()
    {
        var path = GenerateWithCurve(D3Curve.StepAfter,
            [(0, 0), (10, 10), (20, 0)]);
        Assert.NotNull(path);
    }

    [Fact]
    public void CurveBasis_Smooth()
    {
        var path = GenerateWithCurve(D3Curve.Basis,
            [(0, 0), (10, 10), (20, 0), (30, 10)]);
        Assert.NotNull(path);
        Assert.Contains("C", path);
    }

    [Fact]
    public void CurveNatural_NaturalSpline()
    {
        var path = GenerateWithCurve(D3Curve.Natural,
            [(0, 0), (10, 10), (20, 0), (30, 10)]);
        Assert.NotNull(path);
        Assert.Contains("C", path);
    }

    [Fact]
    public void CurveCardinal_CardinalSpline()
    {
        var path = GenerateWithCurve(D3Curve.Cardinal,
            [(0, 0), (10, 10), (20, 0), (30, 10)]);
        Assert.NotNull(path);
        Assert.Contains("C", path);
    }

    [Fact]
    public void CurveCatmullRom_CatmullRomSpline()
    {
        var path = GenerateWithCurve(D3Curve.CatmullRom,
            [(0, 0), (10, 10), (20, 0), (30, 10)]);
        Assert.NotNull(path);
        Assert.Contains("C", path);
    }

    [Fact]
    public void CurveMonotoneX_MonotoneSpline()
    {
        var path = GenerateWithCurve(D3Curve.MonotoneX,
            [(0, 0), (10, 10), (20, 0), (30, 10)]);
        Assert.NotNull(path);
        Assert.Contains("C", path);
    }

    [Fact]
    public void CurveLinear_SinglePoint_ReturnsMove()
    {
        var path = GenerateWithCurve(D3Curve.Linear, [(5, 5)]);
        Assert.NotNull(path);
        Assert.StartsWith("M", path);
    }

    [Fact]
    public void CurveLinear_TwoPoints()
    {
        var path = GenerateWithCurve(D3Curve.Linear, [(0, 0), (10, 10)]);
        Assert.Equal("M0,0L10,10", path);
    }

    [Fact]
    public void LineWithoutCurve_MatchesLinearCurve()
    {
        var data = new (double x, double y)[] { (0, 0), (10, 10), (20, 0) };
        var noCurve = LineGenerator.Create().Generate(data);
        var linearCurve = LineGenerator.Create().SetCurve(D3Curve.Linear).Generate(data);
        Assert.Equal(noCurve, linearCurve);
    }

    // ── d3 curve factory call sites ───────────────────────────────────
    // Each delegate body is a one-line `path => new CurveX(path)` that
    // is otherwise only invoked when LineGenerator binds the factory.

    [Fact]
    public void StepBefore_StepAfter_Variants_Differ_From_Default()
    {
        // Drive each Step variant so the offset constant in the closure runs.
        var pts = new (double, double)[] { (0, 0), (10, 5) };
        var defaultStep = LineGenerator.Create().SetCurve(D3Curve.Step).Generate(pts);
        var before = LineGenerator.Create().SetCurve(D3Curve.StepBefore).Generate(pts);
        var after = LineGenerator.Create().SetCurve(D3Curve.StepAfter).Generate(pts);
        Assert.NotEqual(defaultStep, before);
        Assert.NotEqual(defaultStep, after);
    }

    [Fact]
    public void CardinalWithTension_Builds_Valid_Curve()
    {
        var pts = new (double, double)[] { (0, 0), (5, 5), (10, 0), (15, 5) };
        var path = LineGenerator.Create().SetCurve(D3Curve.CardinalWithTension(0.5)).Generate(pts);
        Assert.NotNull(path);
        Assert.Contains("C", path);
    }

    [Fact]
    public void CatmullRomWithAlpha_Builds_Valid_Curve()
    {
        var pts = new (double, double)[] { (0, 0), (5, 5), (10, 0), (15, 5) };
        var path = LineGenerator.Create().SetCurve(D3Curve.CatmullRomWithAlpha(0.25)).Generate(pts);
        Assert.NotNull(path);
        Assert.Contains("C", path);
    }

    // ── Edge cases that exercise specific switch arms ─────────────────

    [Fact]
    public void CurveBasis_TwoPoints_LineEndCase2()
    {
        var path = GenerateWithCurve(D3Curve.Basis, [(0, 0), (10, 10)]);
        Assert.NotNull(path);
    }

    [Fact]
    public void CurveBasis_ThreePoints_LineEndCase3()
    {
        var path = GenerateWithCurve(D3Curve.Basis, [(0, 0), (10, 10), (20, 0)]);
        Assert.NotNull(path);
    }

    [Fact]
    public void CurveBasisClosed_OnePoint_ClosedDot()
    {
        var path = GenerateWithCurve(D3Curve.BasisClosed, [(5, 5)]);
        Assert.NotNull(path);
        Assert.Contains("M", path);
        Assert.Contains("Z", path);
    }

    [Fact]
    public void CurveBasisClosed_TwoPoints_ClosedLine()
    {
        var path = GenerateWithCurve(D3Curve.BasisClosed, [(0, 0), (10, 10)]);
        Assert.NotNull(path);
        Assert.Contains("Z", path);
    }

    [Fact]
    public void CurveBasisClosed_ThreePoints_HitsCase3InLineEnd()
    {
        var path = GenerateWithCurve(D3Curve.BasisClosed, [(0, 0), (5, 5), (10, 0)]);
        Assert.NotNull(path);
    }

    [Fact]
    public void CurveBasisClosed_FourPoints_FullCycle()
    {
        var path = GenerateWithCurve(D3Curve.BasisClosed,
            [(0, 0), (5, 5), (10, 0), (15, 5)]);
        Assert.NotNull(path);
        Assert.Contains("C", path);
    }

    [Fact]
    public void CurveNatural_OnePoint()
    {
        var path = GenerateWithCurve(D3Curve.Natural, [(5, 5)]);
        Assert.NotNull(path);
    }

    [Fact]
    public void CurveCardinal_TwoPoints_LineEndCase2()
    {
        var path = GenerateWithCurve(D3Curve.Cardinal, [(0, 0), (10, 10)]);
        Assert.NotNull(path);
    }

    [Fact]
    public void CurveCatmullRom_CoincidentPoints_HandlesZeroLength()
    {
        // Two coincident points → l_a/l_2a stays 0 → falls into the LineTo branch.
        var path = GenerateWithCurve(D3Curve.CatmullRom,
            [(0, 0), (0, 0), (5, 5), (10, 0)]);
        Assert.NotNull(path);
    }

    [Fact]
    public void CurveMonotoneX_TwoPoints_LineEndCase2()
    {
        var path = GenerateWithCurve(D3Curve.MonotoneX, [(0, 0), (10, 10)]);
        Assert.NotNull(path);
    }

    [Fact]
    public void CurveMonotoneX_ThreePoints_HitsCase2InPoint_AndCase3InLineEnd()
    {
        var path = GenerateWithCurve(D3Curve.MonotoneX, [(0, 0), (5, 10), (10, 0)]);
        Assert.NotNull(path);
    }

    [Fact]
    public void CurveMonotoneX_Skips_Coincident_Points()
    {
        // The (x == _x1 && y == _y1) early-return short-circuits an iteration.
        var path = GenerateWithCurve(D3Curve.MonotoneX,
            [(0, 0), (5, 10), (5, 10), (10, 0)]);
        Assert.NotNull(path);
    }

    [Fact]
    public void CurveMonotoneX_Vertical_Segment_ZeroSlope()
    {
        // dx=0 between two consecutive x's → forces the dx2!=0 false branch.
        var path = GenerateWithCurve(D3Curve.MonotoneX,
            [(0, 0), (0, 5), (5, 10), (10, 0)]);
        Assert.NotNull(path);
    }
}

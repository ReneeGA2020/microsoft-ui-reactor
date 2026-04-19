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
}

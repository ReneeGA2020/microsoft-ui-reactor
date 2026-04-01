// Port of d3-path/test/path-test.js — validates PathBuilder behavior matches d3-path

using Xunit;

namespace Duct.D3.Tests;

public class PathBuilderTests
{
    private static void AssertPath(PathBuilder p, string expected)
    {
        // Normalize: D3 tests compare path strings with some floating-point tolerance.
        // We compare exact strings here since our F() formatting should match.
        Assert.Equal(expected, p.ToString());
    }

    [Fact]
    public void Path_Empty_ReturnsEmptyString()
    {
        var p = new PathBuilder();
        AssertPath(p, "");
    }

    [Fact]
    public void MoveTo_AppendsM()
    {
        var p = new PathBuilder();
        p.MoveTo(150, 50);
        AssertPath(p, "M150,50");
    }

    [Fact]
    public void MoveTo_Then_LineTo()
    {
        var p = new PathBuilder();
        p.MoveTo(150, 50);
        p.LineTo(200, 100);
        AssertPath(p, "M150,50L200,100");
    }

    [Fact]
    public void Multiple_MoveTo()
    {
        var p = new PathBuilder();
        p.MoveTo(150, 50);
        p.LineTo(200, 100);
        p.MoveTo(100, 50);
        AssertPath(p, "M150,50L200,100M100,50");
    }

    [Fact]
    public void ClosePath_AppendsZ()
    {
        var p = new PathBuilder();
        p.MoveTo(150, 50);
        p.ClosePath();
        AssertPath(p, "M150,50Z");
    }

    [Fact]
    public void ClosePath_Twice()
    {
        var p = new PathBuilder();
        p.MoveTo(150, 50);
        p.ClosePath();
        p.ClosePath();
        AssertPath(p, "M150,50ZZ");
    }

    [Fact]
    public void ClosePath_EmptyPath_NoOp()
    {
        var p = new PathBuilder();
        p.ClosePath();
        AssertPath(p, "");
    }

    [Fact]
    public void LineTo_AppendsL()
    {
        var p = new PathBuilder();
        p.MoveTo(150, 50);
        p.LineTo(200, 100);
        p.LineTo(100, 50);
        AssertPath(p, "M150,50L200,100L100,50");
    }

    [Fact]
    public void QuadraticCurveTo_AppendsQ()
    {
        var p = new PathBuilder();
        p.MoveTo(150, 50);
        p.QuadraticCurveTo(100, 50, 200, 100);
        AssertPath(p, "M150,50Q100,50,200,100");
    }

    [Fact]
    public void BezierCurveTo_AppendsC()
    {
        var p = new PathBuilder();
        p.MoveTo(150, 50);
        p.BezierCurveTo(100, 50, 0, 24, 200, 100);
        AssertPath(p, "M150,50C100,50,0,24,200,100");
    }

    [Fact]
    public void Arc_NegativeRadius_Throws()
    {
        var p = new PathBuilder();
        p.MoveTo(150, 100);
        Assert.Throws<ArgumentException>(() => p.Arc(100, 100, -50, 0, Math.PI / 2));
    }

    [Fact]
    public void Arc_ZeroRadius_OnlyM()
    {
        var p = new PathBuilder();
        p.Arc(100, 100, 0, 0, Math.PI / 2);
        AssertPath(p, "M100,100");
    }

    [Fact]
    public void Arc_FullCircle_EmptyPath()
    {
        var p = new PathBuilder();
        p.Arc(100, 100, 50, 0, Math.PI * 2);
        AssertPath(p, "M150,100A50,50,0,1,1,50,100A50,50,0,1,1,150,100");
    }

    [Fact]
    public void Arc_HalfPi_SmallArc()
    {
        var p = new PathBuilder();
        p.MoveTo(150, 100);
        p.Arc(100, 100, 50, 0, Math.PI / 2);
        AssertPath(p, "M150,100A50,50,0,0,1,100,150");
    }

    [Fact]
    public void Arc_FullCircle_WithPriorMoveTo()
    {
        var p = new PathBuilder();
        p.MoveTo(150, 100);
        p.Arc(100, 100, 50, 0, Math.PI * 2);
        AssertPath(p, "M150,100A50,50,0,1,1,50,100A50,50,0,1,1,150,100");
    }

    [Fact]
    public void Arc_Clockwise_SmallArc()
    {
        var p = new PathBuilder();
        p.MoveTo(150, 100);
        p.Arc(100, 100, 50, 0, Math.PI / 2, false);
        AssertPath(p, "M150,100A50,50,0,0,1,100,150");
    }

    [Fact]
    public void Arc_CounterClockwise_FullCircle()
    {
        var p = new PathBuilder();
        p.MoveTo(150, 100);
        p.Arc(100, 100, 50, 0, 1e-16, true);
        AssertPath(p, "M150,100A50,50,0,1,0,50,100A50,50,0,1,0,150,100");
    }

    [Fact]
    public void ArcTo_NegativeRadius_Throws()
    {
        var p = new PathBuilder();
        p.MoveTo(150, 100);
        Assert.Throws<ArgumentException>(() => p.ArcTo(270, 39, 163, 100, -53));
    }

    [Fact]
    public void ArcTo_EmptyPath_AppendsM()
    {
        var p = new PathBuilder();
        p.ArcTo(270, 39, 163, 100, 53);
        AssertPath(p, "M270,39");
    }

    [Fact]
    public void Rect_AppendsMhvhZ()
    {
        var p = new PathBuilder();
        p.MoveTo(150, 100);
        p.Rect(100, 200, 50, 25);
        AssertPath(p, "M150,100M100,200h50v25h-50Z");
    }

    [Fact]
    public void ArcTo_LineStartsAtCurrentPoint_AppendsA()
    {
        var p = new PathBuilder();
        p.MoveTo(100, 100);
        p.ArcTo(200, 100, 200, 200, 100);
        AssertPath(p, "M100,100A100,100,0,0,1,200,200");
    }

    [Fact]
    public void Arc_QuarterCircle_FromNegativeHalfPi()
    {
        // Use rounding digits to avoid floating-point epsilon near zero
        var p = new PathBuilder(3);
        p.Arc(0, 50, 50, -Math.PI / 2, 0);
        // Start: (0+50*cos(-pi/2), 50+50*sin(-pi/2)) ≈ (0, 0)
        // End: (0+50*cos(0), 50+50*sin(0)) = (50, 50)
        AssertPath(p, "M0,0A50,50,0,0,1,50,50");
    }
}

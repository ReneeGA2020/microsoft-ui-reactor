// Extended tests for BandScale, PointScale — covers defaults, padding effects, alignment,
// rounding, copy independence, edge cases, and factory methods not fully exercised in ScaleTests.cs

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class BandScaleExtendedTests
{
    [Fact]
    public void Default_DomainAndRange()
    {
        var s = new BandScale<string>();
        Assert.Empty(s.Domain);
        Assert.Equal([0.0, 1.0], s.Range);
        Assert.Equal(0.0, s.PaddingInner);
        Assert.Equal(0.0, s.PaddingOuter);
        Assert.Equal(0.5, s.Align);
    }

    [Fact]
    public void Map_ReturnsBandStartPosition()
    {
        var s = new BandScale<string>().SetDomain("a", "b", "c", "d").SetRange(0, 200);
        // 4 bands in [0,200] with no padding: step = 200/4 = 50, bandwidth = 50
        Assert.Equal(0, s.Map("a"));
        Assert.Equal(50, s.Map("b"));
        Assert.Equal(100, s.Map("c"));
        Assert.Equal(150, s.Map("d"));
    }

    [Fact]
    public void Bandwidth_ComputedCorrectly()
    {
        var s = new BandScale<string>().SetDomain("x", "y").SetRange(0, 100);
        // 2 bands, no padding: step = 100/2 = 50, bandwidth = 50*(1-0) = 50
        Assert.Equal(50, s.Bandwidth);
        Assert.Equal(50, s.Step);
    }

    [Fact]
    public void Step_IncludesPadding()
    {
        var s = new BandScale<string>().SetDomain("a", "b").SetRange(0, 100).SetPaddingInner(0.5);
        // step = 100 / max(1, 2 - 0.5 + 0*2) = 100 / 1.5 ≈ 66.67
        Assert.Equal(66.67, s.Step, 2);
        // bandwidth = step * (1 - 0.5) = step * 0.5 ≈ 33.33
        Assert.Equal(33.33, s.Bandwidth, 2);
    }

    [Fact]
    public void PaddingInner_ClampedTo01()
    {
        var s = new BandScale<string>();
        s.PaddingInner = 2.0;
        Assert.Equal(1.0, s.PaddingInner);
        s.PaddingInner = -1.0;
        Assert.Equal(0.0, s.PaddingInner);
    }

    [Fact]
    public void PaddingOuter_AffectsLayout()
    {
        var s1 = new BandScale<string>().SetDomain("a", "b").SetRange(0, 100).SetPaddingOuter(0);
        var s2 = new BandScale<string>().SetDomain("a", "b").SetRange(0, 100).SetPaddingOuter(1);
        // Outer padding shifts bands inward, so first band starts later
        Assert.True(s2.Map("a") > s1.Map("a"));
    }

    [Fact]
    public void SetPadding_SetsBothInnerAndOuter()
    {
        var s = new BandScale<string>().SetDomain("a", "b", "c").SetRange(0, 300);
        s.Padding = 0.2;
        Assert.Equal(0.2, s.PaddingInner);
        Assert.Equal(0.2, s.PaddingOuter);
    }

    [Fact]
    public void Align_Zero_LeftAligned()
    {
        var s = new BandScale<string>().SetDomain("a", "b").SetRange(0, 100)
            .SetPaddingOuter(0.5).SetAlign(0);
        // With align=0, bands start as early as possible
        double posA_left = s.Map("a");

        var s2 = new BandScale<string>().SetDomain("a", "b").SetRange(0, 100)
            .SetPaddingOuter(0.5).SetAlign(1);
        double posA_right = s2.Map("a");

        Assert.True(posA_left < posA_right);
    }

    [Fact]
    public void Align_ClampedTo01()
    {
        var s = new BandScale<string>();
        s.Align = 5.0;
        Assert.Equal(1.0, s.Align);
        s.Align = -3.0;
        Assert.Equal(0.0, s.Align);
    }

    [Fact]
    public void SetRound_RoundsValues()
    {
        var s = new BandScale<string>().SetDomain("a", "b", "c").SetRange(0, 100).SetRound(true);
        // With rounding, step/bandwidth/start should be integer-ish
        Assert.Equal(Math.Round(s.Bandwidth), s.Bandwidth);
        Assert.Equal(Math.Floor(s.Step), s.Step);
        Assert.Equal(Math.Round(s.Map("a")), s.Map("a"));
    }

    [Fact]
    public void Copy_IsIndependent()
    {
        var s = new BandScale<string>().SetDomain("a", "b").SetRange(0, 100);
        var copy = s.Copy();

        copy.Domain = ["x", "y", "z"];
        Assert.Equal(2, s.Domain.Length);
        Assert.Equal(3, copy.Domain.Length);

        copy.Range = [0, 200];
        Assert.Equal(100.0, s.Range[1]);
    }

    [Fact]
    public void EmptyDomain_StepIsZero()
    {
        var s = new BandScale<string>().SetRange(0, 100);
        Assert.Equal(0, s.Step);
        Assert.Equal(0, s.Bandwidth);
    }

    [Fact]
    public void SingleDomainElement_FillsRange()
    {
        var s = new BandScale<string>().SetDomain("only").SetRange(0, 100);
        Assert.Equal(0, s.Map("only"));
        // step = 100 / max(1, 1-0+0) = 100
        Assert.Equal(100, s.Step);
        Assert.Equal(100, s.Bandwidth);
    }

    [Fact]
    public void UnknownDomainValue_ReturnsNaN()
    {
        var s = new BandScale<string>().SetDomain("a", "b").SetRange(0, 100);
        Assert.True(double.IsNaN(s.Map("missing")));
    }

    [Fact]
    public void DuplicateDomainValues_Deduplicated()
    {
        var s = new BandScale<string>().SetDomain("a", "b", "a", "c");
        Assert.Equal(3, s.Domain.Length);
    }

    [Fact]
    public void ReversedRange_ReversesMappingOrder()
    {
        var s = new BandScale<string>().SetDomain("a", "b", "c").SetRange(120, 0);
        // With reversed range, "a" should map to the highest position
        Assert.True(s.Map("a") > s.Map("c"));
    }

    [Fact]
    public void Factory_Create_ReturnsStringScale()
    {
        var s = BandScale.Create();
        Assert.Empty(s.Domain);
        // verify it works with strings
        s.Domain = ["x"];
        Assert.Equal(0, s.Map("x"));
    }

    [Fact]
    public void Factory_CreateWithDomain_SetsDomain()
    {
        var s = BandScale.Create("a", "b", "c");
        Assert.Equal(3, s.Domain.Length);
        Assert.Equal(["a", "b", "c"], s.Domain);
    }
}

public class PointScaleExtendedTests
{
    [Fact]
    public void Map_ReturnsPointPositions()
    {
        var s = new PointScale<string>().SetDomain("a", "b", "c").SetRange(0, 100);
        Assert.Equal(0, s.Map("a"));
        Assert.Equal(50, s.Map("b"));
        Assert.Equal(100, s.Map("c"));
    }

    [Fact]
    public void Step_DistanceBetweenPoints()
    {
        var s = new PointScale<string>().SetDomain("a", "b", "c").SetRange(0, 100);
        // 3 points in [0,100]: step = 100 / (3-1) = 50... but actually
        // BandScale with paddingInner=1: step = 100 / max(1, 3 - 1 + 0) = 50
        Assert.Equal(50, s.Step);
    }

    [Fact]
    public void Padding_ShiftsPointsInward()
    {
        var s1 = new PointScale<string>().SetDomain("a", "b").SetRange(0, 100).SetPadding(0);
        var s2 = new PointScale<string>().SetDomain("a", "b").SetRange(0, 100).SetPadding(1);
        // With padding, first point moves inward
        Assert.True(s2.Map("a") > s1.Map("a"));
    }

    [Fact]
    public void Align_AffectsPosition()
    {
        var s1 = new PointScale<string>().SetDomain("a").SetRange(0, 100).SetAlign(0);
        var s2 = new PointScale<string>().SetDomain("a").SetRange(0, 100).SetAlign(1);
        // Single point: align=0 puts it at start, align=1 at end
        Assert.True(s1.Map("a") < s2.Map("a"));
    }

    [Fact]
    public void Copy_IsIndependent()
    {
        var s = new PointScale<string>().SetDomain("a", "b").SetRange(0, 100);
        var copy = s.Copy();

        copy.Domain = ["x", "y", "z"];
        Assert.Equal(2, s.Domain.Length);
        Assert.Equal(3, copy.Domain.Length);
    }

    [Fact]
    public void UnknownValue_ReturnsNaN()
    {
        var s = new PointScale<string>().SetDomain("a", "b").SetRange(0, 100);
        Assert.True(double.IsNaN(s.Map("nope")));
    }

    [Fact]
    public void Factory_Create_ReturnsStringScale()
    {
        var s = PointScale.Create();
        Assert.Empty(s.Domain);
    }

    [Fact]
    public void Factory_CreateWithDomain_SetsDomain()
    {
        var s = PointScale.Create("a", "b");
        Assert.Equal(["a", "b"], s.Domain);
    }
}

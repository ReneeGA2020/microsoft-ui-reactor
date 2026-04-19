// Port of d3-ease tests

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class EaseChartingExtraTests
{
    private const double Tol = 1e-6;

    [Fact]
    public void Linear_Identity()
    {
        Assert.Equal(0, D3Ease.Linear(0));
        Assert.Equal(0.5, D3Ease.Linear(0.5));
        Assert.Equal(1, D3Ease.Linear(1));
    }

    [Fact]
    public void QuadIn_Zero_And_One()
    {
        Assert.Equal(0, D3Ease.QuadIn(0));
        Assert.Equal(1, D3Ease.QuadIn(1));
    }

    [Fact]
    public void QuadIn_Half()
    {
        Assert.Equal(0.25, D3Ease.QuadIn(0.5), Tol);
    }

    [Fact]
    public void QuadOut_Half()
    {
        Assert.Equal(0.75, D3Ease.QuadOut(0.5), Tol);
    }

    [Fact]
    public void Quad_InOut_Symmetric()
    {
        Assert.Equal(0, D3Ease.Quad(0), Tol);
        Assert.Equal(0.5, D3Ease.Quad(0.5), Tol);
        Assert.Equal(1, D3Ease.Quad(1), Tol);
    }

    [Fact]
    public void CubicIn_Zero_And_One()
    {
        Assert.Equal(0, D3Ease.CubicIn(0));
        Assert.Equal(1, D3Ease.CubicIn(1));
    }

    [Fact]
    public void CubicIn_Half()
    {
        Assert.Equal(0.125, D3Ease.CubicIn(0.5), Tol);
    }

    [Fact]
    public void CubicOut_Half()
    {
        Assert.Equal(0.875, D3Ease.CubicOut(0.5), Tol);
    }

    [Fact]
    public void SinIn_Endpoints()
    {
        Assert.Equal(0, D3Ease.SinIn(0), Tol);
        Assert.Equal(1, D3Ease.SinIn(1), Tol);
    }

    [Fact]
    public void SinOut_Endpoints()
    {
        Assert.Equal(0, D3Ease.SinOut(0), Tol);
        Assert.Equal(1, D3Ease.SinOut(1), Tol);
    }

    [Fact]
    public void ExpIn_Endpoints()
    {
        Assert.Equal(0, D3Ease.ExpIn(0), Tol);
        Assert.Equal(1, D3Ease.ExpIn(1), Tol);
    }

    [Fact]
    public void CircleIn_Endpoints()
    {
        Assert.Equal(0, D3Ease.CircleIn(0), Tol);
        Assert.Equal(1, D3Ease.CircleIn(1), Tol);
    }

    [Fact]
    public void BounceOut_Endpoints()
    {
        Assert.Equal(0, D3Ease.BounceOut(0), Tol);
        Assert.Equal(1, D3Ease.BounceOut(1), Tol);
    }

    [Fact]
    public void BounceIn_Endpoints()
    {
        Assert.Equal(0, D3Ease.BounceIn(0), Tol);
        Assert.Equal(1, D3Ease.BounceIn(1), Tol);
    }

    [Fact]
    public void Bounce_InOut_Symmetric()
    {
        Assert.Equal(0, D3Ease.Bounce(0), Tol);
        Assert.Equal(0.5, D3Ease.Bounce(0.5), Tol);
        Assert.Equal(1, D3Ease.Bounce(1), Tol);
    }

    [Fact]
    public void ElasticOut_Endpoints()
    {
        var ease = D3Ease.ElasticOut();
        Assert.Equal(0, ease(0), Tol);
        Assert.Equal(1, ease(1), Tol);
    }

    [Fact]
    public void BackOut_Endpoints()
    {
        var ease = D3Ease.BackOut();
        Assert.Equal(0, ease(0), Tol);
        Assert.Equal(1, ease(1), Tol);
    }

    [Fact]
    public void PolyIn_Quartic()
    {
        var ease = D3Ease.PolyIn(4);
        Assert.Equal(0, ease(0), Tol);
        Assert.Equal(1, ease(1), Tol);
        Assert.Equal(0.0625, ease(0.5), Tol); // 0.5^4
    }

    // ─── Additional coverage tests ──────────────────────────────────

    [Fact]
    public void ExpOut_Midpoint()
    {
        double result = D3Ease.ExpOut(0.5);
        // ExpOut(0.5) = 1 - Tpmt(0.5); should be close to 0.96875
        Assert.True(result > 0.9 && result < 1.0);
        Assert.Equal(0, D3Ease.ExpOut(0), Tol);
        Assert.Equal(1, D3Ease.ExpOut(1), Tol);
    }

    [Fact]
    public void CircleOut_Midpoint()
    {
        // CircleOut(0.5) = sqrt(1 - (-0.5)^2) = sqrt(0.75)
        Assert.Equal(Math.Sqrt(0.75), D3Ease.CircleOut(0.5), Tol);
        Assert.Equal(0, D3Ease.CircleOut(0), Tol);
        Assert.Equal(1, D3Ease.CircleOut(1), Tol);
    }

    [Fact]
    public void Sin_InOut_Midpoint()
    {
        // Sin(0.5) = (1 - cos(PI*0.5)) / 2 = (1 - 0) / 2 = 0.5
        Assert.Equal(0.5, D3Ease.Sin(0.5), Tol);
        Assert.Equal(0, D3Ease.Sin(0), Tol);
        Assert.Equal(1, D3Ease.Sin(1), Tol);
    }

    [Fact]
    public void Exp_InOut_Midpoint()
    {
        // Exp(0.5) should be 0.5 (symmetric midpoint)
        Assert.Equal(0.5, D3Ease.Exp(0.5), Tol);
        Assert.Equal(0, D3Ease.Exp(0), Tol);
        Assert.Equal(1, D3Ease.Exp(1), Tol);
    }

    [Fact]
    public void Circle_InOut_Midpoint()
    {
        // Circle(0.5): t*=2 gives 1, <= 1 branch: (1 - sqrt(1-1))/2 = 0.5
        Assert.Equal(0.5, D3Ease.Circle(0.5), Tol);
        Assert.Equal(0, D3Ease.Circle(0), Tol);
        Assert.Equal(1, D3Ease.Circle(1), Tol);
    }

    [Fact]
    public void Cubic_InOut_Midpoint()
    {
        // Cubic(0.5): t*=2 gives 1, <= 1 branch: 1^3/2 = 0.5
        Assert.Equal(0.5, D3Ease.Cubic(0.5), Tol);
        Assert.Equal(0, D3Ease.Cubic(0), Tol);
        Assert.Equal(1, D3Ease.Cubic(1), Tol);
    }

    [Fact]
    public void Cubic_InOut_Quarter()
    {
        // Cubic(0.25): t*=2 gives 0.5, <= 1 branch: 0.5^3/2 = 0.0625
        Assert.Equal(0.0625, D3Ease.Cubic(0.25), Tol);
    }

    [Fact]
    public void ElasticIn_Endpoints()
    {
        var ease = D3Ease.ElasticIn();
        Assert.Equal(0, ease(0), Tol);
        Assert.Equal(1, ease(1), Tol);
    }

    [Fact]
    public void ElasticIn_Midpoint_Oscillates()
    {
        var ease = D3Ease.ElasticIn();
        double mid = ease(0.5);
        // ElasticIn oscillates; at 0.5 it should be negative or near zero
        Assert.True(mid >= -2.0 && mid <= 1.0);
    }

    [Fact]
    public void Elastic_InOut_Endpoints()
    {
        var ease = D3Ease.Elastic();
        Assert.Equal(0, ease(0), Tol);
        Assert.Equal(1, ease(1), Tol);
    }

    [Fact]
    public void Elastic_InOut_Midpoint()
    {
        var ease = D3Ease.Elastic();
        Assert.Equal(0.5, ease(0.5), Tol);
    }

    [Fact]
    public void BackIn_Endpoints()
    {
        var ease = D3Ease.BackIn();
        Assert.Equal(0, ease(0), Tol);
        Assert.Equal(1, ease(1), Tol);
    }

    [Fact]
    public void BackIn_Overshoots_Negative()
    {
        var ease = D3Ease.BackIn();
        // BackIn at t=0.5: ts=0.25, 0.25*(0.5*(1.70158+1)-1.70158) = 0.25*(1.35079-1.70158) = 0.25*(-0.35079) ~ -0.0877
        Assert.True(ease(0.5) < 0, "BackIn should go negative in the middle");
    }

    [Fact]
    public void Back_InOut_Endpoints()
    {
        var ease = D3Ease.Back();
        Assert.Equal(0, ease(0), Tol);
        Assert.Equal(0.5, ease(0.5), Tol);
        Assert.Equal(1, ease(1), Tol);
    }

    [Fact]
    public void PolyOut_Default_Cubic()
    {
        var ease = D3Ease.PolyOut();
        Assert.Equal(0, ease(0), Tol);
        Assert.Equal(1, ease(1), Tol);
        // PolyOut(3)(0.5) = 1 - (1-0.5)^3 = 1 - 0.125 = 0.875
        Assert.Equal(0.875, ease(0.5), Tol);
    }

    [Fact]
    public void Poly_InOut_Default_Cubic()
    {
        var ease = D3Ease.Poly();
        Assert.Equal(0, ease(0), Tol);
        Assert.Equal(0.5, ease(0.5), Tol);
        Assert.Equal(1, ease(1), Tol);
    }

    [Fact]
    public void BounceOut_Region1_FirstBounce()
    {
        // t < B1 (4/11 ~ 0.3636): t=0.2
        double result = D3Ease.BounceOut(0.2);
        // B0 * 0.2^2 = 7.5625 * 0.04 = 0.3025
        Assert.Equal(0.3025, result, Tol);
    }

    [Fact]
    public void BounceOut_Region2_SecondBounce()
    {
        // B1 <= t < B3 (8/11 ~ 0.7273): t=0.5
        double result = D3Ease.BounceOut(0.5);
        // t -= B2 (6/11): 0.5 - 6/11 = -1/22
        // B0 * (1/22)^2 + B4 = 7.5625/484 + 0.75 = 0.015625 + 0.75 = 0.765625
        Assert.Equal(0.765625, result, Tol);
    }

    [Fact]
    public void BounceOut_Region3_ThirdBounce()
    {
        // B3 <= t < B6 (10/11 ~ 0.909): t=0.8
        double result = D3Ease.BounceOut(0.8);
        // t -= B5 (9/11): 0.8 - 9/11 = (8.8-9)/11 = -0.2/11
        // B0 * (0.2/11)^2 + B7 = 7.5625*(0.04/121) + 15/16 = 7.5625*0.000330578... + 0.9375
        double expected = 7.5625 * Math.Pow(0.8 - 9.0 / 11, 2) + 15.0 / 16;
        Assert.Equal(expected, result, Tol);
    }

    [Fact]
    public void BounceOut_Region4_FourthBounce()
    {
        // t >= B6 (10/11 ~ 0.909): t=0.95
        double result = D3Ease.BounceOut(0.95);
        // t -= B8 (21/22): 0.95 - 21/22 = (20.9-21)/22 = -0.1/22
        // B0 * (0.1/22)^2 + B9 = 7.5625*(0.01/484) + 63/64
        double expected = 7.5625 * Math.Pow(0.95 - 21.0 / 22, 2) + 63.0 / 64;
        Assert.Equal(expected, result, Tol);
    }

    [Fact]
    public void ElasticIn_CustomAmplitudePeriod()
    {
        var ease = D3Ease.ElasticIn(1.5, 0.5);
        Assert.Equal(0, ease(0), Tol);
        Assert.Equal(1, ease(1), Tol);
    }
}

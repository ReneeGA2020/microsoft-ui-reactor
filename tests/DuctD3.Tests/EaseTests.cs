// Port of d3-ease tests

using Xunit;

namespace Duct.D3.Tests;

public class EaseTests
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
}

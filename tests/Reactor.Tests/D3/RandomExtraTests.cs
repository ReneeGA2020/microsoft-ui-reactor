// Distributions not covered by RandomTests: Cauchy, Weibull, IrwinHall.
// Upstream tests in d3-random test/cauchy-test.js, weibull-test.js, irwinHall-test.js.

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class RandomCauchyTests
{
    [Fact]
    public void Cauchy_Returns_Real_Values()
    {
        var gen = D3Random.Cauchy();
        for (int i = 0; i < 1000; i++)
        {
            double v = gen();
            Assert.False(double.IsNaN(v));
        }
    }

    [Fact]
    public void Cauchy_Scale_Changes_Spread()
    {
        // Compare interquartile-like spread for two scales.
        var tight = D3Random.Cauchy(0, 1);
        var wide = D3Random.Cauchy(0, 100);
        var tv = new List<double>();
        var wv = new List<double>();
        for (int i = 0; i < 500; i++) { tv.Add(tight()); wv.Add(wide()); }
        tv.Sort(); wv.Sort();
        // Compare the 40-60 percentile ranges (Cauchy has no mean/variance,
        // but the middle 20% band scales with the distribution's scale param).
        double tSpread = tv[300] - tv[200];
        double wSpread = wv[300] - wv[200];
        Assert.True(wSpread > tSpread * 5, $"wide spread={wSpread}, tight spread={tSpread}");
    }
}

public class RandomWeibullTests
{
    [Fact]
    public void Weibull_Default_Positive()
    {
        var gen = D3Random.Weibull();
        for (int i = 0; i < 1000; i++)
        {
            Assert.True(gen() >= 0);
        }
    }

    [Fact]
    public void Weibull_Shape_One_Equals_Exponential_1_Over_Lambda()
    {
        // Shape k=1 is just exponential with rate 1/lambda.
        var gen = D3Random.Weibull(1, 2);
        double sum = 0;
        for (int i = 0; i < 5000; i++) sum += gen();
        double mean = sum / 5000;
        // Exponential(1/2) mean = 2.
        Assert.InRange(mean, 1.5, 2.5);
    }
}

public class RandomIrwinHallTests
{
    [Fact]
    public void IrwinHall_Sum_Of_Uniforms_In_Range()
    {
        var gen = D3Random.IrwinHall(5);
        for (int i = 0; i < 1000; i++)
        {
            double v = gen();
            Assert.InRange(v, 0, 5);
        }
    }

    [Fact]
    public void IrwinHall_Zero_Always_Zero()
    {
        var gen = D3Random.IrwinHall(0);
        Assert.Equal(0, gen());
    }

    [Fact]
    public void IrwinHall_N_Mean_Approximately_N_Over_2()
    {
        var gen = D3Random.IrwinHall(10);
        double sum = 0;
        for (int i = 0; i < 5000; i++) sum += gen();
        double mean = sum / 5000;
        Assert.InRange(mean, 4.5, 5.5);
    }
}

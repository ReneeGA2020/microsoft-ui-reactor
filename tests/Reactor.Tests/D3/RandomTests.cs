// Port of d3-random tests

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class RandomTests
{
    private const int N = 1000;

    [Fact]
    public void Uniform_InRange()
    {
        var gen = D3Random.Uniform(10, 20);
        for (int i = 0; i < N; i++)
        {
            double v = gen();
            Assert.InRange(v, 10, 20);
        }
    }

    [Fact]
    public void Uniform_DefaultRange()
    {
        var gen = D3Random.Uniform();
        for (int i = 0; i < N; i++)
        {
            double v = gen();
            Assert.InRange(v, 0, 1);
        }
    }

    [Fact]
    public void Int_InRange()
    {
        var gen = D3Random.Int(0, 10);
        for (int i = 0; i < N; i++)
        {
            int v = gen();
            Assert.InRange(v, 0, 9);
        }
    }

    [Fact]
    public void Normal_MeanApproximatelyCorrect()
    {
        var gen = D3Random.Normal(100, 10);
        double sum = 0;
        for (int i = 0; i < N; i++) sum += gen();
        double mean = sum / N;
        Assert.InRange(mean, 90, 110);
    }

    [Fact]
    public void LogNormal_Positive()
    {
        var gen = D3Random.LogNormal();
        for (int i = 0; i < N; i++)
        {
            Assert.True(gen() > 0);
        }
    }

    [Fact]
    public void Exponential_Positive()
    {
        var gen = D3Random.Exponential(1);
        for (int i = 0; i < N; i++)
        {
            Assert.True(gen() > 0);
        }
    }

    [Fact]
    public void Bernoulli_ZeroOrOne()
    {
        var gen = D3Random.Bernoulli(0.5);
        for (int i = 0; i < N; i++)
        {
            int v = gen();
            Assert.True(v == 0 || v == 1);
        }
    }

    [Fact]
    public void Binomial_InRange()
    {
        var gen = D3Random.Binomial(10, 0.5);
        for (int i = 0; i < N; i++)
        {
            int v = gen();
            Assert.InRange(v, 0, 10);
        }
    }

    [Fact]
    public void Geometric_NonNegative()
    {
        var gen = D3Random.Geometric(0.5);
        for (int i = 0; i < N; i++)
        {
            Assert.True(gen() >= 0);
        }
    }

    [Fact]
    public void Poisson_NonNegative()
    {
        var gen = D3Random.Poisson(5);
        for (int i = 0; i < N; i++)
        {
            Assert.True(gen() >= 0);
        }
    }

    [Fact]
    public void Pareto_GreaterThanOne()
    {
        var gen = D3Random.Pareto(1);
        for (int i = 0; i < N; i++)
        {
            Assert.True(gen() >= 1);
        }
    }

    [Fact]
    public void Bates_InUnitRange()
    {
        var gen = D3Random.Bates(10);
        for (int i = 0; i < N; i++)
        {
            double v = gen();
            Assert.InRange(v, 0, 1);
        }
    }
}

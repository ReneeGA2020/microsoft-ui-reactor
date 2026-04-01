// Tests for group and bin functions

using Xunit;

namespace Duct.D3.Tests;

public class GroupTests
{
    [Fact]
    public void Group_ByKey()
    {
        var data = new[] { ("a", 1), ("b", 2), ("a", 3) };
        var groups = D3Group.Group(data, d => d.Item1);
        Assert.Equal(2, groups.Count);
        Assert.Equal(2, groups["a"].Count);
        Assert.Single(groups["b"]);
    }

    [Fact]
    public void Rollup_SumByKey()
    {
        var data = new[] { ("a", 1.0), ("b", 2.0), ("a", 3.0) };
        var sums = D3Group.Rollup(data, d => d.Item1, g => g.Sum(x => x.Item2));
        Assert.Equal(4.0, sums["a"]);
        Assert.Equal(2.0, sums["b"]);
    }

    [Fact]
    public void Index_FirstMatch()
    {
        var data = new[] { ("a", 1), ("b", 2), ("a", 3) };
        var index = D3Group.Index(data, d => d.Item1);
        Assert.Equal(("a", 1), index["a"]); // first match
    }
}

public class BinTests
{
    [Fact]
    public void Bin_DefaultThresholds()
    {
        var data = Enumerable.Range(0, 100).Select(i => (double)i).ToList();
        var bins = BinGenerator.Create().Generate(data);
        Assert.True(bins.Length > 0);

        // All items should be in some bin
        int total = bins.Sum(b => b.Count);
        Assert.Equal(100, total);
    }

    [Fact]
    public void Bin_CustomThresholdCount()
    {
        var data = Enumerable.Range(0, 100).Select(i => (double)i).ToList();
        var bins = BinGenerator.Create().SetThresholdCount(5).Generate(data);
        Assert.True(bins.Length > 0);
    }

    [Fact]
    public void Bin_BoundsCorrect()
    {
        var data = new double[] { 1, 2, 3, 4, 5 };
        var bins = BinGenerator.Create().Generate(data);
        Assert.True(bins.Length > 0);
        Assert.True(bins[0].X0 <= 1);
        Assert.True(bins[^1].X1 >= 5);
    }
}

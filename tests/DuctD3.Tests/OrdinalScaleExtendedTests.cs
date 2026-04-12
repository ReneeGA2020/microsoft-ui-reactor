// Extended tests for OrdinalScale — covers cycling, unknown handling, implicit domain growth,
// copy independence, edge cases, and factory methods not fully exercised in ScaleTests.cs

using Xunit;

namespace Duct.D3.Tests;

public class OrdinalScaleExtendedTests
{
    [Fact]
    public void Map_ReturnsRangeAtIndex()
    {
        var s = new OrdinalScale<string>(["a", "b", "c"], [10, 20, 30]);
        Assert.Equal(10, s.Map("a"));
        Assert.Equal(20, s.Map("b"));
        Assert.Equal(30, s.Map("c"));
    }

    [Fact]
    public void Map_CyclesWhenDomainLargerThanRange()
    {
        var s = new OrdinalScale<string>(["a", "b", "c", "d", "e"], [10, 20, 30]);
        // d is index 3 → 3 % 3 = 0 → 10
        Assert.Equal(10, s.Map("d"));
        // e is index 4 → 4 % 3 = 1 → 20
        Assert.Equal(20, s.Map("e"));
    }

    [Fact]
    public void Map_UnknownValueWithSetUnknown_ReturnsUnknown()
    {
        var s = new OrdinalScale<string>(["a", "b"], [10, 20]).SetUnknown(-99);
        Assert.Equal(-99, s.Map("z"));
        // Domain should NOT grow when Unknown is set to a finite value
        Assert.Equal(2, s.Domain.Length);
    }

    [Fact]
    public void Map_UnknownValueWithDefaultUnknown_AddsToDomain()
    {
        var s = new OrdinalScale<string>().SetRange(10, 20, 30);
        Assert.True(double.IsNaN(s.Unknown)); // default is NaN
        s.Map("x"); // implicitly added
        s.Map("y"); // implicitly added
        Assert.Equal(2, s.Domain.Length);
        Assert.Equal(10, s.Map("x"));
        Assert.Equal(20, s.Map("y"));
    }

    [Fact]
    public void Copy_IsIndependent()
    {
        var s = new OrdinalScale<string>(["a", "b"], [10, 20]);
        var copy = s.Copy();

        copy.Domain = ["x", "y", "z"];
        Assert.Equal(2, s.Domain.Length);
        Assert.Equal(3, copy.Domain.Length);

        copy.Range = [100, 200, 300];
        Assert.Equal(2, s.Range.Length);
    }

    [Fact]
    public void Copy_PreservesUnknown()
    {
        var s = new OrdinalScale<string>().SetUnknown(-1);
        var copy = s.Copy();
        Assert.Equal(-1, copy.Unknown);
    }

    [Fact]
    public void EmptyRange_ReturnsNaN()
    {
        var s = new OrdinalScale<string>(["a", "b"], []);
        Assert.True(double.IsNaN(s.Map("a")));
    }

    [Fact]
    public void DuplicateDomainValues_Deduplicated()
    {
        var s = new OrdinalScale<string>().SetDomain("a", "b", "a", "c");
        Assert.Equal(3, s.Domain.Length);
        Assert.Equal(["a", "b", "c"], s.Domain);
    }

    [Fact]
    public void SetDomain_ClearsPreviousDomain()
    {
        var s = new OrdinalScale<string>(["a", "b", "c"], [10, 20, 30]);
        s.Domain = ["x", "y"];
        Assert.Equal(2, s.Domain.Length);
        Assert.True(double.IsNaN(s.Unknown) || s.Map("a") != 10);
    }

    [Fact]
    public void Factory_Create_ReturnsEmptyStringScale()
    {
        var s = OrdinalScale.Create();
        Assert.Empty(s.Domain);
        Assert.Empty(s.Range);
    }

    [Fact]
    public void Factory_CreateWithDomainAndRange()
    {
        var s = OrdinalScale.Create<string>(["a", "b", "c"], [1, 2, 3]);
        Assert.Equal(3, s.Domain.Length);
        Assert.Equal(1, s.Map("a"));
        Assert.Equal(2, s.Map("b"));
        Assert.Equal(3, s.Map("c"));
    }

    [Fact]
    public void IntegerDomain_WorksCorrectly()
    {
        var s = new OrdinalScale<int>();
        s.Domain = [10, 20, 30];
        s.Range = [1.0, 2.0, 3.0];
        Assert.Equal(1.0, s.Map(10));
        Assert.Equal(2.0, s.Map(20));
        Assert.Equal(3.0, s.Map(30));
    }
}

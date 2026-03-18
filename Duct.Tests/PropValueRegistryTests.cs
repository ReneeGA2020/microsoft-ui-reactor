using Duct.Core;
using Xunit;

namespace Duct.Tests;

public class PropValueRegistryTests
{
    [Fact]
    public void Register_Returns_Sequential_Ids_Starting_At_1()
    {
        var registry = new PropValueRegistry();
        Assert.Equal(1UL, registry.Register("a"));
        Assert.Equal(2UL, registry.Register("b"));
        Assert.Equal(3UL, registry.Register("c"));
    }

    [Fact]
    public void Retrieve_RoundTrips_Registered_Values()
    {
        var registry = new PropValueRegistry();
        var obj1 = "hello";
        var obj2 = new object();
        var id1 = registry.Register(obj1);
        var id2 = registry.Register(obj2);

        Assert.Same(obj1, registry.Retrieve(id1));
        Assert.Same(obj2, registry.Retrieve(id2));
    }

    [Fact]
    public void Retrieve_Zero_Returns_Null_Sentinel()
    {
        var registry = new PropValueRegistry();
        registry.Register("x");
        Assert.Null(registry.Retrieve(0UL));
    }

    [Fact]
    public void Retrieve_OutOfRange_Returns_Null()
    {
        var registry = new PropValueRegistry();
        Assert.Null(registry.Retrieve(999UL));
    }

    [Fact]
    public void Clear_Resets_Registry()
    {
        var registry = new PropValueRegistry();
        var id = registry.Register("first");
        Assert.Equal(1UL, id);

        registry.Clear();

        // After clear, IDs restart from 1
        var newId = registry.Register("second");
        Assert.Equal(1UL, newId);
        Assert.Equal("second", registry.Retrieve(newId));
    }

    [Fact]
    public void No_Deduplication_Same_Value_Gets_Different_Ids()
    {
        var registry = new PropValueRegistry();
        var id1 = registry.Register("same");
        var id2 = registry.Register("same");
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void Register_Delegates()
    {
        var registry = new PropValueRegistry();
        Action action = () => { };
        var id = registry.Register(action);
        Assert.Same(action, registry.Retrieve(id));
    }
}

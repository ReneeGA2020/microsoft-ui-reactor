// Tests for stratify (tabular → hierarchy)

using Xunit;

namespace Duct.D3.Tests;

public class StratifyTests
{
    private record Row(string Id, string? ParentId, double Value = 0);

    [Fact]
    public void Stratify_BuildsTree()
    {
        var data = new Row[]
        {
            new("root", null),
            new("a", "root"),
            new("b", "root"),
            new("a1", "a"),
        };

        var tree = Stratify.Create<Row>()
            .SetId(r => r.Id)
            .SetParentId(r => r.ParentId)
            .Build(data);

        Assert.Equal("root", tree.Data.Id);
        Assert.Equal(2, tree.Children.Count);
    }

    [Fact]
    public void Stratify_SetsDepths()
    {
        var data = new Row[]
        {
            new("root", null),
            new("a", "root"),
            new("a1", "a"),
        };

        var tree = Stratify.Create<Row>()
            .SetId(r => r.Id)
            .SetParentId(r => r.ParentId)
            .Build(data);

        Assert.Equal(0, tree.Depth);
        Assert.Equal(1, tree.Children[0].Depth);
        Assert.Equal(2, tree.Children[0].Children[0].Depth);
    }

    [Fact]
    public void Stratify_ThrowsOnDuplicateId()
    {
        var data = new Row[]
        {
            new("a", null),
            new("a", null),
        };

        Assert.Throws<InvalidOperationException>(() =>
            Stratify.Create<Row>()
                .SetId(r => r.Id)
                .SetParentId(r => r.ParentId)
                .Build(data));
    }

    [Fact]
    public void Stratify_ThrowsOnMissingParent()
    {
        var data = new Row[]
        {
            new("root", null),
            new("a", "missing"),
        };

        Assert.Throws<InvalidOperationException>(() =>
            Stratify.Create<Row>()
                .SetId(r => r.Id)
                .SetParentId(r => r.ParentId)
                .Build(data));
    }

    [Fact]
    public void Stratify_ThrowsOnNoRoot()
    {
        var data = new Row[]
        {
            new("a", "b"),
            new("b", "a"),
        };

        Assert.Throws<InvalidOperationException>(() =>
            Stratify.Create<Row>()
                .SetId(r => r.Id)
                .SetParentId(r => r.ParentId)
                .Build(data));
    }

    [Fact]
    public void Stratify_BuildTreemap()
    {
        var data = new Row[]
        {
            new("root", null),
            new("a", "root", 10),
            new("b", "root", 20),
        };

        var node = Stratify.Create<Row>()
            .SetId(r => r.Id)
            .SetParentId(r => r.ParentId)
            .BuildTreemap(data, r => r.Value);

        Assert.Equal(30, node.Value);
        Assert.Equal(2, node.Children.Count);
    }

    [Fact]
    public void Stratify_BuildPartition()
    {
        var data = new Row[]
        {
            new("root", null),
            new("a", "root", 40),
            new("b", "root", 60),
        };

        var node = Stratify.Create<Row>()
            .SetId(r => r.Id)
            .SetParentId(r => r.ParentId)
            .BuildPartition(data, r => r.Value);

        Assert.Equal(100, node.Value);
    }
}

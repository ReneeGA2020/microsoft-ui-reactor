// Tests for d3-hierarchy treemap and pack layouts

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class TreemapTests
{
    private record FileNode(string Name, double Size, FileNode[]? Children = null);

    [Fact]
    public void Treemap_LeafRectanglesCoverArea()
    {
        var root = new FileNode("root", 0,
        [
            new("a", 10),
            new("b", 20),
            new("c", 30),
        ]);

        var layout = TreemapLayout.Create<FileNode>().Size(100, 100);
        var node = layout.Hierarchy(root, n => n.Children, n => n.Size);
        layout.Layout(node);

        foreach (var leaf in node.Leaves())
        {
            Assert.True(leaf.Width > 0);
            Assert.True(leaf.Height > 0);
        }
    }

    [Fact]
    public void Treemap_RootCoversFullSize()
    {
        var root = new FileNode("root", 0,
        [
            new("a", 50),
            new("b", 50),
        ]);

        var layout = TreemapLayout.Create<FileNode>().Size(200, 100);
        var node = layout.Hierarchy(root, n => n.Children, n => n.Size);
        layout.Layout(node);

        Assert.Equal(0, node.X0);
        Assert.Equal(0, node.Y0);
        Assert.Equal(200, node.X1);
        Assert.Equal(100, node.Y1);
    }

    [Fact]
    public void Treemap_ValuesSum()
    {
        var root = new FileNode("root", 0,
        [
            new("a", 10),
            new("b", 20),
        ]);

        var layout = TreemapLayout.Create<FileNode>().Size(100, 100);
        var node = layout.Hierarchy(root, n => n.Children, n => n.Size);

        Assert.Equal(30, node.Value);
    }

    [Fact]
    public void Treemap_Nested()
    {
        var root = new FileNode("root", 0,
        [
            new("group1", 0, [new("a", 10), new("b", 20)]),
            new("group2", 0, [new("c", 30)]),
        ]);

        var layout = TreemapLayout.Create<FileNode>().Size(100, 100);
        var node = layout.Hierarchy(root, n => n.Children, n => n.Size);
        layout.Layout(node);

        Assert.Equal(60, node.Value);
        var leaves = node.Leaves().ToList();
        Assert.Equal(3, leaves.Count);
    }
}

public class PackTests
{
    private record Item(string Name, double Size, Item[]? Children = null);

    [Fact]
    public void Pack_CirclesHavePositiveRadius()
    {
        var root = new Item("root", 0,
        [
            new("a", 10),
            new("b", 20),
            new("c", 30),
        ]);

        var layout = PackLayout.Create<Item>().Size(200);
        var node = layout.Layout(root, n => n.Children, n => n.Size);

        foreach (var leaf in node.Leaves())
        {
            Assert.True(leaf.R > 0);
        }
    }

    [Fact]
    public void Pack_RootEncloses()
    {
        var root = new Item("root", 0,
        [
            new("a", 25),
            new("b", 25),
        ]);

        var layout = PackLayout.Create<Item>().Size(100);
        var node = layout.Layout(root, n => n.Children, n => n.Size);

        Assert.True(node.R > 0);
        foreach (var child in node.Children)
        {
            double dist = Math.Sqrt(
                (child.X - node.X) * (child.X - node.X) +
                (child.Y - node.Y) * (child.Y - node.Y));
            Assert.True(dist + child.R <= node.R + 1);
        }
    }
}

// Tests for d3-hierarchy cluster layout

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class ClusterTests
{
    private record Node(string Name, Node[]? Children = null);

    [Fact]
    public void Cluster_LeavesAtSameDepth()
    {
        var root = new Node("root",
        [
            new("a", [new("a1"), new("a2")]),
            new("b"),
        ]);

        var layout = ClusterLayout.Create<Node>().Size(200, 200);
        var tree = layout.Hierarchy(root, n => n.Children);
        layout.Layout(tree);

        var leaves = new List<TreeNode<Node>>();
        void CollectLeaves(TreeNode<Node> n)
        {
            if (n.Children.Count == 0) leaves.Add(n);
            foreach (var c in n.Children) CollectLeaves(c);
        }
        CollectLeaves(tree);

        double leafY = leaves[0].Y;
        foreach (var leaf in leaves)
        {
            Assert.Equal(leafY, leaf.Y, 1);
        }
    }

    [Fact]
    public void Cluster_RootAtTop()
    {
        var root = new Node("root", [new("a"), new("b")]);

        var layout = ClusterLayout.Create<Node>().Size(200, 200);
        var tree = layout.Hierarchy(root, n => n.Children);
        layout.Layout(tree);

        Assert.True(tree.Y < tree.Children[0].Y);
    }

    [Fact]
    public void Cluster_SingleChild()
    {
        var root = new Node("root", [new("only")]);

        var layout = ClusterLayout.Create<Node>().Size(100, 100);
        var tree = layout.Hierarchy(root, n => n.Children);
        layout.Layout(tree);

        Assert.True(tree.Y < tree.Children[0].Y);
    }

    [Fact]
    public void Cluster_ManyLeaves()
    {
        var root = new Node("root",
        [
            new("a"), new("b"), new("c"), new("d"), new("e"),
        ]);

        var layout = ClusterLayout.Create<Node>().Size(400, 200);
        var tree = layout.Hierarchy(root, n => n.Children);
        layout.Layout(tree);

        var xs = tree.Children.Select(c => c.X).ToList();
        Assert.True(xs.Max() - xs.Min() > 100);
    }
}

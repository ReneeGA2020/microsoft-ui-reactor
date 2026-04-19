// Tests for d3-sankey layout

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class SankeyTests
{
    [Fact]
    public void Sankey_SimpleThreeNode()
    {
        var graph = new SankeyGraph
        {
            Nodes =
            [
                new SankeyNode { Id = "A" },
                new SankeyNode { Id = "B" },
                new SankeyNode { Id = "C" },
            ],
            Links =
            [
                new SankeyLink { SourceId = "A", TargetId = "B", Value = 10 },
                new SankeyLink { SourceId = "B", TargetId = "C", Value = 10 },
            ],
        };

        new SankeyLayout().Size(200, 100).Layout(graph);

        Assert.True(graph.Nodes[0].X0 < graph.Nodes[1].X0);
        Assert.True(graph.Nodes[1].X0 < graph.Nodes[2].X0);
    }

    [Fact]
    public void Sankey_NodeValues()
    {
        var graph = new SankeyGraph
        {
            Nodes =
            [
                new SankeyNode { Id = "A" },
                new SankeyNode { Id = "B" },
            ],
            Links =
            [
                new SankeyLink { SourceId = "A", TargetId = "B", Value = 50 },
            ],
        };

        new SankeyLayout().Size(200, 100).Layout(graph);

        Assert.Equal(50, graph.Nodes[0].Value);
        Assert.Equal(50, graph.Nodes[1].Value);
    }

    [Fact]
    public void Sankey_MultipleInputs()
    {
        var graph = new SankeyGraph
        {
            Nodes =
            [
                new SankeyNode { Id = "A" },
                new SankeyNode { Id = "B" },
                new SankeyNode { Id = "C" },
            ],
            Links =
            [
                new SankeyLink { SourceId = "A", TargetId = "C", Value = 30 },
                new SankeyLink { SourceId = "B", TargetId = "C", Value = 20 },
            ],
        };

        new SankeyLayout().Size(200, 100).Layout(graph);

        Assert.Equal(50, graph.Nodes[2].Value);
    }

    [Fact]
    public void Sankey_LinkPath()
    {
        var graph = new SankeyGraph
        {
            Nodes =
            [
                new SankeyNode { Id = "A" },
                new SankeyNode { Id = "B" },
            ],
            Links =
            [
                new SankeyLink { SourceId = "A", TargetId = "B", Value = 20 },
            ],
        };

        new SankeyLayout().Size(200, 100).Layout(graph);

        var path = SankeyLayout.LinkPath(graph.Links[0]);
        Assert.NotNull(path);
        Assert.Contains("C", path);
        Assert.Contains("Z", path);
    }

    [Fact]
    public void Sankey_NodePadding()
    {
        var graph = new SankeyGraph
        {
            Nodes =
            [
                new SankeyNode { Id = "A" },
                new SankeyNode { Id = "B" },
                new SankeyNode { Id = "C" },
                new SankeyNode { Id = "D" },
            ],
            Links =
            [
                new SankeyLink { SourceId = "A", TargetId = "C", Value = 10 },
                new SankeyLink { SourceId = "A", TargetId = "D", Value = 10 },
                new SankeyLink { SourceId = "B", TargetId = "C", Value = 10 },
                new SankeyLink { SourceId = "B", TargetId = "D", Value = 10 },
            ],
        };

        new SankeyLayout().Size(200, 100).SetNodePadding(10).Layout(graph);

        var leftNodes = graph.Nodes.Where(n => n.Depth == 0).OrderBy(n => n.Y0).ToList();
        if (leftNodes.Count >= 2)
        {
            Assert.True(leftNodes[1].Y0 >= leftNodes[0].Y1);
        }
    }

    [Fact]
    public void Sankey_DiamondShape()
    {
        var graph = new SankeyGraph
        {
            Nodes =
            [
                new SankeyNode { Id = "S" },
                new SankeyNode { Id = "A" },
                new SankeyNode { Id = "B" },
                new SankeyNode { Id = "T" },
            ],
            Links =
            [
                new SankeyLink { SourceId = "S", TargetId = "A", Value = 5 },
                new SankeyLink { SourceId = "S", TargetId = "B", Value = 5 },
                new SankeyLink { SourceId = "A", TargetId = "T", Value = 5 },
                new SankeyLink { SourceId = "B", TargetId = "T", Value = 5 },
            ],
        };

        new SankeyLayout().Size(300, 200).Layout(graph);

        var s = graph.Nodes.First(n => n.Id == "S");
        var t = graph.Nodes.First(n => n.Id == "T");
        Assert.Equal(0, s.Depth);
        Assert.Equal(2, t.Depth);
    }
}

// Tests for d3-hierarchy partition layout

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class PartitionTests
{
    private record FileItem(string Name, double Size, FileItem[]? Children = null);

    [Fact]
    public void Partition_RootCoversFullArea()
    {
        var root = new FileItem("root", 0,
        [
            new("a", 10),
            new("b", 20),
        ]);

        var layout = PartitionLayout.Create<FileItem>().Size(200, 100);
        var node = layout.Layout(root, n => n.Children, n => n.Size);

        Assert.Equal(0, node.X0);
        Assert.Equal(0, node.Y0);
        Assert.Equal(200, node.X1);
    }

    [Fact]
    public void Partition_ChildrenSubdivideWidth()
    {
        var root = new FileItem("root", 0,
        [
            new("a", 25),
            new("b", 75),
        ]);

        var layout = PartitionLayout.Create<FileItem>().Size(100, 100);
        var node = layout.Layout(root, n => n.Children, n => n.Size);

        Assert.Equal(2, node.Children.Count);
        double aWidth = node.Children[0].Width;
        double bWidth = node.Children[1].Width;
        Assert.True(bWidth > aWidth * 2);
    }

    [Fact]
    public void Partition_ChildrenAtNextDepth()
    {
        var root = new FileItem("root", 0,
        [
            new("a", 50),
            new("b", 50),
        ]);

        var layout = PartitionLayout.Create<FileItem>().Size(100, 100);
        var node = layout.Layout(root, n => n.Children, n => n.Size);

        Assert.Equal(node.Y1, node.Children[0].Y0, 5);
    }

    [Fact]
    public void Partition_Nested()
    {
        var root = new FileItem("root", 0,
        [
            new("dir", 0, [new("file1", 10), new("file2", 20)]),
            new("file3", 30),
        ]);

        var layout = PartitionLayout.Create<FileItem>().Size(300, 300);
        var node = layout.Layout(root, n => n.Children, n => n.Size);

        Assert.Equal(60, node.Value);
        var leaves = node.Leaves().ToList();
        Assert.Equal(3, leaves.Count);
    }

    [Fact]
    public void Partition_ToPolar_ForSunburst()
    {
        var root = new FileItem("root", 0, [new("a", 50), new("b", 50)]);

        var layout = PartitionLayout.Create<FileItem>().Size(100, 100);
        var node = layout.Layout(root, n => n.Children, n => n.Size);

        var (startAngle, endAngle, innerRadius, outerRadius) =
            node.Children[0].ToPolar(100, 100, 200);

        Assert.True(startAngle >= 0);
        Assert.True(endAngle > startAngle);
        Assert.True(outerRadius > innerRadius);
    }

    [Fact]
    public void Partition_Descendants()
    {
        var root = new FileItem("root", 0,
        [
            new("a", 10),
            new("b", 0, [new("b1", 5), new("b2", 5)]),
        ]);

        var layout = PartitionLayout.Create<FileItem>().Size(100, 100);
        var node = layout.Layout(root, n => n.Children, n => n.Size);

        Assert.Equal(5, node.Descendants().Count());
    }
}

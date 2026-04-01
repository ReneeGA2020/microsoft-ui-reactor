using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public sealed class SunburstSample : GallerySample
{
    public override string Title => "Sunburst";
    public override string Description =>
        "A sunburst chart showing disk usage as nested angular slices. Uses PartitionLayout " +
        "with ToPolar and ArcGenerator to render concentric rings of a hierarchy.";
    public override string Category => "Hierarchies";

    public override string SourceCode => """
        var partition = PartitionLayout.Create<DiskNode>()
            .Size(totalAngleWidth, totalHeightNorm);
        var root = partition.Layout(data, n => n.Children, n => n.Size);
        var arc = new ArcGenerator();

        foreach (var node in allNodes)
        {
            if (node.Parent == null) continue;
            var (startAngle, endAngle, innerRadius, outerRadius) =
                node.ToPolar(totalAngleWidth, totalHeightNorm, maxRadius);
            var fill = G.Brush(G.Palette[colorIdx % G.Palette.Length], opacity);
            string? pathData = arc.Generate(
                startAngle, endAngle, 0, innerRadius, outerRadius);
            if (pathData != null)
            {
                var path = G.MakePath(pathData, G.Gray(255), fill, 1);
                path.RenderTransform = new TranslateTransform { X = cx, Y = cy };
                canvas.Children.Add(path);
            }
        }
        """;

    record DiskNode(string Name, double Size = 0, DiskNode[]? Children = null);

    public override FrameworkElement Render()
    {
        const double W = 500, H = 500;
        double cx = W / 2, cy = H / 2;
        double maxRadius = Math.Min(W, H) / 2 - 10;
        var canvas = new Canvas { Width = W, Height = H };

        // Disk usage hierarchy (sizes in MB)
        var data = new DiskNode("C:\\", 0, [
            new("Users", 0, [
                new("Documents", 0, [
                    new("Reports", 450),
                    new("Photos", 1200),
                    new("Projects", 800),
                ]),
                new("Downloads", 0, [
                    new("Installers", 600),
                    new("Media", 900),
                ]),
                new("AppData", 0, [
                    new("Cache", 350),
                    new("Logs", 120),
                    new("Config", 80),
                ]),
            ]),
            new("Program Files", 0, [
                new("VS Code", 400),
                new("Office", 1500),
                new("Browser", 300),
                new("Games", 2200),
            ]),
            new("Windows", 0, [
                new("System32", 1800),
                new("WinSxS", 1200),
                new("Temp", 400),
            ]),
        ]);

        // Use PartitionLayout in polar coordinates
        // Size(totalWidth, totalHeight) where X maps to angle and Y maps to radius
        double totalAngleWidth = 1; // Normalized; ToPolar will scale
        double totalHeightNorm = 1;

        var partition = PartitionLayout.Create<DiskNode>().Size(totalAngleWidth, totalHeightNorm);
        var root = partition.Layout(data, n => n.Children, n => n.Size);

        var arc = new ArcGenerator();

        // Draw arcs for each node (skip root)
        var allNodes = new List<PartitionNode<DiskNode>>();
        CollectPartition(root, allNodes);

        foreach (var node in allNodes)
        {
            if (node.Parent == null) continue; // skip root

            var (startAngle, endAngle, innerRadius, outerRadius) =
                node.ToPolar(totalAngleWidth, totalHeightNorm, maxRadius);

            // Skip tiny slices
            if (endAngle - startAngle < 0.005) continue;

            int colorIdx = GetTopBranch(node);
            double opacity = 0.9 - node.Depth * 0.15;
            var fill = G.Brush(G.Palette[colorIdx % G.Palette.Length], Math.Max(0.3, opacity));

            string? pathData = arc.Generate(startAngle, endAngle, 0, innerRadius, outerRadius);
            if (pathData != null)
            {
                var path = G.MakePath(pathData, G.Gray(255), fill, 1);
                // Translate to center
                path.RenderTransform = new TranslateTransform { X = cx, Y = cy };
                canvas.Children.Add(path);
            }

            // Label on larger slices
            if ((endAngle - startAngle) > 0.15 && node.Children.Count == 0)
            {
                double midAngle = (startAngle + endAngle) / 2 - Math.PI / 2;
                double midR = (innerRadius + outerRadius) / 2;
                double lx = cx + Math.Cos(midAngle) * midR;
                double ly = cy + Math.Sin(midAngle) * midR;
                G.AddText(canvas, lx - 20, ly - 6, node.Data.Name, 8, G.Gray(30),
                    TextAlignment.Center, 40);
            }
        }

        // Center label
        G.AddText(canvas, cx - 20, cy - 7, "Disk", 12, G.Brush("#333333"), TextAlignment.Center, 40);

        return canvas;
    }

    static int GetTopBranch(PartitionNode<DiskNode> node)
    {
        var current = node;
        while (current.Parent != null && current.Parent.Parent != null)
            current = current.Parent;
        if (current.Parent == null) return 0;
        return current.Parent.Children.IndexOf(current);
    }

    static void CollectPartition(PartitionNode<DiskNode> node, List<PartitionNode<DiskNode>> list)
    {
        list.Add(node);
        foreach (var child in node.Children) CollectPartition(child, list);
    }
}

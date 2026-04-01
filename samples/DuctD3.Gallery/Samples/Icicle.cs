using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public sealed class IcicleSample : GallerySample
{
    public override string Title => "Icicle Chart";
    public override string Description =>
        "An icicle (partition) chart showing budget allocation as horizontal rectangles " +
        "stacked by depth. Each row represents a level in the hierarchy. Uses PartitionLayout.";
    public override string Category => "Hierarchies";

    public override string SourceCode => """
        var partition = PartitionLayout.Create<BudgetNode>()
            .Size(W, H).SetPadding(1);
        var root = partition.Layout(data, n => n.Children, n => n.Amount);

        foreach (var node in allNodes)
        {
            double w = node.Width, h = node.Height;
            if (w < 1 || h < 1) continue;
            int colorIdx = GetTopBranch(node);
            var fill = G.Brush(G.Palette[colorIdx % G.Palette.Length], opacity);
            G.AddRect(canvas, node.X0, node.Y0, w, h, fill, 1);
            if (w > 50 && h > 16)
                G.AddText(canvas, node.X0 + 4, node.Y0 + 3, label, 9, G.Gray(20));
        }
        """;

    record BudgetNode(string Name, double Amount = 0, BudgetNode[]? Children = null);

    public override FrameworkElement Render()
    {
        const double W = 700, H = 500;
        var canvas = new Canvas { Width = W, Height = H };

        // Company budget hierarchy (thousands $)
        var data = new BudgetNode("Total Budget", 0, [
            new("Engineering", 0, [
                new("Salaries", 0, [
                    new("Senior Eng", 450),
                    new("Mid Eng", 320),
                    new("Junior Eng", 180),
                ]),
                new("Cloud Infra", 0, [
                    new("AWS", 200),
                    new("Azure", 80),
                    new("GCP", 40),
                ]),
                new("Tools & Licenses", 0, [
                    new("IDE Licenses", 30),
                    new("CI/CD", 25),
                    new("Monitoring", 20),
                ]),
            ]),
            new("Sales & Marketing", 0, [
                new("Advertising", 0, [
                    new("Digital Ads", 180),
                    new("Print", 40),
                    new("Events", 90),
                ]),
                new("Sales Team", 0, [
                    new("Salaries", 280),
                    new("Commissions", 150),
                ]),
            ]),
            new("Operations", 0, [
                new("Rent", 120),
                new("Utilities", 35),
                new("Insurance", 50),
                new("Office Supplies", 15),
            ]),
            new("R&D", 0, [
                new("Research", 160),
                new("Prototyping", 80),
                new("Patents", 40),
            ]),
        ]);

        var partition = PartitionLayout.Create<BudgetNode>()
            .Size(W, H)
            .SetPadding(1);
        var root = partition.Layout(data, n => n.Children, n => n.Amount);

        // Draw all nodes as rectangles
        var allNodes = new List<PartitionNode<BudgetNode>>();
        CollectPartition(root, allNodes);

        foreach (var node in allNodes)
        {
            double w = node.Width;
            double h = node.Height;
            if (w < 1 || h < 1) continue;

            int colorIdx = GetTopBranch(node);
            double opacity = node.Depth == 0 ? 0.35 : 0.5 + node.Depth * 0.1;
            opacity = Math.Min(opacity, 0.85);
            var fill = G.Brush(G.Palette[colorIdx % G.Palette.Length], opacity);
            G.AddRect(canvas, node.X0, node.Y0, w, h, fill, 1);

            // Border
            var border = new WinShapes.Rectangle
            {
                Width = w, Height = h,
                Stroke = G.Gray(255, 180), StrokeThickness = 0.5,
            };
            Canvas.SetLeft(border, node.X0);
            Canvas.SetTop(border, node.Y0);
            canvas.Children.Add(border);

            // Label
            if (w > 50 && h > 16)
            {
                string label = node.Data.Name;
                double maxChars = w / 7;
                if (label.Length > maxChars) label = label[..(int)maxChars] + "..";
                G.AddText(canvas, node.X0 + 4, node.Y0 + 3, label, 9, G.Gray(20));
            }
            if (w > 50 && h > 30)
            {
                string valLabel = node.Value >= 1000
                    ? $"${node.Value / 1000:0.#}M"
                    : $"${node.Value:0}k";
                G.AddText(canvas, node.X0 + 4, node.Y0 + 16, valLabel, 8, G.Gray(60));
            }
        }

        return canvas;
    }

    static int GetTopBranch(PartitionNode<BudgetNode> node)
    {
        if (node.Parent == null) return 0;
        var current = node;
        while (current.Parent != null && current.Parent.Parent != null)
            current = current.Parent;
        return current.Parent == null ? 0 : current.Parent.Children.IndexOf(current);
    }

    static void CollectPartition(PartitionNode<BudgetNode> node, List<PartitionNode<BudgetNode>> list)
    {
        list.Add(node);
        foreach (var child in node.Children) CollectPartition(child, list);
    }
}

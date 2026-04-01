using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public sealed class ClusterDendrogramSample : GallerySample
{
    public override string Title => "Cluster Dendrogram";
    public override string Description =>
        "A cluster (dendrogram) layout showing animal taxonomy. All leaf nodes are " +
        "aligned at the same depth, making it easy to compare lineages. Uses ClusterLayout.";
    public override string Category => "Hierarchies";

    public override string SourceCode => """
        var layout = ClusterLayout.Create<TaxNode>().Size(660, 440);
        var root = layout.Hierarchy(data, n => n.Children);
        layout.Layout(root);

        foreach (var node in nodes)
            foreach (var child in node.Children)
            {
                double my = (node.Y + child.Y) / 2;
                var pb = new PathBuilder(3);
                pb.MoveTo(node.X, node.Y);
                pb.BezierCurveTo(node.X, my, child.X, my, child.X, child.Y);
                canvas.Children.Add(G.MakePath(pb.ToString(), linkStroke, null, 1.2));
            }
        foreach (var node in nodes)
        {
            bool isLeaf = node.Children.Count == 0;
            G.AddEllipse(canvas, node.X, node.Y, isLeaf ? 3.5 : 4.5, fill);
        }
        """;

    record TaxNode(string Name, TaxNode[]? Children = null);

    public override FrameworkElement Render()
    {
        const double W = 700, H = 500;
        var canvas = new Canvas { Width = W, Height = H };

        // Taxonomy hierarchy
        var data = new TaxNode("Animalia", [
            new("Chordata", [
                new("Mammalia", [
                    new("Primates", [
                        new("Human"),
                        new("Chimpanzee"),
                        new("Gorilla"),
                    ]),
                    new("Carnivora", [
                        new("Lion"),
                        new("Wolf"),
                        new("Bear"),
                    ]),
                    new("Cetacea", [
                        new("Dolphin"),
                        new("Blue Whale"),
                    ]),
                ]),
                new("Aves", [
                    new("Passeriformes", [
                        new("Sparrow"),
                        new("Robin"),
                    ]),
                    new("Accipitriformes", [
                        new("Eagle"),
                        new("Hawk"),
                    ]),
                ]),
            ]),
            new("Arthropoda", [
                new("Insecta", [
                    new("Coleoptera", [
                        new("Ladybug"),
                        new("Beetle"),
                    ]),
                    new("Lepidoptera", [
                        new("Monarch"),
                        new("Swallowtail"),
                    ]),
                ]),
            ]),
        ]);

        var layout = ClusterLayout.Create<TaxNode>().Size(660, 440);
        var root = layout.Hierarchy(data, n => n.Children);
        layout.Layout(root);

        // Collect nodes
        var nodes = new List<TreeNode<TaxNode>>();
        Collect(root, nodes);

        // Draw links as step paths (horizontal then vertical)
        var linkStroke = G.Gray(190);
        foreach (var node in nodes)
        {
            foreach (var child in node.Children)
            {
                double my = (node.Y + child.Y) / 2;
                var pb = new PathBuilder(3);
                pb.MoveTo(node.X, node.Y);
                pb.BezierCurveTo(node.X, my, child.X, my, child.X, child.Y);
                var path = G.MakePath(pb.ToString(), linkStroke, null, 1.2);
                canvas.Children.Add(path);
            }
        }

        // Draw nodes
        int colorIdx = 0;
        foreach (var node in nodes)
        {
            bool isLeaf = node.Children.Count == 0;
            double r = isLeaf ? 3.5 : 4.5;

            // Color by top-level branch
            int branchColor = GetBranchColor(node);
            var fill = isLeaf
                ? G.Brush(G.Palette[branchColor % G.Palette.Length])
                : G.Brush("#666666");
            G.AddEllipse(canvas, node.X, node.Y, r, fill);

            // Labels
            if (isLeaf)
            {
                G.AddText(canvas, node.X + 7, node.Y - 7, node.Data.Name, 8.5, G.Gray(50));
            }
            else if (node.Parent == null)
            {
                G.AddText(canvas, node.X - 4, node.Y - 18, node.Data.Name, 11,
                    G.Brush("#222222"), TextAlignment.Center);
            }
            else
            {
                G.AddText(canvas, node.X - 4, node.Y - 16, node.Data.Name, 9, G.Gray(80));
            }
        }

        return canvas;
    }

    static int GetBranchColor(TreeNode<TaxNode> node)
    {
        var current = node;
        while (current.Parent != null && current.Parent.Parent != null)
            current = current.Parent;
        if (current.Parent == null) return 0;
        return current.Parent.Children.IndexOf(current);
    }

    static void Collect(TreeNode<TaxNode> node, List<TreeNode<TaxNode>> list)
    {
        list.Add(node);
        foreach (var child in node.Children) Collect(child, list);
    }
}

using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public sealed class CirclePackingSample : GallerySample
{
    public override string Title => "Circle Packing";
    public override string Description =>
        "A circle packing visualization of an organization hierarchy. Nested circles " +
        "show containment, with leaf circle sizes proportional to team headcount. Uses PackLayout.";
    public override string Category => "Hierarchies";

    public override string SourceCode => """
        var pack = PackLayout.Create<OrgNode>().Size(230).SetPadding(4);
        var root = pack.Layout(data, n => n.Children, n => n.Value);

        foreach (var node in allNodes)
        {
            double nx = cx + node.X, ny = cy + node.Y, r = node.R;
            bool isLeaf = node.Children.Count == 0;
            if (isLeaf)
            {
                var fill = G.Brush(G.Palette[colorIdx % G.Palette.Length], 0.6);
                G.AddEllipse(canvas, nx, ny, r, fill, stroke, 1);
            }
            else
            {
                G.AddEllipse(canvas, nx, ny, r, G.Gray(200, alpha),
                    G.Gray(140), node.Depth == 0 ? 2 : 1);
            }
        }
        """;

    record OrgNode(string Name, double Value = 0, OrgNode[]? Children = null);

    public override FrameworkElement Render()
    {
        const double W = 700, H = 500;
        double cx = W / 2, cy = H / 2;
        var canvas = new Canvas { Width = W, Height = H };

        // Organization hierarchy with headcount values
        var data = new OrgNode("Company", 0, [
            new("Engineering", 0, [
                new("Frontend", 0, [
                    new("React Team", 12),
                    new("Design System", 6),
                    new("Mobile", 8),
                ]),
                new("Backend", 0, [
                    new("APIs", 10),
                    new("Data Platform", 14),
                    new("Infrastructure", 9),
                ]),
                new("QA", 0, [
                    new("Automation", 5),
                    new("Manual", 4),
                ]),
            ]),
            new("Product", 0, [
                new("PM Team", 8),
                new("UX Research", 5),
                new("Analytics", 6),
            ]),
            new("Operations", 0, [
                new("HR", 4),
                new("Finance", 5),
                new("Legal", 3),
                new("Facilities", 3),
            ]),
        ]);

        var pack = PackLayout.Create<OrgNode>().Size(230).SetPadding(4);
        var root = pack.Layout(data, n => n.Children, n => n.Value);

        // Collect all nodes and sort by depth (draw parents first)
        var allNodes = new List<PackNode<OrgNode>>();
        CollectPack(root, allNodes);
        allNodes.Sort((a, b) => a.Depth.CompareTo(b.Depth));

        foreach (var node in allNodes)
        {
            double nx = cx + node.X;
            double ny = cy + node.Y;
            double r = node.R;

            bool isLeaf = node.Children.Count == 0;

            if (isLeaf)
            {
                // Leaf: filled circle
                int colorIdx = GetBranchIndex(node);
                var fill = G.Brush(G.Palette[colorIdx % G.Palette.Length], 0.6);
                var stroke = G.Brush(G.Palette[colorIdx % G.Palette.Length], 0.9);
                G.AddEllipse(canvas, nx, ny, r, fill, stroke, 1);

                // Label if circle is large enough
                if (r > 18)
                {
                    string label = node.Data.Name;
                    double maxChars = r * 2 / 6;
                    if (label.Length > maxChars) label = label[..(int)maxChars] + "..";
                    G.AddText(canvas, nx - r + 4, ny - 6, label, 8, G.Gray(30),
                        TextAlignment.Center, r * 2 - 8);
                }
            }
            else
            {
                // Internal: outlined circle
                byte alpha = node.Depth == 0 ? (byte)40 : (byte)25;
                var fill = G.Gray(200, alpha);
                var stroke = G.Gray(140);
                G.AddEllipse(canvas, nx, ny, r, fill, stroke, node.Depth == 0 ? 2 : 1);
            }
        }

        // Title
        G.AddText(canvas, 10, 5, "Organization Headcount", 13, G.Brush("#333333"));

        return canvas;
    }

    static int GetBranchIndex(PackNode<OrgNode> node)
    {
        var current = node;
        while (current.Parent != null && current.Parent.Parent != null)
            current = current.Parent;
        if (current.Parent == null) return 0;
        return current.Parent.Children.IndexOf(current);
    }

    static void CollectPack(PackNode<OrgNode> node, List<PackNode<OrgNode>> list)
    {
        list.Add(node);
        foreach (var child in node.Children) CollectPack(child, list);
    }
}

using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public sealed class TidyTreeSample : GallerySample
{
    public override string Title => "Tidy Tree";
    public override string Description =>
        "A tidy tree layout (Reingold-Tilford) of a file system hierarchy three levels deep. " +
        "Uses TreeLayout to compute node positions, then draws curved bezier links, circular nodes, and text labels.";
    public override string Category => "Hierarchies";

    public override string SourceCode => """
        var layout = TreeLayout.Create<FsNode>().Size(660, 440);
        var root = layout.Hierarchy(data, n => n.Children);
        layout.Layout(root);

        foreach (var node in nodes)
            foreach (var child in node.Children)
            {
                double my = (node.Y + child.Y) / 2;
                var pb = new PathBuilder(3);
                pb.MoveTo(node.X, node.Y);
                pb.BezierCurveTo(node.X, my, child.X, my, child.X, child.Y);
                canvas.Children.Add(G.MakePath(pb.ToString(), linkStroke, null, 1.5));
            }
        foreach (var node in nodes)
        {
            bool isLeaf = node.Children.Count == 0;
            G.AddEllipse(canvas, node.X, node.Y, isLeaf ? 4 : 5, fill, stroke);
        }
        """;

    record FsNode(string Name, FsNode[]? Children = null);

    public override FrameworkElement Render()
    {
        const double W = 700, H = 500;
        var canvas = new Canvas { Width = W, Height = H };

        // File system hierarchy data
        var data = new FsNode("project", [
            new("src", [
                new("components", [
                    new("App.tsx"),
                    new("Header.tsx"),
                    new("Sidebar.tsx"),
                    new("Footer.tsx"),
                ]),
                new("utils", [
                    new("format.ts"),
                    new("api.ts"),
                    new("hooks.ts"),
                ]),
                new("styles", [
                    new("main.css"),
                    new("theme.css"),
                ]),
            ]),
            new("public", [
                new("index.html"),
                new("favicon.ico"),
            ]),
            new("config", [
                new("tsconfig.json"),
                new("vite.config.ts"),
                new("package.json"),
            ]),
        ]);

        var layout = TreeLayout.Create<FsNode>().Size(660, 440);
        var root = layout.Hierarchy(data, n => n.Children);
        layout.Layout(root);

        // Collect all nodes
        var nodes = new List<TreeNode<FsNode>>();
        Collect(root, nodes);

        // Draw links as vertical bezier curves
        var linkStroke = G.Gray(180);
        foreach (var node in nodes)
        {
            foreach (var child in node.Children)
            {
                double my = (node.Y + child.Y) / 2;
                var pb = new PathBuilder(3);
                pb.MoveTo(node.X, node.Y);
                pb.BezierCurveTo(node.X, my, child.X, my, child.X, child.Y);
                var path = G.MakePath(pb.ToString(), linkStroke, null, 1.5);
                canvas.Children.Add(path);
            }
        }

        // Draw nodes
        foreach (var node in nodes)
        {
            bool isLeaf = node.Children.Count == 0;
            double r = isLeaf ? 4 : 5;
            var fill = isLeaf ? G.Brush(G.Palette[0]) : G.Brush("#555555");
            var stroke = isLeaf ? null : G.Gray(80);
            G.AddEllipse(canvas, node.X, node.Y, r, fill, stroke, 1.5);

            // Label
            if (isLeaf)
            {
                G.AddText(canvas, node.X + 8, node.Y - 7, node.Data.Name, 9, G.Gray(60));
            }
            else
            {
                G.AddText(canvas, node.X - 4, node.Y - 18, node.Data.Name, 10,
                    G.Brush("#333333"), TextAlignment.Center);
            }
        }

        return canvas;
    }

    static void Collect(TreeNode<FsNode> node, List<TreeNode<FsNode>> list)
    {
        list.Add(node);
        foreach (var child in node.Children) Collect(child, list);
    }
}

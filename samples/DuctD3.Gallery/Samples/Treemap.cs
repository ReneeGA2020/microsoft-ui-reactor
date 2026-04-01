using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public sealed class TreemapSample : GallerySample
{
    public override string Title => "Treemap";
    public override string Description =>
        "A treemap showing file sizes in a software project. Each leaf rectangle is " +
        "proportional to the file size. Uses TreemapLayout with squarify tiling.";
    public override string Category => "Hierarchies";

    public override string SourceCode => """
        var treemap = TreemapLayout.Create<FileNode>()
            .Size(W, H).SetPadding(2).SetPaddingInner(2);
        var root = treemap.Hierarchy(data, n => n.Children, n => n.Size);
        treemap.Layout(root);

        foreach (var leaf in root.Leaves())
        {
            double w = leaf.Width, h = leaf.Height;
            if (w < 1 || h < 1) continue;
            int colorIdx = GetTopFolderIndex(leaf, topFolders);
            var fill = G.Brush(G.Palette[colorIdx % G.Palette.Length], 0.75);
            G.AddRect(canvas, leaf.X0, leaf.Y0, w, h, fill, 2);
            if (w > 40 && h > 16)
                G.AddText(canvas, leaf.X0 + 4, leaf.Y0 + 3, label, 9, G.Gray(30));
        }
        """;

    record FileNode(string Name, double Size = 0, FileNode[]? Children = null);

    public override FrameworkElement Render()
    {
        const double W = 700, H = 500;
        var canvas = new Canvas { Width = W, Height = H };

        // Project file hierarchy with sizes in KB
        var data = new FileNode("project", 0, [
            new("src", 0, [
                new("App.tsx", 12),
                new("index.tsx", 3),
                new("Router.tsx", 8),
                new("components", 0, [
                    new("Dashboard.tsx", 28),
                    new("Header.tsx", 15),
                    new("Sidebar.tsx", 22),
                    new("DataGrid.tsx", 45),
                    new("Chart.tsx", 38),
                    new("Modal.tsx", 18),
                ]),
                new("services", 0, [
                    new("api.ts", 32),
                    new("auth.ts", 24),
                    new("storage.ts", 16),
                ]),
                new("utils", 0, [
                    new("format.ts", 10),
                    new("validate.ts", 14),
                    new("helpers.ts", 8),
                ]),
            ]),
            new("tests", 0, [
                new("App.test.tsx", 18),
                new("Dashboard.test.tsx", 22),
                new("DataGrid.test.tsx", 30),
                new("api.test.ts", 20),
                new("auth.test.ts", 15),
            ]),
            new("config", 0, [
                new("webpack.config.js", 12),
                new("tsconfig.json", 4),
                new("jest.config.js", 6),
                new("package.json", 3),
            ]),
        ]);

        var treemap = TreemapLayout.Create<FileNode>()
            .Size(W, H)
            .SetPadding(2)
            .SetPaddingInner(2);
        var root = treemap.Hierarchy(data, n => n.Children, n => n.Size);
        treemap.Layout(root);

        // Assign colors by top-level folder
        var topFolders = root.Children.Select(c => c.Data.Name).ToList();

        // Draw leaf rectangles
        foreach (var leaf in root.Leaves())
        {
            double w = leaf.Width;
            double h = leaf.Height;
            if (w < 1 || h < 1) continue;

            int colorIdx = GetTopFolderIndex(leaf, topFolders);
            var fill = G.Brush(G.Palette[colorIdx % G.Palette.Length], 0.75);
            G.AddRect(canvas, leaf.X0, leaf.Y0, w, h, fill, 2);

            // Label if there is enough room
            if (w > 40 && h > 16)
            {
                string label = leaf.Data.Name;
                if (label.Length > (int)(w / 6)) label = label[..(int)(w / 6)] + "..";
                G.AddText(canvas, leaf.X0 + 4, leaf.Y0 + 3, label, 9, G.Gray(30));
            }
            if (w > 40 && h > 30)
            {
                G.AddText(canvas, leaf.X0 + 4, leaf.Y0 + 15,
                    $"{leaf.Data.Size} KB", 8, G.Gray(80));
            }
        }

        // Draw top-level folder borders and labels
        foreach (var folder in root.Children)
        {
            double fw = folder.Width;
            double fh = folder.Height;
            if (fw < 1 || fh < 1) continue;

            var border = new WinShapes.Rectangle
            {
                Width = fw, Height = fh,
                Stroke = G.Gray(100), StrokeThickness = 1.5,
                Fill = null, RadiusX = 2, RadiusY = 2,
            };
            Canvas.SetLeft(border, folder.X0);
            Canvas.SetTop(border, folder.Y0);
            canvas.Children.Add(border);
        }

        return canvas;
    }

    static int GetTopFolderIndex(TreemapNode<FileNode> node, List<string> topFolders)
    {
        var current = node;
        while (current.Parent != null && current.Parent.Parent != null)
            current = current.Parent;
        return Math.Max(0, topFolders.IndexOf(current.Data.Name));
    }
}

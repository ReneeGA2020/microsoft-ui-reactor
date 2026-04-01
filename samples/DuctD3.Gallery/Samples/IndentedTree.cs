using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public sealed class IndentedTreeSample : GallerySample
{
    public override string Title => "Indented Tree";
    public override string Description =>
        "An indented tree showing a hierarchical list built from flat tabular data. " +
        "Uses Stratify to construct the tree from id/parentId pairs, then renders rows " +
        "with indentation, expand/collapse indicators, and alternating background stripes.";
    public override string Category => "Hierarchies";

    public override string SourceCode => """
        var stratify = Stratify.Create<Row>()
            .SetId(r => r.Id)
            .SetParentId(r => r.ParentId);
        var root = stratify.Build(flatData);

        for (int i = 0; i < rows.Count; i++)
        {
            var node = rows[i];
            double y = startY + (i + 1) * rowH;
            double indent = startX + node.Depth * indentPx;
            bool hasChildren = node.Children.Count > 0;
            if (hasChildren)
            {
                var pb = new PathBuilder(1);
                pb.MoveTo(tx, ty); pb.LineTo(tx + 8, ty);
                pb.LineTo(tx + 4, ty + 6); pb.ClosePath();
                canvas.Children.Add(G.MakePath(pb.ToString(), null, G.Gray(120)));
            }
            G.AddText(canvas, indent, y + 3, node.Data.Name, 10, nameColor);
        }
        """;

    record Row(string Id, string? ParentId, string Name, string Type);

    public override FrameworkElement Render()
    {
        const double W = 700, H = 500;
        var canvas = new Canvas { Width = W, Height = H };

        // Flat tabular data — a file system in id/parentId form
        var flatData = new Row[]
        {
            new("root",       null,       "my-app",         "folder"),
            new("src",        "root",     "src",            "folder"),
            new("comp",       "src",      "components",     "folder"),
            new("app",        "comp",     "App.tsx",        "file"),
            new("header",     "comp",     "Header.tsx",     "file"),
            new("sidebar",    "comp",     "Sidebar.tsx",    "file"),
            new("footer",     "comp",     "Footer.tsx",     "file"),
            new("hooks",      "src",      "hooks",          "folder"),
            new("useAuth",    "hooks",    "useAuth.ts",     "file"),
            new("useFetch",   "hooks",    "useFetch.ts",    "file"),
            new("useTheme",   "hooks",    "useTheme.ts",    "file"),
            new("styles",     "src",      "styles",         "folder"),
            new("global",     "styles",   "global.css",     "file"),
            new("theme",      "styles",   "theme.css",      "file"),
            new("index",      "src",      "index.tsx",      "file"),
            new("pub",        "root",     "public",         "folder"),
            new("html",       "pub",      "index.html",     "file"),
            new("favicon",    "pub",      "favicon.ico",    "file"),
            new("manifest",   "pub",      "manifest.json",  "file"),
            new("cfg",        "root",     "config",         "folder"),
            new("tsconfig",   "cfg",      "tsconfig.json",  "file"),
            new("eslint",     "cfg",      ".eslintrc.js",   "file"),
            new("prettier",   "cfg",      ".prettierrc",    "file"),
            new("pkg",        "root",     "package.json",   "file"),
            new("readme",     "root",     "README.md",      "file"),
            new("gitignore",  "root",     ".gitignore",     "file"),
        };

        // Build hierarchy from flat data using Stratify
        var stratify = Stratify.Create<Row>()
            .SetId(r => r.Id)
            .SetParentId(r => r.ParentId);
        var root = stratify.Build(flatData);

        // Flatten tree in pre-order for rendering
        var rows = new List<TreeNode<Row>>();
        FlattenTree(root, rows);

        // Rendering constants
        const double rowH = 20;
        const double indentPx = 22;
        const double startX = 16;
        const double startY = 10;
        const double nameCol = 0;
        const double typeCol = 360;

        // Header
        G.AddRect(canvas, 0, startY, W, rowH, G.Gray(230));
        G.AddText(canvas, startX, startY + 3, "Name", 10, G.Brush("#333333"));
        G.AddText(canvas, typeCol, startY + 3, "Type", 10, G.Brush("#333333"));

        // Rows
        for (int i = 0; i < rows.Count; i++)
        {
            var node = rows[i];
            double y = startY + (i + 1) * rowH;

            if (y + rowH > H) break; // don't overflow canvas

            // Alternating stripe
            if (i % 2 == 0)
                G.AddRect(canvas, 0, y, W, rowH, G.Gray(248));

            double indent = startX + node.Depth * indentPx;
            bool hasChildren = node.Children.Count > 0;

            // Expand/collapse indicator
            if (hasChildren)
            {
                // Draw a small filled triangle (pointing down)
                var pb = new PathBuilder(1);
                double tx = indent - 12;
                double ty = y + 6;
                pb.MoveTo(tx, ty);
                pb.LineTo(tx + 8, ty);
                pb.LineTo(tx + 4, ty + 6);
                pb.ClosePath();
                var tri = G.MakePath(pb.ToString(), null, G.Gray(120), 0);
                canvas.Children.Add(tri);
            }
            else
            {
                // Draw a small circle for leaf
                G.AddEllipse(canvas, indent - 8, y + 9, 2.5, G.Gray(180));
            }

            // Folder icon or file indicator
            bool isFolder = node.Data.Type == "folder";
            var nameColor = isFolder ? G.Brush(G.Palette[0]) : G.Gray(50);
            string displayName = node.Data.Name;
            G.AddText(canvas, indent, y + 3, displayName, 10, nameColor);

            // Type column
            G.AddText(canvas, typeCol, y + 3, node.Data.Type, 9, G.Gray(120));

            // Subtle horizontal rule
            G.AddLine(canvas, 0, y + rowH, W, y + rowH, G.Gray(235), 0.5);
        }

        return canvas;
    }

    static void FlattenTree(TreeNode<Row> node, List<TreeNode<Row>> list)
    {
        list.Add(node);
        foreach (var child in node.Children) FlattenTree(child, list);
    }
}

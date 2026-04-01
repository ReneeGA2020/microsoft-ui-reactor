using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using static Duct.D3.Charts.D3;
using static Duct.UI;

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
        D3Canvas(W, H,
            [..rows.SelectMany((node, i) => {
                 double indent = startX + node.Depth * indentPx;
                 return new Element[] {
                     hasChildren ? D3Path(pb.ToString(), null, Gray(120), 0)
                                 : D3Circle(indent - 8, y + 9, 2.5) with { Fill = Gray(180) },
                     D3Text(indent, y + 3, node.Data.Name, 10, nameColor),
                 };
             }),
            ]
        )
        """;

    record Row(string Id, string? ParentId, string Name, string Type);

    public override Element Render()
    {
        const double W = 700, H = 500;

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

        var rows = root.Descendants().ToList();

        // Rendering constants
        const double rowH = 20;
        const double indentPx = 22;
        const double startX = 16;
        const double startY = 10;
        const double typeCol = 360;

        return D3Canvas(W, H,
        [
            // Header
            D3Rect(0, startY, W, rowH) with { Fill = Gray(230) },
            D3Text(startX, startY + 3, "Name", 10, Brush("#333333")),
            D3Text(typeCol, startY + 3, "Type", 10, Brush("#333333")),

            // Rows
            .. rows.Select((node, i) => (node, i, y: startY + (i + 1) * rowH))
                .TakeWhile(t => t.y + rowH <= H)
                .SelectMany(t =>
                {
                    var (node, i, y) = t;
                    double indent = startX + node.Depth * indentPx;
                    bool hasChildren = node.Children.Count > 0;
                    bool isFolder = node.Data.Type == "folder";
                    var nameColor = isFolder ? Brush(Palette[0]) : Gray(50);

                    var indicator = hasChildren
                        ? D3Path(new PathBuilder(1)
                            .MoveTo(indent - 12, y + 6)
                            .LineTo(indent - 4, y + 6)
                            .LineTo(indent - 8, y + 12)
                            .ClosePath().ToString(), null, Gray(120), 0)
                        : (Element)(D3Circle(indent - 8, y + 9, r: 2.5) with { Fill = Gray(180) });

                    return (Element[])
                    [
                        .. (i % 2 == 0 ? new[] { D3Rect(0, y, W, rowH) with { Fill = Gray(248) } } : Array.Empty<Element>()),
                        indicator,
                        D3Text(indent, y + 3, node.Data.Name, 10, nameColor),
                        D3Text(typeCol, y + 3, node.Data.Type, 9, Gray(120)),
                        D3Line(0, y + rowH, W, y + rowH) with { Stroke = Gray(235), StrokeThickness = 0.5 },
                    ];
                }),
        ]);
    }

}

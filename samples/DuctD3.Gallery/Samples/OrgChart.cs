using Duct;
using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using static Duct.D3.Charts.D3;
using static Duct.UI;

namespace DuctD3.Gallery;

/// <summary>
/// An org chart where each node is a person card with avatar circle,
/// name, and role — laid out by D3's Reingold-Tilford tree algorithm.
/// All cards are Duct elements positioned inside a D3Canvas.
/// </summary>
public sealed class OrgChartSample : GallerySample
{
    public override string Title => "Org Chart";
    public override string Description =>
        "An organizational chart using D3 tree layout where each node is a rich person card " +
        "with avatar, name, and role. Shows mixing data-driven layout with Duct controls.";
    public override string Category => "Controls";

    public override string SourceCode => """
        var layout = TreeLayout.Create<Person>().Size(W - 100, H - 120);
        var root = layout.Hierarchy(ceo, p => p.Reports);
        layout.Layout(root);

        return D3Canvas(W, H,
            [.. nodes.SelectMany(n => n.Children.Select(c =>
                D3Link(n.X, n.Y, c.X, c.Y, stroke: linkBrush))),
             .. nodes.Select(n =>
                PersonCard(n.Data, depth).Canvas(nx - cardW/2, ny - cardH/2)),
            ]);

        Element PersonCard(person, depth) =>
            Border(VStack(3,
                AvatarCircle(person.Initials, Palette[depth]),
                Text(person.Name).SemiBold(),
                Text(person.Role),
            )) with { CornerRadius = 8, ... };
        """;

    record Person(string Name, string Role, string Initials, Person[]? Reports = null);

    const double W = 1000, H = 600;
    const double PadX = 50, PadY = 60;
    const double CardW = 100, CardH = 88;

    public override Element Render()
    {
        var ceo = new Person("Alex Chen", "CEO", "AC", [
            new("Jordan Lee", "VP Engineering", "JL", [
                new("Sam Park", "Tech Lead", "SP", [
                    new("Riley Kim", "Developer", "RK"),
                    new("Morgan Yu", "Developer", "MY"),
                ]),
                new("Casey Diaz", "Tech Lead", "CD", [
                    new("Quinn Brown", "Developer", "QB"),
                ]),
            ]),
            new("Taylor Swift", "VP Product", "TS", [
                new("Dana White", "PM", "DW"),
                new("Jamie Fox", "Designer", "JF"),
            ]),
            new("Robin Gray", "VP Sales", "RG", [
                new("Avery Stone", "Account Exec", "AS"),
                new("Blake Reed", "Account Exec", "BR"),
            ]),
        ]);

        var layout = TreeLayout.Create<Person>().Size(W - PadX * 2, H - PadY * 2);
        var root = layout.Hierarchy(ceo, p => p.Reports);
        layout.Layout(root);

        var nodes = root.Descendants().ToList();
        var linkBrush = Gray(100, alpha: 80);

        return D3Canvas(W, H,
        [
            // Bezier links
            .. nodes.SelectMany(n =>
                n.Children.Select(c =>
                    (Element)D3Link(PadX + n.X, PadY + n.Y, PadX + c.X, PadY + c.Y,
                        stroke: linkBrush, strokeWidth: 1.5))),

            // Person cards at each node
            .. nodes.Select(n =>
            {
                int depth = NodeDepth(n);
                double nx = PadX + n.X, ny = PadY + n.Y;
                return PersonCard(n.Data, depth)
                    .Canvas(nx - CardW / 2, ny - CardH / 2);
            }),
        ]);
    }

    static Element PersonCard(Person person, int depth)
    {
        var color = Brush(Palette[depth % Palette.Length]);
        var roleBrush = Gray(100, alpha: 180);

        return (Border(
            VStack(3,
                AvatarCircle(person.Initials, color),
                (Text(person.Name) with { FontSize = 10 })
                    .SemiBold().HAlign(HorizontalAlignment.Center).MaxWidth(CardW - 12),
                (Text(person.Role) with { FontSize = 9 })
                    .Foreground(roleBrush).HAlign(HorizontalAlignment.Center).MaxWidth(CardW - 12)
            ).HAlign(HorizontalAlignment.Center)
        ) with
        {
            CornerRadius = 8,
            BorderBrush = Gray(100, alpha: 60),
            BorderThickness = 1,
            Background = Brush("#fcfcfd"),
            Padding = new Thickness(6),
        }).Size(CardW, CardH);
    }

    static Element AvatarCircle(string initials, Microsoft.UI.Xaml.Media.Brush color) =>
        (Border(
            (Text(initials) with { FontSize = 11 })
                .Bold().Foreground("#ffffff")
                .HAlign(HorizontalAlignment.Center)
                .VAlign(VerticalAlignment.Center)
        ) with
        {
            CornerRadius = 14,
            Background = color,
        }).Size(28, 28).HAlign(HorizontalAlignment.Center);

    static int NodeDepth<T>(TreeNode<T> node)
    {
        int d = 0;
        var p = node.Parent;
        while (p != null) { d++; p = p.Parent; }
        return d;
    }
}

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Layout;
using static Microsoft.UI.Reactor.Factories;
using Microsoft.UI.Xaml;

ReactorApp.Run<FlexLayoutApp>("Flex Layout", width: 700, height: 600
#if DEBUG
    , preview: true
#endif
);

// <snippet:flex-direction>
class FlexDirectionDemo : Component
{
    public override Element Render()
    {
        return VStack(16,
            SubHeading("Row (default)"),
            FlexRow(
                Text("A").Padding(12).Background("#e0e0ff"),
                Text("B").Padding(12).Background("#ffe0e0"),
                Text("C").Padding(12).Background("#e0ffe0")
            ) with { ColumnGap = 8 },

            SubHeading("Column"),
            FlexColumn(
                Text("A").Padding(12).Background("#e0e0ff"),
                Text("B").Padding(12).Background("#ffe0e0"),
                Text("C").Padding(12).Background("#e0ffe0")
            ) with { RowGap = 8 }
        ).Padding(24);
    }
}
// </snippet:flex-direction>

// <snippet:justify-align>
class JustifyAlignDemo : Component
{
    public override Element Render()
    {
        return VStack(16,
            SubHeading("JustifyContent: SpaceBetween"),
            FlexRow(
                Text("Left").Padding(8).Background("#e0e0ff"),
                Text("Center").Padding(8).Background("#ffe0e0"),
                Text("Right").Padding(8).Background("#e0ffe0")
            ) with { JustifyContent = FlexJustify.SpaceBetween },

            SubHeading("AlignItems: Center"),
            FlexRow(
                Text("Short").Padding(8).Background("#e0e0ff"),
                Text("Tall\nItem").Padding(8).Background("#ffe0e0"),
                Text("Med").Padding(8).Background("#e0ffe0")
            ) with {
                AlignItems = FlexAlign.Center,
                ColumnGap = 8
            }
        ).Padding(24).Height(300);
    }
}
// </snippet:justify-align>

// <snippet:wrap-gap>
class WrapGapDemo : Component
{
    public override Element Render()
    {
        var tags = new[] {
            "C#", "WinUI", "Reactor", ".NET", "XAML",
            "Flex", "Layout", "Desktop", "Native"
        };

        return VStack(12,
            SubHeading("Wrapping Tags"),
            FlexRow(
                tags.Select(tag =>
                    Text(tag)
                        .Padding(6, 12)
                        .Background("#e8e8e8")
                        .CornerRadius(12)
                ).ToArray()
            ) with {
                Wrap = FlexWrap.Wrap,
                ColumnGap = 8,
                RowGap = 8
            }
        ).Padding(24);
    }
}
// </snippet:wrap-gap>

// <snippet:grow-shrink>
class GrowShrinkDemo : Component
{
    public override Element Render()
    {
        return VStack(16,
            SubHeading("Grow: sidebar + content"),
            FlexRow(
                Text("Sidebar")
                    .Padding(16).Background("#e0e0ff")
                    .Flex(basis: 200, shrink: 0),
                Text("Main content area")
                    .Padding(16).Background("#f0f0f0")
                    .Flex(grow: 1)
            ) with { ColumnGap = 8 },

            SubHeading("Equal columns"),
            FlexRow(
                Text("Column 1").Padding(16).Background("#ffe0e0").Flex(grow: 1),
                Text("Column 2").Padding(16).Background("#e0ffe0").Flex(grow: 1),
                Text("Column 3").Padding(16).Background("#e0e0ff").Flex(grow: 1)
            ) with { ColumnGap = 8 }
        ).Padding(24);
    }
}
// </snippet:grow-shrink>

// <snippet:toolbar>
class ToolbarDemo : Component
{
    public override Element Render()
    {
        var (selected, setSelected) = UseState("Home");

        return VStack(0,
            FlexRow(
                Text("MyApp").Bold().Flex(shrink: 0),
                Empty().Flex(grow: 1),
                Button("Home", () => setSelected("Home")),
                Button("Settings", () => setSelected("Settings")),
                Button("About", () => setSelected("About"))
            ) with {
                AlignItems = FlexAlign.Center,
                ColumnGap = 8,
                FlexPadding = new Thickness(16, 8, 16, 8)
            },
            Text($"Current page: {selected}")
                .Padding(24).FontSize(18)
        );
    }
}
// </snippet:toolbar>

// <snippet:flex-vs-stack>
class FlexVsStackDemo : Component
{
    public override Element Render()
    {
        return VStack(16,
            SubHeading("HStack (fixed spacing)"),
            HStack(8,
                Button("A"), Button("B"), Button("C")
            ),

            SubHeading("FlexRow (justify + align)"),
            FlexRow(
                Button("A"), Button("B"), Button("C")
            ) with {
                JustifyContent = FlexJustify.SpaceEvenly,
                AlignItems = FlexAlign.Center
            }
        ).Padding(24);
    }
}
// </snippet:flex-vs-stack>

class FlexLayoutApp : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(24,
                Heading("Flex Layout"),
                Component<FlexDirectionDemo>(),
                Component<JustifyAlignDemo>(),
                Component<WrapGapDemo>(),
                Component<GrowShrinkDemo>(),
                Component<ToolbarDemo>(),
                Component<FlexVsStackDemo>()
            ).Padding(24)
        );
    }
}

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;
using Microsoft.UI.Xaml;

ReactorApp.Run<LayoutApp>("Layout Demo", width: 700, height: 650
#if DEBUG
    , preview: true
#endif
);

// <snippet:vstack-hstack>
class StackDemo : Component
{
    public override Element Render()
    {
        return VStack(16,
            SubHeading("VStack and HStack"),
            VStack(4,
                Text("VStack: items top to bottom"),
                Text("Item A"), Text("Item B"), Text("Item C")
            ),
            HStack(8,
                Text("HStack:"),
                Button("One"), Button("Two"), Button("Three")
            )
        );
    }
}
// </snippet:vstack-hstack>

// <snippet:grid-layout>
class GridDemo : Component
{
    public override Element Render()
    {
        return VStack(8,
            SubHeading("Grid"),
            Grid(
                columns: ["120", "1*", "Auto"],
                rows: ["Auto", "Auto"],
                Text("Label").Bold().Grid(row: 0, column: 0),
                TextField("", _ => { }, placeholder: "Input...")
                    .Grid(row: 0, column: 1),
                Button("Go").Grid(row: 0, column: 2),
                Text("Status").Grid(row: 1, column: 0),
                Text("Ready").Foreground("#0078D4")
                    .Grid(row: 1, column: 1, columnSpan: 2)
            ).Height(80)
        );
    }
}
// </snippet:grid-layout>

// <snippet:scrollview-border>
class ScrollBorderDemo : Component
{
    public override Element Render()
    {
        return VStack(8,
            SubHeading("ScrollView and Border"),
            Border(
                ScrollView(
                    VStack(4,
                        ForEach(
                            Enumerable.Range(1, 20),
                            i => Text($"Scrollable item {i}"))
                    ).Padding(8)
                ).Height(120)
            ).CornerRadius(4).Background("#F5F5F5")
        );
    }
}
// </snippet:scrollview-border>

// <snippet:expander-canvas>
class ExpanderCanvasDemo : Component
{
    public override Element Render()
    {
        return VStack(12,
            SubHeading("Expander"),
            Expander("Click to expand", VStack(8,
                Text("Hidden content revealed!"),
                Text("Expanders are great for optional details.")
            )),
            SubHeading("Canvas"),
            Border(
                Canvas(
                    Text("Top-left").Set(c => {
                        Microsoft.UI.Xaml.Controls.Canvas.SetLeft((UIElement)c, 10);
                        Microsoft.UI.Xaml.Controls.Canvas.SetTop((UIElement)c, 10);
                    }),
                    Text("Center").Set(c => {
                        Microsoft.UI.Xaml.Controls.Canvas.SetLeft((UIElement)c, 120);
                        Microsoft.UI.Xaml.Controls.Canvas.SetTop((UIElement)c, 40);
                    })
                ).Height(90).Width(300)
            ).Background("#F5F5F5").CornerRadius(4)
        );
    }
}
// </snippet:expander-canvas>

// <snippet:responsive>
class ResponsiveDemo : Component
{
    public override Element Render()
    {
        var (wide, setWide) = UseState(true);

        var content = new Element[]
        {
            Border(Text("Panel A").Padding(16))
                .Background("#E3F2FD").CornerRadius(4),
            Border(Text("Panel B").Padding(16))
                .Background("#FFF3E0").CornerRadius(4),
            Border(Text("Panel C").Padding(16))
                .Background("#E8F5E9").CornerRadius(4),
        };

        return VStack(8,
            SubHeading("Responsive Layout"),
            HStack(8,
                Text("Simulate wide screen:"),
                ToggleSwitch(wide, setWide)
            ),
            If(wide,
                () => HStack(12, content),
                () => VStack(8, content))
        );
    }
}
// </snippet:responsive>

class LayoutApp : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(24,
                Heading("Layout Primitives"),
                Component<StackDemo>(),
                Component<GridDemo>(),
                Component<ScrollBorderDemo>(),
                Component<ExpanderCanvasDemo>(),
                Component<ResponsiveDemo>()
            ).Padding(24)
        );
    }
}

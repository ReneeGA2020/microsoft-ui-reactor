using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;
using static WinUIGalleryReactor.SamplePageHost;

namespace WinUIGalleryReactor;

class ScrollViewPage : Component
{
    public override Element Render()
    {
        var (itemCount, setItemCount) = UseState(20.0);

        return ScrollView(
            VStack(16,
                PageHeader("ScrollView", "A scrollable container for content that exceeds available space."),

                SampleCard("Vertical ScrollView",
                    Border(
                        ScrollView(
                            VStack(4,
                                Enumerable.Range(1, (int)itemCount)
                                    .Select(i => Border(TextBlock($"Item {i}").Padding(8))
                                        .Background(i % 2 == 0 ? Theme.SubtleFill : Theme.LayerFill))
                                    .ToArray()
                            )
                        )
                    ).Size(300, 200).WithBorder(Theme.CardStroke).CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft),
                    @"ScrollView(\n    VStack(4, items.Select(i =>\n        Border(TextBlock($""Item {i}"")).Padding(8)\n    ).ToArray())\n)",
                    OptionPanel(
                        TextBlock($"Item count: {(int)itemCount}"),
                        Slider(itemCount, 5, 50, setItemCount)
                    )),

                SampleCard("Horizontal ScrollView",
                    Border(
                        (ScrollView(
                            HStack(4,
                                Enumerable.Range(1, 15)
                                    .Select(i => Border(TextBlock($"  {i}  ").Center().Foreground("#FFFFFF"))
                                        .Background("#5B6ABF").Size(80, 60).CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft))
                                    .ToArray()
                            )
                        ) with { HorizontalScrollMode = Microsoft.UI.Xaml.Controls.ScrollMode.Enabled,
                                 VerticalScrollMode = Microsoft.UI.Xaml.Controls.ScrollMode.Disabled })
                    ).Height(80).Width(350).WithBorder(Theme.CardStroke).CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft),
                    @"(ScrollView(HStack(...)) with {\n    HorizontalScrollMode = ScrollMode.Enabled,\n    VerticalScrollMode = ScrollMode.Disabled\n})")
            ).Margin(36, 24, 36, 36)
        );
    }
}

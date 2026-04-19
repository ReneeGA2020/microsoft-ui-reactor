using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;
using static WinUIGalleryReactor.SamplePageHost;

namespace WinUIGalleryReactor;

class ListViewPage : Component
{
    public override Element Render()
    {
        var (mode, setMode) = UseState(0);
        var modes = new[] { "Single", "Multiple", "Extended", "None" };
        var items = new[] { "Apples", "Bananas", "Carrots", "Dates", "Eggplant", "Figs" };

        var selectionMode = mode switch
        {
            1 => ListViewSelectionMode.Multiple,
            2 => ListViewSelectionMode.Extended,
            3 => ListViewSelectionMode.None,
            _ => ListViewSelectionMode.Single
        };

        return ScrollView(
            VStack(16,
                PageHeader("ListView", "Displays items in a vertical scrolling list."),

                SampleCard("Basic ListView",
                    ListView(
                        items.Select(i => TextBlock(i) as Element).ToArray()
                    ).SelectionMode(selectionMode).Height(250),
                    @"ListView(\n    TextBlock(""Apples""), TextBlock(""Bananas""), ...\n).SelectionMode(ListViewSelectionMode.Multiple)",
                    OptionPanel(
                        TextBlock("Selection Mode"),
                        ComboBox(modes, mode, setMode)
                    )),

                SampleCard("Data-Driven ListView",
                    ListView(
                        items.ToList().AsReadOnly(),
                        s => s,
                        (s, i) => HStack(8,
                            Border(TextBlock($"{i + 1}").Center().Foreground("#FFFFFF"))
                                .Background(Theme.Accent).Size(28, 28).CornerRadius(14),
                            TextBlock(s)
                        )
                    ).Height(250),
                    @"ListView(\n    items, s => s,\n    (s, i) => HStack(8,\n        Border(TextBlock($""{i+1}"")).Size(28,28),\n        TextBlock(s)\n    )\n)")
            ).Margin(36, 24, 36, 36)
        );
    }
}

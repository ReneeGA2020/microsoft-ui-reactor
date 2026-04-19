using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;
using static WinUIGalleryReactor.SamplePageHost;

namespace WinUIGalleryReactor.ControlPages.BasicInput;

class RepeatButtonPage: Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);

        return ScrollView(VStack(16,
            PageHeader("RepeatButton", "A button that raises click events repeatedly while pressed and held."),

            SampleCard("Incrementing Counter",
                VStack(8,
                    HStack(8,
                        RepeatButton("-", () => setCount(count - 1)),
                        TextBlock($"{count}").ApplyStyle("SubtitleTextBlockStyle").VAlign(VerticalAlignment.Center),
                        RepeatButton("+", () => setCount(count + 1))),
                    TextBlock("Hold the button to repeat").Foreground(Theme.SecondaryText)),
                sourceCode: @"
HStack(8,
    RepeatButton(""-"", () => setCount(count - 1)),
    TextBlock($""{count}""),
    RepeatButton(""+"", () => setCount(count + 1)))
")
        ).Margin(36, 24, 36, 36));
    }
}

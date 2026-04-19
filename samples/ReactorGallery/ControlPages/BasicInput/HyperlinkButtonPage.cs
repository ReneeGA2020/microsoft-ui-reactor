using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;
using static WinUIGalleryReactor.SamplePageHost;

namespace WinUIGalleryReactor.ControlPages.BasicInput;

class HyperlinkButtonPage: Component
{
    public override Element Render()
    {
        var (clickCount, setClickCount) = UseState(0);

        return ScrollView(VStack(16,
            PageHeader("HyperlinkButton", "A button that appears as a hyperlink text and can navigate to a URI."),

            SampleCard("Navigate to URI",
                HyperlinkButton("Go to Microsoft", new Uri("https://www.microsoft.com")),
                sourceCode: @"
HyperlinkButton(""Go to Microsoft"", new Uri(""https://www.microsoft.com""))
"),

            SampleCard("HyperlinkButton with Click Handler",
                VStack(8,
                    HyperlinkButton("Click me", onClick: () => setClickCount(clickCount + 1)),
                    TextBlock($"Clicked {clickCount} times").Foreground(Theme.SecondaryText)),
                sourceCode: @"
HyperlinkButton(""Click me"", onClick: () => setClickCount(clickCount + 1))
")
        ).Margin(36, 24, 36, 36));
    }
}

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;
using static WinUIGalleryReactor.SamplePageHost;

namespace WinUIGalleryReactor.ControlPages.BasicInput;

class ButtonPage: Component
{
    public override Element Render()
    {
        var (basicOutput, setBasicOutput) = UseState("Ready");
        var (accentOutput, setAccentOutput) = UseState("Ready");

        return PageContent("Button",
            "A button that responds to user clicks and pointer input.",

            SampleCard("Basic Button",
                VStack(8,
                    Button("Click Me", () => setBasicOutput("Button clicked!")),
                    TextBlock(basicOutput).Foreground(Theme.SecondaryText)),
                sourceCode: @"Button(""Click Me"", () => setOutput(""Button clicked!""))"),

            SampleCard("Disabled Button",
                Button("Can't Click").Disabled(),
                sourceCode: @"Button(""Can't Click"").Disabled()"),

            SampleCard("Accent Style Button",
                VStack(8,
                    Button("Accent Button", () => setAccentOutput("Accent clicked!"))
                        .Set(b => b.Style = (Style)Application.Current.Resources["AccentButtonStyle"]),
                    TextBlock(accentOutput).Foreground(Theme.SecondaryText)),
                sourceCode: @"Button(""Accent Button"", () => setOutput(""Accent clicked!""))
    .Set(b => b.Style = (Style)Application.Current.Resources[""AccentButtonStyle""])")
        );
    }
}

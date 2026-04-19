using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;
using static WinUIGalleryReactor.SamplePageHost;

namespace WinUIGalleryReactor;

class ViewboxPage : Component
{
    public override Element Render()
    {
        var (size, setSize) = UseState(150.0);

        return ScrollView(
            VStack(16,
                PageHeader("Viewbox", "Scales its child content to fill available space."),

                SampleCard("Scaling Content",
                    Border(
                        Viewbox(
                            VStack(4,
                                TextBlock("Hello").Bold(),
                                TextBlock("Scaled content")
                            )
                        )
                    ).Size(size, size).Background(Theme.SubtleFill).CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft),
                    @"Viewbox(\n    VStack(4, TextBlock(""Hello"").Bold(), TextBlock(""Scaled""))\n)",
                    OptionPanel(
                        TextBlock($"Container size: {(int)size}px"),
                        Slider(size, 50, 300, setSize)
                    )),

                SampleCard("Viewbox Comparison",
                    HStack(16,
                        VStack(4,
                            TextBlock("100x100").ApplyStyle("CaptionTextBlockStyle"),
                            Border(Viewbox(TextBlock("ABC").Bold()))
                                .Size(100, 100).Background(Theme.SubtleFill).CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft)
                        ),
                        VStack(4,
                            TextBlock("150x80").ApplyStyle("CaptionTextBlockStyle"),
                            Border(Viewbox(TextBlock("ABC").Bold()))
                                .Size(150, 80).Background(Theme.SubtleFill).CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft)
                        ),
                        VStack(4,
                            TextBlock("60x150").ApplyStyle("CaptionTextBlockStyle"),
                            Border(Viewbox(TextBlock("ABC").Bold()))
                                .Size(60, 150).Background(Theme.SubtleFill).CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft)
                        )
                    ),
                    @"// Same content at different sizes:\nBorder(Viewbox(TextBlock(""ABC""))).Size(100, 100)\nBorder(Viewbox(TextBlock(""ABC""))).Size(150, 80)")
            ).Margin(36, 24, 36, 36)
        );
    }
}

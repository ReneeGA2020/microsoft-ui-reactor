using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;
using static WinUIGalleryReactor.SamplePageHost;

namespace WinUIGalleryReactor;

class RichTextBlockPage : Component
{
    public override Element Render()
    {
        var (fontSize, setFontSize) = UseState(14.0);

        return ScrollView(
            VStack(16,
                PageHeader("RichTextBlock", "Displays formatted, read-only rich text."),

                SampleCard("Basic RichText",
                    RichText("This is a simple rich text block displaying read-only content.").FontSize(fontSize),
                    @"RichText(""This is a simple rich text block..."")",
                    OptionPanel(
                        TextBlock("Font Size"),
                        Slider(fontSize, 10, 28, setFontSize)
                    )),

                SampleCard("Structured Rich Text",
                    RichText(new[]
                    {
                        Paragraph(Run("Bold introduction. ") with { IsBold = true }, Run("Followed by normal text.")),
                        Paragraph(Run("Italic emphasis ") with { IsItalic = true }, Run("mixed with "), Run("bold") with { IsBold = true }, Run(".")),
                        Paragraph(Run("A third paragraph with different content to show block-level formatting."))
                    }),
                    @"RichText(new[] {\n    Paragraph(Run(""Bold"") with { IsBold = true }, Run(""normal"")),\n    Paragraph(Run(""Italic"") with { IsItalic = true })\n})"),

                SampleCard("Simple RichText String",
                    VStack(8,
                        RichText("Line one of text content."),
                        RichText("Line two with separate blocks.")
                    ),
                    @"RichText(""Line one"")\nRichText(""Line two"")")
            ).Margin(36, 24, 36, 36)
        );
    }
}

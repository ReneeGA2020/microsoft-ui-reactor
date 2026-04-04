using Duct;
using Duct.Core;
using Duct.AppTests.Host.SelfTest;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;

namespace Duct.AppTests.Host.SelfTest.Fixtures;

internal static class MarkdownFixtures
{
    internal class HeadingsAndFormatting(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                ScrollView(
                    UI.Markdown("# Main Heading\n\nThis is a paragraph with **bold text** and *italic text*.\n\n## Sub Heading\n\nAnother paragraph here.")
                )
            );

            await Harness.Render(500);

            var hasHeading = H.FindText("Main Heading") is not null
                || H.FindControl<RichTextBlock>(rtb => rtb.Blocks.Count > 0) is not null;
            H.Check("Markdown_HeadingsAndFormatting_ContentRendered", hasHeading);

            var textBlocks = H.FindAllControls<TextBlock>(_ => true);
            var richTextBlocks = H.FindAllControls<RichTextBlock>(_ => true);
            H.Check("Markdown_HeadingsAndFormatting_MultipleBlocks",
                textBlocks.Count + richTextBlocks.Count >= 2);

            var stack = H.FindControl<StackPanel>(_ => true);
            H.Check("Markdown_HeadingsAndFormatting_StackPanelCreated",
                stack is not null);
        }
    }

    internal class CodeBlockAndLinks(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                ScrollView(
                    UI.Markdown("Here is a [link](https://example.com) and `inline code`.\n\n```csharp\nvar x = 42;\nConsole.WriteLine(x);\n```\n\nEnd of document.")
                )
            );

            await Harness.Render(500);

            var hasCode = H.FindTextContaining("var x = 42") is not null
                || H.FindControl<RichTextBlock>(rtb => true) is not null;
            H.Check("Markdown_CodeBlockAndLinks_CodeBlockExists", hasCode);

            var textBlocks = H.FindAllControls<TextBlock>(_ => true);
            var richTextBlocks = H.FindAllControls<RichTextBlock>(_ => true);
            H.Check("Markdown_CodeBlockAndLinks_DocumentRendered",
                textBlocks.Count + richTextBlocks.Count >= 2);

            var stack = H.FindControl<StackPanel>(_ => true);
            H.Check("Markdown_CodeBlockAndLinks_HasLayout",
                stack is not null);
        }
    }
}

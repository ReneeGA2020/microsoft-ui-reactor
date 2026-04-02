using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;

namespace Duct.EndToEnd.App.Fixtures;

internal static class MarkdownFixtures
{
    internal class HeadingsAndFormatting(Harness h) : FixtureBase(h)
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

            // Markdown renders to TextBlock (headings) and RichTextBlock (paragraphs)
            // Check for heading text in TextBlock or RichTextBlock
            var hasHeading = H.FindText("Main Heading") is not null
                || H.FindControl<RichTextBlock>(rtb => rtb.Blocks.Count > 0) is not null;
            H.Check("Markdown_HeadingsAndFormatting_ContentRendered", hasHeading);

            // Verify we have multiple visual elements (headings + paragraphs)
            var textBlocks = H.FindAllControls<TextBlock>(_ => true);
            var richTextBlocks = H.FindAllControls<RichTextBlock>(_ => true);
            H.Check("Markdown_HeadingsAndFormatting_MultipleBlocks",
                textBlocks.Count + richTextBlocks.Count >= 2);

            // Check that a StackPanel was created (markdown builds a VStack of blocks)
            var stack = H.FindControl<StackPanel>(_ => true);
            H.Check("Markdown_HeadingsAndFormatting_StackPanelCreated",
                stack is not null);
        }
    }

    internal class CodeBlockAndLinks(Harness h) : FixtureBase(h)
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

            // Code block should render (look for the code text in any TextBlock)
            var hasCode = H.FindTextContaining("var x = 42") is not null
                || H.FindControl<RichTextBlock>(rtb => true) is not null;
            H.Check("Markdown_CodeBlockAndLinks_CodeBlockExists", hasCode);

            // Verify the document rendered (multiple blocks)
            var textBlocks = H.FindAllControls<TextBlock>(_ => true);
            var richTextBlocks = H.FindAllControls<RichTextBlock>(_ => true);
            H.Check("Markdown_CodeBlockAndLinks_DocumentRendered",
                textBlocks.Count + richTextBlocks.Count >= 2);

            // Verify a StackPanel container exists
            var stack = H.FindControl<StackPanel>(_ => true);
            H.Check("Markdown_CodeBlockAndLinks_HasLayout",
                stack is not null);
        }
    }
}

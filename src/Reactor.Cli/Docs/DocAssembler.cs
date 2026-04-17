using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.UI.Reactor.Cli.Docs;

internal record ScreenshotInfo(string Id, string TopicId, string Description, string Format);

/// <summary>
/// Replaces <c>snippet=</c> and <c>screenshot://</c> directives in compiled doc output.
/// </summary>
internal static partial class DocAssembler
{
    // ```csharp snippet="topic/id"            or   ```csharp snippet="topic/id" title="Title"
    // ```
    [GeneratedRegex(@"```csharp\s+snippet=""([^""]+)""(?:\s+title=""([^""]+)"")?\s*[\r\n]+```")]
    private static partial Regex SnippetDirective();

    // ![alt text](screenshot://topic/id)
    [GeneratedRegex(@"!\[([^\]]*)\]\(screenshot://([^)]+)\)")]
    private static partial Regex ScreenshotDirective();

    public static string Assemble(
        string body,
        Dictionary<string, SnippetExtractor.Snippet> snippets,
        Dictionary<string, ScreenshotInfo> screenshots,
        out List<string> errors,
        out List<string> warnings)
    {
        var errs = new List<string>();
        var warns = new List<string>();
        var output = body;

        // Replace snippet directives with extracted code
        output = SnippetDirective().Replace(output, match =>
        {
            var snippetId = match.Groups[1].Value;
            var title = match.Groups[2].Success ? match.Groups[2].Value : null;

            if (!snippets.TryGetValue(snippetId, out var snippet))
            {
                errs.Add($"Missing snippet: {snippetId}");
                return match.Value;
            }

            var sb = new StringBuilder();
            if (title != null)
                sb.AppendLine($"// {title}");
            sb.AppendLine("```csharp");
            sb.AppendLine(snippet.Code);
            sb.Append("```");
            return sb.ToString();
        });

        // Replace screenshot:// URLs with relative image paths
        output = ScreenshotDirective().Replace(output, match =>
        {
            var altText = match.Groups[1].Value;
            var screenshotId = match.Groups[2].Value;

            if (!screenshots.ContainsKey(screenshotId))
                warns.Add($"Screenshot not captured: {screenshotId}");

            var parts = screenshotId.Split('/');
            var topic = parts[0];
            var id = parts.Length > 1 ? parts[1] : parts[0];
            var format = screenshots.TryGetValue(screenshotId, out var info) ? info.Format : "png";

            return $"![{altText}](images/{topic}/{id}.{format})";
        });

        errors = errs;
        warnings = warns;
        return output;
    }
}

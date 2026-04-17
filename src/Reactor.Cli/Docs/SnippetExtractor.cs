namespace Microsoft.UI.Reactor.Cli.Docs;

/// <summary>
/// Extracts code snippets from C# source files using <c>// &lt;snippet:id&gt;</c> markers.
/// Supports nested snippets: the outer snippet includes inner snippet code but not markers.
/// </summary>
internal static class SnippetExtractor
{
    internal record Snippet(string Id, string FullId, string Code, string SourceFile, int StartLine);

    public static Dictionary<string, Snippet> ExtractFromApp(string appDir, string topicId)
    {
        var snippets = new Dictionary<string, Snippet>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(appDir, "*.cs", SearchOption.AllDirectories))
        {
            // Skip obj/bin directories
            var relativePath = Path.GetRelativePath(appDir, file);
            if (relativePath.StartsWith("obj" + Path.DirectorySeparatorChar) || relativePath.StartsWith("bin" + Path.DirectorySeparatorChar)) continue;

            ExtractFromFile(file, topicId, snippets);
        }
        return snippets;
    }

    private static void ExtractFromFile(string filePath, string topicId, Dictionary<string, Snippet> results)
    {
        var lines = File.ReadAllLines(filePath);
        var activeSnippets = new Dictionary<string, (List<string> lines, int startLine)>();
        var openStack = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();

            // Opening marker: // <snippet:id>
            if (trimmed.StartsWith("// <snippet:") && trimmed.EndsWith(">") && !trimmed.Contains("/snippet:"))
            {
                var id = trimmed["// <snippet:".Length..^1];
                openStack.Add(id);
                activeSnippets[id] = (new List<string>(), i + 1);
                continue;
            }

            // Closing marker: // </snippet:id>
            if (trimmed.StartsWith("// </snippet:") && trimmed.EndsWith(">"))
            {
                var id = trimmed["// </snippet:".Length..^1];
                openStack.Remove(id);

                if (activeSnippets.TryGetValue(id, out var data))
                {
                    var code = TrimCommonIndentation(data.lines);
                    var fullId = $"{topicId}/{id}";
                    results[fullId] = new Snippet(id, fullId, code, filePath, data.startLine);
                }
                continue;
            }

            // Add line to all currently open snippets
            foreach (var id in openStack)
            {
                activeSnippets[id].lines.Add(lines[i]);
            }
        }

        // Warn about unclosed snippets
        foreach (var id in openStack)
        {
            Console.Error.WriteLine($"  ⚠ Unclosed snippet '{id}' in {filePath}");
        }
    }

    private static string TrimCommonIndentation(List<string> lines)
    {
        if (lines.Count == 0) return "";

        // Remove leading and trailing blank lines
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
            lines.RemoveAt(0);
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
            lines.RemoveAt(lines.Count - 1);

        if (lines.Count == 0) return "";

        // Find minimum indentation among non-empty lines
        var nonEmpty = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        if (nonEmpty.Count == 0) return "";

        var minIndent = nonEmpty.Min(l => l.Length - l.TrimStart().Length);

        return string.Join('\n', lines.Select(l =>
            string.IsNullOrWhiteSpace(l) ? "" : (l.Length > minIndent ? l[minIndent..] : l.TrimStart())
        ));
    }
}

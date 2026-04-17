namespace Microsoft.UI.Reactor.Cli.Docs;

internal class DocTemplate
{
    public string Title { get; set; } = "";
    public string App { get; set; } = "";
    public int Order { get; set; }
    public string Audience { get; set; } = "";
    public string Goal { get; set; } = "";
    public string Body { get; set; } = "";
    public List<LockedSection> LockedSections { get; set; } = [];
    public string FilePath { get; set; } = "";
}

internal class LockedSection
{
    public string Content { get; set; } = "";
}

/// <summary>
/// Parses <c>.md.dt</c> doc templates — YAML front-matter + Markdown body with directives.
/// </summary>
internal static class TemplateParser
{
    public static DocTemplate Parse(string templatePath)
    {
        var content = File.ReadAllText(templatePath);
        var template = new DocTemplate { FilePath = templatePath };

        // Extract YAML front-matter between --- delimiters
        if (content.StartsWith("---"))
        {
            var endIndex = content.IndexOf("\n---", 3);
            if (endIndex > 0)
            {
                var frontMatter = content[3..endIndex].Trim();
                ParseFrontMatter(frontMatter, template);
                // Skip past the closing --- and any immediately following newline
                var bodyStart = endIndex + 4; // "\n---" = 4 chars
                if (bodyStart < content.Length && content[bodyStart] == '\r') bodyStart++;
                if (bodyStart < content.Length && content[bodyStart] == '\n') bodyStart++;
                template.Body = bodyStart < content.Length ? content[bodyStart..] : "";
            }
        }
        else
        {
            template.Body = content;
        }

        ExtractLockedSections(template);
        return template;
    }

    private static void ParseFrontMatter(string yaml, DocTemplate template)
    {
        var lines = yaml.Split('\n');
        string? currentKey = null;
        var multiLineValue = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            // Multi-line continuation (indented)
            if (currentKey != null && (line.StartsWith("  ") || line.StartsWith("\t")))
            {
                multiLineValue.Add(line.TrimStart());
                continue;
            }

            // Flush previous multi-line value
            if (currentKey != null && multiLineValue.Count > 0)
            {
                SetField(template, currentKey, string.Join('\n', multiLineValue));
                currentKey = null;
                multiLineValue.Clear();
            }

            var colonIdx = line.IndexOf(':');
            if (colonIdx < 0) continue;

            var key = line[..colonIdx].Trim();
            var value = line[(colonIdx + 1)..].Trim();

            if (value is "|" or ">")
            {
                currentKey = key;
                multiLineValue.Clear();
            }
            else
            {
                value = value.Trim('"').Trim('\'');
                SetField(template, key, value);
            }
        }

        if (currentKey != null && multiLineValue.Count > 0)
            SetField(template, currentKey, string.Join('\n', multiLineValue));
    }

    private static void SetField(DocTemplate template, string key, string value)
    {
        switch (key.ToLowerInvariant())
        {
            case "title": template.Title = value; break;
            case "app": template.App = value; break;
            case "order": if (int.TryParse(value, out var o)) template.Order = o; break;
            case "audience": template.Audience = value; break;
            case "goal": template.Goal = value; break;
        }
    }

    private static void ExtractLockedSections(DocTemplate template)
    {
        const string openTag = "<!-- ai:lock -->";
        const string closeTag = "<!-- /ai:lock -->";

        var body = template.Body;
        var idx = 0;

        while (true)
        {
            var openIdx = body.IndexOf(openTag, idx, StringComparison.OrdinalIgnoreCase);
            if (openIdx < 0) break;

            var closeIdx = body.IndexOf(closeTag, openIdx + openTag.Length, StringComparison.OrdinalIgnoreCase);
            if (closeIdx < 0) break;

            var contentStart = openIdx + openTag.Length;
            var content = body[contentStart..closeIdx].Trim();
            template.LockedSections.Add(new LockedSection { Content = content });

            idx = closeIdx + closeTag.Length;
        }
    }
}

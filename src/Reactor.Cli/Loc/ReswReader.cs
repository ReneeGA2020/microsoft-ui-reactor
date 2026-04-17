using System.Xml;
using System.Xml.Linq;

namespace Microsoft.UI.Reactor.Cli.Loc;

/// <summary>
/// Represents a single entry from a .resw file with full metadata.
/// </summary>
internal sealed class ReswEntry
{
    public required string Key { get; init; }
    public required string Value { get; init; }
    public string? Comment { get; init; }

    public bool IsAiDraft =>
        Comment != null && Comment.Contains("ai-translated: pending-review", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Represents all entries from a single .resw file within a locale directory.
/// </summary>
internal sealed class ReswFileData
{
    public required string Locale { get; init; }
    public required string Namespace { get; init; }
    public required string FilePath { get; init; }
    public required List<ReswEntry> Entries { get; init; }
}

/// <summary>
/// Reads .resw files from a Strings/ directory structure (Strings/{locale}/{Name}.resw).
/// Shared utility for validate, status, prune, and translate commands.
/// </summary>
internal static class ReswReader
{
    /// <summary>
    /// Reads all .resw files from a Strings/ root directory, organized by locale.
    /// Expected structure: {stringsDir}/{locale}/{Name}.resw
    /// </summary>
    public static List<ReswFileData> ReadAll(string stringsDir)
    {
        var results = new List<ReswFileData>();

        if (!Directory.Exists(stringsDir)) return results;

        foreach (var localeDir in Directory.GetDirectories(stringsDir))
        {
            var locale = Path.GetFileName(localeDir);
            foreach (var reswFile in Directory.GetFiles(localeDir, "*.resw"))
            {
                var ns = Path.GetFileNameWithoutExtension(reswFile);
                var entries = ParseReswFile(reswFile);
                results.Add(new ReswFileData
                {
                    Locale = locale,
                    Namespace = ns,
                    FilePath = reswFile,
                    Entries = entries,
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Reads all .resw files for a single locale directory.
    /// </summary>
    public static List<ReswFileData> ReadLocale(string localeDir)
    {
        var results = new List<ReswFileData>();
        if (!Directory.Exists(localeDir)) return results;

        var locale = Path.GetFileName(localeDir);
        foreach (var reswFile in Directory.GetFiles(localeDir, "*.resw"))
        {
            var ns = Path.GetFileNameWithoutExtension(reswFile);
            var entries = ParseReswFile(reswFile);
            results.Add(new ReswFileData
            {
                Locale = locale,
                Namespace = ns,
                FilePath = reswFile,
                Entries = entries,
            });
        }

        return results;
    }

    /// <summary>
    /// Parses a single .resw file into a list of entries.
    /// </summary>
    public static List<ReswEntry> ParseReswFile(string filePath)
    {
        var entries = new List<ReswEntry>();
        try
        {
            var doc = XDocument.Load(filePath);
            var root = doc.Root;
            if (root == null) return entries;

            foreach (var data in root.Elements("data"))
            {
                var name = data.Attribute("name")?.Value;
                var value = data.Element("value")?.Value;
                if (name == null || value == null) continue;

                entries.Add(new ReswEntry
                {
                    Key = name,
                    Value = value,
                    Comment = data.Element("comment")?.Value,
                });
            }
        }
        catch (XmlException)
        {
            Console.Error.WriteLine($"[WARN] Skipping malformed .resw: {filePath}");
        }

        return entries;
    }

    /// <summary>
    /// Extracts ICU parameter names from a message pattern.
    /// E.g., "Hello, {name}! You have {count, plural, ...}" returns ["name", "count"].
    /// </summary>
    public static HashSet<string> ExtractIcuParameters(string pattern)
    {
        var parameters = new HashSet<string>(StringComparer.Ordinal);
        int i = 0;
        int depth = 0;
        int paramStart = -1;

        while (i < pattern.Length)
        {
            char c = pattern[i];

            if (c == '{')
            {
                depth++;
                if (depth == 1)
                {
                    paramStart = i + 1;
                }
                i++;
            }
            else if (c == '}')
            {
                depth = Math.Max(0, depth - 1);
                if (depth == 0 && paramStart >= 0)
                {
                    var segment = pattern[paramStart..i].Trim();
                    // Extract just the parameter name (before any comma for plural/select/etc.)
                    var commaIdx = segment.IndexOf(',');
                    var paramName = (commaIdx >= 0 ? segment[..commaIdx] : segment).Trim();
                    if (paramName.Length > 0 && paramName != "#")
                    {
                        parameters.Add(paramName);
                    }
                    paramStart = -1;
                }
                i++;
            }
            else if (c == '\'' && i + 1 < pattern.Length)
            {
                // ICU quoting: skip quoted sections
                i++;
                if (pattern[i] == '\'')
                {
                    i++; // escaped quote
                }
                else
                {
                    while (i < pattern.Length && pattern[i] != '\'') i++;
                    if (i < pattern.Length) i++; // skip closing quote
                }
            }
            else
            {
                i++;
            }
        }

        return parameters;
    }

    /// <summary>
    /// Validates ICU message syntax by checking for balanced braces and basic structure.
    /// Returns null if valid, or an error message if invalid.
    /// </summary>
    public static string? ValidateIcuSyntax(string pattern)
    {
        int depth = 0;
        bool inQuote = false;

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\'')
            {
                if (i + 1 < pattern.Length && pattern[i + 1] == '\'')
                {
                    i++; // escaped quote
                }
                else
                {
                    inQuote = !inQuote;
                }
                continue;
            }

            if (inQuote) continue;

            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth < 0)
                {
                    return $"unmatched closing brace at position {i}";
                }
            }
        }

        if (inQuote)
        {
            return "unterminated quoted string";
        }

        if (depth > 0)
        {
            return $"unmatched opening brace ({depth} unclosed)";
        }

        return null;
    }
}

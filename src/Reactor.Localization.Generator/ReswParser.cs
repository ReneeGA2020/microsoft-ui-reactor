using System.Collections.Generic;
using System.Xml;

namespace Microsoft.UI.Reactor.Localization.Generator;

/// <summary>
/// Represents a single string entry from a .resw file.
/// </summary>
internal sealed class ReswEntry
{
    public string Key { get; }
    public string Value { get; }
    public string? Comment { get; }

    public ReswEntry(string key, string value, string? comment)
    {
        Key = key;
        Value = value;
        Comment = comment;
    }
}

/// <summary>
/// Parses .resw XML files into structured data.
/// </summary>
internal static class ReswParser
{
    /// <summary>
    /// Parses a .resw file's XML content and returns its entries.
    /// </summary>
    public static List<ReswEntry> Parse(string xmlContent)
    {
        var entries = new List<ReswEntry>();
        var doc = new XmlDocument();
        doc.LoadXml(xmlContent);

        var dataNodes = doc.SelectNodes("/root/data");
        if (dataNodes == null) return entries;

        foreach (XmlNode node in dataNodes)
        {
            var name = node.Attributes?["name"]?.Value;
            if (name == null) continue;

            var valueNode = node.SelectSingleNode("value");
            var commentNode = node.SelectSingleNode("comment");

            var value = valueNode?.InnerText ?? "";
            var comment = commentNode?.InnerText;

            entries.Add(new ReswEntry(name, value, comment));
        }

        return entries;
    }

    /// <summary>
    /// Determines if this is a flat layout (single Resources.resw) or multi-file layout.
    /// In flat layout, we don't nest under a "Resources" class.
    /// </summary>
    public static bool IsFlatLayout(IReadOnlyList<string> reswFileNames)
    {
        return reswFileNames.Count == 1 &&
               string.Equals(reswFileNames[0], "Resources", System.StringComparison.OrdinalIgnoreCase);
    }
}

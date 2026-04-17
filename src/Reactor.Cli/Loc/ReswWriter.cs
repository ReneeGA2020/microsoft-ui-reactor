using System.Xml;
using System.Xml.Linq;

namespace Microsoft.UI.Reactor.Cli.Loc;

/// <summary>
/// Reads and writes .resw (XML resource) files.
/// Idempotent: preserves existing keys/values/comments, only adds new entries.
/// Keys are kept in alphabetical order.
/// </summary>
internal static class ReswWriter
{
    /// <summary>
    /// Loads all existing .resw entries from the output directory.
    /// Returns a dictionary keyed by (reswFileName, key).
    /// </summary>
    public static Dictionary<(string reswFileName, string key), string> LoadExisting(string outputDir)
    {
        var entries = new Dictionary<(string, string), string>();

        if (!Directory.Exists(outputDir)) return entries;

        foreach (var file in Directory.GetFiles(outputDir, "*.resw"))
        {
            var reswName = Path.GetFileNameWithoutExtension(file);
            try
            {
                var doc = XDocument.Load(file);
                var root = doc.Root;
                if (root == null) continue;

                foreach (var data in root.Elements("data"))
                {
                    var name = data.Attribute("name")?.Value;
                    var value = data.Element("value")?.Value;
                    if (name != null && value != null)
                    {
                        entries[(reswName, name)] = value;
                    }
                }
            }
            catch (XmlException)
            {
                // Skip malformed .resw files
            }
        }

        return entries;
    }

    /// <summary>
    /// Writes new entries to .resw files, preserving existing content.
    /// Groups entries by ReswFileName and writes one .resw per group.
    /// </summary>
    public static void Write(string outputDir, List<KeyedLocString> newEntries)
    {
        if (newEntries.Count == 0) return;

        Directory.CreateDirectory(outputDir);

        // Group new entries by .resw file name
        var groups = newEntries.GroupBy(e => e.ReswFileName);

        foreach (var group in groups)
        {
            var reswFileName = group.Key;
            var filePath = Path.Combine(outputDir, $"{reswFileName}.resw");

            // Load or create the XML document
            XDocument doc;
            XElement root;

            if (File.Exists(filePath))
            {
                doc = XDocument.Load(filePath);
                root = doc.Root!;
            }
            else
            {
                root = new XElement("root",
                    new XElement("resheader",
                        new XAttribute("name", "resmimetype"),
                        new XElement("value", "text/microsoft-resx")),
                    new XElement("resheader",
                        new XAttribute("name", "version"),
                        new XElement("value", "2.0")),
                    new XElement("resheader",
                        new XAttribute("name", "reader"),
                        new XElement("value", "System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")),
                    new XElement("resheader",
                        new XAttribute("name", "writer"),
                        new XElement("value", "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"))
                );
                doc = new XDocument(
                    new XDeclaration("1.0", "utf-8", null),
                    root);
            }

            // Add new entries
            foreach (var entry in group)
            {
                var dataElement = new XElement("data",
                    new XAttribute("name", entry.Key),
                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                    new XElement("value", entry.Value));

                if (entry.Comment != null)
                {
                    dataElement.Add(new XElement("comment", entry.Comment));
                }

                root.Add(dataElement);
            }

            // Sort all <data> elements alphabetically by name
            var headers = root.Elements("resheader").ToList();
            var dataElements = root.Elements("data")
                .OrderBy(d => d.Attribute("name")?.Value, StringComparer.Ordinal)
                .ToList();

            root.RemoveNodes();
            foreach (var h in headers) root.Add(h);
            foreach (var d in dataElements) root.Add(d);

            doc.Save(filePath);
        }
    }
}

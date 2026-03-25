using System.Text;
using DuctRegedit.Models;
using Microsoft.Win32;

namespace DuctRegedit.Services;

/// <summary>
/// Handles .reg file import and export.
/// </summary>
internal static class RegFileService
{
    private const string Header = "Windows Registry Editor Version 5.00";

    // ── Export ────────────────────────────────────────────────────────

    /// <summary>
    /// Exports a registry key (and optionally its subtree) to .reg file format.
    /// </summary>
    public static Task<string> ExportAsync(string keyPath, bool includeSubtree = true)
    {
        return Task.Run(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine(Header);
            sb.AppendLine();
            ExportKey(sb, keyPath, includeSubtree);
            return sb.ToString();
        });
    }

    private static void ExportKey(StringBuilder sb, string keyPath, bool recurse)
    {
        try
        {
            using var key = RegistryService.OpenKey(keyPath);
            if (key is null) return;

            sb.AppendLine($"[{keyPath}]");

            // Export values
            foreach (var valueName in key.GetValueNames())
            {
                try
                {
                    var kind = key.GetValueKind(valueName);
                    var data = key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    if (data is null) continue;

                    var nameStr = string.IsNullOrEmpty(valueName) ? "@" : $"\"{EscapeString(valueName)}\"";
                    sb.AppendLine($"{nameStr}={FormatValue(data, kind)}");
                }
                catch { }
            }

            sb.AppendLine();

            // Recurse subkeys
            if (recurse)
            {
                foreach (var subName in key.GetSubKeyNames())
                {
                    try
                    {
                        ExportKey(sb, $"{keyPath}\\{subName}", recurse);
                    }
                    catch { }
                }
            }
        }
        catch { }
    }

    private static string FormatValue(object data, RegistryValueKind kind)
    {
        return kind switch
        {
            RegistryValueKind.String => $"\"{EscapeString((string)data)}\"",
            RegistryValueKind.ExpandString => FormatHexValue(Encoding.Unicode.GetBytes((string)data + "\0"), 2),
            RegistryValueKind.DWord => $"dword:{(int)data:x8}",
            RegistryValueKind.QWord => FormatHexValue(BitConverter.GetBytes((long)data), 0xb),
            RegistryValueKind.Binary => FormatHexValue((byte[])data, 3),
            RegistryValueKind.MultiString => FormatMultiString((string[])data),
            RegistryValueKind.None => FormatHexValue(data is byte[] bytes ? bytes : [], 0),
            _ => $"\"{data}\""
        };
    }

    private static string FormatHexValue(byte[] data, int typeId)
    {
        var hexBytes = string.Join(",", data.Select(b => b.ToString("x2")));
        var prefix = typeId == 3 ? "hex:" : $"hex({typeId:x}):";

        // Line continuation for long values (80 char limit)
        var result = new StringBuilder(prefix);
        var line = prefix;
        var first = true;
        foreach (var b in data)
        {
            var byteStr = (first ? "" : ",") + b.ToString("x2");
            if (line.Length + byteStr.Length > 78)
            {
                result.Append("\\\r\n  ");
                line = "  ";
            }
            result.Append(byteStr);
            line += byteStr;
            first = false;
        }

        return result.ToString();
    }

    private static string FormatMultiString(string[] strings)
    {
        // REG_MULTI_SZ: null-separated, double-null terminated, stored as hex(7):
        var combined = string.Join("\0", strings) + "\0\0";
        var bytes = Encoding.Unicode.GetBytes(combined);
        return FormatHexValue(bytes, 7);
    }

    private static string EscapeString(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    // ── Import ────────────────────────────────────────────────────────

    /// <summary>
    /// Imports a .reg file content into the registry.
    /// Returns the number of keys processed.
    /// </summary>
    public static Task<int> ImportAsync(string content)
    {
        return Task.Run(() =>
        {
            var lines = content.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
            if (lines.Count == 0) return 0;

            // Validate header
            var headerLine = lines[0].Trim();
            if (!headerLine.StartsWith("Windows Registry Editor Version") &&
                !headerLine.StartsWith("REGEDIT4"))
                return 0;

            // Join continuation lines
            var joined = new List<string>();
            var current = new StringBuilder();
            foreach (var line in lines.Skip(1))
            {
                if (current.Length > 0 && !line.StartsWith("["))
                {
                    if (current.ToString().EndsWith('\\'))
                    {
                        current.Length--; // Remove trailing backslash
                        current.Append(line.TrimStart());
                        continue;
                    }
                }
                if (current.Length > 0)
                    joined.Add(current.ToString());
                current.Clear();
                current.Append(line);
            }
            if (current.Length > 0)
                joined.Add(current.ToString());

            int keyCount = 0;
            string? currentKeyPath = null;

            foreach (var line in joined)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Key line: [HKEY_xxx\path] or delete: [-HKEY_xxx\path]
                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    var keySpec = line[1..^1];
                    if (keySpec.StartsWith('-'))
                    {
                        // Delete key
                        var deletePath = keySpec[1..];
                        _ = RegistryService.DeleteKeyAsync(deletePath).GetAwaiter().GetResult();
                        currentKeyPath = null;
                    }
                    else
                    {
                        currentKeyPath = keySpec;
                        // Ensure key exists
                        var (parentPath, name) = SplitKeyPath(currentKeyPath);
                        if (parentPath is not null && name is not null)
                        {
                            _ = RegistryService.CreateKeyAsync(parentPath, name).GetAwaiter().GetResult();
                        }
                        keyCount++;
                    }
                    continue;
                }

                if (currentKeyPath is null) continue;

                // Value line: "name"=value or @=value
                var eqIdx = line.IndexOf('=');
                if (eqIdx < 0) continue;

                var nameStr = line[..eqIdx].Trim();
                var valueStr = line[(eqIdx + 1)..].Trim();

                string valueName;
                if (nameStr == "@")
                    valueName = "";
                else if (nameStr.StartsWith('"') && nameStr.EndsWith('"'))
                    valueName = UnescapeString(nameStr[1..^1]);
                else
                    continue;

                // Delete marker
                if (valueStr == "-")
                {
                    _ = RegistryService.DeleteValueAsync(currentKeyPath, valueName).GetAwaiter().GetResult();
                    continue;
                }

                // Parse value
                var (data, kind) = ParseValue(valueStr);
                if (data is not null)
                {
                    _ = RegistryService.SetValueAsync(currentKeyPath, valueName, data, kind).GetAwaiter().GetResult();
                }
            }

            return keyCount;
        });
    }

    private static (object? Data, RegistryValueKind Kind) ParseValue(string valueStr)
    {
        // String: "value"
        if (valueStr.StartsWith('"') && valueStr.EndsWith('"'))
        {
            return (UnescapeString(valueStr[1..^1]), RegistryValueKind.String);
        }

        // DWORD: dword:xxxxxxxx
        if (valueStr.StartsWith("dword:", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(valueStr[6..], System.Globalization.NumberStyles.HexNumber, null, out var dword))
                return (dword, RegistryValueKind.DWord);
            return (null, RegistryValueKind.None);
        }

        // Hex types: hex:, hex(2):, hex(7):, hex(b):, hex(0):
        if (valueStr.StartsWith("hex", StringComparison.OrdinalIgnoreCase))
        {
            int typeId = 3; // default for hex:
            var colonIdx = valueStr.IndexOf(':');
            if (colonIdx < 0) return (null, RegistryValueKind.None);

            var prefix = valueStr[..colonIdx];
            if (prefix.Contains('(') && prefix.Contains(')'))
            {
                var typeStr = prefix[(prefix.IndexOf('(') + 1)..prefix.IndexOf(')')];
                typeId = int.Parse(typeStr, System.Globalization.NumberStyles.HexNumber);
            }

            var hexStr = valueStr[(colonIdx + 1)..].Trim();
            var bytes = ParseHexBytes(hexStr);

            return typeId switch
            {
                0 => (bytes, RegistryValueKind.None),
                2 => (Encoding.Unicode.GetString(bytes).TrimEnd('\0'), RegistryValueKind.ExpandString),
                3 => (bytes, RegistryValueKind.Binary),
                7 => (ParseMultiString(bytes), RegistryValueKind.MultiString),
                0xb => (bytes.Length >= 8 ? BitConverter.ToInt64(bytes, 0) : 0L, RegistryValueKind.QWord),
                _ => (bytes, RegistryValueKind.Binary)
            };
        }

        return (null, RegistryValueKind.None);
    }

    private static byte[] ParseHexBytes(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return [];
        return hex.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0)
            .Select(s => byte.Parse(s, System.Globalization.NumberStyles.HexNumber))
            .ToArray();
    }

    private static string[] ParseMultiString(byte[] bytes)
    {
        var str = Encoding.Unicode.GetString(bytes);
        // Remove trailing double-null
        str = str.TrimEnd('\0');
        return str.Split('\0');
    }

    private static string UnescapeString(string s)
    {
        return s.Replace("\\\\", "\x01").Replace("\\\"", "\"").Replace("\x01", "\\");
    }

    private static (string? ParentPath, string? Name) SplitKeyPath(string path)
    {
        var lastSep = path.LastIndexOf('\\');
        if (lastSep < 0) return (null, null);
        return (path[..lastSep], path[(lastSep + 1)..]);
    }
}

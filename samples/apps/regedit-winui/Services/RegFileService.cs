using Microsoft.Win32;
using RegeditWinUI.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace RegeditWinUI.Services;

public static class RegFileService
{
    private const string Header = "Windows Registry Editor Version 5.00";

    public static string Export(string keyPath, bool allKeys = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header);
        sb.AppendLine();

        if (allKeys)
        {
            foreach (var root in RegistryService.GetRootKeys())
                ExportKeyRecursive(root.FullPath, sb);
        }
        else
        {
            ExportKeyRecursive(keyPath, sb);
        }
        return sb.ToString();
    }

    private static void ExportKeyRecursive(string keyPath, StringBuilder sb)
    {
        sb.AppendLine($"[{keyPath}]");

        var values = RegistryService.GetValues(keyPath);
        foreach (var val in values)
        {
            string name = val.IsDefault ? "@" : $"\"{EscapeString(val.Name)}\"";
            string data = FormatValue(val);
            sb.AppendLine($"{name}={data}");
        }
        sb.AppendLine();

        foreach (var child in RegistryService.GetSubKeys(keyPath))
        {
            ExportKeyRecursive(child.FullPath, sb);
        }
    }

    private static string FormatValue(RegistryValueEntry val)
    {
        if (val.Data == null) return "\"\"";
        return val.Kind switch
        {
            RegistryValueKind.String => $"\"{EscapeString(val.Data.ToString() ?? string.Empty)}\"",
            RegistryValueKind.DWord => $"dword:{(uint)(int)val.Data:x8}",
            RegistryValueKind.QWord => FormatHexLine("hex(b)", BitConverter.GetBytes((long)val.Data)),
            RegistryValueKind.Binary => FormatHexLine("hex", (byte[])val.Data),
            RegistryValueKind.ExpandString => FormatHexLine("hex(2)", Encoding.Unicode.GetBytes((string)val.Data + "\0")),
            RegistryValueKind.MultiString => FormatHexLine("hex(7)",
                Encoding.Unicode.GetBytes(string.Join("\0", (string[])val.Data) + "\0\0")),
            RegistryValueKind.None => FormatHexLine("hex(0)", val.Data is byte[] b ? b : Array.Empty<byte>()),
            _ => $"\"{EscapeString(val.Data.ToString() ?? string.Empty)}\""
        };
    }

    private static string FormatHexLine(string prefix, byte[] data)
    {
        if (data.Length == 0) return $"{prefix}:";
        var sb = new StringBuilder();
        sb.Append($"{prefix}:");
        for (int i = 0; i < data.Length; i++)
        {
            if (i > 0) sb.Append(',');
            // Line continuation at 80 chars
            if (sb.Length > 0 && (sb.Length % 78) > 74)
            {
                sb.Append("\\\r\n  ");
            }
            sb.Append(data[i].ToString("x2"));
        }
        return sb.ToString();
    }

    private static string EscapeString(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    public static ImportResult Import(string content)
    {
        var lines = content.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        int imported = 0;
        int errors = 0;

        if (lines.Count == 0 || !lines[0].StartsWith("Windows Registry Editor"))
        {
            return new ImportResult(0, 1, "Invalid .reg file header");
        }

        string? currentKey = null;
        var pendingHex = new StringBuilder();
        string pendingName = string.Empty;

        for (int i = 1; i < lines.Count; i++)
        {
            string line = lines[i];

            // Handle line continuations for hex values
            if (pendingHex.Length > 0)
            {
                string trimmed = line.Trim();
                if (trimmed.EndsWith("\\"))
                {
                    pendingHex.Append(trimmed.TrimEnd('\\'));
                    continue;
                }
                pendingHex.Append(trimmed);
                if (currentKey != null)
                {
                    if (ImportValue(currentKey, pendingName, pendingHex.ToString()))
                        imported++;
                    else
                        errors++;
                }
                pendingHex.Clear();
                continue;
            }

            if (string.IsNullOrWhiteSpace(line)) continue;

            // Key line: [HKEY_...]
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentKey = line.Substring(1, line.Length - 2);
                bool isDelete = currentKey.StartsWith("-");
                if (isDelete)
                {
                    currentKey = currentKey.Substring(1);
                    string parent = RegistryService.GetParentPath(currentKey);
                    string name = RegistryService.GetKeyName(currentKey);
                    if (RegistryService.DeleteKey(parent, name)) imported++; else errors++;
                    currentKey = null;
                }
                else
                {
                    // Ensure key exists
                    EnsureKeyExists(currentKey);
                }
                continue;
            }

            if (currentKey == null) continue;

            // Parse value assignments
            string valueName;
            string valueData;
            if (line.StartsWith("@="))
            {
                valueName = string.Empty;
                valueData = line.Substring(2);
            }
            else if (line.StartsWith("\""))
            {
                int eqIdx = FindUnescapedQuote(line, 1);
                if (eqIdx < 0) { errors++; continue; }
                valueName = UnescapeString(line.Substring(1, eqIdx - 1));
                if (eqIdx + 1 >= line.Length || line[eqIdx + 1] != '=') { errors++; continue; }
                valueData = line.Substring(eqIdx + 2);
            }
            else continue;

            // Check for value deletion
            if (valueData == "-")
            {
                if (RegistryService.DeleteValue(currentKey, valueName)) imported++; else errors++;
                continue;
            }

            // Check for line continuation
            if (valueData.TrimEnd().EndsWith("\\"))
            {
                pendingName = valueName;
                pendingHex.Append(valueData.TrimEnd().TrimEnd('\\'));
                continue;
            }

            if (ImportValue(currentKey, valueName, valueData))
                imported++;
            else
                errors++;
        }

        return new ImportResult(imported, errors, null);
    }

    private static bool ImportValue(string keyPath, string name, string data)
    {
        try
        {
            if (data.StartsWith("\"") && data.EndsWith("\""))
            {
                string str = UnescapeString(data.Substring(1, data.Length - 2));
                return RegistryService.SetValue(keyPath, name, str, RegistryValueKind.String);
            }
            if (data.StartsWith("dword:"))
            {
                uint val = uint.Parse(data.Substring(6), NumberStyles.HexNumber);
                return RegistryService.SetValue(keyPath, name, (int)val, RegistryValueKind.DWord);
            }
            if (data.StartsWith("hex:"))
            {
                byte[] bytes = ParseHexBytes(data.Substring(4));
                return RegistryService.SetValue(keyPath, name, bytes, RegistryValueKind.Binary);
            }
            if (data.StartsWith("hex("))
            {
                int paren = data.IndexOf(')');
                if (paren < 0) return false;
                int typeNum = int.Parse(data.Substring(4, paren - 4), NumberStyles.HexNumber);
                string hexStr = data.Substring(paren + 2); // skip "):"
                byte[] bytes = ParseHexBytes(hexStr);
                var kind = (RegistryValueKind)typeNum;

                return kind switch
                {
                    RegistryValueKind.ExpandString =>
                        RegistryService.SetValue(keyPath, name,
                            Encoding.Unicode.GetString(bytes).TrimEnd('\0'), RegistryValueKind.ExpandString),
                    RegistryValueKind.MultiString =>
                        RegistryService.SetValue(keyPath, name,
                            Encoding.Unicode.GetString(bytes).TrimEnd('\0').Split('\0'), RegistryValueKind.MultiString),
                    RegistryValueKind.QWord =>
                        RegistryService.SetValue(keyPath, name,
                            BitConverter.ToInt64(bytes, 0), RegistryValueKind.QWord),
                    _ => RegistryService.SetValue(keyPath, name, bytes, kind)
                };
            }
            return false;
        }
        catch { return false; }
    }

    private static byte[] ParseHexBytes(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return Array.Empty<byte>();
        return hex.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => byte.Parse(s.Trim(), NumberStyles.HexNumber))
            .ToArray();
    }

    private static void EnsureKeyExists(string keyPath)
    {
        var parts = keyPath.Split('\\');
        string current = parts[0]; // hive
        for (int i = 1; i < parts.Length; i++)
        {
            string parent = current;
            current = current + "\\" + parts[i];
            RegistryService.CreateKey(parent, parts[i]);
        }
    }

    private static int FindUnescapedQuote(string s, int start)
    {
        for (int i = start; i < s.Length; i++)
        {
            if (s[i] == '\\') { i++; continue; }
            if (s[i] == '"') return i;
        }
        return -1;
    }

    private static string UnescapeString(string s) =>
        s.Replace("\\\\", "\x01").Replace("\\\"", "\"").Replace("\x01", "\\");
}

public record ImportResult(int Imported, int Errors, string? ErrorMessage);

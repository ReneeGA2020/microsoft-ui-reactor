using Microsoft.Win32;
using System.Collections.Generic;

namespace RegeditWinUI.Models;

public class RegistryKeyEntry
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool HasChildren { get; set; }

    public override string ToString() => Name;
}

public class RegistryValueEntry
{
    public string Name { get; set; } = string.Empty;
    public RegistryValueKind Kind { get; set; }
    public object? Data { get; set; }

    public bool IsDefault => string.IsNullOrEmpty(Name);

    public string DisplayName => IsDefault ? "(Default)" : Name;

    public string DisplayType => Kind switch
    {
        RegistryValueKind.String => "REG_SZ",
        RegistryValueKind.ExpandString => "REG_EXPAND_SZ",
        RegistryValueKind.Binary => "REG_BINARY",
        RegistryValueKind.DWord => "REG_DWORD",
        RegistryValueKind.QWord => "REG_QWORD",
        RegistryValueKind.MultiString => "REG_MULTI_SZ",
        RegistryValueKind.None => "REG_NONE",
        _ => "REG_UNKNOWN"
    };

    public string DisplayData
    {
        get
        {
            if (Data == null) return "(value not set)";
            return Kind switch
            {
                RegistryValueKind.String or RegistryValueKind.ExpandString => Data.ToString() ?? string.Empty,
                RegistryValueKind.DWord => $"0x{(int)Data:x8} ({(int)Data})",
                RegistryValueKind.QWord => $"0x{(long)Data:x16} ({(long)Data})",
                RegistryValueKind.Binary => FormatBinary((byte[])Data),
                RegistryValueKind.MultiString => string.Join(" ", (string[])Data),
                RegistryValueKind.None => FormatBinary(Data is byte[] b ? b : System.Array.Empty<byte>()),
                _ => Data.ToString() ?? string.Empty
            };
        }
    }

    private static string FormatBinary(byte[] data)
    {
        if (data.Length == 0) return "(zero-length binary value)";
        var parts = new List<string>();
        int count = System.Math.Min(data.Length, 128);
        for (int i = 0; i < count; i++)
            parts.Add(data[i].ToString("x2"));
        string result = string.Join(" ", parts);
        if (data.Length > 128) result += "...";
        return result;
    }
}

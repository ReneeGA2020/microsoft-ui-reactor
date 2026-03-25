using Microsoft.Win32;

namespace DuctRegedit.Models;

/// <summary>
/// Represents a registry key (node in the tree).
/// </summary>
public sealed record RegistryKeyEntry(
    string Name,
    string FullPath,
    bool HasChildren
);

/// <summary>
/// Represents a single registry value within a key.
/// </summary>
public sealed record RegistryValueEntry(
    string Name,
    RegistryValueKind Kind,
    object? Data
)
{
    /// <summary>
    /// Display name: empty name is shown as "(Default)".
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(Name) ? "(Default)" : Name;

    /// <summary>
    /// Human-readable type string matching native regedit display.
    /// </summary>
    public string TypeName => Kind switch
    {
        RegistryValueKind.String => "REG_SZ",
        RegistryValueKind.ExpandString => "REG_EXPAND_SZ",
        RegistryValueKind.Binary => "REG_BINARY",
        RegistryValueKind.DWord => "REG_DWORD",
        RegistryValueKind.QWord => "REG_QWORD",
        RegistryValueKind.MultiString => "REG_MULTI_SZ",
        RegistryValueKind.None => "REG_NONE",
        _ => "REG_SZ"
    };

    /// <summary>
    /// Formatted data for display in the value list.
    /// </summary>
    public string DisplayData => FormatData();

    private string FormatData()
    {
        if (Data is null) return "(value not set)";

        return Kind switch
        {
            RegistryValueKind.String => (string)Data,
            RegistryValueKind.ExpandString => (string)Data,
            RegistryValueKind.DWord => $"0x{(int)Data:x8} ({(int)Data})",
            RegistryValueKind.QWord => $"0x{(long)Data:x16} ({(long)Data})",
            RegistryValueKind.MultiString => string.Join(" ", ((string[])Data).Select(s => s)),
            RegistryValueKind.Binary => FormatBinaryData((byte[])Data),
            RegistryValueKind.None => FormatBinaryData(Data is byte[] bytes ? bytes : []),
            _ => Data?.ToString() ?? ""
        };
    }

    private static string FormatBinaryData(byte[] data)
    {
        if (data.Length == 0) return "(zero-length binary value)";
        // Show first 8 bytes like native regedit
        var display = data.Take(8).Select(b => b.ToString("x2"));
        var result = string.Join(" ", display);
        if (data.Length > 8) result += "...";
        return result;
    }
}

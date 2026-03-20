namespace DuctFiles.Models;

/// <summary>
/// Immutable record representing a file or directory in the file list.
/// </summary>
public sealed record FileEntry(
    string Name,
    string FullPath,
    bool IsDirectory,
    long Size,
    DateTime Modified,
    bool HasChildren
)
{
    /// <summary>
    /// Returns a human-readable size string (e.g., "1.2 MB").
    /// Directories return an empty string.
    /// </summary>
    public string FormattedSize
    {
        get
        {
            if (IsDirectory) return "";
            return Size switch
            {
                < 1024 => $"{Size} B",
                < 1024 * 1024 => $"{Size / 1024.0:F1} KB",
                < 1024L * 1024 * 1024 => $"{Size / (1024.0 * 1024):F1} MB",
                _ => $"{Size / (1024.0 * 1024 * 1024):F2} GB"
            };
        }
    }

    public string TypeDescription => IsDirectory ? "File folder" : GetFileType(Name);

    private static string GetFileType(string name)
    {
        var ext = Path.GetExtension(name);
        if (string.IsNullOrEmpty(ext)) return "File";
        return ext.ToUpperInvariant()[1..] + " File";
    }
}

namespace Microsoft.UI.Reactor.Cli.Loc;

/// <summary>
/// Represents a localizable string found by the AST scanner.
/// </summary>
internal sealed class LocalizableString
{
    /// <summary>The file path where this string was found.</summary>
    public required string FilePath { get; init; }

    /// <summary>The class name containing the string (e.g., "SettingsPage").</summary>
    public required string ClassName { get; init; }

    /// <summary>The DSL context (e.g., "Text", "Button", "Placeholder").</summary>
    public required string Context { get; init; }

    /// <summary>The English string value (or ICU message for interpolations).</summary>
    public required string Value { get; init; }

    /// <summary>Start position in the source file (character offset).</summary>
    public int SpanStart { get; init; }

    /// <summary>Length of the original expression in the source file.</summary>
    public int SpanLength { get; init; }

    /// <summary>Whether this was an interpolated string converted to ICU.</summary>
    public bool IsInterpolation { get; init; }

    /// <summary>Named arguments for ICU messages (from interpolation conversion).</summary>
    public Dictionary<string, string>? ArgumentMap { get; init; }

    /// <summary>Optional warning message (e.g., complex expression skipped).</summary>
    public string? Warning { get; set; }

    /// <summary>Branch index for ternary extractions (0 = true branch, 1 = false branch).</summary>
    public int? TernaryBranch { get; init; }
}

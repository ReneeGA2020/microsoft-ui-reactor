namespace Microsoft.UI.Reactor.Cli.Loc;

/// <summary>
/// A localizable string with an assigned key, ready for .resw generation.
/// </summary>
internal sealed class KeyedLocString
{
    /// <summary>The .resw file name (without extension), e.g., "Settings" or "Resources".</summary>
    public required string ReswFileName { get; init; }

    /// <summary>The namespace within the .resw file (null for flat layout).</summary>
    public string? Namespace { get; init; }

    /// <summary>The key within the .resw file, e.g., "Save" or "SearchPlaceholder".</summary>
    public required string Key { get; init; }

    /// <summary>The English string value (or ICU message).</summary>
    public required string Value { get; init; }

    /// <summary>Optional comment for the .resw entry.</summary>
    public string? Comment { get; init; }

    /// <summary>Optional warning to display during extraction.</summary>
    public string? Warning { get; init; }

    /// <summary>The original localizable string this was generated from.</summary>
    public required LocalizableString Source { get; init; }
}

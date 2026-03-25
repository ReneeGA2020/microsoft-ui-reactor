using DuctRegedit.Models;
using Microsoft.Win32;

namespace DuctRegedit.Services;

/// <summary>
/// Async registry search with cancellation support.
/// </summary>
internal static class SearchService
{
    public sealed record SearchResult(string KeyPath, string? ValueName, string? MatchedData);

    /// <summary>
    /// Searches the registry starting from the given path.
    /// Returns the first match found, or null if none found before cancellation.
    /// </summary>
    public static Task<SearchResult?> FindNextAsync(
        string startPath,
        string? startAfterValue,
        string searchText,
        FindFlags flags,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => FindNext(startPath, startAfterValue, searchText, flags, cancellationToken), cancellationToken);
    }

    private static SearchResult? FindNext(
        string startPath,
        string? startAfterValue,
        string searchText,
        FindFlags flags,
        CancellationToken ct)
    {
        var comparison = flags.HasFlag(FindFlags.WholeStringOnly)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.OrdinalIgnoreCase;

        bool wholeString = flags.HasFlag(FindFlags.WholeStringOnly);

        // Search from current key forward
        return SearchKey(startPath, startAfterValue, searchText, flags, wholeString, comparison, ct);
    }

    private static SearchResult? SearchKey(
        string keyPath,
        string? startAfterValue,
        string searchText,
        FindFlags flags,
        bool wholeString,
        StringComparison comparison,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            using var key = RegistryService.OpenKey(keyPath);
            if (key is null) return null;

            // Search values in this key
            if (flags.HasFlag(FindFlags.Values) || flags.HasFlag(FindFlags.Data))
            {
                bool pastStart = startAfterValue is null;
                foreach (var valueName in key.GetValueNames())
                {
                    ct.ThrowIfCancellationRequested();

                    if (!pastStart)
                    {
                        if (valueName == startAfterValue) pastStart = true;
                        continue;
                    }

                    // Check value name
                    if (flags.HasFlag(FindFlags.Values) && Matches(valueName, searchText, wholeString, comparison))
                    {
                        return new SearchResult(keyPath, valueName, null);
                    }

                    // Check data
                    if (flags.HasFlag(FindFlags.Data))
                    {
                        try
                        {
                            var data = key.GetValue(valueName);
                            var dataStr = data?.ToString() ?? "";
                            if (Matches(dataStr, searchText, wholeString, comparison))
                            {
                                return new SearchResult(keyPath, valueName, dataStr);
                            }
                        }
                        catch { }
                    }
                }
            }

            // Search subkeys
            foreach (var subName in key.GetSubKeyNames())
            {
                ct.ThrowIfCancellationRequested();

                var childPath = $"{keyPath}\\{subName}";

                // Check key name
                if (flags.HasFlag(FindFlags.Keys) && Matches(subName, searchText, wholeString, comparison))
                {
                    return new SearchResult(childPath, null, null);
                }

                // Recurse into subkey
                var result = SearchKey(childPath, null, searchText, flags, wholeString, comparison, ct);
                if (result is not null) return result;
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { }

        return null;
    }

    private static bool Matches(string value, string searchText, bool wholeString, StringComparison comparison)
    {
        if (string.IsNullOrEmpty(value)) return false;
        if (wholeString)
            return value.Equals(searchText, comparison);
        else
            return value.Contains(searchText, comparison);
    }
}

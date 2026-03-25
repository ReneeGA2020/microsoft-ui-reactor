using Microsoft.Win32;
using RegeditWinUI.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RegeditWinUI.Services;

public class SearchResult
{
    public string KeyPath { get; set; } = string.Empty;
    public string? ValueName { get; set; }
    public bool IsKeyMatch { get; set; }
    public bool IsValueNameMatch { get; set; }
    public bool IsDataMatch { get; set; }
}

public class SearchService
{
    public async Task<SearchResult?> FindNextAsync(
        string startKeyPath,
        string? startValueName,
        string searchText,
        FindFlags flags,
        CancellationToken ct,
        Action<string>? onProgress = null)
    {
        return await Task.Run(() =>
        {
            bool searchKeys = flags.HasFlag(FindFlags.Keys);
            bool searchValues = flags.HasFlag(FindFlags.Values);
            bool searchData = flags.HasFlag(FindFlags.Data);
            bool wholeString = flags.HasFlag(FindFlags.WholeString);

            // Start searching from the given position
            bool pastStart = false;
            return SearchRecursive(startKeyPath, startValueName, ref pastStart,
                searchText, searchKeys, searchValues, searchData, wholeString, ct, onProgress);
        }, ct);
    }

    private SearchResult? SearchRecursive(
        string keyPath,
        string? skipToValue,
        ref bool pastStart,
        string searchText,
        bool searchKeys,
        bool searchValues,
        bool searchData,
        bool wholeString,
        CancellationToken ct,
        Action<string>? onProgress)
    {
        ct.ThrowIfCancellationRequested();
        onProgress?.Invoke(keyPath);

        // If we're past start, check this key name
        if (pastStart && searchKeys)
        {
            string keyName = RegistryService.GetKeyName(keyPath);
            if (Matches(keyName, searchText, wholeString))
                return new SearchResult { KeyPath = keyPath, IsKeyMatch = true };
        }

        // Check values
        if (searchValues || searchData)
        {
            try
            {
                using var key = RegistryService.OpenKey(keyPath);
                if (key != null)
                {
                    string[] valueNames = key.GetValueNames().OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray();
                    bool skipDone = (skipToValue == null);

                    foreach (string name in valueNames)
                    {
                        ct.ThrowIfCancellationRequested();

                        if (!skipDone)
                        {
                            if (name.Equals(skipToValue, StringComparison.OrdinalIgnoreCase))
                            {
                                skipDone = true;
                                pastStart = true;
                                continue; // skip the value we started on
                            }
                            continue;
                        }

                        if (!pastStart) { pastStart = true; continue; }

                        if (searchValues && Matches(name, searchText, wholeString))
                            return new SearchResult { KeyPath = keyPath, ValueName = name, IsValueNameMatch = true };

                        if (searchData)
                        {
                            try
                            {
                                object? val = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                                if (val != null && DataMatches(val, key.GetValueKind(name), searchText, wholeString))
                                    return new SearchResult { KeyPath = keyPath, ValueName = name, IsDataMatch = true };
                            }
                            catch { }
                        }
                    }

                    if (!skipDone) pastStart = true; // value wasn't found, move on
                }
            }
            catch { }
        }

        if (!pastStart) pastStart = true;

        // Recurse into children
        var children = RegistryService.GetSubKeys(keyPath);
        foreach (var child in children)
        {
            ct.ThrowIfCancellationRequested();
            string? childSkip = null; // no value skip in children
            var result = SearchRecursive(child.FullPath, childSkip, ref pastStart,
                searchText, searchKeys, searchValues, searchData, wholeString, ct, onProgress);
            if (result != null) return result;
        }

        return null;
    }

    private static bool Matches(string text, string search, bool wholeString)
    {
        if (wholeString)
            return text.Equals(search, StringComparison.OrdinalIgnoreCase);
        return text.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static bool DataMatches(object data, RegistryValueKind kind, string search, bool wholeString)
    {
        string text = kind switch
        {
            RegistryValueKind.String or RegistryValueKind.ExpandString => data.ToString() ?? string.Empty,
            RegistryValueKind.MultiString => string.Join("\0", (string[])data),
            RegistryValueKind.DWord => ((int)data).ToString(),
            RegistryValueKind.QWord => ((long)data).ToString(),
            _ => string.Empty
        };
        return Matches(text, search, wholeString);
    }
}

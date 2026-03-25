using DuctRegedit.Models;
using Microsoft.Win32;

namespace DuctRegedit.Services;

/// <summary>
/// Provides all registry access operations. All mutations use Task.Run for thread safety.
/// </summary>
internal static class RegistryService
{
    private static readonly (string Name, string FullPath, RegistryKey Root)[] RootKeys =
    [
        ("HKEY_CLASSES_ROOT", "HKEY_CLASSES_ROOT", Registry.ClassesRoot),
        ("HKEY_CURRENT_USER", "HKEY_CURRENT_USER", Registry.CurrentUser),
        ("HKEY_LOCAL_MACHINE", "HKEY_LOCAL_MACHINE", Registry.LocalMachine),
        ("HKEY_USERS", "HKEY_USERS", Registry.Users),
        ("HKEY_CURRENT_CONFIG", "HKEY_CURRENT_CONFIG", Registry.CurrentConfig),
    ];

    /// <summary>
    /// Returns the 5 root hive entries.
    /// </summary>
    public static RegistryKeyEntry[] GetRootKeys()
    {
        return RootKeys.Select(r =>
        {
            bool hasChildren;
            try
            {
                using var key = r.Root;
                hasChildren = key.SubKeyCount > 0;
            }
            catch
            {
                hasChildren = true; // assume children if access denied
            }
            return new RegistryKeyEntry(r.Name, r.FullPath, hasChildren);
        }).ToArray();
    }

    /// <summary>
    /// Enumerates subkeys of the given registry path.
    /// </summary>
    public static Task<RegistryKeyEntry[]> GetSubKeysAsync(string path)
    {
        return Task.Run(() =>
        {
            try
            {
                using var key = OpenKey(path);
                if (key is null) return [];

                var names = key.GetSubKeyNames();
                var results = new List<RegistryKeyEntry>(names.Length);
                foreach (var name in names)
                {
                    var childPath = $"{path}\\{name}";
                    bool hasChildren;
                    try
                    {
                        using var child = key.OpenSubKey(name);
                        hasChildren = child is not null && child.SubKeyCount > 0;
                    }
                    catch
                    {
                        hasChildren = true;
                    }
                    results.Add(new RegistryKeyEntry(name, childPath, hasChildren));
                }
                return results.ToArray();
            }
            catch
            {
                return [];
            }
        });
    }

    /// <summary>
    /// Enumerates values of the given registry key path.
    /// </summary>
    public static Task<RegistryValueEntry[]> GetValuesAsync(string path)
    {
        return Task.Run(() =>
        {
            try
            {
                using var key = OpenKey(path);
                if (key is null) return [new RegistryValueEntry("", RegistryValueKind.String, "")];

                var valueNames = key.GetValueNames();
                var results = new List<RegistryValueEntry>(valueNames.Length + 1);

                // Always include (Default) value
                bool hasDefault = false;
                foreach (var name in valueNames)
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        hasDefault = true;
                    }
                    try
                    {
                        var kind = key.GetValueKind(name);
                        var data = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                        results.Add(new RegistryValueEntry(name, kind, data));
                    }
                    catch
                    {
                        // Skip values we can't read
                    }
                }

                if (!hasDefault)
                {
                    results.Insert(0, new RegistryValueEntry("", RegistryValueKind.String, "(value not set)"));
                }
                else
                {
                    // Move default to front
                    var defaultIdx = results.FindIndex(v => string.IsNullOrEmpty(v.Name));
                    if (defaultIdx > 0)
                    {
                        var def = results[defaultIdx];
                        results.RemoveAt(defaultIdx);
                        results.Insert(0, def);
                    }
                }

                return results.ToArray();
            }
            catch
            {
                return [new RegistryValueEntry("", RegistryValueKind.String, "(value not set)")];
            }
        });
    }

    /// <summary>
    /// Creates a new subkey under the given parent path.
    /// </summary>
    public static Task<string?> CreateKeyAsync(string parentPath, string name)
    {
        return Task.Run(() =>
        {
            try
            {
                using var parent = OpenKeyWritable(parentPath);
                if (parent is null) return null;
                using var newKey = parent.CreateSubKey(name);
                return $"{parentPath}\\{name}";
            }
            catch
            {
                return null;
            }
        });
    }

    /// <summary>
    /// Deletes a subkey and all its contents.
    /// </summary>
    public static Task<bool> DeleteKeyAsync(string path)
    {
        return Task.Run(() =>
        {
            try
            {
                var (parentPath, name) = SplitPath(path);
                if (parentPath is null || name is null) return false;
                using var parent = OpenKeyWritable(parentPath);
                if (parent is null) return false;
                parent.DeleteSubKeyTree(name, throwOnMissingSubKey: false);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Renames a registry key by recursively copying to new name and deleting old.
    /// </summary>
    public static Task<bool> RenameKeyAsync(string path, string newName)
    {
        return Task.Run(() =>
        {
            try
            {
                var (parentPath, oldName) = SplitPath(path);
                if (parentPath is null || oldName is null) return false;
                using var parent = OpenKeyWritable(parentPath);
                if (parent is null) return false;

                // Create new key and copy contents
                using var source = parent.OpenSubKey(oldName);
                if (source is null) return false;
                using var dest = parent.CreateSubKey(newName);
                if (dest is null) return false;

                CopyKey(source, dest);
                parent.DeleteSubKeyTree(oldName);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Creates a new value in the specified key.
    /// </summary>
    public static Task<bool> CreateValueAsync(string keyPath, string valueName, RegistryValueKind kind)
    {
        return Task.Run(() =>
        {
            try
            {
                using var key = OpenKeyWritable(keyPath);
                if (key is null) return false;

                object defaultValue = kind switch
                {
                    RegistryValueKind.String => "",
                    RegistryValueKind.ExpandString => "",
                    RegistryValueKind.MultiString => Array.Empty<string>(),
                    RegistryValueKind.DWord => 0,
                    RegistryValueKind.QWord => 0L,
                    RegistryValueKind.Binary => Array.Empty<byte>(),
                    _ => ""
                };

                key.SetValue(valueName, defaultValue, kind);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Sets a registry value.
    /// </summary>
    public static Task<bool> SetValueAsync(string keyPath, string valueName, object data, RegistryValueKind kind)
    {
        return Task.Run(() =>
        {
            try
            {
                using var key = OpenKeyWritable(keyPath);
                if (key is null) return false;
                key.SetValue(valueName, data, kind);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Deletes a value from the specified key.
    /// </summary>
    public static Task<bool> DeleteValueAsync(string keyPath, string valueName)
    {
        return Task.Run(() =>
        {
            try
            {
                using var key = OpenKeyWritable(keyPath);
                if (key is null) return false;
                key.DeleteValue(valueName, throwOnMissingValue: false);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Renames a value by copying to new name and deleting old.
    /// </summary>
    public static Task<bool> RenameValueAsync(string keyPath, string oldName, string newName)
    {
        return Task.Run(() =>
        {
            try
            {
                using var key = OpenKeyWritable(keyPath);
                if (key is null) return false;

                var kind = key.GetValueKind(oldName);
                var data = key.GetValue(oldName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                if (data is null) return false;

                key.SetValue(newName, data, kind);
                key.DeleteValue(oldName);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Gets the count of subkeys for status bar display.
    /// </summary>
    public static Task<int> GetSubKeyCountAsync(string path)
    {
        return Task.Run(() =>
        {
            try
            {
                using var key = OpenKey(path);
                return key?.SubKeyCount ?? 0;
            }
            catch
            {
                return 0;
            }
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Opens a registry key for reading given a full path like HKEY_LOCAL_MACHINE\SOFTWARE.
    /// </summary>
    internal static RegistryKey? OpenKey(string path)
    {
        var (root, subPath) = ParsePath(path);
        if (root is null) return null;
        if (string.IsNullOrEmpty(subPath)) return root;
        return root.OpenSubKey(subPath);
    }

    private static RegistryKey? OpenKeyWritable(string path)
    {
        var (root, subPath) = ParsePath(path);
        if (root is null) return null;
        if (string.IsNullOrEmpty(subPath)) return null; // can't write to root hive
        return root.OpenSubKey(subPath, writable: true);
    }

    internal static (RegistryKey? Root, string SubPath) ParsePath(string path)
    {
        var sepIdx = path.IndexOf('\\');
        var hiveName = sepIdx >= 0 ? path[..sepIdx] : path;
        var subPath = sepIdx >= 0 ? path[(sepIdx + 1)..] : "";

        var root = hiveName.ToUpperInvariant() switch
        {
            "HKEY_CLASSES_ROOT" or "HKCR" => Registry.ClassesRoot,
            "HKEY_CURRENT_USER" or "HKCU" => Registry.CurrentUser,
            "HKEY_LOCAL_MACHINE" or "HKLM" => Registry.LocalMachine,
            "HKEY_USERS" or "HKU" => Registry.Users,
            "HKEY_CURRENT_CONFIG" or "HKCC" => Registry.CurrentConfig,
            _ => null
        };

        return (root, subPath);
    }

    private static (string? ParentPath, string? Name) SplitPath(string path)
    {
        var lastSep = path.LastIndexOf('\\');
        if (lastSep < 0) return (null, null);
        return (path[..lastSep], path[(lastSep + 1)..]);
    }

    private static void CopyKey(RegistryKey source, RegistryKey dest)
    {
        // Copy values
        foreach (var name in source.GetValueNames())
        {
            try
            {
                var kind = source.GetValueKind(name);
                var data = source.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                if (data is not null)
                    dest.SetValue(name, data, kind);
            }
            catch { }
        }

        // Recursively copy subkeys
        foreach (var subName in source.GetSubKeyNames())
        {
            try
            {
                using var srcSub = source.OpenSubKey(subName);
                if (srcSub is null) continue;
                using var dstSub = dest.CreateSubKey(subName);
                if (dstSub is not null)
                    CopyKey(srcSub, dstSub);
            }
            catch { }
        }
    }
}

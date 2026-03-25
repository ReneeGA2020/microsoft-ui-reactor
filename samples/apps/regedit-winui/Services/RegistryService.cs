using Microsoft.Win32;
using RegeditWinUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace RegeditWinUI.Services;

public static class RegistryService
{
    private static readonly (string ShortName, string FullName, RegistryHive Hive)[] Hives =
    {
        ("HKCR", "HKEY_CLASSES_ROOT", RegistryHive.ClassesRoot),
        ("HKCU", "HKEY_CURRENT_USER", RegistryHive.CurrentUser),
        ("HKLM", "HKEY_LOCAL_MACHINE", RegistryHive.LocalMachine),
        ("HKU",  "HKEY_USERS", RegistryHive.Users),
        ("HKCC", "HKEY_CURRENT_CONFIG", RegistryHive.CurrentConfig),
    };

    public static List<RegistryKeyEntry> GetRootKeys()
    {
        return Hives.Select(h => new RegistryKeyEntry
        {
            Name = h.FullName,
            FullPath = h.FullName,
            HasChildren = true
        }).ToList();
    }

    public static RegistryKey? OpenKey(string fullPath, bool writable = false)
    {
        if (string.IsNullOrEmpty(fullPath)) return null;

        var parts = fullPath.Split('\\', 2);
        string hiveName = parts[0].ToUpperInvariant();
        string subPath = parts.Length > 1 ? parts[1] : string.Empty;

        RegistryKey? hiveKey = hiveName switch
        {
            "HKEY_CLASSES_ROOT" or "HKCR" => Registry.ClassesRoot,
            "HKEY_CURRENT_USER" or "HKCU" => Registry.CurrentUser,
            "HKEY_LOCAL_MACHINE" or "HKLM" => Registry.LocalMachine,
            "HKEY_USERS" or "HKU" => Registry.Users,
            "HKEY_CURRENT_CONFIG" or "HKCC" => Registry.CurrentConfig,
            _ => null
        };

        if (hiveKey == null) return null;
        if (string.IsNullOrEmpty(subPath)) return hiveKey;

        try
        {
            return hiveKey.OpenSubKey(subPath, writable);
        }
        catch (SecurityException)
        {
            return null;
        }
    }

    public static List<RegistryKeyEntry> GetSubKeys(string parentPath)
    {
        var results = new List<RegistryKeyEntry>();
        try
        {
            using var key = OpenKey(parentPath);
            if (key == null) return results;

            foreach (string name in key.GetSubKeyNames().OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                string childPath = $"{parentPath}\\{name}";
                bool hasChildren = false;
                try
                {
                    using var child = key.OpenSubKey(name);
                    hasChildren = child != null && child.SubKeyCount > 0;
                }
                catch (SecurityException) { }

                results.Add(new RegistryKeyEntry
                {
                    Name = name,
                    FullPath = childPath,
                    HasChildren = hasChildren
                });
            }
        }
        catch (SecurityException) { }
        return results;
    }

    public static List<RegistryValueEntry> GetValues(string keyPath)
    {
        var results = new List<RegistryValueEntry>();
        try
        {
            using var key = OpenKey(keyPath);
            if (key == null) return results;

            // Always include the (Default) value
            try
            {
                object? defaultVal = key.GetValue(string.Empty);
                RegistryValueKind defaultKind = RegistryValueKind.String;
                try { defaultKind = key.GetValueKind(string.Empty); } catch { }
                results.Add(new RegistryValueEntry
                {
                    Name = string.Empty,
                    Kind = defaultKind,
                    Data = defaultVal
                });
            }
            catch { }

            foreach (string name in key.GetValueNames().OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(name)) continue; // already added default
                try
                {
                    results.Add(new RegistryValueEntry
                    {
                        Name = name,
                        Kind = key.GetValueKind(name),
                        Data = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames)
                    });
                }
                catch (SecurityException) { }
            }
        }
        catch (SecurityException) { }
        return results;
    }

    public static bool CreateKey(string parentPath, string name)
    {
        try
        {
            using var parent = OpenKey(parentPath, writable: true);
            if (parent == null) return false;
            using var newKey = parent.CreateSubKey(name);
            return newKey != null;
        }
        catch { return false; }
    }

    public static bool DeleteKey(string parentPath, string name)
    {
        try
        {
            using var parent = OpenKey(parentPath, writable: true);
            if (parent == null) return false;
            parent.DeleteSubKeyTree(name, throwOnMissingSubKey: false);
            return true;
        }
        catch { return false; }
    }

    public static bool RenameKey(string parentPath, string oldName, string newName)
    {
        try
        {
            using var parent = OpenKey(parentPath, writable: true);
            if (parent == null) return false;

            using var source = parent.OpenSubKey(oldName);
            if (source == null) return false;

            using var dest = parent.CreateSubKey(newName);
            if (dest == null) return false;

            CopyKey(source, dest);
            parent.DeleteSubKeyTree(oldName);
            return true;
        }
        catch { return false; }
    }

    private static void CopyKey(RegistryKey source, RegistryKey dest)
    {
        foreach (string valueName in source.GetValueNames())
        {
            object? value = source.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            RegistryValueKind kind = source.GetValueKind(valueName);
            dest.SetValue(valueName, value ?? string.Empty, kind);
        }
        foreach (string subKeyName in source.GetSubKeyNames())
        {
            using var sourceChild = source.OpenSubKey(subKeyName);
            if (sourceChild == null) continue;
            using var destChild = dest.CreateSubKey(subKeyName);
            if (destChild == null) continue;
            CopyKey(sourceChild, destChild);
        }
    }

    public static bool SetValue(string keyPath, string name, object value, RegistryValueKind kind)
    {
        try
        {
            using var key = OpenKey(keyPath, writable: true);
            if (key == null) return false;
            key.SetValue(name, value, kind);
            return true;
        }
        catch { return false; }
    }

    public static bool DeleteValue(string keyPath, string name)
    {
        try
        {
            using var key = OpenKey(keyPath, writable: true);
            if (key == null) return false;
            key.DeleteValue(name, throwOnMissingValue: false);
            return true;
        }
        catch { return false; }
    }

    public static bool RenameValue(string keyPath, string oldName, string newName)
    {
        try
        {
            using var key = OpenKey(keyPath, writable: true);
            if (key == null) return false;

            object? value = key.GetValue(oldName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            RegistryValueKind kind = key.GetValueKind(oldName);
            key.SetValue(newName, value ?? string.Empty, kind);
            key.DeleteValue(oldName);
            return true;
        }
        catch { return false; }
    }

    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        path = path.Trim().TrimEnd('\\');

        // Expand short hive names
        foreach (var (shortName, fullName, _) in Hives)
        {
            if (path.Equals(shortName, StringComparison.OrdinalIgnoreCase))
                return fullName;
            if (path.StartsWith(shortName + "\\", StringComparison.OrdinalIgnoreCase))
                return fullName + path.Substring(shortName.Length);
        }
        return path;
    }

    public static string GetParentPath(string path)
    {
        int idx = path.LastIndexOf('\\');
        return idx >= 0 ? path.Substring(0, idx) : string.Empty;
    }

    public static string GetKeyName(string path)
    {
        int idx = path.LastIndexOf('\\');
        return idx >= 0 ? path.Substring(idx + 1) : path;
    }
}

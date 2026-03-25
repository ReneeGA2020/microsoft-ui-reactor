using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RegeditWinUI.Services;

public static class FavoritesService
{
    private const string FavoritesKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Applets\Regedit\Favorites";

    public static Dictionary<string, string> GetFavorites()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(FavoritesKeyPath);
            if (key == null) return result;

            foreach (string name in key.GetValueNames().OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                if (key.GetValue(name) is string path)
                    result[name] = path;
            }
        }
        catch { }
        return result;
    }

    public static bool AddFavorite(string name, string keyPath)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(FavoritesKeyPath);
            if (key == null) return false;
            key.SetValue(name, keyPath, RegistryValueKind.String);
            return true;
        }
        catch { return false; }
    }

    public static bool RemoveFavorite(string name)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(FavoritesKeyPath, writable: true);
            if (key == null) return false;
            key.DeleteValue(name, throwOnMissingValue: false);
            return true;
        }
        catch { return false; }
    }
}

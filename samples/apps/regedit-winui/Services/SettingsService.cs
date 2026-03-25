using Microsoft.Win32;
using System;

namespace RegeditWinUI.Services;

public static class SettingsService
{
    private const string SettingsKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Applets\Regedit";

    public static string GetLastKey()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(SettingsKeyPath);
            return key?.GetValue("LastKey") as string ?? "HKEY_CURRENT_USER";
        }
        catch { return "HKEY_CURRENT_USER"; }
    }

    public static void SetLastKey(string path)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(SettingsKeyPath);
            key?.SetValue("LastKey", path, RegistryValueKind.String);
        }
        catch { }
    }

    public static (int X, int Y, int Width, int Height) GetWindowPosition()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(SettingsKeyPath);
            if (key == null) return (100, 100, 1000, 600);

            // regedit stores window position in a "View" binary value
            // We'll use separate values for our WinUI version
            int x = (int)(key.GetValue("WindowX") ?? 100);
            int y = (int)(key.GetValue("WindowY") ?? 100);
            int w = (int)(key.GetValue("WindowWidth") ?? 1000);
            int h = (int)(key.GetValue("WindowHeight") ?? 600);
            return (x, y, w, h);
        }
        catch { return (100, 100, 1000, 600); }
    }

    public static void SetWindowPosition(int x, int y, int width, int height)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(SettingsKeyPath);
            if (key == null) return;
            key.SetValue("WindowX", x, RegistryValueKind.DWord);
            key.SetValue("WindowY", y, RegistryValueKind.DWord);
            key.SetValue("WindowWidth", width, RegistryValueKind.DWord);
            key.SetValue("WindowHeight", height, RegistryValueKind.DWord);
        }
        catch { }
    }

    public static double GetSplitterPosition()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(SettingsKeyPath);
            int pos = (int)(key?.GetValue("SplitterPos") ?? 250);
            return pos;
        }
        catch { return 250; }
    }

    public static void SetSplitterPosition(double pos)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(SettingsKeyPath);
            key?.SetValue("SplitterPos", (int)pos, RegistryValueKind.DWord);
        }
        catch { }
    }
}

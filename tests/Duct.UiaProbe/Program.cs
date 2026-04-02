// UIA Probe — launches a persistent Duct app and walks its UIA tree from an external process.
// Uses FlaUI (COM IUIAutomation3) — the same API real screen readers and test tools use.
// Tests multiple traversal strategies to identify what works across the DesktopChildSiteBridge.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

UiaProbe.Run(args);

static class UiaProbe
{
    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc callback, IntPtr lParam);
    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hwnd, StringBuilder lpClassName, int nMaxCount);

    public static void Run(string[] args)
    {
        var platform = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "ARM64",
            _ => "x64",
        };

        var repoRoot = FindRepoRoot();
        var appExe = Path.Combine(repoRoot, "tests", "Duct.UiaTestApp", "bin", platform,
            "Debug", "net9.0-windows10.0.22621.0", "Duct.UiaTestApp.exe");

        if (!File.Exists(appExe))
        {
            Console.Error.WriteLine($"UiaTestApp not built at: {appExe}");
            Console.Error.WriteLine($"Run: dotnet build tests/Duct.UiaTestApp -p:Platform={platform}");
            Environment.Exit(1);
        }

        Console.WriteLine("=== Duct UIA Probe ===");
        Console.WriteLine($"App: {appExe}");

        using var automation = new UIA3Automation();
        var psi = new ProcessStartInfo(appExe) { UseShellExecute = false };
        using var process = Process.Start(psi)!;
        Console.WriteLine($"PID: {process.Id} — waiting 3s...");
        Thread.Sleep(3000);

        if (process.HasExited)
        {
            Console.WriteLine($"*** App exited with code {process.ExitCode}");
            Environment.Exit(1);
        }

        AutomationElement? mainWindow = null;
        try
        {
            var flaApp = Application.Attach(process);
            mainWindow = flaApp.GetMainWindow(automation, TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FlaUI Attach: {ex.Message}");
        }

        if (mainWindow is null)
        {
            Console.WriteLine("*** FAIL: Could not find window.");
            TryKill(process);
            Environment.Exit(1);
        }

        Console.WriteLine($"\n=== Main Window ===");
        Console.WriteLine($"  ControlType: {mainWindow.ControlType}");
        Console.WriteLine($"  Name: '{SafeName(mainWindow)}'");
        Console.WriteLine($"  Class: '{SafeClass(mainWindow)}'");
        try { Console.WriteLine($"  FrameworkId: '{mainWindow.Properties.FrameworkId.ValueOrDefault}'"); } catch { }
        try { Console.WriteLine($"  BoundingRect: {mainWindow.BoundingRectangle}"); } catch { }

        // ── Method 1: Standard FindAllChildren ───────────────────
        Console.WriteLine("\n=== Method 1: FindAllChildren ===");
        WalkTree(mainWindow, 0, 15);

        // ── Method 2: RawViewWalker ──────────────────────────────
        Console.WriteLine("\n=== Method 2: RawViewWalker ===");
        var rawWalker = automation.TreeWalkerFactory.GetRawViewWalker();
        WalkTreeWalker(rawWalker, mainWindow, 0, 15);

        // ── Method 3: ControlViewWalker ──────────────────────────
        Console.WriteLine("\n=== Method 3: ControlViewWalker ===");
        var controlWalker = automation.TreeWalkerFactory.GetControlViewWalker();
        WalkTreeWalker(controlWalker, mainWindow, 0, 15);

        // ── Method 4: FindAll Descendants ────────────────────────
        Console.WriteLine("\n=== Method 4: FindAll Descendants ===");
        try
        {
            var all = mainWindow.FindAll(TreeScope.Descendants,
                FlaUI.Core.Conditions.TrueCondition.Default);
            Console.WriteLine($"Total descendants: {all.Length}");
            foreach (var e in all)
                PrintElementShort(e, "  ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  FindAll failed: {ex.Message}");
        }

        // ── Method 5: HWND enumeration + FromHandle ─────────────
        Console.WriteLine("\n=== Method 5: HWND Enumeration + FromHandle ===");
        try
        {
            var mainHwnd = mainWindow.Properties.NativeWindowHandle.ValueOrDefault;
            Console.WriteLine($"Main HWND: {mainHwnd}");

            var childHwnds = EnumChildHwnds(mainHwnd);
            Console.WriteLine($"Child HWNDs: {childHwnds.Count}");

            foreach (var childHwnd in childHwnds)
            {
                var cls = GetWin32ClassName(childHwnd);
                Console.WriteLine($"\n  HWND {childHwnd}: Class='{cls}'");

                try
                {
                    var childElement = automation.FromHandle(childHwnd);
                    Console.WriteLine($"  UIA: [{childElement.ControlType}] Name='{SafeName(childElement)}'");
                    Console.WriteLine($"  Walking children:");
                    WalkTree(childElement, 2, 8);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  <FromHandle error: {ex.Message}>");
                }

                var grandchildren = EnumChildHwnds(childHwnd);
                foreach (var gcHwnd in grandchildren)
                {
                    var gcCls = GetWin32ClassName(gcHwnd);
                    Console.WriteLine($"\n    Sub-HWND {gcHwnd}: Class='{gcCls}'");

                    try
                    {
                        var gcElement = automation.FromHandle(gcHwnd);
                        Console.WriteLine($"    UIA: [{gcElement.ControlType}] Name='{SafeName(gcElement)}'");
                        Console.WriteLine($"    Walking children:");
                        WalkTree(gcElement, 3, 8);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"    <FromHandle error: {ex.Message}>");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  HWND approach failed: {ex.Message}");
        }

        TryKill(process);
        Console.WriteLine("\n=== Done ===");
    }

    private static List<IntPtr> EnumChildHwnds(IntPtr parent)
    {
        var list = new List<IntPtr>();
        EnumChildWindows(parent, (hwnd, _) => { list.Add(hwnd); return true; }, IntPtr.Zero);
        return list;
    }

    private static string GetWin32ClassName(IntPtr hwnd)
    {
        var sb = new StringBuilder(256);
        GetClassName(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static void WalkTree(AutomationElement element, int depth, int maxDepth)
    {
        if (depth > maxDepth) return;
        var indent = new string(' ', depth * 2);
        PrintElementShort(element, indent);
        try
        {
            foreach (var child in element.FindAllChildren())
                WalkTree(child, depth + 1, maxDepth);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{indent}  <error: {ex.Message}>");
        }
    }

    private static void WalkTreeWalker(ITreeWalker walker, AutomationElement element, int depth, int maxDepth)
    {
        if (depth > maxDepth) return;
        var indent = new string(' ', depth * 2);
        PrintElementShort(element, indent);
        try
        {
            var child = walker.GetFirstChild(element);
            while (child is not null)
            {
                WalkTreeWalker(walker, child, depth + 1, maxDepth);
                child = walker.GetNextSibling(child);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{indent}  <error: {ex.Message}>");
        }
    }

    private static void PrintElementShort(AutomationElement e, string prefix)
    {
        var name = SafeName(e) ?? "";
        if (name.Length > 60) name = name[..57] + "...";
        Console.WriteLine($"{prefix}[{e.ControlType}] Name='{name}' AID='{SafeAutomationId(e)}' Class='{SafeClass(e)}'");
    }

    private static string? SafeName(AutomationElement e)
    { try { return e.Properties.Name.ValueOrDefault; } catch { return null; } }
    private static string? SafeAutomationId(AutomationElement e)
    { try { return e.Properties.AutomationId.ValueOrDefault; } catch { return null; } }
    private static string? SafeClass(AutomationElement e)
    { try { return e.Properties.ClassName.ValueOrDefault; } catch { return null; } }

    private static void TryKill(Process p)
    { try { if (!p.HasExited) p.Kill(); } catch { } }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Duct.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not find Duct.sln");
    }
}

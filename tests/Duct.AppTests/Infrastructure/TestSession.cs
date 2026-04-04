using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace Duct.AppTests.Infrastructure;

/// <summary>
/// Manages the Appium WindowsDriver session for the test assembly.
/// Uses a two-step approach: launches the Host app as a process, then creates
/// a WinAppDriver Desktop session and attaches to the app window. This avoids
/// WinAppDriver's app-launch session which can fail with WinUI3 apps.
/// </summary>
public class TestSession
{
    private const string WinAppDriverUrl = "http://127.0.0.1:4723";

    private static WindowsDriver<WindowsElement>? _session;
    private static Process? _appProcess;

    public static WindowsDriver<WindowsElement> Session =>
        _session ?? throw new InvalidOperationException(
            "Test session has not been initialized. Ensure [AssemblyInitialize] has run.");

    /// <summary>
    /// Called by InteractiveTests.ClassInitialize to start the Appium session.
    /// Not [AssemblyInitialize] — self-test batch doesn't need Appium.
    /// </summary>
    public static void AssemblyInit(TestContext context)
    {
        WinAppDriverHelper.Start();

        // Step 1: Launch the Host app as a regular process
        var exePath = FindHostExe();
        Console.WriteLine($"Host app: {exePath}");

        _appProcess = Process.Start(new ProcessStartInfo(exePath)
        {
            UseShellExecute = false,
        });
        Console.WriteLine($"Host app launched (PID {_appProcess?.Id}).");

        // Wait for the app to initialize
        Thread.Sleep(3000);

        // Step 2: Create a Desktop session and find the app window
        var desktopOptions = new AppiumOptions();
        desktopOptions.AddAdditionalCapability("app", "Root");
        desktopOptions.AddAdditionalCapability("deviceName", "WindowsPC");

        using var desktopSession = new WindowsDriver<WindowsElement>(
            new Uri(WinAppDriverUrl), desktopOptions);

        // Find the Host app window by title
        var appWindow = desktopSession.FindElementByName("Duct Test Host");
        var appWindowHandle = appWindow.GetAttribute("NativeWindowHandle");
        var hwnd = int.Parse(appWindowHandle).ToString("x"); // hex for WinAppDriver

        // Step 3: Create a session attached to the app window
        var appOptions = new AppiumOptions();
        appOptions.AddAdditionalCapability("appTopLevelWindow", $"0x{hwnd}");
        appOptions.AddAdditionalCapability("deviceName", "WindowsPC");

        _session = new WindowsDriver<WindowsElement>(new Uri(WinAppDriverUrl), appOptions);
        _session.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

        Console.WriteLine("WindowsDriver session attached to Host app.");
    }

    public static void AssemblyCleanup()
    {
        if (_session != null)
        {
            try { _session.Quit(); }
            catch (Exception ex) { Console.WriteLine($"Warning: session close failed: {ex.Message}"); }
            finally { _session = null; }
        }

        if (_appProcess != null)
        {
            try
            {
                if (!_appProcess.HasExited)
                {
                    _appProcess.Kill();
                    _appProcess.WaitForExit(5000);
                }
            }
            catch { }
            finally
            {
                _appProcess.Dispose();
                _appProcess = null;
            }
        }

        WinAppDriverHelper.Stop();
    }

    private static string FindHostExe()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "Duct.sln")))
            dir = Path.GetDirectoryName(dir);

        if (dir == null)
            throw new DirectoryNotFoundException("Could not find repo root (Duct.sln)");

        var platform = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "ARM64",
            _ => "x64"
        };

        var exe = Path.Combine(dir, "tests", "Duct.AppTests.Host", "bin", platform,
            "Debug", "net9.0-windows10.0.22621.0", "Duct.AppTests.Host.exe");

        if (!File.Exists(exe))
            throw new FileNotFoundException($"Build the Host app first. Expected: {exe}");

        return exe;
    }
}

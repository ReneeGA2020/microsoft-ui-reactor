using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using System.Diagnostics;

namespace Duct.UITests;

/// <summary>
/// Manages the WinAppDriver session and Duct.TestApp lifecycle for UI tests.
/// Starts WinAppDriver if not running, launches the TestApp, and provides
/// the Appium session to all tests.
/// </summary>
[TestClass]
public class SessionManager
{
    private const string WinAppDriverUrl = "http://127.0.0.1:4723";

    private static WindowsDriver<WindowsElement>? _session;
    private static Process? _winAppDriverProcess;


    public static WindowsDriver<WindowsElement> Session =>
        _session ?? throw new InvalidOperationException("Session not initialized. Ensure [AssemblyInitialize] ran.");

    [AssemblyInitialize]
    public static void Setup(TestContext context)
    {
        // Build the test app first
        var testAppDir = FindTestAppDirectory();
        var testAppExe = Path.Combine(testAppDir, "Duct.TestApp.exe");

        if (!File.Exists(testAppExe))
            throw new FileNotFoundException($"Duct.TestApp.exe not found at {testAppExe}. Build the TestApp first.");

        // Start WinAppDriver if not already running
        EnsureWinAppDriverRunning();

        // Launch the test app
        var appiumOptions = new AppiumOptions();
        appiumOptions.AddAdditionalCapability("app", testAppExe);
        appiumOptions.AddAdditionalCapability("deviceName", "WindowsPC");
        appiumOptions.AddAdditionalCapability("ms:waitForAppLaunch", "5");

        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                _session = new WindowsDriver<WindowsElement>(new Uri(WinAppDriverUrl), appiumOptions);
                break;
            }
            catch (Exception ex) when (attempt < 2)
            {
                Console.WriteLine($"Session init attempt {attempt + 1} failed: {ex.Message}");
                Thread.Sleep(2000);
            }
        }

        Assert.IsNotNull(_session, "Failed to create WinAppDriver session.");
        _session!.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

        // Wait for the app to be ready
        Thread.Sleep(2000);
    }

    [AssemblyCleanup]
    public static void Cleanup()
    {
        try
        {
            _session?.CloseApp();
            _session?.Quit();
        }
        catch { }
        _session = null;
    }

    private static string FindTestAppDirectory()
    {
        // Walk up from the test output dir to find the repo root
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "Duct.sln")))
            dir = Path.GetDirectoryName(dir);

        if (dir == null)
            throw new DirectoryNotFoundException("Could not find repo root (Duct.sln)");

        // Determine platform from current process architecture
        var platform = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.X64 => "x64",
            System.Runtime.InteropServices.Architecture.Arm64 => "ARM64",
            _ => "x64"
        };

        return Path.Combine(dir, "Duct.TestApp", "bin", platform, "Debug", "net8.0-windows10.0.22621.0");
    }

    private static void EnsureWinAppDriverRunning()
    {
        // Check if WinAppDriver is already running
        var existing = Process.GetProcessesByName("WinAppDriver");
        if (existing.Length > 0) return;

        // Try to start it
        var paths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windows Application Driver", "WinAppDriver.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Windows Application Driver", "WinAppDriver.exe"),
        };

        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                _winAppDriverProcess = Process.Start(new ProcessStartInfo(path)
                {
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Minimized,
                });
                Thread.Sleep(3000); // Give it time to start
                return;
            }
        }

        throw new FileNotFoundException(
            "WinAppDriver not found. Install from https://github.com/microsoft/WinAppDriver/releases");
    }
}

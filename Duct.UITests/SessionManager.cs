using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Duct.UITests;

/// <summary>
/// Manages the Duct.TestApp self-test lifecycle.
///
/// WinUI 3 Desktop XAML islands cannot be accessed via cross-process UIA
/// (WinAppDriver, FlaUI, System.Windows.Automation all throw E_UNEXPECTED on the
/// DesktopChildSiteBridge boundary). WinUI 3 apps also cannot be hosted inside
/// a standard test runner process (missing manifests/COM initialization).
///
/// Instead, we run the TestApp with a --self-test flag. The app launches normally,
/// runs its VisualTreeHelper-based assertions against its own UI from the UI thread,
/// writes results to stdout as TAP (Test Anything Protocol), and exits.
/// The MSTest tests here parse those results.
/// </summary>
[TestClass]
public class SessionManager
{
    private static string? _testOutput;
    private static int _exitCode;
    private static readonly Dictionary<string, bool> _testResults = new();

    public static IReadOnlyDictionary<string, bool> TestResults => _testResults;

    [AssemblyInitialize]
    public static void Setup(TestContext context)
    {
        var testAppExe = FindTestAppExe();

        var psi = new ProcessStartInfo(testAppExe, "--self-test")
        {
            WorkingDirectory = Path.GetDirectoryName(testAppExe)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var proc = Process.Start(psi)!;
        _testOutput = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit(60_000);
        _exitCode = proc.ExitCode;

        Console.WriteLine("=== TestApp self-test output ===");
        Console.WriteLine(_testOutput);
        if (!string.IsNullOrEmpty(stderr))
        {
            Console.WriteLine("=== stderr ===");
            Console.WriteLine(stderr);
        }
        Console.WriteLine($"=== Exit code: {_exitCode} ===");

        // Parse TAP-style output: "ok <name>" or "not ok <name> - <reason>"
        foreach (var line in _testOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("ok "))
            {
                var name = trimmed[3..].Split(" - ")[0].Trim();
                _testResults[name] = true;
            }
            else if (trimmed.StartsWith("not ok "))
            {
                var name = trimmed[7..].Split(" - ")[0].Trim();
                _testResults[name] = false;
            }
        }
    }

    public static void AssertTestPassed(string testName)
    {
        if (!_testResults.TryGetValue(testName, out var passed))
            Assert.Fail($"Test '{testName}' was not reported by the self-test runner. " +
                        $"Available tests: {string.Join(", ", _testResults.Keys)}");
        Assert.IsTrue(passed, $"Self-test '{testName}' failed. Check output for details.");
    }

    private static string FindTestAppExe()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "Duct.sln")))
            dir = Path.GetDirectoryName(dir);

        if (dir == null)
            throw new DirectoryNotFoundException("Could not find repo root (Duct.sln)");

        var platform = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.X64 => "x64",
            System.Runtime.InteropServices.Architecture.Arm64 => "ARM64",
            _ => "x64"
        };

        var exe = Path.Combine(dir, "Duct.TestApp", "bin", platform, "Debug",
            "net8.0-windows10.0.22621.0", "Duct.TestApp.exe");

        if (!File.Exists(exe))
            throw new FileNotFoundException($"Build the TestApp first: {exe}");

        return exe;
    }
}

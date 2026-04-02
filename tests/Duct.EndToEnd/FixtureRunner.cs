using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Duct.EndToEnd;

/// <summary>
/// Launches the EndToEnd.App exe for each fixture, captures TAP output, and reports results.
/// Each fixture runs in its own process for fault isolation.
/// </summary>
internal static class FixtureRunner
{
    private static string? _exePath;

    public static void EnsureExePath()
    {
        if (_exePath is not null) return;

        // Walk up from test assembly to find repo root
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Duct.sln")))
            dir = dir.Parent;

        if (dir is null)
            throw new DirectoryNotFoundException("Could not find Duct.sln in parent directories");

        var platform = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "ARM64",
            _ => "x64",
        };

        _exePath = Path.Combine(dir.FullName,
            "tests", "Duct.EndToEnd.App", "bin", platform, "Debug",
            "net9.0-windows10.0.22621.0", "Duct.EndToEnd.App.exe");

        if (!File.Exists(_exePath))
            throw new FileNotFoundException(
                $"EndToEnd.App not built. Run: dotnet build tests/Duct.EndToEnd.App -p:Platform={platform}\n" +
                $"Expected at: {_exePath}");
    }

    public static void RunFixture(string fixtureName)
    {
        EnsureExePath();

        var psi = new ProcessStartInfo(_exePath!, $"--test --fixture {fixtureName}")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false, // WinUI needs a window
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process for fixture {fixtureName}");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();

        if (!process.WaitForExit(30_000))
        {
            process.Kill();
            Assert.Fail($"Fixture '{fixtureName}' timed out after 30s");
        }

        // Parse TAP output
        var results = new Dictionary<string, bool>();
        foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("ok "))
            {
                var name = trimmed[3..].Split(" - ")[0].Trim();
                results[name] = true;
            }
            else if (trimmed.StartsWith("not ok "))
            {
                var name = trimmed[7..].Split(" - ")[0].Trim();
                results[name] = false;
            }
        }

        // Report
        if (process.ExitCode == 2)
            Assert.Fail($"Fixture '{fixtureName}' not found. stderr: {stderr}");

        if (results.Count == 0)
            Assert.Fail($"Fixture '{fixtureName}' produced no TAP output.\nstdout: {stdout}\nstderr: {stderr}");

        var failures = results.Where(r => !r.Value).Select(r => r.Key).ToList();
        if (failures.Count > 0)
        {
            Assert.Fail($"Fixture '{fixtureName}' failed {failures.Count} check(s):\n" +
                string.Join("\n", failures.Select(f => $"  - {f}")) +
                $"\n\nFull output:\n{stdout}" +
                (stderr.Length > 0 ? $"\nstderr:\n{stderr}" : ""));
        }

        // All checks passed
        Assert.AreEqual(0, process.ExitCode,
            $"Fixture '{fixtureName}' exited with code {process.ExitCode}.\nstdout: {stdout}\nstderr: {stderr}");
    }
}

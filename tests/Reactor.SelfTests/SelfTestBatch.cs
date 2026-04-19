using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.UI.Reactor.SelfTests;

/// <summary>
/// Runs all in-process self-test fixtures in a single Host app launch and reports
/// one [TestMethod] per fixture. The Host mounts each fixture, runs assertions via
/// VisualTreeHelper, and emits TAP to stdout. We parse the TAP stream, split it by
/// `# Running: <fixture>` boundaries, and pair each fixture with pass/fail.
///
/// Fixture names are discovered at test-discovery time by launching the Host with
/// `--list-fixtures` (a fast-path that prints names and exits without starting WinUI).
/// </summary>
[TestClass]
public class SelfTestBatch
{
    // Per-fixture aggregated outcome, populated by ClassInitialize.
    // Key = fixture name; Value = (passed, joined failure reasons).
    private static readonly ConcurrentDictionary<string, (bool Passed, string Detail)> _byFixture = new();
    private static string _fullOutput = "";
    private static bool _initialized;
    private static string? _initError;

    [ClassInitialize]
    public static void RunSelfTests(TestContext context)
    {
        var exe = FindHostExe();
        var psi = new ProcessStartInfo(exe, "--self-test")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(300_000); // 5 minute timeout

        _fullOutput = stdout;
        if (!string.IsNullOrEmpty(stderr))
            _fullOutput += "\n--- stderr ---\n" + stderr;

        // Parse TAP: track the current fixture via `# Running: <name>` markers,
        // then bucket every `ok` / `not ok` line that follows into that fixture
        // until the next marker (or end of stream).
        string? current = null;
        var failuresForCurrent = new List<string>();
        var sawChecksForCurrent = false;

        void Flush()
        {
            if (current is null) return;
            var passed = failuresForCurrent.Count == 0 && sawChecksForCurrent;
            var detail = failuresForCurrent.Count == 0
                ? (sawChecksForCurrent ? "" : "fixture emitted no TAP checks")
                : string.Join("\n", failuresForCurrent);
            _byFixture[current] = (passed, detail);
        }

        foreach (var raw in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.StartsWith("# Running: "))
            {
                Flush();
                current = line["# Running: ".Length..].Trim();
                failuresForCurrent = new List<string>();
                sawChecksForCurrent = false;
            }
            else if (line.StartsWith("ok "))
            {
                sawChecksForCurrent = true;
            }
            else if (line.StartsWith("not ok "))
            {
                sawChecksForCurrent = true;
                var rest = line[7..].Trim();
                // SelfTestRunner emits fixture-level crashes as "not ok N FIXTURE_CRASH - ...".
                // These appear BEFORE a `# Running:` marker for that fixture (or after the
                // marker if the fixture was created then threw). Either way, attribute to
                // `current` if set; otherwise detect via trailing "_CRASH" suffix.
                if (current is null)
                {
                    // Crash before any fixture marker: try to recover fixture name.
                    var nameEnd = rest.IndexOf(' ');
                    var name = nameEnd > 0 ? rest[..nameEnd] : rest;
                    if (name.EndsWith("_CRASH", StringComparison.Ordinal))
                        name = name[..^"_CRASH".Length];
                    _byFixture[name] = (false, rest);
                }
                else
                {
                    failuresForCurrent.Add(rest);
                }
            }
        }
        Flush();

        _initialized = true;

        if (process.ExitCode != 0 && _byFixture.IsEmpty)
        {
            _initError = $"Self-test process exited with code {process.ExitCode} but produced no parsable TAP output.\n{_fullOutput}";
        }
    }

    public static IEnumerable<object[]> AllFixtures => FixtureNames.Value.Select(n => new object[] { n });

    [DataTestMethod]
    [DynamicData(nameof(AllFixtures))]
    public void Fixture(string name)
    {
        Assert.IsTrue(_initialized, "Self-test batch did not run.");
        if (_initError is not null)
            Assert.Fail(_initError);

        if (!_byFixture.TryGetValue(name, out var result))
            Assert.Fail($"Fixture '{name}' was not reported by the Host. Full output:\n{_fullOutput}");

        if (!result.Passed)
            Assert.Fail(result.Detail);
    }

    // -- Discovery: one-shot Host launch to list fixture names -----------------

    private static readonly Lazy<string[]> FixtureNames = new(LoadFixtureNames);

    private static string[] LoadFixtureNames()
    {
        var exe = FindHostExe();
        var psi = new ProcessStartInfo(exe, "--list-fixtures")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        using var p = Process.Start(psi)!;
        var stdout = p.StandardOutput.ReadToEnd();
        p.WaitForExit(30_000);
        return stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToArray();
    }

    private static string FindHostExe()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "Reactor.sln")))
            dir = Path.GetDirectoryName(dir);

        if (dir == null)
            throw new DirectoryNotFoundException("Could not find repo root (Reactor.sln)");

        var platform = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "ARM64",
            _ => "x64"
        };

        var exe = Path.Combine(dir, "tests", "Reactor.AppTests.Host", "bin", platform,
            "Debug", "net9.0-windows10.0.22621.0", "Reactor.AppTests.Host.exe");

        if (!File.Exists(exe))
            throw new FileNotFoundException($"Host app not built. Expected: {exe}");

        return exe;
    }
}

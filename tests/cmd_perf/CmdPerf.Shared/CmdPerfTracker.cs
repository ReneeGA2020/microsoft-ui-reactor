using System.Diagnostics;
using System.Text;

namespace CmdPerf.Shared;

public sealed class CmdPerfTracker
{
    private double _mountTimeMs;
    private readonly List<(string Flag, double Ms)> _toggleSamples = new();
    private readonly List<double> _bulkToggleSamples = new();
    private long _memoryBaseline;
    private long _memoryAfterToggles;
    private readonly Stopwatch _sw = new();

    public double LastToggleMs { get; private set; }
    public double MountTimeMs => _mountTimeMs;

    // ── Mount timing ────────────────────────────────────────────

    public void BeginMount() => _sw.Restart();

    public void EndMount()
    {
        _sw.Stop();
        _mountTimeMs = _sw.Elapsed.TotalMilliseconds;
        _memoryBaseline = Process.GetCurrentProcess().WorkingSet64;
    }

    // ── Toggle timing ───────────────────────────────────────────

    public void BeginToggle() => _sw.Restart();

    public void EndToggle(string flagName)
    {
        _sw.Stop();
        var ms = _sw.Elapsed.TotalMilliseconds;
        LastToggleMs = ms;
        _toggleSamples.Add((flagName, ms));
    }

    public void EndBulkToggle()
    {
        _sw.Stop();
        var ms = _sw.Elapsed.TotalMilliseconds;
        LastToggleMs = ms;
        _bulkToggleSamples.Add(ms);
    }

    public void RecordMemoryAfterToggles()
    {
        _memoryAfterToggles = Process.GetCurrentProcess().WorkingSet64;
    }

    // ── Report ──────────────────────────────────────────────────

    public string GetReport(string appName, string scenario)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== {appName} ({scenario}) ===");
        sb.AppendLine($"Commands:        {CommandSet.All.Length}");
        sb.AppendLine($"Mount Time:      {_mountTimeMs:F2} ms");
        sb.AppendLine($"Memory Baseline: {_memoryBaseline / (1024.0 * 1024):F1} MB");

        if (_toggleSamples.Count > 0)
        {
            var allMs = _toggleSamples.Select(t => t.Ms).ToList();
            allMs.Sort();
            sb.AppendLine($"Toggle Count:    {_toggleSamples.Count}");
            sb.AppendLine($"Toggle Avg:      {allMs.Average():F3} ms");
            sb.AppendLine($"Toggle P50:      {Percentile(allMs, 50):F3} ms");
            sb.AppendLine($"Toggle P95:      {Percentile(allMs, 95):F3} ms");
            sb.AppendLine($"Toggle P99:      {Percentile(allMs, 99):F3} ms");
            sb.AppendLine($"Toggle Max:      {allMs.Max():F3} ms");

            // Per-flag breakdown
            var byFlag = _toggleSamples.GroupBy(t => t.Flag);
            foreach (var g in byFlag)
            {
                var flagMs = g.Select(t => t.Ms).ToList();
                sb.AppendLine($"  {g.Key,-20} avg={flagMs.Average():F3} ms  max={flagMs.Max():F3} ms  n={flagMs.Count}");
            }
        }

        if (_bulkToggleSamples.Count > 0)
        {
            var sorted = _bulkToggleSamples.ToList();
            sorted.Sort();
            sb.AppendLine($"Bulk Toggle Count: {sorted.Count}");
            sb.AppendLine($"Bulk Toggle Avg:   {sorted.Average():F3} ms");
            sb.AppendLine($"Bulk Toggle P50:   {Percentile(sorted, 50):F3} ms");
            sb.AppendLine($"Bulk Toggle P95:   {Percentile(sorted, 95):F3} ms");
            sb.AppendLine($"Bulk Toggle Max:   {sorted.Max():F3} ms");
        }

        if (_memoryAfterToggles > 0)
            sb.AppendLine($"Memory After:    {_memoryAfterToggles / (1024.0 * 1024):F1} MB");

        return sb.ToString();
    }

    public void WriteReportFile(string appName, string scenario)
    {
        var report = GetReport(appName, scenario);
        var path = Path.Combine(AppContext.BaseDirectory, $"{appName}.{scenario}.report.txt");
        File.WriteAllText(path, report);
        Console.WriteLine(report);
    }

    private static double Percentile(List<double> sorted, double pct)
    {
        if (sorted.Count == 0) return 0;
        int idx = (int)Math.Ceiling(pct / 100.0 * sorted.Count) - 1;
        return sorted[Math.Clamp(idx, 0, sorted.Count - 1)];
    }
}

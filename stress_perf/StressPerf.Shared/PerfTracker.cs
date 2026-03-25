using System.Diagnostics;
using System.Text;

namespace StressPerf.Shared;

public sealed class PerfTracker
{
    private readonly Stopwatch _wallClock = Stopwatch.StartNew();
    private readonly Stopwatch _updateSw = new();
    private int _frameCount;
    private double _lastSampleTime;
    private double _currentFps;
    private double _lastUpdateMs;

    private readonly List<double> _fpsSamples = new();
    private readonly List<long> _memorySamples = new();
    private readonly List<double> _updateTimeSamples = new();

    public double CurrentFps => _currentFps;
    public double LastUpdateMs => _lastUpdateMs;
    public long CurrentMemoryMB => Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);

    /// <summary>
    /// Call from CompositionTarget.Rendering to count composed frames.
    /// </summary>
    public void FrameRendered()
    {
        _frameCount++;
        double now = _wallClock.Elapsed.TotalSeconds;
        double elapsed = now - _lastSampleTime;
        if (elapsed >= 1.0)
        {
            _currentFps = _frameCount / elapsed;
            _fpsSamples.Add(_currentFps);
            _memorySamples.Add(Process.GetCurrentProcess().WorkingSet64);
            _frameCount = 0;
            _lastSampleTime = now;
        }
    }

    /// <summary>
    /// Call before updating data + UI.
    /// </summary>
    public void BeginUpdate() => _updateSw.Restart();

    /// <summary>
    /// Call after updating data + UI.
    /// </summary>
    public void EndUpdate()
    {
        _updateSw.Stop();
        _lastUpdateMs = _updateSw.Elapsed.TotalMilliseconds;
        _updateTimeSamples.Add(_lastUpdateMs);
    }

    public double ElapsedSeconds => _wallClock.Elapsed.TotalSeconds;

    public string GetReport(string appName, double percent)
    {
        if (_fpsSamples.Count == 0) return "No data collected.";

        var sb = new StringBuilder();
        sb.AppendLine($"=== {appName} ===");
        sb.AppendLine($"Duration:    {_wallClock.Elapsed.TotalSeconds:F1}s");
        sb.AppendLine($"Percent:     {percent:F0}%");
        sb.AppendLine($"Avg FPS:     {_fpsSamples.Average():F1}");
        sb.AppendLine($"Min FPS:     {_fpsSamples.Min():F1}");
        sb.AppendLine($"Max FPS:     {_fpsSamples.Max():F1}");
        if (_updateTimeSamples.Count > 0)
        {
            sb.AppendLine($"Avg Update:  {_updateTimeSamples.Average():F1} ms");
            sb.AppendLine($"Max Update:  {_updateTimeSamples.Max():F1} ms");
        }
        sb.AppendLine($"Avg Memory:  {_memorySamples.Average() / (1024 * 1024):F1} MB");
        sb.AppendLine($"Peak Memory: {_memorySamples.Max() / (1024 * 1024):F1} MB");
        return sb.ToString();
    }

    /// <summary>
    /// Write report to a file next to the executable.
    /// </summary>
    public void WriteReportFile(string appName, double percent)
    {
        var report = GetReport(appName, percent);
        var path = Path.Combine(AppContext.BaseDirectory, $"{appName}.report.txt");
        File.WriteAllText(path, report);
    }
}

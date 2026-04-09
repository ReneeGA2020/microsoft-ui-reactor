using NetPulse.Native;

namespace NetPulse;

/// <summary>
/// Runs 4 independent background polling threads that call setState directly
/// from ThreadPool threads. Hook state is thread-safe (locked read/write),
/// and RequestRender coalesces via Interlocked CAS + DispatcherQueue.TryEnqueue.
///
/// Threads 3 and 4 maintain mutable state across ticks, so they use
/// Interlocked guards to prevent re-entrant Timer callbacks from corrupting
/// their dictionaries.
/// </summary>
sealed class NetworkMonitor : IDisposable
{
    readonly Timer _tcpTimer;
    readonly Timer _udpTimer;
    readonly Timer _ifaceTimer;
    readonly Timer _sparklineTimer;

    // Callbacks — called directly from background threads
    readonly Action<TcpConn[]> _onTcpUpdate;
    readonly Action<UdpEndpoint[]> _onUdpUpdate;
    readonly Action<TrafficSample> _onTrafficSample;
    readonly Action<SparklineEntry[]> _onSparklineUpdate;

    // Re-entrancy guards — prevents overlapping Timer callbacks when
    // P/Invoke calls take longer than the poll interval
    int _tcpRunning;       // 0 or 1 — thread 1
    int _udpRunning;       // 0 or 1 — thread 2

    // Interface delta tracking (accessed only from thread 3, guarded by _ifaceRunning)
    Dictionary<uint, (ulong InOctets, ulong OutOctets, long TimestampMs)> _prevIfaceStats = new();
    int _ifaceRunning;  // 0 or 1 — re-entrancy guard

    // Sparkline state history (accessed only from thread 4, guarded by _sparklineRunning)
    readonly Dictionary<string, (string Label, List<int> History)> _connHistory = new();
    int _sparklineRunning;  // 0 or 1 — re-entrancy guard
    const int SparklineMaxPoints = 40;

    bool _disposed;

    /// <summary>Polling interval in ms. Lower = more stress on reconciler.</summary>
    public const int PollIntervalMs = 33;

    public NetworkMonitor(
        Action<TcpConn[]> onTcpUpdate,
        Action<UdpEndpoint[]> onUdpUpdate,
        Action<TrafficSample> onTrafficSample,
        Action<SparklineEntry[]> onSparklineUpdate)
    {
        _onTcpUpdate = onTcpUpdate;
        _onUdpUpdate = onUdpUpdate;
        _onTrafficSample = onTrafficSample;
        _onSparklineUpdate = onSparklineUpdate;

        int p = PollIntervalMs;
        int stagger = p / 4;

        _tcpTimer = new Timer(PollTcp, null, 0, p);
        _udpTimer = new Timer(PollUdp, null, stagger, p);
        _ifaceTimer = new Timer(PollInterfaces, null, stagger * 2, p);
        _sparklineTimer = new Timer(PollSparklines, null, stagger * 3, p);
    }

    // ── Thread 1: TCP (guarded — P/Invoke can exceed poll interval) ──

    void PollTcp(object? _)
    {
        if (_disposed) return;
        if (Interlocked.CompareExchange(ref _tcpRunning, 1, 0) != 0) return;
        try
        {
            var connections = IpHelper.GetTcpConnections();
            _onTcpUpdate(connections);
        }
        catch { /* swallow polling failures */ }
        finally { Interlocked.Exchange(ref _tcpRunning, 0); }
    }

    // ── Thread 2: UDP (guarded — P/Invoke can exceed poll interval) ─

    void PollUdp(object? _)
    {
        if (_disposed) return;
        if (Interlocked.CompareExchange(ref _udpRunning, 1, 0) != 0) return;
        try
        {
            var endpoints = IpHelper.GetUdpEndpoints();
            _onUdpUpdate(endpoints);
        }
        catch { /* swallow polling failures */ }
        finally { Interlocked.Exchange(ref _udpRunning, 0); }
    }

    // ── Thread 3: Interface stats (stateful — guarded) ──────────────

    void PollInterfaces(object? _)
    {
        if (_disposed) return;
        // Skip this tick if the previous one is still running
        if (Interlocked.CompareExchange(ref _ifaceRunning, 1, 0) != 0) return;
        try
        {
            var snapshots = IpHelper.GetInterfaceSnapshots();
            long now = Environment.TickCount64;

            double totalInRate = 0, totalOutRate = 0;
            var newPrev = new Dictionary<uint, (ulong, ulong, long)>();

            foreach (var snap in snapshots)
            {
                newPrev[snap.Index] = (snap.InOctets, snap.OutOctets, now);

                if (_prevIfaceStats.TryGetValue(snap.Index, out var prev))
                {
                    double elapsed = (now - prev.TimestampMs) / 1000.0;
                    if (elapsed > 0.001)
                    {
                        totalInRate += (snap.InOctets - prev.InOctets) / elapsed;
                        totalOutRate += (snap.OutOctets - prev.OutOctets) / elapsed;
                    }
                }
            }

            _prevIfaceStats = newPrev;

            var sample = new TrafficSample(now, totalInRate, totalOutRate);
            _onTrafficSample(sample);
        }
        catch { /* swallow polling failures */ }
        finally
        {
            Interlocked.Exchange(ref _ifaceRunning, 0);
        }
    }

    // ── Thread 4: Connection sparkline history (stateful — guarded) ─

    void PollSparklines(object? _)
    {
        if (_disposed) return;
        // Skip this tick if the previous one is still running
        if (Interlocked.CompareExchange(ref _sparklineRunning, 1, 0) != 0) return;
        try
        {
            var connections = IpHelper.GetTcpConnections();
            var currentKeys = new HashSet<string>();

            foreach (var conn in connections)
            {
                if (conn.State == TcpState.Listen) continue;

                currentKeys.Add(conn.Key);
                if (!_connHistory.TryGetValue(conn.Key, out var entry))
                {
                    entry = (conn.ShortRemote, new List<int>());
                    _connHistory[conn.Key] = entry;
                }
                entry.History.Add((int)conn.State);
                if (entry.History.Count > SparklineMaxPoints)
                    entry.History.RemoveAt(0);
            }

            foreach (var key in _connHistory.Keys.Where(k => !currentKeys.Contains(k)).ToList())
                _connHistory.Remove(key);

            var snapshot = _connHistory
                .OrderByDescending(kv => kv.Value.History.Count)
                .Take(60)
                .Select(kv => new SparklineEntry(kv.Key, kv.Value.Label, kv.Value.History.ToArray()))
                .ToArray();

            _onSparklineUpdate(snapshot);
        }
        catch { /* swallow polling failures */ }
        finally
        {
            Interlocked.Exchange(ref _sparklineRunning, 0);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _tcpTimer.Dispose();
        _udpTimer.Dispose();
        _ifaceTimer.Dispose();
        _sparklineTimer.Dispose();
    }
}

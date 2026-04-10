using Duct;
using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using NetPulse.Charts;
using static Duct.D3.Charts.D3;
using static Duct.UI;

namespace NetPulse;

/// <summary>
/// NetPulse root component — a reconciler torture test driven by real network data.
///
/// Architecture:
///   4 independent background threads poll NT-level IP Helper APIs at staggered
///   intervals and call setState directly from ThreadPool threads. Hook state
///   is thread-safe (locked reads/writes), and RequestRender uses Interlocked CAS
///   + DispatcherQueue.TryEnqueue for natural coalescing — multiple setState calls
///   between frames produce a single render that sees all updates.
///
/// Reconciler stress vectors:
///   - Keyed list reordering (LIS algorithm in ChildReconciler)
///   - Key churn from connections appearing/disappearing
///   - Large element counts (sparklines: 60 connections × ~50 elements)
///   - Multiple independent setState sources interleaving
///   - Full path geometry regeneration on every traffic sample
/// </summary>
sealed class App : Component
{
    const int TrafficHistoryMax = 200;

    public override Element Render()
    {
        // ── State: 4 independent sources from 4 background threads ──

        var (tcpConns, setTcpConns) = UseState(Array.Empty<TcpConn>(), threadSafe: true);
        var (udpEndpoints, setUdpEndpoints) = UseState(Array.Empty<UdpEndpoint>(), threadSafe: true);
        var (sparklineData, setSparklineData) = UseState(Array.Empty<SparklineEntry>(), threadSafe: true);

        // Traffic history accumulates across renders
        var (trafficHistory, updateTrafficHistory) = UseReducer<IReadOnlyList<TrafficSample>>(
            Array.Empty<TrafficSample>(), threadSafe: true);

        // Read live stats from DuctHost
        var stats = DuctApp.ActiveHost?.Stats ?? default;

        // ── Monitor lifecycle ───────────────────────────────────────

        var monitorRef = UseRef<NetworkMonitor?>(null);

        UseEffect(() =>
        {
            monitorRef.Current = new NetworkMonitor(
                onTcpUpdate: conns => setTcpConns(conns),
                onUdpUpdate: endpoints => setUdpEndpoints(endpoints),
                onTrafficSample: sample =>
                {
                    updateTrafficHistory(prev =>
                    {
                        var list = new List<TrafficSample>(prev.Count + 1);
                        int start = prev.Count >= TrafficHistoryMax ? prev.Count - TrafficHistoryMax + 1 : 0;
                        for (int i = start; i < prev.Count; i++)
                            list.Add(prev[i]);
                        list.Add(sample);
                        return list;
                    });
                },
                onSparklineUpdate: data => setSparklineData(data));

            return () =>
            {
                monitorRef.Current?.Dispose();
                monitorRef.Current = null;
            };
        });

        // ── Derived data ────────────────────────────────────────────

        int tcpActive = 0;
        foreach (var c in tcpConns)
            if (c.State != TcpState.Listen) tcpActive++;

        // ── Layout ──────────────────────────────────────────────────
        //
        //  ┌──────────────────────────────────────────────────────────┐
        //  │ Header + stats                                          │
        //  ├────────────────────────────────┬─────────────────────────┤
        //  │ Streaming Area Chart           │ Protocol Donut          │
        //  ├────────────────────────────────┼─────────────────────────┤
        //  │ Top Connections Bar Chart      │ TCP State Heatmap       │
        //  ├────────────────────────────────┴─────────────────────────┤
        //  │ Connection Sparklines (scrollable)                      │
        //  └──────────────────────────────────────────────────────────┘

        return VStack(0,
            // Header bar with FPS meter
            HStack(12,
                Text("NetPulse").FontSize(18).Bold().Margin(12, 8, 0, 4),
                Text($"TCP: {tcpActive}").FontSize(11).Foreground(Brushes.Gray100).Margin(0, 10, 0, 0),
                Text($"UDP: {udpEndpoints.Length}").FontSize(11).Foreground(Brushes.Gray100).Margin(0, 10, 0, 0),
                Text($"Sparklines: {sparklineData.Length}").FontSize(11).Foreground(Brushes.Gray100).Margin(0, 10, 0, 0),
                Text($"History: {trafficHistory.Count}").FontSize(11).Foreground(Brushes.Gray80).Margin(0, 10, 0, 0),
                RenderFpsMeter(stats)
            ),

            // Row 1: Area chart + Donut
            HStack(0,
                Component<TrafficAreaChart, TrafficAreaChartProps>(
                    new(trafficHistory)),

                Component<ProtocolDonut, ProtocolDonutProps>(
                    new(tcpActive, udpEndpoints.Length, tcpConns))
            ).Margin(8, 0, 0, 0),

            // Row 2: Bar chart + Heatmap
            HStack(0,
                Component<TopConnectionsBar, TopConnectionsBarProps>(
                    new(tcpConns)),

                Component<StateHeatmap, StateHeatmapProps>(
                    new(tcpConns))
            ),

            // Row 3: Sparklines (scrollable)
            ScrollView(
                Component<ConnectionSparklines, ConnectionSparklinesProps>(
                    new(sparklineData))
            ).Height(300)
        ).Set(s =>
        {
            s.HorizontalAlignment = HorizontalAlignment.Stretch;
            s.VerticalAlignment = VerticalAlignment.Top;
        });
    }

    /// <summary>
    /// Compact D3 FPS meter: bar gauge + text stats.
    /// Green ≥30fps, yellow ≥15fps, red below.
    /// </summary>
    static Element RenderFpsMeter(RenderStats s)
    {
        const double W = 320, H = 36;
        double fps = s.Fps;
        double maxFps = 60;
        double barW = Math.Clamp(fps / maxFps, 0, 1) * (W - 130);

        var barBrush = fps >= 30
            ? Brushes.Green
            : fps >= 15 ? Brushes.Orange : Brushes.Red;

        var elements = new Element[]
        {
            // Bar background
            D3Rect(0, 6, W - 130, 10) with { Fill = Brushes.BarBg, RadiusX = 3, RadiusY = 3 },
            // Bar fill
            D3Rect(0, 6, Math.Max(1, barW), 10) with { Fill = barBrush, RadiusX = 3, RadiusY = 3 },
            // FPS number
            D3Text(0, 20, $"{fps:F0} fps", 9, barBrush),
            // Frame time
            D3Text(44, 20, $"{s.AvgTotalMs:F1}ms", 9, Brushes.Gray100),
            // Renders total
            D3Text(96, 20, $"#{s.TotalRenders}", 9, Brushes.Gray140),
#if DEBUG
            // Element counters (debug only)
            D3Text(W - 126, 2, $"diffed:{s.LastDiffed}", 8, Brushes.Gray120),
            D3Text(W - 126, 12, $"skip:{s.LastSkipped}", 8, Brushes.Gray120),
            D3Text(W - 62, 2, $"new:{s.LastCreated}", 8, Brushes.Gray120),
            D3Text(W - 62, 12, $"mod:{s.LastModified}", 8, Brushes.Gray120),
#endif
        };

        return D3Canvas(W, H, elements).Margin(16, 4, 0, 0);
    }

}

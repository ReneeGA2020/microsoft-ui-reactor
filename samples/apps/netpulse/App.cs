using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using NetPulse.Charts;
using static Microsoft.UI.Reactor.Factories;

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

        // Render count for perf overlay
        var renderCountRef = UseRef(0);
        renderCountRef.Current++;

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
            // Header bar
            HStack(12,
                TextBlock("NetPulse").FontSize(18).Bold().Margin(12, 8, 0, 4),
                TextBlock($"TCP: {tcpActive} active").FontSize(11).Foreground(Gray(100)).Margin(0, 10, 0, 0),
                TextBlock($"UDP: {udpEndpoints.Length} endpoints").FontSize(11).Foreground(Gray(100)).Margin(0, 10, 0, 0),
                TextBlock($"Sparklines: {sparklineData.Length}").FontSize(11).Foreground(Gray(100)).Margin(0, 10, 0, 0),
                TextBlock($"Renders: {renderCountRef.Current}").FontSize(11).Foreground(Gray(80)).Margin(0, 10, 0, 0),
                TextBlock($"History: {trafficHistory.Count} samples").FontSize(11).Foreground(Gray(80)).Margin(0, 10, 0, 0)
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

    static Microsoft.UI.Xaml.Media.SolidColorBrush Gray(byte v) =>
        new(global::Windows.UI.Color.FromArgb(255, v, v, v));
}

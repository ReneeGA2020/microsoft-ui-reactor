using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml.Media;
using static Duct.D3.Charts.D3;
using static Duct.UI;

namespace NetPulse.Charts;

/// <summary>
/// TCP state heatmap: a grid of colored cells, one per active connection.
/// Color encodes TCP state (ESTABLISHED=green, TIME_WAIT=orange, etc.).
/// Connections are sorted by state then by remote port, causing constant
/// cell reordering and recoloring as states change.
/// </summary>
sealed record StateHeatmapProps(IReadOnlyList<TcpConn> Connections);

sealed class StateHeatmap : Component<StateHeatmapProps>
{
    const double W = 660, H = 280;
    const double Left = 10, Top = 30, Right = 10, Bottom = 10;
    const double CellSize = 14;
    const double CellGap = 2;

    static Dictionary<TcpState, SolidColorBrush>? _stateBrush;
    static Dictionary<TcpState, SolidColorBrush> StateBrush => _stateBrush ??= new()
    {
        [TcpState.Established] = Brushes.Green,
        [TcpState.TimeWait] = Brushes.Orange,
        [TcpState.CloseWait] = Brushes.Purple,
        [TcpState.FinWait1] = Brushes.Red,
        [TcpState.FinWait2] = Brushes.Red,
        [TcpState.SynSent] = Brushes.Blue,
        [TcpState.SynReceived] = Brushes.Blue,
        [TcpState.Listen] = Brushes.GrayMed,
        [TcpState.Closing] = Brushes.DarkOrange,
        [TcpState.LastAck] = Brushes.DarkRed,
        [TcpState.Closed] = Brushes.DarkGray,
        [TcpState.DeleteTcb] = Brushes.Slate,
    };

    public override Element Render()
    {
        var connections = Props.Connections;
        if (connections.Count == 0)
        {
            return D3Canvas(W, H,
                D3Text(Left, Top + 40, "No connections", 12, Brushes.Gray100));
        }

        // Sort by state (causes reordering as states change), then by remote port
        var sorted = connections
            .Where(c => c.State != TcpState.Listen)
            .OrderBy(c => c.State)
            .ThenBy(c => c.RemotePort)
            .ToArray();

        double usableW = W - Left - Right;
        int cols = Math.Max(1, (int)(usableW / (CellSize + CellGap)));
        int rows = (sorted.Length + cols - 1) / cols;
        double gridH = rows * (CellSize + CellGap);

        var elements = new List<Element>();
        elements.Add(D3Text(Left, 4, $"TCP State Heatmap ({sorted.Length} connections)", 13, Brushes.Gray40));

        for (int i = 0; i < sorted.Length; i++)
        {
            var conn = sorted[i];
            int col = i % cols;
            int row = i / cols;
            double x = Left + col * (CellSize + CellGap);
            double y = Top + row * (CellSize + CellGap);

            var fill = StateBrush.GetValueOrDefault(conn.State, Brushes.Gray200);

            elements.Add(
                D3Rect(x, y, CellSize, CellSize) with
                {
                    Fill = fill,
                    RadiusX = 2,
                    RadiusY = 2,
                });
        }

        // Legend at bottom
        double legendY = Math.Min(Top + gridH + 8, H - 20);
        var legendStates = new[] {
            (TcpState.Established, "ESTABLISHED"),
            (TcpState.TimeWait, "TIME_WAIT"),
            (TcpState.CloseWait, "CLOSE_WAIT"),
            (TcpState.SynSent, "SYN_SENT"),
            (TcpState.FinWait1, "FIN_WAIT"),
            (TcpState.Closing, "CLOSING"),
        };
        double lx = Left;
        foreach (var (state, label) in legendStates)
        {
            var brush = StateBrush.GetValueOrDefault(state, Brushes.Gray200);
            elements.Add(D3Rect(lx, legendY, 10, 10) with { Fill = brush, RadiusX = 1, RadiusY = 1 });
            elements.Add(D3Text(lx + 13, legendY - 1, label, 8, Brushes.Gray80));
            lx += label.Length * 6 + 24;
        }

        // State count summary
        var stateCounts = sorted.GroupBy(c => c.State)
            .OrderByDescending(g => g.Count())
            .Take(4);
        double sx = W - Right - 200;
        elements.Add(D3Text(sx, 6, "Counts:", 10, Brushes.Gray80));
        sx += 50;
        foreach (var g in stateCounts)
        {
            elements.Add(D3Text(sx, 6, $"{g.Key}: {g.Count()}", 10, Brushes.Gray60));
            sx += 80;
        }

        return D3Canvas(W, Math.Max(H, legendY + 20), elements.ToArray());
    }
}

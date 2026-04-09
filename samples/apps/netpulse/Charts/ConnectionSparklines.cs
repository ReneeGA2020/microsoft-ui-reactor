using Duct;
using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using static Duct.D3.Charts.D3;
using static Duct.UI;

namespace NetPulse.Charts;

/// <summary>
/// Per-connection sparklines: a grid of mini line charts, one per active TCP connection.
/// Each sparkline plots the TCP state value (1-12) over time with a filled area,
/// state transition markers, a baseline grid, and a current-value indicator.
///
/// Element count: 60 connections x ~12 elements each ≈ 720+ D3 shapes,
/// plus a header and column headers. Exercises positional reconciliation at scale
/// with mixed element types (Rect, Text, Path, Line, Ellipse).
/// </summary>
sealed record ConnectionSparklinesProps(IReadOnlyList<SparklineEntry> Entries);

sealed class ConnectionSparklines : Component<ConnectionSparklinesProps>
{
    const double CellW = 200, CellH = 64;
    const double Pad = 6;
    const double SparkW = 120, SparkH = 36;
    const double SparkLeft = 70, SparkTop = 18;

    // Map TCP state to color for the sparkline
    static readonly Dictionary<int, int> StateColor = new()
    {
        [(int)TcpState.Established] = 2,  // green
        [(int)TcpState.TimeWait] = 3,     // orange
        [(int)TcpState.CloseWait] = 4,    // purple
        [(int)TcpState.FinWait1] = 5,
        [(int)TcpState.FinWait2] = 5,
        [(int)TcpState.SynSent] = 6,
        [(int)TcpState.SynReceived] = 6,
        [(int)TcpState.Closing] = 7,
    };

    public override Element Render()
    {
        var entries = Props.Entries;
        if (entries.Count == 0)
        {
            return D3Canvas(100, 40,
                D3Text(4, 12, "No connection history yet...", 11, Gray(100)));
        }

        // Compute grid dimensions — fill available width (~1340px minus margins)
        int cols = Math.Max(1, (int)(1340 / (CellW + Pad)));
        int rows = (entries.Count + cols - 1) / cols;
        double totalW = cols * (CellW + Pad);
        double totalH = rows * (CellH + Pad) + 26;

        var elements = new List<Element>(entries.Count * 12 + 1);
        elements.Add(D3Text(4, 4, $"Connection Sparklines ({entries.Count})", 13, Gray(40)));

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            int col = i % cols;
            int row = i / cols;
            double cx = col * (CellW + Pad);
            double cy = 24 + row * (CellH + Pad);

            // 1. Background card
            elements.Add(
                D3Rect(cx, cy, CellW, CellH) with
                {
                    Fill = Gray(245),
                    RadiusX = 4,
                    RadiusY = 4,
                });

            // 2. Connection label
            string label = entry.Label.Length > 24
                ? string.Concat(entry.Label.AsSpan(0, 23), "\u2026")
                : entry.Label;
            elements.Add(
                D3Text(cx + 4, cy + 2, label, 8, Gray(80)));

            var history = entry.StateHistory;
            if (history.Length >= 2)
            {
                double xStep = SparkW / (history.Length - 1);
                double yScale = SparkH / 12.0;

                int latestState = history[^1];
                int colorIdx = StateColor.GetValueOrDefault(latestState, 0);
                var strokeBrush = Brush(Palette[colorIdx % Palette.Length], 0.8);
                var fillBrush = Brush(Palette[colorIdx % Palette.Length], 0.15);

                var pts = history.Select((s, j) => (
                    x: cx + SparkLeft + j * xStep,
                    y: cy + SparkTop + SparkH - s * yScale
                )).ToArray();

                // 3. Baseline grid line (y=0 axis)
                elements.Add(
                    D3Line(cx + SparkLeft, cy + SparkTop + SparkH,
                           cx + SparkLeft + SparkW, cy + SparkTop + SparkH)
                    with { Stroke = Gray(220), StrokeThickness = 0.5 });

                // 4. Midpoint grid line
                elements.Add(
                    D3Line(cx + SparkLeft, cy + SparkTop + SparkH / 2,
                           cx + SparkLeft + SparkW, cy + SparkTop + SparkH / 2)
                    with { Stroke = Gray(230), StrokeThickness = 0.5 });

                // 5. Filled area under sparkline
                elements.Add(
                    D3AreaPath(pts,
                        x: p => p.x,
                        y0: _ => cy + SparkTop + SparkH,
                        y1: p => p.y,
                        fill: fillBrush));

                // 6. Sparkline path
                elements.Add(
                    D3LinePath(pts, x: p => p.x, y: p => p.y,
                        stroke: strokeBrush, strokeWidth: 1.5));

                // 7. Current value indicator dot
                var last = pts[^1];
                elements.Add(
                    D3Circle(last.x, last.y, 2.5)
                    with { Fill = strokeBrush });

                // 8. State transition markers (dots where state changed)
                for (int j = 1; j < history.Length; j++)
                {
                    if (history[j] != history[j - 1])
                    {
                        elements.Add(
                            D3Circle(pts[j].x, pts[j].y, 1.5)
                            with { Fill = Gray(180) });
                        break; // one marker per card to keep element count bounded
                    }
                }

                // 9. State name
                string stateName = ((TcpState)latestState).ToString();
                elements.Add(
                    D3Text(cx + 4, cy + CellH - 16, stateName, 8,
                        Brush(Palette[colorIdx % Palette.Length])));

                // 10. Sample count
                elements.Add(
                    D3Text(cx + SparkLeft + SparkW - 24, cy + CellH - 16,
                        $"{history.Length}pt", 7, Gray(160)));
            }
        }

        return D3Canvas(totalW, totalH, elements.ToArray());
    }
}

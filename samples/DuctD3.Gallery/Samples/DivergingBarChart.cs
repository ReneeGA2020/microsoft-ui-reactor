using Duct.D3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public class DivergingBarChartSample : GallerySample
{
    public override string Title => "Diverging Bar Chart";
    public override string Description => "A diverging bar chart showing customer sentiment scores ranging from negative to positive, with bars extending from a central baseline.";
    public override string Category => "Bars";

    public override string SourceCode => """
        var xs = new LinearScale([-absMax, absMax], [0, plotW]).Nice();
        var band = BandScale.Create(items)
            .SetRange(0, plotH).SetPaddingInner(0.2).SetPaddingOuter(0.1);

        double zeroX = left + xs.Map(0);
        G.AddLine(canvas, zeroX, top, zeroX, top + plotH, G.Gray(80), 1.5);

        var posBrush = G.Brush(G.Palette[0]);
        var negBrush = G.Brush(G.Palette[3]);

        for (int i = 0; i < items.Length; i++)
        {
            double y = top + band.Map(items[i]);
            double v = scores[i];
            double barStart = v >= 0 ? zeroX : left + xs.Map(v);
            double barWidth = v >= 0
                ? xs.Map(v) - xs.Map(0)
                : xs.Map(0) - xs.Map(v);
            var fill = v >= 0 ? posBrush : negBrush;
            G.AddRect(canvas, barStart, y, barWidth, band.Bandwidth, fill, 2);
        }
        """;

    public override FrameworkElement Render()
    {
        const double W = 700, H = 400;
        const double left = 110, top = 30, right = 30, bottom = 40;
        double plotW = W - left - right;
        double plotH = H - top - bottom;

        var canvas = new Canvas { Width = W, Height = H };

        // Sample data — sentiment scores (-100 to +100)
        string[] items = ["Product Quality", "Customer Service", "Pricing",
                          "Delivery Speed", "Return Policy", "Website UX",
                          "Mobile App", "Loyalty Program", "Packaging",
                          "Overall Value"];
        double[] scores = [72, 45, -18, -35, 28, 55, -12, 60, 38, -8];

        // Find extent
        double minVal = double.MaxValue, maxVal = double.MinValue;
        foreach (var v in scores)
        {
            if (v < minVal) minVal = v;
            if (v > maxVal) maxVal = v;
        }
        // Ensure zero is included and symmetric-ish
        double absMax = Math.Max(Math.Abs(minVal), Math.Abs(maxVal));

        // Scales
        var xs = new LinearScale([-absMax, absMax], [0, plotW]).Nice();
        var band = BandScale.Create(items).SetRange(0, plotH).SetPaddingInner(0.2).SetPaddingOuter(0.1);

        // Vertical grid lines
        var gridBrush = G.Gray(128, 40);
        foreach (var t in xs.Ticks(8))
            G.AddLine(canvas, left + xs.Map(t), top, left + xs.Map(t), top + plotH, gridBrush);

        // Zero line
        double zeroX = left + xs.Map(0);
        G.AddLine(canvas, zeroX, top, zeroX, top + plotH, G.Gray(80), 1.5);

        // Bars
        var posBrush = G.Brush(G.Palette[0]);
        var negBrush = G.Brush(G.Palette[3]);

        for (int i = 0; i < items.Length; i++)
        {
            double y = top + band.Map(items[i]);
            double v = scores[i];
            double barStart, barWidth;

            if (v >= 0)
            {
                barStart = zeroX;
                barWidth = xs.Map(v) - xs.Map(0);
            }
            else
            {
                barStart = left + xs.Map(v);
                barWidth = xs.Map(0) - xs.Map(v);
            }

            var fill = v >= 0 ? posBrush : negBrush;
            G.AddRect(canvas, barStart, y, barWidth, band.Bandwidth, fill, 2);

            // Value label
            double labelX = v >= 0 ? barStart + barWidth + 4 : barStart - 30;
            G.AddText(canvas, labelX, y + band.Bandwidth / 2 - 7,
                      (v >= 0 ? "+" : "") + v.ToString("F0"), 10, G.Gray(60));
        }

        // Axes
        var axisBrush = G.Gray(100, 180);
        G.AddLine(canvas, left, top + plotH, left + plotW, top + plotH, axisBrush);

        // X axis tick labels
        foreach (var t in xs.Ticks(8))
            G.AddText(canvas, left + xs.Map(t) - 12, top + plotH + 6, G.Fmt(t), 10, axisBrush);

        // Y axis labels (item names)
        for (int i = 0; i < items.Length; i++)
        {
            double cy = top + band.Map(items[i]) + band.Bandwidth / 2 - 7;
            G.AddText(canvas, 2, cy, items[i], 10, G.Gray(60), TextAlignment.Right, left - 6);
        }

        // Title
        G.AddText(canvas, left, 4, "Customer Sentiment Scores", 13, G.Gray(40));

        // Legend
        G.AddRect(canvas, left + plotW - 120, top + 2, 12, 12, posBrush, 2);
        G.AddText(canvas, left + plotW - 104, top + 1, "Positive", 10, G.Gray(60));
        G.AddRect(canvas, left + plotW - 50, top + 2, 12, 12, negBrush, 2);
        G.AddText(canvas, left + plotW - 34, top + 1, "Negative", 10, G.Gray(60));

        return canvas;
    }
}

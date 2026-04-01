using Duct.D3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public class GroupedBarChartSample : GallerySample
{
    public override string Title => "Grouped Bar Chart";
    public override string Description => "A grouped bar chart comparing quarterly sales for three product lines side by side.";
    public override string Category => "Bars";

    public override string SourceCode => """
        var ys = new LinearScale([0, maxVal], [plotH, 0]).Nice();
        var band = BandScale.Create(categories)
            .SetRange(0, plotW).SetPaddingInner(0.2).SetPaddingOuter(0.1);
        var groupBand = BandScale.Create(seriesNames)
            .SetRange(0, band.Bandwidth).SetPaddingInner(0.05);

        G.DrawGrid(canvas, ysScreen, left, plotW);

        for (int si = 0; si < seriesNames.Length; si++)
        {
            var fill = G.Brush(G.Palette[si]);
            for (int ci = 0; ci < categories.Length; ci++)
            {
                double x = left + band.Map(categories[ci])
                         + groupBand.Map(seriesNames[si]);
                double barH = plotH - ys.Map(values[si][ci]);
                double y = top + ys.Map(values[si][ci]);
                G.AddRect(canvas, x, y, groupBand.Bandwidth, barH, fill, 2);
            }
        }
        """;

    public override FrameworkElement Render()
    {
        const double W = 700, H = 400;
        const double left = 60, top = 30, right = 120, bottom = 50;
        double plotW = W - left - right;
        double plotH = H - top - bottom;

        var canvas = new Canvas { Width = W, Height = H };

        // Sample data — quarterly sales by product line
        string[] categories = ["Q1", "Q2", "Q3", "Q4", "FY"];
        string[] seriesNames = ["Electronics", "Clothing", "Groceries"];
        double[][] values =
        [
            [120, 85, 95, 130, 430],   // Electronics
            [75, 110, 90, 100, 375],    // Clothing
            [60, 70, 105, 80, 315],     // Groceries
        ];

        // Find max value
        double maxVal = 0;
        foreach (var row in values)
            foreach (var v in row)
                if (v > maxVal) maxVal = v;

        // Scales
        var ys = new LinearScale([0, maxVal], [plotH, 0]).Nice();
        var band = BandScale.Create(categories).SetRange(0, plotW).SetPaddingInner(0.2).SetPaddingOuter(0.1);
        var groupBand = BandScale.Create(seriesNames).SetRange(0, band.Bandwidth).SetPaddingInner(0.05);
        var ysScreen = new LinearScale(ys.Domain, [top + plotH, top]);

        // Grid
        G.DrawGrid(canvas, ysScreen, left, plotW);

        // Grouped bars
        for (int si = 0; si < seriesNames.Length; si++)
        {
            var fill = G.Brush(G.Palette[si]);
            for (int ci = 0; ci < categories.Length; ci++)
            {
                double x = left + band.Map(categories[ci]) + groupBand.Map(seriesNames[si]);
                double barH = plotH - ys.Map(values[si][ci]);
                double y = top + ys.Map(values[si][ci]);
                G.AddRect(canvas, x, y, groupBand.Bandwidth, barH, fill, 2);
            }
        }

        // Axes
        var axisBrush = G.Gray(100, 180);
        G.AddLine(canvas, left, top + plotH, left + plotW, top + plotH, axisBrush);
        G.AddLine(canvas, left, top, left, top + plotH, axisBrush);

        // Y axis labels
        foreach (var t in ysScreen.Ticks(5))
            G.AddText(canvas, 0, ysScreen.Map(t) - 7, G.Fmt(t), 10, axisBrush, TextAlignment.Right, left - 6);

        // X axis labels
        for (int i = 0; i < categories.Length; i++)
        {
            double cx = left + band.Map(categories[i]) + band.Bandwidth / 2;
            G.AddText(canvas, cx - 10, top + plotH + 8, categories[i], 10, axisBrush);
        }

        // Legend
        double legendX = W - right + 12;
        double legendY = top + 10;
        for (int i = 0; i < seriesNames.Length; i++)
        {
            G.AddRect(canvas, legendX, legendY + i * 22, 14, 14, G.Brush(G.Palette[i]), 2);
            G.AddText(canvas, legendX + 20, legendY + i * 22, seriesNames[i], 11, G.Gray(60));
        }

        // Title
        G.AddText(canvas, left, 4, "Quarterly Sales by Product Line", 13, G.Gray(40));

        return canvas;
    }
}

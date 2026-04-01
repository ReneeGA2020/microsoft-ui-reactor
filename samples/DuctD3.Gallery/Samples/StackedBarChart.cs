using Duct.D3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public class StackedBarChartSample : GallerySample
{
    public override string Title => "Stacked Bar Chart";
    public override string Description => "A stacked bar chart showing fruit sales (Apples, Bananas, Cherries) across six months using StackGenerator.";
    public override string Category => "Bars";

    public override string SourceCode => """
        var stack = StackGenerator.Create<Dictionary<string, double>>()
            .SetKeys(keys)
            .SetValue((row, key) => row[key]);
        var series = stack.Generate(data);

        var ys = new LinearScale([0, maxVal], [plotH, 0]).Nice();
        var band = BandScale.Create(months)
            .SetRange(0, plotW).SetPaddingInner(0.2).SetPaddingOuter(0.1);

        for (int si = 0; si < series.Length; si++)
        {
            var fill = G.Brush(G.Palette[si]);
            for (int j = 0; j < months.Length; j++)
            {
                var pt = series[si].Points[j];
                double y0Screen = top + ys.Map(pt.Y0);
                double y1Screen = top + ys.Map(pt.Y1);
                double x = left + band.Map(months[j]);
                G.AddRect(canvas, x, y1Screen, band.Bandwidth,
                          y0Screen - y1Screen, fill, 1);
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

        // Sample data
        string[] months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun"];
        string[] keys = ["Apples", "Bananas", "Cherries"];
        var data = new List<Dictionary<string, double>>
        {
            new() { ["Apples"] = 30, ["Bananas"] = 20, ["Cherries"] = 15 },
            new() { ["Apples"] = 25, ["Bananas"] = 35, ["Cherries"] = 20 },
            new() { ["Apples"] = 40, ["Bananas"] = 25, ["Cherries"] = 30 },
            new() { ["Apples"] = 35, ["Bananas"] = 40, ["Cherries"] = 25 },
            new() { ["Apples"] = 50, ["Bananas"] = 30, ["Cherries"] = 35 },
            new() { ["Apples"] = 45, ["Bananas"] = 45, ["Cherries"] = 40 },
        };

        // Stack generator
        var stack = StackGenerator.Create<Dictionary<string, double>>()
            .SetKeys(keys)
            .SetValue((row, key) => row[key]);
        var series = stack.Generate(data);

        // Find max stacked value
        double maxVal = 0;
        foreach (var s in series)
            foreach (var pt in s.Points)
                if (pt.Y1 > maxVal) maxVal = pt.Y1;

        // Scales
        var ys = new LinearScale([0, maxVal], [plotH, 0]).Nice();
        var band = BandScale.Create(months).SetRange(0, plotW).SetPaddingInner(0.2).SetPaddingOuter(0.1);
        var ysScreen = new LinearScale(ys.Domain, [top + plotH, top]);

        // Grid
        G.DrawGrid(canvas, ysScreen, left, plotW);

        // Draw stacked bars
        for (int si = 0; si < series.Length; si++)
        {
            var fill = G.Brush(G.Palette[si]);
            for (int j = 0; j < months.Length; j++)
            {
                var pt = series[si].Points[j];
                double y0Screen = top + ys.Map(pt.Y0);
                double y1Screen = top + ys.Map(pt.Y1);
                double x = left + band.Map(months[j]);
                G.AddRect(canvas, x, y1Screen, band.Bandwidth, y0Screen - y1Screen, fill, 1);
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
        for (int i = 0; i < months.Length; i++)
        {
            double cx = left + band.Map(months[i]) + band.Bandwidth / 2;
            G.AddText(canvas, cx - 14, top + plotH + 8, months[i], 10, axisBrush);
        }

        // Legend
        double legendX = W - right + 12;
        double legendY = top + 10;
        for (int i = 0; i < keys.Length; i++)
        {
            G.AddRect(canvas, legendX, legendY + i * 22, 14, 14, G.Brush(G.Palette[i]), 2);
            G.AddText(canvas, legendX + 20, legendY + i * 22, keys[i], 11, G.Gray(60));
        }

        // Title
        G.AddText(canvas, left, 4, "Fruit Sales by Month (Stacked)", 13, G.Gray(40));

        return canvas;
    }
}

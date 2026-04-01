using Duct.D3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public class BarChartSample : GallerySample
{
    public override string Title => "Bar Chart";
    public override string Description => "A simple vertical bar chart showing monthly revenue data for a fictional company.";
    public override string Category => "Bars";

    public override string SourceCode => """
        var ys = new LinearScale([0, maxVal], [plotH, 0]).Nice();
        var band = BandScale.Create(months)
            .SetRange(0, plotW).SetPaddingInner(0.2).SetPaddingOuter(0.1);
        var ysScreen = new LinearScale(ys.Domain, [top + plotH, top]);

        G.DrawGrid(canvas, ysScreen, left, plotW);

        var fill = G.Brush(G.Palette[0]);
        for (int i = 0; i < months.Length; i++)
        {
            double x = left + band.Map(months[i]);
            double barH = plotH - ys.Map(revenue[i]);
            double y = top + ys.Map(revenue[i]);
            G.AddRect(canvas, x, y, band.Bandwidth, barH, fill, 2);
        }

        foreach (var t in ysScreen.Ticks(5))
            G.AddText(canvas, 0, ysScreen.Map(t) - 7, G.Fmt(t) + "k",
                      10, axisBrush, TextAlignment.Right, left - 6);
        """;

    public override FrameworkElement Render()
    {
        const double W = 700, H = 400;
        const double left = 60, top = 30, right = 20, bottom = 50;
        double plotW = W - left - right;
        double plotH = H - top - bottom;

        var canvas = new Canvas { Width = W, Height = H };

        // Sample data — monthly revenue in thousands
        string[] months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun",
                           "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
        double[] revenue = [42, 55, 48, 61, 73, 68, 82, 91, 77, 85, 94, 103];

        double maxVal = 0;
        foreach (var v in revenue) if (v > maxVal) maxVal = v;

        // Scales
        var ys = new LinearScale([0, maxVal], [plotH, 0]).Nice();
        var band = BandScale.Create(months).SetRange(0, plotW).SetPaddingInner(0.2).SetPaddingOuter(0.1);

        // Offset ys for drawing helpers
        var ysScreen = new LinearScale(ys.Domain, [top + plotH, top]);

        // Grid
        G.DrawGrid(canvas, ysScreen, left, plotW);

        // Bars
        var fill = G.Brush(G.Palette[0]);
        for (int i = 0; i < months.Length; i++)
        {
            double x = left + band.Map(months[i]);
            double barH = plotH - ys.Map(revenue[i]);
            double y = top + ys.Map(revenue[i]);
            G.AddRect(canvas, x, y, band.Bandwidth, barH, fill, 2);
        }

        // Axes
        var axisBrush = G.Gray(100, 180);
        G.AddLine(canvas, left, top + plotH, left + plotW, top + plotH, axisBrush);
        G.AddLine(canvas, left, top, left, top + plotH, axisBrush);

        // Y axis tick labels
        foreach (var t in ysScreen.Ticks(5))
            G.AddText(canvas, 0, ysScreen.Map(t) - 7, G.Fmt(t) + "k", 10, axisBrush, TextAlignment.Right, left - 6);

        // X axis labels
        for (int i = 0; i < months.Length; i++)
        {
            double cx = left + band.Map(months[i]) + band.Bandwidth / 2;
            G.AddText(canvas, cx - 14, top + plotH + 8, months[i], 10, axisBrush);
        }

        // Title
        G.AddText(canvas, left, 4, "Monthly Revenue ($k)", 13, G.Gray(40));

        return canvas;
    }
}

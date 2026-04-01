using Duct.D3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public class HorizontalBarChartSample : GallerySample
{
    public override string Title => "Horizontal Bar Chart";
    public override string Description => "A horizontal bar chart comparing populations of the world's most populous countries (in millions).";
    public override string Category => "Bars";

    public override string SourceCode => """
        var xs = new LinearScale([0, maxVal], [0, plotW]).Nice();
        var band = BandScale.Create(countries)
            .SetRange(0, plotH).SetPaddingInner(0.25).SetPaddingOuter(0.1);

        var gridBrush = G.Gray(128, 40);
        foreach (var t in xs.Ticks(6))
            G.AddLine(canvas, left + xs.Map(t), top,
                      left + xs.Map(t), top + plotH, gridBrush);

        for (int i = 0; i < countries.Length; i++)
        {
            var fill = G.Brush(G.Palette[i % G.Palette.Length], 0.85);
            double y = top + band.Map(countries[i]);
            double barW = xs.Map(populations[i]);
            G.AddRect(canvas, left, y, barW, band.Bandwidth, fill, 2);

            G.AddText(canvas, left + barW + 4, y + band.Bandwidth / 2 - 7,
                      $"{populations[i]:F0}M", 10, G.Gray(60));
        }
        """;

    public override FrameworkElement Render()
    {
        const double W = 700, H = 400;
        const double left = 100, top = 30, right = 30, bottom = 40;
        double plotW = W - left - right;
        double plotH = H - top - bottom;

        var canvas = new Canvas { Width = W, Height = H };

        // Sample data — country populations in millions
        string[] countries = ["India", "China", "USA", "Indonesia", "Pakistan",
                              "Nigeria", "Brazil", "Bangladesh", "Russia", "Mexico"];
        double[] populations = [1428, 1425, 340, 277, 230, 223, 216, 173, 144, 128];

        double maxVal = 0;
        foreach (var v in populations) if (v > maxVal) maxVal = v;

        // Scales
        var xs = new LinearScale([0, maxVal], [0, plotW]).Nice();
        var band = BandScale.Create(countries).SetRange(0, plotH).SetPaddingInner(0.25).SetPaddingOuter(0.1);

        // Horizontal grid lines (vertical in this case — along X)
        var gridBrush = G.Gray(128, 40);
        foreach (var t in xs.Ticks(6))
            G.AddLine(canvas, left + xs.Map(t), top, left + xs.Map(t), top + plotH, gridBrush);

        // Bars
        for (int i = 0; i < countries.Length; i++)
        {
            var fill = G.Brush(G.Palette[i % G.Palette.Length], 0.85);
            double y = top + band.Map(countries[i]);
            double barW = xs.Map(populations[i]);
            G.AddRect(canvas, left, y, barW, band.Bandwidth, fill, 2);

            // Value label at end of bar
            G.AddText(canvas, left + barW + 4, y + band.Bandwidth / 2 - 7,
                      $"{populations[i]:F0}M", 10, G.Gray(60));
        }

        // Axes
        var axisBrush = G.Gray(100, 180);
        G.AddLine(canvas, left, top + plotH, left + plotW, top + plotH, axisBrush);
        G.AddLine(canvas, left, top, left, top + plotH, axisBrush);

        // X axis tick labels
        foreach (var t in xs.Ticks(6))
            G.AddText(canvas, left + xs.Map(t) - 12, top + plotH + 6, G.Fmt(t), 10, axisBrush);

        // Y axis labels (country names)
        for (int i = 0; i < countries.Length; i++)
        {
            double cy = top + band.Map(countries[i]) + band.Bandwidth / 2 - 7;
            G.AddText(canvas, 2, cy, countries[i], 10, G.Gray(60), TextAlignment.Right, left - 6);
        }

        // Title
        G.AddText(canvas, left, 4, "Population by Country (millions)", 13, G.Gray(40));

        return canvas;
    }
}

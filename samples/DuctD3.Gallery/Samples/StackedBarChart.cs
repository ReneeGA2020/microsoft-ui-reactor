using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using static Duct.D3.Charts.D3;
using static Duct.UI;

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
            var fill = Brush(Palette[si]);
            for (int j = 0; j < months.Length; j++)
            {
                var pt = series[si].Points[j];
                double y0Screen = top + ys.Map(pt.Y0);
                double y1Screen = top + ys.Map(pt.Y1);
                double x = left + band.Map(months[j]);
                D3Rect(x, y1Screen, band.Bandwidth, y0Screen - y1Screen)
                    with { Fill = fill, RadiusX = 1, RadiusY = 1 };
            }
        }
        """;

    public override Element Render()
    {
        const double W = 700, H = 400;
        const double left = 60, top = 30, right = 120, bottom = 50;
        double plotW = W - left - right;
        double plotH = H - top - bottom;

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

        double maxVal = series.SelectMany(s => s.Points).Max(p => p.Y1);

        // Scales
        var ys = new LinearScale([0, maxVal], [plotH, 0]).Nice();
        var band = BandScale.Create(months).SetRange(0, plotW).SetPaddingInner(0.2).SetPaddingOuter(0.1);
        var ysScreen = new LinearScale(ys.Domain, [top + plotH, top]);

        // Axes
        var axisBrush = Gray(100, alpha: 180);
        double legendX = W - right + 12;
        double legendY = top + 10;

        return D3Canvas(W, H,
            [.. D3Grid(ysScreen, left, plotW),

             // Stacked bars
             .. series.SelectMany((s, si) =>
             {
                 var fill = Brush(Palette[si]);
                 return months.Select((month, j) =>
                 {
                     var pt = s.Points[j];
                     double y0Screen = top + ys.Map(pt.Y0);
                     double y1Screen = top + ys.Map(pt.Y1);
                     double x = left + band.Map(month);
                     return D3Rect(x, y1Screen, band.Bandwidth, y0Screen - y1Screen) with { Fill = fill, RadiusX = 1, RadiusY = 1 };
                 });
             }),

             D3Line(left, top + plotH, left + plotW, top + plotH) with { Stroke = axisBrush, StrokeThickness = 1 },
             D3Line(left, top, left, top + plotH) with { Stroke = axisBrush, StrokeThickness = 1 },
             .. ysScreen.Ticks(5).Select(t =>
                 D3TextRight(0, ysScreen.Map(t) - 7, Fmt(t), left - 6, 10, axisBrush)),

             // X axis labels
             .. months.Select((month, i) =>
                 D3Text(left + band.Map(month) + band.Bandwidth / 2 - 14, top + plotH + 8, month, 10, axisBrush)),

             // Legend
             .. D3Legend(legendX, legendY, keys.Select((key, i) => (key, Brush(Palette[i])))),

             D3Text(left, 4, "Fruit Sales by Month (Stacked)", 13, Gray(40)),
            ]
        );
    }
}

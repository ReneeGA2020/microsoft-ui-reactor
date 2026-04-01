using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.D3.Charts.D3;
using static Duct.UI;

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

        D3Canvas(W, H,
            ..D3Grid(ysScreen, left, plotW),
            ..bars,
            ..D3Axes(xs, ys, left, top, plotW, plotH)
        )
        """;

    public override Element Render()
    {
        const double W = 700, H = 400;
        const double left = 60, top = 30, right = 20, bottom = 50;
        double plotW = W - left - right;
        double plotH = H - top - bottom;

        string[] months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun",
                           "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
        double[] revenue = [42, 55, 48, 61, 73, 68, 82, 91, 77, 85, 94, 103];

        double maxVal = revenue.Max();

        var ys = new LinearScale([0, maxVal], [plotH, 0]).Nice();
        var band = BandScale.Create(months).SetRange(0, plotW).SetPaddingInner(0.2).SetPaddingOuter(0.1);
        var ysScreen = new LinearScale(ys.Domain, [top + plotH, top]);

        var fill = Brush(Palette[0]);
        var axisBrush = Gray(100, alpha: 180);

        var bars =
            from i in Enumerable.Range(0, months.Length)
            let x = left + band.Map(months[i])
            let barH = plotH - ys.Map(revenue[i])
            let y = top + ys.Map(revenue[i])
            select D3Rect(x, y, band.Bandwidth, barH) with { Fill = fill, RadiusX = 2, RadiusY = 2 };

        var xLabels =
            from month in months
            let cx = left + band.Map(month) + band.Bandwidth / 2
            select D3Text(cx - 14, top + plotH + 8, month, 10, axisBrush);

        return D3Canvas(W, H,
            [.. D3Grid(ysScreen, left, plotW),
             .. bars,
             D3Line(left, top + plotH, left + plotW, top + plotH) with { Stroke = axisBrush, StrokeThickness = 1 },
             D3Line(left, top, left, top + plotH) with { Stroke = axisBrush, StrokeThickness = 1 },
             .. ysScreen.Ticks(5).Select(t =>
                 D3TextRight(0, ysScreen.Map(t) - 7, Fmt(t) + "k", left - 6, 10, axisBrush)),
             .. xLabels,
             D3Text(left, 4, "Monthly Revenue ($k)", 13, Gray(40))]
        );
    }
}

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Charting.D3;
using Microsoft.UI.Reactor.Charting;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Charting.D3Dsl;
using static Microsoft.UI.Reactor.Factories;

namespace ReactorCharting.Gallery;

public class MultiLineChart : GallerySample
{
    public override string Title => "Multi-Line Chart";
    public override string Description => "Three overlapping temperature series (New York, London, Tokyo) drawn with different colors from the D3 categorical palette.";
    public override string Category => "Lines";

    public override string SourceCode => @"
var lines = allSeries.Select((series, s) =>
{
    var data = series.Select((v, i) => (x: (double)i, y: v)).ToArray();
    return D3LinePath(data, x: d => xs.Map(d.x), y: d => ys.Map(d.y),
        stroke: Brush(colors[s]), strokeWidth: 2);
})
    .AutomationName(""Monthly Temperatures (°C)"")
    .FullDescription(""Multi-line chart comparing monthly average temperatures for New York, London, and Tokyo across 12 months."")
";

    public override Element Render()
    {
        const double W = 700, H = 400;
        const double left = 50, top = 20, right = 120, bottom = 40;
        double width = W - left - right;
        double height = H - top - bottom;

        // Monthly average temperatures (12 months)
        string[] months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

        double[] newYork = [0.5, 1.8, 6.5, 12.2, 17.8, 22.9, 25.6, 25.0, 20.8, 14.7, 8.9, 3.2];
        double[] london  = [5.2, 5.5, 7.6, 9.9, 13.3, 16.4, 18.7, 18.4, 15.6, 12.0, 8.0, 5.5];
        double[] tokyo   = [5.8, 6.4, 9.4, 14.6, 19.0, 22.4, 25.9, 27.1, 23.5, 18.2, 13.0, 7.9];

        string[] labels = ["New York", "London", "Tokyo"];
        var colors = new[] { Palette[0], Palette[1], Palette[2] };
        var allSeries = new[] { newYork, london, tokyo };

        // Find global extent
        var allValues = newYork.Concat(london).Concat(tokyo);
        var (yMin, yMax) = D3Extent.Extent(allValues);

        var xs = new LinearScale([0, 11], [left, left + width]);
        var ys = new LinearScale([yMax + 3, yMin - 3], [top, top + height]).Nice();

        var monthLabels = months.Select((m, i) =>
            D3Dsl.Text(xs.Map(i) - 10, top + height + 4, m, 10, ChartMutedForeground));

        var lines = allSeries.Select((series, s) =>
        {
            var data = series.Select((v, i) => (x: (double)i, y: v)).ToArray();
            return (Element)D3LinePath(data, x: d => xs.Map(d.x), y: d => ys.Map(d.y),
                stroke: Brush(colors[s]), strokeWidth: 2);
        });

        double legendX = W - right + 10;

        return D3Canvas(W, H,
            [.. D3Grid(ys, left, width),
             .. D3Axes(xs, ys, left, top, width, height),
             .. monthLabels,
             .. lines,
             .. D3Legend(legendX, top + 10, labels.Select((label, s) => (label, Brush(colors[s])))),
             D3Dsl.Text(2, top - 14, "\u00b0C", 11, ChartMutedForeground)]
        )
            .AutomationName("Monthly Temperatures (\u00b0C)")
            .FullDescription("Multi-line chart comparing monthly average temperatures for New York, London, and Tokyo across 12 months.");
    }
}

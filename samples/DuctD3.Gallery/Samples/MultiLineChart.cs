using Duct.D3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public class MultiLineChart : GallerySample
{
    public override string Title => "Multi-Line Chart";
    public override string Description => "Three overlapping temperature series (New York, London, Tokyo) drawn with different colors from the D3 categorical palette.";
    public override string Category => "Lines";

    public override string SourceCode => @"
foreach (var (series, color) in seriesData.Zip(colors))
{
    var line = LineGenerator.Create<(double x, double y)>(
        d => xs.Map(d.x), d => ys.Map(d.y));
    var pathData = line.Generate(series);
    canvas.Children.Add(G.MakePath(pathData, G.Brush(color), strokeWidth: 2));
}";

    public override FrameworkElement Render()
    {
        const double canvasW = 700, canvasH = 400;
        const double left = 50, top = 20, right = 120, bottom = 40;
        double width = canvasW - left - right;
        double height = canvasH - top - bottom;

        // Monthly average temperatures (12 months)
        string[] months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

        double[] newYork = [0.5, 1.8, 6.5, 12.2, 17.8, 22.9, 25.6, 25.0, 20.8, 14.7, 8.9, 3.2];
        double[] london  = [5.2, 5.5, 7.6, 9.9, 13.3, 16.4, 18.7, 18.4, 15.6, 12.0, 8.0, 5.5];
        double[] tokyo   = [5.8, 6.4, 9.4, 14.6, 19.0, 22.4, 25.9, 27.1, 23.5, 18.2, 13.0, 7.9];

        string[] labels = ["New York", "London", "Tokyo"];
        var colors = new[] { G.Palette[0], G.Palette[1], G.Palette[2] };
        var allSeries = new[] { newYork, london, tokyo };

        // Find global extent
        var allValues = newYork.Concat(london).Concat(tokyo);
        var (yMin, yMax) = D3Extent.Extent(allValues);

        var xs = new LinearScale([0, 11], [left, left + width]);
        var ys = new LinearScale([yMax + 3, yMin - 3], [top, top + height]);
        ys.Nice();

        var canvas = new Canvas { Width = canvasW, Height = canvasH };

        G.DrawGrid(canvas, ys, left, width);
        G.DrawAxes(canvas, xs, ys, left, top, width, height);

        // Draw month labels on x-axis
        for (int i = 0; i < 12; i++)
            G.AddText(canvas, xs.Map(i) - 10, top + height + 4, months[i], 10, G.Gray(100));

        // Draw each series
        for (int s = 0; s < allSeries.Length; s++)
        {
            var series = allSeries[s];
            var data = new (double x, double y)[12];
            for (int i = 0; i < 12; i++)
                data[i] = (i, series[i]);

            var line = LineGenerator.Create<(double x, double y)>(
                d => xs.Map(d.x), d => ys.Map(d.y));
            var pathData = line.Generate(data);
            canvas.Children.Add(G.MakePath(pathData, G.Brush(colors[s]), strokeWidth: 2));
        }

        // Legend
        double legendX = canvasW - right + 10;
        for (int s = 0; s < labels.Length; s++)
        {
            double ly = top + 10 + s * 22;
            G.AddRect(canvas, legendX, ly, 14, 14, G.Brush(colors[s]), rx: 2);
            G.AddText(canvas, legendX + 20, ly, labels[s], 11, G.Gray(60));
        }

        // Y-axis label
        G.AddText(canvas, 2, top - 14, "\u00b0C", 11, G.Gray(80));

        return canvas;
    }
}

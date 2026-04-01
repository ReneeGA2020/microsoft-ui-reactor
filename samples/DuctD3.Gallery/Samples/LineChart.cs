using Duct.D3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public class LineChart : GallerySample
{
    public override string Title => "Line Chart";
    public override string Description => "A simple line chart showing daily temperature readings over 30 days using LineGenerator with linear interpolation.";
    public override string Category => "Lines";

    public override string SourceCode => @"
var line = LineGenerator.Create<(double x, double y)>(d => xs.Map(d.x), d => ys.Map(d.y));
var pathData = line.Generate(data);
canvas.Children.Add(G.MakePath(pathData, G.Brush(G.Palette[0])));
G.DrawGrid(canvas, ys, left, width);
G.DrawAxes(canvas, xs, ys, left, top, width, height);";

    public override FrameworkElement Render()
    {
        const double canvasW = 700, canvasH = 400;
        const double left = 50, top = 20, right = 20, bottom = 40;
        double width = canvasW - left - right;
        double height = canvasH - top - bottom;

        // Sample data: 30 days of temperature (degrees C)
        double[] temps =
        [
            5.2, 6.1, 7.8, 6.5, 8.3, 10.1, 11.4, 12.0, 11.2, 13.5,
            14.8, 15.1, 13.9, 12.7, 14.2, 16.0, 17.3, 18.1, 17.5, 16.8,
            18.4, 19.2, 20.1, 19.0, 17.6, 18.9, 20.5, 21.3, 20.8, 22.0
        ];

        var data = new (double x, double y)[temps.Length];
        for (int i = 0; i < temps.Length; i++)
            data[i] = (i + 1, temps[i]);

        var (yMin, yMax) = D3Extent.Extent(temps);
        var xs = new LinearScale([1, 30], [left, left + width]);
        var ys = new LinearScale([yMax + 2, yMin - 2], [top, top + height]);
        ys.Nice();

        var line = LineGenerator.Create<(double x, double y)>(d => xs.Map(d.x), d => ys.Map(d.y));
        var pathData = line.Generate(data);

        var canvas = new Canvas { Width = canvasW, Height = canvasH };

        G.DrawGrid(canvas, ys, left, width);
        G.DrawAxes(canvas, xs, ys, left, top, width, height);

        // Draw the line
        var path = G.MakePath(pathData, G.Brush(G.Palette[0]), strokeWidth: 2);
        canvas.Children.Add(path);

        // Draw data points
        var dotBrush = G.Brush(G.Palette[0]);
        foreach (var d in data)
            G.AddEllipse(canvas, xs.Map(d.x), ys.Map(d.y), 3, dotBrush);

        // Axis labels
        G.AddText(canvas, canvasW / 2 - 20, canvasH - 12, "Day", 11, G.Gray(80));
        G.AddText(canvas, 2, top - 14, "\u00b0C", 11, G.Gray(80));

        return canvas;
    }
}

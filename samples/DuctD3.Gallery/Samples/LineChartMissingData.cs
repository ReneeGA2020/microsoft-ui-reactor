using Duct.D3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public class LineChartMissingData : GallerySample
{
    public override string Title => "Line Chart with Missing Data";
    public override string Description => "Demonstrates using SetDefined to skip NaN values, creating visible gaps in the line where sensor readings were unavailable.";
    public override string Category => "Lines";

    public override string SourceCode => @"
var line = LineGenerator.Create<(double x, double y)>(
        d => xs.Map(d.x), d => ys.Map(d.y))
    .SetDefined((d, _) => !double.IsNaN(d.y));
var pathData = line.Generate(data);
canvas.Children.Add(G.MakePath(pathData, G.Brush(G.Palette[0]), strokeWidth: 2));";

    public override FrameworkElement Render()
    {
        const double canvasW = 700, canvasH = 400;
        const double left = 50, top = 20, right = 20, bottom = 40;
        double width = canvasW - left - right;
        double height = canvasH - top - bottom;

        // Sensor readings over 30 days, with some missing (NaN)
        double[] readings =
        [
            22.1, 23.4, 24.0, 23.8, 25.1, double.NaN, double.NaN, double.NaN, 26.3, 27.0,
            27.8, 28.1, 27.5, 26.9, 28.4, 29.1, double.NaN, double.NaN, 28.0, 27.2,
            26.5, 25.8, 26.1, 27.3, 28.0, 28.9, 29.5, double.NaN, 30.1, 30.8
        ];

        var data = new (double x, double y)[readings.Length];
        for (int i = 0; i < readings.Length; i++)
            data[i] = (i + 1, readings[i]);

        // Compute extent excluding NaN
        var validValues = readings.Where(v => !double.IsNaN(v));
        var (yMin, yMax) = D3Extent.Extent(validValues);

        var xs = new LinearScale([1, 30], [left, left + width]);
        var ys = new LinearScale([yMax + 1, yMin - 1], [top, top + height]);
        ys.Nice();

        var canvas = new Canvas { Width = canvasW, Height = canvasH };

        G.DrawGrid(canvas, ys, left, width);
        G.DrawAxes(canvas, xs, ys, left, top, width, height);

        // Draw dashed line showing where data is missing (connecting across gaps)
        var fullLine = LineGenerator.Create<(double x, double y)>(
                d => xs.Map(d.x), d => ys.Map(d.y))
            .SetDefined((d, _) => !double.IsNaN(d.y));

        // First draw a faint dashed line connecting across gaps
        var connectingData = data.Where(d => !double.IsNaN(d.y)).ToArray();
        var connectingLine = LineGenerator.Create<(double x, double y)>(
            d => xs.Map(d.x), d => ys.Map(d.y));
        var connectingPath = G.MakePath(connectingLine.Generate(connectingData),
            G.Brush(G.Palette[0], 0.2), strokeWidth: 1.5);
        connectingPath.StrokeDashArray = new DoubleCollection { 4, 3 };
        canvas.Children.Add(connectingPath);

        // Then draw the solid line with gaps
        var pathData = fullLine.Generate(data);
        canvas.Children.Add(G.MakePath(pathData, G.Brush(G.Palette[0]), strokeWidth: 2));

        // Draw data points (only for valid values)
        var dotBrush = G.Brush(G.Palette[0]);
        var missBrush = G.Brush(G.Palette[3], 0.5);
        for (int i = 0; i < data.Length; i++)
        {
            if (!double.IsNaN(data[i].y))
                G.AddEllipse(canvas, xs.Map(data[i].x), ys.Map(data[i].y), 3, dotBrush);
        }

        // Mark missing regions with a shaded band
        var bandBrush = G.Brush(G.Palette[3], 0.08);
        for (int i = 0; i < readings.Length; i++)
        {
            if (double.IsNaN(readings[i]))
            {
                double x0 = xs.Map(i + 0.5);
                double x1 = xs.Map(i + 1.5);
                G.AddRect(canvas, x0, top, x1 - x0, height, bandBrush);
            }
        }

        // Labels
        G.AddText(canvas, canvasW / 2 - 20, canvasH - 12, "Day", 11, G.Gray(80));
        G.AddText(canvas, 2, top - 14, "\u00b0C", 11, G.Gray(80));

        // Annotation
        G.AddText(canvas, left + 5, top + 5, "Shaded regions = missing sensor data", 10, G.Brush(G.Palette[3]));

        return canvas;
    }
}

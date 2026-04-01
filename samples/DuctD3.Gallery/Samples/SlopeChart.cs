using Duct.D3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public class SlopeChart : GallerySample
{
    public override string Title => "Slope Chart";
    public override string Description => "A slope chart comparing 'Before' and 'After' values for six departments, with connecting lines colored by whether values improved or declined.";
    public override string Category => "Lines";

    public override string SourceCode => @"
var ys = new LinearScale([yMax + 5, yMin - 5], [top, top + height]).Nice();
foreach (var item in items)
{
    double y1 = ys.Map(item.Before), y2 = ys.Map(item.After);
    var color = item.After >= item.Before ? G.Palette[2] : G.Palette[3];
    G.AddLine(canvas, xLeft, y1, xRight, y2, G.Brush(color), 2);
    G.AddEllipse(canvas, xLeft, y1, 5, G.Brush(color));
    G.AddEllipse(canvas, xRight, y2, 5, G.Brush(color));
}";

    public override FrameworkElement Render()
    {
        const double canvasW = 700, canvasH = 400;
        const double left = 130, top = 40, right = 130, bottom = 30;
        double width = canvasW - left - right;
        double height = canvasH - top - bottom;

        // Sample data: department satisfaction scores before and after initiative
        var items = new (string Label, double Before, double After)[]
        {
            ("Engineering", 72, 85),
            ("Marketing", 65, 61),
            ("Sales", 58, 78),
            ("Support", 80, 82),
            ("Finance", 70, 68),
            ("HR", 55, 74),
        };

        var allValues = items.Select(i => i.Before).Concat(items.Select(i => i.After));
        var (yMin, yMax) = D3Extent.Extent(allValues);

        var ys = new LinearScale([yMax + 5, yMin - 5], [top, top + height]);
        ys.Nice();

        double xLeft = left;
        double xRight = left + width;

        var canvas = new Canvas { Width = canvasW, Height = canvasH };

        // Column headers
        G.AddText(canvas, xLeft - 30, top - 30, "Before", 13, G.Gray(60), TextAlignment.Center, 60);
        G.AddText(canvas, xRight - 30, top - 30, "After", 13, G.Gray(60), TextAlignment.Center, 60);

        // Draw horizontal grid lines
        var gridBrush = G.Gray(128, 30);
        foreach (var t in ys.Ticks(6))
        {
            G.AddLine(canvas, xLeft, ys.Map(t), xRight, ys.Map(t), gridBrush);
            G.AddText(canvas, xLeft - 28, ys.Map(t) - 7, G.Fmt(t), 9, G.Gray(140));
            G.AddText(canvas, xRight + 8, ys.Map(t) - 7, G.Fmt(t), 9, G.Gray(140));
        }

        // Vertical axis lines
        var axisBrush = G.Gray(100, 180);
        G.AddLine(canvas, xLeft, top, xLeft, top + height, axisBrush);
        G.AddLine(canvas, xRight, top, xRight, top + height, axisBrush);

        // Draw slopes
        foreach (var item in items)
        {
            double y1 = ys.Map(item.Before);
            double y2 = ys.Map(item.After);
            bool improved = item.After >= item.Before;
            var color = improved ? G.Palette[2] : G.Palette[3];
            var brush = G.Brush(color);

            // Connecting line
            G.AddLine(canvas, xLeft, y1, xRight, y2, brush, 2);

            // Dots at each end
            G.AddEllipse(canvas, xLeft, y1, 5, brush);
            G.AddEllipse(canvas, xRight, y2, 5, brush);

            // Labels: item name on the left, value on the right
            G.AddText(canvas, 4, y1 - 7, $"{item.Label} ({item.Before:F0})", 10, brush, TextAlignment.Left);
            G.AddText(canvas, xRight + 30, y2 - 7, $"{item.Label} ({item.After:F0})", 10, brush, TextAlignment.Left);
        }

        // Legend
        G.AddRect(canvas, canvasW / 2 - 80, canvasH - 22, 12, 12, G.Brush(G.Palette[2]), rx: 2);
        G.AddText(canvas, canvasW / 2 - 64, canvasH - 22, "Improved", 10, G.Gray(80));
        G.AddRect(canvas, canvasW / 2 + 10, canvasH - 22, 12, 12, G.Brush(G.Palette[3]), rx: 2);
        G.AddText(canvas, canvasW / 2 + 26, canvasH - 22, "Declined", 10, G.Gray(80));

        return canvas;
    }
}

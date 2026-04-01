using Duct.D3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public class RidgePlot : GallerySample
{
    public override string Title => "Ridgeline Plot";
    public override string Description =>
        "A ridgeline (joy) plot with 5 overlapping area charts stacked vertically, " +
        "each representing a different distribution. Rows overlap slightly to create " +
        "the characteristic layered appearance.";
    public override string Category => "Areas";

    public override string SourceCode => @"
double rowHeight = plotH / rows;
double overlap = rowHeight * 0.45;

for (int r = 0; r < rows; r++)
{
    double baselineY = marginTop + r * (rowHeight - overlap) + rowHeight;
    var yScale = new LinearScale([0, peak], [baselineY, baselineY - rowHeight]);

    var area = AreaGenerator.Create<(double x, double y)>(
        d => xScale.Map(d.x),
        _ => baselineY,       // baseline
        d => yScale.Map(d.y));
    string? path = area.Generate(distribution);
}";

    public override FrameworkElement Render()
    {
        const double W = 700, H = 400;
        const double marginLeft = 80, marginTop = 25, marginRight = 20, marginBottom = 20;
        double plotW = W - marginLeft - marginRight;
        double plotH = H - marginTop - marginBottom;

        const int rows = 5;
        const int points = 50;
        string[] labels = ["Group A", "Group B", "Group C", "Group D", "Group E"];

        // Generate distribution-like data for each row
        // Each has a different center and spread, like kernel density estimates
        double[][] distributions = new double[rows][];
        double[] centers = [0.25, 0.40, 0.55, 0.35, 0.65];
        double[] spreads = [0.12, 0.15, 0.10, 0.18, 0.13];
        double[] heights = [1.0, 0.8, 1.2, 0.7, 0.9];
        double[] secondaryCenters = [0.60, 0.70, 0.80, 0.65, 0.30];
        double[] secondaryHeights = [0.4, 0.3, 0.5, 0.6, 0.35];

        double globalPeak = 0;
        for (int r = 0; r < rows; r++)
        {
            distributions[r] = new double[points];
            for (int i = 0; i < points; i++)
            {
                double t = i / (double)(points - 1);
                // Primary Gaussian bump
                double v = heights[r] * Math.Exp(-Math.Pow((t - centers[r]) / spreads[r], 2) / 2);
                // Secondary bump for bimodal effect
                v += secondaryHeights[r] * Math.Exp(-Math.Pow((t - secondaryCenters[r]) / (spreads[r] * 0.8), 2) / 2);
                // Small noise-like ripple
                v += 0.05 * Math.Sin(t * Math.PI * 12 + r * 1.7);
                distributions[r][i] = Math.Max(v, 0);
                if (v > globalPeak) globalPeak = v;
            }
        }

        var xScale = new LinearScale([0, points - 1], [marginLeft, marginLeft + plotW]);

        double rowStep = plotH / rows;
        double overlap = rowStep * 0.45;

        var canvas = new Canvas { Width = W, Height = H };

        // Draw rows from back (bottom) to front (top) so front overlaps back
        for (int r = rows - 1; r >= 0; r--)
        {
            double baselineY = marginTop + r * (rowStep - overlap) + rowStep + overlap;

            var yScale = new LinearScale([0, globalPeak * 1.1],
                [baselineY, baselineY - rowStep * 1.2]);

            var pts = new (double x, double y)[points];
            for (int i = 0; i < points; i++)
                pts[i] = (i, distributions[r][i]);

            // Area fill
            var area = AreaGenerator.Create<(double x, double y)>(
                d => xScale.Map(d.x),
                _ => baselineY,
                d => yScale.Map(d.y));
            string? areaPath = area.Generate(pts);

            if (areaPath != null)
            {
                // White background to occlude rows behind
                canvas.Children.Add(G.MakePath(areaPath,
                    fill: G.Brush(D3Color.Parse("#ffffff"), 0.85)));
                // Colored fill
                canvas.Children.Add(G.MakePath(areaPath,
                    fill: G.Brush(G.Palette[r], 0.55)));
            }

            // Stroke line on top
            var line = LineGenerator.Create<(double x, double y)>(
                d => xScale.Map(d.x),
                d => yScale.Map(d.y))
                .SetCurve(D3Curve.Natural);
            string? linePath = line.Generate(pts);
            if (linePath != null)
            {
                canvas.Children.Add(G.MakePath(linePath,
                    stroke: G.Brush(G.Palette[r]), strokeWidth: 1.5));
            }

            // Baseline
            G.AddLine(canvas, marginLeft, baselineY, marginLeft + plotW, baselineY,
                G.Gray(210), 0.5);

            // Row label
            G.AddText(canvas, 4, baselineY - rowStep * 0.5 - 6, labels[r], 11, G.Gray(80));
        }

        // Title
        G.AddText(canvas, marginLeft, 4, "Ridgeline Plot", 14, G.Gray(40));

        return canvas;
    }
}

using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using static Duct.D3.Charts.D3;
using static Duct.UI;

namespace DuctD3.Gallery;

public class RidgePlot : GallerySample
{
    public override string Title => "Ridgeline Plot";
    public override string Description =>
        "A ridgeline (joy) plot with 5 overlapping area charts stacked vertically, " +
        "each representing a different distribution. Rows overlap slightly to create " +
        "the characteristic layered appearance.";
    public override string Category => "Areas";

    public override string SourceCode => """
        double rowHeight = plotH / rows;
        double overlap = rowHeight * 0.45;

        for (int r = 0; r < rows; r++)
        {
            double baselineY = marginTop + r * (rowStep - overlap) + rowStep + overlap;
            var yScale = new LinearScale([0, peak], [baselineY, baselineY - rowStep * 1.2]);

            var area = AreaGenerator.Create<(double x, double y)>(
                d => xScale.Map(d.x),
                _ => baselineY,
                d => yScale.Map(d.y));
            string? path = area.Generate(distribution);
            D3Path(path, fill: Brush(D3Color.Parse("#ffffff"), opacity: 0.85))
            D3Path(path, fill: Brush(Palette[r], opacity: 0.55))
        }
        """;

    public override Element Render()
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

        // Draw rows from back (bottom) to front (top) so front overlaps back
        return D3Canvas(W, H,
        [
            .. Enumerable.Range(0, rows).Reverse().SelectMany(r =>
            {
                double baselineY = marginTop + r * (rowStep - overlap) + rowStep + overlap;

                var yScale = new LinearScale([0, globalPeak * 1.1],
                    [baselineY, baselineY - rowStep * 1.2]);

                var pts = Enumerable.Range(0, points)
                    .Select(i => (x: (double)i, y: distributions[r][i]))
                    .ToArray();

                var area = AreaGenerator.Create<(double x, double y)>(
                    d => xScale.Map(d.x),
                    _ => baselineY,
                    d => yScale.Map(d.y));
                string? areaPath = area.Generate(pts);

                var line = LineGenerator.Create<(double x, double y)>(
                    d => xScale.Map(d.x),
                    d => yScale.Map(d.y))
                    .SetCurve(D3Curve.Natural);
                string? linePath = line.Generate(pts);

                return (Element[])
                [
                    D3Path(areaPath, fill: Brush(D3Color.Parse("#ffffff"), opacity: 0.85)),
                    D3Path(areaPath, fill: Brush(Palette[r], opacity: 0.55)),
                    D3Path(linePath, stroke: Brush(Palette[r]), strokeWidth: 1.5),
                    D3Line(marginLeft, baselineY, marginLeft + plotW, baselineY) with { Stroke = Gray(210), StrokeThickness = 0.5 },
                    D3Text(4, baselineY - rowStep * 0.5 - 6, labels[r], 11, Gray(80)),
                ];
            }),
            D3Text(marginLeft, 4, "Ridgeline Plot", 14, Gray(40)),
        ]);
    }
}

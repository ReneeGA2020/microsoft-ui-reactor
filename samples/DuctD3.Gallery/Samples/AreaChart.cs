using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using static Duct.D3.Charts.D3;
using static Duct.UI;

namespace DuctD3.Gallery;

public class AreaChart : GallerySample
{
    public override string Title => "Area Chart";
    public override string Description =>
        "A simple area chart with 20 smoothly varying data points. " +
        "Uses AreaGenerator to fill beneath the curve with a semi-transparent color " +
        "and draws a solid stroke line on top.";
    public override string Category => "Areas";

    public override string SourceCode => """
        var area = AreaGenerator.Create<(double x, double y)>(
            d => xScale.Map(d.x),
            d => yScale.Map(0),
            d => yScale.Map(d.y));
        string? areaPath = area.Generate(data);
        var line = LineGenerator.Create<(double x, double y)>(...)
            .SetCurve(D3Curve.MonotoneX);
        D3Canvas(W, H,
            ..D3Grid(yScale, marginLeft, plotW),
            ..D3Axes(xScale, yScale, marginLeft, marginTop, plotW, plotH),
            D3Path(areaPath, fill: Brush(Palette[0], opacity: 0.3)),
            D3Path(linePath, stroke: Brush(Palette[0]), strokeWidth: 2),
            ..dots
        )
        """;

    public override Element Render()
    {
        const double W = 700, H = 400;
        const double marginLeft = 50, marginTop = 20, marginRight = 20, marginBottom = 40;
        double plotW = W - marginLeft - marginRight;
        double plotH = H - marginTop - marginBottom;

        // Generate 20 smooth data points
        var data = new (double x, double y)[20];
        double phase = 1.2;
        for (int i = 0; i < 20; i++)
        {
            double t = i / 19.0;
            data[i] = (i, 30 + 50 * Math.Sin(t * Math.PI * 2 * 0.8 + phase)
                           + 20 * Math.Sin(t * Math.PI * 4 + 0.5)
                           + 10 * Math.Cos(t * Math.PI * 3));
        }

        var (yMin, yMax) = D3Extent.Extent(data, d => d.y);
        var xScale = new LinearScale([0, 19], [marginLeft, marginLeft + plotW]);
        var yScale = new LinearScale([0, Math.Max(yMax * 1.1, 10)], [marginTop + plotH, marginTop]);
        yScale.Nice();

        // Area generator
        var area = AreaGenerator.Create<(double x, double y)>(
            d => xScale.Map(d.x),
            d => yScale.Map(0),
            d => yScale.Map(d.y));
        string? areaPath = area.Generate(data);

        // Line generator with monotone curve
        var line = LineGenerator.Create<(double x, double y)>(
            d => xScale.Map(d.x),
            d => yScale.Map(d.y))
            .SetCurve(D3Curve.MonotoneX);
        string? linePath = line.Generate(data);

        var dots = data.Select(d =>
            (Element)(D3Circle(xScale.Map(d.x), yScale.Map(d.y), 3) with { Fill = Brush(Palette[0]) }));

        return D3Canvas(W, H,
            [.. D3Grid(yScale, marginLeft, plotW),
             .. D3Axes(xScale, yScale, marginLeft, marginTop, plotW, plotH),
             D3Path(areaPath, fill: Brush(Palette[0], opacity: 0.3)),
             D3Path(linePath, stroke: Brush(Palette[0]), strokeWidth: 2),
             .. dots,
             D3Text(marginLeft, 2, "Area Chart", 14, Gray(40))]
        );
    }
}

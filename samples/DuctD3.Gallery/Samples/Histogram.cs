using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public sealed class HistogramSample : GallerySample
{
    public override string Title => "Histogram";
    public override string Description => "A histogram of 200 normally-distributed random values binned with BinGenerator and drawn as bars.";
    public override string Category => "Analysis";

    public override string SourceCode => """
        // Box-Muller transform for normal distribution
        var rng = new Random(99);
        var values = Enumerable.Range(0, 200).Select(_ => {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = rng.NextDouble();
            return 50 + 15 * Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
        }).ToArray();
        var binner = BinGenerator.Create().SetThresholdCount(20);
        var bins = binner.Generate(values);
        int maxCount = bins.Max(b => b.Count);
        var xs = new LinearScale([bins[0].X0, bins[^1].X1], [left, left+pw]);
        var ys = new LinearScale([0, maxCount], [top+ph, top]).Nice();
        foreach (var bin in bins)
            G.AddRect(canvas, xs.Map(bin.X0)+1, ys.Map(bin.Count),
                xs.Map(bin.X1)-xs.Map(bin.X0)-2, ys.Map(0)-ys.Map(bin.Count), fill);
        """;

    public override FrameworkElement Render()
    {
        const double width = 700, height = 400;
        const double left = 50, top = 30, right = 20, bottom = 30;
        double pw = width - left - right;
        double ph = height - top - bottom;

        var canvas = new Canvas { Width = width, Height = height };

        // Generate 200 normally-distributed values using Box-Muller
        var rng = new Random(99);
        var values = Enumerable.Range(0, 200).Select(_ =>
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = rng.NextDouble();
            return 50 + 15 * Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
        }).ToArray();

        var binner = BinGenerator.Create().SetThresholdCount(20);
        var bins = binner.Generate(values);

        if (bins.Length == 0) return canvas;

        int maxCount = bins.Max(b => b.Count);

        var xs = new LinearScale([bins[0].X0, bins[^1].X1], [left, left + pw]);
        var ys = new LinearScale([0, maxCount], [top + ph, top]).Nice();

        G.DrawGrid(canvas, ys, left, pw);
        G.DrawAxes(canvas, xs, ys, left, top, pw, ph);

        var fill = G.Brush(G.Palette[0], 0.7);

        foreach (var bin in bins)
        {
            double bx = xs.Map(bin.X0) + 1;
            double bw = xs.Map(bin.X1) - xs.Map(bin.X0) - 2;
            double by = ys.Map(bin.Count);
            double bh = ys.Map(0) - by;
            G.AddRect(canvas, bx, by, bw, bh, fill);
        }

        G.AddText(canvas, left, 6, "Histogram (normal distribution, 200 values)", 14, G.Gray(40));

        return canvas;
    }
}

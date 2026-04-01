using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using static Duct.D3.Charts.D3;
using static Duct.UI;

namespace DuctD3.Gallery;

public class CandlestickChart : GallerySample
{
    public override string Title => "Candlestick Chart";
    public override string Description => "An OHLC candlestick chart showing 20 trading days. Green candles indicate close > open (bullish), red candles indicate close < open (bearish).";
    public override string Category => "Lines";

    public override string SourceCode => """
        D3Canvas(canvasW, canvasH,
            [..D3Grid(ys, left, width),
             ..D3Axes(xs, ys, left, top, width, height),
             ..candles.SelectMany((c, i) => {
                 var brush = c.Close >= c.Open ? bullBrush : bearBrush;
                 return new Element[] {
                     D3Line(cx, ys.Map(c.High), cx, ys.Map(c.Low))
                         with { Stroke = brush, StrokeThickness = 1.5 },
                     D3Rect(cx - barW/2, bodyTop, barW, bodyH)
                         with { Fill = brush },
                 };
             }),
            ]
        )
        """;

    public override Element Render()
    {
        const double canvasW = 700, canvasH = 400;
        const double left = 55, top = 20, right = 20, bottom = 40;
        double width = canvasW - left - right;
        double height = canvasH - top - bottom;

        // Generate 20 days of realistic OHLC data
        var candles = new (double Open, double High, double Low, double Close)[20];
        double price = 150.0;
        // Deterministic pseudo-random using a simple seed
        int seed = 42;
        double NextRand()
        {
            seed = (seed * 1103515245 + 12345) & 0x7fffffff;
            return (double)seed / 0x7fffffff;
        }

        for (int i = 0; i < 20; i++)
        {
            double open = price;
            double change = (NextRand() - 0.48) * 6; // slight upward bias
            double close = open + change;
            double highExtra = NextRand() * 3 + 0.5;
            double lowExtra = NextRand() * 3 + 0.5;
            double high = Math.Max(open, close) + highExtra;
            double low = Math.Min(open, close) - lowExtra;
            candles[i] = (Math.Round(open, 2), Math.Round(high, 2), Math.Round(low, 2), Math.Round(close, 2));
            price = close;
        }

        // Compute extent over all high/low values
        var allHigh = candles.Select(c => c.High);
        var allLow = candles.Select(c => c.Low);
        var (yMin, _) = D3Extent.Extent(allLow);
        var (_, yMax) = D3Extent.Extent(allHigh);

        var xs = new LinearScale([0, 19], [left + 15, left + width - 15]);
        var ys = new LinearScale([yMax + 2, yMin - 2], [top, top + height]);
        ys.Nice();

        double barW = width / 20 * 0.6;

        // Bullish and bearish brushes
        var bullBrush = Brush("#26a269");
        var bearBrush = Brush("#e01b24");

        return D3Canvas(canvasW, canvasH,
            [.. D3Grid(ys, left, width),
             .. D3Axes(xs, ys, left, top, width, height),
             // Candlesticks
             .. (from t in candles.Select((c, i) => (c, i))
                 let cx = xs.Map(t.i)
                 let bullish = t.c.Close >= t.c.Open
                 let brush = bullish ? bullBrush : bearBrush
                 let bodyTop = ys.Map(Math.Max(t.c.Open, t.c.Close))
                 let bodyH = Math.Max(ys.Map(Math.Min(t.c.Open, t.c.Close)) - bodyTop, 1)
                 from el in new Element[]
                 {
                     D3Line(cx, ys.Map(t.c.High), cx, ys.Map(t.c.Low)) with { Stroke = brush, StrokeThickness = 1.5 },
                     D3Rect(cx - barW / 2, bodyTop, barW, bodyH) with { Fill = brush },
                 }
                 select el),
             // X-axis labels (every 5 days)
             .. Enumerable.Range(0, 4).Select(n => n * 5)
                 .Select(i => D3Text(xs.Map(i) - 12, top + height + 4, $"Day {i + 1}", 10, Gray(100))),
             D3Text(2, top - 14, "Price", 11, Gray(80)),
             D3Rect(left + width - 120, top + 5, 12, 12) with { Fill = bullBrush, RadiusX = 2, RadiusY = 2 },
             D3Text(left + width - 104, top + 5, "Bullish", 10, Gray(80)),
             D3Rect(left + width - 55, top + 5, 12, 12) with { Fill = bearBrush, RadiusX = 2, RadiusY = 2 },
             D3Text(left + width - 39, top + 5, "Bearish", 10, Gray(80))]
        );
    }
}

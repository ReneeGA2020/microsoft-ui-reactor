using Duct.D3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public class CandlestickChart : GallerySample
{
    public override string Title => "Candlestick Chart";
    public override string Description => "An OHLC candlestick chart showing 20 trading days. Green candles indicate close > open (bullish), red candles indicate close < open (bearish).";
    public override string Category => "Lines";

    public override string SourceCode => @"
foreach (var (day, candle) in candles.Select((c, i) => (i, c)))
{
    double cx = xs.Map(day);
    bool bullish = candle.Close >= candle.Open;
    var brush = G.Brush(bullish ? ""#26a269"" : ""#e01b24"");
    // Wick: high to low
    G.AddLine(canvas, cx, ys.Map(candle.High), cx, ys.Map(candle.Low), brush, 1);
    // Body: open to close
    double bodyTop = ys.Map(Math.Max(candle.Open, candle.Close));
    double bodyBot = ys.Map(Math.Min(candle.Open, candle.Close));
    G.AddRect(canvas, cx - barW/2, bodyTop, barW, bodyBot - bodyTop, brush);
}";

    public override FrameworkElement Render()
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

        var canvas = new Canvas { Width = canvasW, Height = canvasH };

        G.DrawGrid(canvas, ys, left, width);
        G.DrawAxes(canvas, xs, ys, left, top, width, height);

        // Bullish and bearish brushes
        var bullBrush = G.Brush("#26a269");
        var bearBrush = G.Brush("#e01b24");

        for (int i = 0; i < candles.Length; i++)
        {
            var c = candles[i];
            double cx = xs.Map(i);
            bool bullish = c.Close >= c.Open;
            var brush = bullish ? bullBrush : bearBrush;

            // Wick line: high to low
            G.AddLine(canvas, cx, ys.Map(c.High), cx, ys.Map(c.Low), brush, 1.5);

            // Body rectangle: open to close
            double bodyTop = ys.Map(Math.Max(c.Open, c.Close));
            double bodyBot = ys.Map(Math.Min(c.Open, c.Close));
            double bodyH = Math.Max(bodyBot - bodyTop, 1); // minimum 1px
            G.AddRect(canvas, cx - barW / 2, bodyTop, barW, bodyH, brush);
        }

        // X-axis labels (every 5 days)
        for (int i = 0; i < 20; i += 5)
            G.AddText(canvas, xs.Map(i) - 12, top + height + 4, $"Day {i + 1}", 10, G.Gray(100));

        // Y-axis label
        G.AddText(canvas, 2, top - 14, "Price", 11, G.Gray(80));

        // Legend
        G.AddRect(canvas, left + width - 120, top + 5, 12, 12, bullBrush, rx: 2);
        G.AddText(canvas, left + width - 104, top + 5, "Bullish", 10, G.Gray(80));
        G.AddRect(canvas, left + width - 55, top + 5, 12, 12, bearBrush, rx: 2);
        G.AddText(canvas, left + width - 39, top + 5, "Bearish", 10, G.Gray(80));

        return canvas;
    }
}

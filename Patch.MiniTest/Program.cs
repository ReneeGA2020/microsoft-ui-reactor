using Patch;
using Patch.Core;
using static Patch.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

PatchApp.Run<Calculator>("Calculator", width: 380, height: 620);

class Calculator : Component
{
    static readonly SolidColorBrush NumBg  = new(Rgb(51, 51, 51));
    static readonly SolidColorBrush FuncBg = new(Rgb(165, 165, 165));
    static readonly SolidColorBrush OpBg   = new(Rgb(255, 159, 10));
    static readonly SolidColorBrush WFg    = new(Rgb(255, 255, 255));
    static readonly SolidColorBrush BFg    = new(Rgb(0, 0, 0));

    static Windows.UI.Color Rgb(byte r, byte g, byte b) =>
        Windows.UI.Color.FromArgb(255, r, g, b);

    public override Element Render()
    {
        var (display, setDisplay) = UseState("0");
        var (first, setFirst) = UseState(0.0);
        var (op, setOp) = UseState("");
        var (fresh, setFresh) = UseState(true);

        double Val() => double.TryParse(display, out var v) ? v : 0;

        void Digit(string d)
        {
            if (display == "Error" || fresh)
            { setDisplay(d == "." ? "0." : d); setFresh(false); }
            else if (d == "." && display.Contains('.')) { }
            else if (display.Replace(".", "").Replace("-", "").Length >= 9) { }
            else setDisplay(display == "0" && d != "." ? d : display + d);
        }

        void SetOp(string next)
        {
            if (!fresh && op != "")
            { var r = Compute(first, op, Val()); setDisplay(Fmt(r)); setFirst(r); }
            else setFirst(Val());
            setOp(next); setFresh(true);
        }

        void Calculate()
        {
            if (op == "") return;
            var r = Compute(first, op, Val());
            setDisplay(Fmt(r)); setFirst(r); setOp(""); setFresh(true);
        }

        void Clear()   { setDisplay("0"); setFirst(0); setOp(""); setFresh(true); }
        void Negate()  { setDisplay(Fmt(-Val())); }
        void Percent() { setDisplay(Fmt(Val() / 100)); }

        // Shrink font for long numbers, like iOS
        var fontSize = display.Length <= 6 ? 64 : display.Length <= 9 ? 48 : 36;

        return Border(
            Grid(
                columns: ["*", "*", "*", "*"],
                rows: ["*", "72", "72", "72", "72", "72"],

                Cell(Text(display).Bold().FontSize(fontSize)
                    .Set(t => { t.Foreground = WFg; t.TextAlignment = TextAlignment.Right; })
                    .VAlign(VerticalAlignment.Bottom).Margin(24, 0, 24, 12),
                    row: 0, column: 0, columnSpan: 4),

                Cell(Btn("C",  Clear,            FuncBg, BFg), row: 1, column: 0),
                Cell(Btn("±",  Negate,           FuncBg, BFg), row: 1, column: 1),
                Cell(Btn("%",  Percent,           FuncBg, BFg), row: 1, column: 2),
                Cell(Btn("÷",  () => SetOp("÷"), OpBg),       row: 1, column: 3),

                Cell(Btn("7",  () => Digit("7")),              row: 2, column: 0),
                Cell(Btn("8",  () => Digit("8")),              row: 2, column: 1),
                Cell(Btn("9",  () => Digit("9")),              row: 2, column: 2),
                Cell(Btn("×",  () => SetOp("×"), OpBg),       row: 2, column: 3),

                Cell(Btn("4",  () => Digit("4")),              row: 3, column: 0),
                Cell(Btn("5",  () => Digit("5")),              row: 3, column: 1),
                Cell(Btn("6",  () => Digit("6")),              row: 3, column: 2),
                Cell(Btn("−",  () => SetOp("−"), OpBg),       row: 3, column: 3),

                Cell(Btn("1",  () => Digit("1")),              row: 4, column: 0),
                Cell(Btn("2",  () => Digit("2")),              row: 4, column: 1),
                Cell(Btn("3",  () => Digit("3")),              row: 4, column: 2),
                Cell(Btn("+",  () => SetOp("+"), OpBg),       row: 4, column: 3),

                Cell(Btn("0",  () => Digit("0")),              row: 5, column: 0, columnSpan: 2),
                Cell(Btn(".",  () => Digit(".")),              row: 5, column: 2),
                Cell(Btn("=",  Calculate,         OpBg),       row: 5, column: 3)
            )
        ).Background("#000000");
    }

    static double Compute(double a, string op, double b) => op switch
    {
        "+" => a + b, "−" => a - b, "×" => a * b,
        "÷" => b != 0 ? a / b : double.NaN, _ => b
    };

    static string Fmt(double v)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) return "Error";
        var s = v.ToString("G10");
        return s.Length > 12 ? v.ToString("G6") : s;
    }

    static Element Btn(string label, Action click, SolidColorBrush? bg = null,
                        SolidColorBrush? fg = null) =>
        Button(label, click)
            .Set(b =>
            {
                b.Background = bg ?? NumBg;
                b.Foreground = fg ?? WFg;
                b.CornerRadius = new CornerRadius(36);
                b.BorderThickness = new Thickness(0);
                b.FontSize = 26;
                b.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                b.HorizontalAlignment = HorizontalAlignment.Stretch;
                b.VerticalAlignment = VerticalAlignment.Stretch;
            })
            .Margin(3);
}
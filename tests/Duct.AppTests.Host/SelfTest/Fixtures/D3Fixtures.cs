using Duct;
using Duct.Core;
using Duct.D3.Charts;
using Duct.AppTests.Host.SelfTest;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static Duct.D3.Charts.ChartDsl;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace Duct.AppTests.Host.SelfTest.Fixtures;

internal static class D3Fixtures
{
    private record DataPoint(double X, double Y);

    private static readonly DataPoint[] SampleLine =
        Enumerable.Range(0, 10).Select(i => new DataPoint(i, Math.Sin(i * 0.5) * 50 + 50)).ToArray();

    private static readonly DataPoint[] SampleBars =
    [
        new(0, 30), new(1, 70), new(2, 45), new(3, 90), new(4, 55)
    ];

    private record PieSlice(string Label, double Value);
    private static readonly PieSlice[] SamplePie =
    [
        new("A", 30), new("B", 20), new("C", 35), new("D", 15)
    ];

    internal class LineChart(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            // D3 charts use XamlHostElement which requires XamlInterop registration
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx =>
                VStack(
                    Text("Line Chart"),
                    ChartDsl.LineChart(SampleLine, d => d.X, d => d.Y)
                        .Width(600).Height(400)
                        .ShowAxes(true)
                        .ShowGrid(true)
                        .ToElement()
                )
            );

            await Harness.Render(800);

            H.Check("D3_LineChart_TitleVisible",
                H.FindText("Line Chart") is not null);

            var canvas = H.FindControl<Canvas>(_ => true);
            H.Check("D3_LineChart_CanvasCreated",
                canvas is not null);

            H.Check("D3_LineChart_HasChildren",
                canvas is not null && canvas.Children.Count > 0);
        }
    }

    internal class BarChart(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx =>
                VStack(
                    Text("Bar Chart"),
                    ChartDsl.BarChart(SampleBars, d => d.X, d => d.Y)
                        .Width(600).Height(400)
                        .ShowAxes(true)
                        .ToElement()
                )
            );

            await Harness.Render(800);

            H.Check("D3_BarChart_TitleVisible",
                H.FindText("Bar Chart") is not null);

            var canvas = H.FindControl<Canvas>(_ => true);
            H.Check("D3_BarChart_CanvasCreated",
                canvas is not null);

            var rects = H.FindAllControls<WinShapes.Rectangle>(_ => true);
            H.Check("D3_BarChart_HasRectangles",
                rects.Count >= SampleBars.Length);
        }
    }

    internal class PieChart(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx =>
                VStack(
                    Text("Pie Chart"),
                    ChartDsl.PieChart(SamplePie, d => d.Value, d => d.Label)
                        .Width(400).Height(400)
                        .ToElement()
                )
            );

            await Harness.Render(800);

            H.Check("D3_PieChart_TitleVisible",
                H.FindText("Pie Chart") is not null);

            var canvas = H.FindControl<Canvas>(_ => true);
            H.Check("D3_PieChart_CanvasCreated",
                canvas is not null);

            var paths = H.FindAllControls<WinShapes.Path>(_ => true);
            H.Check("D3_PieChart_HasArcs",
                paths.Count >= SamplePie.Length);
        }
    }
}

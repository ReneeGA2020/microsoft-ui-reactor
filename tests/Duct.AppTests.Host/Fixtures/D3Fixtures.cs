using Duct;
using Duct.Core;
using Duct.D3.Charts;
using static Duct.UI;
using static Duct.D3.Charts.ChartDsl;

namespace Duct.AppTests.Host.Fixtures;

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

    internal static Element LineChart(RenderContext ctx) =>
        VStack(
            Text("Line Chart").AutomationId("LineChartTitle"),
            ChartDsl.LineChart(SampleLine, d => d.X, d => d.Y)
                .Width(600).Height(400)
                .ShowAxes(true)
                .ShowGrid(true)
                .ToElement()
                .AutomationId("LineChartCanvas")
        );

    internal static Element BarChart(RenderContext ctx) =>
        VStack(
            Text("Bar Chart").AutomationId("BarChartTitle"),
            ChartDsl.BarChart(SampleBars, d => d.X, d => d.Y)
                .Width(600).Height(400)
                .ShowAxes(true)
                .ToElement()
                .AutomationId("BarChartCanvas")
        );

    internal static Element PieChart(RenderContext ctx) =>
        VStack(
            Text("Pie Chart").AutomationId("PieChartTitle"),
            ChartDsl.PieChart(SamplePie, d => d.Value, d => d.Label)
                .Width(400).Height(400)
                .ToElement()
                .AutomationId("PieChartCanvas")
        );
}

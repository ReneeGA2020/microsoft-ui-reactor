using Duct;
using Duct.Core;
using Duct.Flex;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace Duct.EndToEnd.App.Fixtures;

internal static class LayoutFixtures
{
    internal class FlexRowDistribution(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                FlexRow(
                    Text("A").Flex(grow: 1).Background("Red"),
                    Text("B").Flex(grow: 2).Background("Green"),
                    Text("C").Flex(grow: 1).Background("Blue")
                ).Width(600).Height(100)
            );

            await Harness.Render(500); // extra time for Yoga layout

            H.Check("FlexLayout_RowDistribution_AllItemsPresent",
                H.FindText("A") is not null &&
                H.FindText("B") is not null &&
                H.FindText("C") is not null);

            // Verify a Duct.Flex.FlexPanel was created
            H.Check("FlexLayout_RowDistribution_FlexPanelCreated",
                H.FindControl<Duct.Flex.FlexPanel>(_ => true) is not null);

            // Verify the flex panel has 3 children
            var flexPanel = H.FindControl<Duct.Flex.FlexPanel>(_ => true);
            H.Check("FlexLayout_RowDistribution_HasThreeChildren",
                flexPanel?.Children.Count >= 3);
        }
    }

    internal class FlexColumnWrap(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
            {
                var children = Enumerable.Range(0, 8)
                    .Select(i => Text($"Box {i}").Width(80).Height(80))
                    .ToArray();
                return new FlexElement(children)
                {
                    Direction = Duct.Flex.FlexDirection.Column,
                    Wrap = Duct.Flex.FlexWrap.Wrap,
                }.Width(400).Height(200);
            });

            await Harness.Render(500);

            H.Check("FlexLayout_ColumnWrap_AllItemsPresent",
                H.FindText("Box 0") is not null && H.FindText("Box 7") is not null);

            // With wrap enabled, items should wrap to multiple columns
            var flexPanel = H.FindControl<Duct.Flex.FlexPanel>(_ => true);
            H.Check("FlexLayout_ColumnWrap_HasFlexPanel",
                flexPanel is not null);

            H.Check("FlexLayout_ColumnWrap_ChildCount",
                flexPanel?.Children.Count == 8);
        }
    }

    internal class GridRowColumn(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                Grid(["200", "*"], ["Auto", "*"],
                    Text("TopLeft").Grid(row: 0, column: 0),
                    Text("TopRight").Grid(row: 0, column: 1),
                    Text("BottomLeft").Grid(row: 1, column: 0),
                    Text("BottomRight").Grid(row: 1, column: 1)
                ).Width(600).Height(400)
            );

            await Harness.Render();

            H.Check("Grid_RowColumnLayout_AllCellsPresent",
                H.FindText("TopLeft") is not null &&
                H.FindText("TopRight") is not null &&
                H.FindText("BottomLeft") is not null &&
                H.FindText("BottomRight") is not null);

            var grid = H.FindControl<WinUI.Grid>(g => g.ColumnDefinitions.Count == 2);
            H.Check("Grid_RowColumnLayout_GridCreated",
                grid is not null);

            H.Check("Grid_RowColumnLayout_HasTwoRows",
                grid?.RowDefinitions.Count == 2);

            H.Check("Grid_RowColumnLayout_HasTwoColumns",
                grid?.ColumnDefinitions.Count == 2);
        }
    }

    /// <summary>
    /// Compares Grid star column sizing vs FlexPanel grow distribution.
    /// Both should produce identical proportional widths for children.
    ///
    /// Scenario 1 (simple): 3 items with 1:2:1 ratio in 600px → expect 150, 300, 150
    /// Scenario 2 (text):   Long text in a proportional column — both should constrain
    ///                       text width identically so wrapping matches.
    /// </summary>
    internal class GridVsFlexStarSizing(Harness h) : FixtureBase(h)
    {
        private const double ContainerWidth = 600;
        private const double ContainerHeight = 200;
        private const double Tolerance = 2.0; // layout rounding tolerance

        public override async Task RunAsync()
        {
            // ── Scenario 1: Simple proportional sizing (1:2:1) ──────────
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                VStack(8,
                    // Grid with 1* / 2* / 1* star columns
                    Grid(["*", "2*", "*"], ["*"],
                        Text("G1").Grid(row: 0, column: 0).Background("LightCoral"),
                        Text("G2").Grid(row: 0, column: 1).Background("LightGreen"),
                        Text("G3").Grid(row: 0, column: 2).Background("LightBlue")
                    ).Width(ContainerWidth).Height(80),

                    // FlexRow with grow 1 / 2 / 1 (basis:0 to match star behavior)
                    FlexRow(
                        Text("F1").Flex(grow: 1, basis: 0).Background("LightCoral"),
                        Text("F2").Flex(grow: 2, basis: 0).Background("LightGreen"),
                        Text("F3").Flex(grow: 1, basis: 0).Background("LightBlue")
                    ).Width(ContainerWidth).Height(80)
                ).Width(ContainerWidth).Height(ContainerHeight * 2)
            );

            await Harness.Render(800);

            // Find the Grid and FlexPanel
            var grid = H.FindControl<WinUI.Grid>(g => g.ColumnDefinitions.Count == 3);
            var flex = H.FindControl<FlexPanel>(_ => true);

            H.Check("GridVsFlex_BothPanelsCreated",
                grid is not null && flex is not null);

            if (grid is null || flex is null) return;

            // Read Grid column actual widths
            double g1Width = grid.ColumnDefinitions[0].ActualWidth;
            double g2Width = grid.ColumnDefinitions[1].ActualWidth;
            double g3Width = grid.ColumnDefinitions[2].ActualWidth;

            // Read FlexPanel children actual widths
            double f1Width = ((FrameworkElement)flex.Children[0]).RenderSize.Width;
            double f2Width = ((FrameworkElement)flex.Children[1]).RenderSize.Width;
            double f3Width = ((FrameworkElement)flex.Children[2]).RenderSize.Width;

            Console.WriteLine($"# Grid columns:  {g1Width:F1}, {g2Width:F1}, {g3Width:F1}");
            Console.WriteLine($"# Flex children: {f1Width:F1}, {f2Width:F1}, {f3Width:F1}");

            // Grid star columns: 1*+2*+1* = 4 parts of 600 → 150, 300, 150
            H.Check("GridVsFlex_GridProportionsCorrect",
                Near(g1Width, 150) && Near(g2Width, 300) && Near(g3Width, 150));

            // Flex grow: same 1:2:1 ratio with basis:0 should match
            H.Check("GridVsFlex_FlexProportionsCorrect",
                Near(f1Width, 150) && Near(f2Width, 300) && Near(f3Width, 150));

            // Cross-check: Flex widths should match Grid widths
            H.Check("GridVsFlex_ProportionsMatch",
                Near(f1Width, g1Width) && Near(f2Width, g2Width) && Near(f3Width, g3Width));

            // ── Scenario 2: Content-dependent sizing with text ──────────
            // Remount with long text in the middle column to test wrapping
            var longText = "This is a long paragraph of text that should wrap within " +
                "the proportional column width. Both Grid and Flex should constrain " +
                "this text to the same width, producing the same line wrapping.";

            host.Mount(ctx =>
                VStack(8,
                    // Grid: auto-height row, star columns
                    Grid(["*", "2*", "*"], ["Auto"],
                        Text("Left").Grid(row: 0, column: 0).Background("LightCoral"),
                        Text(longText).Grid(row: 0, column: 1).Background("LightGreen"),
                        Text("Right").Grid(row: 0, column: 2).Background("LightBlue")
                    ).Width(ContainerWidth),

                    // Flex: same ratios with long text
                    FlexRow(
                        Text("Left").Flex(grow: 1, basis: 0).Background("LightCoral"),
                        Text(longText).Flex(grow: 2, basis: 0).Background("LightGreen"),
                        Text("Right").Flex(grow: 1, basis: 0).Background("LightBlue")
                    ).Width(ContainerWidth)
                ).Width(ContainerWidth)
            );

            await Harness.Render(800);

            // Find the updated panels (find by column count / type)
            var grid2 = H.FindControl<WinUI.Grid>(g => g.ColumnDefinitions.Count == 3);
            var flex2 = H.FindControl<FlexPanel>(_ => true);

            H.Check("GridVsFlex_Text_BothPanelsCreated",
                grid2 is not null && flex2 is not null);

            if (grid2 is null || flex2 is null) return;

            // Compare using the same metric for both panels. TextBlock RenderSize
            // may reflect text content (not the layout slot) when TextWrapping=NoWrap,
            // but both Grid and Flex TextBlocks behave identically, so comparing
            // their RenderSize verifies equivalent layout behavior.
            double g2TextWidth = ((FrameworkElement)grid2.Children[1]).RenderSize.Width;
            double f2TextWidth = ((FrameworkElement)flex2.Children[1]).RenderSize.Width;
            double g2TextHeight = ((FrameworkElement)grid2.Children[1]).RenderSize.Height;
            double f2TextHeight = ((FrameworkElement)flex2.Children[1]).RenderSize.Height;

            Console.WriteLine($"# Grid text child:  {g2TextWidth:F1}w x {g2TextHeight:F1}h");
            Console.WriteLine($"# Flex text child:  {f2TextWidth:F1}w x {f2TextHeight:F1}h");

            // Text rendering dimensions should match (same text, same layout constraints)
            H.Check("GridVsFlex_Text_WidthsMatch",
                Near(f2TextWidth, g2TextWidth));

            // Text heights should match (same layout slot height)
            H.Check("GridVsFlex_Text_HeightsMatch",
                Near(f2TextHeight, g2TextHeight, 20));

            await H.CaptureScreenshotAsync("GridVsFlex_StarSizing");
        }

        private static bool Near(double a, double b, double tolerance = Tolerance)
            => Math.Abs(a - b) <= tolerance;
    }
}

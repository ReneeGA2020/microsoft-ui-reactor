using Duct;
using Duct.Core;
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
}

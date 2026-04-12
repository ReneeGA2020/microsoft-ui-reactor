using Duct;
using Duct.Core;
using Duct.Flex;
using Duct.AppTests.Host.SelfTest;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace Duct.AppTests.Host.SelfTest.Fixtures;

/// <summary>
/// Comprehensive Flex layout E2E tests covering nesting, composition with
/// other layout panels, scroll interaction, and cross-axis sizing.
/// </summary>
internal static class FlexLayoutFixtures
{
    private const double Tolerance = 2.0;

    private static bool Near(double a, double b, double tol = Tolerance)
        => Math.Abs(a - b) <= tol;

    // ----------------------------------------------------------------
    // 1. Nested Flex: FlexRow children inside FlexColumn
    // ----------------------------------------------------------------

    internal class FlexNestedRowInColumn(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                FlexColumn(
                    FlexRow(
                        Text("Logo").Flex(basis: 0, grow: 1).Background("LightCoral"),
                        Text("Nav").Flex(basis: 0, grow: 2).Background("LightBlue")
                    ).Height(50),

                    FlexRow(
                        Text("Sidebar").Flex(basis: 0, grow: 1).Background("LightGreen"),
                        Text("Content").Flex(basis: 0, grow: 3).Background("LightYellow")
                    ).Flex(grow: 1),

                    FlexRow(
                        Text("Footer").Flex(grow: 1)
                    ).Height(40).Background("LightGray")
                ).Width(600).Height(400)
            );

            await Harness.Render();

            H.Check("FlexNested_RowInCol_AllPresent",
                H.FindText("Logo") is not null &&
                H.FindText("Nav") is not null &&
                H.FindText("Sidebar") is not null &&
                H.FindText("Content") is not null &&
                H.FindText("Footer") is not null);

            var outerCol = H.FindControl<FlexPanel>(p =>
                FlexPanel.GetGrow(p) == 0 && p.Direction == FlexDirection.Column);
            H.Check("FlexNested_RowInCol_OuterColumnExists", outerCol is not null);

            var rows = H.FindAllControls<FlexPanel>(p => p.Direction == FlexDirection.Row);
            H.Check("FlexNested_RowInCol_ThreeInnerRows", rows.Count >= 3);

            if (outerCol is not null)
            {
                var bodyRow = rows.FirstOrDefault(r => FlexPanel.GetGrow(r) > 0);
                H.Check("FlexNested_RowInCol_BodyGrew",
                    bodyRow is not null && Near(bodyRow.ActualHeight, 310, 5));

                var contentText = H.FindText("Content");
                if (contentText is not null)
                {
                    H.Check("FlexNested_RowInCol_ContentWidth",
                        Near(contentText.RenderSize.Width, 450, 5));
                }
            }
        }
    }

    // ----------------------------------------------------------------
    // 2. Nested Flex: FlexColumn inside FlexRow
    // ----------------------------------------------------------------

    internal class FlexNestedColumnInRow(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                FlexRow(
                    FlexColumn(
                        Text("Menu A"),
                        Text("Menu B"),
                        Text("Menu C")
                    ).Flex(basis: 0, grow: 1).Background("LightCoral"),

                    FlexColumn(
                        Text("Title").Height(40),
                        Text("Body text here").Flex(grow: 1)
                    ).Flex(basis: 0, grow: 3).Background("LightBlue")
                ).Width(600).Height(300)
            );

            await Harness.Render();

            H.Check("FlexNested_ColInRow_AllPresent",
                H.FindText("Menu A") is not null &&
                H.FindText("Title") is not null &&
                H.FindText("Body text here") is not null);

            var columns = H.FindAllControls<FlexPanel>(p => p.Direction == FlexDirection.Column);
            var sidebar = columns.FirstOrDefault(c => c.ActualWidth < 200);
            H.Check("FlexNested_ColInRow_SidebarWidth",
                sidebar is not null && Near(sidebar.ActualWidth, 150, 5));

            var content = columns.FirstOrDefault(c => c.ActualWidth > 400);
            H.Check("FlexNested_ColInRow_ContentWidth",
                content is not null && Near(content.ActualWidth, 450, 5));
        }
    }

    // ----------------------------------------------------------------
    // 3. Deep nesting: 3+ levels of FlexPanels
    // ----------------------------------------------------------------

    internal class FlexNestedDeep(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                FlexColumn(
                    Text("L1-Header").Height(30),

                    FlexRow(
                        Text("L2-Left").Flex(basis: 0, grow: 1),

                        FlexColumn(
                            Text("L3-Top"),
                            Text("L3-Mid").Flex(grow: 1),
                            Text("L3-Bot")
                        ).Flex(basis: 0, grow: 2)
                    ).Flex(grow: 1),

                    Text("L1-Footer").Height(30)
                ).Width(600).Height(400)
            );

            await Harness.Render();

            H.Check("FlexNested_Deep_AllPresent",
                H.FindText("L1-Header") is not null &&
                H.FindText("L2-Left") is not null &&
                H.FindText("L3-Top") is not null &&
                H.FindText("L3-Mid") is not null &&
                H.FindText("L3-Bot") is not null &&
                H.FindText("L1-Footer") is not null);

            var allFlex = H.FindAllControls<FlexPanel>(_ => true);
            H.Check("FlexNested_Deep_ThreePanels", allFlex.Count >= 3);

            var l2Left = H.FindText("L2-Left");
            H.Check("FlexNested_Deep_L2LeftWidth",
                l2Left is not null && Near(l2Left.RenderSize.Width, 200, 5));

            var l3Columns = H.FindAllControls<FlexPanel>(p =>
                p.Direction == FlexDirection.Column && p.ActualWidth > 350 && p.ActualWidth < 450);
            H.Check("FlexNested_Deep_L3ColumnWidth", l3Columns.Count >= 1);
        }
    }

    // ----------------------------------------------------------------
    // 4. Flex inside Grid star cells
    // ----------------------------------------------------------------

    internal class FlexInsideGrid(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                Grid(["*", "2*"], ["*", "*"],
                    FlexRow(
                        Text("A1").Flex(grow: 1).Background("LightCoral"),
                        Text("A2").Flex(grow: 1).Background("LightBlue")
                    ).Grid(row: 0, column: 0),

                    FlexColumn(
                        Text("B1").Flex(grow: 1).Background("LightGreen"),
                        Text("B2").Flex(grow: 2).Background("LightYellow")
                    ).Grid(row: 0, column: 1),

                    FlexRow(
                        Text("C1").Flex(grow: 1).Background("Wheat")
                    ).Grid(row: 1, column: 0),

                    FlexRow(
                        Text("D1").Width(80).Background("Plum"),
                        Text("D2").Flex(grow: 1).Background("PeachPuff")
                    ).Grid(row: 1, column: 1)
                ).Width(600).Height(400)
            );

            await Harness.Render();

            H.Check("FlexInGrid_AllPresent",
                H.FindText("A1") is not null &&
                H.FindText("B1") is not null &&
                H.FindText("C1") is not null &&
                H.FindText("D1") is not null &&
                H.FindText("D2") is not null);

            var grid = H.FindControl<WinUI.Grid>(g => g.ColumnDefinitions.Count == 2);
            H.Check("FlexInGrid_GridCreated", grid is not null);

            if (grid is null) return;

            double col0Width = grid.ColumnDefinitions[0].ActualWidth;
            double col1Width = grid.ColumnDefinitions[1].ActualWidth;

            H.Check("FlexInGrid_GridColumnWidths",
                Near(col0Width, 200) && Near(col1Width, 400));

            var a1 = H.FindText("A1");
            var a2 = H.FindText("A2");
            H.Check("FlexInGrid_TopLeftDistribution",
                a1 is not null && a2 is not null &&
                Near(a1.RenderSize.Width, 100, 5) && Near(a2.RenderSize.Width, 100, 5));

            var d1 = H.FindText("D1");
            var d2 = H.FindText("D2");
            H.Check("FlexInGrid_MixedFixedGrow",
                d1 is not null && d2 is not null &&
                Near(d1.RenderSize.Width, 80, 5) && Near(d2.RenderSize.Width, 320, 5));
        }
    }

    // ----------------------------------------------------------------
    // 5. Flex inside Border
    // ----------------------------------------------------------------

    internal class FlexInsideBorder(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                VStack(8,
                    Border(
                        FlexRow(
                            Text("Left").Flex(grow: 1).Background("LightCoral"),
                            Text("Right").Flex(grow: 1).Background("LightBlue")
                        )
                    ).Width(600).Background("LightGray"),

                    Text("Reference line")
                ).Width(600).Height(400)
            );

            await Harness.Render();

            H.Check("FlexInBorder_AllPresent",
                H.FindText("Left") is not null && H.FindText("Right") is not null);

            var flex = H.FindControl<FlexPanel>(_ => true);
            H.Check("FlexInBorder_CrossAxisContentSized",
                flex is not null && flex.ActualHeight < 100);

            var left = H.FindText("Left");
            var right = H.FindText("Right");
            H.Check("FlexInBorder_MainAxisDistributed",
                left is not null && right is not null &&
                Near(left.RenderSize.Width, 300, 5) && Near(right.RenderSize.Width, 300, 5));
        }
    }

    // ----------------------------------------------------------------
    // 6. Flex inside VStack
    // ----------------------------------------------------------------

    internal class FlexInsideVStack(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                VStack(4,
                    Text("Before"),

                    FlexRow(
                        Text("Row1-A").Flex(basis: 0, grow: 1).Background("LightCoral"),
                        Text("Row1-B").Flex(basis: 0, grow: 2).Background("LightBlue"),
                        Text("Row1-C").Flex(basis: 0, grow: 1).Background("LightGreen")
                    ),

                    FlexRow(
                        Text("Row2-A").Flex(basis: 0, grow: 1).Background("Wheat"),
                        Text("Row2-B").Flex(basis: 0, grow: 1).Background("Plum")
                    ),

                    Text("After")
                ).Width(600).Height(400)
            );

            await Harness.Render();

            H.Check("FlexInVStack_AllPresent",
                H.FindText("Before") is not null &&
                H.FindText("Row1-A") is not null &&
                H.FindText("Row2-A") is not null &&
                H.FindText("After") is not null);

            var flexPanels = H.FindAllControls<FlexPanel>(_ => true);
            H.Check("FlexInVStack_TwoFlexPanels", flexPanels.Count >= 2);

            H.Check("FlexInVStack_ContentHeights",
                flexPanels.All(p => p.ActualHeight < 80));

            var afterText = H.FindText("After");
            H.Check("FlexInVStack_AfterVisible", afterText is not null);

            var row1B = H.FindText("Row1-B");
            H.Check("FlexInVStack_GrowDistribution",
                row1B is not null && Near(row1B.RenderSize.Width, 300, 5));
        }
    }

    // ----------------------------------------------------------------
    // 7. Flex inside ScrollViewer (infinite height)
    // ----------------------------------------------------------------

    internal class FlexInsideScrollView(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                ScrollView(
                    FlexColumn(
                        Text("Item 1").Height(60).Background("LightCoral"),
                        Text("Item 2").Height(60).Background("LightBlue"),
                        Text("Item 3").Height(60).Background("LightGreen"),
                        Text("Item 4").Height(60).Background("Wheat"),
                        Text("Item 5").Height(60).Background("Plum")
                    ).Width(400)
                ).Width(400).Height(200)
            );

            await Harness.Render();

            H.Check("FlexInScroll_AllItemsExist",
                H.FindText("Item 1") is not null &&
                H.FindText("Item 5") is not null);

            var flex = H.FindControl<FlexPanel>(_ => true);
            H.Check("FlexInScroll_ContentTallerThanViewport",
                flex is not null && flex.ActualHeight >= 290);

            var item1 = H.FindText("Item 1");
            H.Check("FlexInScroll_ItemHeight",
                item1 is not null && Near(item1.RenderSize.Height, 60, 5));
        }
    }

    // ----------------------------------------------------------------
    // 8. ScrollViewer inside a Flex grow child
    // ----------------------------------------------------------------

    internal class ScrollViewInsideFlex(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                FlexColumn(
                    Text("Header").Height(50).Flex(shrink: 0).Background("LightCoral"),

                    ScrollView(
                        VStack(0,
                            Text("Scroll Item 1").Height(80),
                            Text("Scroll Item 2").Height(80),
                            Text("Scroll Item 3").Height(80),
                            Text("Scroll Item 4").Height(80),
                            Text("Scroll Item 5").Height(80)
                        )
                    ).Flex(grow: 1),

                    Text("Footer").Height(50).Flex(shrink: 0).Background("LightBlue")
                ).Width(400).Height(400)
            );

            await Harness.Render();

            H.Check("ScrollInFlex_HeaderFooterPresent",
                H.FindText("Header") is not null &&
                H.FindText("Footer") is not null);

            var scrollViewer = H.FindControl<WinUI.ScrollViewer>(_ => true);
            H.Check("ScrollInFlex_ScrollViewerExists", scrollViewer is not null);
            H.Check("ScrollInFlex_ScrollViewerHeight",
                scrollViewer is not null && Near(scrollViewer.RenderSize.Height, 300, 10));

            H.Check("ScrollInFlex_ContentScrollable",
                H.FindText("Scroll Item 1") is not null &&
                H.FindText("Scroll Item 5") is not null);
        }
    }

    // ----------------------------------------------------------------
    // 9. FlexColumn grow distribution (vertical main axis)
    // ----------------------------------------------------------------

    internal class FlexColumnGrow(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                FlexColumn(
                    Text("Top").Flex(basis: 0, grow: 1).Background("LightCoral"),
                    Text("Middle").Flex(basis: 0, grow: 2).Background("LightGreen"),
                    Text("Bottom").Flex(basis: 0, grow: 1).Background("LightBlue")
                ).Width(400).Height(400)
            );

            await Harness.Render();

            H.Check("FlexColGrow_AllPresent",
                H.FindText("Top") is not null &&
                H.FindText("Middle") is not null &&
                H.FindText("Bottom") is not null);

            var top = H.FindText("Top");
            var mid = H.FindText("Middle");
            var bot = H.FindText("Bottom");

            H.Check("FlexColGrow_TopHeight",
                top is not null && Near(top.RenderSize.Height, 100, 5));
            H.Check("FlexColGrow_MiddleHeight",
                mid is not null && Near(mid.RenderSize.Height, 200, 5));
            H.Check("FlexColGrow_BottomHeight",
                bot is not null && Near(bot.RenderSize.Height, 100, 5));

            H.Check("FlexColGrow_FullWidth",
                top is not null && Near(top.RenderSize.Width, 400, 5));
        }
    }

    // ----------------------------------------------------------------
    // 10. Mixed grow + fixed-width children
    // ----------------------------------------------------------------

    internal class FlexMixedGrowFixed(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                FlexRow(
                    Text("Fixed1").Width(100).Background("LightCoral"),
                    Text("Grow").Flex(grow: 1).Background("LightGreen"),
                    Text("Fixed2").Width(100).Background("LightBlue")
                ).Width(600).Height(60)
            );

            await Harness.Render();

            var fixed1 = H.FindText("Fixed1");
            var grow = H.FindText("Grow");
            var fixed2 = H.FindText("Fixed2");

            H.Check("FlexMixed_AllPresent",
                fixed1 is not null && grow is not null && fixed2 is not null);

            H.Check("FlexMixed_Fixed1Width",
                fixed1 is not null && Near(fixed1.RenderSize.Width, 100, 3));
            H.Check("FlexMixed_Fixed2Width",
                fixed2 is not null && Near(fixed2.RenderSize.Width, 100, 3));

            H.Check("FlexMixed_GrowWidth",
                grow is not null && Near(grow.RenderSize.Width, 400, 5));
        }
    }

    // ----------------------------------------------------------------
    // 11. Flex with gaps
    // ----------------------------------------------------------------

    internal class FlexWithGaps(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                new FlexElement([
                    Text("A").Flex(basis: 0, grow: 1).Background("LightCoral"),
                    Text("B").Flex(basis: 0, grow: 1).Background("LightGreen"),
                    Text("C").Flex(basis: 0, grow: 1).Background("LightBlue")
                ])
                {
                    Direction = FlexDirection.Row,
                    ColumnGap = 20,
                }.Width(600).Height(60)
            );

            await Harness.Render();

            var a = H.FindText("A");
            var b = H.FindText("B");
            var c = H.FindText("C");

            H.Check("FlexGaps_AllPresent",
                a is not null && b is not null && c is not null);

            double expected = (600 - 2 * 20) / 3.0;
            H.Check("FlexGaps_EqualWidthsWithGap",
                a is not null && b is not null && c is not null &&
                Near(a.RenderSize.Width, expected, 3) &&
                Near(b.RenderSize.Width, expected, 3) &&
                Near(c.RenderSize.Width, expected, 3));

            if (a is not null && b is not null && c is not null)
            {
                double total = a.RenderSize.Width + b.RenderSize.Width + c.RenderSize.Width + 2 * 20;
                Console.WriteLine($"# Gap layout total: {total:F1} (expected 600)");
                H.Check("FlexGaps_TotalWidthCorrect", Near(total, 600, 3));
            }
        }
    }

    // ----------------------------------------------------------------
    // 12. Flex with FlexPadding
    // ----------------------------------------------------------------

    internal class FlexWithPadding(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                FlexRow(
                    Text("Padded-A").Flex(basis: 0, grow: 1).Background("LightCoral"),
                    Text("Padded-B").Flex(basis: 0, grow: 1).Background("LightBlue")
                ).FlexPadding(20).Width(600).Height(100)
            );

            await Harness.Render();

            var a = H.FindText("Padded-A");
            var b = H.FindText("Padded-B");

            H.Check("FlexPadding_AllPresent",
                a is not null && b is not null);

            H.Check("FlexPadding_ChildWidths",
                a is not null && b is not null &&
                Near(a.RenderSize.Width, 280, 3) && Near(b.RenderSize.Width, 280, 3));

            H.Check("FlexPadding_ChildHeights",
                a is not null && Near(a.RenderSize.Height, 60, 5));
        }
    }

    // ----------------------------------------------------------------
    // 13. Flex cross-axis content sizing regression
    // ----------------------------------------------------------------

    internal class FlexCrossAxisTextHeight(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var longText = "This is a moderately long piece of text that will wrap " +
                "within its allocated width. The number of lines depends on the " +
                "width constraint, so the height should be consistent.";

            var host = H.CreateHost();
            host.Mount(ctx =>
                VStack(8,
                    FlexRow(
                        Text("Sidebar").Width(100).Background("LightCoral"),
                        Text(longText).Flex(grow: 1).Background("LightBlue")
                    ).Width(600).Background("LightGray"),

                    Text(longText).Width(500).Background("LightGreen"),

                    Text("Below-all-visible")
                ).Width(600).Height(600)
            );

            await Harness.Render();

            H.Check("FlexCrossText_AllPresent",
                H.FindText("Sidebar") is not null &&
                H.FindText("Below-all-visible") is not null);

            var flex = H.FindControl<FlexPanel>(_ => true);
            H.Check("FlexCrossText_ReasonableHeight",
                flex is not null && flex.ActualHeight < 200);

            var flexTexts = H.FindAllControls<TextBlock>(tb => tb.Text == longText);
            if (flexTexts.Count >= 2)
            {
                double flexTextHeight = flexTexts[0].ActualHeight;
                double refTextHeight = flexTexts[1].ActualHeight;
                Console.WriteLine($"# Flex text height: {flexTextHeight:F1}, Ref text height: {refTextHeight:F1}");
                H.Check("FlexCrossText_HeightsMatch",
                    Near(flexTextHeight, refTextHeight, 20));
            }
        }
    }

    // ----------------------------------------------------------------
    // 14. Grid inside Flex
    // ----------------------------------------------------------------

    internal class GridInsideFlex(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                FlexColumn(
                    Text("Header").Height(50).Background("LightCoral"),

                    Grid(["*", "2*"], ["*"],
                        Text("GridLeft").Grid(row: 0, column: 0).Background("LightGreen"),
                        Text("GridRight").Grid(row: 0, column: 1).Background("LightBlue")
                    ).Flex(grow: 1),

                    Text("Footer").Height(50).Background("Wheat")
                ).Width(600).Height(400)
            );

            await Harness.Render();

            H.Check("GridInFlex_AllPresent",
                H.FindText("Header") is not null &&
                H.FindText("GridLeft") is not null &&
                H.FindText("GridRight") is not null &&
                H.FindText("Footer") is not null);

            var grid = H.FindControl<WinUI.Grid>(g => g.ColumnDefinitions.Count == 2);
            H.Check("GridInFlex_GridHeight",
                grid is not null && Near(grid.ActualHeight, 300, 10));

            H.Check("GridInFlex_StarColumns",
                grid is not null &&
                Near(grid.ColumnDefinitions[0].ActualWidth, 200, 3) &&
                Near(grid.ColumnDefinitions[1].ActualWidth, 400, 3));
        }
    }

    // ----------------------------------------------------------------
    // 15. Flex with child margins
    // ----------------------------------------------------------------

    internal class FlexWithChildMargins(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                FlexRow(
                    Text("M1").Flex(basis: 0, grow: 1).Margin(10).Background("LightCoral"),
                    Text("M2").Flex(basis: 0, grow: 1).Margin(10).Background("LightBlue"),
                    Text("M3").Flex(basis: 0, grow: 1).Margin(10).Background("LightGreen")
                ).Width(600).Height(80)
            );

            await Harness.Render();

            var m1 = H.FindText("M1");
            var m2 = H.FindText("M2");
            var m3 = H.FindText("M3");

            H.Check("FlexMargins_AllPresent",
                m1 is not null && m2 is not null && m3 is not null);

            double expected = (600 - 6 * 10) / 3.0;
            H.Check("FlexMargins_ContentWidths",
                m1 is not null && m2 is not null && m3 is not null &&
                Near(m1.RenderSize.Width, expected, 5) &&
                Near(m2.RenderSize.Width, expected, 5) &&
                Near(m3.RenderSize.Width, expected, 5));
        }
    }

    // ----------------------------------------------------------------
    // 16. Flex with JustifyContent variations
    // ----------------------------------------------------------------

    internal class FlexJustifySpaceBetween(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                new FlexElement([
                    Text("J1").Width(100).Background("LightCoral"),
                    Text("J2").Width(100).Background("LightGreen"),
                    Text("J3").Width(100).Background("LightBlue"),
                ])
                {
                    Direction = FlexDirection.Row,
                    JustifyContent = FlexJustify.SpaceBetween,
                }.Width(600).Height(60)
            );

            await Harness.Render();

            var j1 = H.FindText("J1");
            var j2 = H.FindText("J2");
            var j3 = H.FindText("J3");

            H.Check("FlexJustify_AllPresent",
                j1 is not null && j2 is not null && j3 is not null);

            if (j1 is not null && j2 is not null && j3 is not null)
            {
                var flex = H.FindControl<FlexPanel>(_ => true);
                if (flex is not null)
                {
                    H.Check("FlexJustify_ChildWidthsFixed",
                        Near(j1.RenderSize.Width, 100, 3) &&
                        Near(j2.RenderSize.Width, 100, 3) &&
                        Near(j3.RenderSize.Width, 100, 3));
                }
            }
        }
    }

    // ----------------------------------------------------------------
    // 17. Layout cycle prevention: FlexPanel inside Grid star column
    // ----------------------------------------------------------------

    internal class FlexLayoutCycleInGridStar(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                Grid(["200", "*"], ["*"],
                    Text("Sidebar").Grid(row: 0, column: 0).Background("LightCoral"),

                    FlexRow(
                        Text("Grow-A").Flex(basis: 0, grow: 1).Background("LightGreen"),
                        Text("Grow-B").Flex(basis: 0, grow: 2).Background("LightBlue")
                    ).Grid(row: 0, column: 1)
                ).Width(600).Height(300)
            );

            await Harness.Render();

            H.Check("FlexCycle_GridStar_NoException", true);

            var growA = H.FindText("Grow-A");
            var growB = H.FindText("Grow-B");
            H.Check("FlexCycle_GridStar_ChildrenPresent",
                growA is not null && growB is not null);

            H.Check("FlexCycle_GridStar_GrowDistribution",
                growA is not null && growB is not null &&
                Near(growA.RenderSize.Width, 133, 5) && Near(growB.RenderSize.Width, 267, 5));
        }
    }

    // ----------------------------------------------------------------
    // 18. Layout cycle prevention: deeply nested FlexPanels
    // ----------------------------------------------------------------

    internal class FlexLayoutCycleNestedDeep(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                FlexColumn(
                    Text("L1-Top").Height(40),

                    FlexRow(
                        Text("L2-Left").Flex(basis: 0, grow: 1),

                        FlexColumn(
                            Text("L3-Header").Height(30),

                            FlexRow(
                                Text("L4-A").Flex(basis: 0, grow: 1).Background("LightCoral"),
                                Text("L4-B").Flex(basis: 0, grow: 1).Background("LightBlue"),
                                Text("L4-C").Flex(basis: 0, grow: 1).Background("LightGreen")
                            ).Flex(grow: 1),

                            Text("L3-Footer").Height(30)
                        ).Flex(basis: 0, grow: 2)
                    ).Flex(grow: 1),

                    Text("L1-Bottom").Height(40)
                ).Width(600).Height(400)
            );

            await Harness.Render();

            H.Check("FlexCycle_Nested4_NoException", true);

            H.Check("FlexCycle_Nested4_AllPresent",
                H.FindText("L1-Top") is not null &&
                H.FindText("L2-Left") is not null &&
                H.FindText("L3-Header") is not null &&
                H.FindText("L4-A") is not null &&
                H.FindText("L4-B") is not null &&
                H.FindText("L4-C") is not null &&
                H.FindText("L3-Footer") is not null &&
                H.FindText("L1-Bottom") is not null);

            var l4a = H.FindText("L4-A");
            var l4b = H.FindText("L4-B");
            var l4c = H.FindText("L4-C");
            H.Check("FlexCycle_Nested4_L4Distribution",
                l4a is not null && l4b is not null && l4c is not null &&
                Near(l4a.ActualWidth, l4b.ActualWidth, 3) &&
                Near(l4b.ActualWidth, l4c.ActualWidth, 3));
        }
    }

    // ----------------------------------------------------------------
    // 19. Layout cycle prevention: FlexPanel with auto-sizing text
    // ----------------------------------------------------------------

    internal class FlexLayoutCycleAutoText(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var longText = "This text is long enough to wrap at narrow widths but " +
                "should display in fewer lines at wider widths. The layout cycle " +
                "bug would cause a crash when text is re-measured during arrange.";

            var host = H.CreateHost();
            host.Mount(ctx =>
                Grid(["*", "2*"], ["Auto", "*"],
                    FlexRow(
                        Text("Label").Width(80).Background("LightCoral"),
                        Text(longText).Flex(grow: 1).Background("LightBlue")
                    ).Grid(row: 0, column: 0, columnSpan: 2),

                    FlexRow(
                        Text("Body-A").Flex(basis: 0, grow: 1).Background("LightGreen"),
                        Text("Body-B").Flex(basis: 0, grow: 1).Background("Wheat")
                    ).Grid(row: 1, column: 0, columnSpan: 2)
                ).Width(600).Height(400)
            );

            await Harness.Render();

            H.Check("FlexCycle_AutoText_NoException", true);

            H.Check("FlexCycle_AutoText_ChildrenPresent",
                H.FindText("Label") is not null &&
                H.FindText("Body-A") is not null &&
                H.FindText("Body-B") is not null);

            var flexPanels = H.FindAllControls<FlexPanel>(_ => true);
            var topRow = flexPanels.FirstOrDefault(p =>
                p.Direction == FlexDirection.Row && p.ActualHeight < 200);
            H.Check("FlexCycle_AutoText_ReasonableHeight",
                topRow is not null && topRow.ActualHeight > 10);
        }
    }

    // ----------------------------------------------------------------
    // 20. Layout cycle prevention: size mismatch between Measure and Arrange
    // ----------------------------------------------------------------

    internal class FlexLayoutCycleSizeMismatch(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                Grid(["*"], ["60", "*", "60"],
                    Text("Header").Grid(row: 0, column: 0).Background("LightCoral"),

                    ScrollView(
                        FlexColumn(
                            Text("Item-1").Height(50).Background("LightBlue"),
                            Text("Item-2").Height(50).Background("LightGreen"),
                            Text("Item-3").Height(50).Background("Wheat"),
                            Text("Item-4").Height(50).Background("Plum"),
                            Text("Item-5").Height(50).Background("LightCoral"),
                            Text("Item-6").Height(50).Background("PeachPuff")
                        ).Width(400)
                    ).Grid(row: 1, column: 0),

                    Text("Footer").Grid(row: 2, column: 0).Background("LightGray")
                ).Width(400).Height(300)
            );

            await Harness.Render();

            H.Check("FlexCycle_SizeMismatch_NoException", true);

            H.Check("FlexCycle_SizeMismatch_AllPresent",
                H.FindText("Header") is not null &&
                H.FindText("Item-1") is not null &&
                H.FindText("Item-6") is not null &&
                H.FindText("Footer") is not null);

            var flex = H.FindControl<FlexPanel>(_ => true);
            H.Check("FlexCycle_SizeMismatch_ContentSized",
                flex is not null && Near(flex.ActualHeight, 300, 5));
        }
    }
}

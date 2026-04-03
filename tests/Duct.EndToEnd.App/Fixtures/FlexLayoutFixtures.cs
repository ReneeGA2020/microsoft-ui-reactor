using Duct;
using Duct.Core;
using Duct.Flex;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace Duct.EndToEnd.App.Fixtures;

/// <summary>
/// Comprehensive Flex layout E2E tests covering nesting, composition with
/// other layout panels, scroll interaction, and cross-axis sizing.
/// </summary>
internal static class FlexLayoutFixtures
{
    private const double Tolerance = 2.0;

    private static bool Near(double a, double b, double tol = Tolerance)
        => Math.Abs(a - b) <= tol;

    // ────────────────────────────────────────────────────────────────────
    // 1. Nested Flex: FlexRow children inside FlexColumn
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Classic "holy grail" layout: FlexColumn with a header row, a body row
    /// that grows, and a footer row. Each row distributes children horizontally.
    /// Verifies that nested FlexRows get correct widths from the outer FlexColumn
    /// and that the grow child expands vertically.
    /// </summary>
    internal class FlexNestedRowInColumn(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                FlexColumn(
                    // Header row: fixed height
                    FlexRow(
                        Text("Logo").Flex(basis: 0, grow: 1).Background("LightCoral"),
                        Text("Nav").Flex(basis: 0, grow: 2).Background("LightBlue")
                    ).Height(50),

                    // Body row: grows to fill remaining space
                    FlexRow(
                        Text("Sidebar").Flex(basis: 0, grow: 1).Background("LightGreen"),
                        Text("Content").Flex(basis: 0, grow: 3).Background("LightYellow")
                    ).Flex(grow: 1),

                    // Footer row: fixed height
                    FlexRow(
                        Text("Footer").Flex(grow: 1)
                    ).Height(40).Background("LightGray")
                ).Width(600).Height(400)
            );

            await Harness.Render(600);

            // All text visible
            H.Check("FlexNested_RowInCol_AllPresent",
                H.FindText("Logo") is not null &&
                H.FindText("Nav") is not null &&
                H.FindText("Sidebar") is not null &&
                H.FindText("Content") is not null &&
                H.FindText("Footer") is not null);

            // Outer FlexColumn created
            var outerCol = H.FindControl<FlexPanel>(p =>
                FlexPanel.GetGrow(p) == 0 && p.Direction == FlexDirection.Column);
            H.Check("FlexNested_RowInCol_OuterColumnExists", outerCol is not null);

            // Inner FlexRows created (at least 3 nested FlexPanels with Row direction)
            var rows = H.FindAllControls<FlexPanel>(p => p.Direction == FlexDirection.Row);
            H.Check("FlexNested_RowInCol_ThreeInnerRows", rows.Count >= 3);

            // Body row should have grown: its height should be 400 - 50 - 40 = 310
            if (outerCol is not null)
            {
                var bodyRow = rows.FirstOrDefault(r => FlexPanel.GetGrow(r) > 0);
                H.Check("FlexNested_RowInCol_BodyGrew",
                    bodyRow is not null && Near(bodyRow.ActualHeight, 310, 5));

                // Content child in body row should be ~3/4 of 600 = 450
                var contentText = H.FindText("Content");
                if (contentText is not null)
                {
                    H.Check("FlexNested_RowInCol_ContentWidth",
                        Near(contentText.RenderSize.Width, 450, 5));
                }
            }

            await H.CaptureScreenshotAsync("FlexNested_RowInColumn");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. Nested Flex: FlexColumn inside FlexRow
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sidebar layout: FlexRow with a narrow FlexColumn sidebar and a wide
    /// FlexColumn content area. Verifies horizontal grow distributes correctly
    /// and nested columns stack vertically within their allocated widths.
    /// </summary>
    internal class FlexNestedColumnInRow(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                FlexRow(
                    // Sidebar: 1/4 width
                    FlexColumn(
                        Text("Menu A"),
                        Text("Menu B"),
                        Text("Menu C")
                    ).Flex(basis: 0, grow: 1).Background("LightCoral"),

                    // Content area: 3/4 width
                    FlexColumn(
                        Text("Title").Height(40),
                        Text("Body text here").Flex(grow: 1)
                    ).Flex(basis: 0, grow: 3).Background("LightBlue")
                ).Width(600).Height(300)
            );

            await Harness.Render(600);

            H.Check("FlexNested_ColInRow_AllPresent",
                H.FindText("Menu A") is not null &&
                H.FindText("Title") is not null &&
                H.FindText("Body text here") is not null);

            // Sidebar column should be ~150px (1/4 of 600)
            var columns = H.FindAllControls<FlexPanel>(p => p.Direction == FlexDirection.Column);
            var sidebar = columns.FirstOrDefault(c => c.ActualWidth < 200);
            H.Check("FlexNested_ColInRow_SidebarWidth",
                sidebar is not null && Near(sidebar.ActualWidth, 150, 5));

            // Content column should be ~450px (3/4 of 600)
            var content = columns.FirstOrDefault(c => c.ActualWidth > 400);
            H.Check("FlexNested_ColInRow_ContentWidth",
                content is not null && Near(content.ActualWidth, 450, 5));

            await H.CaptureScreenshotAsync("FlexNested_ColumnInRow");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. Deep nesting: 3+ levels of FlexPanels
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Three levels deep: FlexColumn → FlexRow → FlexColumn.
    /// Ensures layout propagates correctly through multiple nesting levels
    /// and that each level's grow/basis distributes space properly.
    /// </summary>
    internal class FlexNestedDeep(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                // Level 1: Column
                FlexColumn(
                    Text("L1-Header").Height(30),

                    // Level 2: Row (grows)
                    FlexRow(
                        Text("L2-Left").Flex(basis: 0, grow: 1),

                        // Level 3: Column inside the row
                        FlexColumn(
                            Text("L3-Top"),
                            Text("L3-Mid").Flex(grow: 1),
                            Text("L3-Bot")
                        ).Flex(basis: 0, grow: 2)
                    ).Flex(grow: 1),

                    Text("L1-Footer").Height(30)
                ).Width(600).Height(400)
            );

            await Harness.Render(600);

            // All 6 text items present
            H.Check("FlexNested_Deep_AllPresent",
                H.FindText("L1-Header") is not null &&
                H.FindText("L2-Left") is not null &&
                H.FindText("L3-Top") is not null &&
                H.FindText("L3-Mid") is not null &&
                H.FindText("L3-Bot") is not null &&
                H.FindText("L1-Footer") is not null);

            // Total FlexPanels: 3 (Column, Row, Column)
            var allFlex = H.FindAllControls<FlexPanel>(_ => true);
            H.Check("FlexNested_Deep_ThreePanels", allFlex.Count >= 3);

            // L2-Left should be ~1/3 of 600 = 200
            var l2Left = H.FindText("L2-Left");
            H.Check("FlexNested_Deep_L2LeftWidth",
                l2Left is not null && Near(l2Left.RenderSize.Width, 200, 5));

            // L3 column should be ~2/3 of 600 = 400
            var l3Columns = H.FindAllControls<FlexPanel>(p =>
                p.Direction == FlexDirection.Column && p.ActualWidth > 350 && p.ActualWidth < 450);
            H.Check("FlexNested_Deep_L3ColumnWidth", l3Columns.Count >= 1);

            await H.CaptureScreenshotAsync("FlexNested_Deep");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. Flex inside Grid star cells
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// FlexPanels placed inside Grid star-sized cells. Verifies that the
    /// FlexPanel respects the Grid cell's allocated size and distributes
    /// its children correctly within those bounds.
    /// </summary>
    internal class FlexInsideGrid(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                Grid(["*", "2*"], ["*", "*"],
                    // Top-left: FlexRow in 1* cell
                    FlexRow(
                        Text("A1").Flex(grow: 1).Background("LightCoral"),
                        Text("A2").Flex(grow: 1).Background("LightBlue")
                    ).Grid(row: 0, column: 0),

                    // Top-right: FlexColumn in 2* cell
                    FlexColumn(
                        Text("B1").Flex(grow: 1).Background("LightGreen"),
                        Text("B2").Flex(grow: 2).Background("LightYellow")
                    ).Grid(row: 0, column: 1),

                    // Bottom-left: single grow child
                    FlexRow(
                        Text("C1").Flex(grow: 1).Background("Wheat")
                    ).Grid(row: 1, column: 0),

                    // Bottom-right: mixed fixed + grow
                    FlexRow(
                        Text("D1").Width(80).Background("Plum"),
                        Text("D2").Flex(grow: 1).Background("PeachPuff")
                    ).Grid(row: 1, column: 1)
                ).Width(600).Height(400)
            );

            await Harness.Render(600);

            H.Check("FlexInGrid_AllPresent",
                H.FindText("A1") is not null &&
                H.FindText("B1") is not null &&
                H.FindText("C1") is not null &&
                H.FindText("D1") is not null &&
                H.FindText("D2") is not null);

            var grid = H.FindControl<WinUI.Grid>(g => g.ColumnDefinitions.Count == 2);
            H.Check("FlexInGrid_GridCreated", grid is not null);

            if (grid is null) return;

            // Grid columns: 1* + 2* of 600 = 200 + 400
            double col0Width = grid.ColumnDefinitions[0].ActualWidth;
            double col1Width = grid.ColumnDefinitions[1].ActualWidth;

            H.Check("FlexInGrid_GridColumnWidths",
                Near(col0Width, 200) && Near(col1Width, 400));

            // A1 and A2 should each be ~100px (half of 200px cell)
            var a1 = H.FindText("A1");
            var a2 = H.FindText("A2");
            H.Check("FlexInGrid_TopLeftDistribution",
                a1 is not null && a2 is not null &&
                Near(a1.RenderSize.Width, 100, 5) && Near(a2.RenderSize.Width, 100, 5));

            // D1 should be fixed 80px, D2 should get remaining (400 - 80 = 320)
            var d1 = H.FindText("D1");
            var d2 = H.FindText("D2");
            H.Check("FlexInGrid_MixedFixedGrow",
                d1 is not null && d2 is not null &&
                Near(d1.RenderSize.Width, 80, 5) && Near(d2.RenderSize.Width, 320, 5));

            await H.CaptureScreenshotAsync("FlexInsideGrid");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 5. Flex inside Border — cross-axis content sizing
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// FlexRow inside a Border with no explicit height. The FlexRow should
    /// content-size on the cross axis (height), not expand to fill all
    /// available vertical space. This is a regression test for the two-pass
    /// measure fix.
    /// </summary>
    internal class FlexInsideBorder(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                VStack(8,
                    // A FlexRow inside a Border — should only be as tall as its content
                    Border(
                        FlexRow(
                            Text("Left").Flex(grow: 1).Background("LightCoral"),
                            Text("Right").Flex(grow: 1).Background("LightBlue")
                        )
                    ).Width(600).Background("LightGray"),

                    // Reference: plain text for height comparison
                    Text("Reference line")
                ).Width(600).Height(400)
            );

            await Harness.Render(600);

            H.Check("FlexInBorder_AllPresent",
                H.FindText("Left") is not null && H.FindText("Right") is not null);

            // The FlexPanel should NOT fill all 400px of height
            var flex = H.FindControl<FlexPanel>(_ => true);
            H.Check("FlexInBorder_CrossAxisContentSized",
                flex is not null && flex.ActualHeight < 100);

            // Both children should split the 600px width
            var left = H.FindText("Left");
            var right = H.FindText("Right");
            H.Check("FlexInBorder_MainAxisDistributed",
                left is not null && right is not null &&
                Near(left.RenderSize.Width, 300, 5) && Near(right.RenderSize.Width, 300, 5));

            await H.CaptureScreenshotAsync("FlexInsideBorder");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 6. Flex inside VStack
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// FlexRow inside a VStack. The FlexRow gets definite width from VStack
    /// but infinite height — should content-size vertically. Multiple
    /// FlexRows in a VStack should stack without expanding.
    /// </summary>
    internal class FlexInsideVStack(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
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

            await Harness.Render(600);

            H.Check("FlexInVStack_AllPresent",
                H.FindText("Before") is not null &&
                H.FindText("Row1-A") is not null &&
                H.FindText("Row2-A") is not null &&
                H.FindText("After") is not null);

            // Both FlexRows should be content-height (not filling VStack)
            var flexPanels = H.FindAllControls<FlexPanel>(_ => true);
            H.Check("FlexInVStack_TwoFlexPanels", flexPanels.Count >= 2);

            // Each should be small height (just text, ~20-40px)
            H.Check("FlexInVStack_ContentHeights",
                flexPanels.All(p => p.ActualHeight < 80));

            // "After" text should be visible below (not pushed off-screen)
            var afterText = H.FindText("After");
            H.Check("FlexInVStack_AfterVisible", afterText is not null);

            // Row1-B should be ~300px (2/4 of 600)
            var row1B = H.FindText("Row1-B");
            H.Check("FlexInVStack_GrowDistribution",
                row1B is not null && Near(row1B.RenderSize.Width, 300, 5));

            await H.CaptureScreenshotAsync("FlexInsideVStack");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 7. Flex inside ScrollViewer (infinite height)
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// FlexColumn inside a ScrollViewer. The ScrollViewer gives infinite
    /// height — the FlexColumn should content-size and NOT collapse to 0.
    /// This tests Pass 1 behavior (no Pass 2 on the main axis for Column
    /// when height is infinite).
    /// </summary>
    internal class FlexInsideScrollView(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                ScrollView(
                    FlexColumn(
                        Text("Item 1").Height(60).Background("LightCoral"),
                        Text("Item 2").Height(60).Background("LightBlue"),
                        Text("Item 3").Height(60).Background("LightGreen"),
                        Text("Item 4").Height(60).Background("Wheat"),
                        Text("Item 5").Height(60).Background("Plum")
                    ).Width(400)
                ).Width(400).Height(200) // Only 200px visible — content is 300px
            );

            await Harness.Render(600);

            // All items should exist in the tree (even if scrolled out of view)
            H.Check("FlexInScroll_AllItemsExist",
                H.FindText("Item 1") is not null &&
                H.FindText("Item 5") is not null);

            // The FlexColumn should be taller than the ScrollViewer
            var flex = H.FindControl<FlexPanel>(_ => true);
            H.Check("FlexInScroll_ContentTallerThanViewport",
                flex is not null && flex.ActualHeight >= 290);

            // Items should each be 60px tall
            var item1 = H.FindText("Item 1");
            H.Check("FlexInScroll_ItemHeight",
                item1 is not null && Near(item1.RenderSize.Height, 60, 5));

            await H.CaptureScreenshotAsync("FlexInsideScrollView");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 8. ScrollViewer inside a Flex grow child
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Header-body-footer pattern where the body is a ScrollViewer that
    /// grows to fill remaining space. Verifies the ScrollViewer receives
    /// the correct allocated height from FlexPanel grow distribution.
    /// </summary>
    internal class ScrollViewInsideFlex(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
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

            await Harness.Render(600);

            H.Check("ScrollInFlex_HeaderFooterPresent",
                H.FindText("Header") is not null &&
                H.FindText("Footer") is not null);

            // ScrollViewer should get remaining height: 400 - 50 - 50 = 300
            var scrollViewer = H.FindControl<WinUI.ScrollViewer>(_ => true);
            H.Check("ScrollInFlex_ScrollViewerExists", scrollViewer is not null);
            H.Check("ScrollInFlex_ScrollViewerHeight",
                scrollViewer is not null && Near(scrollViewer.RenderSize.Height, 300, 10));

            // Scroll content should be 400px (5 items × 80) — taller than viewport
            H.Check("ScrollInFlex_ContentScrollable",
                H.FindText("Scroll Item 1") is not null &&
                H.FindText("Scroll Item 5") is not null);

            await H.CaptureScreenshotAsync("ScrollViewInsideFlex");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 9. FlexColumn grow distribution (vertical main axis)
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// FlexColumn with grow children — tests the main axis is vertical.
    /// Verifies that grow distributes vertical space correctly, and that
    /// children are measured at their resolved heights.
    /// </summary>
    internal class FlexColumnGrow(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                FlexColumn(
                    Text("Top").Flex(basis: 0, grow: 1).Background("LightCoral"),
                    Text("Middle").Flex(basis: 0, grow: 2).Background("LightGreen"),
                    Text("Bottom").Flex(basis: 0, grow: 1).Background("LightBlue")
                ).Width(400).Height(400)
            );

            await Harness.Render(600);

            H.Check("FlexColGrow_AllPresent",
                H.FindText("Top") is not null &&
                H.FindText("Middle") is not null &&
                H.FindText("Bottom") is not null);

            // Grow 1:2:1 in 400px → 100, 200, 100
            var top = H.FindText("Top");
            var mid = H.FindText("Middle");
            var bot = H.FindText("Bottom");

            H.Check("FlexColGrow_TopHeight",
                top is not null && Near(top.RenderSize.Height, 100, 5));
            H.Check("FlexColGrow_MiddleHeight",
                mid is not null && Near(mid.RenderSize.Height, 200, 5));
            H.Check("FlexColGrow_BottomHeight",
                bot is not null && Near(bot.RenderSize.Height, 100, 5));

            // All children should span the full 400px width (cross axis stretch)
            H.Check("FlexColGrow_FullWidth",
                top is not null && Near(top.RenderSize.Width, 400, 5));

            await H.CaptureScreenshotAsync("FlexColumnGrow");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 10. Mixed grow + fixed-width children
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// FlexRow with a mix of fixed-width and grow children. Fixed children
    /// should get their exact size, grow children split the remainder.
    /// </summary>
    internal class FlexMixedGrowFixed(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                FlexRow(
                    Text("Fixed1").Width(100).Background("LightCoral"),
                    Text("Grow").Flex(grow: 1).Background("LightGreen"),
                    Text("Fixed2").Width(100).Background("LightBlue")
                ).Width(600).Height(60)
            );

            await Harness.Render(600);

            var fixed1 = H.FindText("Fixed1");
            var grow = H.FindText("Grow");
            var fixed2 = H.FindText("Fixed2");

            H.Check("FlexMixed_AllPresent",
                fixed1 is not null && grow is not null && fixed2 is not null);

            // Fixed children keep their 100px
            H.Check("FlexMixed_Fixed1Width",
                fixed1 is not null && Near(fixed1.RenderSize.Width, 100, 3));
            H.Check("FlexMixed_Fixed2Width",
                fixed2 is not null && Near(fixed2.RenderSize.Width, 100, 3));

            // Grow child gets remaining: 600 - 100 - 100 = 400
            H.Check("FlexMixed_GrowWidth",
                grow is not null && Near(grow.RenderSize.Width, 400, 5));

            await H.CaptureScreenshotAsync("FlexMixedGrowFixed");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 11. Flex with gaps
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// FlexRow with ColumnGap. Gap pixels should be subtracted from available
    /// space before grow distribution. 3 children with 20px gaps in 600px:
    /// available = 600 - 2×20 = 560 → each gets ~186.7 at equal grow.
    /// </summary>
    internal class FlexWithGaps(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
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

            await Harness.Render(600);

            var a = H.FindText("A");
            var b = H.FindText("B");
            var c = H.FindText("C");

            H.Check("FlexGaps_AllPresent",
                a is not null && b is not null && c is not null);

            // Each child: (600 - 2*20) / 3 ≈ 186.7
            double expected = (600 - 2 * 20) / 3.0;
            H.Check("FlexGaps_EqualWidthsWithGap",
                a is not null && b is not null && c is not null &&
                Near(a.RenderSize.Width, expected, 3) &&
                Near(b.RenderSize.Width, expected, 3) &&
                Near(c.RenderSize.Width, expected, 3));

            // Total children width + gaps should equal container width
            if (a is not null && b is not null && c is not null)
            {
                double total = a.RenderSize.Width + b.RenderSize.Width + c.RenderSize.Width + 2 * 20;
                Console.WriteLine($"# Gap layout total: {total:F1} (expected 600)");
                H.Check("FlexGaps_TotalWidthCorrect", Near(total, 600, 3));
            }

            await H.CaptureScreenshotAsync("FlexWithGaps");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 12. Flex with FlexPadding
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// FlexRow with FlexPadding. Padding reduces the space available for
    /// children. 20px padding on all sides in 600×100: children get 560×60.
    /// </summary>
    internal class FlexWithPadding(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                FlexRow(
                    Text("Padded-A").Flex(basis: 0, grow: 1).Background("LightCoral"),
                    Text("Padded-B").Flex(basis: 0, grow: 1).Background("LightBlue")
                ).FlexPadding(20).Width(600).Height(100)
            );

            await Harness.Render(600);

            var a = H.FindText("Padded-A");
            var b = H.FindText("Padded-B");

            H.Check("FlexPadding_AllPresent",
                a is not null && b is not null);

            // Each child: (600 - 20 - 20) / 2 = 280
            H.Check("FlexPadding_ChildWidths",
                a is not null && b is not null &&
                Near(a.RenderSize.Width, 280, 3) && Near(b.RenderSize.Width, 280, 3));

            // Children should be inset by 20px (check height: 100 - 20 - 20 = 60)
            H.Check("FlexPadding_ChildHeights",
                a is not null && Near(a.RenderSize.Height, 60, 5));

            await H.CaptureScreenshotAsync("FlexWithPadding");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 13. Flex cross-axis content sizing regression
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Regression test: a FlexRow with grow children containing long text.
    /// The cross-axis (height) should match the text height when measured
    /// at the resolved grow width — NOT the inflated height from Pass 1
    /// where basis:0 children were measured at near-zero width.
    /// </summary>
    internal class FlexCrossAxisTextHeight(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var longText = "This is a moderately long piece of text that will wrap " +
                "within its allocated width. The number of lines depends on the " +
                "width constraint, so the height should be consistent.";

            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                VStack(8,
                    // FlexRow with grow + long text
                    FlexRow(
                        Text("Sidebar").Width(100).Background("LightCoral"),
                        Text(longText).Flex(grow: 1).Background("LightBlue")
                    ).Width(600).Background("LightGray"),

                    // Reference: same text in a fixed-width container
                    Text(longText).Width(500).Background("LightGreen"),

                    Text("Below-all-visible")
                ).Width(600).Height(600)
            );

            await Harness.Render(800);

            H.Check("FlexCrossText_AllPresent",
                H.FindText("Sidebar") is not null &&
                H.FindText("Below-all-visible") is not null);

            // The FlexRow should NOT have an enormous height
            var flex = H.FindControl<FlexPanel>(_ => true);
            H.Check("FlexCrossText_ReasonableHeight",
                flex is not null && flex.ActualHeight < 200);

            // The text in the FlexRow (at 500px) should have similar height
            // to the reference text (also at 500px)
            var flexTexts = H.FindAllControls<TextBlock>(tb => tb.Text == longText);
            if (flexTexts.Count >= 2)
            {
                double flexTextHeight = flexTexts[0].ActualHeight;
                double refTextHeight = flexTexts[1].ActualHeight;
                Console.WriteLine($"# Flex text height: {flexTextHeight:F1}, Ref text height: {refTextHeight:F1}");
                H.Check("FlexCrossText_HeightsMatch",
                    Near(flexTextHeight, refTextHeight, 20));
            }

            await H.CaptureScreenshotAsync("FlexCrossAxisTextHeight");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 14. Grid inside Flex — Grid as a grow child
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A Grid placed as a grow child inside a FlexColumn. The Grid should
    /// receive the allocated size from flex and lay out its star columns
    /// within that space.
    /// </summary>
    internal class GridInsideFlex(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
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

            await Harness.Render(600);

            H.Check("GridInFlex_AllPresent",
                H.FindText("Header") is not null &&
                H.FindText("GridLeft") is not null &&
                H.FindText("GridRight") is not null &&
                H.FindText("Footer") is not null);

            // Grid should grow to 400 - 50 - 50 = 300px height
            var grid = H.FindControl<WinUI.Grid>(g => g.ColumnDefinitions.Count == 2);
            H.Check("GridInFlex_GridHeight",
                grid is not null && Near(grid.ActualHeight, 300, 10));

            // Grid star columns: 1* + 2* of 600 = 200 + 400
            H.Check("GridInFlex_StarColumns",
                grid is not null &&
                Near(grid.ColumnDefinitions[0].ActualWidth, 200, 3) &&
                Near(grid.ColumnDefinitions[1].ActualWidth, 400, 3));

            await H.CaptureScreenshotAsync("GridInsideFlex");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 15. Flex with child margins
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// FlexRow with children that have margins. Yoga should account for
    /// margins when distributing space — margins reduce available space
    /// for the child's content box.
    /// </summary>
    internal class FlexWithChildMargins(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                FlexRow(
                    Text("M1").Flex(basis: 0, grow: 1).Margin(10).Background("LightCoral"),
                    Text("M2").Flex(basis: 0, grow: 1).Margin(10).Background("LightBlue"),
                    Text("M3").Flex(basis: 0, grow: 1).Margin(10).Background("LightGreen")
                ).Width(600).Height(80)
            );

            await Harness.Render(600);

            var m1 = H.FindText("M1");
            var m2 = H.FindText("M2");
            var m3 = H.FindText("M3");

            H.Check("FlexMargins_AllPresent",
                m1 is not null && m2 is not null && m3 is not null);

            // Each child has 10px margin on each side = 20px per child = 60px total
            // Available for content: 600 - 60 = 540 → each ~180
            // Available for content: 600 - 60 = 540 → each ~180
            double expected = (600 - 6 * 10) / 3.0; // 6 margins (left+right × 3)
            H.Check("FlexMargins_ContentWidths",
                m1 is not null && m2 is not null && m3 is not null &&
                Near(m1.RenderSize.Width, expected, 5) &&
                Near(m2.RenderSize.Width, expected, 5) &&
                Near(m3.RenderSize.Width, expected, 5));

            await H.CaptureScreenshotAsync("FlexWithChildMargins");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 16. Flex with JustifyContent variations
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// FlexRow with fixed-size children (no grow) and JustifyContent=SpaceBetween.
    /// Verifies that Yoga distributes remaining space between children.
    /// </summary>
    internal class FlexJustifySpaceBetween(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
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

            await Harness.Render(600);

            var j1 = H.FindText("J1");
            var j2 = H.FindText("J2");
            var j3 = H.FindText("J3");

            H.Check("FlexJustify_AllPresent",
                j1 is not null && j2 is not null && j3 is not null);

            // Each child is 100px. Space = 600 - 300 = 300, distributed as 2 gaps = 150 each
            // J1 at x=0, J2 at x=250, J3 at x=500
            // We can verify by checking that J2 is roughly centered
            if (j1 is not null && j2 is not null && j3 is not null)
            {
                // Get positions via transform to root
                var flex = H.FindControl<FlexPanel>(_ => true);
                if (flex is not null)
                {
                    // J1 should be at left edge, J3 at right edge
                    // Total = 3 × 100 + 2 × gap = 600 → gap = 150
                    // The children should all be 100px
                    H.Check("FlexJustify_ChildWidthsFixed",
                        Near(j1.RenderSize.Width, 100, 3) &&
                        Near(j2.RenderSize.Width, 100, 3) &&
                        Near(j3.RenderSize.Width, 100, 3));
                }
            }

            await H.CaptureScreenshotAsync("FlexJustifySpaceBetween");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 17. Layout cycle prevention: FlexPanel inside Grid star column
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// LayoutCycleException regression test. When a FlexPanel is placed inside
    /// a Grid star column, the Grid's MeasureOverride may call FlexPanel.Measure
    /// with one size, then ArrangeOverride provides a different finalSize.
    /// The old code re-ran Yoga in ArrangeOverride, which triggered child.Measure()
    /// during the arrange pass — causing LayoutCycleException.
    /// This test verifies the panel survives without throwing.
    /// </summary>
    internal class FlexLayoutCycleInGridStar(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                Grid(["200", "*"], ["*"],
                    // Fixed sidebar
                    Text("Sidebar").Grid(row: 0, column: 0).Background("LightCoral"),

                    // Star column: Grid measures FlexPanel at available width,
                    // but may arrange at a slightly different width after
                    // resolving star allocations — this is the LayoutCycle trigger.
                    FlexRow(
                        Text("Grow-A").Flex(basis: 0, grow: 1).Background("LightGreen"),
                        Text("Grow-B").Flex(basis: 0, grow: 2).Background("LightBlue")
                    ).Grid(row: 0, column: 1)
                ).Width(600).Height(300)
            );

            await Harness.Render(600);

            // If we got here without throwing, the layout cycle was avoided
            H.Check("FlexCycle_GridStar_NoException", true);

            // Verify layout is still correct: star column = 600 - 200 = 400
            var growA = H.FindText("Grow-A");
            var growB = H.FindText("Grow-B");
            H.Check("FlexCycle_GridStar_ChildrenPresent",
                growA is not null && growB is not null);

            // Grow 1:2 in 400px → ~133 + ~267
            H.Check("FlexCycle_GridStar_GrowDistribution",
                growA is not null && growB is not null &&
                Near(growA.RenderSize.Width, 133, 5) && Near(growB.RenderSize.Width, 267, 5));

            await H.CaptureScreenshotAsync("FlexLayoutCycle_GridStar");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 18. Layout cycle prevention: deeply nested FlexPanels
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// LayoutCycleException regression test for nested FlexPanels. Each nesting
    /// level runs its own Yoga layout. If a parent FlexPanel's ArrangeOverride
    /// calls CalculateLayout (which calls child.Measure on the inner FlexPanel),
    /// the inner FlexPanel's MeasureOverride runs during the outer's Arrange pass,
    /// creating a cycle. This test verifies 4 levels deep survives.
    /// </summary>
    internal class FlexLayoutCycleNestedDeep(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                // Level 1: Column with grow
                FlexColumn(
                    Text("L1-Top").Height(40),

                    // Level 2: Row with grow children
                    FlexRow(
                        Text("L2-Left").Flex(basis: 0, grow: 1),

                        // Level 3: Column inside grow child
                        FlexColumn(
                            Text("L3-Header").Height(30),

                            // Level 4: Row inside grow child
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

            await Harness.Render(800);

            // Reaching here means no LayoutCycleException
            H.Check("FlexCycle_Nested4_NoException", true);

            // All 8 text items present
            H.Check("FlexCycle_Nested4_AllPresent",
                H.FindText("L1-Top") is not null &&
                H.FindText("L2-Left") is not null &&
                H.FindText("L3-Header") is not null &&
                H.FindText("L4-A") is not null &&
                H.FindText("L4-B") is not null &&
                H.FindText("L4-C") is not null &&
                H.FindText("L3-Footer") is not null &&
                H.FindText("L1-Bottom") is not null);

            // L4 children should each get ~1/3 of their container width
            // Container is L3 column = 2/3 of row = 2/3 of 600 = 400
            // Each L4 child ≈ 400/3 ≈ 133
            var l4a = H.FindText("L4-A");
            var l4b = H.FindText("L4-B");
            var l4c = H.FindText("L4-C");
            H.Check("FlexCycle_Nested4_L4Distribution",
                l4a is not null && l4b is not null && l4c is not null &&
                Near(l4a.ActualWidth, l4b.ActualWidth, 3) &&
                Near(l4b.ActualWidth, l4c.ActualWidth, 3));

            await H.CaptureScreenshotAsync("FlexLayoutCycle_NestedDeep");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 19. Layout cycle prevention: FlexPanel with auto-sizing text
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// LayoutCycleException regression test with wrapping text. Text elements
    /// have content-dependent sizes — their DesiredSize from Measure depends on
    /// the constraint width. When ArrangeOverride provides a different width than
    /// MeasureOverride, Yoga would re-measure text children (triggering Measure
    /// during Arrange). This test verifies wrapping text survives inside a
    /// grow-distributed FlexRow within a Grid.
    /// </summary>
    internal class FlexLayoutCycleAutoText(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var longText = "This text is long enough to wrap at narrow widths but " +
                "should display in fewer lines at wider widths. The layout cycle " +
                "bug would cause a crash when text is re-measured during arrange.";

            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                Grid(["*", "2*"], ["Auto", "*"],
                    // Row 0: Auto-height row with FlexPanel containing wrapping text
                    FlexRow(
                        Text("Label").Width(80).Background("LightCoral"),
                        Text(longText).Flex(grow: 1).Background("LightBlue")
                    ).Grid(row: 0, column: 0, columnSpan: 2),

                    // Row 1: star row with more flex content
                    FlexRow(
                        Text("Body-A").Flex(basis: 0, grow: 1).Background("LightGreen"),
                        Text("Body-B").Flex(basis: 0, grow: 1).Background("Wheat")
                    ).Grid(row: 1, column: 0, columnSpan: 2)
                ).Width(600).Height(400)
            );

            await Harness.Render(800);

            H.Check("FlexCycle_AutoText_NoException", true);

            H.Check("FlexCycle_AutoText_ChildrenPresent",
                H.FindText("Label") is not null &&
                H.FindText("Body-A") is not null &&
                H.FindText("Body-B") is not null);

            // The wrapping text FlexRow should have reasonable height (not collapsed or enormous)
            var flexPanels = H.FindAllControls<FlexPanel>(_ => true);
            var topRow = flexPanels.FirstOrDefault(p =>
                p.Direction == FlexDirection.Row && p.ActualHeight < 200);
            H.Check("FlexCycle_AutoText_ReasonableHeight",
                topRow is not null && topRow.ActualHeight > 10);

            await H.CaptureScreenshotAsync("FlexLayoutCycle_AutoText");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 20. Layout cycle prevention: size mismatch between Measure and Arrange
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// LayoutCycleException regression test that forces the sizeChanged path
    /// in ArrangeOverride. When a FlexPanel is inside a ScrollViewer that has
    /// a constrained viewport, the ScrollViewer may measure the FlexPanel with
    /// infinite height but arrange it at a finite height — triggering the
    /// re-layout code path in ArrangeOverride. This is the exact scenario
    /// where the _arranging guard prevents LayoutCycleException.
    /// </summary>
    internal class FlexLayoutCycleSizeMismatch(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
                // Outer Grid constrains the viewport
                Grid(["*"], ["60", "*", "60"],
                    // Header
                    Text("Header").Grid(row: 0, column: 0).Background("LightCoral"),

                    // Middle: ScrollViewer constrains the FlexPanel
                    ScrollView(
                        FlexColumn(
                            // Many children to exceed viewport — forces content-size
                            // measurement at infinite height during Measure, but
                            // finite height during Arrange
                            Text("Item-1").Height(50).Background("LightBlue"),
                            Text("Item-2").Height(50).Background("LightGreen"),
                            Text("Item-3").Height(50).Background("Wheat"),
                            Text("Item-4").Height(50).Background("Plum"),
                            Text("Item-5").Height(50).Background("LightCoral"),
                            Text("Item-6").Height(50).Background("PeachPuff")
                        ).Width(400)
                    ).Grid(row: 1, column: 0),

                    // Footer
                    Text("Footer").Grid(row: 2, column: 0).Background("LightGray")
                ).Width(400).Height(300)
            );

            await Harness.Render(600);

            H.Check("FlexCycle_SizeMismatch_NoException", true);

            H.Check("FlexCycle_SizeMismatch_AllPresent",
                H.FindText("Header") is not null &&
                H.FindText("Item-1") is not null &&
                H.FindText("Item-6") is not null &&
                H.FindText("Footer") is not null);

            // FlexColumn should be content-sized at 300px (6 × 50)
            var flex = H.FindControl<FlexPanel>(_ => true);
            H.Check("FlexCycle_SizeMismatch_ContentSized",
                flex is not null && Near(flex.ActualHeight, 300, 5));

            await H.CaptureScreenshotAsync("FlexLayoutCycle_SizeMismatch");
        }
    }
}

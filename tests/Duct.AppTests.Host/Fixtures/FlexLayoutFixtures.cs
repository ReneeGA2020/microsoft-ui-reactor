using Duct;
using Duct.Core;
using Duct.Flex;
using static Duct.UI;

namespace Duct.AppTests.Host.Fixtures;

/// <summary>
/// Comprehensive Flex layout fixtures covering nesting, composition with
/// other layout panels, scroll interaction, and cross-axis sizing.
/// All elements that tests measure have AutomationIds.
/// </summary>
internal static class FlexLayoutFixtures
{
    // 1. Nested Flex: FlexRow children inside FlexColumn (holy grail)
    internal static Element FlexNestedRowInColumn(RenderContext ctx) =>
        FlexColumn(
            // Header row: fixed height
            FlexRow(
                Text("Logo").Flex(basis: 0, grow: 1).AutomationId("Logo").Background("LightCoral"),
                Text("Nav").Flex(basis: 0, grow: 2).AutomationId("Nav").Background("LightBlue")
            ).Height(50).AutomationId("HeaderRow"),

            // Body row: grows to fill remaining space
            FlexRow(
                Text("Sidebar").Flex(basis: 0, grow: 1).AutomationId("Sidebar").Background("LightGreen"),
                Text("Content").Flex(basis: 0, grow: 3).AutomationId("Content").Background("LightYellow")
            ).Flex(grow: 1).AutomationId("BodyRow"),

            // Footer row: fixed height
            FlexRow(
                Text("Footer").Flex(grow: 1).AutomationId("Footer")
            ).Height(40).Background("LightGray").AutomationId("FooterRow")
        ).Width(600).Height(400);

    // 2. Nested Flex: FlexColumn inside FlexRow (sidebar layout)
    internal static Element FlexNestedColumnInRow(RenderContext ctx) =>
        FlexRow(
            // Sidebar: 1/4 width
            FlexColumn(
                Text("Menu A").AutomationId("MenuA"),
                Text("Menu B").AutomationId("MenuB"),
                Text("Menu C").AutomationId("MenuC")
            ).Flex(basis: 0, grow: 1).Background("LightCoral").AutomationId("SidebarCol"),

            // Content area: 3/4 width
            FlexColumn(
                Text("Title").Height(40).AutomationId("Title"),
                Text("Body text here").Flex(grow: 1).AutomationId("BodyText")
            ).Flex(basis: 0, grow: 3).Background("LightBlue").AutomationId("ContentCol")
        ).Width(600).Height(300);

    // 3. Deep nesting: 3+ levels of FlexPanels
    internal static Element FlexNestedDeep(RenderContext ctx) =>
        // Level 1: Column
        FlexColumn(
            Text("L1-Header").Height(30).AutomationId("L1Header"),

            // Level 2: Row (grows)
            FlexRow(
                Text("L2-Left").Flex(basis: 0, grow: 1).AutomationId("L2Left"),

                // Level 3: Column inside the row
                FlexColumn(
                    Text("L3-Top").AutomationId("L3Top"),
                    Text("L3-Mid").Flex(grow: 1).AutomationId("L3Mid"),
                    Text("L3-Bot").AutomationId("L3Bot")
                ).Flex(basis: 0, grow: 2).AutomationId("L3Col")
            ).Flex(grow: 1).AutomationId("L2Row"),

            Text("L1-Footer").Height(30).AutomationId("L1Footer")
        ).Width(600).Height(400);

    // 4. Flex inside Grid star cells
    internal static Element FlexInsideGrid(RenderContext ctx) =>
        Grid(["*", "2*"], ["*", "*"],
            // Top-left: FlexRow in 1* cell
            FlexRow(
                Text("A1").Flex(grow: 1).AutomationId("A1").Background("LightCoral"),
                Text("A2").Flex(grow: 1).AutomationId("A2").Background("LightBlue")
            ).Grid(row: 0, column: 0),

            // Top-right: FlexColumn in 2* cell
            FlexColumn(
                Text("B1").Flex(grow: 1).AutomationId("B1").Background("LightGreen"),
                Text("B2").Flex(grow: 2).AutomationId("B2").Background("LightYellow")
            ).Grid(row: 0, column: 1),

            // Bottom-left: single grow child
            FlexRow(
                Text("C1").Flex(grow: 1).AutomationId("C1").Background("Wheat")
            ).Grid(row: 1, column: 0),

            // Bottom-right: mixed fixed + grow
            FlexRow(
                Text("D1").Width(80).AutomationId("D1").Background("Plum"),
                Text("D2").Flex(grow: 1).AutomationId("D2").Background("PeachPuff")
            ).Grid(row: 1, column: 1)
        ).Width(600).Height(400);

    // 5. Flex inside Border -- cross-axis content sizing
    internal static Element FlexInsideBorder(RenderContext ctx) =>
        VStack(8,
            // A FlexRow inside a Border -- should only be as tall as its content
            Border(
                FlexRow(
                    Text("Left").Flex(grow: 1).AutomationId("Left").Background("LightCoral"),
                    Text("Right").Flex(grow: 1).AutomationId("Right").Background("LightBlue")
                ).AutomationId("FlexInBorderRow")
            ).Width(600).Background("LightGray"),

            // Reference: plain text for height comparison
            Text("Reference line").AutomationId("ReferenceLine")
        ).Width(600).Height(400);

    // 6. Flex inside VStack
    internal static Element FlexInsideVStack(RenderContext ctx) =>
        VStack(4,
            Text("Before").AutomationId("Before"),

            FlexRow(
                Text("Row1-A").Flex(basis: 0, grow: 1).AutomationId("Row1A").Background("LightCoral"),
                Text("Row1-B").Flex(basis: 0, grow: 2).AutomationId("Row1B").Background("LightBlue"),
                Text("Row1-C").Flex(basis: 0, grow: 1).AutomationId("Row1C").Background("LightGreen")
            ).AutomationId("FlexRow1"),

            FlexRow(
                Text("Row2-A").Flex(basis: 0, grow: 1).AutomationId("Row2A").Background("Wheat"),
                Text("Row2-B").Flex(basis: 0, grow: 1).AutomationId("Row2B").Background("Plum")
            ).AutomationId("FlexRow2"),

            Text("After").AutomationId("After")
        ).Width(600).Height(400);

    // 7. Flex inside ScrollViewer (infinite height)
    internal static Element FlexInsideScrollView(RenderContext ctx) =>
        ScrollView(
            FlexColumn(
                Text("Item 1").Height(60).AutomationId("ScrollItem1").Background("LightCoral"),
                Text("Item 2").Height(60).AutomationId("ScrollItem2").Background("LightBlue"),
                Text("Item 3").Height(60).AutomationId("ScrollItem3").Background("LightGreen"),
                Text("Item 4").Height(60).AutomationId("ScrollItem4").Background("Wheat"),
                Text("Item 5").Height(60).AutomationId("ScrollItem5").Background("Plum")
            ).Width(400).AutomationId("ScrollFlexCol")
        ).Width(400).Height(200);

    // 8. ScrollViewer inside a Flex grow child
    internal static Element ScrollViewInsideFlex(RenderContext ctx) =>
        FlexColumn(
            Text("Header").Height(50).Flex(shrink: 0).AutomationId("Header").Background("LightCoral"),

            ScrollView(
                VStack(0,
                    Text("Scroll Item 1").Height(80).AutomationId("SIFScrollItem1"),
                    Text("Scroll Item 2").Height(80).AutomationId("SIFScrollItem2"),
                    Text("Scroll Item 3").Height(80).AutomationId("SIFScrollItem3"),
                    Text("Scroll Item 4").Height(80).AutomationId("SIFScrollItem4"),
                    Text("Scroll Item 5").Height(80).AutomationId("SIFScrollItem5")
                )
            ).Flex(grow: 1).AutomationId("ScrollViewerInFlex"),

            Text("Footer").Height(50).Flex(shrink: 0).AutomationId("Footer").Background("LightBlue")
        ).Width(400).Height(400);

    // 9. FlexColumn grow distribution (vertical main axis)
    internal static Element FlexColumnGrow(RenderContext ctx) =>
        FlexColumn(
            Text("Top").Flex(basis: 0, grow: 1).AutomationId("Top").Background("LightCoral"),
            Text("Middle").Flex(basis: 0, grow: 2).AutomationId("Middle").Background("LightGreen"),
            Text("Bottom").Flex(basis: 0, grow: 1).AutomationId("Bottom").Background("LightBlue")
        ).Width(400).Height(400);

    // 10. Mixed grow + fixed-width children
    internal static Element FlexMixedGrowFixed(RenderContext ctx) =>
        FlexRow(
            Text("Fixed1").Width(100).AutomationId("Fixed1").Background("LightCoral"),
            Text("Grow").Flex(grow: 1).AutomationId("Grow").Background("LightGreen"),
            Text("Fixed2").Width(100).AutomationId("Fixed2").Background("LightBlue")
        ).Width(600).Height(60);

    // 11. Flex with gaps
    internal static Element FlexWithGaps(RenderContext ctx) =>
        new FlexElement([
            Text("A").Flex(basis: 0, grow: 1).AutomationId("GapA").Background("LightCoral"),
            Text("B").Flex(basis: 0, grow: 1).AutomationId("GapB").Background("LightGreen"),
            Text("C").Flex(basis: 0, grow: 1).AutomationId("GapC").Background("LightBlue")
        ])
        {
            Direction = FlexDirection.Row,
            ColumnGap = 20,
        }.Width(600).Height(60);

    // 12. Flex with FlexPadding
    internal static Element FlexWithPadding(RenderContext ctx) =>
        FlexRow(
            Text("Padded-A").Flex(basis: 0, grow: 1).AutomationId("PaddedA").Background("LightCoral"),
            Text("Padded-B").Flex(basis: 0, grow: 1).AutomationId("PaddedB").Background("LightBlue")
        ).FlexPadding(20).Width(600).Height(100);

    // 13. Flex cross-axis content sizing regression
    internal static Element FlexCrossAxisTextHeight(RenderContext ctx)
    {
        var longText = "This is a moderately long piece of text that will wrap " +
            "within its allocated width. The number of lines depends on the " +
            "width constraint, so the height should be consistent.";

        return VStack(8,
            // FlexRow with grow + long text
            FlexRow(
                Text("Sidebar").Width(100).AutomationId("CrossSidebar").Background("LightCoral"),
                Text(longText).Flex(grow: 1).AutomationId("CrossLongText").Background("LightBlue")
            ).Width(600).Background("LightGray").AutomationId("CrossFlexRow"),

            // Reference: same text in a fixed-width container
            Text(longText).Width(500).AutomationId("CrossRefText").Background("LightGreen"),

            Text("Below-all-visible").AutomationId("BelowAll")
        ).Width(600).Height(600);
    }

    // 14. Grid inside Flex -- Grid as a grow child
    internal static Element GridInsideFlex(RenderContext ctx) =>
        FlexColumn(
            Text("Header").Height(50).AutomationId("GIFHeader").Background("LightCoral"),

            Grid(["*", "2*"], ["*"],
                Text("GridLeft").Grid(row: 0, column: 0).AutomationId("GridLeft").Background("LightGreen"),
                Text("GridRight").Grid(row: 0, column: 1).AutomationId("GridRight").Background("LightBlue")
            ).Flex(grow: 1).AutomationId("GridInFlex"),

            Text("Footer").Height(50).AutomationId("GIFFooter").Background("Wheat")
        ).Width(600).Height(400);

    // 15. Flex with child margins
    internal static Element FlexWithChildMargins(RenderContext ctx) =>
        FlexRow(
            Text("M1").Flex(basis: 0, grow: 1).Margin(10).AutomationId("M1").Background("LightCoral"),
            Text("M2").Flex(basis: 0, grow: 1).Margin(10).AutomationId("M2").Background("LightBlue"),
            Text("M3").Flex(basis: 0, grow: 1).Margin(10).AutomationId("M3").Background("LightGreen")
        ).Width(600).Height(80);

    // 16. Flex with JustifyContent=SpaceBetween
    internal static Element FlexJustifySpaceBetween(RenderContext ctx) =>
        new FlexElement([
            Text("J1").Width(100).AutomationId("J1").Background("LightCoral"),
            Text("J2").Width(100).AutomationId("J2").Background("LightGreen"),
            Text("J3").Width(100).AutomationId("J3").Background("LightBlue"),
        ])
        {
            Direction = FlexDirection.Row,
            JustifyContent = FlexJustify.SpaceBetween,
        }.Width(600).Height(60);

    // 17. Layout cycle prevention: FlexPanel inside Grid star column
    internal static Element FlexLayoutCycleInGridStar(RenderContext ctx) =>
        Grid(["200", "*"], ["*"],
            // Fixed sidebar
            Text("Sidebar").Grid(row: 0, column: 0).AutomationId("CycleSidebar").Background("LightCoral"),

            // Star column: FlexPanel
            FlexRow(
                Text("Grow-A").Flex(basis: 0, grow: 1).AutomationId("GrowA").Background("LightGreen"),
                Text("Grow-B").Flex(basis: 0, grow: 2).AutomationId("GrowB").Background("LightBlue")
            ).Grid(row: 0, column: 1)
        ).Width(600).Height(300);

    // 18. Layout cycle prevention: deeply nested FlexPanels
    internal static Element FlexLayoutCycleNestedDeep(RenderContext ctx) =>
        // Level 1: Column with grow
        FlexColumn(
            Text("L1-Top").Height(40).AutomationId("CycleL1Top"),

            // Level 2: Row with grow children
            FlexRow(
                Text("L2-Left").Flex(basis: 0, grow: 1).AutomationId("CycleL2Left"),

                // Level 3: Column inside grow child
                FlexColumn(
                    Text("L3-Header").Height(30).AutomationId("CycleL3Header"),

                    // Level 4: Row inside grow child
                    FlexRow(
                        Text("L4-A").Flex(basis: 0, grow: 1).AutomationId("CycleL4A").Background("LightCoral"),
                        Text("L4-B").Flex(basis: 0, grow: 1).AutomationId("CycleL4B").Background("LightBlue"),
                        Text("L4-C").Flex(basis: 0, grow: 1).AutomationId("CycleL4C").Background("LightGreen")
                    ).Flex(grow: 1),

                    Text("L3-Footer").Height(30).AutomationId("CycleL3Footer")
                ).Flex(basis: 0, grow: 2)
            ).Flex(grow: 1),

            Text("L1-Bottom").Height(40).AutomationId("CycleL1Bottom")
        ).Width(600).Height(400);

    // 19. Layout cycle prevention: FlexPanel with auto-sizing text
    internal static Element FlexLayoutCycleAutoText(RenderContext ctx)
    {
        var longText = "This text is long enough to wrap at narrow widths but " +
            "should display in fewer lines at wider widths. The layout cycle " +
            "bug would cause a crash when text is re-measured during arrange.";

        return Grid(["*", "2*"], ["Auto", "*"],
            // Row 0: Auto-height row with FlexPanel containing wrapping text
            FlexRow(
                Text("Label").Width(80).AutomationId("AutoLabel").Background("LightCoral"),
                Text(longText).Flex(grow: 1).AutomationId("AutoLongText").Background("LightBlue")
            ).Grid(row: 0, column: 0, columnSpan: 2).AutomationId("AutoTextFlexRow"),

            // Row 1: star row with more flex content
            FlexRow(
                Text("Body-A").Flex(basis: 0, grow: 1).AutomationId("AutoBodyA").Background("LightGreen"),
                Text("Body-B").Flex(basis: 0, grow: 1).AutomationId("AutoBodyB").Background("Wheat")
            ).Grid(row: 1, column: 0, columnSpan: 2)
        ).Width(600).Height(400);
    }

    // 20. Layout cycle prevention: size mismatch between Measure and Arrange
    internal static Element FlexLayoutCycleSizeMismatch(RenderContext ctx) =>
        // Outer Grid constrains the viewport
        Grid(["*"], ["60", "*", "60"],
            // Header
            Text("Header").Grid(row: 0, column: 0).AutomationId("MismatchHeader").Background("LightCoral"),

            // Middle: ScrollViewer constrains the FlexPanel
            ScrollView(
                FlexColumn(
                    Text("Item-1").Height(50).AutomationId("MismatchItem1").Background("LightBlue"),
                    Text("Item-2").Height(50).AutomationId("MismatchItem2").Background("LightGreen"),
                    Text("Item-3").Height(50).AutomationId("MismatchItem3").Background("Wheat"),
                    Text("Item-4").Height(50).AutomationId("MismatchItem4").Background("Plum"),
                    Text("Item-5").Height(50).AutomationId("MismatchItem5").Background("LightCoral"),
                    Text("Item-6").Height(50).AutomationId("MismatchItem6").Background("PeachPuff")
                ).Width(400).AutomationId("MismatchFlexCol")
            ).Grid(row: 1, column: 0),

            // Footer
            Text("Footer").Grid(row: 2, column: 0).AutomationId("MismatchFooter").Background("LightGray")
        ).Width(400).Height(300);
}

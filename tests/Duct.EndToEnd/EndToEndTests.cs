using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Duct.EndToEnd;

/// <summary>
/// End-to-end tests that launch the test app in a separate process per fixture.
/// Each test is fault-isolated — one crash doesn't affect others.
/// Screenshots are captured to tests/Duct.EndToEnd.App/bin/.../screenshots/.
/// </summary>
[TestClass]
public class EndToEndTests
{
    // ── Error Handling ──────────────────────────────────────────────

    [TestMethod] public void ErrorBoundary_CatchesRenderError()
        => FixtureRunner.RunFixture("ErrorBoundary_CatchesRenderError");

    [TestMethod] public void ErrorBoundary_Recovery()
        => FixtureRunner.RunFixture("ErrorBoundary_Recovery");

    // ── Reconciler Core ─────────────────────────────────────────────

    [TestMethod] public void Reconciler_MountText()
        => FixtureRunner.RunFixture("Reconciler_MountText");

    [TestMethod] public void Reconciler_UpdateText()
        => FixtureRunner.RunFixture("Reconciler_UpdateText");

    [TestMethod] public void Reconciler_AddRemoveChildren()
        => FixtureRunner.RunFixture("Reconciler_AddRemoveChildren");

    [TestMethod] public void Reconciler_ComponentRerender()
        => FixtureRunner.RunFixture("Reconciler_ComponentRerender");

    [TestMethod] public void Reconciler_KeyedList()
        => FixtureRunner.RunFixture("Reconciler_KeyedList");

    // ── Layout ──────────────────────────────────────────────────────

    [TestMethod] public void FlexLayout_RowDistribution()
        => FixtureRunner.RunFixture("FlexLayout_RowDistribution");

    [TestMethod] public void FlexLayout_ColumnWrap()
        => FixtureRunner.RunFixture("FlexLayout_ColumnWrap");

    [TestMethod] public void Grid_RowColumnLayout()
        => FixtureRunner.RunFixture("Grid_RowColumnLayout");

    // ── Dynamic Updates ─────────────────────────────────────────────

    [TestMethod] public void DynamicList_GrowShrink()
        => FixtureRunner.RunFixture("DynamicList_GrowShrink");

    [TestMethod] public void ConditionalRendering_Toggle()
        => FixtureRunner.RunFixture("ConditionalRendering_Toggle");

    // ── Markdown ────────────────────────────────────────────────────

    [TestMethod] public void Markdown_HeadingsAndFormatting()
        => FixtureRunner.RunFixture("Markdown_HeadingsAndFormatting");

    [TestMethod] public void Markdown_CodeBlockAndLinks()
        => FixtureRunner.RunFixture("Markdown_CodeBlockAndLinks");

    // ── Monaco Editor ───────────────────────────────────────────────

    [TestMethod] public void MonacoEditor_Mounts()
        => FixtureRunner.RunFixture("MonacoEditor_Mounts");

    // ── DuctD3 Charts ───────────────────────────────────────────────

    [TestMethod] public void D3_LineChart()
        => FixtureRunner.RunFixture("D3_LineChart");

    [TestMethod] public void D3_BarChart()
        => FixtureRunner.RunFixture("D3_BarChart");

    [TestMethod] public void D3_PieChart()
        => FixtureRunner.RunFixture("D3_PieChart");

    // ── Collections & Navigation ────────────────────────────────────

    [TestMethod] public void ListView_TypedRendering()
        => FixtureRunner.RunFixture("ListView_TypedRendering");

    [TestMethod] public void Navigation_TabSwitching()
        => FixtureRunner.RunFixture("Navigation_TabSwitching");
}

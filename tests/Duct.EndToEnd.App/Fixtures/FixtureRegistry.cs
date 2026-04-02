namespace Duct.EndToEnd.App.Fixtures;

/// <summary>
/// Maps fixture names to their implementations.
/// </summary>
internal static class FixtureRegistry
{
    public static readonly string[] AllFixtures =
    [
        "ErrorBoundary_CatchesRenderError",
        "ErrorBoundary_Recovery",
        "Reconciler_MountText",
        "Reconciler_UpdateText",
        "Reconciler_AddRemoveChildren",
        "Reconciler_ComponentRerender",
        "Reconciler_KeyedList",
        "FlexLayout_RowDistribution",
        "FlexLayout_ColumnWrap",
        "Grid_RowColumnLayout",
        "DynamicList_GrowShrink",
        "ConditionalRendering_Toggle",
        "Markdown_HeadingsAndFormatting",
        "Markdown_CodeBlockAndLinks",
        "MonacoEditor_Mounts",
        "D3_LineChart",
        "D3_BarChart",
        "D3_PieChart",
        "ListView_TypedRendering",
        "Navigation_TabSwitching",
    ];

    public static FixtureBase? Create(string name, Harness harness) => name switch
    {
        "ErrorBoundary_CatchesRenderError" => new ErrorBoundaryFixtures.CatchesRenderError(harness),
        "ErrorBoundary_Recovery" => new ErrorBoundaryFixtures.Recovery(harness),
        "Reconciler_MountText" => new ReconcilerFixtures.MountText(harness),
        "Reconciler_UpdateText" => new ReconcilerFixtures.UpdateText(harness),
        "Reconciler_AddRemoveChildren" => new ReconcilerFixtures.AddRemoveChildren(harness),
        "Reconciler_ComponentRerender" => new ReconcilerFixtures.ComponentRerender(harness),
        "Reconciler_KeyedList" => new ReconcilerFixtures.KeyedList(harness),
        "FlexLayout_RowDistribution" => new LayoutFixtures.FlexRowDistribution(harness),
        "FlexLayout_ColumnWrap" => new LayoutFixtures.FlexColumnWrap(harness),
        "Grid_RowColumnLayout" => new LayoutFixtures.GridRowColumn(harness),
        "DynamicList_GrowShrink" => new DynamicFixtures.ListGrowShrink(harness),
        "ConditionalRendering_Toggle" => new DynamicFixtures.ConditionalToggle(harness),
        "Markdown_HeadingsAndFormatting" => new MarkdownFixtures.HeadingsAndFormatting(harness),
        "Markdown_CodeBlockAndLinks" => new MarkdownFixtures.CodeBlockAndLinks(harness),
        "MonacoEditor_Mounts" => new MonacoFixtures.EditorMounts(harness),
        "D3_LineChart" => new D3Fixtures.LineChart(harness),
        "D3_BarChart" => new D3Fixtures.BarChart(harness),
        "D3_PieChart" => new D3Fixtures.PieChart(harness),
        "ListView_TypedRendering" => new CollectionFixtures.ListViewTyped(harness),
        "Navigation_TabSwitching" => new NavigationFixtures.TabSwitching(harness),
        _ => null,
    };
}

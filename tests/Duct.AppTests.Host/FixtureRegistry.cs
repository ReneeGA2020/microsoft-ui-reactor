using Duct;
using Duct.Core;
using Duct.AppTests.Host.Fixtures;

namespace Duct.AppTests.Host;

/// <summary>
/// Maps fixture names to their Element-building methods.
/// Each fixture is either a static method (RenderContext -> Element) or
/// returns a Component via Component&lt;T&gt;().
/// </summary>
internal static class FixtureRegistry
{
    public static readonly string[] AllFixtures =
    [
        // Layout
        "FlexLayout_RowDistribution",
        "FlexLayout_ColumnWrap",
        "Grid_RowColumnLayout",
        "GridVsFlex_StarSizing",

        // Flex Layout (20 fixtures)
        "Flex_NestedRowInColumn",
        "Flex_NestedColumnInRow",
        "Flex_NestedDeep",
        "Flex_InsideGrid",
        "Flex_InsideBorder",
        "Flex_InsideVStack",
        "Flex_InsideScrollView",
        "Flex_ScrollViewInsideFlex",
        "Flex_ColumnGrow",
        "Flex_MixedGrowFixed",
        "Flex_WithGaps",
        "Flex_WithPadding",
        "Flex_CrossAxisTextHeight",
        "Flex_GridInsideFlex",
        "Flex_WithChildMargins",
        "Flex_JustifySpaceBetween",
        "Flex_LayoutCycleInGridStar",
        "Flex_LayoutCycleNestedDeep",
        "Flex_LayoutCycleAutoText",
        "Flex_LayoutCycleSizeMismatch",

        // Reconciler
        "Reconciler_MountText",
        "Reconciler_UpdateText",
        "Reconciler_AddRemoveChildren",
        "Reconciler_ComponentRerender",
        "Reconciler_KeyedList",
        "Reconciler_GridDynamicChildCount",
        "Reconciler_AllLayoutsDynamicChildCount",

        // Error Boundary
        "ErrorBoundary_CatchesRenderError",
        "ErrorBoundary_Recovery",

        // Dynamic
        "DynamicList_GrowShrink",
        "ConditionalRendering_Toggle",

        // Collections
        "ListView_TypedRendering",

        // Navigation
        "Navigation_TabSwitching",

        // Observable
        "Observable_UseObservable_Rerender",
        "Observable_UseObservable_ExternalMutation",
        "Observable_UseObservableProperty_FineGrained",
        "Observable_UseCollection_ListUpdates",
        "Observable_UseObservable_SourceSwap",

        // PropertyGrid
        "PropertyGrid_Reflection_MutableObject",
        "PropertyGrid_Reflection_Categorized",
        "PropertyGrid_Reflection_EnumEditor",
        "PropertyGrid_Nested_ImmutableRecord",
        "PropertyGrid_Immutable_Root",
        "PropertyGrid_Custom_Editor",
        "PropertyGrid_Target_Switching",
        "PropertyGrid_Category_ExpandCollapse",
        "PropertyGrid_DeepNesting_RecordInRecord",
        "PropertyGrid_INPC_ExternalMutation",

        // Markdown
        "Markdown_HeadingsAndFormatting",
        "Markdown_CodeBlockAndLinks",

        // Monaco
        "MonacoEditor_Mounts",

        // D3 Charts
        "D3_LineChart",
        "D3_BarChart",
        "D3_PieChart",

        // Localization
        "Localization_LocaleSwitching",

        // Demo
        "Demo_Counter",
        "Demo_Conditional",
        "Demo_TabNavigation",
    ];

    public static Element? Build(string name, RenderContext ctx) => name switch
    {
        // Layout
        "FlexLayout_RowDistribution" => LayoutFixtures.FlexRowDistribution(ctx),
        "FlexLayout_ColumnWrap" => LayoutFixtures.FlexColumnWrap(ctx),
        "Grid_RowColumnLayout" => LayoutFixtures.GridRowColumn(ctx),
        "GridVsFlex_StarSizing" => LayoutFixtures.GridVsFlexStarSizing(ctx),

        // Flex Layout
        "Flex_NestedRowInColumn" => FlexLayoutFixtures.FlexNestedRowInColumn(ctx),
        "Flex_NestedColumnInRow" => FlexLayoutFixtures.FlexNestedColumnInRow(ctx),
        "Flex_NestedDeep" => FlexLayoutFixtures.FlexNestedDeep(ctx),
        "Flex_InsideGrid" => FlexLayoutFixtures.FlexInsideGrid(ctx),
        "Flex_InsideBorder" => FlexLayoutFixtures.FlexInsideBorder(ctx),
        "Flex_InsideVStack" => FlexLayoutFixtures.FlexInsideVStack(ctx),
        "Flex_InsideScrollView" => FlexLayoutFixtures.FlexInsideScrollView(ctx),
        "Flex_ScrollViewInsideFlex" => FlexLayoutFixtures.ScrollViewInsideFlex(ctx),
        "Flex_ColumnGrow" => FlexLayoutFixtures.FlexColumnGrow(ctx),
        "Flex_MixedGrowFixed" => FlexLayoutFixtures.FlexMixedGrowFixed(ctx),
        "Flex_WithGaps" => FlexLayoutFixtures.FlexWithGaps(ctx),
        "Flex_WithPadding" => FlexLayoutFixtures.FlexWithPadding(ctx),
        "Flex_CrossAxisTextHeight" => FlexLayoutFixtures.FlexCrossAxisTextHeight(ctx),
        "Flex_GridInsideFlex" => FlexLayoutFixtures.GridInsideFlex(ctx),
        "Flex_WithChildMargins" => FlexLayoutFixtures.FlexWithChildMargins(ctx),
        "Flex_JustifySpaceBetween" => FlexLayoutFixtures.FlexJustifySpaceBetween(ctx),
        "Flex_LayoutCycleInGridStar" => FlexLayoutFixtures.FlexLayoutCycleInGridStar(ctx),
        "Flex_LayoutCycleNestedDeep" => FlexLayoutFixtures.FlexLayoutCycleNestedDeep(ctx),
        "Flex_LayoutCycleAutoText" => FlexLayoutFixtures.FlexLayoutCycleAutoText(ctx),
        "Flex_LayoutCycleSizeMismatch" => FlexLayoutFixtures.FlexLayoutCycleSizeMismatch(ctx),

        // Reconciler
        "Reconciler_MountText" => ReconcilerFixtures.MountText(ctx),
        "Reconciler_UpdateText" => ReconcilerFixtures.UpdateText(ctx),
        "Reconciler_AddRemoveChildren" => ReconcilerFixtures.AddRemoveChildren(ctx),
        "Reconciler_ComponentRerender" => ReconcilerFixtures.ComponentRerender(ctx),
        "Reconciler_KeyedList" => ReconcilerFixtures.KeyedList(ctx),
        "Reconciler_GridDynamicChildCount" => ReconcilerFixtures.GridDynamicChildCount(ctx),
        "Reconciler_AllLayoutsDynamicChildCount" => ReconcilerFixtures.AllLayoutsDynamicChildCount(ctx),

        // Error Boundary
        "ErrorBoundary_CatchesRenderError" => ErrorBoundaryFixtures.CatchesRenderError(ctx),
        "ErrorBoundary_Recovery" => ErrorBoundaryFixtures.Recovery(ctx),

        // Dynamic
        "DynamicList_GrowShrink" => DynamicFixtures.ListGrowShrink(ctx),
        "ConditionalRendering_Toggle" => DynamicFixtures.ConditionalToggle(ctx),

        // Collections
        "ListView_TypedRendering" => CollectionFixtures.ListViewTyped(ctx),

        // Navigation
        "Navigation_TabSwitching" => NavigationFixtures.TabSwitching(ctx),

        // Observable
        "Observable_UseObservable_Rerender" => ObservableFixtures.UseObservable_Rerender(ctx),
        "Observable_UseObservable_ExternalMutation" => ObservableFixtures.UseObservable_ExternalMutation(ctx),
        "Observable_UseObservableProperty_FineGrained" => ObservableFixtures.UseObservableProperty_FineGrained(ctx),
        "Observable_UseCollection_ListUpdates" => ObservableFixtures.UseCollection_ListUpdates(ctx),
        "Observable_UseObservable_SourceSwap" => ObservableFixtures.UseObservable_SourceSwap(ctx),

        // PropertyGrid
        "PropertyGrid_Reflection_MutableObject" => PropertyGridFixtures.Reflection_MutableObject(ctx),
        "PropertyGrid_Reflection_Categorized" => PropertyGridFixtures.Reflection_Categorized(ctx),
        "PropertyGrid_Reflection_EnumEditor" => PropertyGridFixtures.Reflection_EnumEditor(ctx),
        "PropertyGrid_Nested_ImmutableRecord" => PropertyGridFixtures.Nested_ImmutableRecord(ctx),
        "PropertyGrid_Immutable_Root" => PropertyGridFixtures.Immutable_Root(ctx),
        "PropertyGrid_Custom_Editor" => PropertyGridFixtures.Custom_Editor(ctx),
        "PropertyGrid_Target_Switching" => PropertyGridFixtures.Target_Switching(ctx),
        "PropertyGrid_Category_ExpandCollapse" => PropertyGridFixtures.Category_ExpandCollapse(ctx),
        "PropertyGrid_DeepNesting_RecordInRecord" => PropertyGridFixtures.DeepNesting_RecordInRecord(ctx),
        "PropertyGrid_INPC_ExternalMutation" => PropertyGridFixtures.INPC_ExternalMutation(ctx),

        // Markdown
        "Markdown_HeadingsAndFormatting" => MarkdownFixtures.HeadingsAndFormatting(ctx),
        "Markdown_CodeBlockAndLinks" => MarkdownFixtures.CodeBlockAndLinks(ctx),

        // Monaco
        "MonacoEditor_Mounts" => MonacoFixtures.EditorMounts(ctx),

        // D3 Charts
        "D3_LineChart" => D3Fixtures.LineChart(ctx),
        "D3_BarChart" => D3Fixtures.BarChart(ctx),
        "D3_PieChart" => D3Fixtures.PieChart(ctx),

        // Localization
        "Localization_LocaleSwitching" => LocalizationFixtures.LocaleSwitching(ctx),

        // Demo
        "Demo_Counter" => DemoFixtures.CounterDemo(ctx),
        "Demo_Conditional" => DemoFixtures.ConditionalDemo(ctx),
        "Demo_TabNavigation" => DemoFixtures.TabNavigation(ctx),

        _ => null,
    };
}

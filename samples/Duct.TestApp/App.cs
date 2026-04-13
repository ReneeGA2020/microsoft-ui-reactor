// Duct Test App — A WinUI 3 application using the Duct functional projection.
// No XAML. No data binding. No resources. No templates. Just C#.

using System.Diagnostics;
using Duct;
using Duct.Core;
using Duct.Core.Navigation;
using Duct.Flex;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Duct.PropertyGrid;
using static Duct.UI;
using static Duct.Core.Theme;

DuctApp.Run<DemoApp>("Duct Demo", width: 1200, height: 800
#if DEBUG
    , preview: true
#endif
);

// ─── Root application component ────────────────────────────────────────────────

enum Tab { Counter, TodoList, ConditionalUI, Form, DynamicList, PerfStress, Virtualization, Flyout, DataTemplate, FlexPanel, Transitions, PropertyGrid, DataSystem, DataGrid, IntegratedData, Context, Memo, Persisted, Slots, Navigation, Commanding }

class DemoApp : Component
{
    static readonly string[] Languages = ["English"];

    static readonly string[] TabLabels = Enum.GetValues<Tab>()
        .Select(tab => tab switch
        {
            Tab.Counter => "Counter",
            Tab.TodoList => "Todo List",
            Tab.ConditionalUI => "Conditional UI",
            Tab.Form => "Form",
            Tab.DynamicList => "Dynamic List",
            Tab.PerfStress => "Perf Stress",
            Tab.Virtualization => "Virtualization",
            Tab.Flyout => "Flyout",
            Tab.DataTemplate => "DataTemplate",
            Tab.FlexPanel => "FlexPanel",
            Tab.Transitions => "Transitions",
            Tab.PropertyGrid => "PropertyGrid",
            Tab.DataSystem => "Data System",
            Tab.DataGrid => "DataGrid",
            Tab.IntegratedData => "Integrated Data",
            Tab.Context => "Context",
            Tab.Memo => "Memo",
            Tab.Persisted => "Persisted",
            Tab.Slots => "Slots",
            Tab.Navigation => "Navigation",
            Tab.Commanding => "Commanding",
            _ => tab.ToString()
        }).ToArray();

    public override Element Render()
    {
        var (currentTab, setTab) = UseState(Tab.Counter);
        var (langIndex, setLangIndex) = UseState(0);

        return FlexColumn(
            (TitleBar("TestApp") with
            {
                Content = HStack(8,
                    ComboBox(TabLabels, (int)currentTab, i => setTab((Tab)i)).Width(200),
                    ComboBox(Languages, langIndex, setLangIndex)
                ),
            }).Flex(shrink: 0),

            // Content area — Flex(grow:1) fills remaining vertical space
            // so ScrollView inside each demo gets a bounded height and can scroll.
            Border(
                currentTab switch
                {
                    Tab.Counter => Component<CounterDemo>(),
                    Tab.TodoList => Component<TodoDemo>(),
                    Tab.ConditionalUI => Component<ConditionalDemo>(),
                    Tab.Form => Component<FormDemo>(),
                    Tab.DynamicList => Component<DynamicListDemo>(),
                    Tab.PerfStress => Component<PerfStressDemo>(),
                    Tab.Virtualization => Component<VirtualizationDemo>(),
                    Tab.Flyout => Component<FlyoutDemo>(),
                    Tab.DataTemplate => Component<DataTemplateDemo>(),
                    Tab.FlexPanel => Component<FlexPanelDemo>(),
                    Tab.Transitions => Component<TransitionsDemo>(),
                    Tab.PropertyGrid => Component<PropertyGridDemo>(),
                    Tab.DataSystem => Component<DataSystemDemo>(),
                    Tab.DataGrid => Component<DataGridDemo>(),
                    Tab.IntegratedData => Component<IntegratedDataDemo>(),
                    Tab.Context => Component<ContextDemo>(),
                    Tab.Memo => Component<MemoDemo>(),
                    Tab.Persisted => Component<PersistedDemo>(),
                    Tab.Slots => Component<SlotsDemo>(),
                    Tab.Navigation => Component<NavigationDemo>(),
                    Tab.Commanding => Component<CommandingTestDemo>(),
                    _ => Text("Select a tab")
                }
            ).Padding(24).Margin(16).Flex(grow: 1)
        );
    }
}

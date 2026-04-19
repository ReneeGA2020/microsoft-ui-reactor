using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.AppTests.Host.Fixtures;

internal static class DynamicFixtures
{
    // Interactive: list grow/shrink with buttons
    internal class ListGrowShrinkComponent : Component
    {
        public override Element Render()
        {
            var (count, setCount) = UseState(3);
            return VStack(
                HStack(
                    TextBlock($"Items: {count}").AutomationId("ItemCount"),
                    Button("Add", () => setCount(count + 1)).AutomationId("AddBtn"),
                    Button("Remove", () => setCount(Math.Max(0, count - 1))).AutomationId("RemoveBtn")
                ),
                VStack(Enumerable.Range(0, count)
                    .Select(i => TextBlock($"Item #{i}").WithKey($"item-{i}").AutomationId($"ListItem{i}"))
                    .ToArray())
            );
        }
    }

    internal static Element ListGrowShrink(RenderContext ctx) =>
        Component<ListGrowShrinkComponent>();

    // Interactive: checkbox toggles conditional content
    internal class ConditionalToggleComponent : Component
    {
        public override Element Render()
        {
            var (showAdvanced, setShowAdvanced) = UseState(false);
            return VStack(
                CheckBox(showAdvanced, v => setShowAdvanced(v), "Show details")
                    .AutomationId("ShowDetailsCheckBox"),
                showAdvanced
                    ? VStack(
                        TextBlock("Advanced Settings").AutomationId("AdvancedSettings"),
                        TextBlock("Debug mode: ON").AutomationId("DebugMode"),
                        TextBlock("Verbose logging: ON").AutomationId("VerboseLogging")
                    )
                    : Empty()
            );
        }
    }

    internal static Element ConditionalToggle(RenderContext ctx) =>
        Component<ConditionalToggleComponent>();
}

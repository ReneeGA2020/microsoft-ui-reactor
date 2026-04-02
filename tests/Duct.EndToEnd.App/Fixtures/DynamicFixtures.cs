using Duct;
using Duct.Core;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;

namespace Duct.EndToEnd.App.Fixtures;

internal static class DynamicFixtures
{
    internal class ListGrowShrink(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
            {
                var (count, setCount) = ctx.UseState(3);
                return VStack(
                    HStack(
                        Text($"Items: {count}"),
                        Button("Add", () => setCount(count + 1)),
                        Button("Remove", () => setCount(Math.Max(0, count - 1)))
                    ),
                    VStack(Enumerable.Range(0, count)
                        .Select(i => Text($"Item #{i}").WithKey($"item-{i}"))
                        .ToArray())
                );
            });

            await Harness.Render();

            H.Check("DynamicList_GrowShrink_InitialItems",
                H.FindText("Items: 3") is not null &&
                H.FindText("Item #0") is not null &&
                H.FindText("Item #2") is not null);

            H.ClickButton("Add");
            await Harness.Render();
            H.ClickButton("Add");
            await Harness.Render();

            H.Check("DynamicList_GrowShrink_AfterAdd",
                H.FindText("Items: 5") is not null &&
                H.FindText("Item #4") is not null);

            H.ClickButton("Remove");
            await Harness.Render();
            H.ClickButton("Remove");
            await Harness.Render();
            H.ClickButton("Remove");
            await Harness.Render();

            H.Check("DynamicList_GrowShrink_AfterRemove",
                H.FindText("Items: 2") is not null &&
                H.FindText("Item #2") is null);
        }
    }

    internal class ConditionalToggle(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
            {
                var (showAdvanced, setShowAdvanced) = ctx.UseState(false);
                return VStack(
                    CheckBox(showAdvanced, v => setShowAdvanced(v), "Show details"),
                    showAdvanced
                        ? VStack(
                            Text("Advanced Settings"),
                            Text("Debug mode: ON"),
                            Text("Verbose logging: ON")
                        )
                        : Empty()
                );
            });

            await Harness.Render();

            H.Check("ConditionalRendering_Toggle_InitiallyHidden",
                H.FindText("Advanced Settings") is null);

            H.ToggleCheckBox("Show details");
            await Harness.Render();

            H.Check("ConditionalRendering_Toggle_ShownAfterToggle",
                H.FindText("Advanced Settings") is not null &&
                H.FindText("Debug mode: ON") is not null);

            H.ToggleCheckBox("Show details");
            await Harness.Render();

            H.Check("ConditionalRendering_Toggle_HiddenAgain",
                H.FindText("Advanced Settings") is null);
        }
    }
}

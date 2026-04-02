using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;

namespace Duct.EndToEnd.App.Fixtures;

internal static class ReconcilerFixtures
{
    internal class MountText(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx => Text("Hello from Duct"));

            await Harness.Render();

            H.Check("Reconciler_MountText_TextAppears",
                H.FindText("Hello from Duct") is not null);

            H.Check("Reconciler_MountText_IsTextBlock",
                H.FindControl<TextBlock>(tb => tb.Text == "Hello from Duct") is not null);
        }
    }

    internal class UpdateText(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
            {
                var (text, setText) = ctx.UseState("Before");
                return VStack(
                    Text(text),
                    Button("Change", () => setText("After"))
                );
            });

            await Harness.Render();

            H.Check("Reconciler_UpdateText_InitialText",
                H.FindText("Before") is not null);

            H.ClickButton("Change");
            await Harness.Render();

            H.Check("Reconciler_UpdateText_UpdatedText",
                H.FindText("After") is not null);

            H.Check("Reconciler_UpdateText_OldTextGone",
                H.FindText("Before") is null);
        }
    }

    internal class AddRemoveChildren(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
            {
                var (count, setCount) = ctx.UseState(3);
                return VStack(
                    HStack(
                        Button("Add", () => setCount(count + 1)),
                        Button("Remove", () => setCount(Math.Max(0, count - 1)))
                    ),
                    VStack(Enumerable.Range(0, count).Select(i => Text($"Item {i}")).ToArray())
                );
            });

            await Harness.Render();

            H.Check("Reconciler_AddRemoveChildren_InitialCount",
                H.FindText("Item 0") is not null && H.FindText("Item 2") is not null);

            H.ClickButton("Add");
            await Harness.Render();

            H.Check("Reconciler_AddRemoveChildren_AfterAdd",
                H.FindText("Item 3") is not null);

            H.ClickButton("Remove");
            await Harness.Render();
            H.ClickButton("Remove");
            await Harness.Render();

            H.Check("Reconciler_AddRemoveChildren_AfterRemove",
                H.FindText("Item 3") is null && H.FindText("Item 2") is null);
        }
    }

    // Simple counter component to test re-rendering
    private class CounterComponent : Component
    {
        public override Element Render()
        {
            var (count, setCount) = UseState(0);
            return VStack(
                Text($"Count: {count}"),
                Button("Increment", () => setCount(count + 1))
            );
        }
    }

    internal class ComponentRerender(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(new CounterComponent());

            await Harness.Render();

            H.Check("Reconciler_ComponentRerender_InitialState",
                H.FindText("Count: 0") is not null);

            H.ClickButton("Increment");
            await Harness.Render();

            H.Check("Reconciler_ComponentRerender_AfterClick",
                H.FindText("Count: 1") is not null);

            H.ClickButton("Increment");
            await Harness.Render();
            H.ClickButton("Increment");
            await Harness.Render();

            H.Check("Reconciler_ComponentRerender_MultipleClicks",
                H.FindText("Count: 3") is not null);
        }
    }

    internal class KeyedList(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
            {
                var (reversed, setReversed) = ctx.UseState(false);
                var items = new[] { "Alpha", "Beta", "Gamma" };
                var ordered = reversed ? items.Reverse().ToArray() : items;

                return VStack(
                    Button("Reverse", () => setReversed(!reversed)),
                    VStack(ordered.Select(item => Text(item).WithKey(item)).ToArray())
                );
            });

            await Harness.Render();

            // Capture the initial TextBlock references
            var alpha1 = H.FindText("Alpha");
            H.Check("Reconciler_KeyedList_InitialOrder",
                alpha1 is not null && H.FindText("Beta") is not null && H.FindText("Gamma") is not null);

            H.ClickButton("Reverse");
            await Harness.Render();

            // After reversal, keyed items should be reused (same TextBlock instances)
            var alpha2 = H.FindText("Alpha");
            H.Check("Reconciler_KeyedList_AfterReverse",
                alpha2 is not null && H.FindText("Gamma") is not null);

            H.Check("Reconciler_KeyedList_ControlReused",
                ReferenceEquals(alpha1, alpha2));
        }
    }
}

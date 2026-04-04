using Duct;
using Duct.Core;
using static Duct.UI;

namespace Duct.AppTests.Host.Fixtures;

internal static class ReconcilerFixtures
{
    // Static: just mounts text
    internal static Element MountText(RenderContext ctx) =>
        Text("Hello from Duct").AutomationId("MountedText");

    // Interactive: button changes text
    internal class UpdateTextComponent : Component
    {
        public override Element Render()
        {
            var (text, setText) = UseState("Before");
            return VStack(
                Text(text).AutomationId("DisplayText"),
                Button("Change", () => setText("After")).AutomationId("ChangeBtn")
            );
        }
    }

    internal static Element UpdateText(RenderContext ctx) =>
        Component<UpdateTextComponent>();

    // Interactive: add/remove children
    internal class AddRemoveChildrenComponent : Component
    {
        public override Element Render()
        {
            var (count, setCount) = UseState(3);
            return VStack(
                HStack(
                    Button("Add", () => setCount(count + 1)).AutomationId("AddBtn"),
                    Button("Remove", () => setCount(Math.Max(0, count - 1))).AutomationId("RemoveBtn")
                ),
                VStack(Enumerable.Range(0, count)
                    .Select(i => Text($"Item {i}").AutomationId($"Item{i}"))
                    .ToArray())
            );
        }
    }

    internal static Element AddRemoveChildren(RenderContext ctx) =>
        Component<AddRemoveChildrenComponent>();

    // Interactive: counter component re-render
    internal class CounterComponent : Component
    {
        public override Element Render()
        {
            var (count, setCount) = UseState(0);
            return VStack(
                Text($"Count: {count}").AutomationId("CountText"),
                Button("Increment", () => setCount(count + 1)).AutomationId("IncrementBtn")
            );
        }
    }

    internal static Element ComponentRerender(RenderContext ctx) =>
        Component<CounterComponent>();

    // Interactive: keyed list reversal
    internal class KeyedListComponent : Component
    {
        public override Element Render()
        {
            var (reversed, setReversed) = UseState(false);
            var items = new[] { "Alpha", "Beta", "Gamma" };
            var ordered = reversed ? items.Reverse().ToArray() : items;

            return VStack(
                Button("Reverse", () => setReversed(!reversed)).AutomationId("ReverseBtn"),
                VStack(ordered.Select(item =>
                    Text(item).WithKey(item).AutomationId($"Key_{item}")
                ).ToArray())
            );
        }
    }

    internal static Element KeyedList(RenderContext ctx) =>
        Component<KeyedListComponent>();
}

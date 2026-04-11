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

class MemoDemo : Component
{
    public override Element Render()
    {
        var (parentRenders, setParentRenders) = UseState(0);
        var (childProp, setChildProp) = UseState("A");

        // Force parent re-render (increments counter)
        void BumpParent() => setParentRenders(parentRenders + 1);

        return ScrollView(VStack(12,
            Heading("Component Memoization"),
            Text("Memo skips re-rendering children when their inputs haven't changed."),

            // Parent controls
            SubHeading("Parent state"),
            Text($"Parent has rendered {parentRenders + 1} time(s)."),
            HStack(8,
                Button("Re-render parent", BumpParent),
                Button($"Change child prop (now \"{childProp}\")",
                    () => setChildProp(childProp == "A" ? "B" : "A"))
            ),

            // 1. Memoized class component
            SubHeading("1. Memoized class component (ShouldUpdate)"),
            Text("Component<TProps> uses record equality by default. Renders only when Props changes."),
            Component<RenderCounter, RenderCounterProps>(new(childProp)),

            // 2. Propless component
            SubHeading("2. Propless component (auto-memo)"),
            Text("Components without props return ShouldUpdate() => false — never re-render from parent."),
            Component<ProplessCounter>(),

            // 3. Memo() with deps
            SubHeading("3. Memo() function component with deps"),
            Text("Re-renders only when the dependency (childProp) changes."),
            Memo(ctx =>
            {
                var count = ctx.UseRef(0);
                count.Current++;
                return Border(
                    Text($"Memo(dep: \"{childProp}\") — rendered {count.Current} time(s)").SemiBold()
                ).Padding(8).CornerRadius(4).Background("#e3f2fd");
            }, childProp),

            // 4. Memo() with no deps
            SubHeading("4. Memo() with no deps (render once)"),
            Text("No dependencies = renders once on mount, then only from own state changes."),
            Memo(ctx =>
            {
                var count = ctx.UseRef(0);
                count.Current++;
                var (localCount, setLocal) = ctx.UseState(0);
                return VStack(4,
                    Text($"Memo(no deps) — rendered {count.Current} time(s)").SemiBold(),
                    HStack(8,
                        Button("Self-trigger", () => setLocal(localCount + 1)),
                        Text($"Local state: {localCount}")
                    )
                );
            }),

            // 5. UseCallback
            SubHeading("5. UseCallback stabilizes delegates"),
            Text("Without UseCallback, new Action instances defeat memo on every parent render."),
            Component<RenderCounter, RenderCounterProps>(
                new(childProp, UseCallback(BumpParent, parentRenders)))
        ));
    }

    record RenderCounterProps(string Label, Action? OnClick = null);

    class RenderCounter : Component<RenderCounterProps>
    {
        int _renderCount;

        public override Element Render()
        {
            _renderCount++;
            return Border(
                VStack(4,
                    Text($"Prop=\"{Props.Label}\"  rendered {_renderCount} time(s)").SemiBold(),
                    When(Props.OnClick is not null,
                        () => Button("Invoke callback", Props.OnClick!))
                )
            ).Padding(8).CornerRadius(4).Background(SubtleFill);
        }
    }

    class ProplessCounter : Component
    {
        int _renderCount;

        public override Element Render()
        {
            _renderCount++;
            return Border(
                Text($"ProplessCounter — rendered {_renderCount} time(s)").SemiBold()
            ).Padding(8).CornerRadius(4).Background("#fff3e0");
        }
    }
}

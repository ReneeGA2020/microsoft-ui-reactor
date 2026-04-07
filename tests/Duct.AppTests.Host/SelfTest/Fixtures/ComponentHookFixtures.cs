using Duct;
using Duct.Core;
using Duct.AppTests.Host.SelfTest;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;

namespace Duct.AppTests.Host.SelfTest.Fixtures;

/// <summary>
/// Tests for Component base class hooks: UseReducer (both variants), UseMemo,
/// UseCallback, UseRef, UseEffect with cleanup, UseWindowSize, and Component&lt;TProps&gt;.
/// These target the Component.cs wrapper methods and RenderContext.cs hook implementations.
/// </summary>
internal static class ComponentHookFixtures
{
    // ════════════════════════════════════════════════════════════════════
    //  UseReducer (functional variant)
    // ════════════════════════════════════════════════════════════════════

    private class ReducerComponent : Component
    {
        public override Element Render()
        {
            var (count, update) = UseReducer(0);
            return VStack(
                Text($"Reducer: {count}"),
                Button("Inc", () => update(c => c + 1)),
                Button("Dec", () => update(c => c - 1))
            );
        }
    }

    internal class UseReducerFunctional(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(new ReducerComponent());

            await Harness.Render();
            H.Check("Reducer_Initial", H.FindText("Reducer: 0") is not null);

            H.ClickButton("Inc");
            await Harness.Render();
            H.Check("Reducer_Incremented", H.FindText("Reducer: 1") is not null);

            H.ClickButton("Dec");
            await Harness.Render();
            H.Check("Reducer_Decremented", H.FindText("Reducer: 0") is not null);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  UseReducer (Redux-style with dispatch)
    // ════════════════════════════════════════════════════════════════════

    private record CounterAction(string Type);

    private class ReduxReducerComponent : Component
    {
        public override Element Render()
        {
            var (state, dispatch) = UseReducer(
                (int s, CounterAction a) => a.Type switch
                {
                    "add5" => s + 5,
                    "reset" => 0,
                    _ => s
                },
                10);

            return VStack(
                Text($"Redux: {state}"),
                Button("Add5", () => dispatch(new CounterAction("add5"))),
                Button("Reset", () => dispatch(new CounterAction("reset")))
            );
        }
    }

    internal class UseReducerRedux(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(new ReduxReducerComponent());

            await Harness.Render();
            H.Check("Redux_Initial", H.FindText("Redux: 10") is not null);

            H.ClickButton("Add5");
            await Harness.Render();
            H.Check("Redux_Added", H.FindText("Redux: 15") is not null);

            H.ClickButton("Reset");
            await Harness.Render();
            H.Check("Redux_Reset", H.FindText("Redux: 0") is not null);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  UseMemo + UseCallback + UseRef
    // ════════════════════════════════════════════════════════════════════

    private class MemoRefComponent : Component
    {
        public static int MemoComputeCount;
        public static int CallbackCallCount;

        public override Element Render()
        {
            var (count, setCount) = UseState(0);

            var expensive = UseMemo(() =>
            {
                MemoComputeCount++;
                return $"Computed: {count * 10}";
            }, count);

            var stableCallback = UseCallback(() =>
            {
                CallbackCallCount++;
                setCount(count + 1);
            }, count);

            var renderCount = UseRef(0);
            renderCount.Current++;

            return VStack(
                Text(expensive),
                Text($"Renders: {renderCount.Current}"),
                Button("MemoInc", stableCallback)
            );
        }
    }

    internal class UseMemoCallbackRef(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            MemoRefComponent.MemoComputeCount = 0;
            MemoRefComponent.CallbackCallCount = 0;

            var host = new DuctHost(H.Window);
            host.Mount(new MemoRefComponent());

            await Harness.Render();
            H.Check("Memo_Initial", H.FindText("Computed: 0") is not null);
            H.Check("Memo_ComputedOnce", MemoRefComponent.MemoComputeCount == 1);
            H.Check("Ref_FirstRender", H.FindText("Renders: 1") is not null);

            H.ClickButton("MemoInc");
            await Harness.Render();
            H.Check("Memo_Recomputed", H.FindText("Computed: 10") is not null);
            H.Check("Callback_Called", MemoRefComponent.CallbackCallCount == 1);
            H.Check("Ref_SecondRender", H.FindText("Renders: 2") is not null);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  UseEffect with dependencies (re-run on change)
    // ════════════════════════════════════════════════════════════════════

    private class EffectDepsComponent : Component
    {
        public static int EffectRunCount;
        public static int CleanupRunCount;

        public override Element Render()
        {
            var (value, setValue) = UseState("A");

            UseEffect(() =>
            {
                EffectRunCount++;
                return () => { CleanupRunCount++; };
            }, value);

            return VStack(
                Text($"Val: {value}"),
                Text($"Effects: {EffectRunCount}"),
                Button("SetB", () => setValue("B")),
                Button("SetA", () => setValue("A"))
            );
        }
    }

    internal class UseEffectWithDeps(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            EffectDepsComponent.EffectRunCount = 0;
            EffectDepsComponent.CleanupRunCount = 0;

            var host = new DuctHost(H.Window);
            host.Mount(new EffectDepsComponent());

            await Harness.Render();
            H.Check("EffectDeps_InitialRun", EffectDepsComponent.EffectRunCount == 1);

            H.ClickButton("SetB");
            await Harness.Render();
            H.Check("EffectDeps_RerunOnChange", EffectDepsComponent.EffectRunCount == 2);
            H.Check("EffectDeps_CleanupRan", EffectDepsComponent.CleanupRunCount == 1);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Component<TProps> with typed props
    // ════════════════════════════════════════════════════════════════════

    private record GreetingProps(string Name, int Count);

    private class GreetingComponent : Component<GreetingProps>
    {
        public override Element Render()
        {
            return Text($"Hello {Props.Name} x{Props.Count}");
        }
    }

    internal class TypedPropsComponent(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
            {
                var (count, setCount) = ctx.UseState(1);
                return VStack(
                    Button("IncProps", () => setCount(count + 1)),
                    Component<GreetingComponent, GreetingProps>(new GreetingProps("World", count))
                );
            });

            await Harness.Render();
            H.Check("Props_Initial", H.FindText("Hello World x1") is not null);

            H.ClickButton("IncProps");
            await Harness.Render();
            H.Check("Props_Updated", H.FindText("Hello World x2") is not null);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  UseWindowSize hook
    // ════════════════════════════════════════════════════════════════════

    internal class UseWindowSizeHook(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
            {
                var (w, h2) = ctx.UseWindowSize(H.Window);
                return Text($"Window: {w:F0}x{h2:F0}");
            });

            await Harness.Render();
            var text = H.FindTextContaining("Window:");
            H.Check("WindowSize_Present", text is not null);
            // Window should have non-zero dimensions
            H.Check("WindowSize_NonZero",
                text is not null && !text.Text.Contains("0x0"));
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  UseBreakpoint hook
    // ════════════════════════════════════════════════════════════════════

    internal class UseBreakpointHook(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
            {
                var isWide = ctx.UseBreakpoint(H.Window, 100); // 100px should always be true
                var isHuge = ctx.UseBreakpoint(H.Window, 100000); // 100000px should always be false
                return VStack(
                    Text(isWide ? "Wide: true" : "Wide: false"),
                    Text(isHuge ? "Huge: true" : "Huge: false")
                );
            });

            await Harness.Render();
            H.Check("Breakpoint_Wide", H.FindText("Wide: true") is not null);
            H.Check("Breakpoint_NotHuge", H.FindText("Huge: false") is not null);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Multiple components in tree (tests component node tracking)
    // ════════════════════════════════════════════════════════════════════

    private class CounterA : Component
    {
        public override Element Render()
        {
            var (n, set) = UseState(0);
            return VStack(
                Text($"CounterA: {n}"),
                Button("IncA", () => set(n + 1))
            );
        }
    }

    private class CounterB : Component
    {
        public override Element Render()
        {
            var (n, set) = UseState(100);
            return VStack(
                Text($"CounterB: {n}"),
                Button("IncB", () => set(n + 1))
            );
        }
    }

    internal class MultipleComponents(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx => VStack(
                Component<CounterA>(),
                Component<CounterB>()
            ));

            await Harness.Render();
            H.Check("Multi_AInitial", H.FindText("CounterA: 0") is not null);
            H.Check("Multi_BInitial", H.FindText("CounterB: 100") is not null);

            H.ClickButton("IncA");
            await Harness.Render();
            H.Check("Multi_AIncremented", H.FindText("CounterA: 1") is not null);
            H.Check("Multi_BUnchanged", H.FindText("CounterB: 100") is not null);

            H.ClickButton("IncB");
            await Harness.Render();
            H.Check("Multi_BIncremented", H.FindText("CounterB: 101") is not null);
        }
    }
}

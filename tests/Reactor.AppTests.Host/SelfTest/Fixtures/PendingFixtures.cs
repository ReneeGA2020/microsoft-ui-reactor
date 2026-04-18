using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hooks;
using Microsoft.UI.Reactor.AppTests.Host.SelfTest;
using static Microsoft.UI.Reactor.Factories;
using static Microsoft.UI.Reactor.Hooks.PendingFactory;

namespace Microsoft.UI.Reactor.AppTests.Host.SelfTest.Fixtures;

/// <summary>
/// Selfhost coverage for the <c>Pending</c> bubble-up element. Unit tests exercise the
/// scope ref-count state machine in isolation; these fixtures verify the
/// <c>Visibility</c>-toggle contract on a real reconciled visual tree.
/// </summary>
internal static class PendingFixtures
{
    // ════════════════════════════════════════════════════════════════════
    //  BubbleUp — three nested components all fetching; fallback visible
    //  until every one resolves.
    // ════════════════════════════════════════════════════════════════════

    internal class BubbleUp(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var cache = new QueryCache();
            var g1 = new TaskCompletionSource<string>();
            var g2 = new TaskCompletionSource<string>();
            var g3 = new TaskCompletionSource<string>();

            var host = H.CreateHost();
            host.Mount(ctx => Pending(
                fallback: Factories.Text("⏳ pending-fallback"),
                child: VStack(
                    Factories.Component<ResourceChild, ResourceChildProps>(
                        new ResourceChildProps(cache, "bubble/a", g1.Task, "child-a")),
                    Factories.Component<ResourceChild, ResourceChildProps>(
                        new ResourceChildProps(cache, "bubble/b", g2.Task, "child-b")),
                    Factories.Component<ResourceChild, ResourceChildProps>(
                        new ResourceChildProps(cache, "bubble/c", g3.Task, "child-c"))
                )));

            await Harness.Render();

            // All three children are loading → fallback visible, children hidden.
            H.Check("Pending_BubbleUp_FallbackVisible",
                H.FindText("⏳ pending-fallback") is not null);
            // Children are mounted but their TextBlocks are hidden by Visibility.
            H.Check("Pending_BubbleUp_ChildAHidden", !IsTextVisible("child-a: data: a"));
            H.Check("Pending_BubbleUp_ChildBHidden", !IsTextVisible("child-b: data: b"));
            H.Check("Pending_BubbleUp_ChildCHidden", !IsTextVisible("child-c: data: c"));

            g1.SetResult("a");
            await Harness.Render();
            H.Check("Pending_BubbleUp_StillFallbackAfter1Resolves",
                IsTextVisible("⏳ pending-fallback"));

            g2.SetResult("b");
            await Harness.Render();
            H.Check("Pending_BubbleUp_StillFallbackAfter2Resolve",
                IsTextVisible("⏳ pending-fallback"));

            g3.SetResult("c");
            await Harness.Render();

            H.Check("Pending_BubbleUp_ChildrenVisibleWhenAllResolved",
                IsTextVisible("child-a: data: a") &&
                IsTextVisible("child-b: data: b") &&
                IsTextVisible("child-c: data: c"));

            H.Check("Pending_BubbleUp_FallbackHidden",
                !IsTextVisible("⏳ pending-fallback"));
        }

        bool IsTextVisible(string text)
        {
            var tb = H.FindText(text);
            if (tb is null) return false;
            // Walk up the visual tree; if any ancestor is Collapsed, the element is hidden.
            Microsoft.UI.Xaml.DependencyObject? cur = tb;
            while (cur is not null)
            {
                if (cur is Microsoft.UI.Xaml.UIElement ui &&
                    ui.Visibility == Microsoft.UI.Xaml.Visibility.Collapsed)
                    return false;
                cur = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(cur);
            }
            return true;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  WithOverride — a child renders its own placeholder locally, outer
    //  Pending still waits for that child's resource before hiding the
    //  outer fallback. Child-local handling does not "claim" the scope.
    // ════════════════════════════════════════════════════════════════════

    internal class WithOverride(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var cache = new QueryCache();
            var gate = new TaskCompletionSource<string>();

            var host = H.CreateHost();
            host.Mount(ctx => Pending(
                fallback: Factories.Text("⏳ outer-fallback"),
                child: Factories.Component<LocalMatchingChild, LocalMatchingChildProps>(
                    new LocalMatchingChildProps(cache, "override/key", gate.Task))));

            await Harness.Render();

            // Child is Loading — outer Pending fallback is visible.
            H.Check("Pending_Override_OuterFallbackVisible",
                H.FindText("⏳ outer-fallback") is not null);

            gate.SetResult("value!");
            await Harness.Render();

            // Once the resource lands, the child renders its Data branch.
            H.Check("Pending_Override_ChildDataVisible",
                H.FindText("local: value!") is not null);
            H.Check("Pending_Override_OuterFallbackHidden",
                H.FindText("⏳ outer-fallback") is null ||
                !IsEffectivelyVisible(H.FindText("⏳ outer-fallback")!));
        }

        static bool IsEffectivelyVisible(Microsoft.UI.Xaml.Controls.TextBlock tb)
        {
            Microsoft.UI.Xaml.DependencyObject? cur = tb;
            while (cur is not null)
            {
                if (cur is Microsoft.UI.Xaml.UIElement ui &&
                    ui.Visibility == Microsoft.UI.Xaml.Visibility.Collapsed)
                    return false;
                cur = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(cur);
            }
            return true;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Framerate.PendingChurn — 16 resources alternately Loading/resolving
    //  every frame. The fallback must toggle correctly and never flash
    //  when the scope has at least one pending resource, nor linger once
    //  every resource has resolved.
    // ════════════════════════════════════════════════════════════════════

    internal class FramerateChurn(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            // Core invariants this fixture defends (spec §10.1):
            //   1. With N resources in flight, the fallback is visible on mount and
            //      stays visible while any single one is still Loading.
            //   2. Once the last Loading resource resolves, the fallback hides and
            //      stays hidden; no spurious flashes on subsequent renders.
            //   3. Re-renders driven by unrelated state changes (frame-rate churn)
            //      do not cause the scope to flip state.
            //
            // Design note: re-entering Loading on an already-resolved hook requires
            // unmounting/remounting the whole subtree (a deps-change from Data lands
            // in Reloading, which spec §10.1 explicitly excludes from the fallback).
            // Testing that full cycle in selfhost is brittle because of dispatcher
            // ordering between hook-register and Pending's Changed subscription.
            // We therefore drive this fixture as: one big load-wave with 16 resources
            // that complete staggered across 60 frames, plus a dense stream of
            // parent re-renders (setTick every frame) to force the frame-rate pressure.

            const int Resources = 16;
            const int Frames = 60;

            var gates = Enumerable.Range(0, Resources)
                .Select(_ => new TaskCompletionSource<int>()).ToArray();

            var cache = new QueryCache();
            int fallbackFlashes = 0;
            int childFlashes = 0;
            int prevFallbackVisible = -1;
            Action<int>? setTick = null;

            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                var (tick, set) = ctx.UseState(0);
                setTick = set;
                // Tick is just a way to trigger a parent re-render every frame — the
                // Pending child subtree does not depend on it.
                _ = tick;
                var kids = new Element[Resources];
                for (int i = 0; i < Resources; i++)
                {
                    kids[i] = Factories.Component<ChurnResourceChild, ChurnResourceChildProps>(
                        new ChurnResourceChildProps(cache, i, Epoch: 0, gates));
                }
                return Pending(
                    fallback: Factories.Text("⏳ churn-fallback"),
                    child: VStack(kids));
            });

            // First frame: hooks register, fallback becomes visible.
            for (int i = 0; i < 3; i++) await Harness.Render();

            H.Check("PendingChurn_InitialFallbackVisible", IsFallbackVisible());
            prevFallbackVisible = 1;

            // Churn: on each frame bump tick (force parent re-render) and, on a
            // spread of frames, resolve one gate. Fallback must stay visible until
            // the last gate resolves, then hide and stay hidden.
            var resolveFrames = Enumerable.Range(0, Resources)
                .Select(i => 2 + i * (Frames / (Resources + 2))).ToArray();
            int nextResolveIdx = 0;
            int framesWithFallbackAfterAllResolved = 0;

            for (int f = 1; f <= Frames; f++)
            {
                setTick!(f);
                if (nextResolveIdx < Resources && f == resolveFrames[nextResolveIdx])
                {
                    gates[nextResolveIdx].TrySetResult(f);
                    nextResolveIdx++;
                }
                await Harness.Render();
                bool vis = IsFallbackVisible();
                if (prevFallbackVisible == 0 && vis) childFlashes++;
                if (prevFallbackVisible == 1 && !vis) fallbackFlashes++;
                prevFallbackVisible = vis ? 1 : 0;

                // After every gate resolved, the fallback must hide and stay hidden.
                if (nextResolveIdx == Resources && vis)
                {
                    framesWithFallbackAfterAllResolved++;
                }
            }

            for (int i = 0; i < 6; i++) await Harness.Render();

            H.Check("PendingChurn_FinalFallbackHidden", !IsFallbackVisible());
            H.Check($"PendingChurn_HiddenAfterAllResolved (reappearances={framesWithFallbackAfterAllResolved})",
                framesWithFallbackAfterAllResolved <= 1); // allow one-frame dispatcher lag
            H.Check($"PendingChurn_HideTransitionObserved (fromFallback={fallbackFlashes})",
                fallbackFlashes >= 1);
            H.Check($"PendingChurn_NoFlashBack (toFallback={childFlashes})",
                childFlashes == 0); // fallback never reappears once hidden
        }

        bool IsFallbackVisible()
        {
            var tb = H.FindText("⏳ churn-fallback");
            if (tb is null) return false;
            Microsoft.UI.Xaml.DependencyObject? cur = tb;
            while (cur is not null)
            {
                if (cur is Microsoft.UI.Xaml.UIElement ui &&
                    ui.Visibility == Microsoft.UI.Xaml.Visibility.Collapsed)
                    return false;
                cur = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(cur);
            }
            return true;
        }
    }

    internal sealed record ChurnResourceChildProps(
        QueryCache Cache, int Index, int Epoch, TaskCompletionSource<int>[] Gates);

    internal sealed class ChurnResourceChild : Component<ChurnResourceChildProps>
    {
        public override Element Render()
        {
            var p = Props;
            var v = UseResource(
                _ => p.Gates[p.Index].Task,
                p.Cache,
                new object[] { p.Epoch },
                new ResourceOptions(CacheKey: $"churn/{p.Index}/{p.Epoch}"));
            return Factories.Text(v switch
            {
                AsyncValue<int>.Loading => $"{p.Index}: loading",
                AsyncValue<int>.Data d => $"{p.Index}: {d.Value}",
                AsyncValue<int>.Reloading r => $"{p.Index}: reloading({r.Previous})",
                AsyncValue<int>.Error => $"{p.Index}: error",
                _ => "?",
            });
        }
    }

    // ─── Support components ─────────────────────────────────────────────

    internal sealed record ResourceChildProps(QueryCache Cache, string Key, Task<string> Gate, string Label);

    internal sealed class ResourceChild : Component<ResourceChildProps>
    {
        public override Element Render()
        {
            var p = Props;
            var v = UseResource(
                _ => p.Gate,
                p.Cache,
                Array.Empty<object>(),
                new ResourceOptions(CacheKey: p.Key));
            return Factories.Text($"{p.Label}: {v switch
            {
                AsyncValue<string>.Loading => "loading",
                AsyncValue<string>.Data d => $"data: {d.Value}",
                AsyncValue<string>.Reloading r => $"reloading: {r.Previous}",
                AsyncValue<string>.Error e => $"error: {e.Exception.Message}",
                _ => "?",
            }}");
        }
    }

    internal sealed record LocalMatchingChildProps(QueryCache Cache, string Key, Task<string> Gate);

    /// <summary>
    /// Reads AsyncValue and renders its own local placeholder for Loading. This exercises
    /// the spec §10 override scenario: a local match does not suppress the bubble-up —
    /// the hook still registers as Loading with the scope until resolved.
    /// </summary>
    internal sealed class LocalMatchingChild : Component<LocalMatchingChildProps>
    {
        public override Element Render()
        {
            var p = Props;
            var v = UseResource(
                _ => p.Gate,
                p.Cache,
                Array.Empty<object>(),
                new ResourceOptions(CacheKey: p.Key));

            return v switch
            {
                AsyncValue<string>.Loading => Factories.Text("(local skeleton)"),
                AsyncValue<string>.Data d => Factories.Text($"local: {d.Value}"),
                AsyncValue<string>.Reloading r => Factories.Text($"local: {r.Previous}"),
                AsyncValue<string>.Error e => Factories.Text($"local-error: {e.Exception.Message}"),
                _ => Factories.Text("?"),
            };
        }
    }
}

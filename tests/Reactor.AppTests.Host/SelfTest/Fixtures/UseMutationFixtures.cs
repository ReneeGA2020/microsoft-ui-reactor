using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hooks;
using Microsoft.UI.Reactor.AppTests.Host.SelfTest;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.AppTests.Host.SelfTest.Fixtures;

/// <summary>
/// Selfhost coverage for <c>UseMutation</c> under a real dispatcher. Unit tests already
/// cover the state-machine; these fixtures verify the callback ordering, optimistic
/// render timing, and cache-invalidation handoff to sibling <c>UseResource</c> hooks
/// when marshalled through the real <c>DispatcherQueue</c>.
/// </summary>
internal static class UseMutationFixtures
{
    // ════════════════════════════════════════════════════════════════════
    //  Optimistic — UI reflects optimistic value before server responds
    // ════════════════════════════════════════════════════════════════════

    internal class Optimistic(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var gate = new TaskCompletionSource<int>();

            Action? runMutation = null;

            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                var (localCount, setLocal) = ctx.UseState(0);

                var mutation = ctx.UseMutation<int, int>(
                    mutator: async (delta, ct) =>
                    {
                        var server = await gate.Task;
                        ct.ThrowIfCancellationRequested();
                        return server;
                    },
                    cache: null,
                    options: new MutationOptions<int, int>(
                        OnOptimistic: delta => setLocal(localCount + delta),
                        OnSuccess: (server, delta) => setLocal(server)));

                runMutation = () => _ = mutation.RunAsync(1);

                return Factories.Text($"count: {localCount} pending: {mutation.IsPending}");
            });

            await Harness.Render();
            H.Check("UseMutation_Optimistic_InitialZero",
                H.FindTextContaining("count: 0 pending: False") is not null);

            runMutation!();
            await Harness.Render();

            // Optimistic path ran synchronously — UI shows 1 while the mutation is still pending.
            H.Check("UseMutation_Optimistic_Applied",
                H.FindTextContaining("count: 1 pending: True") is not null);

            gate.SetResult(42);
            await Harness.Render();

            // After resolution, OnSuccess overrode the optimistic value.
            H.Check("UseMutation_Optimistic_ServerValueAfterResolve",
                H.FindTextContaining("count: 42 pending: False") is not null);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Rollback — OnError restores pre-optimistic state
    // ════════════════════════════════════════════════════════════════════

    internal class Rollback(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var gate = new TaskCompletionSource<int>();

            Action? runMutation = null;

            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                var (count, setCount) = ctx.UseState(0);
                var snapshotRef = ctx.UseRef<int>(0);

                var mutation = ctx.UseMutation<int, int>(
                    mutator: async (delta, ct) =>
                    {
                        var v = await gate.Task;
                        ct.ThrowIfCancellationRequested();
                        return v;
                    },
                    cache: null,
                    options: new MutationOptions<int, int>(
                        OnOptimistic: delta =>
                        {
                            snapshotRef.Current = count;
                            setCount(count + delta);
                        },
                        OnError: (_, _) => setCount(snapshotRef.Current)));

                runMutation = () => _ = mutation.RunAsync(1);

                return Factories.Text(
                    $"count: {count} error: {mutation.Error?.Message ?? "(none)"}");
            });

            await Harness.Render();
            H.Check("UseMutation_Rollback_Initial",
                H.FindTextContaining("count: 0 error: (none)") is not null);

            runMutation!();
            await Harness.Render();
            H.Check("UseMutation_Rollback_Optimistic",
                H.FindTextContaining("count: 1 error: (none)") is not null);

            gate.SetException(new InvalidOperationException("server said no"));
            await Harness.Render();

            H.Check("UseMutation_Rollback_RolledBack",
                H.FindTextContaining("count: 0 error: server said no") is not null);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Invalidates — OnSuccess invalidates a key; a sibling UseResource
    //  subscribed to that key re-renders with fresh data.
    // ════════════════════════════════════════════════════════════════════

    internal class Invalidates(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var cache = new QueryCache();
            ReaderChild.ResetCounter();

            Action? runMutation = null;

            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                // Sibling reader subscribes to "invalidate/target". Reader owns its own
                // fetch counter so Props stay ref-equal across renders — otherwise
                // Component<T>'s default ShouldUpdate would skip the re-render path
                // we're trying to exercise. Identity of the cache *is* the signal.
                var reader = Factories.Component<ReaderChild, QueryCache>(cache);

                // Mutation that invalidates the reader's key on success.
                var mutation = ctx.UseMutation<int, int>(
                    mutator: (x, _) => Task.FromResult(x * 2),
                    cache: cache,
                    options: new MutationOptions<int, int>(
                        InvalidateKeys: new[] { "invalidate/target" }));

                runMutation = () => _ = mutation.RunAsync(5);

                return VStack(reader, Factories.Text($"mutated: {mutation.LastResult}"));
            });

            // Let the reader's initial fetch land.
            await Harness.Render();
            await Harness.Render();

            int initialInvocations = ReaderChild.FetchCount;
            H.Check($"UseMutation_Invalidates_InitialFetch (got {initialInvocations})",
                initialInvocations == 1);
            H.Check("UseMutation_Invalidates_ReaderDataVisible",
                H.FindTextContaining("reader: fetch #1") is not null);

            runMutation!();
            // Mutation resolves synchronously (Task.FromResult) — give the dispatcher
            // a couple of frames to apply the invalidation and refetch.
            await Harness.Render();
            await Harness.Render();
            await Harness.Render();

            // The cache entry should have been invalidated and re-populated by the refetch.
            // Verify both the refetch count and the visible text reflect the new value.
            H.Check($"UseMutation_Invalidates_RefetchHappened (got {ReaderChild.FetchCount})",
                ReaderChild.FetchCount >= 2);
            H.Check("UseMutation_Invalidates_MutationResultVisible",
                H.FindTextContaining("mutated: 10") is not null);
            H.Check("UseMutation_Invalidates_ReaderRefreshed",
                H.FindTextContaining($"reader: fetch #{ReaderChild.FetchCount}") is not null);
        }
    }

    // ─── Reader child used by Invalidates ───────────────────────────────

    internal sealed class ReaderChild : Component<QueryCache>
    {
        static int _counter;
        public static int FetchCount => Volatile.Read(ref _counter);
        public static void ResetCounter() => Volatile.Write(ref _counter, 0);

        public override Element Render()
        {
            var cache = Props;
            var v = UseResource(
                _ => Task.FromResult($"fetch #{Interlocked.Increment(ref _counter)}"),
                cache,
                Array.Empty<object>(),
                new ResourceOptions(CacheKey: "invalidate/target"));
            return Factories.Text($"reader: {v switch
            {
                AsyncValue<string>.Loading => "loading",
                AsyncValue<string>.Data d => d.Value,
                AsyncValue<string>.Reloading r => r.Previous,
                AsyncValue<string>.Error e => $"error: {e.Exception.Message}",
                _ => "?",
            }}");
        }

        // A mutation-triggered cache invalidation should still cause a refetch even
        // when Props are reference-equal — force the reconciler to rerun Render().
        protected override bool ShouldUpdate(QueryCache? oldProps, QueryCache? newProps) => true;
    }
}

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hooks;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Reactor.Controls;
using static Microsoft.UI.Reactor.Factories;
using static Microsoft.UI.Reactor.Core.Theme;

// ═══════════════════════════════════════════════════════════════════════
//  AsyncValueSamples — exercises every UseResource state machine path.
//  Each scenario is a self-contained class component so the hook slot order
//  and deps lifetime are isolated. The shared QueryCache for scenarios 1d/1e
//  is parented via AppContexts.QueryCache in the hosting code.
// ═══════════════════════════════════════════════════════════════════════

enum AsyncValueScenario
{
    DeterministicFetcher,
    SyncComplete,
    DepsChangeCancel,
    SiblingsSharedKey,
    CacheHitAcrossRemount,
}

class AsyncValueSamplesDemo : Component
{
    static readonly (AsyncValueScenario Key, string Label)[] Scenarios =
    [
        (AsyncValueScenario.DeterministicFetcher, "1a. Deterministic fetcher (succeed / fail / cancel)"),
        (AsyncValueScenario.SyncComplete, "1b. Sync-complete fetcher (no Loading flash)"),
        (AsyncValueScenario.DepsChangeCancel, "1c. Deps-change cancellation (text input drives deps)"),
        (AsyncValueScenario.SiblingsSharedKey, "1d. Two siblings, one explicit CacheKey"),
        (AsyncValueScenario.CacheHitAcrossRemount, "1e. Cache hit across remount"),
    ];

    public override Element Render()
    {
        var (scenario, setScenario) = UseState(AsyncValueScenario.DeterministicFetcher);

        return ScrollView(VStack(12,
            Heading("UseResource scenarios"),
            Factories.Text("Every arm of the AsyncValue<T> state machine exercised under a real dispatcher."),

            ComboBox(
                Scenarios.Select(s => Factories.Text(s.Label) as Element).ToArray(),
                Array.IndexOf(Scenarios, Scenarios.First(s => s.Key == scenario)),
                i => setScenario(Scenarios[i].Key)
            ).Width(460),

            Border(
                scenario switch
                {
                    AsyncValueScenario.DeterministicFetcher => Component<DeterministicFetcherScenario>(),
                    AsyncValueScenario.SyncComplete => Component<SyncCompleteScenario>(),
                    AsyncValueScenario.DepsChangeCancel => Component<DepsChangeCancelScenario>(),
                    AsyncValueScenario.SiblingsSharedKey => Component<SiblingsSharedKeyScenario>(),
                    AsyncValueScenario.CacheHitAcrossRemount => Component<CacheHitRemountScenario>(),
                    _ => Factories.Text("Select a scenario"),
                }
            ).Padding(16).CornerRadius(8).Background(SubtleFill).Margin(0, 8)
        ));
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  1a — Deterministic fetcher: succeed, fail, cancel
// ═══════════════════════════════════════════════════════════════════════

class DeterministicFetcherScenario : Component
{
    enum Mode { Succeed, Fail, Slow }

    public override Element Render()
    {
        var (mode, setMode) = UseState(Mode.Succeed);
        var (runId, setRunId) = UseState(0);

        var result = UseResource(async ct =>
        {
            await Task.Delay(500, ct);
            return mode switch
            {
                Mode.Succeed => $"ok (run {runId})",
                Mode.Slow => $"slow ok (run {runId})",
                Mode.Fail => throw new InvalidOperationException($"boom (run {runId})"),
                _ => "",
            };
        }, new object[] { mode, runId });

        return VStack(8,
            SubHeading("1a. Deterministic fetcher"),
            HStack(8,
                Button("Succeed", () => { setMode(Mode.Succeed); setRunId(runId + 1); }),
                Button("Fail", () => { setMode(Mode.Fail); setRunId(runId + 1); }),
                Button("Slow", () => { setMode(Mode.Slow); setRunId(runId + 1); })
            ),
            Factories.Text(DescribeValue(result)).SemiBold()
        );
    }

    static string DescribeValue(AsyncValue<string> v) => v switch
    {
        AsyncValue<string>.Loading => "⏳ Loading…",
        AsyncValue<string>.Data d => $"✅ Data: {d.Value}",
        AsyncValue<string>.Reloading r => $"♻️ Reloading (prev: {r.Previous})",
        AsyncValue<string>.Error e => $"❌ Error: {e.Exception.Message}",
        _ => "?",
    };
}

// ═══════════════════════════════════════════════════════════════════════
//  1b — Sync-complete fetcher: Task.FromResult → no Loading flash
// ═══════════════════════════════════════════════════════════════════════

class SyncCompleteScenario : Component
{
    public override Element Render()
    {
        var result = UseResource(
            _ => Task.FromResult("from Task.FromResult (resolved before hook returns)"),
            Array.Empty<object>());

        return VStack(8,
            SubHeading("1b. Sync-complete fetcher"),
            Factories.Text("A fetcher returning an already-completed task skips the Loading state entirely."),
            Factories.Text(result switch
            {
                AsyncValue<string>.Loading => "⏳ Loading… (should not appear!)",
                AsyncValue<string>.Data d => $"✅ {d.Value}",
                AsyncValue<string>.Error e => $"❌ {e.Exception.Message}",
                AsyncValue<string>.Reloading r => $"♻️ {r.Previous}",
                _ => "?",
            }).SemiBold()
        );
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  1c — Deps-change cancellation: text input drives deps
// ═══════════════════════════════════════════════════════════════════════

class DepsChangeCancelScenario : Component
{
    public override Element Render()
    {
        var (query, setQuery) = UseState("");

        var result = UseResource(async ct =>
        {
            await Task.Delay(400, ct);
            // If we reach here without cancellation, return a digest of the query.
            return string.IsNullOrEmpty(query) ? "<empty>" : $"processed: {query.ToUpperInvariant()}";
        }, new object[] { query });

        return VStack(8,
            SubHeading("1c. Deps-change cancellation"),
            Factories.Text("Each keystroke cancels the previous fetch and starts a new one — only the last lands."),
            TextField(query, v => setQuery(v ?? ""), placeholder: "type here…"),
            Factories.Text(result switch
            {
                AsyncValue<string>.Loading => "⏳ Loading…",
                AsyncValue<string>.Data d => $"✅ {d.Value}",
                AsyncValue<string>.Reloading r => $"♻️ Reloading (prev: {r.Previous})",
                AsyncValue<string>.Error e => $"❌ {e.Exception.Message}",
                _ => "?",
            }).SemiBold()
        );
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  1d — Two siblings, one explicit CacheKey → share a single fetch
// ═══════════════════════════════════════════════════════════════════════

class SiblingsSharedKeyScenario : Component
{
    static int _sharedCallCount;

    public override Element Render()
    {
        return VStack(8,
            SubHeading("1d. Siblings share cache via explicit CacheKey"),
            Factories.Text($"Total fetcher invocations across both siblings: {_sharedCallCount}"),
            Component<SharedKeySibling>(),
            Component<SharedKeySibling>()
        );
    }

    class SharedKeySibling : Component
    {
        public override Element Render()
        {
            var result = UseResource(async _ =>
            {
                Interlocked.Increment(ref _sharedCallCount);
                await Task.Delay(250);
                return $"shared payload (invocation {_sharedCallCount})";
            },
            Array.Empty<object>(),
            new ResourceOptions(CacheKey: "demo/shared", StaleTime: TimeSpan.FromMinutes(5)));

            return Border(
                Factories.Text(result switch
                {
                    AsyncValue<string>.Loading => "⏳ Loading…",
                    AsyncValue<string>.Data d => $"✅ {d.Value}",
                    AsyncValue<string>.Reloading r => $"♻️ {r.Previous}",
                    AsyncValue<string>.Error e => $"❌ {e.Exception.Message}",
                    _ => "?",
                })
            ).Padding(8).CornerRadius(4).Background(SubtleFill);
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  1e — Cache hit across remount: toggle visibility within StaleTime
// ═══════════════════════════════════════════════════════════════════════

class CacheHitRemountScenario : Component
{
    public override Element Render()
    {
        var (visible, setVisible) = UseState(true);

        return VStack(8,
            SubHeading("1e. Cache hit across remount"),
            Factories.Text("Hide the fetcher component, then show it again within StaleTime — the cached value returns synchronously with no Loading flash."),
            Button(visible ? "Hide" : "Show", () => setVisible(!visible)),
            visible
                ? (Element)Component<RemountChild>()
                : Factories.Text("(hidden)").Foreground(TertiaryText)
        );
    }

    class RemountChild : Component
    {
        public override Element Render()
        {
            var result = UseResource(async _ =>
            {
                await Task.Delay(600);
                return $"fetched at {DateTime.Now:HH:mm:ss.fff}";
            },
            Array.Empty<object>(),
            new ResourceOptions(
                CacheKey: "demo/remount-key",
                StaleTime: TimeSpan.FromMinutes(1),
                CacheTime: TimeSpan.FromMinutes(5)));

            return Border(
                Factories.Text(result switch
                {
                    AsyncValue<string>.Loading => "⏳ First load — takes 600ms.",
                    AsyncValue<string>.Data d => $"✅ {d.Value} (cached)",
                    AsyncValue<string>.Reloading r => $"♻️ {r.Previous}",
                    AsyncValue<string>.Error e => $"❌ {e.Exception.Message}",
                    _ => "?",
                })
            ).Padding(8).CornerRadius(4).Background(SubtleFill);
        }
    }
}

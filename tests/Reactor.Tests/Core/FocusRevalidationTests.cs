using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hooks;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Core;

/// <summary>
/// Unit tests for <see cref="FocusRevalidationService"/> and its integration with
/// <c>UseResource</c> via <c>ResourceOptions.RefetchOnWindowFocus</c>. See
/// <c>docs/specs/020-async-resources-design.md</c> §15 Q1 for the design rationale.
/// </summary>
public class FocusRevalidationTests
{
    private static (QueryCache cache, FocusRevalidationService service, Func<DateTime> clock) NewService(
        TimeSpan? throttle = null)
    {
        var cache = new QueryCache();
        var now = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        cache.UtcNow = () => now;
        var svc = new FocusRevalidationService(cache);
        svc.UtcNow = () => now;
        if (throttle is { } t) svc.ThrottleWindow = t;
        return (cache, svc, () => now);
    }

    [Fact]
    public void Enroll_Is_Idempotent()
    {
        var (_, svc, _) = NewService();
        svc.Enroll("k");
        svc.Enroll("k");
        Assert.Equal(1, svc.EnrolledCount);
    }

    [Fact]
    public void Unenroll_Removes_Key()
    {
        var (_, svc, _) = NewService();
        svc.Enroll("a");
        svc.Enroll("b");
        svc.Unenroll("a");
        Assert.Equal(1, svc.EnrolledCount);
    }

    [Fact]
    public void RevalidateNow_Invalidates_Stale_Enrolled_Entries()
    {
        var cache = new QueryCache();
        var baseTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        cache.UtcNow = () => baseTime;

        // Seed the cache with two entries that differ in staleness.
        cache.Set("stale", 1, staleTime: TimeSpan.FromSeconds(1), cacheTime: TimeSpan.FromMinutes(5));
        cache.Set("fresh", 2, staleTime: TimeSpan.FromMinutes(5), cacheTime: TimeSpan.FromMinutes(5));

        var svc = new FocusRevalidationService(cache) { ThrottleWindow = TimeSpan.Zero };
        svc.Enroll("stale");
        svc.Enroll("fresh");

        // Advance the clock 10s past the stale entry's StaleTime.
        var later = baseTime + TimeSpan.FromSeconds(10);
        cache.UtcNow = () => later;
        svc.UtcNow = () => later;

        var invalidated = svc.RevalidateNow();

        Assert.Contains("stale", invalidated);
        Assert.DoesNotContain("fresh", invalidated);

        // Entry confirmation: stale was invalidated (TryGet returns false), fresh still there.
        Assert.False(cache.TryGet<int>("stale", out _));
        Assert.True(cache.TryGet<int>("fresh", out _));
    }

    [Fact]
    public void RevalidateNow_Ignores_Non_Enrolled_Keys()
    {
        var cache = new QueryCache();
        var baseTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        cache.UtcNow = () => baseTime;
        cache.Set("not-enrolled", 1, TimeSpan.Zero, TimeSpan.FromMinutes(5));

        var svc = new FocusRevalidationService(cache);
        // Don't enroll — the entry is stale but unknown to the service.

        var later = baseTime + TimeSpan.FromSeconds(30);
        svc.UtcNow = () => later;
        cache.UtcNow = () => later;

        Assert.Empty(svc.RevalidateNow());
        Assert.True(cache.TryGet<int>("not-enrolled", out _));
    }

    [Fact]
    public void RevalidateNow_Throttles_Within_Window()
    {
        var cache = new QueryCache();
        var t = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        cache.UtcNow = () => t;
        cache.Set("k", 1, TimeSpan.Zero, TimeSpan.FromMinutes(5));

        var svc = new FocusRevalidationService(cache) { ThrottleWindow = TimeSpan.FromSeconds(30) };
        svc.UtcNow = () => t;
        svc.Enroll("k");

        // First sweep fires.
        Assert.Single(svc.RevalidateNow());
        // Re-set so the next sweep has something to evaluate.
        cache.Set("k", 2, TimeSpan.Zero, TimeSpan.FromMinutes(5));

        // Within 5s of the first sweep — should be throttled to a no-op.
        t = t.AddSeconds(5);
        svc.UtcNow = () => t;
        cache.UtcNow = () => t;
        Assert.Empty(svc.RevalidateNow());

        // Past the throttle window — fires again.
        t = t.AddSeconds(30);
        svc.UtcNow = () => t;
        cache.UtcNow = () => t;
        Assert.Single(svc.RevalidateNow());
    }

    [Fact]
    public void RevalidateNowForce_Bypasses_Throttle()
    {
        var cache = new QueryCache();
        var t = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        cache.UtcNow = () => t;
        cache.Set("k", 1, TimeSpan.Zero, TimeSpan.FromMinutes(5));

        var svc = new FocusRevalidationService(cache) { ThrottleWindow = TimeSpan.FromHours(1) };
        svc.UtcNow = () => t;
        svc.Enroll("k");

        Assert.Single(svc.RevalidateNow());

        // Throttle would normally block a second sweep — force bypasses it.
        cache.Set("k", 2, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        Assert.Single(svc.RevalidateNowForce());
    }

    // ════════════════════════════════════════════════════════════════════
    //  Integration with UseResource via ResourceOptions.RefetchOnWindowFocus
    // ════════════════════════════════════════════════════════════════════

    private sealed class InlineDispatcher : IHookDispatcher
    {
        public void Post(Action action) => action();
    }

    [Fact]
    public void Hook_Enrolls_When_RefetchOnWindowFocus_True()
    {
        // UseResource picks up the ambient FocusRevalidationService from
        // AppContexts.FocusRevalidation. Its default is bound to the default QueryCache,
        // so this test uses that pair directly.
        var svc = AppContexts.FocusRevalidation.DefaultValue!;
        var cache = AppContexts.QueryCache.DefaultValue;
        int before = svc.EnrolledCount;

        var ctx = new RenderContext();
        ctx.BeginRender(() => { });
        var key = $"focus/enrolled/{Guid.NewGuid():N}";
        ctx.UseResource(_ => Task.FromResult(1), cache, Array.Empty<object>(),
            new ResourceOptions(CacheKey: key, RefetchOnWindowFocus: true),
            new InlineDispatcher());

        Assert.Equal(before + 1, svc.EnrolledCount);
        // Clean up to keep other tests' EnrolledCount stable.
        svc.Unenroll(key);
    }

    [Fact]
    public void Hook_Does_Not_Enroll_When_RefetchOnWindowFocus_False()
    {
        var svc = AppContexts.FocusRevalidation.DefaultValue!;
        var cache = AppContexts.QueryCache.DefaultValue;
        int before = svc.EnrolledCount;

        var ctx = new RenderContext();
        ctx.BeginRender(() => { });
        var key = $"focus/skip/{Guid.NewGuid():N}";
        ctx.UseResource(_ => Task.FromResult(1), cache, Array.Empty<object>(),
            new ResourceOptions(CacheKey: key, RefetchOnWindowFocus: false),
            new InlineDispatcher());

        Assert.Equal(before, svc.EnrolledCount);
    }
}

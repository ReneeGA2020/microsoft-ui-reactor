using Microsoft.UI.Reactor.Core;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Core;

public class QueryCacheTests
{
    private static QueryCache NewCache()
    {
        var cache = new QueryCache();
        // Pin clock so tests are deterministic.
        var t = DateTime.UtcNow;
        cache.UtcNow = () => t;
        return cache;
    }

    // ════════════════════════════════════════════════════════════════
    //  Basic get / set
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Cache_Miss_Returns_False()
    {
        var cache = NewCache();
        Assert.False(cache.TryGet<int>("k", out var _));
    }

    [Fact]
    public void Cache_Set_Then_Get_Returns_Entry()
    {
        using var cache = NewCache();
        cache.Set("k", 42, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));

        Assert.True(cache.TryGet<int>("k", out var entry));
        Assert.Equal(42, entry.Value);
        Assert.Equal(TimeSpan.FromSeconds(30), entry.StaleTime);
    }

    [Fact]
    public void Stale_Entry_Is_Still_Returned_By_TryGet()
    {
        using var cache = NewCache();
        var now = cache.UtcNow();
        cache.Set("k", 42, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));
        cache.UtcNow = () => now + TimeSpan.FromSeconds(60);

        Assert.True(cache.TryGet<int>("k", out var entry));
        // The caller decides stale-vs-fresh via FetchedAt + StaleTime.
        Assert.Equal(42, entry.Value);
    }

    [Fact]
    public void TryGet_Wrong_Type_Returns_False_Without_Throwing()
    {
        using var cache = NewCache();
        cache.Set("k", 42, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        Assert.False(cache.TryGet<string>("k", out _));
    }

    // ════════════════════════════════════════════════════════════════
    //  Invalidation
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Invalidate_Removes_Entry_And_Fires_Event()
    {
        using var cache = NewCache();
        cache.Set("k", 1, TimeSpan.Zero, TimeSpan.FromMinutes(5));

        var fires = new List<string>();
        cache.EntryChanged += fires.Add;
        cache.Invalidate("k");

        Assert.False(cache.TryGet<int>("k", out _));
        Assert.Contains("k", fires);
    }

    [Fact]
    public void Invalidate_Missing_Key_Does_Not_Fire_Event()
    {
        using var cache = NewCache();
        var fires = new List<string>();
        cache.EntryChanged += fires.Add;
        cache.Invalidate("missing");
        Assert.Empty(fires);
    }

    [Fact]
    public void InvalidatePattern_Removes_Prefix_Matches_Only()
    {
        using var cache = NewCache();
        cache.Set("user/1", 1, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        cache.Set("user/2", 2, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        cache.Set("employees/1", 99, TimeSpan.Zero, TimeSpan.FromMinutes(5));

        cache.InvalidatePattern("user/");

        Assert.False(cache.TryGet<int>("user/1", out _));
        Assert.False(cache.TryGet<int>("user/2", out _));
        Assert.True(cache.TryGet<int>("employees/1", out _));
    }

    [Fact]
    public void Clear_Removes_All_And_Fires_Per_Entry()
    {
        using var cache = NewCache();
        cache.Set("a", 1, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        cache.Set("b", 2, TimeSpan.Zero, TimeSpan.FromMinutes(5));

        var fires = new HashSet<string>();
        cache.EntryChanged += k => fires.Add(k);
        cache.Clear();

        Assert.False(cache.TryGet<int>("a", out _));
        Assert.False(cache.TryGet<int>("b", out _));
        Assert.Contains("a", fires);
        Assert.Contains("b", fires);
    }

    // ════════════════════════════════════════════════════════════════
    //  Subscribers
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Subscribe_Unsubscribe_Tracks_Count()
    {
        using var cache = NewCache();
        Assert.Equal(1, cache.Subscribe("k"));
        Assert.Equal(2, cache.Subscribe("k"));
        Assert.Equal(1, cache.Unsubscribe("k"));
        Assert.Equal(0, cache.Unsubscribe("k"));
    }

    [Fact]
    public void Unsubscribe_Below_Zero_Throws()
    {
        using var cache = NewCache();
        cache.Subscribe("k");
        cache.Unsubscribe("k");
        Assert.Throws<InvalidOperationException>(() => cache.Unsubscribe("k"));
    }

    [Fact]
    public void Unsubscribe_Nonexistent_Throws()
    {
        using var cache = NewCache();
        Assert.Throws<InvalidOperationException>(() => cache.Unsubscribe("never-seen"));
    }

    [Fact]
    public void Entry_SubscriberCount_Mirrors_Ref_Count()
    {
        using var cache = NewCache();
        cache.Subscribe("k");
        cache.Subscribe("k");
        cache.Set("k", 42, TimeSpan.Zero, TimeSpan.FromMinutes(5));

        Assert.True(cache.TryGet<int>("k", out var entry));
        Assert.Equal(2, entry.SubscriberCount);

        cache.Subscribe("k");
        Assert.True(cache.TryGet<int>("k", out entry));
        Assert.Equal(3, entry.SubscriberCount);

        cache.Unsubscribe("k");
        cache.Unsubscribe("k");
        Assert.True(cache.TryGet<int>("k", out entry));
        Assert.Equal(1, entry.SubscriberCount);
    }

    // ════════════════════════════════════════════════════════════════
    //  Eviction
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Entry_With_Zero_Subscribers_Past_CacheTime_Is_Evicted()
    {
        using var cache = NewCache();
        var now = cache.UtcNow();
        cache.Subscribe("k");
        cache.Set("k", 42, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        cache.Unsubscribe("k");

        cache.UtcNow = () => now + TimeSpan.FromSeconds(11);
        var evicted = cache.EvictNow();

        Assert.Contains("k", evicted);
        Assert.False(cache.TryGet<int>("k", out _));
    }

    [Fact]
    public void Entry_With_Subscribers_Is_Not_Evicted()
    {
        using var cache = NewCache();
        var now = cache.UtcNow();
        cache.Subscribe("k");
        cache.Set("k", 42, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        // subscriber stays

        cache.UtcNow = () => now + TimeSpan.FromSeconds(1000);
        var evicted = cache.EvictNow();

        Assert.Empty(evicted);
        Assert.True(cache.TryGet<int>("k", out _));
    }

    [Fact]
    public void Resubscribe_Before_CacheTime_Cancels_Eviction()
    {
        using var cache = NewCache();
        var now = cache.UtcNow();
        cache.Subscribe("k");
        cache.Set("k", 42, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        cache.Unsubscribe("k");

        cache.UtcNow = () => now + TimeSpan.FromSeconds(5);
        cache.Subscribe("k");
        cache.UtcNow = () => now + TimeSpan.FromSeconds(20);

        var evicted = cache.EvictNow();
        Assert.Empty(evicted);
        Assert.True(cache.TryGet<int>("k", out _));
    }

    // ════════════════════════════════════════════════════════════════
    //  Dispatcher marshalling
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void EntryChanged_Uses_Dispatcher_Post_When_Set()
    {
        using var cache = NewCache();
        var queued = new List<Action>();
        cache.DispatcherPost = a => queued.Add(a);

        var fires = new List<string>();
        cache.EntryChanged += fires.Add;

        cache.Set("k", 1, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        Assert.Empty(fires); // not fired yet — queued
        Assert.Single(queued);

        queued[0]();
        Assert.Contains("k", fires);
    }
}

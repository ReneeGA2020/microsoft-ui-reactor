using Duct.Core.Navigation;
using Microsoft.UI.Xaml;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Phase 5 tests: NavigationCache LRU cache.
///
/// Tests the cache data structure in isolation (no reconciler).
/// Uses null! for MountedControl since WinUI controls can't be
/// instantiated in pure xUnit tests. LRU behavior is verified
/// by checking which routes remain in the cache after operations.
///
/// Self-host tests for full cache-restore behavior with the reconciler
/// run in Duct.AppTests.
/// </summary>
public class NavigationCacheTests
{
    private abstract record Route;
    private sealed record Home : Route;
    private sealed record Detail(int Id) : Route;
    private sealed record Settings : Route;
    private sealed record Profile(string Name) : Route;

    private static CachedPage MakePage(NavigationCacheMode cacheMode = NavigationCacheMode.Enabled) =>
        new() { MountedControl = null!, CacheMode = cacheMode };

    // ════════════════════════════════════════════════════════════════
    //  Basic cache operations
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Add_And_TryGet_Returns_Cached_Entry()
    {
        var cache = new NavigationCache(10);
        cache.Add(new Home(), MakePage());

        Assert.True(cache.TryGet(new Home(), out _));
    }

    [Fact]
    public void TryGet_Returns_False_For_Missing_Route()
    {
        var cache = new NavigationCache(10);
        Assert.False(cache.TryGet(new Home(), out _));
    }

    [Fact]
    public void TryGet_Uses_Structural_Equality()
    {
        var cache = new NavigationCache(10);
        cache.Add(new Detail(42), MakePage());

        // Different instance, same structural value
        Assert.True(cache.TryGet(new Detail(42), out _));

        // Different value
        Assert.False(cache.TryGet(new Detail(99), out _));
    }

    [Fact]
    public void Remove_Removes_Entry()
    {
        var cache = new NavigationCache(10);
        cache.Add(new Home(), MakePage());

        Assert.True(cache.Remove(new Home()));
        Assert.False(cache.TryGet(new Home(), out _));
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Remove_Returns_False_For_Missing_Route()
    {
        var cache = new NavigationCache(10);
        Assert.False(cache.Remove(new Home()));
    }

    // ════════════════════════════════════════════════════════════════
    //  LRU eviction
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void LRU_Eviction_Removes_Oldest_Entry()
    {
        int evictions = 0;
        var cache = new NavigationCache(2, _ => evictions++);

        cache.Add(new Home(), MakePage());
        cache.Add(new Detail(1), MakePage());
        Assert.Equal(2, cache.Count);
        Assert.Equal(0, evictions);

        // Adding a third entry should evict the LRU (Home, added first)
        cache.Add(new Settings(), MakePage());

        Assert.Equal(2, cache.Count);
        Assert.Equal(1, evictions);
        Assert.False(cache.TryGet(new Home(), out _)); // Evicted
        Assert.True(cache.TryGet(new Detail(1), out _));
        Assert.True(cache.TryGet(new Settings(), out _));
    }

    [Fact]
    public void TryGet_Updates_LastAccessed_Preventing_Eviction()
    {
        int evictions = 0;
        var cache = new NavigationCache(2, _ => evictions++);

        cache.Add(new Home(), MakePage());
        cache.Add(new Detail(1), MakePage());

        // Access Home to update its LastAccessed — now Detail is LRU
        cache.TryGet(new Home(), out _);

        // Adding Settings should evict Detail (now LRU), not Home
        cache.Add(new Settings(), MakePage());

        Assert.Equal(1, evictions);
        Assert.True(cache.TryGet(new Home(), out _)); // Still there
        Assert.False(cache.TryGet(new Detail(1), out _)); // Evicted
    }

    [Fact]
    public void Multiple_Evictions_In_Single_Add()
    {
        int evictions = 0;
        var cache = new NavigationCache(10, _ => evictions++);

        cache.Add(new Home(), MakePage());
        cache.Add(new Detail(1), MakePage());
        cache.Add(new Settings(), MakePage());

        // Shrink and force eviction
        cache.MaxSize = 1;
        cache.Add(new Profile("Alice"), MakePage());

        // Should evict down to MaxSize (1)
        Assert.Equal(1, cache.Count);
        Assert.Equal(3, evictions);
        Assert.True(cache.TryGet(new Profile("Alice"), out _));
    }

    // ════════════════════════════════════════════════════════════════
    //  Required cache mode (never evicted)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Required_Pages_Are_Never_Evicted()
    {
        int evictions = 0;
        var cache = new NavigationCache(2, _ => evictions++);

        // Home is Required — should never be evicted
        cache.Add(new Home(), MakePage(NavigationCacheMode.Required));
        cache.Add(new Detail(1), MakePage());

        // Adding Settings: Home is oldest but Required, so Detail gets evicted
        cache.Add(new Settings(), MakePage());

        Assert.Equal(1, evictions);
        Assert.True(cache.TryGet(new Home(), out _)); // Still there (Required)
        Assert.False(cache.TryGet(new Detail(1), out _)); // Evicted
        Assert.True(cache.TryGet(new Settings(), out _));
    }

    [Fact]
    public void All_Required_Pages_Prevents_Eviction()
    {
        int evictions = 0;
        var cache = new NavigationCache(2, _ => evictions++);

        cache.Add(new Home(), MakePage(NavigationCacheMode.Required));
        cache.Add(new Detail(1), MakePage(NavigationCacheMode.Required));

        // All entries are Required — eviction attempt does nothing
        cache.Add(new Settings(), MakePage(NavigationCacheMode.Required));

        Assert.Equal(0, evictions);
        Assert.Equal(3, cache.Count); // Over capacity but nothing can be evicted
    }

    // ════════════════════════════════════════════════════════════════
    //  Clear
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Clear_Evicts_All_Entries()
    {
        int evictions = 0;
        var cache = new NavigationCache(10, _ => evictions++);

        cache.Add(new Home(), MakePage());
        cache.Add(new Detail(1), MakePage());
        cache.Add(new Settings(), MakePage(NavigationCacheMode.Required));

        cache.Clear();

        Assert.Equal(3, evictions);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Clear_On_Empty_Cache_Does_Nothing()
    {
        var cache = new NavigationCache(10);
        cache.Clear(); // Should not throw
        Assert.Equal(0, cache.Count);
    }

    // ════════════════════════════════════════════════════════════════
    //  Cache replaces existing entry for same route
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Add_Same_Route_Replaces_Existing_Entry()
    {
        var cache = new NavigationCache(10);

        cache.Add(new Home(), MakePage());
        cache.Add(new Home(), MakePage());

        Assert.Equal(1, cache.Count);
        Assert.True(cache.TryGet(new Home(), out _));
    }

    // ════════════════════════════════════════════════════════════════
    //  Count
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Count_Reflects_Current_Entries()
    {
        var cache = new NavigationCache(10);
        Assert.Equal(0, cache.Count);

        cache.Add(new Home(), MakePage());
        Assert.Equal(1, cache.Count);

        cache.Add(new Detail(1), MakePage());
        Assert.Equal(2, cache.Count);

        cache.Remove(new Home());
        Assert.Equal(1, cache.Count);
    }
}

using Microsoft.UI.Reactor.Navigation;
using Xunit;

namespace Microsoft.UI.Reactor.Tests;

/// <summary>
/// Drives the internal NavigationDiagnostics OnX entry points so the
/// Debug.WriteLine + event-invoke bodies execute. Subscribers verify
/// the right payload made it through.
/// </summary>
public class NavigationDiagnosticsCoverageTests
{
    [Fact]
    public void OnNavigationRequested_Fires_Event_With_Payload()
    {
        NavigationDiagnosticEvent? captured = null;
        Action<NavigationDiagnosticEvent> handler = e => captured = e;
        NavigationDiagnostics.NavigationRequested += handler;
        try
        {
            NavigationDiagnostics.OnNavigationRequested("Home", "Detail", NavigationMode.Push);
        }
        finally
        {
            NavigationDiagnostics.NavigationRequested -= handler;
        }
        Assert.NotNull(captured);
        Assert.Equal("Home", captured!.From);
        Assert.Equal("Detail", captured.To);
        Assert.Equal(NavigationMode.Push, captured.Mode);
    }

    [Fact]
    public void OnNavigationCompleted_Fires_Event()
    {
        NavigationDiagnosticEvent? captured = null;
        Action<NavigationDiagnosticEvent> handler = e => captured = e;
        NavigationDiagnostics.NavigationCompleted += handler;
        try
        {
            NavigationDiagnostics.OnNavigationCompleted("A", "B", NavigationMode.Pop);
        }
        finally
        {
            NavigationDiagnostics.NavigationCompleted -= handler;
        }
        Assert.NotNull(captured);
        Assert.Equal(NavigationMode.Pop, captured!.Mode);
    }

    [Fact]
    public void OnNavigationCancelled_Captures_Reason()
    {
        NavigationDiagnosticEvent? captured = null;
        Action<NavigationDiagnosticEvent> handler = e => captured = e;
        NavigationDiagnostics.NavigationCancelled += handler;
        try
        {
            NavigationDiagnostics.OnNavigationCancelled("A", "B", NavigationMode.Push, "guard");
        }
        finally
        {
            NavigationDiagnostics.NavigationCancelled -= handler;
        }
        Assert.NotNull(captured);
        Assert.Equal("guard", captured!.Reason);
    }

    [Fact]
    public void OnCacheHit_OnCacheMiss_OnCacheEviction_Fire()
    {
        var hits = new List<CacheDiagnosticEvent>();
        var misses = new List<CacheDiagnosticEvent>();
        var evicts = new List<CacheDiagnosticEvent>();
        Action<CacheDiagnosticEvent> hh = e => hits.Add(e);
        Action<CacheDiagnosticEvent> mh = e => misses.Add(e);
        Action<CacheDiagnosticEvent> eh = e => evicts.Add(e);

        NavigationDiagnostics.CacheHit += hh;
        NavigationDiagnostics.CacheMiss += mh;
        NavigationDiagnostics.CacheEviction += eh;
        try
        {
            NavigationDiagnostics.OnCacheHit("R1");
            NavigationDiagnostics.OnCacheMiss("R2");
            NavigationDiagnostics.OnCacheEviction("R3");
        }
        finally
        {
            NavigationDiagnostics.CacheHit -= hh;
            NavigationDiagnostics.CacheMiss -= mh;
            NavigationDiagnostics.CacheEviction -= eh;
        }
        Assert.Equal("R1", Assert.Single(hits).Route);
        Assert.Equal("R2", Assert.Single(misses).Route);
        Assert.Equal("R3", Assert.Single(evicts).Route);
    }

    [Fact]
    public void OnTransitionStarted_OnTransitionCompleted_Fire()
    {
        TransitionDiagnosticEvent? start = null;
        TransitionDiagnosticEvent? end = null;
        Action<TransitionDiagnosticEvent> sh = e => start = e;
        Action<TransitionDiagnosticEvent> eh = e => end = e;
        NavigationDiagnostics.TransitionStarted += sh;
        NavigationDiagnostics.TransitionCompleted += eh;
        try
        {
            var transition = new SlideTransition();
            NavigationDiagnostics.OnTransitionStarted(transition, NavigationMode.Push);
            NavigationDiagnostics.OnTransitionCompleted(transition, NavigationMode.Pop);
        }
        finally
        {
            NavigationDiagnostics.TransitionStarted -= sh;
            NavigationDiagnostics.TransitionCompleted -= eh;
        }
        Assert.NotNull(start);
        Assert.NotNull(end);
        Assert.Equal(NavigationMode.Push, start!.Mode);
        Assert.Equal(NavigationMode.Pop, end!.Mode);
    }

    [Fact]
    public void OnDeepLinkResolved_Fires_With_Match_And_Miss()
    {
        DeepLinkDiagnosticEvent? captured = null;
        Action<DeepLinkDiagnosticEvent> handler = e => captured = e;
        NavigationDiagnostics.DeepLinkResolved += handler;
        try
        {
            NavigationDiagnostics.OnDeepLinkResolved("/home", true, 2);
        }
        finally
        {
            NavigationDiagnostics.DeepLinkResolved -= handler;
        }
        Assert.NotNull(captured);
        Assert.Equal("/home", captured!.Path);
        Assert.True(captured.Matched);
        Assert.Equal(2, captured.RouteCount);

        // Re-fire for the miss branch (matched=false formats differently)
        DeepLinkDiagnosticEvent? miss = null;
        Action<DeepLinkDiagnosticEvent> handler2 = e => miss = e;
        NavigationDiagnostics.DeepLinkResolved += handler2;
        try
        {
            NavigationDiagnostics.OnDeepLinkResolved("/missing", false, 0);
        }
        finally
        {
            NavigationDiagnostics.DeepLinkResolved -= handler2;
        }
        Assert.False(miss!.Matched);
    }

    [Fact]
    public void Diagnostic_Events_Without_Subscribers_Do_Not_Throw()
    {
        // No subscribers — should not throw.
        NavigationDiagnostics.OnNavigationRequested("A", "B", NavigationMode.Replace);
        NavigationDiagnostics.OnNavigationCompleted("A", "B", NavigationMode.Replace);
        NavigationDiagnostics.OnCacheHit("R");
        NavigationDiagnostics.OnCacheMiss("R");
        NavigationDiagnostics.OnCacheEviction("R");
        NavigationDiagnostics.OnTransitionStarted(new SlideTransition(), NavigationMode.Push);
        NavigationDiagnostics.OnTransitionCompleted(new SlideTransition(), NavigationMode.Push);
        NavigationDiagnostics.OnDeepLinkResolved("/", false, 0);
        NavigationDiagnostics.OnNavigationCancelled("A", "B", NavigationMode.Push, "x");
    }
}

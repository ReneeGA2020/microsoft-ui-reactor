using Duct.Core;
using Duct.Core.Navigation;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Phase 2 unit tests: UseNavigation hook (root and child modes).
/// Pure logic — exercises RenderContext directly, no reconciler, no WinUI controls.
/// </summary>
public class UseNavigationTests
{
    private abstract record Route;
    private sealed record Home : Route;
    private sealed record Detail(int Id) : Route;
    private sealed record Settings : Route;

    // ════════════════════════════════════════════════════════════════
    //  Root mode: UseNavigation<TRoute>(TRoute initial)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Root_Mode_Creates_Handle_With_Initial_Route()
    {
        var ctx = new RenderContext();
        ctx.BeginRender(() => { });

        var nav = ctx.UseNavigation<Route>(new Home());

        Assert.NotNull(nav);
        Assert.IsType<Home>(nav.CurrentRoute);
        Assert.False(nav.CanGoBack);
        Assert.False(nav.CanGoForward);
        Assert.Equal(1, nav.Depth);
    }

    [Fact]
    public void Root_Mode_Returns_Stable_Handle_Across_Rerenders()
    {
        var ctx = new RenderContext();

        ctx.BeginRender(() => { });
        var nav1 = ctx.UseNavigation<Route>(new Home());

        ctx.BeginRender(() => { });
        var nav2 = ctx.UseNavigation<Route>(new Home());

        Assert.Same(nav1, nav2);
    }

    [Fact]
    public void Root_Mode_Ignores_Initial_On_Rerender()
    {
        var ctx = new RenderContext();

        ctx.BeginRender(() => { });
        var nav = ctx.UseNavigation<Route>(new Home());
        nav.Navigate(new Detail(1));

        // Re-render with different initial — should be ignored
        ctx.BeginRender(() => { });
        var nav2 = ctx.UseNavigation<Route>(new Settings());

        Assert.Same(nav, nav2);
        Assert.IsType<Detail>(nav2.CurrentRoute);
    }

    [Fact]
    public void Navigation_Mutation_Triggers_Rerender()
    {
        var ctx = new RenderContext();
        int rerenderCount = 0;

        ctx.BeginRender(() => rerenderCount++);
        var nav = ctx.UseNavigation<Route>(new Home());

        Assert.Equal(0, rerenderCount);
        nav.Navigate(new Detail(1));
        Assert.Equal(1, rerenderCount);
    }

    [Fact]
    public void Navigation_GoBack_Triggers_Rerender()
    {
        var ctx = new RenderContext();
        int rerenderCount = 0;

        ctx.BeginRender(() => rerenderCount++);
        var nav = ctx.UseNavigation<Route>(new Home());
        nav.Navigate(new Detail(1));
        Assert.Equal(1, rerenderCount);

        // Re-render to capture new callback
        ctx.BeginRender(() => rerenderCount++);
        ctx.UseNavigation<Route>(new Home());

        nav.GoBack();
        Assert.Equal(2, rerenderCount);
    }

    [Fact]
    public void Cancelled_Navigation_Does_Not_Trigger_Rerender()
    {
        var ctx = new RenderContext();
        int rerenderCount = 0;

        ctx.BeginRender(() => rerenderCount++);
        var nav = ctx.UseNavigation<Route>(new Home());

        // Pop from empty stack should fail and NOT trigger rerender
        nav.GoBack();
        Assert.Equal(0, rerenderCount);
    }

    [Fact]
    public void Rerender_Captures_Latest_Callback()
    {
        var ctx = new RenderContext();
        int callbackA = 0;
        int callbackB = 0;

        // Render 1: capture callback A
        ctx.BeginRender(() => callbackA++);
        var nav = ctx.UseNavigation<Route>(new Home());

        // Render 2: capture callback B
        ctx.BeginRender(() => callbackB++);
        ctx.UseNavigation<Route>(new Home());

        // Navigate should call latest callback (B)
        nav.Navigate(new Detail(1));
        Assert.Equal(0, callbackA);
        Assert.Equal(1, callbackB);
    }

    // ════════════════════════════════════════════════════════════════
    //  Child mode: UseNavigation<TRoute>()
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Child_Mode_Throws_When_No_Ancestor_Provides_Context()
    {
        var ctx = new RenderContext();
        ctx.BeginRender(() => { });

        var ex = Assert.Throws<InvalidOperationException>(() => ctx.UseNavigation<Route>());
        Assert.Contains("UseNavigation", ex.Message);
        Assert.Contains("Route", ex.Message);
    }

    [Fact]
    public void Child_Mode_Retrieves_Handle_From_Context_Scope()
    {
        // Simulate what the reconciler does: push context values before rendering child
        var rootCtx = new RenderContext();
        rootCtx.BeginRender(() => { });
        var rootNav = rootCtx.UseNavigation<Route>(new Home());

        // Create a child context with the navigation handle provided via context scope.
        // We need to simulate the reconciler's ContextScope push/pop.
        // The NavigationHost DSL adds .Provide(NavigationContext<Route>.Instance, nav).
        // During reconciliation, this gets pushed onto the ContextScope.
        var childCtx = new RenderContext();
        var scope = new ContextScope();
        scope.Push(new Dictionary<DuctContextBase, object?> { [NavigationContext<Route>.Instance] = rootNav });
        childCtx.BeginRender(() => { }, scope);

        var childNav = childCtx.UseNavigation<Route>();
        Assert.Same(rootNav, childNav);
    }

    // ════════════════════════════════════════════════════════════════
    //  Hook state stability
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void UseNavigation_Can_Coexist_With_Other_Hooks()
    {
        var ctx = new RenderContext();
        ctx.BeginRender(() => { });

        var (count, setCount) = ctx.UseState(0);
        var nav = ctx.UseNavigation<Route>(new Home());
        var myRef = ctx.UseRef("hello");

        Assert.Equal(0, count);
        Assert.IsType<Home>(nav.CurrentRoute);
        Assert.Equal("hello", myRef.Current);

        // Second render — same order
        setCount(1);
        ctx.BeginRender(() => { });
        var (count2, _) = ctx.UseState(0);
        var nav2 = ctx.UseNavigation<Route>(new Home());
        var myRef2 = ctx.UseRef("hello");

        Assert.Equal(1, count2);
        Assert.Same(nav, nav2);
        Assert.Same(myRef, myRef2);
    }
}

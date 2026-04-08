using Duct.Core.Navigation;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Phase 1 unit tests: NavigationStack, guards, and NavigationHandle.
/// Pure logic — no reconciler, no WinUI controls, no UI thread.
/// </summary>
public class NavigationStackTests
{
    // Test route types — records give us structural equality for free.
    private abstract record Route;
    private sealed record Home : Route;
    private sealed record Detail(int Id) : Route;
    private sealed record Settings : Route;
    private sealed record Profile(string Name) : Route;

    // ════════════════════════════════════════════════════════════════
    //  Stack operations
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Push_Adds_To_BackStack_And_Sets_New_Current()
    {
        var stack = new NavigationStack<Route>(new Home());
        stack.Push(new Detail(1));

        Assert.IsType<Detail>(stack.Current);
        Assert.Equal(new Detail(1), stack.Current);
        Assert.Single(stack.BackStack);
        Assert.IsType<Home>(stack.BackStack[0]);
    }

    [Fact]
    public void Push_Clears_Forward_Stack()
    {
        var stack = new NavigationStack<Route>(new Home());
        stack.Push(new Detail(1));
        stack.Pop();
        Assert.Single(stack.ForwardStack);

        stack.Push(new Settings());
        Assert.Empty(stack.ForwardStack);
    }

    [Fact]
    public void Pop_Returns_To_Previous_Route_And_Pushes_Current_To_ForwardStack()
    {
        var stack = new NavigationStack<Route>(new Home());
        stack.Push(new Detail(1));

        var result = stack.Pop();

        Assert.True(result);
        Assert.IsType<Home>(stack.Current);
        Assert.Single(stack.ForwardStack);
        Assert.Equal(new Detail(1), stack.ForwardStack[0]);
        Assert.Empty(stack.BackStack);
    }

    [Fact]
    public void Pop_Returns_False_When_BackStack_Is_Empty()
    {
        var stack = new NavigationStack<Route>(new Home());
        Assert.False(stack.Pop());
        Assert.IsType<Home>(stack.Current);
    }

    [Fact]
    public void Forward_Navigates_To_ForwardStack_Entry_And_Pushes_Current_To_BackStack()
    {
        var stack = new NavigationStack<Route>(new Home());
        stack.Push(new Detail(1));
        stack.Pop(); // Home is current, Detail(1) in forward

        var result = stack.Forward();

        Assert.True(result);
        Assert.Equal(new Detail(1), stack.Current);
        Assert.Single(stack.BackStack);
        Assert.IsType<Home>(stack.BackStack[0]);
        Assert.Empty(stack.ForwardStack);
    }

    [Fact]
    public void Forward_Returns_False_When_ForwardStack_Is_Empty()
    {
        var stack = new NavigationStack<Route>(new Home());
        Assert.False(stack.Forward());
    }

    [Fact]
    public void Replace_Changes_Current_Without_Modifying_BackOrForward_Stacks()
    {
        var stack = new NavigationStack<Route>(new Home());
        stack.Push(new Detail(1));
        stack.Pop(); // forward has Detail(1)

        stack.Replace(new Settings());

        Assert.IsType<Settings>(stack.Current);
        Assert.Empty(stack.BackStack);
        Assert.Single(stack.ForwardStack); // forward unchanged
        Assert.Equal(new Detail(1), stack.ForwardStack[0]);
    }

    [Fact]
    public void Reset_Clears_All_Stacks_And_Sets_New_Root()
    {
        var stack = new NavigationStack<Route>(new Home());
        stack.Push(new Detail(1));
        stack.Push(new Settings());
        stack.Pop(); // forward has Settings

        stack.Reset(new Profile("Alice"));

        Assert.Equal(new Profile("Alice"), stack.Current);
        Assert.Empty(stack.BackStack);
        Assert.Empty(stack.ForwardStack);
    }

    [Fact]
    public void PopTo_Pops_Until_Predicate_Matches_Returns_True()
    {
        var stack = new NavigationStack<Route>(new Home());
        stack.Push(new Detail(1));
        stack.Push(new Detail(2));
        stack.Push(new Settings());

        var result = stack.PopTo(r => r is Detail { Id: 1 });

        Assert.True(result);
        Assert.Equal(new Detail(1), stack.Current);
        // Back stack: [Home]
        Assert.Single(stack.BackStack);
        Assert.IsType<Home>(stack.BackStack[0]);
        // Forward stack: [Settings (was current), Detail(2)]
        Assert.Equal(2, stack.ForwardStack.Count);
    }

    [Fact]
    public void PopTo_Returns_False_When_No_Match_In_BackStack()
    {
        var stack = new NavigationStack<Route>(new Home());
        stack.Push(new Detail(1));

        var result = stack.PopTo(r => r is Profile);
        Assert.False(result);
        // Stack unchanged
        Assert.Equal(new Detail(1), stack.Current);
        Assert.Single(stack.BackStack);
    }

    [Fact]
    public void Depth_Returns_BackStack_Count_Plus_One()
    {
        var stack = new NavigationStack<Route>(new Home());
        Assert.Equal(1, stack.Depth);

        stack.Push(new Detail(1));
        Assert.Equal(2, stack.Depth);

        stack.Push(new Settings());
        Assert.Equal(3, stack.Depth);

        stack.Pop();
        Assert.Equal(2, stack.Depth);
    }

    // ════════════════════════════════════════════════════════════════
    //  OnChanged callback
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void OnChanged_Fires_After_Every_Successful_Mutation()
    {
        var stack = new NavigationStack<Route>(new Home());
        var callCount = 0;
        stack.OnChanged = () => callCount++;

        stack.Push(new Detail(1));
        Assert.Equal(1, callCount);

        stack.Pop();
        Assert.Equal(2, callCount);

        stack.Forward();
        Assert.Equal(3, callCount);

        stack.Replace(new Settings());
        Assert.Equal(4, callCount);

        stack.Reset(new Home());
        Assert.Equal(5, callCount);
    }

    // ════════════════════════════════════════════════════════════════
    //  Guard tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Guard_That_Cancels_Prevents_Push_And_Returns_False()
    {
        var stack = new NavigationStack<Route>(new Home());
        stack.Guard = ctx => { ctx.Cancel(); return false; };

        var result = stack.Push(new Detail(1));

        Assert.False(result);
        Assert.IsType<Home>(stack.Current);
        Assert.Empty(stack.BackStack);
    }

    [Fact]
    public void Guard_That_Cancels_Prevents_Pop_And_Returns_False()
    {
        var stack = new NavigationStack<Route>(new Home());
        stack.Push(new Detail(1));
        stack.Guard = ctx => { ctx.Cancel(); return false; };

        var result = stack.Pop();

        Assert.False(result);
        Assert.Equal(new Detail(1), stack.Current);
        Assert.Single(stack.BackStack); // unchanged
    }

    [Fact]
    public void Guard_Receives_Correct_NavigatingFromContext()
    {
        var stack = new NavigationStack<Route>(new Home());
        NavigatingFromContext? captured = null;
        stack.Guard = ctx =>
        {
            captured = ctx;
            return true; // allow
        };

        stack.Push(new Detail(42));

        Assert.NotNull(captured);
        Assert.IsType<Home>(captured!.Route);
        Assert.Equal(new Detail(42), captured.TargetRoute);
        Assert.Equal(NavigationMode.Push, captured.Mode);
        Assert.False(captured.IsCancelled);
    }

    [Fact]
    public void Guard_Not_Invoked_When_Guard_Is_Null()
    {
        var stack = new NavigationStack<Route>(new Home());
        // Guard is null by default — just verify Push succeeds without issues.
        Assert.True(stack.Push(new Detail(1)));
        Assert.Equal(new Detail(1), stack.Current);
    }

    [Fact]
    public void Guard_Cancellation_Leaves_Stack_State_Unchanged()
    {
        var stack = new NavigationStack<Route>(new Home());
        stack.Push(new Detail(1));
        stack.Push(new Detail(2));
        stack.Pop(); // back: [Home, Detail(1)], current: Detail(1)... wait

        // Let's be explicit:
        // After: Push(Home→Detail(1)), Push(Detail(1)→Detail(2)), Pop(Detail(2)→Detail(1))
        // State: back=[Home], current=Detail(1), forward=[Detail(2)]

        // Snapshot state
        var backCount = stack.BackStack.Count;
        var forwardCount = stack.ForwardStack.Count;
        var current = stack.Current;

        stack.Guard = ctx => { ctx.Cancel(); return false; };

        // Try all mutations — all should be blocked
        Assert.False(stack.Push(new Settings()));
        Assert.False(stack.Pop());
        Assert.False(stack.Forward());
        Assert.False(stack.Replace(new Settings()));
        Assert.False(stack.Reset(new Home()));
        Assert.False(stack.PopTo(r => r is Home));

        // State unchanged
        Assert.Equal(current, stack.Current);
        Assert.Equal(backCount, stack.BackStack.Count);
        Assert.Equal(forwardCount, stack.ForwardStack.Count);
    }

    // ════════════════════════════════════════════════════════════════
    //  NavigationHandle tests
    // ════════════════════════════════════════════════════════════════

    private static NavigationHandle<Route> CreateHandle(Route initial)
    {
        var stack = new NavigationStack<Route>(initial);
        return new NavigationHandle<Route>(stack);
    }

    [Fact]
    public void Navigate_Fires_Navigated_Event_With_Correct_Args()
    {
        var nav = CreateHandle(new Home());
        NavigationEventArgs<Route>? captured = null;
        nav.Navigated += args => captured = args;

        nav.Navigate(new Detail(1));

        Assert.NotNull(captured);
        Assert.Equal(new Detail(1), captured!.Route);
        Assert.IsType<Home>(captured.PreviousRoute);
        Assert.Equal(NavigationMode.Push, captured.Mode);
    }

    [Fact]
    public void GoBack_Fires_Navigated_Event_With_Mode_Pop()
    {
        var nav = CreateHandle(new Home());
        nav.Navigate(new Detail(1));

        NavigationEventArgs<Route>? captured = null;
        nav.Navigated += args => captured = args;

        nav.GoBack();

        Assert.NotNull(captured);
        Assert.IsType<Home>(captured!.Route);
        Assert.Equal(new Detail(1), captured.PreviousRoute);
        Assert.Equal(NavigationMode.Pop, captured.Mode);
    }

    [Fact]
    public void GoForward_Fires_Navigated_Event_With_Mode_Forward()
    {
        var nav = CreateHandle(new Home());
        nav.Navigate(new Detail(1));
        nav.GoBack(); // forward has Detail(1)

        NavigationEventArgs<Route>? captured = null;
        nav.Navigated += args => captured = args;

        nav.GoForward();

        Assert.NotNull(captured);
        Assert.Equal(new Detail(1), captured!.Route);
        Assert.IsType<Home>(captured.PreviousRoute);
        Assert.Equal(NavigationMode.Forward, captured.Mode);
    }

    [Fact]
    public void Replace_Fires_Navigated_Event_With_Mode_Replace()
    {
        var nav = CreateHandle(new Home());

        NavigationEventArgs<Route>? captured = null;
        nav.Navigated += args => captured = args;

        nav.Replace(new Settings());

        Assert.NotNull(captured);
        Assert.IsType<Settings>(captured!.Route);
        Assert.IsType<Home>(captured.PreviousRoute);
        Assert.Equal(NavigationMode.Replace, captured.Mode);
    }

    [Fact]
    public void Reset_Fires_Navigated_Event_With_Mode_Reset()
    {
        var nav = CreateHandle(new Home());
        nav.Navigate(new Detail(1));
        nav.Navigate(new Settings());

        NavigationEventArgs<Route>? captured = null;
        nav.Navigated += args => captured = args;

        nav.Reset(new Home());

        Assert.NotNull(captured);
        Assert.IsType<Home>(captured!.Route);
        Assert.IsType<Settings>(captured.PreviousRoute);
        Assert.Equal(NavigationMode.Reset, captured.Mode);
    }

    [Fact]
    public void Navigate_With_PushToBackStack_False_Calls_Replace_Internally()
    {
        var nav = CreateHandle(new Home());
        NavigationEventArgs<Route>? captured = null;
        nav.Navigated += args => captured = args;

        nav.Navigate(new Detail(1), new NavigateOptions { PushToBackStack = false });

        Assert.NotNull(captured);
        Assert.Equal(NavigationMode.Replace, captured!.Mode);
        Assert.Equal(new Detail(1), nav.CurrentRoute);
        Assert.Empty(nav.BackStack); // no back stack entry since we replaced
    }

    [Fact]
    public void NavigationHandle_Exposes_Correct_Readonly_State_After_Operations()
    {
        var nav = CreateHandle(new Home());

        Assert.IsType<Home>(nav.CurrentRoute);
        Assert.False(nav.CanGoBack);
        Assert.False(nav.CanGoForward);
        Assert.Equal(1, nav.Depth);
        Assert.Empty(nav.BackStack);
        Assert.Empty(nav.ForwardStack);

        nav.Navigate(new Detail(1));
        Assert.Equal(new Detail(1), nav.CurrentRoute);
        Assert.True(nav.CanGoBack);
        Assert.False(nav.CanGoForward);
        Assert.Equal(2, nav.Depth);
        Assert.Single(nav.BackStack);

        nav.GoBack();
        Assert.IsType<Home>(nav.CurrentRoute);
        Assert.False(nav.CanGoBack);
        Assert.True(nav.CanGoForward);
        Assert.Equal(1, nav.Depth);
        Assert.Single(nav.ForwardStack);
    }
}

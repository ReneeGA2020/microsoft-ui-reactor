namespace Duct.Core.Navigation;

/// <summary>
/// Context passed to <c>onNavigatedTo</c> lifecycle callbacks after a page becomes active.
/// </summary>
public sealed class NavigatedToContext
{
    public object Route { get; }
    public object? PreviousRoute { get; }
    public NavigationMode Mode { get; }

    internal NavigatedToContext(object route, object? previousRoute, NavigationMode mode)
    {
        Route = route;
        PreviousRoute = previousRoute;
        Mode = mode;
    }
}

/// <summary>
/// Context passed to <c>onNavigatedFrom</c> lifecycle callbacks after a page is no longer active.
/// </summary>
public sealed class NavigatedFromContext
{
    public object Route { get; }
    public object TargetRoute { get; }
    public NavigationMode Mode { get; }

    internal NavigatedFromContext(object route, object targetRoute, NavigationMode mode)
    {
        Route = route;
        TargetRoute = targetRoute;
        Mode = mode;
    }
}

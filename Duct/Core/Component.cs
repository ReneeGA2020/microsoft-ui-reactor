namespace Duct.Core;

/// <summary>
/// Base class for stateful components (like React class components, but using hooks).
/// Components hold a RenderContext that tracks their hook state across re-renders.
/// </summary>
public abstract class Component
{
    internal RenderContext Context { get; } = new();

    /// <summary>
    /// Override to describe the UI. Use UseState, UseEffect, etc. from the context.
    /// Must call hooks in the same order every render.
    /// </summary>
    public abstract Element Render();

    /// <summary>
    /// Controls whether this propless component should re-render when its parent re-renders.
    /// Default: false — propless components only re-render from their own state changes or context changes.
    /// Override and return true to always re-render when the parent re-renders.
    /// </summary>
    protected internal virtual bool ShouldUpdate() => false;

    // ── Hook convenience methods (delegate to Context) ─────────────

    protected (T Value, Action<T> Set) UseState<T>(T initialValue)
        => Context.UseState(initialValue);

    protected (T Value, Action<Func<T, T>> Update) UseReducer<T>(T initialValue)
        => Context.UseReducer(initialValue);

    /// <summary>
    /// Redux-style reducer: takes a reducer function (state, action) => newState.
    /// Returns (currentState, dispatch) where dispatch sends an action through the reducer.
    /// </summary>
    protected (TState Value, Action<TAction> Dispatch) UseReducer<TState, TAction>(
        Func<TState, TAction, TState> reducer, TState initialValue)
        => Context.UseReducer(reducer, initialValue);

    protected void UseEffect(Action effect, params object[] dependencies)
        => Context.UseEffect(effect, dependencies);

    protected void UseEffect(Func<Action> effectWithCleanup, params object[] dependencies)
        => Context.UseEffect(effectWithCleanup, dependencies);

    protected T UseMemo<T>(Func<T> factory, params object[] dependencies)
        => Context.UseMemo(factory, dependencies);

    protected Action UseCallback(Action callback, params object[] dependencies)
        => Context.UseCallback(callback, dependencies);

    protected Ref<T> UseRef<T>(T initialValue = default!)
        => Context.UseRef(initialValue);

    protected (double Width, double Height) UseWindowSize(Microsoft.UI.Xaml.Window window)
        => Context.UseWindowSize(window);

    protected bool UseBreakpoint(Microsoft.UI.Xaml.Window window, double minWidth)
        => Context.UseBreakpoint(window, minWidth);

    protected T UseObservableTree<T>(T source) where T : System.ComponentModel.INotifyPropertyChanged
        => Context.UseObservableTree(source);

    protected T UseObservable<T>(T source) where T : System.ComponentModel.INotifyPropertyChanged
        => Context.UseObservable(source);

    protected TProp UseObservableProperty<T, TProp>(T source, Func<T, TProp> selector, string propertyName)
        where T : System.ComponentModel.INotifyPropertyChanged
        => Context.UseObservableProperty(source, selector, propertyName);

    protected IReadOnlyList<T> UseCollection<T>(System.Collections.ObjectModel.ObservableCollection<T> collection)
        => Context.UseCollection(collection);

    protected Localization.IntlAccessor UseIntl()
        => Context.UseIntl();

    protected T UseContext<T>(DuctContext<T> context)
        => Context.UseContext(context);

    protected Navigation.NavigationHandle<TRoute> UseNavigation<TRoute>(TRoute initial) where TRoute : notnull
        => Context.UseNavigation(initial);

    protected Navigation.NavigationHandle<TRoute> UseNavigation<TRoute>() where TRoute : notnull
        => Context.UseNavigation<TRoute>();

    protected void UseNavigationLifecycle(
        Action<Navigation.NavigatedToContext>? onNavigatedTo = null,
        Action<Navigation.NavigatingFromContext>? onNavigatingFrom = null,
        Action<Navigation.NavigatedFromContext>? onNavigatedFrom = null)
        => Context.UseNavigationLifecycle(onNavigatedTo, onNavigatingFrom, onNavigatedFrom);

    protected void UseSystemBackButton<TRoute>(
        Navigation.NavigationHandle<TRoute> nav,
        Microsoft.UI.Xaml.Window window) where TRoute : notnull
        => Context.UseSystemBackButton(nav, window);

    protected (T Value, Action<T> Set) UsePersisted<T>(string key, T initialValue)
        => Context.UsePersisted(key, initialValue);

    protected DuctCommand UseCommand(DuctCommand command)
        => Context.UseCommand(command);

    protected DuctCommand<T> UseCommand<T>(DuctCommand<T> command)
        => Context.UseCommand(command);
}

/// <summary>
/// Interface for setting props without reflection.
/// </summary>
internal interface IPropsReceiver
{
    void SetProps(object props);
}

/// <summary>
/// Base class for components that receive typed props (e.g., navigation parameters).
/// Props are set by the host before rendering.
/// </summary>
public abstract class Component<TProps> : Component, IPropsReceiver
{
    /// <summary>
    /// The typed props passed to this component by its parent or host.
    /// </summary>
    public TProps Props { get; internal set; } = default!;

    void IPropsReceiver.SetProps(object props) => Props = (TProps)props;

    /// <summary>
    /// Controls whether this component should re-render when its parent re-renders with new props.
    /// Default: structural equality via record Equals — record props get auto-comparison for free;
    /// class props need an Equals override.
    /// Override for custom comparison logic.
    /// </summary>
    protected internal virtual bool ShouldUpdate(TProps? oldProps, TProps? newProps)
        => !Equals(oldProps, newProps);
}

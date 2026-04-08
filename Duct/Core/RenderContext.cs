namespace Duct.Core;

/// <summary>
/// Passed to function components and provides access to hooks.
/// Each component instance gets its own RenderContext which tracks hook call order.
/// </summary>
public sealed class RenderContext
{
    private readonly List<HookState> _hooks = new();
    private int _hookIndex;
    private Action? _requestRerender;
    private ContextScope? _contextScope;

    internal void BeginRender(Action requestRerender)
    {
        _hookIndex = 0;
        _requestRerender = requestRerender;
    }

    internal void BeginRender(Action requestRerender, ContextScope contextScope)
    {
        _hookIndex = 0;
        _requestRerender = requestRerender;
        _contextScope = contextScope;
    }

    /// <summary>
    /// DEBUG ONLY: Directly set a UseState hook value by index and trigger re-render.
    /// Used for testing state changes without event handlers.
    /// </summary>
    internal void UseStateSetterByIndex<T>(int index, T newValue)
    {
        if (index < _hooks.Count && _hooks[index] is ValueHookState<T> hook)
        {
            hook.Value = newValue;
            _requestRerender?.Invoke();
        }
    }

    /// <summary>
    /// Declares a piece of state. Returns (currentValue, setter).
    /// Must be called in the same order every render (just like React hooks).
    /// </summary>
    public (T Value, Action<T> Set) UseState<T>(T initialValue)
    {
        if (_hookIndex >= _hooks.Count)
        {
            _hooks.Add(new ValueHookState<T>(initialValue));
        }

        var currentIndex = _hookIndex;
        _hookIndex++;

        if (_hooks[currentIndex] is not ValueHookState<T> hook)
            throw new InvalidOperationException(
                $"Hook at index {currentIndex} is {_hooks[currentIndex].GetType().Name}, expected ValueHookState<{typeof(T).Name}> (UseState). " +
                "Hooks must be called in the same order every render.");

        T current = hook.Value;

        void Setter(T newValue)
        {
            var h = (ValueHookState<T>)_hooks[currentIndex];
            if (!EqualityComparer<T>.Default.Equals(h.Value, newValue))
            {
                h.Value = newValue;
                _requestRerender?.Invoke();
            }
        }

        return (current, Setter);
    }

    /// <summary>
    /// Declares a piece of state with a functional updater variant.
    /// The updater receives the previous value and returns the next.
    /// </summary>
    public (T Value, Action<Func<T, T>> Update) UseReducer<T>(T initialValue)
    {
        if (_hookIndex >= _hooks.Count)
        {
            _hooks.Add(new ValueHookState<T>(initialValue));
        }

        var currentIndex = _hookIndex;
        _hookIndex++;

        if (_hooks[currentIndex] is not ValueHookState<T> hook)
            throw new InvalidOperationException(
                $"Hook at index {currentIndex} is {_hooks[currentIndex].GetType().Name}, expected ValueHookState<{typeof(T).Name}> (UseReducer). " +
                "Hooks must be called in the same order every render.");

        T current = hook.Value;

        void Updater(Func<T, T> reducer)
        {
            var h = (ValueHookState<T>)_hooks[currentIndex];
            var prev = h.Value;
            var next = reducer(prev);
            if (!EqualityComparer<T>.Default.Equals(prev, next))
            {
                h.Value = next;
                _requestRerender?.Invoke();
            }
        }

        return (current, Updater);
    }

    /// <summary>
    /// Declares a piece of state managed by a reducer function (like Redux).
    /// The reducer takes (currentState, action) and returns the next state.
    /// Returns (currentState, dispatch) where dispatch sends an action through the reducer.
    /// </summary>
    public (TState Value, Action<TAction> Dispatch) UseReducer<TState, TAction>(
        Func<TState, TAction, TState> reducer, TState initialValue)
    {
        if (_hookIndex >= _hooks.Count)
        {
            _hooks.Add(new ValueHookState<TState>(initialValue));
        }

        var currentIndex = _hookIndex;
        _hookIndex++;

        if (_hooks[currentIndex] is not ValueHookState<TState> hook)
            throw new InvalidOperationException(
                $"Hook at index {currentIndex} is {_hooks[currentIndex].GetType().Name}, expected ValueHookState<{typeof(TState).Name}> (UseReducer). " +
                "Hooks must be called in the same order every render.");

        TState current = hook.Value;

        void Dispatch(TAction action)
        {
            var h = (ValueHookState<TState>)_hooks[currentIndex];
            var prev = h.Value;
            var next = reducer(prev, action);
            if (!EqualityComparer<TState>.Default.Equals(prev, next))
            {
                h.Value = next;
                _requestRerender?.Invoke();
            }
        }

        return (current, Dispatch);
    }

    /// <summary>
    /// Runs a side effect after render. The effect re-runs when any dependency changes.
    /// Pass an empty array for "run once on mount" semantics.
    /// Returns a cleanup action that runs before the next effect or on unmount.
    /// </summary>
    public void UseEffect(Action effect, params object[] dependencies)
    {
        if (_hookIndex >= _hooks.Count)
        {
            _hooks.Add(new EffectHookState { Dependencies = null, Effect = effect });
        }

        if (_hooks[_hookIndex] is not EffectHookState hook)
            throw new InvalidOperationException(
                $"Hook at index {_hookIndex} is {_hooks[_hookIndex].GetType().Name}, expected EffectHookState. " +
                "Hooks must be called in the same order every render.");
        _hookIndex++;

        if (hook.Dependencies is null || !DepsEqual(hook.Dependencies, dependencies))
        {
            hook.PendingCleanup = hook.Cleanup;
            hook.Cleanup = null;
            hook.Dependencies = dependencies.ToArray();
            hook.Effect = effect;
            hook.Pending = true;
        }
    }

    /// <summary>
    /// Like UseEffect but the effect returns a cleanup function.
    /// </summary>
    public void UseEffect(Func<Action> effectWithCleanup, params object[] dependencies)
    {
        if (_hookIndex >= _hooks.Count)
        {
            _hooks.Add(new EffectHookState { Dependencies = null });
        }

        if (_hooks[_hookIndex] is not EffectHookState hook)
            throw new InvalidOperationException(
                $"Hook at index {_hookIndex} is {_hooks[_hookIndex].GetType().Name}, expected EffectHookState. " +
                "Hooks must be called in the same order every render.");
        _hookIndex++;

        if (hook.Dependencies is null || !DepsEqual(hook.Dependencies, dependencies))
        {
            hook.PendingCleanup = hook.Cleanup;
            hook.Cleanup = null;
            hook.Dependencies = dependencies.ToArray();
            hook.EffectWithCleanup = effectWithCleanup;
            hook.Pending = true;
        }
    }

    /// <summary>
    /// Memoizes a computed value, recomputing only when dependencies change.
    /// </summary>
    public T UseMemo<T>(Func<T> factory, params object[] dependencies)
    {
        if (_hookIndex >= _hooks.Count)
        {
            _hooks.Add(new MemoHookState<T> { Dependencies = null });
        }

        if (_hooks[_hookIndex] is not MemoHookState<T> hook)
            throw new InvalidOperationException(
                $"Hook at index {_hookIndex} is {_hooks[_hookIndex].GetType().Name}, expected MemoHookState<{typeof(T).Name}>. " +
                "Hooks must be called in the same order every render.");
        _hookIndex++;

        if (hook.Dependencies is null || !DepsEqual(hook.Dependencies, dependencies))
        {
            hook.Value = factory();
            hook.Dependencies = dependencies.ToArray();
        }

        return hook.Value;
    }

    /// <summary>
    /// Returns a stable callback reference that doesn't change between renders.
    /// </summary>
    public Action UseCallback(Action callback, params object[] dependencies)
    {
        return UseMemo(() => callback, dependencies);
    }

    /// <summary>
    /// Returns a mutable ref object that persists across renders.
    /// </summary>
    public Ref<T> UseRef<T>(T initialValue = default!)
    {
        if (_hookIndex >= _hooks.Count)
        {
            _hooks.Add(new ValueHookState<Ref<T>>(new Ref<T>(initialValue)));
        }

        var currentIndex = _hookIndex;
        _hookIndex++;

        if (_hooks[currentIndex] is not ValueHookState<Ref<T>> hook)
            throw new InvalidOperationException(
                $"Hook at index {currentIndex} expected ValueHookState<Ref<{typeof(T).Name}>>, got {_hooks[currentIndex].GetType().Name}. " +
                "Hooks must be called in the same order every render.");
        return hook.Value;
    }

    // ════════════════════════════════════════════════════════════════
    //  Persisted state hooks
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Like UseState, but the value survives unmount/remount via an in-memory cache.
    /// On first mount, uses cached value if available, otherwise uses initialValue.
    /// Value is saved to cache on unmount.
    /// </summary>
    public (T Value, Action<T> Set) UsePersisted<T>(string key, T initialValue)
    {
        if (_hookIndex >= _hooks.Count)
        {
            T initial = PersistedStateCache.TryGet<T>(key, out var cached) ? cached : initialValue;
            _hooks.Add(new PersistedHookState<T>(initial) { PersistKey = key });
        }

        var currentIndex = _hookIndex;
        _hookIndex++;

        if (_hooks[currentIndex] is not PersistedHookState<T> hook)
            throw new InvalidOperationException(
                $"Hook at index {currentIndex} is {_hooks[currentIndex].GetType().Name}, expected PersistedHookState<{typeof(T).Name}> (UsePersisted). " +
                "Hooks must be called in the same order every render.");

        T current = hook.Value;

        void Setter(T newValue)
        {
            var h = (PersistedHookState<T>)_hooks[currentIndex];
            if (!EqualityComparer<T>.Default.Equals(h.Value, newValue))
            {
                h.Value = newValue;
                _requestRerender?.Invoke();
            }
        }

        return (current, Setter);
    }

    // ════════════════════════════════════════════════════════════════
    //  Observable interop hooks
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Observes an object and all nested INotifyPropertyChanged values
    /// reachable through its properties. Re-renders when any property
    /// at any depth changes. Automatically subscribes/unsubscribes as
    /// property values change.
    /// </summary>
    public T UseObservableTree<T>(T source) where T : System.ComponentModel.INotifyPropertyChanged
    {
        var (_, forceRender) = UseReducer(false);
        var trackerRef = UseRef<ObservableTreeTracker?>(null);

        UseEffect(() =>
        {
            var tracker = new ObservableTreeTracker(() => forceRender(v => !v));
            trackerRef.Current = tracker;
            tracker.SyncSubscriptions(source);
            return () => tracker.Dispose();
        }, source);

        return source;
    }

    /// <summary>
    /// Subscribes to an INotifyPropertyChanged source and re-renders when any property changes.
    /// Returns the same source object.
    /// </summary>
    public T UseObservable<T>(T source) where T : System.ComponentModel.INotifyPropertyChanged
    {
        var (_, forceRender) = UseReducer(false);
        UseEffect(() =>
        {
            void handler(object? s, System.ComponentModel.PropertyChangedEventArgs e)
                => forceRender(v => !v);
            source.PropertyChanged += handler;
            return () => source.PropertyChanged -= handler;
        }, source);
        return source;
    }

    /// <summary>
    /// Subscribes to a specific property on an INotifyPropertyChanged source.
    /// Re-renders only when that property changes.
    /// </summary>
    public TProp UseObservableProperty<T, TProp>(T source, Func<T, TProp> selector, string propertyName)
        where T : System.ComponentModel.INotifyPropertyChanged
    {
        var (_, forceRender) = UseReducer(false);
        UseEffect(() =>
        {
            void handler(object? s, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName == propertyName || string.IsNullOrEmpty(e.PropertyName))
                    forceRender(v => !v);
            }
            source.PropertyChanged += handler;
            return () => source.PropertyChanged -= handler;
        }, source, propertyName);
        return selector(source);
    }

    /// <summary>
    /// Subscribes to an ObservableCollection and re-renders on Add/Remove/Reset.
    /// Returns the collection as IReadOnlyList.
    /// </summary>
    public IReadOnlyList<T> UseCollection<T>(System.Collections.ObjectModel.ObservableCollection<T> collection)
    {
        var (_, forceRender) = UseReducer(false);
        UseEffect(() =>
        {
            void handler(object? s, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
                => forceRender(v => !v);
            collection.CollectionChanged += handler;
            return () => collection.CollectionChanged -= handler;
        }, collection);
        return collection;
    }

    // ════════════════════════════════════════════════════════════════
    //  Navigation hooks
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Root mode: creates a navigation stack with the given initial route.
    /// Returns a stable <see cref="Navigation.NavigationHandle{TRoute}"/> across re-renders.
    /// Wire this handle to a <c>NavigationHost</c> in the DSL to render route content.
    /// The handle is automatically provided to descendants via context so child components
    /// can call <c>UseNavigation&lt;TRoute&gt;()</c> (parameterless) to access it.
    /// </summary>
    public Navigation.NavigationHandle<TRoute> UseNavigation<TRoute>(TRoute initial) where TRoute : notnull
    {
        var stackRef = UseRef<Navigation.NavigationStack<TRoute>?>(null);
        if (stackRef.Current is null)
            stackRef.Current = new Navigation.NavigationStack<TRoute>(initial);

        var handleRef = UseRef<Navigation.NavigationHandle<TRoute>?>(null);
        if (handleRef.Current is null)
            handleRef.Current = new Navigation.NavigationHandle<TRoute>(stackRef.Current);

        // Capture the latest rerender callback every render so navigation mutations
        // that originate from event handlers always trigger a re-render of this component.
        stackRef.Current.OnChanged = _requestRerender;

        return handleRef.Current;
    }

    /// <summary>
    /// Child mode: retrieves an ancestor's <see cref="Navigation.NavigationHandle{TRoute}"/>
    /// from context. Throws if no ancestor provides one (i.e., no root <c>UseNavigation</c>
    /// with a <c>NavigationHost</c> exists above this component in the tree).
    /// </summary>
    public Navigation.NavigationHandle<TRoute> UseNavigation<TRoute>() where TRoute : notnull
    {
        var handle = UseContext(Navigation.NavigationContext<TRoute>.Instance);
        if (handle is null)
            throw new InvalidOperationException(
                $"UseNavigation<{typeof(TRoute).Name}>() (child mode) found no ancestor NavigationHost " +
                $"providing NavigationContext<{typeof(TRoute).Name}>. " +
                "Ensure a parent component calls UseNavigation<T>(initialRoute) and renders a NavigationHost.");
        return handle;
    }

    // ════════════════════════════════════════════════════════════════
    //  Navigation system back button
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Subscribes to Alt+Left and VirtualKey.GoBack keyboard events on the given window's content
    /// to call <see cref="Navigation.NavigationHandle{TRoute}.GoBack"/>. Unsubscribes on unmount.
    /// </summary>
    public void UseSystemBackButton<TRoute>(
        Navigation.NavigationHandle<TRoute> nav,
        Microsoft.UI.Xaml.Window window) where TRoute : notnull
    {
        UseEffect(() =>
        {
            void handler(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
            {
                if (e.Key == Windows.System.VirtualKey.GoBack ||
                    (e.Key == Windows.System.VirtualKey.Left &&
                     Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu)
                         .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down)))
                {
                    if (nav.CanGoBack)
                    {
                        nav.GoBack();
                        e.Handled = true;
                    }
                }
            }

            if (window.Content is Microsoft.UI.Xaml.UIElement rootElement)
            {
                rootElement.KeyDown += handler;
                return () => rootElement.KeyDown -= handler;
            }
            return () => { };
        }, nav, window);
    }

    // ════════════════════════════════════════════════════════════════
    //  Navigation lifecycle hooks
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Registers lifecycle callbacks that fire during navigation events.
    /// <list type="bullet">
    /// <item><c>onNavigatedTo</c> — fires after this page becomes active.</item>
    /// <item><c>onNavigatingFrom</c> — fires before navigating away. Call <c>ctx.Cancel()</c> to block.</item>
    /// <item><c>onNavigatedFrom</c> — fires after this page is no longer active.</item>
    /// </list>
    /// Callbacks are always updated to the latest references on every render.
    /// </summary>
    public void UseNavigationLifecycle(
        Action<Navigation.NavigatedToContext>? onNavigatedTo = null,
        Action<Navigation.NavigatingFromContext>? onNavigatingFrom = null,
        Action<Navigation.NavigatedFromContext>? onNavigatedFrom = null)
    {
        if (_hookIndex >= _hooks.Count)
        {
            _hooks.Add(new NavigationLifecycleHookState());
        }

        if (_hooks[_hookIndex] is not NavigationLifecycleHookState hook)
            throw new InvalidOperationException(
                $"Hook at index {_hookIndex} is {_hooks[_hookIndex].GetType().Name}, expected NavigationLifecycleHookState. " +
                "Hooks must be called in the same order every render.");
        _hookIndex++;

        // Always update to latest callbacks so closures capture current state
        hook.OnNavigatedTo = onNavigatedTo;
        hook.OnNavigatingFrom = onNavigatingFrom;
        hook.OnNavigatedFrom = onNavigatedFrom;
    }

    /// <summary>
    /// Returns the navigation lifecycle hook state if one was registered, or null.
    /// Used by the reconciler to collect lifecycle callbacks from a component tree.
    /// </summary>
    internal NavigationLifecycleHookState? GetNavigationLifecycleHook()
    {
        for (int i = 0; i < _hooks.Count; i++)
        {
            if (_hooks[i] is NavigationLifecycleHookState hook)
                return hook;
        }
        return null;
    }

    // ════════════════════════════════════════════════════════════════
    //  Context hooks
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reads the nearest ancestor's provided value for the given context.
    /// Returns the context's DefaultValue if no provider exists in the ancestor chain.
    /// Follows hook rules — must be called in the same order every render.
    /// </summary>
    public T UseContext<T>(DuctContext<T> context)
    {
        if (_hookIndex >= _hooks.Count)
        {
            _hooks.Add(new ContextHookState { Context = context });
        }

        if (_hooks[_hookIndex] is not ContextHookState hook)
            throw new InvalidOperationException(
                $"Hook at index {_hookIndex} is {_hooks[_hookIndex].GetType().Name}, expected ContextHookState (UseContext). " +
                "Hooks must be called in the same order every render.");
        _hookIndex++;

        var value = _contextScope is not null
            ? _contextScope.Read(context)
            : context.DefaultValue;
        hook.LastValue = value;
        return value;
    }

    /// <summary>
    /// Enumerates ContextHookState entries for memo change detection (Phase 3).
    /// </summary>
    internal IEnumerable<ContextHookState> ContextHooks
    {
        get
        {
            for (int i = 0; i < _hooks.Count; i++)
            {
                if (_hooks[i] is ContextHookState ctx)
                    yield return ctx;
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  Localization hooks
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns an IntlAccessor for the current locale. Re-renders this component
    /// when the locale changes via a parent LocaleProvider.
    /// If no LocaleProvider is present, returns a default accessor using the OS locale.
    /// Uses DuctContext internally — the context system handles re-renders automatically.
    /// </summary>
    public Localization.IntlAccessor UseIntl()
    {
        var contextAccessor = UseContext(Localization.IntlContexts.Locale);
        return contextAccessor ?? _defaultAccessor.Value;
    }

    private static readonly Lazy<Localization.IntlAccessor> _defaultAccessor = new(() =>
    {
        var osLocale = System.Globalization.CultureInfo.CurrentUICulture.Name;
        if (string.IsNullOrEmpty(osLocale)) osLocale = "en-US";
        var cache = new Localization.MessageCache();
        var provider = new Localization.ReswResourceProvider(osLocale);
        return new Localization.IntlAccessor(osLocale, provider, cache, osLocale);
    });

    // ════════════════════════════════════════════════════════════════
    //  Command hooks
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Processes a DuctCommand for use in a component. For sync-only commands, returns
    /// the command unchanged (no hook slots consumed). For async commands, wraps ExecuteAsync
    /// with automatic IsExecuting tracking and re-entrance guards. The returned command
    /// always has a sync Execute action and ExecuteAsync = null.
    /// </summary>
    public DuctCommand UseCommand(DuctCommand command)
    {
        // Sync-only commands pass through unchanged — no hooks consumed
        if (command.ExecuteAsync is null)
            return command;

        var (isExecuting, setIsExecuting) = UseState(false);
        var asyncAction = command.ExecuteAsync;

        var wrappedExecute = UseMemo<Action>(() => () =>
        {
            // Re-entrance guard: don't start if already executing
            if (isExecuting) return;
            setIsExecuting(true);
            _ = Task.Run(async () =>
            {
                try
                {
                    await asyncAction();
                }
                finally
                {
                    setIsExecuting(false);
                }
            });
        }, command.ExecuteAsync, isExecuting);

        return command with { Execute = wrappedExecute, ExecuteAsync = null, IsExecuting = isExecuting };
    }

    /// <summary>
    /// Processes a parameterized DuctCommand for use in a component. For sync-only commands,
    /// returns unchanged. For async commands, wraps ExecuteAsync with IsExecuting tracking
    /// and re-entrance guards.
    /// </summary>
    public DuctCommand<T> UseCommand<T>(DuctCommand<T> command)
    {
        if (command.ExecuteAsync is null)
            return command;

        var (isExecuting, setIsExecuting) = UseState(false);
        var asyncAction = command.ExecuteAsync;

        var wrappedExecute = UseMemo<Action<T>>(() => (arg) =>
        {
            if (isExecuting) return;
            setIsExecuting(true);
            _ = Task.Run(async () =>
            {
                try
                {
                    await asyncAction(arg);
                }
                finally
                {
                    setIsExecuting(false);
                }
            });
        }, command.ExecuteAsync, isExecuting);

        return command with { Execute = wrappedExecute, ExecuteAsync = null, IsExecuting = isExecuting };
    }

    // ════════════════════════════════════════════════════════════════
    //  Responsive layout hooks
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns (width, height) of the given window and re-renders when the window resizes.
    /// </summary>
    public (double Width, double Height) UseWindowSize(Microsoft.UI.Xaml.Window window)
    {
        var (size, setSize) = UseState((window.Bounds.Width, window.Bounds.Height));

        UseEffect(() =>
        {
            void handler(object sender, Microsoft.UI.Xaml.WindowSizeChangedEventArgs args)
            {
                setSize((args.Size.Width, args.Size.Height));
            }
            window.SizeChanged += handler;
            return () => window.SizeChanged -= handler;
        }, window);

        return size;
    }

    /// <summary>
    /// Returns true when the given window's width is >= minWidth.
    /// Re-renders when the window resizes across the breakpoint.
    /// </summary>
    public bool UseBreakpoint(Microsoft.UI.Xaml.Window window, double minWidth)
    {
        var (width, _) = UseWindowSize(window);
        return width >= minWidth;
    }

    internal void FlushEffects()
    {
        // Phase 1: Run all pending cleanups from previous effects
        for (int i = 0; i < _hooks.Count; i++)
        {
            if (_hooks[i] is EffectHookState hook && hook.PendingCleanup is not null)
            {
                try
                {
                    hook.PendingCleanup();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Duct] Effect cleanup at index {i} threw: {ex}");
                }
                hook.PendingCleanup = null;
            }
        }

        // Phase 2: Run all pending new effects
        for (int i = 0; i < _hooks.Count; i++)
        {
            if (_hooks[i] is not EffectHookState hook || !hook.Pending) continue;
            hook.Pending = false;

            try
            {
                if (hook.EffectWithCleanup is not null)
                {
                    hook.Cleanup = hook.EffectWithCleanup();
                    hook.EffectWithCleanup = null;
                }
                else if (hook.Effect is not null)
                {
                    hook.Effect();
                    hook.Effect = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Duct] Effect at index {i} threw: {ex}");
            }
        }
    }

    internal void RunCleanups()
    {
        // Phase 1: Run effect cleanups
        for (int i = 0; i < _hooks.Count; i++)
        {
            if (_hooks[i] is EffectHookState hook)
            {
                try
                {
                    hook.Cleanup?.Invoke();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Duct] Cleanup at index {i} threw: {ex}");
                }
            }
        }

        // Phase 2: Save persisted state to cache
        for (int i = 0; i < _hooks.Count; i++)
        {
            if (_hooks[i] is PersistedHookStateBase persisted)
            {
                try
                {
                    persisted.SaveToCache();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Duct] Persisted state save at index {i} threw: {ex}");
                }
            }
        }
    }

    private static bool DepsEqual(object[] prev, object[] next)
    {
        if (prev.Length != next.Length) return false;
        for (int i = 0; i < prev.Length; i++)
        {
            if (!Equals(prev[i], next[i])) return false;
        }
        return true;
    }

    internal abstract class HookState { }

    private class ValueHookState<T> : HookState
    {
        public T Value;
        public ValueHookState(T value) => Value = value;
    }

    private class EffectHookState : HookState
    {
        public object[]? Dependencies;
        public Action? Effect;
        public Func<Action>? EffectWithCleanup;
        public Action? Cleanup;
        public Action? PendingCleanup;
        public bool Pending;
    }

    private class MemoHookState<T> : HookState
    {
        public T Value = default!;
        public object[]? Dependencies;
    }

    internal class ContextHookState : HookState
    {
        public DuctContextBase Context = default!;
        public object? LastValue;
    }

    internal class NavigationLifecycleHookState : HookState
    {
        public Action<Navigation.NavigatedToContext>? OnNavigatedTo;
        public Action<Navigation.NavigatingFromContext>? OnNavigatingFrom;
        public Action<Navigation.NavigatedFromContext>? OnNavigatedFrom;
    }

    internal abstract class PersistedHookStateBase : HookState
    {
        public string PersistKey = default!;
        public abstract void SaveToCache();
    }

    private class PersistedHookState<T> : PersistedHookStateBase
    {
        public T Value;
        public PersistedHookState(T value) => Value = value;
        public override void SaveToCache() => PersistedStateCache.Set(PersistKey, Value);
    }
}

/// <summary>
/// A mutable reference that persists across renders (like React's useRef).
/// </summary>
public class Ref<T>
{
    public T Current { get; set; }
    public Ref(T initial) => Current = initial;
}

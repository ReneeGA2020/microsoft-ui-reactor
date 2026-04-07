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

    internal void BeginRender(Action requestRerender)
    {
        _hookIndex = 0;
        _requestRerender = requestRerender;
    }

    /// <summary>
    /// DEBUG ONLY: Directly set a UseState hook value by index and trigger re-render.
    /// Used for testing state changes without event handlers.
    /// </summary>
    internal void UseStateSetterByIndex<T>(int index, T newValue)
    {
        if (index < _hooks.Count)
        {
            _hooks[index].Value = newValue!;
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
            _hooks.Add(new HookState { Value = initialValue! });
        }

        var hook = _hooks[_hookIndex];
        var currentIndex = _hookIndex;
        _hookIndex++;

        if (hook is not HookState || hook is EffectHookState or MemoHookState)
            throw new InvalidOperationException(
                $"Hook at index {currentIndex} is {hook.GetType().Name}, expected HookState (UseState). " +
                "Hooks must be called in the same order every render.");

        T current = (T)hook.Value;

        void Setter(T newValue)
        {
            var h = _hooks[currentIndex];
            if (!EqualityComparer<T>.Default.Equals((T)h.Value, newValue))
            {
                h.Value = newValue!;
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
            _hooks.Add(new HookState { Value = initialValue! });
        }

        var hook = _hooks[_hookIndex];
        var currentIndex = _hookIndex;
        _hookIndex++;

        if (hook is not HookState || hook is EffectHookState or MemoHookState)
            throw new InvalidOperationException(
                $"Hook at index {currentIndex} is {hook.GetType().Name}, expected HookState (UseReducer). " +
                "Hooks must be called in the same order every render.");

        T current = (T)hook.Value;

        void Updater(Func<T, T> reducer)
        {
            var h = _hooks[currentIndex];
            var prev = (T)h.Value;
            var next = reducer(prev);
            if (!EqualityComparer<T>.Default.Equals(prev, next))
            {
                h.Value = next!;
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
            _hooks.Add(new HookState { Value = initialValue! });
        }

        var hook = _hooks[_hookIndex];
        var currentIndex = _hookIndex;
        _hookIndex++;

        if (hook is not HookState || hook is EffectHookState or MemoHookState)
            throw new InvalidOperationException(
                $"Hook at index {currentIndex} is {hook.GetType().Name}, expected HookState (UseReducer). " +
                "Hooks must be called in the same order every render.");

        TState current = (TState)hook.Value;

        void Dispatch(TAction action)
        {
            var h = _hooks[currentIndex];
            var prev = (TState)h.Value;
            var next = reducer(prev, action);
            if (!EqualityComparer<TState>.Default.Equals(prev, next))
            {
                h.Value = next!;
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
            hook.Cleanup?.Invoke();
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
            hook.Cleanup?.Invoke();
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
            _hooks.Add(new MemoHookState { Dependencies = null, Value = default! });
        }

        if (_hooks[_hookIndex] is not MemoHookState hook)
            throw new InvalidOperationException(
                $"Hook at index {_hookIndex} is {_hooks[_hookIndex].GetType().Name}, expected MemoHookState. " +
                "Hooks must be called in the same order every render.");
        _hookIndex++;

        if (hook.Dependencies is null || !DepsEqual(hook.Dependencies, dependencies))
        {
            hook.Value = factory()!;
            hook.Dependencies = dependencies.ToArray();
        }

        return (T)hook.Value;
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
            _hooks.Add(new HookState { Value = new Ref<T>(initialValue) });
        }

        var hook = _hooks[_hookIndex];
        _hookIndex++;
        if (hook.Value is not Ref<T> refValue)
            throw new InvalidOperationException(
                $"Hook at index {_hookIndex - 1} expected Ref<{typeof(T).Name}>, got {hook.Value?.GetType().Name ?? "null"}. " +
                "Hooks must be called in the same order every render.");
        return refValue;
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
    //  Localization hooks
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns an IntlAccessor for the current locale. Re-renders this component
    /// when the locale changes via a parent LocaleProvider.
    /// If no LocaleProvider is present, returns a default accessor using the OS locale.
    /// </summary>
    public Localization.IntlAccessor UseIntl()
    {
        var (_, forceRender) = UseReducer(false);

        var ctx = Localization.LocaleContext.Current;
        var accessor = ctx?.Accessor ?? _defaultAccessor.Value;

        UseEffect(() =>
        {
            if (ctx is null) return () => { };

            void handler() => forceRender(v => !v);
            ctx.Subscribe(handler);
            return () => ctx.Unsubscribe(handler);
        }, ctx?.Accessor.Locale ?? "");

        return accessor;
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

    private class HookState
    {
        public object Value = default!;
    }

    private class EffectHookState : HookState
    {
        public object[]? Dependencies;
        public Action? Effect;
        public Func<Action>? EffectWithCleanup;
        public Action? Cleanup;
        public bool Pending;
    }

    private class MemoHookState : HookState
    {
        public object[]? Dependencies;
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

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

        var hook = (EffectHookState)_hooks[_hookIndex];
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

        var hook = (EffectHookState)_hooks[_hookIndex];
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

        var hook = (MemoHookState)_hooks[_hookIndex];
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
        return (Ref<T>)hook.Value;
    }

    // ════════════════════════════════════════════════════════════════
    //  Observable interop hooks
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Subscribes to an INotifyPropertyChanged source and re-renders when any property changes.
    /// Returns the same source object.
    /// </summary>
    public T UseObservable<T>(T source) where T : System.ComponentModel.INotifyPropertyChanged
    {
        var (_, forceRender) = UseReducer(0);
        UseEffect(() =>
        {
            void handler(object? s, System.ComponentModel.PropertyChangedEventArgs e)
                => forceRender(v => v + 1);
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
        var (_, forceRender) = UseReducer(0);
        UseEffect(() =>
        {
            void handler(object? s, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName == propertyName || string.IsNullOrEmpty(e.PropertyName))
                    forceRender(v => v + 1);
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
        var (_, forceRender) = UseReducer(0);
        UseEffect(() =>
        {
            void handler(object? s, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
                => forceRender(v => v + 1);
            collection.CollectionChanged += handler;
            return () => collection.CollectionChanged -= handler;
        }, collection);
        return collection;
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
            // Set initial size
            setSize((window.Bounds.Width, window.Bounds.Height));
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
        foreach (var hook in _hooks.OfType<EffectHookState>())
        {
            if (!hook.Pending) continue;
            hook.Pending = false;

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
    }

    internal void RunCleanups()
    {
        foreach (var hook in _hooks.OfType<EffectHookState>())
        {
            hook.Cleanup?.Invoke();
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

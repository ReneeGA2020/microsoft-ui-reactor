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

    // ── Hook convenience methods (delegate to Context) ─────────────

    protected (T Value, Action<T> Set) UseState<T>(T initialValue)
        => Context.UseState(initialValue);

    protected (T Value, Action<Func<T, T>> Update) UseReducer<T>(T initialValue)
        => Context.UseReducer(initialValue);

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
}

namespace Duct.Core;

/// <summary>
/// Stores complex CLR values (strings, delegates, brushes) that can't be represented
/// as a simple hash. TreeSerializer stores the registry ID in ViewProp.ValueHash;
/// a future patch applicator retrieves the actual object by ID.
/// </summary>
public sealed class PropValueRegistry
{
    private readonly List<object> _values = new();

    /// <summary>
    /// Register a value and return its monotonic ID (starting at 1; 0 = null sentinel).
    /// No deduplication — delegate equality is unreliable.
    /// </summary>
    public ulong Register(object value)
    {
        _values.Add(value);
        return (ulong)_values.Count; // 1-based
    }

    /// <summary>
    /// O(1) lookup by ID. Returns null for ID 0 (sentinel) or out-of-range.
    /// </summary>
    public object? Retrieve(ulong id)
    {
        if (id == 0 || id > (ulong)_values.Count) return null;
        return _values[(int)(id - 1)];
    }

    /// <summary>
    /// Reset between serialization passes.
    /// </summary>
    public void Clear()
    {
        _values.Clear();
    }

    /// <summary>
    /// Starts a scoped serialization pass. The registry is cleared at the start
    /// so that IDs from a previous pass don't leak. The returned scope is a
    /// disposable marker that enforces the "one pass at a time" contract via
    /// the type system. Disposing the scope is a no-op — the registry retains
    /// its values so consumers can retrieve them by ID after serialization.
    /// </summary>
    public RegistryScope BeginPass()
    {
        Clear();
        return new RegistryScope(this);
    }

    public readonly struct RegistryScope : IDisposable
    {
        private readonly PropValueRegistry _registry;
        internal RegistryScope(PropValueRegistry registry) => _registry = registry;
        public void Dispose() { /* Values remain readable after the pass ends. */ }
    }
}

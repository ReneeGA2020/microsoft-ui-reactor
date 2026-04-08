using System.Text.RegularExpressions;

namespace Duct.Core.Navigation;

/// <summary>
/// Provides typed access to parameters extracted from a URI pattern match.
/// </summary>
public sealed class RouteArgs
{
    private readonly Dictionary<string, string> _values;

    internal RouteArgs(Dictionary<string, string> values)
    {
        _values = values;
    }

    /// <summary>
    /// Gets a parameter value by name, converted to the specified type.
    /// Supports <c>int</c>, <c>long</c>, <c>bool</c>, <c>Guid</c>, and <c>string</c>.
    /// </summary>
    public T Get<T>(string name)
    {
        if (!_values.TryGetValue(name, out var raw))
            throw new KeyNotFoundException($"Parameter '{name}' not found in route args.");

        var type = typeof(T);
        object result = type switch
        {
            _ when type == typeof(string) => raw,
            _ when type == typeof(int) => int.Parse(raw),
            _ when type == typeof(long) => long.Parse(raw),
            _ when type == typeof(bool) => bool.Parse(raw),
            _ when type == typeof(Guid) => Guid.Parse(raw),
            _ => throw new NotSupportedException($"RouteArgs.Get<{type.Name}> is not supported."),
        };
        return (T)result;
    }

    /// <summary>
    /// Returns the raw string value of a parameter, or null if not found.
    /// </summary>
    public string? GetString(string name) =>
        _values.TryGetValue(name, out var v) ? v : null;
}

/// <summary>
/// Result of a deep link resolution attempt.
/// </summary>
public readonly struct DeepLinkResult<TRoute>
{
    /// <summary>The matched routes (current + optional synthetic back stack).</summary>
    public TRoute[] Routes { get; init; }

    /// <summary>True if the URI matched a registered pattern.</summary>
    public bool Matched { get; init; }
}

/// <summary>
/// Maps URI patterns to route constructors for deep linking.
/// Patterns use <c>/segment/{param:type}</c> syntax where type is <c>int</c> or <c>string</c>.
/// </summary>
public sealed class DeepLinkMap<TRoute> where TRoute : notnull
{
    private readonly List<(Regex Pattern, string[] ParamNames, Func<RouteArgs, TRoute> Factory, Func<TRoute[]>? BackStackFactory)> _routes = new();

    /// <summary>
    /// Registers a URI pattern mapped to a route factory.
    /// Pattern syntax: <c>/segment/{param}</c> or <c>/segment/{param:int}</c>.
    /// </summary>
    public DeepLinkMap<TRoute> Map(string pattern, Func<RouteArgs, TRoute> factory)
    {
        var (regex, paramNames) = CompilePattern(pattern);
        _routes.Add((regex, paramNames, factory, null));
        return this;
    }

    /// <summary>
    /// Registers a URI pattern with a synthetic back stack.
    /// The back stack factory provides routes that appear "behind" the deep-linked route.
    /// </summary>
    public DeepLinkMap<TRoute> Map(string pattern, Func<RouteArgs, TRoute> factory, Func<TRoute[]> backStackFactory)
    {
        var (regex, paramNames) = CompilePattern(pattern);
        _routes.Add((regex, paramNames, factory, backStackFactory));
        return this;
    }

    /// <summary>
    /// Resolves a URI to a route (and optional back stack).
    /// Returns <see cref="DeepLinkResult{TRoute}.Matched"/> = false if no pattern matches.
    /// </summary>
    public DeepLinkResult<TRoute> Resolve(Uri uri)
    {
        return Resolve(uri.AbsolutePath);
    }

    /// <summary>
    /// Resolves a URI path string to a route.
    /// </summary>
    public DeepLinkResult<TRoute> Resolve(string path)
    {
        // Normalize: remove trailing slash
        path = path.TrimEnd('/');
        if (string.IsNullOrEmpty(path))
            path = "/";

        foreach (var (pattern, paramNames, factory, backStackFactory) in _routes)
        {
            var match = pattern.Match(path);
            if (!match.Success)
                continue;

            var values = new Dictionary<string, string>();
            for (int i = 0; i < paramNames.Length; i++)
            {
                values[paramNames[i]] = match.Groups[i + 1].Value;
            }

            var route = factory(new RouteArgs(values));

            if (backStackFactory is not null)
            {
                var backStack = backStackFactory();
                var all = new TRoute[backStack.Length + 1];
                Array.Copy(backStack, all, backStack.Length);
                all[^1] = route;
                return new DeepLinkResult<TRoute> { Routes = all, Matched = true };
            }

            return new DeepLinkResult<TRoute> { Routes = new[] { route }, Matched = true };
        }

        return new DeepLinkResult<TRoute> { Routes = Array.Empty<TRoute>(), Matched = false };
    }

    private static (Regex Regex, string[] ParamNames) CompilePattern(string pattern)
    {
        var paramNames = new List<string>();
        // Match {name} or {name:type} placeholders
        var regexPattern = Regex.Replace(pattern, @"\{(\w+)(?::(\w+))?\}", m =>
        {
            var name = m.Groups[1].Value;
            var type = m.Groups[2].Success ? m.Groups[2].Value : "string";
            paramNames.Add(name);

            return type switch
            {
                "int" => @"(\d+)",
                "long" => @"(\d+)",
                "bool" => @"(true|false)",
                "guid" => @"([0-9a-fA-F\-]+)",
                _ => @"([^/]+)", // string (default)
            };
        });

        return (new Regex($"^{regexPattern}$", RegexOptions.Compiled | RegexOptions.IgnoreCase), paramNames.ToArray());
    }
}

using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hooks;

namespace Microsoft.UI.Reactor.Data;

/// <summary>
/// Adapter bridging <see cref="IDataSource{T}"/> and <c>UseInfiniteResource</c>. Lets
/// any existing data source plug into the hook surface without modification.
/// </summary>
public static class DataSourceResourceExtensions
{
    /// <summary>
    /// Projects an <see cref="IDataSource{T}"/> through <c>UseInfiniteResource</c>,
    /// surfacing its pages as an <see cref="InfiniteResource{T}"/>. Deps are identity-based
    /// on <paramref name="source"/> and value-based on the request descriptors, so a new
    /// sort/filter/search restarts the pagination cleanly.
    /// </summary>
    public static InfiniteResource<T> UseDataSource<T>(
        this RenderContext ctx,
        IDataSource<T> source,
        DataRequest request,
        QueryCache cache,
        InfiniteResourceOptions? options = null,
        IHookDispatcher? dispatcher = null)
    {
        return ctx.UseInfiniteResource<T, string>(
            fetchPage: async (cursor, ct) =>
            {
                var req = request with { ContinuationToken = cursor };
                var page = await source.GetPageAsync(req, ct).ConfigureAwait(false);
                return new Page<T, string>(page.Items, page.ContinuationToken, page.TotalCount);
            },
            cache: cache,
            deps: new object[] { source, request.Sort ?? (object)"", request.Filters ?? (object)"", request.SearchQuery ?? "" },
            options: options,
            dispatcher: dispatcher);
    }

    /// <summary>
    /// Overload that reads the ambient <see cref="QueryCache"/> from
    /// <see cref="AppContexts.QueryCache"/>. <see cref="Hosting.ReactorHost"/> installs a
    /// process-wide default cache at startup; tests or subtrees may override it via
    /// <c>.Provide(AppContexts.QueryCache, customCache)</c>.
    /// </summary>
    public static InfiniteResource<T> UseDataSource<T>(
        this RenderContext ctx,
        IDataSource<T> source,
        DataRequest request,
        InfiniteResourceOptions? options = null,
        IHookDispatcher? dispatcher = null)
        => UseDataSource(ctx, source, request, ctx.UseContext(AppContexts.QueryCache), options, dispatcher);
}

using Duct.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Duct;

/// <summary>
/// A WinUI ContentControl that hosts a Duct component tree.
/// Place this inside any XAML page to embed Duct-rendered UI.
/// Each instance owns its own Reconciler and render loop.
/// </summary>
public sealed class DuctHostControl : ContentControl, IDisposable
{
    private readonly Reconciler _reconciler;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly IDuctLogger _logger;

    private Component? _rootComponent;
    private Func<RenderContext, Element>? _rootRenderFunc;
    private RenderContext? _funcContext;

    private Element? _currentTree;
    private UIElement? _currentControl;
    private bool _renderPending;
    private bool _isRendering;
    private bool _needsRerender;
    private bool _disposed;

    /// <summary>
    /// Factory to create the root component. Set this or use Mount() for more control.
    /// Example: ComponentFactory = () => new MyComponent();
    /// </summary>
    public Func<Component>? ComponentFactory { get; set; }

    /// <summary>
    /// Optional props to pass to the root component.
    /// </summary>
    public object? Props { get; set; }

    /// <summary>
    /// Provides access to the underlying reconciler for RegisterType calls.
    /// </summary>
    public Reconciler Reconciler => _reconciler;

    public DuctHostControl(IDuctLogger? logger = null)
    {
        _logger = logger ?? new DebugDuctLogger();
        _reconciler = new Reconciler(_logger);
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>
    /// Mount a Component instance directly.
    /// </summary>
    public void Mount(Component component)
    {
        _rootComponent = component;
        RequestRender();
    }

    /// <summary>
    /// Mount a function component.
    /// </summary>
    public void Mount(Func<RenderContext, Element> renderFunc)
    {
        _rootRenderFunc = renderFunc;
        _funcContext = new RenderContext();
        RequestRender();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_rootComponent is not null || _rootRenderFunc is not null)
            return; // Already mounted via Mount()

        if (ComponentFactory is null)
            return;

        var component = ComponentFactory();

        if (Props is not null && component is IPropsReceiver receiver)
            receiver.SetProps(Props);

        Mount(component);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Dispose();
    }

    private void RequestRender()
    {
        if (_disposed) return;

        if (_isRendering)
        {
            _needsRerender = true;
            return;
        }

        if (_renderPending) return;
        _renderPending = true;

        _dispatcherQueue.TryEnqueue(RenderLoop);
    }

    private const int MaxRenderIterations = 50;

    private void RenderLoop()
    {
        if (_disposed) return;

        if (_isRendering)
        {
            _needsRerender = true;
            _renderPending = false;
            return;
        }

        _renderPending = false;
        int iteration = 0;

        do
        {
            _needsRerender = false;
            Render();
            if (++iteration >= MaxRenderIterations)
            {
                _logger.Log(DuctLogLevel.Warning, "Maximum re-render limit exceeded — possible infinite loop in component state.");
                break;
            }
        }
        while (_needsRerender && !_disposed);
    }

    private void Render()
    {
        _isRendering = true;
        try
        {
            Element? newTree = null;

            if (_rootComponent is not null)
            {
                _rootComponent.Context.BeginRender(RequestRender);
                newTree = _rootComponent.Render();
                _rootComponent.Context.FlushEffects();
            }
            else if (_rootRenderFunc is not null && _funcContext is not null)
            {
                _funcContext.BeginRender(RequestRender);
                newTree = _rootRenderFunc(_funcContext);
                _funcContext.FlushEffects();
            }

            if (newTree is null) return;

            var newControl = _reconciler.Reconcile(
                _currentTree,
                newTree,
                _currentControl,
                RequestRender
            );

            if (newControl != _currentControl)
            {
                Content = newControl;
            }

            _currentControl = newControl;
            _currentTree = newTree;
        }
        catch (Exception ex)
        {
            _logger.Log(DuctLogLevel.Error, "Render FAILED", ex);
            throw;
        }
        finally
        {
            _isRendering = false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;

        _rootComponent?.Context.RunCleanups();
        _funcContext?.RunCleanups();
        _reconciler.Dispose();
        _rootComponent = null;
        _rootRenderFunc = null;
        _funcContext = null;
        _currentTree = null;
        _currentControl = null;
        Content = null;
    }
}

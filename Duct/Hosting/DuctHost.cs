using Duct.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace Duct;

/// <summary>
/// Hosts a Duct component tree inside a WinUI Window.
/// Manages the render loop: when state changes, re-renders the component
/// and reconciles the virtual tree against the real WinUI control tree.
/// </summary>
public sealed class DuctHost
{
    private readonly Window _window;
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

    /// <summary>
    /// Controls which diffing engine is used. Set to CSharpFallback or NativeDiffTree for A/B testing.
    /// </summary>
    public ReconcileMode ReconcileMode
    {
        get => _reconciler.Mode;
        set => _reconciler.Mode = value;
    }

    public DuctHost(Window window, IDuctLogger? logger = null)
    {
        _logger = logger ?? new DebugDuctLogger();
        _reconciler = new Reconciler(_logger);
        _window = window;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    public void Mount(Component component)
    {
        _rootComponent = component;
        RequestRender();
    }

    public void Mount(Func<RenderContext, Element> renderFunc)
    {
        _rootRenderFunc = renderFunc;
        _funcContext = new RenderContext();
        RequestRender();
    }

    internal void RequestRender()
    {
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
        while (_needsRerender);
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
                try
                {
                    newTree = _rootComponent.Render();
                }
                catch (Exception ex)
                {
                    _logger.Log(DuctLogLevel.Error, "Component Render() threw", ex);
                    ShowErrorFallback(ex);
                    return;
                }
                _rootComponent.Context.FlushEffects();
            }
            else if (_rootRenderFunc is not null && _funcContext is not null)
            {
                _funcContext.BeginRender(RequestRender);
                try
                {
                    newTree = _rootRenderFunc(_funcContext);
                }
                catch (Exception ex)
                {
                    _logger.Log(DuctLogLevel.Error, "Function component threw", ex);
                    ShowErrorFallback(ex);
                    return;
                }
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
                _window.Content = newControl;
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

    private void ShowErrorFallback(Exception ex)
    {
        var errorPanel = new WinUI.Border
        {
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 255, 0, 0)),
            BorderThickness = new Thickness(2),
            Padding = new Thickness(16),
            Child = new WinUI.TextBlock
            {
                Text = $"Render error: {ex.GetType().Name}: {ex.Message}",
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                IsTextSelectionEnabled = true,
            }
        };
        _window.Content = errorPanel;
        _currentControl = errorPanel;
        _currentTree = null;
    }
}

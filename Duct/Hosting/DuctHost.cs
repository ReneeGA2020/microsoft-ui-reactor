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
    private readonly Reconciler _reconciler = new();
    private readonly DispatcherQueue _dispatcherQueue;

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

    public DuctHost(Window window)
    {
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

    private void RequestRender()
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

    private void RenderLoop()
    {
        if (_isRendering)
        {
            _needsRerender = true;
            _renderPending = false;
            return;
        }

        _renderPending = false;

        do
        {
            _needsRerender = false;
            Render();
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
                null,
                childIndex: 0,
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
            System.Diagnostics.Debug.WriteLine($"[DuctHost] Render FAILED: {ex}");
            throw; // re-throw so the debugger still breaks
        }
        finally
        {
            _isRendering = false;
        }
    }
}

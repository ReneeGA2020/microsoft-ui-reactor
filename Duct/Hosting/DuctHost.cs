using System.Diagnostics;
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
    private bool _themeListenerAttached;

    // Render phase timing instrumentation
    private readonly Stopwatch _phaseSw = new();
    private double _treeBuildSum;
    private double _reconcileSum;
    private double _effectsSum;
    private int _renderCount;
    private readonly Stopwatch _reportClock = Stopwatch.StartNew();

    /// <summary>
    /// Provides access to the underlying reconciler for RegisterType calls.
    /// </summary>
    public Reconciler Reconciler => _reconciler;

    /// <summary>
    /// The WinUI Window hosting this Duct tree.
    /// Useful for obtaining the HWND (e.g., for file pickers in unpackaged apps).
    /// </summary>
    public Window Window => _window;

    public DuctHost(Window window, IDuctLogger? logger = null)
    {
        _logger = logger ?? new DebugDuctLogger();
        _reconciler = new Reconciler(_logger);
        _window = window;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        DuctApp.ActiveHost = this;
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

            _phaseSw.Restart();

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
            }

            double treeBuildMs = _phaseSw.Elapsed.TotalMilliseconds;

            if (newTree is null) return;

            _phaseSw.Restart();

            var newControl = _reconciler.Reconcile(
                _currentTree,
                newTree,
                _currentControl,
                RequestRender
            );

            if (newControl != _currentControl)
            {
                _window.Content = newControl;
                AttachThemeListener(newControl);
            }

            _currentControl = newControl;
            _currentTree = newTree;

            double reconcileMs = _phaseSw.Elapsed.TotalMilliseconds;

            _phaseSw.Restart();

            if (_rootComponent is not null)
                _rootComponent.Context.FlushEffects();
            else if (_funcContext is not null)
                _funcContext.FlushEffects();

            double effectsMs = _phaseSw.Elapsed.TotalMilliseconds;

            // Accumulate and report every ~1 second
            _treeBuildSum += treeBuildMs;
            _reconcileSum += reconcileMs;
            _effectsSum += effectsMs;
            _renderCount++;

            if (_reportClock.Elapsed.TotalSeconds >= 1.0 && _renderCount > 0)
            {
                var line = $"PERF [{_renderCount} renders]: tree={_treeBuildSum / _renderCount:F2}ms  reconcile={_reconcileSum / _renderCount:F2}ms  effects={_effectsSum / _renderCount:F2}ms  total={(_treeBuildSum + _reconcileSum + _effectsSum) / _renderCount:F2}ms";
                System.Diagnostics.Debug.WriteLine(line);
#if DEBUG
                _logger.Log(DuctLogLevel.Debug, line);
#endif
                _treeBuildSum = 0;
                _reconcileSum = 0;
                _effectsSum = 0;
                _renderCount = 0;
                _reportClock.Restart();
            }
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

    /// <summary>
    /// Subscribes to ActualThemeChanged on the root content element so that
    /// ThemeRef-bound properties are re-resolved when the theme switches.
    /// WinUI controls handle theme changes natively via {ThemeResource} bindings,
    /// but Duct's ThemeRef values are resolved once during reconciliation —
    /// this listener triggers a re-render so they pick up the new theme.
    /// </summary>
    private void AttachThemeListener(UIElement? control)
    {
        if (_themeListenerAttached || control is not FrameworkElement fe) return;
        _themeListenerAttached = true;

        fe.ActualThemeChanged += (_, _) =>
        {
            _logger.Log(DuctLogLevel.Debug, $"Theme changed to {fe.ActualTheme} — re-rendering");
            RequestRender();
        };
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

using System.Diagnostics;
using Duct.Animation;
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
public sealed class DuctHost : IDisposable
{
#pragma warning disable CS0414 // Design constant for render-loop limiting; wiring pending
    private static readonly int MaxRenderIterations = 50;
#pragma warning restore CS0414

    private readonly Window _window;
    private readonly Reconciler _reconciler;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly IDuctLogger _logger;

    private Component? _rootComponent;
    private Func<RenderContext, Element>? _rootRenderFunc;
    private RenderContext? _funcContext;

    private Element? _currentTree;
    private UIElement? _currentControl;
    private int _renderPending;    // 0 or 1 — Interlocked for thread-safe access
    private volatile bool _isRendering;     // only touched on UI thread
    private volatile bool _needsRerender;   // only touched on UI thread
    private FrameworkElement? _themeListenerElement;
    private volatile bool _disposed;
    private readonly Windows.Foundation.TypedEventHandler<object, WindowEventArgs> _closedHandler;

    // Captured AnimationScope curve — when a state setter is called inside
    // WithAnimation, the scope is synchronous but the render is async.
    // We capture the curve here so the reconcile pass can restore it.
    private Curve? _pendingAnimationCurve;

    // Render phase timing instrumentation
    private readonly Stopwatch _phaseSw = new();
    private double _treeBuildSum;
    private double _reconcileSum;
    private double _effectsSum;
    private int _renderCount;
    private readonly Stopwatch _reportClock = Stopwatch.StartNew();
    private long _totalRenderCount;

    // Public perf snapshot — updated every ~1 second, readable from components
    private RenderStats _stats;

    /// <summary>
    /// Live render performance snapshot, updated every ~1 second.
    /// Always available (FPS, frame time). DEBUG builds include per-reconcile element counters.
    /// </summary>
    public ref readonly RenderStats Stats => ref _stats;

    /// <summary>
    /// Provides access to the underlying reconciler for RegisterType calls.
    /// </summary>
    public Reconciler Reconciler => _reconciler;

    /// <summary>
    /// Optional callback invoked after each render pass with phase timings (ms):
    /// treeBuildMs, reconcileMs, effectsMs. Used by perf harnesses to capture
    /// the breakdown of a Duct render cycle.
    /// </summary>
    public Action<double, double, double>? OnRenderComplete { get; set; }

    /// <summary>
    /// The WinUI Window hosting this Duct tree.
    /// Useful for obtaining the HWND (e.g., for file pickers in unpackaged apps).
    /// </summary>
    public Window Window => _window;

    /// <summary>
    /// Optional: when set, Duct renders into this Border instead of Window.Content.
    /// Useful for embedding Duct content in a pre-existing layout (e.g., a test harness
    /// with a persistent TitleBar).
    /// </summary>
    public WinUI.Border? ContentTarget { get; set; }

    public DuctHost(Window window, IDuctLogger? logger = null)
    {
        _logger = logger ?? new DebugDuctLogger();
        _reconciler = new Reconciler(_logger);
        _window = window;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        DuctApp.ActiveHost = this;

        // Stop the render loop when the window closes — background threads
        // may still call setState after this, but RequestRender will bail out.
        _closedHandler = (_, _) => Dispose();
        _window.Closed += _closedHandler;
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

    /// <summary>
    /// Thread-safe: can be called from any thread. Coalesces multiple calls into
    /// a single render. At most one RenderLoop is ever pending on the dispatcher.
    ///
    /// During render: setState calls set _needsRerender (no enqueue).
    /// Between renders: first setState CAS-flips _renderPending 0→1 and enqueues.
    /// _renderPending stays 1 throughout the render, blocking duplicate enqueues.
    /// </summary>
    internal void RequestRender()
    {
        if (_disposed) return;

        // Capture ambient animation curve so the async render pass can restore it.
        // Multiple state changes may fire before the render — last curve wins.
        if (AnimationScope.HasScope)
            _pendingAnimationCurve = AnimationScope.Current;

        // During render: just flag — the render loop will re-enqueue after Render().
        if (_isRendering)
        {
            _needsRerender = true;
            return;
        }

        // Between renders: CAS 0→1 gates a single TryEnqueue.
        if (Interlocked.CompareExchange(ref _renderPending, 1, 0) != 0)
        {
            _needsRerender = true;
            return;
        }

        _dispatcherQueue.TryEnqueue(RenderLoop);
    }

    private void RenderLoop()
    {
        if (_disposed) return;

        // _renderPending is 1 here — all concurrent RequestRender calls are
        // blocked from enqueuing duplicates. Render once, then decide.
        _needsRerender = false;
        Render();

        // Reset the gate so future setState calls can enqueue.
        Interlocked.Exchange(ref _renderPending, 0);

        // If state changed during render, re-enqueue at LOW priority so WinUI
        // layout/paint/input (normal priority + WM_PAINT) run first. Without this,
        // high-frequency setState sources cause back-to-back renders that starve the
        // compositor — layout never runs, property sets on dirty elements get
        // progressively slower, and reconcile time blows up non-linearly.
        if (_needsRerender)
        {
            if (Interlocked.CompareExchange(ref _renderPending, 1, 0) == 0)
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, RenderLoop);
        }
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

            // Restore captured animation scope so ApplyModifiers routes through
            // compositor animations instead of direct property sets.
            var capturedCurve = Interlocked.Exchange(ref _pendingAnimationCurve, null);
            if (capturedCurve is not null)
                AnimationScope.PushScope(capturedCurve);

            UIElement? newControl;
            try
            {
                newControl = _reconciler.Reconcile(
                    _currentTree,
                    newTree,
                    _currentControl,
                    RequestRender
                );
            }
            finally
            {
                if (capturedCurve is not null)
                    AnimationScope.PopScope();
            }

            if (newControl != _currentControl)
            {
                if (ContentTarget is not null)
                    ContentTarget.Child = newControl;
                else
                    _window.Content = newControl;
                AttachThemeListener(newControl);
            }

            _currentControl = newControl;
            _currentTree = newTree;

            // Start any connected animations now that the new tree is in the visual tree
            _reconciler.FlushConnectedAnimations();

            double reconcileMs = _phaseSw.Elapsed.TotalMilliseconds;

            _phaseSw.Restart();

            if (_rootComponent is not null)
                _rootComponent.Context.FlushEffects();
            else if (_funcContext is not null)
                _funcContext.FlushEffects();

            double effectsMs = _phaseSw.Elapsed.TotalMilliseconds;

            OnRenderComplete?.Invoke(treeBuildMs, reconcileMs, effectsMs);

#if DEBUG
            _logger.Log(DuctLogLevel.Debug,
                $"RECONCILE: tree={treeBuildMs:F2}ms  reconcile={reconcileMs:F2}ms  effects={effectsMs:F2}ms  total={treeBuildMs + reconcileMs + effectsMs:F2}ms  |  diffed={_reconciler.DebugElementsDiffed}  skipped={_reconciler.DebugElementsSkipped}  created={_reconciler.DebugUIElementsCreated}  modified={_reconciler.DebugUIElementsModified}");
#endif

            // Accumulate and report every ~1 second
            _treeBuildSum += treeBuildMs;
            _reconcileSum += reconcileMs;
            _effectsSum += effectsMs;
            _renderCount++;
            _totalRenderCount++;

            if (_reportClock.Elapsed.TotalSeconds >= 1.0 && _renderCount > 0)
            {
                double avgTree = _treeBuildSum / _renderCount;
                double avgReconcile = _reconcileSum / _renderCount;
                double avgEffects = _effectsSum / _renderCount;
                double avgTotal = avgTree + avgReconcile + avgEffects;

                _stats = new RenderStats
                {
                    Fps = _renderCount / _reportClock.Elapsed.TotalSeconds,
                    RendersInWindow = _renderCount,
                    TotalRenders = _totalRenderCount,
                    AvgTreeBuildMs = avgTree,
                    AvgReconcileMs = avgReconcile,
                    AvgEffectsMs = avgEffects,
                    AvgTotalMs = avgTotal,
#if DEBUG
                    LastDiffed = _reconciler.DebugElementsDiffed,
                    LastSkipped = _reconciler.DebugElementsSkipped,
                    LastCreated = _reconciler.DebugUIElementsCreated,
                    LastModified = _reconciler.DebugUIElementsModified,
#endif
                };

                _logger.Log(DuctLogLevel.Debug,
                    $"PERF [{_renderCount} renders]: tree={avgTree:F2}ms  reconcile={avgReconcile:F2}ms  effects={avgEffects:F2}ms  total={avgTotal:F2}ms");
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
            ShowErrorFallback(ex);
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
        if (_themeListenerElement is not null)
            _themeListenerElement.ActualThemeChanged -= OnActualThemeChanged;

        if (control is not FrameworkElement fe)
        {
            _themeListenerElement = null;
            return;
        }

        _themeListenerElement = fe;
        fe.ActualThemeChanged += OnActualThemeChanged;
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        _logger.Log(DuctLogLevel.Debug, $"Theme changed to {sender.ActualTheme} — re-rendering");
        Duct.Core.Reconciler.ClearStyleCache();
        RequestRender();
    }

    /// <summary>
    /// Awaits until the render loop is idle (no pending or in-flight renders).
    /// Yields to the dispatcher at Low priority in a loop so that Normal-priority
    /// RenderLoop callbacks and Low-priority re-renders all complete before returning.
    /// Used by test harnesses to replace blind Task.Delay waits.
    /// </summary>
    public Task WaitForIdleAsync(int maxYields = 10)
    {
        if (_disposed) return Task.CompletedTask;
        if (_renderPending == 0 && !_isRendering && !_needsRerender)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource();
        int yields = 0;
        void CheckIdle()
        {
            if (_disposed || ++yields > maxYields ||
                (_renderPending == 0 && !_isRendering && !_needsRerender))
            {
                tcs.TrySetResult();
            }
            else
            {
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, CheckIdle);
            }
        }
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, CheckIdle);
        return tcs.Task;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _window.Closed -= _closedHandler;

        // Theme listener touches UI-affine objects — marshal to UI thread if needed.
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (_themeListenerElement is not null)
                _themeListenerElement.ActualThemeChanged -= OnActualThemeChanged;
            _themeListenerElement = null;
        });

        _rootComponent?.Context.RunCleanups();
        _funcContext?.RunCleanups();
        _reconciler.Dispose();
        _rootComponent = null;
        _rootRenderFunc = null;
        _funcContext = null;
        _currentTree = null;
        _currentControl = null;
        DuctApp.ActiveHost = null;
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
        if (ContentTarget is not null)
            ContentTarget.Child = errorPanel;
        else
            _window.Content = errorPanel;
        _currentControl = errorPanel;
        _currentTree = null;
    }
}

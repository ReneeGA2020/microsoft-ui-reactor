using System.Runtime.InteropServices;
using Duct.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace Duct;

/// <summary>
/// Configuration for DuctApp.Run. Scoped as a single record to avoid scattered static fields.
/// </summary>
internal record DuctAppOptions(
    Func<Component>? RootFactory = null,
    Func<RenderContext, Element>? RootRenderFunc = null,
    Action<DuctHost>? Configure = null,
    string WindowTitle = "Duct App",
    int WindowWidth = 1024,
    int WindowHeight = 768,
    bool FullScreen = false);

public static class DuctApp
{
    // Application.Start blocks and creates DuctApplication via parameterless constructor,
    // so we must communicate config through a static. Using a single record keeps this scoped.
    private static DuctAppOptions _options = new();
    internal static DuctAppOptions Options
    {
        get => Volatile.Read(ref _options);
        set => Volatile.Write(ref _options, value);
    }
    public static DuctHost? ActiveHost { get; internal set; }

    // Unpackaged WinUI apps (WindowsPackageType=None) don't inherit DPI awareness from an
    // MSIX manifest, so the process defaults to DPI-unaware and Windows applies blurry bitmap
    // scaling. Setting PerMonitorV2 awareness before any window is created tells the OS the
    // app will handle DPI itself, producing crisp rendering at any scale factor.
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetProcessDpiAwarenessContext(nint value);

    private static readonly nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;

    public static void Run<TRoot>(string title = "Duct App", int width = 1024, int height = 768, bool fullScreen = false, Action<DuctHost>? configure = null)
        where TRoot : Component, new()
    {
        RunOnSta(() =>
        {
            InitProcess();
            Options = new DuctAppOptions(
                RootFactory: () => new TRoot(),
                Configure: configure,
                WindowTitle: title,
                WindowWidth: width,
                WindowHeight: height,
                FullScreen: fullScreen);

            Application.Start(_ =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                new DuctApplication();
            });
        });
    }

    public static void Run(string title, Func<RenderContext, Element> rootRender, int width = 1024, int height = 768, bool fullScreen = false)
    {
        RunOnSta(() =>
        {
            InitProcess();
            Options = new DuctAppOptions(
                RootRenderFunc: rootRender,
                WindowTitle: title,
                WindowWidth: width,
                WindowHeight: height,
                FullScreen: fullScreen);

            Application.Start(_ =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                new DuctApplication();
            });
        });
    }

    private static void InitProcess()
    {
        if (!SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
            System.Diagnostics.Debug.WriteLine($"SetProcessDpiAwarenessContext failed: {Marshal.GetLastWin32Error()}");
        WinRT.ComWrappersSupport.InitializeComWrappers();
    }

    /// <summary>
    /// Ensures the action runs on an STA thread. WinUI 3's DesktopChildSiteBridge requires
    /// STA for UI Automation (screen readers, test tools) to traverse into the XAML island.
    /// Top-level statements and async Main produce MTA threads where [STAThread] cannot be
    /// applied, so we re-launch on a dedicated STA thread when needed.
    /// </summary>
    private static void RunOnSta(Action action)
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            action();
            return;
        }

        // Current thread is MTA — spawn a new STA thread and run there.
        Exception? caught = null;
        var staThread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { caught = ex; }
        });
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
        staThread.Join();
        if (caught is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(caught).Throw();
    }
}

/// <summary>
/// Application subclass that implements IXamlMetadataProvider so the native XAML
/// schema context can resolve managed types from XBF theme resources.
/// No App.xaml needed — XamlControlsResources are loaded programmatically.
/// The IXamlMetadataProvider implementation delegates to the WinUI controls'
/// built-in provider so that custom control types (TextCommandBarFlyout, etc.)
/// can be instantiated from XBF theme resources.
/// </summary>
public partial class DuctApplication : Application, IXamlMetadataProvider
{
    private IXamlMetadataProvider? _controlsProvider;

    private IXamlMetadataProvider ControlsProvider
    {
        get
        {
            if (_controlsProvider == null)
            {
                // Activate the WinUI controls' built-in metadata provider.
                // This knows about all control types (TextCommandBarFlyout, etc.)
                var provider = new Microsoft.UI.Xaml.XamlTypeInfo.XamlControlsXamlMetaDataProvider();
                _controlsProvider = provider;
            }
            return _controlsProvider;
        }
    }

    /// <summary>
    /// Optional callback for unhandled exceptions. If set, called before deciding whether to handle.
    /// Return true to mark the exception as handled; return false (or leave null) to let it crash.
    /// </summary>
    public static Func<Exception, bool>? OnUnhandledException { get; set; }

    private readonly IDuctLogger _logger = new DebugDuctLogger();

    public DuctApplication()
    {
        UnhandledException += (_, e) =>
        {
            _logger.Log(DuctLogLevel.Error, $"UnhandledException: {e.Exception.GetType().Name}: {e.Exception.Message}", e.Exception);
            if (OnUnhandledException is not null)
                e.Handled = OnUnhandledException(e.Exception);
            // Don't set e.Handled = true for unknown exceptions — let the app crash
            // with a useful error rather than silently running in a corrupt state.
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Load WinUI control theme resources programmatically.
        Resources.MergedDictionaries.Add(new XamlControlsResources());

        var opts = DuctApp.Options;
        var window = new Window { Title = opts.WindowTitle };
        if (opts.FullScreen)
            window.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
        else
            window.AppWindow.Resize(new Windows.Graphics.SizeInt32(opts.WindowWidth, opts.WindowHeight)); 

        var host = new DuctHost(window);

        opts.Configure?.Invoke(host);

        if (opts.RootFactory is not null)
        {
            host.Mount(opts.RootFactory());
        }
        else if (opts.RootRenderFunc is not null)
        {
            host.Mount(opts.RootRenderFunc);
        }

        window.Activate();
    }

    // IXamlMetadataProvider — delegates to the WinUI controls' built-in provider
    // so custom control types can be resolved from XBF theme resources.
    public IXamlType GetXamlType(Type type) => ControlsProvider.GetXamlType(type);
    public IXamlType GetXamlType(string fullName) => ControlsProvider.GetXamlType(fullName);
    public XmlnsDefinition[] GetXmlnsDefinitions() => ControlsProvider.GetXmlnsDefinitions();
}

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

    public static void Run<TRoot>(string title = "Duct App", int width = 1024, int height = 768, bool fullScreen = false, bool preview = false, Action<DuctHost>? configure = null)
        where TRoot : Component, new()
    {
        if (preview && TryRunPreview(title, width, height, configure)) return;

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

    public static void Run(string title, Func<RenderContext, Element> rootRender, int width = 1024, int height = 768, bool fullScreen = false, bool preview = false, Action<DuctHost>? configure = null)
    {
        if (preview && TryRunPreview(title, width, height, configure)) return;

        RunOnSta(() =>
        {
            InitProcess();
            Options = new DuctAppOptions(
                RootRenderFunc: rootRender,
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

    /// <summary>
    /// Checks for <c>--preview</c> in the process command-line args.
    /// If found, launches a minimal preview window showing the specified (or first) component.
    /// Works with <c>dotnet watch run -- --preview CounterDemo</c> for hot reload.
    /// With <c>--vscode</c>, starts a capture server with <c>/components</c> and
    /// <c>POST /preview</c> endpoints for live component switching without restart.
    /// Only active when the caller passes <c>preview: true</c>.
    /// </summary>
    private static bool TryRunPreview(string title, int width, int height, Action<DuctHost>? configure)
    {
        var args = Environment.GetCommandLineArgs();

        // --preview-list: output all available component names (one per line) and exit.
        // Supports optional file path for tools: --preview-list C:\temp\components.txt
        var listIdx = Array.IndexOf(args, "--preview-list");
        if (listIdx >= 0)
        {
            var names = FindAllComponentNames().ToList();
            foreach (var name in names)
                Console.WriteLine(name);
            Console.Out.Flush();
            if (listIdx + 1 < args.Length && !args[listIdx + 1].StartsWith("-"))
                File.WriteAllLines(args[listIdx + 1], names);
            return true;
        }

        var idx = Array.IndexOf(args, "--preview");
        if (idx < 0) return false;

        // Component name is optional — if next arg looks like a flag (or is missing), use first found
        string? componentName = null;
        if (idx + 1 < args.Length && !args[idx + 1].StartsWith("-"))
            componentName = args[idx + 1];

        // Resolve the initial component type
        Type? componentType = null;
        if (componentName != null)
        {
            componentType = FindComponentType(componentName);
            if (componentType == null)
            {
                Console.Error.WriteLine($"[preview] Component '{componentName}' not found.");
                Console.Error.WriteLine($"[preview] Available components: {string.Join(", ", FindAllComponentNames())}");
                return true;
            }
        }
        else
        {
            // Default to first available component
            var firstName = FindAllComponentNames().FirstOrDefault();
            if (firstName == null)
            {
                Console.Error.WriteLine("[preview] No Component subclasses found.");
                return true;
            }
            componentType = FindComponentType(firstName)!;
            componentName = firstName;
        }

        bool vscodeMode = args.Contains("--vscode");
        int fps = 10;
        var fpsIdx = Array.IndexOf(args, "--fps");
        if (fpsIdx >= 0 && fpsIdx + 1 < args.Length && int.TryParse(args[fpsIdx + 1], out var parsedFps))
            fps = Math.Clamp(parsedFps, 1, 30);

        Console.WriteLine($"[preview] Previewing {componentType.FullName}");
        Console.WriteLine($"[preview] Hot reload active — edit and save to see changes instantly");
        if (vscodeMode) Console.WriteLine($"[preview] VS Code mode enabled (capture @ {fps} fps)");

        var initialComponentType = componentType;
        var initialComponentName = componentName;
        var captureFps = fps;

        RunOnSta(() =>
        {
            InitProcess();

            Action<DuctHost> combinedConfigure = host =>
            {
                configure?.Invoke(host);

                if (vscodeMode)
                {
                    var server = new PreviewCaptureServer(
                        host.Window.DispatcherQueue,
                        host.Window,
                        captureFps);

                    server.GetComponents = () => FindAllComponentNames().ToList();
                    server.GetCurrentComponent = () => initialComponentName;
                    server.SwitchComponent = name =>
                    {
                        var type = FindComponentType(name);
                        if (type == null) return false;

                        host.Window.DispatcherQueue.TryEnqueue(() =>
                        {
                            var instance = (Core.Component)Activator.CreateInstance(type)!;
                            host.Mount(instance);
                            host.Window.Title = $"Preview — {name}";
                        });

                        // Update the closure so GetCurrentComponent stays correct
                        initialComponentName = name;
                        Console.WriteLine($"[preview] Switched to {type.FullName}");
                        return true;
                    };

                    server.Start();
                }
            };

            Options = new DuctAppOptions(
                RootFactory: () => (Core.Component)Activator.CreateInstance(initialComponentType)!,
                Configure: combinedConfigure,
                WindowTitle: $"Preview — {initialComponentName}",
                WindowWidth: width,
                WindowHeight: height);

            Application.Start(_ =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                new DuctApplication();
            });
        });

        return true;
    }

    /// <summary>
    /// Finds a Component type by name across all loaded assemblies (case-insensitive).
    /// </summary>
    internal static Type? FindComponentType(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (System.Reflection.ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray()!; }
            catch { continue; }

            var match = types.FirstOrDefault(t =>
                string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase) &&
                typeof(Core.Component).IsAssignableFrom(t) &&
                !t.IsAbstract);
            if (match != null) return match;
        }
        return null;
    }

    private static IEnumerable<string> FindAllComponentNames()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch (System.Reflection.ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null)!; } catch { return []; } })
            .Where(t => typeof(Core.Component).IsAssignableFrom(t!) && !t!.IsAbstract && !t.FullName!.StartsWith("Duct."))
            .Select(t => t!.Name)
            .Distinct()
            .OrderBy(n => n);
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

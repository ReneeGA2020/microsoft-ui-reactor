using Duct.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace Duct;

public static class DuctApp
{
    internal static Type? RootComponentType;
    internal static Func<RenderContext, Element>? RootRenderFunc;
    internal static string WindowTitle = "Duct App";
    internal static int WindowWidth = 1024;
    internal static int WindowHeight = 768;
    public static DuctHost? ActiveHost;

    public static void Run<TRoot>(string title = "Duct App", int width = 1024, int height = 768)
        where TRoot : Component, new()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        RootComponentType = typeof(TRoot);
        WindowTitle = title;
        WindowWidth = width;
        WindowHeight = height;

        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new DuctApplication();
        });
    }

    public static void Run(string title, Func<RenderContext, Element> rootRender, int width = 1024, int height = 768)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        RootRenderFunc = rootRender;
        WindowTitle = title;
        WindowWidth = width;
        WindowHeight = height;

        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new DuctApplication();
        });
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
public class DuctApplication : Application, IXamlMetadataProvider
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

    public DuctApplication()
    {
        UnhandledException += (_, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"[Duct] UnhandledException: {e.Exception.GetType().Name}: {e.Exception.Message}");
            e.Handled = true;
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Load WinUI control theme resources programmatically.
        Resources.MergedDictionaries.Add(new XamlControlsResources());

        var window = new Window { Title = DuctApp.WindowTitle };
        window.AppWindow.Resize(new Windows.Graphics.SizeInt32(DuctApp.WindowWidth, DuctApp.WindowHeight));

        var host = new DuctHost(window);
        DuctApp.ActiveHost = host;

        if (DuctApp.RootComponentType is not null)
        {
            var component = (Component)Activator.CreateInstance(DuctApp.RootComponentType)!;
            host.Mount(component);
        }
        else if (DuctApp.RootRenderFunc is not null)
        {
            host.Mount(DuctApp.RootRenderFunc);
        }

        window.Activate();
    }

    // IXamlMetadataProvider — delegates to the WinUI controls' built-in provider
    // so custom control types can be resolved from XBF theme resources.
    public IXamlType GetXamlType(Type type) => ControlsProvider.GetXamlType(type);
    public IXamlType GetXamlType(string fullName) => ControlsProvider.GetXamlType(fullName);
    public XmlnsDefinition[] GetXmlnsDefinitions() => ControlsProvider.GetXmlnsDefinitions();
}

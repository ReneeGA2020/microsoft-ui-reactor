using Duct.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Duct;

/// <summary>
/// A Page subclass that hosts a Duct component tree, enabling Duct pages
/// to participate in Frame-based navigation (e.g., WinUI Gallery).
///
/// Usage:
///   // In page registry:
///   { "ButtonPage", typeof(DuctPage&lt;ButtonPageComponent&gt;) }
///
///   // Navigation parameter is passed as Props (string by default).
/// </summary>
public class DuctPage<TComponent> : Page
    where TComponent : Component, new()
{
    private DuctHostControl? _host;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        _host = new DuctHostControl();

        var component = new TComponent();

        // Pass navigation parameter as props if the component accepts them
        if (e.Parameter is not null)
        {
            var propsProperty = component.GetType().GetProperty("Props");
            propsProperty?.SetValue(component, e.Parameter);
        }

        _host.Mount(component);
        Content = _host;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _host?.Dispose();
        _host = null;
    }
}

/// <summary>
/// A Page subclass that hosts a Duct component with typed props,
/// supporting strongly-typed navigation parameters.
///
/// Usage:
///   { "ButtonPage", typeof(DuctPage&lt;ButtonPageComponent, string&gt;) }
/// </summary>
public class DuctPage<TComponent, TProps> : Page
    where TComponent : Component<TProps>, new()
{
    private DuctHostControl? _host;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        _host = new DuctHostControl();

        var component = new TComponent();

        if (e.Parameter is TProps props)
            component.Props = props;

        _host.Mount(component);
        Content = _host;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _host?.Dispose();
        _host = null;
    }
}

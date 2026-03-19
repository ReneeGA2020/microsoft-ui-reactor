using Duct.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Duct;

/// <summary>
/// Helper methods for hosting Duct components inside XAML Pages that participate
/// in Frame-based navigation.
///
/// IMPORTANT: WinUI's Frame.Navigate requires pages to have XAML metadata
/// (IXamlType registration). Code-only Page subclasses — including generic
/// base classes like DuctPage&lt;T&gt; — crash with a null access violation
/// in ActivationAPI::ActivateInstance because GetXamlTypeNoRef() returns null.
///
/// The correct pattern for Duct pages in Frame navigation:
///
///   1. Create a minimal .xaml file for the page:
///      &lt;Page x:Class="MyApp.Pages.ButtonPage" ... /&gt;
///
///   2. In the code-behind, use DuctPageHelper to mount the component:
///
///      public sealed partial class ButtonPage : Page
///      {
///          private DuctHostControl? _host;
///
///          public ButtonPage() { InitializeComponent(); }
///
///          protected override void OnNavigatedTo(NavigationEventArgs e)
///          {
///              base.OnNavigatedTo(e);
///              _host = DuctPageHelper.Mount&lt;MyComponent&gt;(this, e);
///          }
///
///          protected override void OnNavigatedFrom(NavigationEventArgs e)
///          {
///              base.OnNavigatedFrom(e);
///              DuctPageHelper.Unmount(ref _host);
///          }
///      }
/// </summary>
public static class DuctPageHelper
{
    /// <summary>
    /// Creates a DuctHostControl, mounts the component, and sets it as the page's content.
    /// Passes navigation parameters as props if the component accepts them.
    /// </summary>
    public static DuctHostControl Mount<TComponent>(Page page, NavigationEventArgs e)
        where TComponent : Component, new()
    {
        var host = new DuctHostControl();
        var component = new TComponent();

        if (e.Parameter is not null)
        {
            var propsProperty = component.GetType().GetProperty("Props");
            propsProperty?.SetValue(component, e.Parameter);
        }

        host.Mount(component);
        page.Content = host;
        return host;
    }

    /// <summary>
    /// Creates a DuctHostControl, mounts the component with typed props, and sets it as the page's content.
    /// </summary>
    public static DuctHostControl Mount<TComponent, TProps>(Page page, NavigationEventArgs e)
        where TComponent : Component<TProps>, new()
    {
        var host = new DuctHostControl();
        var component = new TComponent();

        if (e.Parameter is TProps props)
            component.Props = props;

        host.Mount(component);
        page.Content = host;
        return host;
    }

    /// <summary>
    /// Disposes the DuctHostControl and clears the reference.
    /// </summary>
    public static void Unmount(ref DuctHostControl? host)
    {
        host?.Dispose();
        host = null;
    }
}

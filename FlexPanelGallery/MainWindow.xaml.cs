using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FlexPanelGallery;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ContentFrame.Navigate(typeof(Pages.OverviewPage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            var pageType = tag switch
            {
                "Overview" => typeof(Pages.OverviewPage),
                "Direction" => typeof(Pages.DirectionPage),
                "JustifyContent" => typeof(Pages.JustifyContentPage),
                "AlignItems" => typeof(Pages.AlignItemsPage),
                "Wrap" => typeof(Pages.WrapPage),
                "GrowShrink" => typeof(Pages.GrowShrinkPage),
                "Gap" => typeof(Pages.GapPage),
                "AbsolutePosition" => typeof(Pages.AbsolutePositionPage),
                "NestedFlex" => typeof(Pages.NestedFlexPage),
                _ => typeof(Pages.OverviewPage),
            };
            ContentFrame.Navigate(pageType);
        }
    }
}

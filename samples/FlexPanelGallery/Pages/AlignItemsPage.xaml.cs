using Duct.Flex;
using Microsoft.UI.Xaml.Controls;

namespace FlexPanelGallery.Pages;

public sealed partial class AlignItemsPage : Page
{
    public AlignItemsPage()
    {
        InitializeComponent();
    }

    private void AlignCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DemoPanel is null) return;
        if (AlignCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            DemoPanel.AlignItems = tag switch
            {
                "Stretch" => FlexAlign.Stretch,
                "FlexStart" => FlexAlign.FlexStart,
                "Center" => FlexAlign.Center,
                "FlexEnd" => FlexAlign.FlexEnd,
                "Baseline" => FlexAlign.Baseline,
                _ => FlexAlign.Stretch,
            };
        }
    }
}

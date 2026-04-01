using Duct.Flex;
using Microsoft.UI.Xaml.Controls;

namespace FlexPanelGallery.Pages;

public sealed partial class JustifyContentPage : Page
{
    public JustifyContentPage()
    {
        InitializeComponent();
    }

    private void JustifyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DemoPanel is null) return;
        if (JustifyCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            DemoPanel.JustifyContent = tag switch
            {
                "FlexStart" => FlexJustify.FlexStart,
                "Center" => FlexJustify.Center,
                "FlexEnd" => FlexJustify.FlexEnd,
                "SpaceBetween" => FlexJustify.SpaceBetween,
                "SpaceAround" => FlexJustify.SpaceAround,
                "SpaceEvenly" => FlexJustify.SpaceEvenly,
                _ => FlexJustify.FlexStart,
            };
        }
    }
}

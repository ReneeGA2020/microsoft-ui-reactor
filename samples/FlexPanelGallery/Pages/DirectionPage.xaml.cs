using Duct.Flex;
using Microsoft.UI.Xaml.Controls;

namespace FlexPanelGallery.Pages;

public sealed partial class DirectionPage : Page
{
    public DirectionPage()
    {
        InitializeComponent();
    }

    private void DirectionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DemoPanel is null) return;
        if (DirectionCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            DemoPanel.Direction = tag switch
            {
                "Row" => FlexDirection.Row,
                "RowReverse" => FlexDirection.RowReverse,
                "Column" => FlexDirection.Column,
                "ColumnReverse" => FlexDirection.ColumnReverse,
                _ => FlexDirection.Row,
            };
        }
    }
}

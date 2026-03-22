using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace FlexPanelGallery.Pages;

public sealed partial class GapPage : Page
{
    public GapPage()
    {
        InitializeComponent();
    }

    private void GapSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (DemoPanel is null) return;
        DemoPanel.ColumnGap = ColumnGapSlider.Value;
        DemoPanel.RowGap = RowGapSlider.Value;
    }
}

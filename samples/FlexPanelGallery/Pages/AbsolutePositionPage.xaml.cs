using Duct.Flex;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace FlexPanelGallery.Pages;

public sealed partial class AbsolutePositionPage : Page
{
    public AbsolutePositionPage()
    {
        InitializeComponent();
    }

    private void PositionSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (MovableBox is null) return;
        FlexPanel.SetLeft(MovableBox, LeftSlider.Value);
        FlexPanel.SetTop(MovableBox, TopSlider.Value);
    }
}

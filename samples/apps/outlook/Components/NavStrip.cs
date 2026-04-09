using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using static Duct.UI;
using static Duct.Core.Theme;

namespace DuctOutlook.Components;

internal sealed record NavStripProps(
    string ActiveView,
    Action<string> OnViewChanged
);

internal sealed class NavStrip : Component<NavStripProps>
{
    public override Element Render()
    {
        return HStack(0,
            NavBtn("\uE715", "Mail", "mail"),
            NavBtn("\uE787", "Calendar", "calendar")
        ).Set(sp =>
        {
            sp.BorderBrush = new SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 232, 232, 232));
            sp.BorderThickness = new Thickness(0, 1, 0, 0);
        });
    }

    Element NavBtn(string icon, string label, string view)
    {
        var isActive = Props.ActiveView == view;
        var fg = isActive ? AccentText : SecondaryText;

        return Button(
            VStack(0,
                Text(icon).FontSize(16).Foreground(fg)
                    .Set(t => t.FontFamily = new FontFamily("Segoe MDL2 Assets"))
                    .HAlign(HorizontalAlignment.Center),
                Text(label).FontSize(10).Foreground(fg)
                    .HAlign(HorizontalAlignment.Center)
            ).Padding(6, 3, 6, 3),
            () => Props.OnViewChanged(view)
        ).Set(b =>
        {
            b.Background = isActive
                ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 235, 243, 252))
                : new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
            b.BorderThickness = new Thickness(0);
            b.Padding = new Thickness(0);
            b.CornerRadius = new CornerRadius(0);
            b.Width = 56;
            b.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 243, 243, 243));
            b.Resources["ButtonBorderBrushPointerOver"] = new SolidColorBrush(
                Windows.UI.Color.FromArgb(0, 0, 0, 0));
        });
    }
}

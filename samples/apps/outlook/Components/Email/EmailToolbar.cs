using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Reactor.Factories;
using static Microsoft.UI.Reactor.Core.Theme;

namespace ReactorOutlook.Components.Email;

internal sealed class EmailToolbar : Component
{
    public override Element Render()
    {
        return VStack(0,
            (FlexRow(
                ToolbarBtn("\uE74D", "Delete", "D"),
                ToolbarBtn("\uE7B8", "Archive", "A"),
                ToolbarBtn("\uE8DE", "Move to"),
                Separator(),
                ToolbarBtn("\uE97A", "Reply", "R"),
                ToolbarBtn("\uE8C2", "Reply all"),
                ToolbarBtn("\uE72A", "Forward", "F"),
                Separator(),
                ToolbarBtn("\uE8A3", "Read / Unread"),
                ToolbarBtn("\uE129", "Flag")
            ) with { ColumnGap = 4 }).Padding(10, 6, 10, 6),
            Border(Empty()).Height(1).Background(DividerStroke)
        ).AutomationName("Message actions toolbar");
    }

    static Element ToolbarBtn(string icon, string label, string? accessKey = null)
    {
        var btn = Button(
            (FlexRow(
                TextBlock(icon).FontSize(16).Foreground(SecondaryText)
                    .Set(t => t.FontFamily = new FontFamily("Segoe MDL2 Assets"))
                    .AccessibilityHidden(),
                TextBlock(label).FontSize(13).Foreground(PrimaryText)
            ) with { ColumnGap = 6 }),
            null
        ).AutomationName(label)
         .Set(b =>
        {
            b.Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0));
            b.BorderThickness = new Thickness(0);
            b.Padding = new Thickness(10, 6, 10, 6);
            b.CornerRadius = new CornerRadius(3);
            b.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(
                global::Windows.UI.Color.FromArgb(255, 243, 243, 243));
            b.Resources["ButtonBorderBrushPointerOver"] = new SolidColorBrush(
                global::Windows.UI.Color.FromArgb(0, 0, 0, 0));
        });
        if (accessKey is not null) btn = btn.AccessKey(accessKey);
        return btn;
    }

    static Element Separator() =>
        Border(Empty()).Width(1).Height(22).Background(DividerStroke).Margin(4, 0, 4, 0);
}

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using ReactorOutlook.Models;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;
using static Microsoft.UI.Reactor.Core.Theme;

namespace ReactorOutlook.Components.Calendar;

internal sealed record CalendarSidebarProps(
    CalendarSource[] Sources,
    HashSet<string> EnabledSourceIds,
    Action<string, bool> OnSourceToggled
);

internal sealed class CalendarSidebar : Component<CalendarSidebarProps>
{
    public override Element Render()
    {
        return FlexColumn(
            // Mini calendar
            CalendarView()
                .Set(cv =>
                {
                    cv.SelectionMode = Microsoft.UI.Xaml.Controls.CalendarViewSelectionMode.Single;
                    cv.IsOutOfScopeEnabled = true;
                })
                .Margin(4),

            // My calendars header
            HStack(4,
                MdlIcon("\uE70D", 12),
                TextBlock("My calendars").SemiBold().FontSize(13)
            ).Padding(12, 8, 12, 4),

            // Calendar sources with checkboxes
            VStack(2,
                Props.Sources.Select(s =>
                {
                    var isEnabled = Props.EnabledSourceIds.Contains(s.Id);
                    return (Element)HStack(8,
                        CheckBox(isEnabled, v => Props.OnSourceToggled(s.Id, v)),
                        Border(Empty())
                            .Size(12, 12)
                            .CornerRadius(2)
                            .Background(s.ColorHex),
                        TextBlock(s.Name).FontSize(13)
                    ).Padding(12, 2, 12, 2);
                }).ToArray()
            ),

            // Show all link
            Button(
                TextBlock("Show all").FontSize(12).Foreground(AccentText),
                null
            ).Set(b =>
            {
                b.BorderThickness = new Thickness(0);
                b.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    global::Windows.UI.Color.FromArgb(0, 0, 0, 0));
            }).Margin(12, 4, 0, 0)
        ).Background(LayerFill);
    }

    static Element MdlIcon(string glyph, double size = 14) =>
        TextBlock(glyph).FontSize(size)
            .Set(t => t.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"));
}

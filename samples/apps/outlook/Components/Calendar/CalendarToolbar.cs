using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using static Duct.UI;
using static Duct.Core.Theme;

namespace DuctOutlook.Components.Calendar;

internal sealed record CalendarToolbarProps(
    string ViewMode,
    DateTimeOffset WeekStart,
    Action GoToday,
    Action GoPrev,
    Action GoNext,
    Action<string> OnViewModeChanged
);

internal sealed class CalendarToolbar : Component<CalendarToolbarProps>
{
    static readonly string[] ViewModes = ["Day", "Work week", "Week", "Month"];

    public override Element Render()
    {
        var weekEnd = Props.WeekStart.AddDays(6);
        var title = FormatRange(Props.WeekStart, weekEnd);
        var selectedIdx = Array.IndexOf(ViewModes, Props.ViewMode);
        if (selectedIdx < 0) selectedIdx = 2;

        return HStack(8,
            // New event button
            Button(
                HStack(4,
                    MdlIcon("\uE710", 14),
                    Text("New event").FontSize(13)
                ),
                null
            ).Set(b =>
            {
                b.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(255, 0, 120, 212));
                b.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(255, 255, 255, 255));
            }),

            // Separator
            Border(Empty()).Width(1).Height(24).Background(DividerStroke).Margin(4, 0, 4, 0),

            // View mode selector
            SelectorBar(
                ViewModes.Select(v => SelectorBarItem(v)).ToArray(),
                selectedIdx,
                idx => Props.OnViewModeChanged(ViewModes[idx])
            ),

            // Separator
            Border(Empty()).Width(1).Height(24).Background(DividerStroke).Margin(4, 0, 4, 0),

            // Navigation
            Button(MdlIcon("\uE76B", 12), Props.GoPrev)
                .Set(b => { b.BorderThickness = new Thickness(0); b.Padding = new Thickness(6); }),
            Button("Today", Props.GoToday),
            Button(MdlIcon("\uE76C", 12), Props.GoNext)
                .Set(b => { b.BorderThickness = new Thickness(0); b.Padding = new Thickness(6); }),

            // Date range title
            Text(title).SemiBold().FontSize(16).VAlign(VerticalAlignment.Center)
        ).Padding(8, 6, 8, 6);
    }

    static Element MdlIcon(string glyph, double size = 14) =>
        Text(glyph).FontSize(size)
            .Set(t => t.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"));

    static string FormatRange(DateTimeOffset start, DateTimeOffset end)
    {
        if (start.Month == end.Month)
            return $"{start:MMMM d}\u2013{end:d}, {start:yyyy}";
        if (start.Year == end.Year)
            return $"{start:MMM d} \u2013 {end:MMM d}, {start:yyyy}";
        return $"{start:MMM d, yyyy} \u2013 {end:MMM d, yyyy}";
    }
}

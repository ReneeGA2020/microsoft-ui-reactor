using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using ReactorOutlook.Models;
using static Microsoft.UI.Reactor.Factories;
using static Microsoft.UI.Reactor.Core.Theme;

namespace ReactorOutlook.Components.Calendar;

internal sealed record AllDayRowProps(
    CalendarEvent[] AllDayEvents,
    DateTimeOffset WeekStart,
    Dictionary<string, string> SourceColors
);

internal sealed class AllDayRow : Component<AllDayRowProps>
{
    public override Element Render()
    {
        // Build columns: time label + 7 days
        var columns = new[] { "60" }.Concat(Enumerable.Repeat("*", 7)).ToArray();

        var children = new List<Element>
        {
            // Label
            TextBlock("").FontSize(11).Foreground(TertiaryText)
                .Grid(row: 0, column: 0)
                .Padding(4, 2, 4, 2)
        };

        for (int d = 0; d < 7; d++)
        {
            var day = Props.WeekStart.AddDays(d).Date;
            var dayEvents = Props.AllDayEvents
                .Where(e => e.Start.Date <= day && e.End.Date > day)
                .ToArray();

            if (dayEvents.Length > 0)
            {
                var stack = VStack(1,
                    dayEvents.Select(e =>
                    {
                        var color = Props.SourceColors.GetValueOrDefault(e.CalendarSourceId, "#0078D4");
                        return (Element)Border(
                            TextBlock(e.Title).FontSize(10)
                                .Set(t =>
                                {
                                    t.TextTrimming = Microsoft.UI.Xaml.TextTrimming.CharacterEllipsis;
                                    t.MaxLines = 1;
                                })
                        )
                        .Background(color + "30")
                        .WithBorder(color, 1)
                        .CornerRadius(2)
                        .Padding(4, 1, 4, 1)
                        .Set(b => b.BorderThickness = new Microsoft.UI.Xaml.Thickness(2, 0, 0, 0));
                    }).ToArray()
                ).Padding(2);
                children.Add(stack.Grid(row: 0, column: d + 1));
            }
        }

        return Grid(columns, ["Auto"], children.ToArray())
            .Set(g =>
            {
                g.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    global::Windows.UI.Color.FromArgb(255, 224, 224, 224));
                g.BorderThickness = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 1);
            });
    }
}

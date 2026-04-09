using Duct;
using Duct.Core;
using DuctOutlook.Models;
using static Duct.UI;
using static Duct.Core.Theme;

namespace DuctOutlook.Components.Calendar;

internal sealed record WeekGridProps(
    CalendarEvent[] Events,
    DateTimeOffset WeekStart,
    Dictionary<string, string> SourceColors
);

internal sealed class WeekGrid : Component<WeekGridProps>
{
    const int SlotMinutes = 15;
    const int SlotsPerHour = 60 / SlotMinutes;
    const int TotalSlots = 24 * SlotsPerHour; // 96
    const string SlotHeight = "15"; // 15px per 15-min slot = 60px/hour
    const int StartHour = 7; // Show from 7 AM
    const int EndHour = 20;  // to 8 PM

    public override Element Render()
    {
        var timedEvents = Props.Events.Where(e => !e.IsAllDay).ToArray();

        // Grid: 8 columns (time label + 7 days), slots as rows
        var columns = new[] { "60" }.Concat(Enumerable.Repeat("*", 7)).ToArray();
        var visibleSlots = (EndHour - StartHour) * SlotsPerHour;
        var rows = Enumerable.Range(0, visibleSlots).Select(_ => SlotHeight).ToArray();

        var children = new List<Element>();

        // Hour labels and grid lines
        for (int h = StartHour; h < EndHour; h++)
        {
            int row = (h - StartHour) * SlotsPerHour;
            var label = h == 0 ? "12 AM"
                : h < 12 ? $"{h} AM"
                : h == 12 ? "12 PM"
                : $"{h - 12} PM";

            children.Add(
                Text(label).FontSize(11).Foreground(TertiaryText)
                    .Padding(4, 0, 8, 0)
                    .VAlign(Microsoft.UI.Xaml.VerticalAlignment.Top)
                    .HAlign(Microsoft.UI.Xaml.HorizontalAlignment.Right)
                    .Grid(row: row, column: 0)
            );

            // Horizontal grid line across all day columns
            children.Add(
                Border(Empty())
                    .Height(1)
                    .Background(DividerStroke)
                    .VAlign(Microsoft.UI.Xaml.VerticalAlignment.Top)
                    .Grid(row: row, column: 1, columnSpan: 7)
            );
        }

        // Vertical separators between days
        for (int d = 1; d <= 7; d++)
        {
            children.Add(
                Border(Empty())
                    .Width(1)
                    .Background(DividerStroke)
                    .HAlign(Microsoft.UI.Xaml.HorizontalAlignment.Left)
                    .Grid(row: 0, column: d, rowSpan: visibleSlots)
            );
        }

        // Event blocks
        foreach (var evt in timedEvents)
        {
            int dayCol = GetDayColumn(evt.Start);
            if (dayCol < 0 || dayCol > 6) continue;

            var color = Props.SourceColors.GetValueOrDefault(evt.CalendarSourceId, "#0078D4");
            int startSlot = TimeToSlot(evt.Start) - StartHour * SlotsPerHour;
            int endSlot = TimeToSlot(evt.End) - StartHour * SlotsPerHour;
            int span = Math.Max(1, endSlot - startSlot);

            // Clamp to visible range
            if (startSlot < 0) { span += startSlot; startSlot = 0; }
            if (startSlot + span > visibleSlots) span = visibleSlots - startSlot;
            if (span <= 0) continue;

            children.Add(
                Component<EventBlock, EventBlockProps>(new(evt, color))
                    .Margin(1, 1, 2, 1)
                    .Grid(row: startSlot, column: dayCol + 1, rowSpan: span)
            );
        }

        return Grid(columns, rows, children.ToArray());
    }

    int GetDayColumn(DateTimeOffset dt)
    {
        var diff = (dt.Date - Props.WeekStart.Date).Days;
        return diff;
    }

    static int TimeToSlot(DateTimeOffset dt) =>
        dt.Hour * SlotsPerHour + dt.Minute / SlotMinutes;
}

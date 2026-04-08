using Duct;
using Duct.Core;
using DuctOutlook.Models;
using static Duct.UI;

namespace DuctOutlook.Components.Calendar;

internal sealed class CalendarViewComponent : Component
{
    public override Element Render()
    {
        var (weekStart, setWeekStart) = UsePersisted("outlook.cal.weekStart", MockData.GetCurrentWeekStart());
        var (viewMode, setViewMode) = UsePersisted("outlook.cal.viewMode", "Week");

        var sources = UseMemo(() => MockData.GetCalendarSources(), 0);
        var (enabledIds, setEnabledIds) = UseState(
            new HashSet<string>(MockData.GetCalendarSources().Select(s => s.Id)));

        var events = UseMemo(() => MockData.GetCalendarEvents(weekStart), weekStart);

        var visibleEvents = UseMemo(() =>
            events.Where(e => enabledIds.Contains(e.CalendarSourceId)).ToArray(),
            events, enabledIds);

        var sourceColors = UseMemo(() =>
            sources.ToDictionary(s => s.Id, s => s.ColorHex),
            sources);

        var allDayEvents = UseMemo(() =>
            visibleEvents.Where(e => e.IsAllDay).ToArray(),
            visibleEvents);

        var timedEvents = UseMemo(() =>
            visibleEvents.Where(e => !e.IsAllDay).ToArray(),
            visibleEvents);

        // Day headers
        var dayHeaders = BuildDayHeaders(weekStart);

        return Grid(
            ["*"],
            ["Auto", "*"],

            // Toolbar
            Component<CalendarToolbar, CalendarToolbarProps>(new(
                ViewMode: viewMode,
                WeekStart: weekStart,
                GoToday: () => setWeekStart(MockData.GetCurrentWeekStart()),
                GoPrev: () => setWeekStart(weekStart.AddDays(-7)),
                GoNext: () => setWeekStart(weekStart.AddDays(7)),
                OnViewModeChanged: setViewMode
            )).Grid(row: 0, column: 0),

            // Body: sidebar + calendar
            Component<SplitPanel, SplitPanelProps>(new(
                Left: Component<CalendarSidebar, CalendarSidebarProps>(new(
                    Sources: sources,
                    EnabledSourceIds: enabledIds,
                    OnSourceToggled: (id, enabled) =>
                    {
                        var next = new HashSet<string>(enabledIds);
                        if (enabled) next.Add(id); else next.Remove(id);
                        setEnabledIds(next);
                    }
                )),
                Right: FlexColumn(
                    // Day headers
                    dayHeaders,

                    // All day row
                    Component<AllDayRow, AllDayRowProps>(new(
                        AllDayEvents: allDayEvents,
                        WeekStart: weekStart,
                        SourceColors: sourceColors
                    )),

                    // Scrollable time grid
                    ScrollView(
                        Component<WeekGrid, WeekGridProps>(new(
                            Events: timedEvents,
                            WeekStart: weekStart,
                            SourceColors: sourceColors
                        ))
                    ).Flex(grow: 1, basis: 0)
                ),
                InitialWidth: 220,
                MinWidth: 180
            )).Grid(row: 1, column: 0)
        );
    }

    static Element BuildDayHeaders(DateTimeOffset weekStart)
    {
        var columns = new[] { "60" }.Concat(Enumerable.Repeat("*", 7)).ToArray();
        var today = DateTimeOffset.Now.Date;

        var headers = new List<Element>
        {
            Empty().Grid(row: 0, column: 0)
        };

        string[] dayNames = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

        for (int d = 0; d < 7; d++)
        {
            var date = weekStart.AddDays(d).Date;
            var isToday = date == today;

            var dayNum = Text(date.Day.ToString())
                .SemiBold()
                .FontSize(20);

            if (isToday)
                dayNum = dayNum.Foreground("#0078D4");

            var header = VStack(0,
                Text(dayNames[d]).FontSize(12).Foreground("#888"),
                dayNum
            ).HAlign(Microsoft.UI.Xaml.HorizontalAlignment.Center)
             .Padding(4);

            headers.Add(header.Grid(row: 0, column: d + 1));
        }

        return Grid(columns, ["Auto"], headers.ToArray())
            .Set(g =>
            {
                g.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(255, 224, 224, 224));
                g.BorderThickness = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 1);
            });
    }
}

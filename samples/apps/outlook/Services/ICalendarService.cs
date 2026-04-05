using DuctOutlook.Models;

namespace DuctOutlook.Services;

public interface ICalendarService
{
    Task<CalendarSource[]> GetCalendarSourcesAsync();
    Task<CalendarEvent[]> GetEventsAsync(DateTimeOffset weekStart, DateTimeOffset weekEnd);
}

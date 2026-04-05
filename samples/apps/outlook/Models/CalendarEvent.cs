namespace DuctOutlook.Models;

public sealed record CalendarEvent(
    string Id,
    string Title,
    string? Location,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsAllDay,
    string CalendarSourceId,
    string? HtmlBody
);

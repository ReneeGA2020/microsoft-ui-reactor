namespace DuctOutlook.Models;

public sealed record MailFolder(
    string Id,
    string DisplayName,
    string Icon,
    int UnreadCount,
    string? ParentId,
    string AccountId,
    bool IsFavorite,
    MailFolder[]? Children
);

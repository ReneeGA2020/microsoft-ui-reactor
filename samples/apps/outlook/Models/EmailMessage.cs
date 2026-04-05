namespace DuctOutlook.Models;

public enum MessageImportance { Normal, High, Low }
public enum MessageTab { Focused, Other }

public sealed record EmailMessage(
    string Id,
    string SenderName,
    string SenderEmail,
    string SenderInitials,
    string Subject,
    string PreviewText,
    string HtmlBody,
    DateTimeOffset ReceivedDate,
    bool IsRead,
    bool IsFlagged,
    bool HasAttachments,
    bool HasRsvp,
    MessageImportance Importance,
    MessageTab Tab,
    string FolderId,
    string[] ToRecipients
);

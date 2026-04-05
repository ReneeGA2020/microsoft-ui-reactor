using DuctOutlook.Models;

namespace DuctOutlook.Services;

public interface IMailService
{
    Task<MailFolder[]> GetFoldersAsync();
    Task<EmailMessage[]> GetMessagesAsync(string folderId);
}

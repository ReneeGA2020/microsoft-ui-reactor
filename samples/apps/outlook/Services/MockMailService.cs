using DuctOutlook.Models;

namespace DuctOutlook.Services;

public sealed class MockMailService : IMailService
{
    public Task<MailFolder[]> GetFoldersAsync() =>
        Task.FromResult(MockData.GetFolders());

    public Task<EmailMessage[]> GetMessagesAsync(string folderId) =>
        Task.FromResult(MockData.GetMessages(folderId));
}

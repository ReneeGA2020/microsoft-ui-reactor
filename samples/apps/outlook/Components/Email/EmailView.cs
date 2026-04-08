using Duct;
using Duct.Core;
using DuctOutlook.Models;
using static Duct.UI;

namespace DuctOutlook.Components.Email;

internal sealed class EmailView : Component
{
    public override Element Render()
    {
        var (selectedFolderId, setSelectedFolderId) = UsePersisted("outlook.email.folderId", "inbox");
        var autoSelect = AppSettings.ScreenshotMode ? "m1" : null;
        var (selectedMessageId, setSelectedMessageId) = UsePersisted("outlook.email.messageId", autoSelect);
        var (activeTab, setActiveTab) = UsePersisted("outlook.email.tab", MessageTab.Focused);

        var folders = UseMemo(() => MockData.GetFolders(), 0);
        var messages = UseMemo(() => MockData.GetMessages(selectedFolderId), selectedFolderId);

        var selectedMessage = UseMemo(() =>
            selectedMessageId is not null
                ? messages.FirstOrDefault(m => m.Id == selectedMessageId)
                : null,
            selectedMessageId!, messages);

        // Left sidebar: just the folder pane
        var folderPane = Component<FolderPane, FolderPaneProps>(new(
            Folders: folders,
            SelectedFolderId: selectedFolderId,
            OnFolderSelected: id =>
            {
                setSelectedFolderId(id);
                setSelectedMessageId(null);
            }
        ));

        // Right area: toolbar + message list / reading pane split
        var rightArea = FlexColumn(
            Component<EmailToolbar>(),
            Component<SplitPanel, SplitPanelProps>(new(
                Left: Component<MessageListPane, MessageListPaneProps>(new(
                    Messages: messages,
                    ActiveTab: activeTab,
                    OnTabChanged: setActiveTab,
                    SelectedMessageId: selectedMessageId,
                    OnMessageSelected: setSelectedMessageId
                )),
                Right: Component<ReadingPane, ReadingPaneProps>(new(
                    Message: selectedMessage
                )),
                InitialWidth: 420,
                MinWidth: 300
            )).Flex(grow: 1, basis: 0)
        );

        return Component<SplitPanel, SplitPanelProps>(new(
            Left: folderPane,
            Right: rightArea,
            InitialWidth: 280,
            MinWidth: 220
        ));
    }
}

using Duct;
using Duct.Core;
using DuctOutlook.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using static Duct.UI;
using static Duct.Core.Theme;

namespace DuctOutlook.Components.Email;

internal sealed record MessageListPaneProps(
    EmailMessage[] Messages,
    MessageTab ActiveTab,
    Action<MessageTab> OnTabChanged,
    string? SelectedMessageId,
    Action<string> OnMessageSelected
);

internal abstract record MessageListItem(string Key);
internal sealed record DateHeaderItem(string Label) : MessageListItem($"header-{Label}");
internal sealed record MessageItem(EmailMessage Message) : MessageListItem(Message.Id);

internal sealed class MessageListPane : Component<MessageListPaneProps>
{
    public override Element Render()
    {
        var filtered = UseMemo(() =>
            Props.Messages.Where(m => m.Tab == Props.ActiveTab).ToArray(),
            Props.Messages, Props.ActiveTab);

        var flatList = UseMemo(() => BuildFlatList(filtered), filtered);

        var tabIndex = Props.ActiveTab == MessageTab.Focused ? 0 : 1;

        return FlexColumn(
            // Tab bar — more padding
            SelectorBar(
                [SelectorBarItem("Focused"), SelectorBarItem("Other")],
                tabIndex,
                idx => Props.OnTabChanged(idx == 0 ? MessageTab.Focused : MessageTab.Other)
            ).Padding(8, 4, 8, 2),

            // Sort indicator
            (FlexRow(
                MdlIcon("\uE8CB", 12),
                Text("By Date").FontSize(12).Foreground(TertiaryText)
            ) with { ColumnGap = 5 }).Padding(16, 4, 16, 6),

            // Separator
            Border(Empty()).Height(1).Background(DividerStroke),

            // Virtualized message list
            LazyVStack<MessageListItem>(
                flatList,
                item => item.Key,
                (item, _) => item switch
                {
                    DateHeaderItem dh => (FlexRow(
                        MdlIcon("\uE76C", 10),
                        Text(dh.Label).FontSize(13).SemiBold().Foreground(SecondaryText)
                    ) with { ColumnGap = 5 }).Padding(16, 12, 16, 4),

                    MessageItem mi => Component<MessageRow, MessageRowProps>(new(
                        Message: mi.Message,
                        IsSelected: mi.Message.Id == Props.SelectedMessageId,
                        OnSelected: () => Props.OnMessageSelected(mi.Message.Id)
                    )),

                    _ => Empty()
                }
            ) with { Spacing = 0, EstimatedItemSize = 80 }
        );
    }

    static Element MdlIcon(string glyph, double size) =>
        Text(glyph).FontSize(size).Foreground(TertiaryText)
            .Set(t => t.FontFamily = new FontFamily("Segoe MDL2 Assets"));

    static IReadOnlyList<MessageListItem> BuildFlatList(EmailMessage[] messages)
    {
        var today = DateTimeOffset.Now.Date;
        var yesterday = today.AddDays(-1);
        var result = new List<MessageListItem>();
        string? lastGroup = null;

        foreach (var msg in messages)
        {
            var group = msg.ReceivedDate.Date == today ? "Today"
                : msg.ReceivedDate.Date == yesterday ? "Yesterday"
                : (today - msg.ReceivedDate.Date).Days <= 7 ? msg.ReceivedDate.ToString("dddd")
                : msg.ReceivedDate.ToString("MMMM d");

            if (group != lastGroup)
            {
                result.Add(new DateHeaderItem(group));
                lastGroup = group;
            }
            result.Add(new MessageItem(msg));
        }
        return result;
    }
}

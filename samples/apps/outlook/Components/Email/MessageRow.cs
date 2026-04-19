using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using ReactorOutlook.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Reactor.Factories;
using static Microsoft.UI.Reactor.Core.Theme;

namespace ReactorOutlook.Components.Email;

internal sealed record MessageRowProps(
    EmailMessage Message,
    bool IsSelected,
    Action OnSelected
);

internal sealed class MessageRow : Component<MessageRowProps>
{
    static readonly string[] AvatarColors =
        ["#0078D4", "#008272", "#C239B3", "#E74856", "#FF8C00", "#107C10", "#767676", "#5C2D91"];

    static readonly SolidColorBrush TransparentBrush = new(global::Windows.UI.Color.FromArgb(0, 0, 0, 0));
    static readonly SolidColorBrush SelectedBrush = new(global::Windows.UI.Color.FromArgb(255, 218, 234, 251));
    static readonly SolidColorBrush UnreadBrush = new(global::Windows.UI.Color.FromArgb(255, 240, 246, 255));
    static readonly SolidColorBrush HoverBrush = new(global::Windows.UI.Color.FromArgb(255, 230, 240, 250));
    static readonly SolidColorBrush BorderBrush = new(global::Windows.UI.Color.FromArgb(255, 237, 237, 237));

    public override Element Render()
    {
        var msg = Props.Message;
        var color = AvatarColors[Math.Abs(msg.SenderName.GetHashCode()) % AvatarColors.Length];
        var dateStr = FormatDate(msg.ReceivedDate);
        var bold = msg.IsRead
            ? Microsoft.UI.Text.FontWeights.Normal
            : Microsoft.UI.Text.FontWeights.SemiBold;

        var bg = Props.IsSelected ? SelectedBrush
            : !msg.IsRead ? UnreadBrush
            : TransparentBrush;

        // Unread indicator bar
        var unreadBar = !msg.IsRead && !Props.IsSelected
            ? Border(Empty()).Width(3).Background(Accent)
                .VAlign(VerticalAlignment.Stretch)
                .HAlign(HorizontalAlignment.Left)
            : Empty();

        var avatar = Border(
            TextBlock(msg.SenderInitials).Foreground("white").FontSize(13)
                .Set(t =>
                {
                    t.HorizontalTextAlignment = TextAlignment.Center;
                    t.VerticalAlignment = VerticalAlignment.Center;
                    t.HorizontalAlignment = HorizontalAlignment.Center;
                })
        ).Size(36, 36).CornerRadius(18).Background(color)
         .VAlign(VerticalAlignment.Top).Margin(0, 2, 0, 0);

        // Line 1: Sender + date — use FlexRow so grow works
        var senderLine = FlexRow(
            TextBlock(msg.SenderName).FontSize(14)
                .Set(t => { t.FontWeight = bold; t.TextTrimming = TextTrimming.CharacterEllipsis; })
                .Flex(grow: 1),
            TextBlock(dateStr).FontSize(12).Foreground(TertiaryText)
        ) with { ColumnGap = 8 };

        // Line 2: Subject
        var subjectLine = TextBlock(msg.Subject).FontSize(13)
            .Set(t => { t.FontWeight = bold; t.TextTrimming = TextTrimming.CharacterEllipsis; t.MaxLines = 1; });

        // Line 3: Preview + badges — use FlexRow so grow works
        var previewLine = FlexRow(
            TextBlock(msg.PreviewText).FontSize(12).Foreground(TertiaryText)
                .Set(t => { t.TextTrimming = TextTrimming.CharacterEllipsis; t.MaxLines = 1; })
                .Flex(grow: 1),
            msg.HasAttachments
                ? MdlIcon("\uE723", 12, TertiaryText)
                : Empty(),
            msg.HasRsvp
                ? Border(TextBlock("RSVP").FontSize(9).Foreground(AccentText))
                    .Padding(4, 1, 4, 1).CornerRadius(2).WithBorder(Accent, 1)
                : Empty()
        ) with { ColumnGap = 6 };

        var content = FlexRow(
            avatar,
            VStack(2, senderLine, subjectLine, previewLine)
                .Flex(grow: 1).MinWidth(0)
        ) with { ColumnGap = 12 };

        return Button(
            Grid(["*"], ["*"],
                content.Padding(14, 10, 14, 10).Grid(row: 0, column: 0),
                unreadBar.Grid(row: 0, column: 0)
            ),
            Props.OnSelected
        ).Set(b =>
        {
            b.Background = bg;
            b.BorderThickness = new Thickness(0, 0, 0, 1);
            b.BorderBrush = BorderBrush;
            b.Padding = new Thickness(0);
            b.HorizontalAlignment = HorizontalAlignment.Stretch;
            b.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            b.CornerRadius = new CornerRadius(0);
            b.Resources["ButtonBackgroundPointerOver"] = Props.IsSelected ? SelectedBrush : HoverBrush;
            b.Resources["ButtonBackgroundPressed"] = SelectedBrush;
            b.Resources["ButtonBorderBrushPointerOver"] = BorderBrush;
            b.Resources["ButtonBorderBrushPressed"] = BorderBrush;
        });
    }

    static Element MdlIcon(string glyph, double size, string color) =>
        TextBlock(glyph).FontSize(size).Foreground(color)
            .Set(t => t.FontFamily = new FontFamily("Segoe MDL2 Assets"));

    static Element MdlIcon(string glyph, double size, ThemeRef color) =>
        TextBlock(glyph).FontSize(size).Foreground(color)
            .Set(t => t.FontFamily = new FontFamily("Segoe MDL2 Assets"));

    static string FormatDate(DateTimeOffset date)
    {
        var now = DateTimeOffset.Now;
        if (date.Date == now.Date)
            return date.ToString("h:mm tt");
        if ((now.Date - date.Date).Days <= 7)
            return date.ToString("ddd h:mm tt");
        if (date.Year == now.Year)
            return date.ToString("ddd M/d");
        return date.ToString("M/d/yyyy");
    }
}

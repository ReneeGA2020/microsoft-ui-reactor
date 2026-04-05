using Duct;
using Duct.Core;
using DuctOutlook.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using static Duct.UI;

namespace DuctOutlook.Components.Email;

internal sealed record ReadingPaneProps(
    EmailMessage? Message
);

internal sealed class ReadingPane : Component<ReadingPaneProps>
{
    static readonly string[] AvatarColors =
        ["#0078D4", "#008272", "#C239B3", "#E74856", "#FF8C00", "#107C10", "#767676", "#5C2D91"];

    public override Element Render()
    {
        if (Props.Message is not { } msg)
        {
            return VStack(
                MdlIcon("\uE715", 48, "#CCC")
                    .HAlign(HorizontalAlignment.Center),
                Text("Select a message to read")
                    .FontSize(15).Foreground("#999")
                    .HAlign(HorizontalAlignment.Center)
            ).Spacing(16).Center();
        }

        var toLine = msg.ToRecipients.Length > 0
            ? string.Join("; ", msg.ToRecipients)
            : "you";

        var color = AvatarColors[Math.Abs(msg.SenderName.GetHashCode()) % AvatarColors.Length];

        var avatar = Border(
            Text(msg.SenderInitials).Foreground("white").FontSize(15)
                .Set(t =>
                {
                    t.HorizontalTextAlignment = TextAlignment.Center;
                    t.VerticalAlignment = VerticalAlignment.Center;
                    t.HorizontalAlignment = HorizontalAlignment.Center;
                })
        ).Size(44, 44).CornerRadius(22).Background(color);

        return FlexColumn(
            // Subject line
            Text(msg.Subject)
                .FontSize(22).SemiBold()
                .Set(t => t.TextWrapping = TextWrapping.Wrap)
                .Padding(28, 20, 28, 10),

            // Sender row
            (FlexRow(
                avatar,
                VStack(2,
                    (FlexRow(
                        Text(msg.SenderName).SemiBold().FontSize(14),
                        Text($"<{msg.SenderEmail}>").FontSize(12).Foreground("#888")
                    ) with { ColumnGap = 6 }),
                    Text($"To: {toLine}").FontSize(12).Foreground("#666")
                ).Flex(grow: 1),
                // Date + action buttons
                VStack(4,
                    Text(msg.ReceivedDate.ToString("M/d/yyyy h:mm tt"))
                        .FontSize(12).Foreground("#888")
                        .HAlign(HorizontalAlignment.Right),
                    (FlexRow(
                        SmallBtn("\uE97A"),
                        SmallBtn("\uE8C2"),
                        SmallBtn("\uE72A")
                    ) with { ColumnGap = 2 })
                ).VAlign(VerticalAlignment.Top)
            ) with { ColumnGap = 14 }).Padding(28, 6, 28, 16),

            // Separator
            Border(Empty()).Height(1).Background("#E8E8E8"),

            // HTML body via WebView2
            WebView2()
                .WithKey(msg.Id)
                .Set(wv =>
                {
                    wv.DefaultBackgroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
                })
                .OnMount(fe =>
                {
                    var wv = (Microsoft.UI.Xaml.Controls.WebView2)fe;
                    _ = InitWebView(wv, msg.HtmlBody);
                })
                .Flex(grow: 1, basis: 0)
        );
    }

    static Element MdlIcon(string glyph, double size, string color) =>
        Text(glyph).FontSize(size).Foreground(color)
            .Set(t => t.FontFamily = new FontFamily("Segoe MDL2 Assets"));

    static Element SmallBtn(string icon) =>
        Button(
            Text(icon).FontSize(15).Foreground("#555")
                .Set(t => t.FontFamily = new FontFamily("Segoe MDL2 Assets")),
            null
        ).Set(b =>
        {
            b.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
            b.BorderThickness = new Thickness(0);
            b.Padding = new Thickness(7, 5, 7, 5);
            b.CornerRadius = new CornerRadius(3);
            b.MinWidth = 0;
            b.MinHeight = 0;
            b.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 240, 240, 240));
            b.Resources["ButtonBorderBrushPointerOver"] = new SolidColorBrush(
                Windows.UI.Color.FromArgb(0, 0, 0, 0));
        });

    static async Task InitWebView(Microsoft.UI.Xaml.Controls.WebView2 wv, string html)
    {
        await wv.EnsureCoreWebView2Async();
        wv.NavigateToString(html);
    }
}

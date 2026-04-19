using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using ReactorOutlook.Models;
using static Microsoft.UI.Reactor.Factories;
using static Microsoft.UI.Reactor.Core.Theme;

namespace ReactorOutlook.Components.Calendar;

internal sealed record EventBlockProps(
    CalendarEvent Event,
    string ColorHex
);

internal sealed class EventBlock : Component<EventBlockProps>
{
    public override Element Render()
    {
        var evt = Props.Event;
        var lines = evt.Title.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var content = VStack(1,
            lines.Select((line, i) =>
                (Element)(i == 0
                    ? TextBlock(line).SemiBold().FontSize(11)
                        .Set(t =>
                        {
                            t.TextTrimming = Microsoft.UI.Xaml.TextTrimming.CharacterEllipsis;
                            t.MaxLines = 1;
                        })
                    : TextBlock(line).FontSize(11)
                        .Set(t =>
                        {
                            t.TextTrimming = Microsoft.UI.Xaml.TextTrimming.CharacterEllipsis;
                            t.MaxLines = 1;
                        })
                )
            ).Concat(
                evt.Location is not null
                    ? [TextBlock(evt.Location).FontSize(10).Foreground(SecondaryText)
                        .Set(t =>
                        {
                            t.TextTrimming = Microsoft.UI.Xaml.TextTrimming.CharacterEllipsis;
                            t.MaxLines = 1;
                        })]
                    : []
            ).ToArray()
        ).Padding(4, 2, 4, 2);

        return Border(content)
            .Background(Props.ColorHex + "30") // ~19% opacity
            .WithBorder(Props.ColorHex, 2)
            .CornerRadius(3)
            .Set(b => b.BorderThickness = new Microsoft.UI.Xaml.Thickness(2, 0, 0, 0));
    }
}

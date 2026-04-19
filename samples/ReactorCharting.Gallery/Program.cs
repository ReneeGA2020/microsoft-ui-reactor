using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hosting;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Reactor.Factories;
using ReactorCharting.Gallery;

if (args.Contains("--self-test"))
{
    GallerySelfTestRunner.RunAll();
}
else
{
    ReactorApp.Run<GalleryApp>("Reactor Charting Gallery", width: 1400, height: 900,
        configure: host => XamlInterop.Register(host.Reconciler));
}

// ═══════════════════════════════════════════════════════════════════════
//  Gallery App — Landing page + sample detail pages
// ═══════════════════════════════════════════════════════════════════════

class GalleryApp : Component
{
    public override Element Render()
    {
        var (current, setCurrent) = UseState<GallerySample?>(null);
        var (isDark, setIsDark) = UseState(false);

        Element page;
        if (current != null)
            page = RenderSamplePage(current, () => setCurrent(null), isDark, setIsDark);
        else
            page = RenderLanding(setCurrent, isDark, setIsDark);

        // Apply theme to the root container
        return Border(page)
            .Background(Theme.SolidBackground)
            .Set(b => b.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light);
    }

    static string IconPath(GallerySample sample) =>
        global::System.IO.Path.Combine(AppContext.BaseDirectory, "Icons", $"{sample.IconName}.svg");

    static Element SampleIcon(GallerySample sample, double size) =>
        Image(IconPath(sample)) with { Width = size, Height = size };

    Element RenderLanding(Action<GallerySample?> navigate, bool isDark, Action<bool> setIsDark)
    {
        var categories = SampleRegistry.All
            .GroupBy(s => s.Category)
            .OrderBy(g => CategoryOrder(g.Key));

        var sections = categories.Select(group =>
            VStack(8,
                SubHeading(group.Key).Foreground(Theme.PrimaryText),
                new FlexElement(
                    group.Select(sample =>
                        Button(
                            VStack(6,
                                SampleIcon(sample, 36),
                                TextBlock(sample.Title) with { FontSize = 12 }
                            ).MaxWidth(100).HAlign(HorizontalAlignment.Center),
                            () => navigate(sample)
                        ).Width(130).Height(90)
                    ).ToArray()
                )
                {
                    Direction = FlexDirection.Row,
                    Wrap = FlexWrap.Wrap,
                    ColumnGap = 8,
                    RowGap = 8,
                }
            )
        ).ToArray();

        return FlexColumn(
            HStack(12,
                Heading("Reactor Charting Gallery").Foreground(Theme.PrimaryText).Flex(grow: 1),
                ThemeToggle(isDark, setIsDark)
            ).Padding(24, 24, 24, 0).VAlign(VerticalAlignment.Center),
            Caption($"{SampleRegistry.All.Length} samples — powered by D3.js ported to C#")
                .Foreground(Theme.SecondaryText)
                .Padding(24, 0, 24, 0),
            ScrollView(
                VStack(24, sections).Padding(24, 12, 24, 24).Margin(4)
            ).Flex(grow: 1, basis: 0)
        );
    }

    Element RenderSamplePage(GallerySample sample, Action goBack, bool isDark, Action<bool> setIsDark)
    {
        return FlexColumn(
            HStack(12,
                Button("< Back", goBack),
                SampleIcon(sample, 28),
                Heading(sample.Title).Foreground(Theme.PrimaryText).Flex(grow: 1),
                ThemeToggle(isDark, setIsDark)
            ).Padding(24, 24, 24, 0).VAlign(VerticalAlignment.Center),
            Caption(sample.Description)
                .Foreground(Theme.SecondaryText)
                .Padding(24, 8, 24, 0),
            ScrollView(VStack(16,
                Border(sample.Render())
                    .Background(Theme.CardBackground)
                    .WithBorder(Theme.CardStroke)
                    .CornerRadius(8)
                    .Padding(16),
                SubHeading("Source Code").Foreground(Theme.PrimaryText),
                Border(
                    ScrollView(
                        (TextBlock(sample.SourceCode) with
                        {
                            IsTextSelectionEnabled = true,
                            TextWrapping = TextWrapping.Wrap,
                        })
                        .Set(tb =>
                        {
                            tb.FontFamily = new FontFamily("Cascadia Code, Consolas, monospace");
                            tb.FontSize = 12;
                        })
                        .Foreground(Theme.PrimaryText)
                    ).Set(sv => sv.MaxHeight = 400)
                )
                .Background(Theme.LayerFill)
                .WithBorder(Theme.SurfaceStroke)
                .CornerRadius(6)
                .Padding(16)
            ).Padding(24, 0, 24, 24)).Flex(grow: 1, basis: 0)
        );
    }

    static int CategoryOrder(string category) => category switch
    {
        "Bars" => 0,
        "Lines" => 1,
        "Areas" => 2,
        "Dots" => 3,
        "Radial" => 4,
        "Hierarchies" => 5,
        "Networks" => 6,
        "Analysis" => 7,
        "Controls" => 8,
        "Interactive" => 9,
        "Animation" => 10,
        "Design" => 11,
        _ => 99,
    };

    static Element ThemeToggle(bool isDark, Action<bool> setIsDark) =>
        Button(isDark ? "\uE793" : "\uE708", () => setIsDark(!isDark))
            .Foreground(Theme.AccentText)
            .Set(b =>
            {
                b.FontFamily = new FontFamily("Segoe MDL2 Assets");
                b.Width = 36;
                b.Height = 36;
                b.Padding = new Thickness(0);
                b.MinWidth = 0;
                b.MinHeight = 0;
            });
}

using Duct;
using Duct.Core;
using Duct.Flex;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using static Duct.UI;
using DuctD3.Gallery;

DuctApp.Run<GalleryApp>("DuctD3 Gallery", width: 1400, height: 900,
    configure: host => XamlInterop.Register(host.Reconciler));

// ═══════════════════════════════════════════════════════════════════════
//  Gallery App — Landing page + sample detail pages
// ═══════════════════════════════════════════════════════════════════════

class GalleryApp : Component
{
    public override Element Render()
    {
        var (current, setCurrent) = UseState<GallerySample?>(null);

        if (current != null)
            return RenderSamplePage(current, () => setCurrent(null));

        return RenderLanding(setCurrent);
    }

    static string IconPath(GallerySample sample) =>
        System.IO.Path.Combine(AppContext.BaseDirectory, "Icons", $"{sample.IconName}.svg");

    static Element SampleIcon(GallerySample sample, double size) =>
        Image(IconPath(sample)) with { Width = size, Height = size };

    Element RenderLanding(Action<GallerySample?> navigate)
    {
        var categories = SampleRegistry.All
            .GroupBy(s => s.Category)
            .OrderBy(g => CategoryOrder(g.Key));

        var sections = categories.Select(group =>
            VStack(8,
                SubHeading(group.Key),
                new FlexElement(
                    group.Select(sample =>
                        Button(
                            VStack(6,
                                SampleIcon(sample, 36),
                                Text(sample.Title) with { FontSize = 12 }
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
            Heading("DuctD3 Gallery").Padding(24, 24, 24, 0),
            Caption($"{SampleRegistry.All.Length} interactive samples — powered by D3.js ported to C#").Padding(24, 0, 24, 0),
            ScrollView(VStack(24, sections).Padding(24, 12, 24, 24)).Flex(grow: 1, basis: 0)
        );
    }

    Element RenderSamplePage(GallerySample sample, Action goBack)
    {
        return FlexColumn(
            HStack(12,
                Button("< Back", goBack),
                SampleIcon(sample, 28),
                Heading(sample.Title)
            ).Padding(24, 24, 24, 0),
            Caption(sample.Description).Padding(24, 8, 24, 0),
            ScrollView(VStack(16,
                Border(sample.Render()) with
                {
                    Padding = new Thickness(16),
                    Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 249, 250)),
                    CornerRadius = 8,
                },
                SubHeading("Source Code"),
                new XamlHostElement(
                    () => new ScrollViewer
                    {
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Content = new TextBlock
                        {
                            Text = sample.SourceCode,
                            FontFamily = new FontFamily("Cascadia Code, Consolas, monospace"),
                            FontSize = 12,
                            IsTextSelectionEnabled = true,
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 30, 30)),
                        },
                        Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 245, 245)),
                        Padding = new Thickness(16),
                        CornerRadius = new CornerRadius(6),
                        MaxHeight = 400,
                    },
                    _ => { }
                ) { TypeKey = $"Code_{sample.Title}" }
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
        _ => 99,
    };
}

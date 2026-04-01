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
                        Button(sample.Title, () => navigate(sample))
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

        return VStack(16,
            Heading("DuctD3 Gallery"),
            Caption($"{SampleRegistry.All.Length} interactive samples — powered by D3.js ported to C#"),
            ScrollView(VStack(24, sections))
        ).Padding(24);
    }

    Element RenderSamplePage(GallerySample sample, Action goBack)
    {
        return VStack(12,
            HStack(12,
                Button("< Back", goBack),
                Heading(sample.Title)
            ),
            Caption(sample.Description),
            ScrollView(VStack(16,
                new XamlHostElement(
                    () =>
                    {
                        var border = new Border
                        {
                            Padding = new Thickness(16),
                            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 249, 250)),
                            CornerRadius = new CornerRadius(8),
                            Child = sample.Render(),
                        };
                        return border;
                    },
                    _ => { }
                ) { TypeKey = $"Chart_{sample.Title}" },
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
            ))
        ).Padding(24);
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

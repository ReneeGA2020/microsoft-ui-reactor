using Duct.Core;
using Microsoft.UI.Xaml;
using static Duct.UI;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for the RequestedTheme modifier on ElementModifiers and the fluent
/// extension method. Pure C# record tests — no WinUI activation needed.
/// </summary>
public class RequestedThemeTests
{
    [Fact]
    public void RequestedTheme_Extension_Sets_Modifier()
    {
        var el = Border(Text("x")).RequestedTheme(ElementTheme.Dark);
        Assert.Equal(ElementTheme.Dark, el.Modifiers!.RequestedTheme);
    }

    [Fact]
    public void RequestedTheme_Default_Restores_Inheritance()
    {
        var el = Border(Text("x")).RequestedTheme(ElementTheme.Default);
        Assert.Equal(ElementTheme.Default, el.Modifiers!.RequestedTheme);
    }

    [Fact]
    public void RequestedTheme_Light_Sets_Correctly()
    {
        var el = VStack(Text("x")).RequestedTheme(ElementTheme.Light);
        Assert.Equal(ElementTheme.Light, el.Modifiers!.RequestedTheme);
    }

    [Fact]
    public void RequestedTheme_Merge_Right_Side_Wins()
    {
        var left = new ElementModifiers { RequestedTheme = ElementTheme.Light };
        var right = new ElementModifiers { RequestedTheme = ElementTheme.Dark };
        var merged = left.Merge(right);
        Assert.Equal(ElementTheme.Dark, merged.RequestedTheme);
    }

    [Fact]
    public void RequestedTheme_Merge_Preserves_When_Right_Is_Null()
    {
        var left = new ElementModifiers { RequestedTheme = ElementTheme.Dark };
        var right = new ElementModifiers();
        var merged = left.Merge(right);
        Assert.Equal(ElementTheme.Dark, merged.RequestedTheme);
    }

    [Fact]
    public void RequestedTheme_Chains_With_Other_Modifiers()
    {
        var el = Border(Text("x"))
            .Padding(16)
            .RequestedTheme(ElementTheme.Dark)
            .Opacity(0.8);

        Assert.Equal(ElementTheme.Dark, el.Modifiers!.RequestedTheme);
        Assert.Equal(0.8, el.Modifiers.Opacity);
        Assert.Equal(new Thickness(16), el.Modifiers.Padding);
    }
}

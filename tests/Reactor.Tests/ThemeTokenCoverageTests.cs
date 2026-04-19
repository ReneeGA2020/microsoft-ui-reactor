using Microsoft.UI.Reactor.Core;
using Xunit;

namespace Microsoft.UI.Reactor.Tests;

/// <summary>
/// Read every public Theme token to cover the ThemeRef factory bodies.
/// Each property is a one-line getter that allocates a new ThemeRef and is
/// otherwise impossible to exercise without rendering against the WinUI tree.
/// </summary>
public class ThemeTokenCoverageTests
{
    private static void AssertHasKey(ThemeRef token)
    {
        Assert.False(string.IsNullOrEmpty(token.ResourceKey));
    }

    [Fact]
    public void Accent_Tokens()
    {
        AssertHasKey(Theme.Accent);
        AssertHasKey(Theme.AccentSecondary);
        AssertHasKey(Theme.AccentTertiary);
        AssertHasKey(Theme.AccentDisabled);
    }

    [Fact]
    public void Text_Tokens()
    {
        AssertHasKey(Theme.PrimaryText);
        AssertHasKey(Theme.SecondaryText);
        AssertHasKey(Theme.TertiaryText);
        AssertHasKey(Theme.DisabledText);
        AssertHasKey(Theme.AccentText);
    }

    [Fact]
    public void Surface_And_Fill_Tokens()
    {
        AssertHasKey(Theme.SolidBackground);
        AssertHasKey(Theme.CardBackground);
        AssertHasKey(Theme.SmokeFill);
        AssertHasKey(Theme.SubtleFill);
        AssertHasKey(Theme.LayerFill);
    }

    [Fact]
    public void Control_Fill_Tokens()
    {
        AssertHasKey(Theme.ControlFill);
        AssertHasKey(Theme.ControlFillSecondary);
        AssertHasKey(Theme.ControlFillTertiary);
        AssertHasKey(Theme.ControlFillDisabled);
        AssertHasKey(Theme.ControlFillInputActive);
    }

    [Fact]
    public void Stroke_Tokens()
    {
        AssertHasKey(Theme.CardStroke);
        AssertHasKey(Theme.SurfaceStroke);
        AssertHasKey(Theme.DividerStroke);
        AssertHasKey(Theme.ControlStroke);
        AssertHasKey(Theme.ControlStrokeSecondary);
    }

    [Fact]
    public void Signal_Tokens()
    {
        AssertHasKey(Theme.SystemAttention);
        AssertHasKey(Theme.SystemSuccess);
        AssertHasKey(Theme.SystemCaution);
        AssertHasKey(Theme.SystemCritical);
        AssertHasKey(Theme.SystemNeutral);
        AssertHasKey(Theme.SystemSolidNeutral);
        AssertHasKey(Theme.SystemAttentionBackground);
        AssertHasKey(Theme.SystemSuccessBackground);
        AssertHasKey(Theme.SystemCautionBackground);
        AssertHasKey(Theme.SystemCriticalBackground);
        AssertHasKey(Theme.SystemNeutralBackground);
        AssertHasKey(Theme.SystemSolidAttention);
    }

    [Fact]
    public void Custom_Ref_Returns_ThemeRef_With_Key()
    {
        var token = Theme.Ref("MyCustomBrush");
        Assert.Equal("MyCustomBrush", token.ResourceKey);
    }

    [Fact]
    public void ThemeRef_ToString_Includes_Key()
    {
        var token = new ThemeRef("AccentFillColorDefaultBrush");
        Assert.Contains("AccentFillColorDefaultBrush", token.ToString());
    }

    [Fact]
    public void Resolve_With_Null_Application_Resources_Returns_Null()
    {
        // Outside an Application host, Application.Current is null and Resolve
        // must fail safe without throwing.
        var brush = ThemeRef.Resolve("AccentFillColorDefaultBrush", isDark: true);
        Assert.Null(brush);
        var brushLight = ThemeRef.Resolve("AccentFillColorDefaultBrush", isDark: false);
        Assert.Null(brushLight);
    }
}

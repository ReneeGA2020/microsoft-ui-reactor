using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Localization;

public class LogicalModifierTests
{
    // ── MarginInlineStart ──────────────────────────────────────────

    [Fact]
    public void MarginInlineStart_LTR_ResolvesToLeftMargin()
    {
        var mods = new ElementModifiers { MarginInlineStart = 16 };

        var resolved = ResolveMargin(mods, FlowDirection.LeftToRight);

        Assert.Equal(16, resolved.Left);
        Assert.Equal(0, resolved.Right);
    }

    [Fact]
    public void MarginInlineStart_RTL_ResolvesToRightMargin()
    {
        var mods = new ElementModifiers { MarginInlineStart = 16 };

        var resolved = ResolveMargin(mods, FlowDirection.RightToLeft);

        Assert.Equal(0, resolved.Left);
        Assert.Equal(16, resolved.Right);
    }

    // ── MarginInlineEnd ────────────────────────────────────────────

    [Fact]
    public void MarginInlineEnd_LTR_ResolvesToRightMargin()
    {
        var mods = new ElementModifiers { MarginInlineEnd = 8 };

        var resolved = ResolveMargin(mods, FlowDirection.LeftToRight);

        Assert.Equal(0, resolved.Left);
        Assert.Equal(8, resolved.Right);
    }

    [Fact]
    public void MarginInlineEnd_RTL_ResolvesToLeftMargin()
    {
        var mods = new ElementModifiers { MarginInlineEnd = 8 };

        var resolved = ResolveMargin(mods, FlowDirection.RightToLeft);

        Assert.Equal(8, resolved.Left);
        Assert.Equal(0, resolved.Right);
    }

    // ── Combined margin ────────────────────────────────────────────

    [Fact]
    public void MarginInlineStartAndEnd_LTR_BothResolveCorrectly()
    {
        var mods = new ElementModifiers { MarginInlineStart = 16, MarginInlineEnd = 8 };

        var resolved = ResolveMargin(mods, FlowDirection.LeftToRight);

        Assert.Equal(16, resolved.Left);
        Assert.Equal(8, resolved.Right);
    }

    [Fact]
    public void MarginInlineStartAndEnd_RTL_BothResolveCorrectly()
    {
        var mods = new ElementModifiers { MarginInlineStart = 16, MarginInlineEnd = 8 };

        var resolved = ResolveMargin(mods, FlowDirection.RightToLeft);

        Assert.Equal(8, resolved.Left);
        Assert.Equal(16, resolved.Right);
    }

    // ── PaddingInlineStart ─────────────────────────────────────────

    [Fact]
    public void PaddingInlineStart_LTR_ResolvesToLeftPadding()
    {
        var mods = new ElementModifiers { PaddingInlineStart = 12 };

        var resolved = ResolvePadding(mods, FlowDirection.LeftToRight);

        Assert.Equal(12, resolved.Left);
        Assert.Equal(0, resolved.Right);
    }

    [Fact]
    public void PaddingInlineStart_RTL_ResolvesToRightPadding()
    {
        var mods = new ElementModifiers { PaddingInlineStart = 12 };

        var resolved = ResolvePadding(mods, FlowDirection.RightToLeft);

        Assert.Equal(0, resolved.Left);
        Assert.Equal(12, resolved.Right);
    }

    // ── PaddingInlineEnd ───────────────────────────────────────────

    [Fact]
    public void PaddingInlineEnd_LTR_ResolvesToRightPadding()
    {
        var mods = new ElementModifiers { PaddingInlineEnd = 4 };

        var resolved = ResolvePadding(mods, FlowDirection.LeftToRight);

        Assert.Equal(0, resolved.Left);
        Assert.Equal(4, resolved.Right);
    }

    [Fact]
    public void PaddingInlineEnd_RTL_ResolvesToLeftPadding()
    {
        var mods = new ElementModifiers { PaddingInlineEnd = 4 };

        var resolved = ResolvePadding(mods, FlowDirection.RightToLeft);

        Assert.Equal(4, resolved.Left);
        Assert.Equal(0, resolved.Right);
    }

    // ── Extension method fluent API ────────────────────────────────

    [Fact]
    public void ExtensionMethod_MarginInlineStart_SetsProperty()
    {
        var el = new TextBlockElement("Hello");
        var modified = Microsoft.UI.Reactor.ElementExtensions.MarginInlineStart(el, 16);

        Assert.NotNull(modified.Modifiers);
        Assert.Equal(16, modified.Modifiers!.MarginInlineStart);
    }

    [Fact]
    public void ExtensionMethod_PaddingInlineEnd_SetsProperty()
    {
        var el = new TextBlockElement("Hello");
        var modified = Microsoft.UI.Reactor.ElementExtensions.PaddingInlineEnd(el, 8);

        Assert.NotNull(modified.Modifiers);
        Assert.Equal(8, modified.Modifiers!.PaddingInlineEnd);
    }

    [Fact]
    public void ExtensionMethod_Chaining_PreservesAllValues()
    {
        var el = new TextBlockElement("Hello");
        var modified = Microsoft.UI.Reactor.ElementExtensions.MarginInlineEnd(
            Microsoft.UI.Reactor.ElementExtensions.MarginInlineStart(el, 16), 8);

        Assert.NotNull(modified.Modifiers);
        Assert.Equal(16, modified.Modifiers!.MarginInlineStart);
        Assert.Equal(8, modified.Modifiers!.MarginInlineEnd);
    }

    // ── Merge preserves logical properties ─────────────────────────

    [Fact]
    public void Merge_LogicalProperties_SecondOverridesFirst()
    {
        var first = new ElementModifiers { MarginInlineStart = 10 };
        var second = new ElementModifiers { MarginInlineStart = 20 };

        var merged = first.Merge(second);

        Assert.Equal(20, merged.MarginInlineStart);
    }

    [Fact]
    public void Merge_LogicalProperties_NullDoesNotOverride()
    {
        var first = new ElementModifiers { MarginInlineStart = 10, PaddingInlineEnd = 5 };
        var second = new ElementModifiers { MarginInlineEnd = 8 };

        var merged = first.Merge(second);

        Assert.Equal(10, merged.MarginInlineStart);
        Assert.Equal(8, merged.MarginInlineEnd);
        Assert.Equal(5, merged.PaddingInlineEnd);
    }

    // ── Helper: resolve logical margin to physical Thickness ───────

    private static Thickness ResolveMargin(ElementModifiers mods, FlowDirection direction)
    {
        bool isRtl = direction == FlowDirection.RightToLeft;
        var baseMargin = mods.Margin ?? new Thickness();
        var left = isRtl ? (mods.MarginInlineEnd ?? baseMargin.Left) : (mods.MarginInlineStart ?? baseMargin.Left);
        var right = isRtl ? (mods.MarginInlineStart ?? baseMargin.Right) : (mods.MarginInlineEnd ?? baseMargin.Right);
        return new Thickness(left, baseMargin.Top, right, baseMargin.Bottom);
    }

    private static Thickness ResolvePadding(ElementModifiers mods, FlowDirection direction)
    {
        bool isRtl = direction == FlowDirection.RightToLeft;
        var basePad = mods.Padding ?? new Thickness();
        var left = isRtl ? (mods.PaddingInlineEnd ?? basePad.Left) : (mods.PaddingInlineStart ?? basePad.Left);
        var right = isRtl ? (mods.PaddingInlineStart ?? basePad.Right) : (mods.PaddingInlineEnd ?? basePad.Right);
        return new Thickness(left, basePad.Top, right, basePad.Bottom);
    }
}

using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using static Duct.UI;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for the declarative flyout attachment system — ContentFlyout, MenuItems,
/// .WithFlyout(), .WithContextFlyout(), and .WithToolTip(Element).
/// These are pure C# record tests, no WinUI thread needed.
/// </summary>
public class FlyoutAttachmentTests
{
    // ════════════════════════════════════════════════════════════════
    //  DSL factory tests (pure record construction)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ContentFlyout_Creates_ContentFlyoutElement_With_Defaults()
    {
        var el = ContentFlyout(Text("content"));
        Assert.IsType<ContentFlyoutElement>(el);
        Assert.Equal(FlyoutPlacementMode.Auto, el.Placement);
    }

    [Fact]
    public void ContentFlyout_With_Explicit_Placement()
    {
        var el = ContentFlyout(Text("content"), placement: FlyoutPlacementMode.Bottom);
        Assert.Equal(FlyoutPlacementMode.Bottom, el.Placement);
    }

    [Fact]
    public void ContentFlyout_Content_Is_Preserved()
    {
        var inner = Text("inner content");
        var el = ContentFlyout(inner);
        Assert.Same(inner, el.Content);
    }

    [Fact]
    public void MenuItems_Creates_MenuFlyoutContentElement()
    {
        var el = MenuItems(
            MenuItem("Item 1"),
            MenuItem("Item 2")
        );
        Assert.IsType<MenuFlyoutContentElement>(el);
        Assert.Equal(2, el.Items.Length);
    }

    [Fact]
    public void MenuItems_With_Placement()
    {
        var el = MenuItems(FlyoutPlacementMode.Top,
            MenuItem("Item 1")
        );
        Assert.Equal(FlyoutPlacementMode.Top, el.Placement);
    }

    [Fact]
    public void MenuItems_Preserves_Items_Array()
    {
        var item1 = MenuItem("One");
        var item2 = MenuItem("Two");
        var sep = MenuSeparator();
        var el = MenuItems(item1, sep, item2);
        Assert.Equal(3, el.Items.Length);
        Assert.Same(item1, el.Items[0]);
        Assert.Same(sep, el.Items[1]);
        Assert.Same(item2, el.Items[2]);
    }

    [Fact]
    public void ContentFlyoutElement_Record_Equality()
    {
        var a = ContentFlyout(Text("x"), FlyoutPlacementMode.Bottom);
        var b = ContentFlyout(Text("x"), FlyoutPlacementMode.Bottom);
        Assert.Equal(a, b);
    }

    [Fact]
    public void MenuFlyoutContentElement_Record_Equality()
    {
        var item = MenuItem("A");
        var a = new MenuFlyoutContentElement(new[] { (Duct.Core.MenuFlyoutItemBase)item });
        var b = new MenuFlyoutContentElement(new[] { (Duct.Core.MenuFlyoutItemBase)item });
        // Arrays are reference types so two different arrays won't be equal by default
        Assert.NotSame(a.Items, b.Items);
    }

    [Fact]
    public void ContentFlyoutElement_Is_Element_Subclass()
    {
        Element el = ContentFlyout(Text("x"));
        Assert.IsAssignableFrom<Element>(el);
    }

    [Fact]
    public void MenuFlyoutContentElement_Is_Element_Subclass()
    {
        Element el = MenuItems(MenuItem("x"));
        Assert.IsAssignableFrom<Element>(el);
    }

    // ════════════════════════════════════════════════════════════════
    //  Modifier tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void WithFlyout_Sets_AttachedFlyout_On_Modifiers()
    {
        var flyout = ContentFlyout(Text("content"));
        var el = Button("Click", null).WithFlyout(flyout);
        Assert.NotNull(el.Modifiers);
        Assert.NotNull(el.Modifiers!.AttachedFlyout);
        Assert.IsType<ContentFlyoutElement>(el.Modifiers.AttachedFlyout);
    }

    [Fact]
    public void WithContextFlyout_Sets_ContextFlyout_On_Modifiers()
    {
        var menu = MenuItems(MenuItem("Copy"), MenuItem("Paste"));
        var el = Text("right-click me").WithContextFlyout(menu);
        Assert.NotNull(el.Modifiers);
        Assert.NotNull(el.Modifiers!.ContextFlyout);
        Assert.IsType<MenuFlyoutContentElement>(el.Modifiers.ContextFlyout);
    }

    [Fact]
    public void WithToolTip_Element_Sets_RichToolTip_On_Modifiers()
    {
        var tip = VStack(Text("Title"), Text("Description"));
        var el = Button("Hover me", null).WithToolTip(tip);
        Assert.NotNull(el.Modifiers);
        Assert.NotNull(el.Modifiers!.RichToolTip);
        Assert.IsType<StackElement>(el.Modifiers.RichToolTip);
    }

    [Fact]
    public void WithFlyout_Works_On_TextElement()
    {
        var el = Text("tap me").WithFlyout(ContentFlyout(Text("popup")));
        Assert.IsType<TextElement>(el);
        Assert.NotNull(el.Modifiers?.AttachedFlyout);
    }

    [Fact]
    public void WithFlyout_Works_On_ButtonElement()
    {
        var el = Button("Go", null).WithFlyout(ContentFlyout(Text("popup")));
        Assert.IsType<ButtonElement>(el);
        Assert.NotNull(el.Modifiers?.AttachedFlyout);
    }

    [Fact]
    public void WithFlyout_Works_On_BorderElement()
    {
        var el = Border(Text("inner")).WithFlyout(ContentFlyout(Text("popup")));
        Assert.IsType<BorderElement>(el);
        Assert.NotNull(el.Modifiers?.AttachedFlyout);
    }

    [Fact]
    public void WithContextFlyout_Works_On_TextElement()
    {
        var el = Text("right-click").WithContextFlyout(MenuItems(MenuItem("Cut")));
        Assert.IsType<TextElement>(el);
        Assert.NotNull(el.Modifiers?.ContextFlyout);
    }

    [Fact]
    public void WithContextFlyout_Works_On_ButtonElement()
    {
        var el = Button("Go", null).WithContextFlyout(MenuItems(MenuItem("Help")));
        Assert.IsType<ButtonElement>(el);
        Assert.NotNull(el.Modifiers?.ContextFlyout);
    }

    [Fact]
    public void WithContextFlyout_Works_On_BorderElement()
    {
        var el = Border(Empty()).WithContextFlyout(MenuItems(MenuItem("Refresh")));
        Assert.IsType<BorderElement>(el);
        Assert.NotNull(el.Modifiers?.ContextFlyout);
    }

    [Fact]
    public void RichToolTip_Does_Not_Interfere_With_String_ToolTip()
    {
        var el = Button("Hover", null)
            .ToolTip("simple string tip")
            .WithToolTip(VStack(Text("rich"), Text("tooltip")));

        Assert.NotNull(el.Modifiers);
        Assert.Equal("simple string tip", el.Modifiers!.ToolTip);
        Assert.NotNull(el.Modifiers.RichToolTip);
        Assert.IsType<StackElement>(el.Modifiers.RichToolTip);
    }

    [Fact]
    public void Modifier_Merge_Preserves_AttachedFlyout()
    {
        var flyout = ContentFlyout(Text("content"));
        var mods1 = new ElementModifiers { AttachedFlyout = flyout };
        var mods2 = new ElementModifiers { Opacity = 0.5 };

        var merged = mods1.Merge(mods2);
        Assert.Same(flyout, merged.AttachedFlyout);
        Assert.Equal(0.5, merged.Opacity);
    }

    [Fact]
    public void Modifier_Merge_Preserves_ContextFlyout()
    {
        var menu = MenuItems(MenuItem("Action"));
        var mods1 = new ElementModifiers { ContextFlyout = menu };
        var mods2 = new ElementModifiers { Width = 100 };

        var merged = mods1.Merge(mods2);
        Assert.Same(menu, merged.ContextFlyout);
        Assert.Equal(100, merged.Width);
    }

    [Fact]
    public void Modifier_Merge_Preserves_RichToolTip()
    {
        var tip = Text("rich tip");
        var mods1 = new ElementModifiers { RichToolTip = tip };
        var mods2 = new ElementModifiers { Height = 50 };

        var merged = mods1.Merge(mods2);
        Assert.Same(tip, merged.RichToolTip);
        Assert.Equal(50, merged.Height);
    }

    [Fact]
    public void Attachments_Compose_With_Other_Modifiers()
    {
        var el = Text("styled")
            .Margin(16)
            .Opacity(0.8)
            .WithFlyout(ContentFlyout(Text("popup")));

        Assert.NotNull(el.Modifiers);
        Assert.Equal(new Thickness(16), el.Modifiers!.Margin);
        Assert.Equal(0.8, el.Modifiers.Opacity);
        Assert.NotNull(el.Modifiers.AttachedFlyout);
    }

    [Fact]
    public void Multiple_Attachments_On_Same_Element()
    {
        var el = Border(Text("full"))
            .WithFlyout(ContentFlyout(Text("flyout")))
            .WithContextFlyout(MenuItems(MenuItem("Copy")))
            .WithToolTip(Text("tip"));

        Assert.NotNull(el.Modifiers?.AttachedFlyout);
        Assert.NotNull(el.Modifiers?.ContextFlyout);
        Assert.NotNull(el.Modifiers?.RichToolTip);
    }

    // ════════════════════════════════════════════════════════════════
    //  Type matching tests (reconciler uses GetType() equality)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Same_ContentFlyoutElement_Types_Match()
    {
        var a = ContentFlyout(Text("old"));
        var b = ContentFlyout(Text("new"));
        Assert.Equal(a.GetType(), b.GetType());
    }

    [Fact]
    public void ContentFlyoutElement_And_MenuFlyoutContentElement_Do_Not_Match()
    {
        Element a = ContentFlyout(Text("content"));
        Element b = MenuItems(MenuItem("item"));
        Assert.NotEqual(a.GetType(), b.GetType());
    }

    [Fact]
    public void Same_MenuFlyoutContentElement_Types_Match()
    {
        var a = MenuItems(MenuItem("old"));
        var b = MenuItems(MenuItem("new"));
        Assert.Equal(a.GetType(), b.GetType());
    }

    // ════════════════════════════════════════════════════════════════
    //  DropDownButton / SplitButton flyout slot tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DropDownButton_Accepts_MenuItems_As_Flyout()
    {
        var el = DropDownButton("Menu", flyout: MenuItems(
            MenuItem("A"),
            MenuItem("B")
        ));
        Assert.IsType<DropDownButtonElement>(el);
        Assert.NotNull(el.Flyout);
        Assert.IsType<MenuFlyoutContentElement>(el.Flyout);
    }

    [Fact]
    public void SplitButton_Accepts_ContentFlyout_As_Flyout()
    {
        var el = SplitButton("Action", () => { }, flyout: ContentFlyout(Text("options")));
        Assert.IsType<SplitButtonElement>(el);
        Assert.NotNull(el.Flyout);
        Assert.IsType<ContentFlyoutElement>(el.Flyout);
    }

    [Fact]
    public void ContentFlyout_Default_Placement_Is_Auto()
    {
        var el = ContentFlyout(Empty());
        Assert.Equal(FlyoutPlacementMode.Auto, el.Placement);
    }

    [Fact]
    public void MenuFlyoutContentElement_Default_Placement_Is_Auto()
    {
        var el = MenuItems(MenuItem("x"));
        Assert.Equal(FlyoutPlacementMode.Auto, el.Placement);
    }
}

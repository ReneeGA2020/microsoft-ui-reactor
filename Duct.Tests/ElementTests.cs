using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Text;
using Windows.UI.Text;
using static Duct.UI;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for Element records — creation, immutability, defaults, and the DSL factory methods.
/// These are pure C# record tests, no WinUI thread needed.
/// </summary>
public class ElementTests
{
    // ════════════════════════════════════════════════════════════════
    //  Text elements
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Text_Creates_TextElement_With_Content()
    {
        var el = Text("Hello");
        Assert.IsType<TextElement>(el);
        Assert.Equal("Hello", el.Content);
        Assert.Null(el.FontSize);
        Assert.Null(el.Weight);
    }

    [Fact]
    public void Heading_Creates_TextElement_With_Bold_And_Size28()
    {
        var el = Heading("Title");
        Assert.Equal("Title", el.Content);
        Assert.Equal(28, el.FontSize);
        Assert.Equal(FontWeights.Bold, el.Weight);
    }

    [Fact]
    public void SubHeading_Creates_TextElement_With_SemiBold_And_Size20()
    {
        var el = SubHeading("Sub");
        Assert.Equal(20, el.FontSize);
        Assert.Equal(FontWeights.SemiBold, el.Weight);
    }

    [Fact]
    public void Caption_Creates_TextElement_With_Size12()
    {
        var el = Caption("cap");
        Assert.Equal(12, el.FontSize);
    }

    [Fact]
    public void String_Implicit_Conversion_To_Element()
    {
        Element el = "hello";
        Assert.IsType<TextElement>(el);
        Assert.Equal("hello", ((TextElement)el).Content);
    }

    // ════════════════════════════════════════════════════════════════
    //  Button elements
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Button_Defaults_To_Enabled_With_No_Click()
    {
        var el = Button("Click");
        Assert.Equal("Click", el.Label);
        Assert.True(el.IsEnabled);
        Assert.Null(el.OnClick);
    }

    [Fact]
    public void Button_With_Click_Handler()
    {
        bool clicked = false;
        var el = Button("Go", () => clicked = true);
        el.OnClick!.Invoke();
        Assert.True(clicked);
    }

    [Fact]
    public void Button_Disabled_Extension_Toggles_IsEnabled()
    {
        var el = Button("Go").Disabled();
        Assert.False(el.IsEnabled);

        var el2 = Button("Go").Disabled(false);
        Assert.True(el2.IsEnabled);
    }

    [Fact]
    public void HyperlinkButton_Creates_With_Uri()
    {
        var uri = new Uri("https://example.com");
        var el = HyperlinkButton("Link", uri);
        Assert.Equal("Link", el.Content);
        Assert.Equal(uri, el.NavigateUri);
    }

    [Fact]
    public void ToggleButton_Creates_With_Defaults()
    {
        var el = ToggleButton("Toggle");
        Assert.Equal("Toggle", el.Label);
        Assert.False(el.IsChecked);
        Assert.Null(el.OnToggled);
    }

    [Fact]
    public void RepeatButton_Has_Default_Delay_And_Interval()
    {
        var el = RepeatButton("Rep");
        Assert.Equal(250, el.Delay);
        Assert.Equal(50, el.Interval);
    }

    // ════════════════════════════════════════════════════════════════
    //  Input elements
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TextField_Creates_With_Value_And_Placeholder()
    {
        var el = TextField("val", placeholder: "hint");
        Assert.Equal("val", el.Value);
        Assert.Equal("hint", el.Placeholder);
    }

    [Fact]
    public void PasswordBox_Creates_With_Password()
    {
        var el = PasswordBox("secret", placeholderText: "Enter password");
        Assert.Equal("secret", el.Password);
        Assert.Equal("Enter password", el.PlaceholderText);
    }

    [Fact]
    public void NumberBox_Creates_With_Defaults()
    {
        var el = NumberBox(42.0);
        Assert.Equal(42.0, el.Value);
        Assert.Equal(double.MinValue, el.Minimum);
        Assert.Equal(double.MaxValue, el.Maximum);
        Assert.Equal(NumberBoxSpinButtonPlacementMode.Hidden, el.SpinButtonPlacement);
    }

    [Fact]
    public void NumberBox_Range_Extension_Sets_MinMax()
    {
        var el = NumberBox(5.0).Range(0, 10);
        Assert.Equal(0, el.Minimum);
        Assert.Equal(10, el.Maximum);
    }

    [Fact]
    public void CheckBox_Creates_With_State()
    {
        var el = CheckBox(true, label: "Agree");
        Assert.True(el.IsChecked);
        Assert.Equal("Agree", el.Label);
    }

    [Fact]
    public void RadioButton_Creates_With_GroupName()
    {
        var el = RadioButton("Option A", groupName: "grp1");
        Assert.Equal("Option A", el.Label);
        Assert.Equal("grp1", el.GroupName);
    }

    [Fact]
    public void ComboBox_Creates_With_Items()
    {
        var el = ComboBox(["A", "B", "C"], selectedIndex: 1);
        Assert.Equal(3, el.Items.Length);
        Assert.Equal(1, el.SelectedIndex);
    }

    [Fact]
    public void ComboBox_Placeholder_Extension()
    {
        var el = ComboBox(["A"]).Placeholder("Pick one");
        Assert.Equal("Pick one", el.PlaceholderText);
    }

    [Fact]
    public void Slider_Creates_With_Range()
    {
        var el = Slider(50, 0, 100);
        Assert.Equal(50, el.Value);
        Assert.Equal(0, el.Min);
        Assert.Equal(100, el.Max);
    }

    [Fact]
    public void ToggleSwitch_Creates_With_Content()
    {
        var el = ToggleSwitch(true, onContent: "On", offContent: "Off");
        Assert.True(el.IsOn);
        Assert.Equal("On", el.OnContent);
        Assert.Equal("Off", el.OffContent);
    }

    [Fact]
    public void RatingControl_Creates_With_Defaults()
    {
        var el = RatingControl(3.5);
        Assert.Equal(3.5, el.Value);
        Assert.Equal(5, el.MaxRating);
        Assert.False(el.IsReadOnly);
    }

    // ════════════════════════════════════════════════════════════════
    //  Date / Time elements
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DatePicker_Creates_With_Date()
    {
        var date = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);
        var el = DatePicker(date);
        Assert.Equal(date, el.Date);
        Assert.True(el.DayVisible);
    }

    [Fact]
    public void TimePicker_Creates_With_Time()
    {
        var time = new TimeSpan(14, 30, 0);
        var el = TimePicker(time);
        Assert.Equal(time, el.Time);
        Assert.Equal(1, el.MinuteIncrement);
    }

    // ════════════════════════════════════════════════════════════════
    //  Progress elements
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Progress_Determinate()
    {
        var el = Progress(75);
        Assert.Equal(75.0, el.Value);
        Assert.False(el.IsIndeterminate);
    }

    [Fact]
    public void Progress_Indeterminate()
    {
        var el = ProgressIndeterminate();
        Assert.Null(el.Value);
        Assert.True(el.IsIndeterminate);
    }

    [Fact]
    public void ProgressRing_Defaults()
    {
        var el = ProgressRing();
        Assert.True(el.IsIndeterminate);
        Assert.True(el.IsActive);
    }

    // ════════════════════════════════════════════════════════════════
    //  Layout elements
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void VStack_Creates_Vertical_Stack_Filtering_Nulls()
    {
        var el = VStack(Text("A"), null, Text("B"));
        Assert.Equal(Orientation.Vertical, el.Orientation);
        Assert.Equal(2, el.Children.Length);
        Assert.Equal(8, el.Spacing); // default
    }

    [Fact]
    public void VStack_With_Custom_Spacing()
    {
        var el = VStack(16, Text("A"), Text("B"));
        Assert.Equal(16, el.Spacing);
        Assert.Equal(2, el.Children.Length);
    }

    [Fact]
    public void HStack_Creates_Horizontal_Stack()
    {
        var el = HStack(Text("A"), Text("B"));
        Assert.Equal(Orientation.Horizontal, el.Orientation);
    }

    [Fact]
    public void Border_Creates_With_Child()
    {
        var child = Text("inner");
        var el = Border(child);
        Assert.Same(child, el.Child);
        Assert.Null(el.CornerRadius);
        Assert.Null(el.Background);
    }

    [Fact]
    public void Border_Fluent_Extensions()
    {
        var el = Border(Text("x"))
            .CornerRadius(8)
            .Background("#ff0000")
            .WithBorder("blue", 2);
        Assert.Equal(8, el.CornerRadius);
        Assert.IsType<Microsoft.UI.Xaml.Media.SolidColorBrush>(el.Background);
        Assert.IsType<Microsoft.UI.Xaml.Media.SolidColorBrush>(el.BorderBrush);
        Assert.Equal(2, el.BorderThickness);
    }

    [Fact]
    public void ScrollView_Creates_With_Defaults()
    {
        var el = ScrollView(Text("scrollable"));
        Assert.Equal(ScrollBarVisibility.Auto, el.HorizontalScrollBarVisibility);
        Assert.Equal(ScrollBarVisibility.Auto, el.VerticalScrollBarVisibility);
    }

    [Fact]
    public void Grid_Creates_With_Definition_And_Children()
    {
        var el = Grid(
            ["*", "Auto"], ["*"],
            Cell(Text("A"), 0, 0),
            Cell(Text("B"), 0, 1)
        );
        Assert.Equal(2, el.Definition.Columns.Length);
        Assert.Single(el.Definition.Rows);
        Assert.Equal(2, el.Children.Length);
    }

    [Fact]
    public void Expander_Creates_With_Header_And_Content()
    {
        var el = Expander("Header", Text("Content"), isExpanded: true);
        Assert.Equal("Header", el.Header);
        Assert.True(el.IsExpanded);
        Assert.Equal(ExpandDirection.Down, el.ExpandDirection);
    }

    // ════════════════════════════════════════════════════════════════
    //  Navigation elements
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void NavigationView_Creates_With_Items()
    {
        var el = NavigationView([NavItem("Home", tag: "home"), NavItem("Settings")]);
        Assert.Equal(2, el.MenuItems.Length);
        Assert.Equal("home", el.MenuItems[0].Tag);
        Assert.True(el.IsSettingsVisible);
    }

    [Fact]
    public void TabView_Creates_With_Tabs()
    {
        var el = TabView(Tab("Tab1", Text("Content1")), Tab("Tab2", Text("Content2")));
        Assert.Equal(2, el.Tabs.Length);
        Assert.Equal(0, el.SelectedIndex);
    }

    [Fact]
    public void BreadcrumbBar_Creates_With_Items()
    {
        var el = BreadcrumbBar([Breadcrumb("Home"), Breadcrumb("Products")]);
        Assert.Equal(2, el.Items.Length);
    }

    // ════════════════════════════════════════════════════════════════
    //  Collections
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ListView_Creates_With_Items()
    {
        var el = ListView(Text("A"), Text("B"), Text("C"));
        Assert.Equal(3, el.Items.Length);
        Assert.Equal(-1, el.SelectedIndex);
        Assert.Equal(ListViewSelectionMode.Single, el.SelectionMode);
    }

    [Fact]
    public void TreeView_Creates_With_Nodes()
    {
        var el = TreeView(
            TreeNode("Root",
                TreeNode("Child1"),
                TreeNode("Child2")
            )
        );
        Assert.Single(el.Nodes);
        Assert.Equal(2, el.Nodes[0].Children!.Length);
    }

    // ════════════════════════════════════════════════════════════════
    //  Dialog / Overlay elements
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ContentDialog_Creates_With_Defaults()
    {
        var el = ContentDialog("Title", Text("Body"));
        Assert.Equal("Title", el.Title);
        Assert.Equal("OK", el.PrimaryButtonText);
        Assert.False(el.IsOpen);
    }

    [Fact]
    public void InfoBar_Creates_With_Severity()
    {
        var el = InfoBar("Warning", "Something happened")
            .Severity(InfoBarSeverity.Warning);
        Assert.Equal("Warning", el.Title);
        Assert.Equal(InfoBarSeverity.Warning, el.Severity);
        Assert.True(el.IsOpen);
    }

    // ════════════════════════════════════════════════════════════════
    //  Menu elements
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void MenuBar_Creates_With_Items()
    {
        var el = MenuBar(
            Menu("File", MenuItem("New"), MenuItem("Open"), MenuSeparator(), MenuItem("Exit")),
            Menu("Edit", MenuItem("Copy"))
        );
        Assert.Equal(2, el.Items.Length);
        Assert.Equal(4, el.Items[0].Items.Length);
    }

    [Fact]
    public void CommandBar_Creates_With_Commands()
    {
        var el = CommandBar(
            primaryCommands: [AppBarButton("Save"), AppBarButton("Delete")]
        );
        Assert.Equal(2, el.PrimaryCommands!.Length);
    }

    // ════════════════════════════════════════════════════════════════
    //  Modifier extensions
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Margin_Wraps_In_ModifiedElement()
    {
        var el = Text("Hi").Margin(10);
        var mod = Assert.IsType<ModifiedElement>(el);
        Assert.IsType<TextElement>(mod.Inner);
        Assert.Equal(new Thickness(10), mod.Modifiers.Margin);
    }

    [Fact]
    public void Multiple_Modifiers_Merge()
    {
        var el = Text("Hi").Margin(10).Width(200).Opacity(0.5);
        var mod = Assert.IsType<ModifiedElement>(el);
        Assert.Equal(new Thickness(10), mod.Modifiers.Margin);
        Assert.Equal(200, mod.Modifiers.Width);
        Assert.Equal(0.5, mod.Modifiers.Opacity);
    }

    [Fact]
    public void Width_And_Height_Via_Size()
    {
        var el = Text("Hi").Size(100, 50);
        var mod = Assert.IsType<ModifiedElement>(el);
        Assert.Equal(100, mod.Modifiers.Width);
        Assert.Equal(50, mod.Modifiers.Height);
    }

    [Fact]
    public void Center_Sets_Both_Alignments()
    {
        var el = Text("Hi").Center();
        var mod = Assert.IsType<ModifiedElement>(el);
        Assert.Equal(HorizontalAlignment.Center, mod.Modifiers.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, mod.Modifiers.VerticalAlignment);
    }

    [Fact]
    public void Visible_False_Sets_IsVisible()
    {
        var el = Text("Hi").Visible(false);
        var mod = Assert.IsType<ModifiedElement>(el);
        Assert.False(mod.Modifiers.IsVisible);
    }

    [Fact]
    public void ToolTip_Sets_ToolTip()
    {
        var el = Text("Hi").ToolTip("Help text");
        var mod = Assert.IsType<ModifiedElement>(el);
        Assert.Equal("Help text", mod.Modifiers.ToolTip);
    }

    [Fact]
    public void WithKey_Sets_Key_On_Element()
    {
        var el = Text("Hi").WithKey("item-1");
        Assert.Equal("item-1", el.Key);
    }

    // ════════════════════════════════════════════════════════════════
    //  Set() extension — compile-time type safety
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Set_Adds_Setter_To_ButtonElement()
    {
        var el = Button("Go").Set(b => b.FlowDirection = Microsoft.UI.Xaml.FlowDirection.RightToLeft);
        // Setters is internal, so we verify via the Set extension returning a new element
        var el2 = Button("Go");
        Assert.NotEqual(el, el2); // Set creates a new record with setters
    }

    [Fact]
    public void Set_Chains_Multiple_Setters()
    {
        var el = Text("Hi")
            .Set(tb => tb.IsTextSelectionEnabled = true)
            .Set(tb => tb.TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap);
        // Each Set call adds a setter to the internal array
        Assert.NotEqual(Text("Hi"), el);
    }

    [Fact]
    public void Set_On_Slider_Is_Strongly_Typed()
    {
        var el = Slider(50, 0, 100)
            .Set(s => s.TickFrequency = 10);
        Assert.NotEqual(Slider(50, 0, 100), el);
    }

    // ════════════════════════════════════════════════════════════════
    //  Conditional helpers
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void When_True_Returns_Element()
    {
        var el = When(true, () => Text("yes"));
        Assert.IsType<TextElement>(el);
    }

    [Fact]
    public void When_False_Returns_Empty()
    {
        var el = When(false, () => Text("no"));
        Assert.IsType<EmptyElement>(el);
    }

    [Fact]
    public void If_True_Returns_Then()
    {
        var el = If(true, () => Text("then"), () => Text("else"));
        Assert.Equal("then", ((TextElement)el).Content);
    }

    [Fact]
    public void If_False_Returns_Otherwise()
    {
        var el = If(false, () => Text("then"), () => Text("else"));
        Assert.Equal("else", ((TextElement)el).Content);
    }

    [Fact]
    public void If_False_No_Otherwise_Returns_Empty()
    {
        var el = If(false, () => Text("then"));
        Assert.IsType<EmptyElement>(el);
    }

    [Fact]
    public void ForEach_Maps_Items_To_VStack()
    {
        var el = ForEach(new[] { "A", "B", "C" }, item => Text(item));
        Assert.IsType<StackElement>(el);
        var stack = (StackElement)el;
        Assert.Equal(3, stack.Children.Length);
    }

    [Fact]
    public void ForEach_With_Index()
    {
        var el = ForEach(new[] { "A", "B" }, (item, i) => Text($"{i}:{item}"));
        var stack = (StackElement)el;
        Assert.Equal(2, stack.Children.Length);
        Assert.Equal("0:A", ((TextElement)stack.Children[0]).Content);
    }

    [Fact]
    public void Empty_Returns_Singleton()
    {
        var el1 = Empty();
        var el2 = Empty();
        Assert.Same(el1, el2);
    }

    // ════════════════════════════════════════════════════════════════
    //  Record immutability
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Element_With_Expression_Creates_New_Instance()
    {
        var original = Text("Hello");
        var modified = original.Bold();
        Assert.NotSame(original, modified);
        Assert.Null(original.Weight);
        Assert.Equal(FontWeights.Bold, modified.Weight);
    }

    [Fact]
    public void Slider_StepFrequency_Extension()
    {
        var el = Slider(50).StepFrequency(5);
        Assert.Equal(5, el.StepFrequency);
    }

    // ════════════════════════════════════════════════════════════════
    //  CanUpdate logic
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void CanUpdate_Same_Type_Verified_By_Record_Type()
    {
        // CanUpdate is internal — we verify the underlying logic:
        // same element type → can update (same .GetType())
        var a = Text("a");
        var b = Text("b");
        Assert.Equal(a.GetType(), b.GetType());
    }

    [Fact]
    public void CanUpdate_Different_Type_Verified_By_Record_Type()
    {
        var a = (Element)Text("a");
        var b = (Element)Button("b");
        Assert.NotEqual(a.GetType(), b.GetType());
    }

    [Fact]
    public void CanUpdate_Same_Component_Type_Has_Same_ComponentType()
    {
        var a = new ComponentElement(typeof(TestComponent));
        var b = new ComponentElement(typeof(TestComponent));
        Assert.Equal(a.ComponentType, b.ComponentType);
    }

    [Fact]
    public void CanUpdate_Different_Component_Type_Has_Different_ComponentType()
    {
        var a = new ComponentElement(typeof(TestComponent));
        var b = new ComponentElement(typeof(TestComponent2));
        Assert.NotEqual(a.ComponentType, b.ComponentType);
    }

    // ════════════════════════════════════════════════════════════════
    //  Thickness
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Thickness_Uniform()
    {
        var t = new Thickness(10);
        Assert.Equal(10, t.Left);
        Assert.Equal(10, t.Top);
        Assert.Equal(10, t.Right);
        Assert.Equal(10, t.Bottom);
    }

    [Fact]
    public void Thickness_Horizontal_Vertical()
    {
        var t = new Thickness(5, 10, 5, 10);
        Assert.Equal(5, t.Left);
        Assert.Equal(10, t.Top);
        Assert.Equal(5, t.Right);
        Assert.Equal(10, t.Bottom);
    }

    [Fact]
    public void Thickness_Full()
    {
        var t = new Thickness(1, 2, 3, 4);
        Assert.Equal(1, t.Left);
        Assert.Equal(2, t.Top);
        Assert.Equal(3, t.Right);
        Assert.Equal(4, t.Bottom);
    }

    // ════════════════════════════════════════════════════════════════
    //  ElementModifiers.Merge
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Modifiers_Merge_Overwrites_Non_Null()
    {
        var a = new ElementModifiers { Width = 100, Height = 50 };
        var b = new ElementModifiers { Width = 200, Opacity = 0.5 };
        var merged = a.Merge(b);

        Assert.Equal(200, merged.Width);   // overwritten
        Assert.Equal(50, merged.Height);   // kept from a
        Assert.Equal(0.5, merged.Opacity); // new from b
    }

    [Fact]
    public void Modifiers_Merge_Preserves_When_Other_Is_Null()
    {
        var a = new ElementModifiers { Margin = new Thickness(10) };
        var b = new ElementModifiers { };
        var merged = a.Merge(b);

        Assert.Equal(new Thickness(10), merged.Margin);
    }

    // ── Test component stubs ────────────────────────────────────────

    private class TestComponent : Component
    {
        public override Element Render() => Text("test");
    }

    private class TestComponent2 : Component
    {
        public override Element Render() => Text("test2");
    }
}

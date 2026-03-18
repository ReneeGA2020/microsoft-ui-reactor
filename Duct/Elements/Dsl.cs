using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Text;
using Windows.UI.Text;
using MenuFlyoutItemBase = Duct.Core.MenuFlyoutItemBase;

namespace Duct;

/// <summary>
/// Static factory methods that form the Duct DSL.
/// Import with: using static Duct.UI;
///
/// This gives you a clean, declarative syntax:
///   VStack(
///       Text("Hello").Bold(),
///       Button("Click me", () => setCount(count + 1)),
///       count > 5 ? Text("Wow!") : null
///   )
/// </summary>
public static class UI
{
    // ── Text ────────────────────────────────────────────────────────

    public static TextElement Text(string content) => new(content);

    public static TextElement Heading(string content) =>
        new(content) { FontSize = 28, Weight = FontWeights.Bold };

    public static TextElement SubHeading(string content) =>
        new(content) { FontSize = 20, Weight = FontWeights.SemiBold };

    public static TextElement Caption(string content) =>
        new(content) { FontSize = 12 };

    public static RichTextBlockElement RichText(string text) => new(text);

    // ── Buttons ─────────────────────────────────────────────────────

    public static ButtonElement Button(string label, Action? onClick = null) =>
        new(label, onClick);

    public static HyperlinkButtonElement HyperlinkButton(string content, Uri? navigateUri = null, Action? onClick = null) =>
        new(content, navigateUri, onClick);

    public static RepeatButtonElement RepeatButton(string label, Action? onClick = null) =>
        new(label, onClick);

    public static ToggleButtonElement ToggleButton(string label, bool isChecked = false, Action<bool>? onToggled = null) =>
        new(label, isChecked, onToggled);

    public static DropDownButtonElement DropDownButton(string label, Element? flyout = null) =>
        new(label, flyout);

    public static SplitButtonElement SplitButton(string label, Action? onClick = null, Element? flyout = null) =>
        new(label, onClick, flyout);

    public static ToggleSplitButtonElement ToggleSplitButton(string label, bool isChecked = false, Action<bool>? onIsCheckedChanged = null, Element? flyout = null) =>
        new(label, isChecked, onIsCheckedChanged, flyout);

    // ── Input controls ──────────────────────────────────────────────

    public static TextFieldElement TextField(string value, Action<string>? onChanged = null, string? placeholder = null) =>
        new(value, onChanged, placeholder);

    public static PasswordBoxElement PasswordBox(string password, Action<string>? onPasswordChanged = null, string? placeholderText = null) =>
        new(password, onPasswordChanged, placeholderText);

    public static NumberBoxElement NumberBox(double value, Action<double>? onValueChanged = null, string? header = null) =>
        new(value, onValueChanged, header);

    public static AutoSuggestBoxElement AutoSuggestBox(string text, Action<string>? onTextChanged = null, Action<string>? onQuerySubmitted = null) =>
        new(text, onTextChanged, onQuerySubmitted);

    public static CheckBoxElement CheckBox(bool isChecked, Action<bool>? onChanged = null, string? label = null) =>
        new(isChecked, onChanged, label);

    public static RadioButtonElement RadioButton(string label, bool isChecked = false, Action<bool>? onChecked = null, string? groupName = null) =>
        new(label, isChecked, onChecked, groupName);

    public static RadioButtonsElement RadioButtons(string[] items, int selectedIndex = -1, Action<int>? onSelectionChanged = null) =>
        new(items, selectedIndex, onSelectionChanged);

    public static ComboBoxElement ComboBox(string[] items, int selectedIndex = -1, Action<int>? onSelectionChanged = null) =>
        new(items, selectedIndex, onSelectionChanged);

    public static SliderElement Slider(double value, double min = 0, double max = 100, Action<double>? onChanged = null) =>
        new(value, min, max, onChanged);

    public static ToggleSwitchElement ToggleSwitch(bool isOn, Action<bool>? onChanged = null, string? onContent = null, string? offContent = null) =>
        new(isOn, onChanged, onContent, offContent);

    public static RatingControlElement RatingControl(double value = 0, Action<double>? onValueChanged = null) =>
        new(value, onValueChanged);

    public static ColorPickerElement ColorPicker(Windows.UI.Color color, Action<Windows.UI.Color>? onColorChanged = null) =>
        new(color, onColorChanged);

    // ── Date / Time ─────────────────────────────────────────────────

    public static CalendarDatePickerElement CalendarDatePicker(DateTimeOffset? date = null, Action<DateTimeOffset?>? onDateChanged = null) =>
        new(date, onDateChanged);

    public static DatePickerElement DatePicker(DateTimeOffset date, Action<DateTimeOffset>? onDateChanged = null) =>
        new(date, onDateChanged);

    public static TimePickerElement TimePicker(TimeSpan time, Action<TimeSpan>? onTimeChanged = null) =>
        new(time, onTimeChanged);

    // ── Progress ────────────────────────────────────────────────────

    public static ProgressElement Progress(double value) => new(value);
    public static ProgressElement ProgressIndeterminate() => new(null);

    public static ProgressRingElement ProgressRing() => new(null);
    public static ProgressRingElement ProgressRing(double value) => new(value);

    // ── Status / Info ───────────────────────────────────────────────

    public static InfoBarElement InfoBar(string? title = null, string? message = null) => new(title, message);

    public static InfoBadgeElement InfoBadge() => new();
    public static InfoBadgeElement InfoBadge(int value) => new() { Value = value };

    // ── Layout ──────────────────────────────────────────────────────

    public static StackElement VStack(params Element?[] children) =>
        new(Orientation.Vertical, FilterChildren(children));

    public static StackElement VStack(double spacing, params Element?[] children) =>
        new(Orientation.Vertical, FilterChildren(children)) { Spacing = spacing };

    public static StackElement HStack(params Element?[] children) =>
        new(Orientation.Horizontal, FilterChildren(children));

    public static StackElement HStack(double spacing, params Element?[] children) =>
        new(Orientation.Horizontal, FilterChildren(children)) { Spacing = spacing };

    public static ScrollViewElement ScrollView(Element child) => new(child);

    public static BorderElement Border(Element child) => new(child);

    public static ExpanderElement Expander(string header, Element content, bool isExpanded = false, Action<bool>? onExpandedChanged = null) =>
        new(header, content, isExpanded, onExpandedChanged);

    public static SplitViewElement SplitView(Element? pane = null, Element? content = null) =>
        new(pane, content);

    public static ViewboxElement Viewbox(Element child) => new(child);

    public static CanvasElement Canvas(params CanvasChild[] children) => new(children);

    public static CanvasChild CanvasItem(Element element, double left = 0, double top = 0) =>
        new(element, left, top);

    // ── Grid ────────────────────────────────────────────────────────

    public static GridElement Grid(
        string[] columns, string[] rows,
        params GridChild[] children) =>
        new(new GridDefinition(columns, rows), children);

    public static GridChild Cell(Element element, int row = 0, int column = 0, int rowSpan = 1, int columnSpan = 1) =>
        new(element, row, column, rowSpan, columnSpan);

    // ── Navigation ──────────────────────────────────────────────────

    public static NavigationViewElement NavigationView(NavigationViewItemData[] menuItems, Element? content = null) =>
        new(menuItems, content);

    public static NavigationViewItemData NavItem(string content, string? icon = null, string? tag = null) =>
        new(content, icon, tag);

    public static TabViewElement TabView(params TabViewItemData[] tabs) => new(tabs);

    public static TabViewItemData Tab(string header, Element content) => new(header, content);

    public static BreadcrumbBarElement BreadcrumbBar(BreadcrumbBarItemData[] items, Action<BreadcrumbBarItemData>? onItemClicked = null) =>
        new(items, onItemClicked);

    public static BreadcrumbBarItemData Breadcrumb(string label, object? tag = null) => new(label, tag);

    public static PivotElement Pivot(params PivotItemData[] items) => new(items);

    public static PivotItemData PivotItem(string header, Element content) => new(header, content);

    // ── Collections ─────────────────────────────────────────────────

    public static ListViewElement ListView(params Element[] items) => new(items);

    public static GridViewElement GridView(params Element[] items) => new(items);

    public static TreeViewElement TreeView(params TreeViewNodeData[] nodes) => new(nodes);

    public static TreeViewNodeData TreeNode(string content, params TreeViewNodeData[] children) =>
        new(content, children.Length > 0 ? children : null);

    public static FlipViewElement FlipView(params Element[] items) => new(items);

    // ── Dialogs / Overlays ──────────────────────────────────────────

    public static ContentDialogElement ContentDialog(string title, Element content, string primaryButtonText = "OK") =>
        new(title, content, primaryButtonText);

    public static FlyoutElement Flyout(Element target, Element flyoutContent) =>
        new(target, flyoutContent);

    public static TeachingTipElement TeachingTip(string title, string? subtitle = null) =>
        new(title, subtitle);

    // ── Menus ───────────────────────────────────────────────────────

    public static MenuBarElement MenuBar(params MenuBarItemData[] items) => new(items);

    public static MenuBarItemData Menu(string title, params MenuFlyoutItemBase[] items) => new(title, items);

    public static MenuFlyoutItemData MenuItem(string text, Action? onClick = null, string? icon = null) => new(text, onClick, icon);

    public static MenuFlyoutSeparatorData MenuSeparator() => new();

    public static MenuFlyoutSubItemData MenuSubItem(string text, params MenuFlyoutItemBase[] items) => new(text, items);

    public static MenuFlyoutElement MenuFlyout(Element target, params MenuFlyoutItemBase[] items) => new(target, items);

    public static CommandBarElement CommandBar(AppBarButtonData[]? primaryCommands = null, AppBarButtonData[]? secondaryCommands = null) =>
        new(primaryCommands, secondaryCommands);

    public static AppBarButtonData AppBarButton(string label, Action? onClick = null, string? icon = null) => new(label, onClick, icon);

    public static AppBarToggleButtonData AppBarToggleButton(string label, bool isChecked = false, Action<bool>? onToggled = null, string? icon = null) =>
        new(label, isChecked, onToggled, icon);

    // ── Media ───────────────────────────────────────────────────────

    public static ImageElement Image(string source) => new(source);

    public static PersonPictureElement PersonPicture() => new();

    public static WebView2Element WebView2(Uri? source = null) => new(source);

    // ── Components ──────────────────────────────────────────────────

    /// <summary>
    /// Embed a Component class as a child element.
    /// Usage: Component&lt;MyWidget&gt;()
    /// </summary>
    public static ComponentElement Component<T>() where T : Component, new() =>
        new(typeof(T));

    /// <summary>
    /// Define an inline function component (like a React function component).
    /// Usage: Func(ctx => { var (n,setN) = ctx.UseState(0); return Text($"{n}"); })
    /// </summary>
    public static FuncElement Func(Func<RenderContext, Element> render) => new(render);

    // ── Conditional helpers ─────────────────────────────────────────

    /// <summary>
    /// Renders element only when condition is true. Reads nicely:
    ///   When(items.Any(), () =&gt; Text("Has items"))
    /// </summary>
    public static Element When(bool condition, Func<Element> then) =>
        condition ? then() : EmptyElement.Instance;

    /// <summary>
    /// If/else as an expression:
    ///   If(loggedIn, () =&gt; Text("Welcome"), () =&gt; Button("Login", ...))
    /// </summary>
    public static Element If(bool condition, Func<Element> then, Func<Element>? otherwise = null) =>
        condition ? then() : (otherwise?.Invoke() ?? EmptyElement.Instance);

    /// <summary>
    /// Map a list to elements (like .map() in React JSX):
    ///   ForEach(items, item =&gt; Text(item.Name))
    /// </summary>
    public static Element ForEach<T>(IEnumerable<T> items, Func<T, Element> render) =>
        VStack(items.Select(render).ToArray());

    /// <summary>
    /// Map with index:
    ///   ForEach(items, (item, i) =&gt; Text($"{i}: {item}"))
    /// </summary>
    public static Element ForEach<T>(IEnumerable<T> items, Func<T, int, Element> render) =>
        VStack(items.Select((item, i) => render(item, i)).ToArray());

    /// <summary>
    /// Renders nothing. Useful as a default/fallback.
    /// </summary>
    public static Element Empty() => EmptyElement.Instance;

    // ── Thickness helpers (WinUI lacks a (horizontal, vertical) constructor) ──

    /// <summary>
    /// Creates a Thickness with horizontal and vertical values.
    /// Usage: Thick(16, 8) → Thickness(16, 8, 16, 8)
    /// </summary>
    public static Thickness Thick(double horizontal, double vertical) =>
        new(horizontal, vertical, horizontal, vertical);

    /// <summary>
    /// Creates a uniform Thickness. Shorthand for new Thickness(uniform).
    /// </summary>
    public static Thickness Thick(double uniform) => new(uniform);

    /// <summary>
    /// Creates a Thickness with all four sides specified.
    /// </summary>
    public static Thickness Thick(double left, double top, double right, double bottom) =>
        new(left, top, right, bottom);

    // ── Internals ───────────────────────────────────────────────────

    private static Element[] FilterChildren(Element?[] children) =>
        children.Where(c => c is not null).Select(c => c!).ToArray();
}

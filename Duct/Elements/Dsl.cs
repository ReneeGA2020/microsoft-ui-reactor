using Duct.Core;
using Duct.Markdown;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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
        new(content) { FontSize = 28, Weight = new Windows.UI.Text.FontWeight { Weight = 700 } };

    public static TextElement SubHeading(string content) =>
        new(content) { FontSize = 20, Weight = new Windows.UI.Text.FontWeight { Weight = 600 } };

    public static TextElement Caption(string content) =>
        new(content) { FontSize = 12 };

    public static RichTextBlockElement RichText(string text) => new(text);

    public static RichEditBoxElement RichEditBox(string text = "", Action<string>? onTextChanged = null) =>
        new(text) { OnTextChanged = onTextChanged };

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

    public static TextFieldElement TextField(string value, Action<string>? onChanged = null, string? placeholder = null, string? header = null) =>
        new(value, onChanged, placeholder) { Header = header };

    public static PasswordBoxElement PasswordBox(string password, Action<string>? onPasswordChanged = null, string? placeholderText = null) =>
        new(password, onPasswordChanged, placeholderText);

    public static NumberBoxElement NumberBox(double value, Action<double>? onValueChanged = null, string? header = null) =>
        new(value, onValueChanged, header);

    public static AutoSuggestBoxElement AutoSuggestBox(string text, Action<string>? onTextChanged = null, Action<string>? onQuerySubmitted = null) =>
        new(text, onTextChanged, onQuerySubmitted);

    public static CheckBoxElement CheckBox(bool isChecked, Action<bool>? onChanged = null, string? label = null) =>
        new(isChecked, onChanged, label);

    public static CheckBoxElement ThreeStateCheckBox(bool? checkedState, Action<bool?>? onCheckedStateChanged = null, string? label = null) =>
        new(checkedState == true, Label: label) { IsThreeState = true, CheckedState = checkedState, OnCheckedStateChanged = onCheckedStateChanged };

    public static RadioButtonElement RadioButton(string label, bool isChecked = false, Action<bool>? onChecked = null, string? groupName = null) =>
        new(label, isChecked, onChecked, groupName);

    public static RadioButtonsElement RadioButtons(string[] items, int selectedIndex = -1, Action<int>? onSelectionChanged = null) =>
        new(items, selectedIndex, onSelectionChanged);

    public static ComboBoxElement ComboBox(string[] items, int selectedIndex = -1, Action<int>? onSelectionChanged = null) =>
        new(items, selectedIndex, onSelectionChanged);

    public static SliderElement Slider(double value, double min = 0, double max = 100, Action<double>? onChanged = null) =>
        new(value, min, max, onChanged);

    public static ToggleSwitchElement ToggleSwitch(bool isOn, Action<bool>? onChanged = null, string? onContent = null, string? offContent = null, string? header = null) =>
        new(isOn, onChanged, onContent, offContent) { Header = header };

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

    public static WrapGridElement WrapGrid(params Element?[] children) =>
        new(FilterChildren(children));

    public static WrapGridElement WrapGrid(int maxRowsOrColumns, params Element?[] children) =>
        new(FilterChildren(children)) { MaximumRowsOrColumns = maxRowsOrColumns };

    public static ScrollViewElement ScrollView(Element child) => new(child);

    public static BorderElement Border(Element child) => new(child);

    public static ExpanderElement Expander(string header, Element content, bool isExpanded = false, Action<bool>? onExpandedChanged = null) =>
        new(header, content, isExpanded, onExpandedChanged);

    public static SplitViewElement SplitView(Element? pane = null, Element? content = null) =>
        new(pane, content);

    public static ViewboxElement Viewbox(Element child) => new(child);

    public static CanvasElement Canvas(params Element?[] children) => new(FilterChildren(children));

    // ── Flex ────────────────────────────────────────────────────────

    public static FlexElement Flex(params Element?[] children) =>
        new(FilterChildren(children));

    public static FlexElement Flex(Duct.Flex.FlexDirection direction, params Element?[] children) =>
        new(FilterChildren(children)) { Direction = direction };

    public static FlexElement FlexRow(params Element?[] children) =>
        new(FilterChildren(children)) { Direction = Duct.Flex.FlexDirection.Row };

    public static FlexElement FlexColumn(params Element?[] children) =>
        new(FilterChildren(children)) { Direction = Duct.Flex.FlexDirection.Column };

    // ── Grid ────────────────────────────────────────────────────────

    public static GridElement Grid(
        string[] columns, string[] rows,
        params Element?[] children) =>
        new(new GridDefinition(columns, rows), FilterChildren(children));

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

    public static ContentFlyoutElement ContentFlyout(Element content, FlyoutPlacementMode placement = FlyoutPlacementMode.Auto) =>
        new(content) { Placement = placement };

    public static MenuFlyoutContentElement MenuItems(params MenuFlyoutItemBase[] items) =>
        new(items);

    public static MenuFlyoutContentElement MenuItems(FlyoutPlacementMode placement, params MenuFlyoutItemBase[] items) =>
        new(items) { Placement = placement };

    // ── Menus ───────────────────────────────────────────────────────

    public static MenuBarElement MenuBar(params MenuBarItemData[] items) => new(items);

    public static MenuBarItemData Menu(string title, params MenuFlyoutItemBase[] items) => new(title, items);

    public static MenuFlyoutItemData MenuItem(string text, Action? onClick = null, string? icon = null) => new(text, onClick, icon);

    public static ToggleMenuFlyoutItemData ToggleMenuItem(string text, bool isChecked = false, Action<bool>? onToggled = null, string? icon = null) => new(text, isChecked, onToggled, icon);

    public static RadioMenuFlyoutItemData RadioMenuItem(string text, string groupName, bool isChecked = false, Action? onClick = null, string? icon = null) => new(text, groupName, isChecked, onClick, icon);

    public static MenuFlyoutSeparatorData MenuSeparator() => new();

    public static MenuFlyoutSubItemData MenuSubItem(string text, params MenuFlyoutItemBase[] items) => new(text, items);

    public static MenuFlyoutElement MenuFlyout(Element target, params MenuFlyoutItemBase[] items) => new(target, items);

    public static CommandBarElement CommandBar(AppBarItemBase[]? primaryCommands = null, AppBarItemBase[]? secondaryCommands = null) =>
        new(primaryCommands, secondaryCommands);

    public static AppBarButtonData AppBarButton(string label, Action? onClick = null, string? icon = null) => new(label, onClick, icon);

    public static AppBarToggleButtonData AppBarToggleButton(string label, bool isChecked = false, Action<bool>? onToggled = null, string? icon = null) =>
        new(label, isChecked, onToggled, icon);

    public static AppBarSeparatorData AppBarSeparator() => new();

    // ── Media ───────────────────────────────────────────────────────

    public static ImageElement Image(string source) => new(source);

    public static PersonPictureElement PersonPicture() => new();

    public static WebView2Element WebView2(Uri? source = null) => new(source);

    public static MonacoEditorElement MonacoEditor(
        string text = "",
        Action<string>? onTextChanged = null,
        string language = "plaintext",
        string theme = "vs") =>
        new(text, onTextChanged) { Language = language, Theme = theme };

    // ── Components ──────────────────────────────────────────────────

    /// <summary>
    /// Embed a Component class as a child element.
    /// Usage: Component&lt;MyWidget&gt;()
    /// </summary>
    public static ComponentElement Component<T>() where T : Component, new() =>
        new(typeof(T)) { _factory = () => new T() };

    /// <summary>
    /// Embed a Component class with typed props as a child element.
    /// Usage: Component&lt;MyWidget, string&gt;("param")
    /// </summary>
    public static ComponentElement Component<T, TProps>(TProps props)
        where T : Component<TProps>, new() =>
        new(typeof(T), props) { _factory = () => new T() };

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

    // ── Typed (data-driven) collections ───────────────────────────

    public static TemplatedListViewElement<T> ListView<T>(
        IReadOnlyList<T> items,
        Func<T, string> keySelector,
        Func<T, int, Element> viewBuilder) => new(items, keySelector, viewBuilder);

    public static TemplatedGridViewElement<T> GridView<T>(
        IReadOnlyList<T> items,
        Func<T, string> keySelector,
        Func<T, int, Element> viewBuilder) => new(items, keySelector, viewBuilder);

    public static TemplatedFlipViewElement<T> FlipView<T>(
        IReadOnlyList<T> items,
        Func<T, string> keySelector,
        Func<T, int, Element> viewBuilder) => new(items, keySelector, viewBuilder);

    // ── Virtualized collections ───────────────────────────────────

    public static LazyVStackElement<T> LazyVStack<T>(
        IReadOnlyList<T> items,
        Func<T, string> keySelector,
        Func<T, int, Element> viewBuilder) => new(items, keySelector, viewBuilder);

    public static LazyHStackElement<T> LazyHStack<T>(
        IReadOnlyList<T> items,
        Func<T, string> keySelector,
        Func<T, int, Element> viewBuilder) => new(items, keySelector, viewBuilder);

    // ── Shapes ───────────────────────────────────────────────────────

    public static RectangleElement Rectangle() => new();

    public static EllipseElement Ellipse() => new();

    // ── Additional layout ───────────────────────────────────────────

    public static RelativePanelElement RelativePanel(params Element?[] children) => new(FilterChildren(children));

    // ── Additional media ────────────────────────────────────────────

    public static MediaPlayerElementElement MediaPlayerElement(string? source = null) => new(source);

    public static AnimatedVisualPlayerElement AnimatedVisualPlayer() => new();

    // ── Additional collections ──────────────────────────────────────

    public static SemanticZoomElement SemanticZoom(Element zoomedInView, Element zoomedOutView) =>
        new(zoomedInView, zoomedOutView);

    public static ListBoxElement ListBox(string[] items, int selectedIndex = -1, Action<int>? onSelectionChanged = null) =>
        new(items) { SelectedIndex = selectedIndex, OnSelectionChanged = onSelectionChanged };

    // ── Additional navigation ───────────────────────────────────────

    public static SelectorBarElement SelectorBar(SelectorBarItemData[] items, int selectedIndex = 0, Action<int>? onSelectionChanged = null) =>
        new(items) { SelectedIndex = selectedIndex, OnSelectionChanged = onSelectionChanged };

    public static SelectorBarItemData SelectorBarItem(string text, string? icon = null) => new(text, icon);

    public static PipsPagerElement PipsPager(int numberOfPages, int selectedPageIndex = 0, Action<int>? onSelectedIndexChanged = null) =>
        new(numberOfPages) { SelectedPageIndex = selectedPageIndex, OnSelectedIndexChanged = onSelectedIndexChanged };

    public static AnnotatedScrollBarElement AnnotatedScrollBar() => new();

    // ── Additional overlays / containers ────────────────────────────

    public static PopupElement Popup(Element child, bool isOpen = false, Action? onClosed = null) =>
        new(child) { IsOpen = isOpen, OnClosed = onClosed };

    public static RefreshContainerElement RefreshContainer(Element content, Action? onRefreshRequested = null) =>
        new(content) { OnRefreshRequested = onRefreshRequested };

    public static CommandBarFlyoutElement CommandBarFlyout(Element target, AppBarItemBase[]? primaryCommands = null, AppBarItemBase[]? secondaryCommands = null) =>
        new(target, primaryCommands, secondaryCommands);

    // ── Additional date / time ──────────────────────────────────────

    public static CalendarViewElement CalendarView() => new();

    // ── SwipeControl ────────────────────────────────────────────────

    public static SwipeControlElement SwipeControl(Element content,
        SwipeItemData[]? leftItems = null, SwipeItemData[]? rightItems = null) =>
        new(content) { LeftItems = leftItems, RightItems = rightItems };

    // ── AnimatedIcon ────────────────────────────────────────────────

    public static AnimatedIconElement AnimatedIcon(object? source = null, IconSource? fallbackIconSource = null) =>
        new() { Source = source, FallbackIconSource = fallbackIconSource };

    // ── ParallaxView ────────────────────────────────────────────────

    public static ParallaxViewElement ParallaxView(Element child, double verticalShift = 0, double horizontalShift = 0) =>
        new(child) { VerticalShift = verticalShift, HorizontalShift = horizontalShift };

    // ── MapControl ──────────────────────────────────────────────────

    public static MapControlElement MapControl(string? mapServiceToken = null, double zoomLevel = 1) =>
        new() { MapServiceToken = mapServiceToken, ZoomLevel = zoomLevel };

    // ── Frame ───────────────────────────────────────────────────────

    public static FrameElement Frame(Type? sourcePageType = null, object? navigationParameter = null) =>
        new() { SourcePageType = sourcePageType, NavigationParameter = navigationParameter };

    // ── ItemsView ───────────────────────────────────────────────────

    public static ItemsViewElement<T> ItemsView<T>(
        IReadOnlyList<T> items,
        Func<T, string> keySelector,
        Func<T, int, Element> viewBuilder) => new(items, keySelector, viewBuilder);

    // ── Rich text helpers ───────────────────────────────────────────

    public static RichTextBlockElement RichText(RichTextParagraph[] paragraphs) =>
        new("") { Paragraphs = paragraphs };

    public static RichTextParagraph Paragraph(params RichTextInline[] inlines) => new(inlines);

    public static RichTextRun Run(string text) => new(text);

    public static RichTextHyperlink Hyperlink(string text, Uri navigateUri) => new(text, navigateUri);

    // ── Markdown ─────────────────────────────────────────────────────

    /// <summary>
    /// Render a markdown string as a Duct element tree.
    /// </summary>
    public static Element Markdown(string markdown) =>
        MarkdownBuilder.Build(markdown, null);

    /// <summary>
    /// Render a markdown string as a Duct element tree with custom rendering options.
    /// </summary>
    public static Element Markdown(string markdown, MarkdownOptions options) =>
        MarkdownBuilder.Build(markdown, options);

    // ── Icons ────────────────────────────────────────────────────────

    public static SymbolIconData SymbolIcon(string symbol) => new(symbol);

    public static FontIconData FontIcon(string glyph, string? fontFamily = null, double? fontSize = null) =>
        new(glyph, fontFamily, fontSize);

    public static BitmapIconData BitmapIcon(System.Uri source, bool showAsMonochrome = true) =>
        new(source, showAsMonochrome);

    public static PathIconData PathIcon(string data) => new(data);

    public static ImageIconData ImageIcon(System.Uri source) => new(source);

    // ── Keyboard Accelerators ───────────────────────────────────────

    public static KeyboardAcceleratorData Accelerator(Windows.System.VirtualKey key, Windows.System.VirtualKeyModifiers modifiers = Windows.System.VirtualKeyModifiers.None) =>
        new(key, modifiers);

    // ── Brushes ─────────────────────────────────────────────────────

    public static Microsoft.UI.Xaml.Media.AcrylicBrush AcrylicBrush(
        Windows.UI.Color tintColor,
        double tintOpacity = 0.8,
        Windows.UI.Color? fallbackColor = null,
        double? tintLuminosityOpacity = null)
    {
        var brush = new Microsoft.UI.Xaml.Media.AcrylicBrush
        {
            TintColor = tintColor,
            TintOpacity = tintOpacity,
        };
        if (fallbackColor.HasValue) brush.FallbackColor = fallbackColor.Value;
        if (tintLuminosityOpacity.HasValue) brush.TintLuminosityOpacity = tintLuminosityOpacity.Value;
        return brush;
    }

    // ── Internals ───────────────────────────────────────────────────

    private static Element[] FilterChildren(Element?[] children) =>
        children.Where(c => c is not null).Select(c => c!).ToArray();
}

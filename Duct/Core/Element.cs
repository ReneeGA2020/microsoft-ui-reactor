using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using Windows.UI.Text;
using WinUI = Microsoft.UI.Xaml.Controls;
using WinPrim = Microsoft.UI.Xaml.Controls.Primitives;

namespace Duct.Core;

// ════════════════════════════════════════════════════════════════════════
//  Base types
// ════════════════════════════════════════════════════════════════════════

/// <summary>
/// A lightweight, immutable description of a UI node (the "virtual DOM").
/// Elements are cheap to create and diff — they never touch real controls directly.
/// </summary>
public abstract record Element
{
    /// <summary>
    /// Optional key for stable identity across re-renders (like React's key prop).
    /// When set, the reconciler uses it to match elements across list reorderings.
    /// </summary>
    public string? Key { get; init; }

    /// <summary>
    /// Layout modifiers (margin, padding, size, alignment, etc.) applied to this element.
    /// Set via fluent extension methods: Text("hi").Margin(10).Width(200)
    /// Modifiers are stored inline so the concrete element type is preserved through chaining.
    /// </summary>
    public ElementModifiers? Modifiers { get; init; }

    /// <summary>
    /// Convenience: implicitly convert a string to a TextElement.
    /// Allows writing: VStack("Hello", "World") instead of VStack(Text("Hello"), Text("World"))
    /// </summary>
    public static implicit operator Element(string text) => new TextElement(text);
}

/// <summary>
/// An element that renders nothing (used for conditional rendering).
/// </summary>
public record EmptyElement : Element
{
    public static readonly EmptyElement Instance = new();
}

/// <summary>
/// Wraps any element with layout modifiers (margin, alignment, size, etc.).
/// Kept for backward compatibility. New code stores modifiers inline on Element.Modifiers.
/// </summary>
public record ModifiedElement(Element Inner, ElementModifiers WrappedModifiers) : Element;

/// <summary>
/// Wraps a Component class so it can participate in the element tree.
/// Created automatically by Component&lt;T&gt;() factory method.
/// </summary>
public record ComponentElement(Type ComponentType, object? Props = null) : Element
{
    // Factory creates the component instance without reflection. Stored as a field
    // so it does not participate in record equality (two ComponentElements for the
    // same Type/Props are equal regardless of factory identity).
    internal Func<Component>? _factory;

    internal Component CreateInstance() =>
        _factory is not null ? _factory() : (Component)Activator.CreateInstance(ComponentType)!;
}

/// <summary>
/// A component defined inline via a render function (like a React function component).
/// </summary>
public record FuncElement(Func<RenderContext, Element> RenderFunc) : Element;

public record ElementModifiers
{
    public Thickness? Margin { get; init; }
    public Thickness? Padding { get; init; }
    public double? Width { get; init; }
    public double? Height { get; init; }
    public double? MinWidth { get; init; }
    public double? MinHeight { get; init; }
    public double? MaxWidth { get; init; }
    public double? MaxHeight { get; init; }
    public HorizontalAlignment? HorizontalAlignment { get; init; }
    public VerticalAlignment? VerticalAlignment { get; init; }
    public double? Opacity { get; init; }
    public bool? IsVisible { get; init; }
    public string? ToolTip { get; init; }

    public ElementModifiers Merge(ElementModifiers other)
    {
        return this with
        {
            Margin = other.Margin ?? Margin,
            Padding = other.Padding ?? Padding,
            Width = other.Width ?? Width,
            Height = other.Height ?? Height,
            MinWidth = other.MinWidth ?? MinWidth,
            MinHeight = other.MinHeight ?? MinHeight,
            MaxWidth = other.MaxWidth ?? MaxWidth,
            MaxHeight = other.MaxHeight ?? MaxHeight,
            HorizontalAlignment = other.HorizontalAlignment ?? HorizontalAlignment,
            VerticalAlignment = other.VerticalAlignment ?? VerticalAlignment,
            Opacity = other.Opacity ?? Opacity,
            IsVisible = other.IsVisible ?? IsVisible,
            ToolTip = other.ToolTip ?? ToolTip,
        };
    }
}

// Duct reuses WinUI types directly — no shadow enums.
// See: Microsoft.UI.Xaml (Thickness, HorizontalAlignment, VerticalAlignment)
//      Microsoft.UI.Xaml.Controls (Orientation, InfoBarSeverity, ExpandDirection, etc.)
//      Microsoft.UI.Xaml.Controls.Primitives (FlyoutPlacementMode)
//      Windows.UI.Text (FontWeight, FontWeights)

// ════════════════════════════════════════════════════════════════════════
//  Supporting data records (non-Element, used as structured params)
// ════════════════════════════════════════════════════════════════════════

public record GridDefinition(string[] Columns, string[] Rows);
public record GridChild(Element Element, int Row = 0, int Column = 0, int RowSpan = 1, int ColumnSpan = 1);

public record CanvasChild(Element Element, double Left = 0, double Top = 0);

public record NavigationViewItemData(string Content, string? Icon = null, string? Tag = null)
{
    public NavigationViewItemData[]? Children { get; init; }
}

public record TabViewItemData(string Header, Element Content)
{
    public string? Icon { get; init; }
    public bool IsClosable { get; init; } = true;
}

public record PivotItemData(string Header, Element Content);

public record BreadcrumbBarItemData(string Label, object? Tag = null);

public record TreeViewNodeData(string Content, TreeViewNodeData[]? Children = null)
{
    public bool IsExpanded { get; init; }

    /// <summary>
    /// Optional Duct element to render as the node's visual content.
    /// When null, a TextBlock showing Content is rendered.
    /// </summary>
    public Element? ContentElement { get; init; }
}

public record MenuBarItemData(string Title, MenuFlyoutItemBase[] Items);

public abstract record MenuFlyoutItemBase;
public record MenuFlyoutItemData(string Text, Action? OnClick = null, string? Icon = null) : MenuFlyoutItemBase;
public record MenuFlyoutSeparatorData() : MenuFlyoutItemBase;
public record MenuFlyoutSubItemData(string Text, MenuFlyoutItemBase[] Items, string? Icon = null) : MenuFlyoutItemBase;

public abstract record AppBarItemBase;
public record AppBarButtonData(string Label, Action? OnClick = null, string? Icon = null) : AppBarItemBase;
public record AppBarToggleButtonData(string Label, bool IsChecked = false, Action<bool>? OnToggled = null, string? Icon = null) : AppBarItemBase;
public record AppBarSeparatorData() : AppBarItemBase;

// ════════════════════════════════════════════════════════════════════════
//  Text elements
// ════════════════════════════════════════════════════════════════════════

public record TextElement(string Content) : Element
{
    public double? FontSize { get; init; }
    public FontWeight? Weight { get; init; }
    public HorizontalAlignment? HorizontalAlignment { get; init; }
    internal Action<WinUI.TextBlock>[] Setters { get; init; } = [];
}

public record RichTextBlockElement(string Text) : Element
{
    public double? FontSize { get; init; }
    internal Action<WinUI.RichTextBlock>[] Setters { get; init; } = [];
}

// ════════════════════════════════════════════════════════════════════════
//  Button elements
// ════════════════════════════════════════════════════════════════════════

public record ButtonElement(string Label, Action? OnClick = null) : Element
{
    public bool IsEnabled { get; init; } = true;
    internal Action<WinUI.Button>[] Setters { get; init; } = [];
}

public record HyperlinkButtonElement(string Content, Uri? NavigateUri = null, Action? OnClick = null) : Element
{
    internal Action<WinUI.HyperlinkButton>[] Setters { get; init; } = [];
}

public record RepeatButtonElement(string Label, Action? OnClick = null) : Element
{
    public int Delay { get; init; } = 250;
    public int Interval { get; init; } = 50;
    internal Action<WinPrim.RepeatButton>[] Setters { get; init; } = [];
}

public record ToggleButtonElement(string Label, bool IsChecked = false, Action<bool>? OnToggled = null) : Element
{
    internal Action<WinPrim.ToggleButton>[] Setters { get; init; } = [];
}

public record DropDownButtonElement(string Label, Element? Flyout = null) : Element
{
    internal Action<WinUI.DropDownButton>[] Setters { get; init; } = [];
}

public record SplitButtonElement(string Label, Action? OnClick = null, Element? Flyout = null) : Element
{
    internal Action<WinUI.SplitButton>[] Setters { get; init; } = [];
}

public record ToggleSplitButtonElement(string Label, bool IsChecked = false, Action<bool>? OnIsCheckedChanged = null, Element? Flyout = null) : Element
{
    internal Action<WinUI.ToggleSplitButton>[] Setters { get; init; } = [];
}

// ════════════════════════════════════════════════════════════════════════
//  Input elements
// ════════════════════════════════════════════════════════════════════════

public record TextFieldElement(
    string Value,
    Action<string>? OnChanged = null,
    string? Placeholder = null
) : Element
{
    internal Action<WinUI.TextBox>[] Setters { get; init; } = [];
}

public record PasswordBoxElement(
    string Password,
    Action<string>? OnPasswordChanged = null,
    string? PlaceholderText = null
) : Element
{
    internal Action<WinUI.PasswordBox>[] Setters { get; init; } = [];
}

public record NumberBoxElement(
    double Value,
    Action<double>? OnValueChanged = null,
    string? Header = null
) : Element
{
    public double Minimum { get; init; } = double.MinValue;
    public double Maximum { get; init; } = double.MaxValue;
    public string? PlaceholderText { get; init; }
    public NumberBoxSpinButtonPlacementMode SpinButtonPlacement { get; init; } = NumberBoxSpinButtonPlacementMode.Hidden;
    public double SmallChange { get; init; } = 1;
    public double LargeChange { get; init; } = 10;
    internal Action<WinUI.NumberBox>[] Setters { get; init; } = [];
}

public record AutoSuggestBoxElement(
    string Text,
    Action<string>? OnTextChanged = null,
    Action<string>? OnQuerySubmitted = null,
    Action<string>? OnSuggestionChosen = null
) : Element
{
    public string[] Suggestions { get; init; } = [];
    public string? PlaceholderText { get; init; }
    internal Action<WinUI.AutoSuggestBox>[] Setters { get; init; } = [];
}

public record CheckBoxElement(
    bool IsChecked,
    Action<bool>? OnChanged = null,
    string? Label = null
) : Element
{
    internal Action<WinUI.CheckBox>[] Setters { get; init; } = [];
}

public record RadioButtonElement(
    string Label,
    bool IsChecked = false,
    Action<bool>? OnChecked = null,
    string? GroupName = null
) : Element
{
    internal Action<WinUI.RadioButton>[] Setters { get; init; } = [];
}

public record RadioButtonsElement(
    string[] Items,
    int SelectedIndex = -1,
    Action<int>? OnSelectionChanged = null
) : Element
{
    public string? Header { get; init; }
    internal Action<WinUI.RadioButtons>[] Setters { get; init; } = [];
}

public record ComboBoxElement(
    string[] Items,
    int SelectedIndex = -1,
    Action<int>? OnSelectionChanged = null
) : Element
{
    public string? PlaceholderText { get; init; }
    public string? Header { get; init; }
    public bool IsEditable { get; init; }
    internal Action<WinUI.ComboBox>[] Setters { get; init; } = [];
}

public record SliderElement(
    double Value,
    double Min = 0,
    double Max = 100,
    Action<double>? OnChanged = null
) : Element
{
    public double StepFrequency { get; init; } = 1;
    public string? Header { get; init; }
    internal Action<WinUI.Slider>[] Setters { get; init; } = [];
}

public record ToggleSwitchElement(
    bool IsOn,
    Action<bool>? OnChanged = null,
    string? OnContent = null,
    string? OffContent = null
) : Element
{
    public string? Header { get; init; }
    internal Action<WinUI.ToggleSwitch>[] Setters { get; init; } = [];
}

public record RatingControlElement(
    double Value = 0,
    Action<double>? OnValueChanged = null
) : Element
{
    public int MaxRating { get; init; } = 5;
    public bool IsReadOnly { get; init; }
    public string? Caption { get; init; }
    internal Action<WinUI.RatingControl>[] Setters { get; init; } = [];
}

public record ColorPickerElement(
    Windows.UI.Color Color,
    Action<Windows.UI.Color>? OnColorChanged = null
) : Element
{
    public bool IsAlphaEnabled { get; init; }
    public bool IsMoreButtonVisible { get; init; }
    public bool IsColorSpectrumVisible { get; init; } = true;
    public bool IsColorSliderVisible { get; init; } = true;
    public bool IsColorChannelTextInputVisible { get; init; } = true;
    public bool IsHexInputVisible { get; init; } = true;
    internal Action<WinUI.ColorPicker>[] Setters { get; init; } = [];
}

// ════════════════════════════════════════════════════════════════════════
//  Date / Time elements
// ════════════════════════════════════════════════════════════════════════

public record CalendarDatePickerElement(
    DateTimeOffset? Date = null,
    Action<DateTimeOffset?>? OnDateChanged = null
) : Element
{
    public string? PlaceholderText { get; init; }
    public string? Header { get; init; }
    public DateTimeOffset? MinDate { get; init; }
    public DateTimeOffset? MaxDate { get; init; }
    internal Action<WinUI.CalendarDatePicker>[] Setters { get; init; } = [];
}

public record DatePickerElement(
    DateTimeOffset Date,
    Action<DateTimeOffset>? OnDateChanged = null
) : Element
{
    public string? Header { get; init; }
    public DateTimeOffset? MinYear { get; init; }
    public DateTimeOffset? MaxYear { get; init; }
    public bool DayVisible { get; init; } = true;
    public bool MonthVisible { get; init; } = true;
    public bool YearVisible { get; init; } = true;
    internal Action<WinUI.DatePicker>[] Setters { get; init; } = [];
}

public record TimePickerElement(
    TimeSpan Time,
    Action<TimeSpan>? OnTimeChanged = null
) : Element
{
    public string? Header { get; init; }
    public int MinuteIncrement { get; init; } = 1;
    public int ClockIdentifier { get; init; } = 12;
    internal Action<WinUI.TimePicker>[] Setters { get; init; } = [];
}

// ════════════════════════════════════════════════════════════════════════
//  Progress elements
// ════════════════════════════════════════════════════════════════════════

public record ProgressElement(double? Value = null) : Element  // null = indeterminate
{
    public bool IsIndeterminate => Value is null;
    public double Minimum { get; init; } = 0;
    public double Maximum { get; init; } = 100;
    public bool ShowError { get; init; }
    public bool ShowPaused { get; init; }
    internal Action<WinUI.ProgressBar>[] Setters { get; init; } = [];
}

public record ProgressRingElement(double? Value = null) : Element
{
    public bool IsIndeterminate => Value is null;
    public double Minimum { get; init; } = 0;
    public double Maximum { get; init; } = 100;
    public bool IsActive { get; init; } = true;
    internal Action<WinUI.ProgressRing>[] Setters { get; init; } = [];
}

// ════════════════════════════════════════════════════════════════════════
//  Media elements
// ════════════════════════════════════════════════════════════════════════

public record ImageElement(string Source) : Element
{
    public double? Width { get; init; }
    public double? Height { get; init; }
    public string? Stretch { get; init; }
    internal Action<WinUI.Image>[] Setters { get; init; } = [];
}

public record PersonPictureElement() : Element
{
    public string? DisplayName { get; init; }
    public string? Initials { get; init; }
    public string? ProfilePicture { get; init; }
    public bool IsGroup { get; init; }
    public int BadgeNumber { get; init; }
    internal Action<WinUI.PersonPicture>[] Setters { get; init; } = [];
}

public record WebView2Element(Uri? Source = null) : Element
{
    public Action<Uri>? OnNavigationStarting { get; init; }
    public Action<Uri>? OnNavigationCompleted { get; init; }
    internal Action<WinUI.WebView2>[] Setters { get; init; } = [];
}

// ════════════════════════════════════════════════════════════════════════
//  Layout / Container elements
// ════════════════════════════════════════════════════════════════════════

public record StackElement(
    Orientation Orientation,
    Element[] Children
) : Element
{
    public double Spacing { get; init; } = 8;
    public HorizontalAlignment? HorizontalAlignment { get; init; }
    public VerticalAlignment? VerticalAlignment { get; init; }
    internal Action<WinUI.StackPanel>[] Setters { get; init; } = [];
}

public record GridElement(
    GridDefinition Definition,
    GridChild[] Children
) : Element
{
    public double RowSpacing { get; init; }
    public double ColumnSpacing { get; init; }
    internal Action<WinUI.Grid>[] Setters { get; init; } = [];
}

public record ScrollViewElement(Element Child) : Element
{
    public Orientation Orientation { get; init; } = Orientation.Vertical;
    public ScrollBarVisibility HorizontalScrollBarVisibility { get; init; } = ScrollBarVisibility.Auto;
    public ScrollBarVisibility VerticalScrollBarVisibility { get; init; } = ScrollBarVisibility.Auto;
    internal Action<WinUI.ScrollViewer>[] Setters { get; init; } = [];
}

public record BorderElement(Element Child) : Element
{
    public double? CornerRadius { get; init; }
    public Thickness? Padding { get; init; }
    public Brush? Background { get; init; }
    public Brush? BorderBrush { get; init; }
    public double? BorderThickness { get; init; }
    internal Action<WinUI.Border>[] Setters { get; init; } = [];
}

public record ExpanderElement(
    string Header,
    Element Content,
    bool IsExpanded = false,
    Action<bool>? OnExpandedChanged = null
) : Element
{
    public ExpandDirection ExpandDirection { get; init; } = ExpandDirection.Down;
    internal Action<WinUI.Expander>[] Setters { get; init; } = [];
}

public record SplitViewElement(
    Element? Pane = null,
    Element? Content = null
) : Element
{
    public bool IsPaneOpen { get; init; } = true;
    public double OpenPaneLength { get; init; } = 320;
    public double CompactPaneLength { get; init; } = 48;
    public SplitViewDisplayMode DisplayMode { get; init; } = SplitViewDisplayMode.Overlay;
    public Action<bool>? OnPaneOpenChanged { get; init; }
    internal Action<WinUI.SplitView>[] Setters { get; init; } = [];
}

public record ViewboxElement(Element Child) : Element
{
    public string? Stretch { get; init; }
    internal Action<WinUI.Viewbox>[] Setters { get; init; } = [];
}

public record CanvasElement(CanvasChild[] Children) : Element
{
    public double? Width { get; init; }
    public double? Height { get; init; }
    public Brush? Background { get; init; }
    internal Action<WinUI.Canvas>[] Setters { get; init; } = [];
}

// ════════════════════════════════════════════════════════════════════════
//  Navigation elements
// ════════════════════════════════════════════════════════════════════════

public record NavigationViewElement(
    NavigationViewItemData[] MenuItems,
    Element? Content = null
) : Element
{
    public string? SelectedTag { get; init; }
    public Action<string?>? OnSelectionChanged { get; init; }
    public bool IsPaneOpen { get; init; } = true;
    public NavigationViewPaneDisplayMode PaneDisplayMode { get; init; } = NavigationViewPaneDisplayMode.Auto;
    public bool IsBackEnabled { get; init; }
    public Action? OnBackRequested { get; init; }
    public Element? Header { get; init; }
    public bool IsSettingsVisible { get; init; } = true;
    public string? PaneTitle { get; init; }
    internal Action<WinUI.NavigationView>[] Setters { get; init; } = [];
}

public record TabViewElement(
    TabViewItemData[] Tabs
) : Element
{
    public int SelectedIndex { get; init; } = 0;
    public Action<int>? OnSelectionChanged { get; init; }
    public Action<int>? OnTabCloseRequested { get; init; }
    public Action? OnAddTabButtonClick { get; init; }
    public bool IsAddTabButtonVisible { get; init; }
    internal Action<WinUI.TabView>[] Setters { get; init; } = [];
}

public record BreadcrumbBarElement(
    BreadcrumbBarItemData[] Items,
    Action<BreadcrumbBarItemData>? OnItemClicked = null
) : Element
{
    internal Action<WinUI.BreadcrumbBar>[] Setters { get; init; } = [];
}

public record PivotElement(
    PivotItemData[] Items
) : Element
{
    public int SelectedIndex { get; init; } = 0;
    public Action<int>? OnSelectionChanged { get; init; }
    public string? Title { get; init; }
    internal Action<WinUI.Pivot>[] Setters { get; init; } = [];
}

// ════════════════════════════════════════════════════════════════════════
//  Collection elements (simple, no item templating)
// ════════════════════════════════════════════════════════════════════════

public record ListViewElement(
    Element[] Items
) : Element
{
    public int SelectedIndex { get; init; } = -1;
    public Action<int>? OnSelectionChanged { get; init; }
    public Action<int>? OnItemClick { get; init; }
    public ListViewSelectionMode SelectionMode { get; init; } = ListViewSelectionMode.Single;
    public string? Header { get; init; }
    internal Action<WinUI.ListView>[] Setters { get; init; } = [];
}

public record GridViewElement(
    Element[] Items
) : Element
{
    public int SelectedIndex { get; init; } = -1;
    public Action<int>? OnSelectionChanged { get; init; }
    public Action<int>? OnItemClick { get; init; }
    public ListViewSelectionMode SelectionMode { get; init; } = ListViewSelectionMode.Single;
    public string? Header { get; init; }
    internal Action<WinUI.GridView>[] Setters { get; init; } = [];
}

public record TreeViewElement(
    TreeViewNodeData[] Nodes
) : Element
{
    public Action<TreeViewNodeData>? OnItemInvoked { get; init; }
    public Action<TreeViewNodeData>? OnExpanding { get; init; }
    public TreeViewSelectionMode SelectionMode { get; init; } = TreeViewSelectionMode.Single;
    internal Action<WinUI.TreeView>[] Setters { get; init; } = [];
}

public record FlipViewElement(
    Element[] Items
) : Element
{
    public int SelectedIndex { get; init; } = 0;
    public Action<int>? OnSelectionChanged { get; init; }
    internal Action<WinUI.FlipView>[] Setters { get; init; } = [];
}

// ════════════════════════════════════════════════════════════════════════
//  Dialog / Overlay elements
// ════════════════════════════════════════════════════════════════════════

/// <summary>
/// Declarative content dialog. Set IsOpen to true to show.
/// OnClosed fires with the result when the user dismisses the dialog.
/// </summary>
public record ContentDialogElement(
    string Title,
    Element Content,
    string PrimaryButtonText = "OK"
) : Element
{
    public bool IsOpen { get; init; }
    public string? SecondaryButtonText { get; init; }
    public string? CloseButtonText { get; init; }
    public ContentDialogButton DefaultButton { get; init; } = ContentDialogButton.Primary;
    public Action<ContentDialogResult>? OnClosed { get; init; }
    internal Action<WinUI.ContentDialog>[] Setters { get; init; } = [];
}

/// <summary>
/// A flyout attached to another element. Wrap the target element.
/// </summary>
public record FlyoutElement(
    Element Target,
    Element FlyoutContent
) : Element
{
    public bool IsOpen { get; init; }
    public FlyoutPlacementMode Placement { get; init; } = FlyoutPlacementMode.Auto;
    public Action? OnOpened { get; init; }
    public Action? OnClosed { get; init; }
    internal Action<WinUI.Flyout>[] Setters { get; init; } = [];
}

public record TeachingTipElement(
    string Title,
    string? Subtitle = null
) : Element
{
    public bool IsOpen { get; init; }
    public Element? Content { get; init; }
    public string? ActionButtonContent { get; init; }
    public Action? OnActionButtonClick { get; init; }
    public string? CloseButtonContent { get; init; }
    public Action? OnClosed { get; init; }
    internal Action<WinUI.TeachingTip>[] Setters { get; init; } = [];
}

// ════════════════════════════════════════════════════════════════════════
//  Status / Info elements
// ════════════════════════════════════════════════════════════════════════

public record InfoBarElement(
    string? Title = null,
    string? Message = null
) : Element
{
    public InfoBarSeverity Severity { get; init; } = InfoBarSeverity.Informational;
    public bool IsOpen { get; init; } = true;
    public bool IsClosable { get; init; } = true;
    public string? ActionButtonContent { get; init; }
    public Action? OnActionButtonClick { get; init; }
    public Action? OnClosed { get; init; }
    internal Action<WinUI.InfoBar>[] Setters { get; init; } = [];
}

public record InfoBadgeElement() : Element
{
    public int? Value { get; init; }
    public string? Icon { get; init; }
    internal Action<WinUI.InfoBadge>[] Setters { get; init; } = [];
}

// ════════════════════════════════════════════════════════════════════════
//  Menu elements
// ════════════════════════════════════════════════════════════════════════

public record MenuBarElement(MenuBarItemData[] Items) : Element
{
    internal Action<WinUI.MenuBar>[] Setters { get; init; } = [];
}

public record CommandBarElement(
    AppBarItemBase[]? PrimaryCommands = null,
    AppBarItemBase[]? SecondaryCommands = null
) : Element
{
    public CommandBarDefaultLabelPosition DefaultLabelPosition { get; init; } = CommandBarDefaultLabelPosition.Bottom;
    public bool IsOpen { get; init; }
    public Element? Content { get; init; }
    internal Action<WinUI.CommandBar>[] Setters { get; init; } = [];
}

public record MenuFlyoutElement(
    Element Target,
    MenuFlyoutItemBase[] Items
) : Element
{
    internal Action<WinUI.MenuFlyout>[] Setters { get; init; } = [];
}

// ════════════════════════════════════════════════════════════════════════
//  Virtualized collection elements (backed by ItemsRepeater)
// ════════════════════════════════════════════════════════════════════════

/// <summary>
/// Abstract base for virtualized lazy stacks. Non-generic so the reconciler
/// can match on a single type in its switch expression.
/// </summary>
public abstract record LazyStackElementBase : Element
{
    public abstract Orientation Orientation { get; }
    public abstract double Spacing { get; init; }
    public abstract double EstimatedItemSize { get; init; }
    public abstract object GetItemsSource();
    public abstract ElementFactory CreateFactory(Reconciler reconciler, Action requestRerender, ElementPool? pool);
    internal Action<WinUI.ScrollViewer>[] ScrollViewerSetters { get; init; } = [];
    internal Action<WinUI.ItemsRepeater>[] RepeaterSetters { get; init; } = [];
}

public record LazyVStackElement<T>(
    IReadOnlyList<T> Items,
    Func<T, string> KeySelector,
    Func<T, int, Element> ViewBuilder
) : LazyStackElementBase
{
    public override Orientation Orientation => Orientation.Vertical;
    public override double Spacing { get; init; } = 8;
    public override double EstimatedItemSize { get; init; } = 40;

    public override object GetItemsSource() =>
        Enumerable.Range(0, Items.Count).ToList();

    public override ElementFactory CreateFactory(Reconciler reconciler, Action requestRerender, ElementPool? pool) =>
        new DuctElementFactory<T>(Items, ViewBuilder, reconciler, requestRerender, pool);
}

public record LazyHStackElement<T>(
    IReadOnlyList<T> Items,
    Func<T, string> KeySelector,
    Func<T, int, Element> ViewBuilder
) : LazyStackElementBase
{
    public override Orientation Orientation => Orientation.Horizontal;
    public override double Spacing { get; init; } = 8;
    public override double EstimatedItemSize { get; init; } = 100;

    public override object GetItemsSource() =>
        Enumerable.Range(0, Items.Count).ToList();

    public override ElementFactory CreateFactory(Reconciler reconciler, Action requestRerender, ElementPool? pool) =>
        new DuctElementFactory<T>(Items, ViewBuilder, reconciler, requestRerender, pool);
}

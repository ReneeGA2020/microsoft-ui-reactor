using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using Windows.UI.Text;
using WinUI = Microsoft.UI.Xaml.Controls;
using WinPrim = Microsoft.UI.Xaml.Controls.Primitives;

namespace Duct;

/// <summary>
/// Fluent modifier extension methods for elements.
/// These wrap an element in a ModifiedElement with layout/style properties.
///
/// Usage:
///   Text("Hello")
///       .Bold()
///       .Margin(16)
///       .HAlign(HorizontalAlignment.Center)
///
/// The Set() extension gives strongly-typed native property access:
///   Button("Click", onClick)
///       .Set(b => b.FlowDirection = FlowDirection.RightToLeft)
/// </summary>
public static class ElementExtensions
{
    // ════════════════════════════════════════════════════════════════
    //  Layout modifiers (apply to any Element via ModifiedElement)
    // ════════════════════════════════════════════════════════════════

    public static Element Margin(this Element el, double uniform) =>
        Modify(el, new ElementModifiers { Margin = new Thickness(uniform) });

    public static Element Margin(this Element el, double horizontal, double vertical) =>
        Modify(el, new ElementModifiers { Margin = new Thickness(horizontal, vertical, horizontal, vertical) });

    public static Element Margin(this Element el, double left, double top, double right, double bottom) =>
        Modify(el, new ElementModifiers { Margin = new Thickness(left, top, right, bottom) });

    public static Element Padding(this Element el, double uniform) =>
        Modify(el, new ElementModifiers { Padding = new Thickness(uniform) });

    public static Element Padding(this Element el, double horizontal, double vertical) =>
        Modify(el, new ElementModifiers { Padding = new Thickness(horizontal, vertical, horizontal, vertical) });

    public static Element Width(this Element el, double width) =>
        Modify(el, new ElementModifiers { Width = width });

    public static Element Height(this Element el, double height) =>
        Modify(el, new ElementModifiers { Height = height });

    public static Element Size(this Element el, double width, double height) =>
        Modify(el, new ElementModifiers { Width = width, Height = height });

    public static Element MinWidth(this Element el, double w) =>
        Modify(el, new ElementModifiers { MinWidth = w });

    public static Element MinHeight(this Element el, double h) =>
        Modify(el, new ElementModifiers { MinHeight = h });

    public static Element MaxWidth(this Element el, double w) =>
        Modify(el, new ElementModifiers { MaxWidth = w });

    public static Element MaxHeight(this Element el, double h) =>
        Modify(el, new ElementModifiers { MaxHeight = h });

    // ── Alignment ───────────────────────────────────────────────────

    public static Element HAlign(this Element el, HorizontalAlignment alignment) =>
        Modify(el, new ElementModifiers { HorizontalAlignment = alignment });

    public static Element VAlign(this Element el, VerticalAlignment alignment) =>
        Modify(el, new ElementModifiers { VerticalAlignment = alignment });

    public static Element Center(this Element el) =>
        Modify(el, new ElementModifiers
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });

    // ── Visibility ──────────────────────────────────────────────────

    public static Element Visible(this Element el, bool isVisible) =>
        Modify(el, new ElementModifiers { IsVisible = isVisible });

    public static Element Opacity(this Element el, double opacity) =>
        Modify(el, new ElementModifiers { Opacity = opacity });

    // ── Decoration ──────────────────────────────────────────────────

    public static Element ToolTip(this Element el, string tip) =>
        Modify(el, new ElementModifiers { ToolTip = tip });

    // ── Theme / Style ───────────────────────────────────────────────

    /// <summary>
    /// Apply a named WinUI Style to the element's control at mount/update time.
    /// Usage: Text("Hello").ApplyStyle("BodyTextBlockStyle")
    /// </summary>
    public static TextElement ApplyStyle(this TextElement el, string styleName) =>
        el.Set(ctrl => ctrl.Style = (Style)Application.Current.Resources[styleName]);

    public static ButtonElement ApplyStyle(this ButtonElement el, string styleName) =>
        el.Set(ctrl => ctrl.Style = (Style)Application.Current.Resources[styleName]);

    public static BorderElement ApplyStyle(this BorderElement el, string styleName) =>
        el.Set(ctrl => ctrl.Style = (Style)Application.Current.Resources[styleName]);

    // ════════════════════════════════════════════════════════════════
    //  Sugar extensions (typed, return concrete element type)
    // ════════════════════════════════════════════════════════════════

    // ── Text sugar ──────────────────────────────────────────────────

    public static TextElement Bold(this TextElement el) =>
        el with { Weight = FontWeights.Bold };

    public static TextElement SemiBold(this TextElement el) =>
        el with { Weight = FontWeights.SemiBold };

    public static TextElement FontSize(this TextElement el, double size) =>
        el with { FontSize = size };

    // ── Button sugar ────────────────────────────────────────────────

    public static ButtonElement Disabled(this ButtonElement el, bool disabled = true) =>
        el with { IsEnabled = !disabled };

    // ── Border sugar ────────────────────────────────────────────────

    public static BorderElement CornerRadius(this BorderElement el, double radius) =>
        el with { CornerRadius = radius };

    public static BorderElement Background(this BorderElement el, string color) =>
        el with { Background = BrushHelper.Parse(color) };

    public static BorderElement Background(this BorderElement el, Brush brush) =>
        el with { Background = brush };

    public static BorderElement WithBorder(this BorderElement el, string color, double thickness = 1) =>
        el with { BorderBrush = BrushHelper.Parse(color), BorderThickness = thickness };

    public static BorderElement WithBorder(this BorderElement el, Brush brush, double thickness = 1) =>
        el with { BorderBrush = brush, BorderThickness = thickness };

    public static BorderElement Padding(this BorderElement el, Thickness padding) =>
        el with { Padding = padding };

    // ── Stack sugar ─────────────────────────────────────────────────

    public static StackElement Spacing(this StackElement el, double spacing) =>
        el with { Spacing = spacing };

    // ── ComboBox sugar ──────────────────────────────────────────────

    public static ComboBoxElement Placeholder(this ComboBoxElement el, string text) =>
        el with { PlaceholderText = text };

    public static ComboBoxElement Editable(this ComboBoxElement el, bool editable = true) =>
        el with { IsEditable = editable };

    // ── NumberBox sugar ─────────────────────────────────────────────

    public static NumberBoxElement Range(this NumberBoxElement el, double min, double max) =>
        el with { Minimum = min, Maximum = max };

    public static NumberBoxElement SpinButtons(this NumberBoxElement el, NumberBoxSpinButtonPlacementMode placement = NumberBoxSpinButtonPlacementMode.Inline) =>
        el with { SpinButtonPlacement = placement };

    // ── Slider sugar ────────────────────────────────────────────────

    public static SliderElement StepFrequency(this SliderElement el, double step) =>
        el with { StepFrequency = step };

    // ── RatingControl sugar ─────────────────────────────────────────

    public static RatingControlElement MaxRating(this RatingControlElement el, int max) =>
        el with { MaxRating = max };

    public static RatingControlElement ReadOnly(this RatingControlElement el, bool readOnly = true) =>
        el with { IsReadOnly = readOnly };

    // ── InfoBar sugar ───────────────────────────────────────────────

    public static InfoBarElement Severity(this InfoBarElement el, InfoBarSeverity severity) =>
        el with { Severity = severity };

    public static InfoBarElement Closable(this InfoBarElement el, bool closable = true) =>
        el with { IsClosable = closable };

    // ── NavigationView sugar ────────────────────────────────────────

    public static NavigationViewElement PaneDisplayMode(this NavigationViewElement el, NavigationViewPaneDisplayMode mode) =>
        el with { PaneDisplayMode = mode };

    public static NavigationViewElement PaneTitle(this NavigationViewElement el, string title) =>
        el with { PaneTitle = title };

    // ── ExpanderElement sugar ───────────────────────────────────────

    public static ExpanderElement Direction(this ExpanderElement el, ExpandDirection dir) =>
        el with { ExpandDirection = dir };

    // ── Expander sugar ──────────────────────────────────────────────

    // ── RepeatButton sugar ──────────────────────────────────────────

    public static RepeatButtonElement Delay(this RepeatButtonElement el, int delay) =>
        el with { Delay = delay };

    public static RepeatButtonElement Interval(this RepeatButtonElement el, int interval) =>
        el with { Interval = interval };

    // ── ProgressRing sugar ──────────────────────────────────────────

    public static ProgressRingElement Active(this ProgressRingElement el, bool active = true) =>
        el with { IsActive = active };

    // ── PersonPicture sugar ─────────────────────────────────────────

    public static PersonPictureElement DisplayName(this PersonPictureElement el, string name) =>
        el with { DisplayName = name };

    public static PersonPictureElement Initials(this PersonPictureElement el, string initials) =>
        el with { Initials = initials };

    // ── ListView / GridView sugar ───────────────────────────────────

    public static ListViewElement SelectionMode(this ListViewElement el, ListViewSelectionMode mode) =>
        el with { SelectionMode = mode };

    public static GridViewElement SelectionMode(this GridViewElement el, ListViewSelectionMode mode) =>
        el with { SelectionMode = mode };

    // ── TabView sugar ───────────────────────────────────────────────

    public static TabViewElement ShowAddButton(this TabViewElement el, bool visible = true) =>
        el with { IsAddTabButtonVisible = visible };

    // ── Key ─────────────────────────────────────────────────────────

    public static T WithKey<T>(this T el, string key) where T : Element =>
        el with { Key = key };

    // ════════════════════════════════════════════════════════════════
    //  Set() — strongly-typed native property access per element type
    //
    //  Usage:  Button("Go", onClick).Set(b => b.FlowDirection = FlowDirection.RightToLeft)
    //
    //  The lambda parameter is the actual WinUI control type, giving you
    //  full IntelliSense and compile-time type checking for every property.
    //  Setters are applied at both mount and update (idempotent property sets).
    // ════════════════════════════════════════════════════════════════

    // Text
    public static TextElement Set(this TextElement el, Action<WinUI.TextBlock> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static RichTextBlockElement Set(this RichTextBlockElement el, Action<WinUI.RichTextBlock> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // Buttons
    public static ButtonElement Set(this ButtonElement el, Action<WinUI.Button> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static HyperlinkButtonElement Set(this HyperlinkButtonElement el, Action<WinUI.HyperlinkButton> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static RepeatButtonElement Set(this RepeatButtonElement el, Action<WinPrim.RepeatButton> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static ToggleButtonElement Set(this ToggleButtonElement el, Action<WinPrim.ToggleButton> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static DropDownButtonElement Set(this DropDownButtonElement el, Action<WinUI.DropDownButton> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static SplitButtonElement Set(this SplitButtonElement el, Action<WinUI.SplitButton> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static ToggleSplitButtonElement Set(this ToggleSplitButtonElement el, Action<WinUI.ToggleSplitButton> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // Input
    public static TextFieldElement Set(this TextFieldElement el, Action<WinUI.TextBox> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static PasswordBoxElement Set(this PasswordBoxElement el, Action<WinUI.PasswordBox> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static NumberBoxElement Set(this NumberBoxElement el, Action<WinUI.NumberBox> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static AutoSuggestBoxElement Set(this AutoSuggestBoxElement el, Action<WinUI.AutoSuggestBox> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static CheckBoxElement Set(this CheckBoxElement el, Action<WinUI.CheckBox> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static RadioButtonElement Set(this RadioButtonElement el, Action<WinUI.RadioButton> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static RadioButtonsElement Set(this RadioButtonsElement el, Action<WinUI.RadioButtons> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static ComboBoxElement Set(this ComboBoxElement el, Action<WinUI.ComboBox> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static SliderElement Set(this SliderElement el, Action<WinUI.Slider> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static ToggleSwitchElement Set(this ToggleSwitchElement el, Action<WinUI.ToggleSwitch> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static RatingControlElement Set(this RatingControlElement el, Action<WinUI.RatingControl> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static ColorPickerElement Set(this ColorPickerElement el, Action<WinUI.ColorPicker> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // Date/Time
    public static CalendarDatePickerElement Set(this CalendarDatePickerElement el, Action<WinUI.CalendarDatePicker> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static DatePickerElement Set(this DatePickerElement el, Action<WinUI.DatePicker> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static TimePickerElement Set(this TimePickerElement el, Action<WinUI.TimePicker> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // Progress
    public static ProgressElement Set(this ProgressElement el, Action<WinUI.ProgressBar> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static ProgressRingElement Set(this ProgressRingElement el, Action<WinUI.ProgressRing> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // Media
    public static ImageElement Set(this ImageElement el, Action<WinUI.Image> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static PersonPictureElement Set(this PersonPictureElement el, Action<WinUI.PersonPicture> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static WebView2Element Set(this WebView2Element el, Action<WinUI.WebView2> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // Layout / Containers
    public static StackElement Set(this StackElement el, Action<WinUI.StackPanel> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static GridElement Set(this GridElement el, Action<WinUI.Grid> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static ScrollViewElement Set(this ScrollViewElement el, Action<WinUI.ScrollViewer> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static BorderElement Set(this BorderElement el, Action<WinUI.Border> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static ExpanderElement Set(this ExpanderElement el, Action<WinUI.Expander> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static SplitViewElement Set(this SplitViewElement el, Action<WinUI.SplitView> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static ViewboxElement Set(this ViewboxElement el, Action<WinUI.Viewbox> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static CanvasElement Set(this CanvasElement el, Action<WinUI.Canvas> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // Navigation
    public static NavigationViewElement Set(this NavigationViewElement el, Action<WinUI.NavigationView> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static TabViewElement Set(this TabViewElement el, Action<WinUI.TabView> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static BreadcrumbBarElement Set(this BreadcrumbBarElement el, Action<WinUI.BreadcrumbBar> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static PivotElement Set(this PivotElement el, Action<WinUI.Pivot> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // Collections
    public static ListViewElement Set(this ListViewElement el, Action<WinUI.ListView> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static GridViewElement Set(this GridViewElement el, Action<WinUI.GridView> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static TreeViewElement Set(this TreeViewElement el, Action<WinUI.TreeView> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static FlipViewElement Set(this FlipViewElement el, Action<WinUI.FlipView> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // Dialogs / Overlays
    public static ContentDialogElement Set(this ContentDialogElement el, Action<WinUI.ContentDialog> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static FlyoutElement Set(this FlyoutElement el, Action<WinUI.Flyout> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static TeachingTipElement Set(this TeachingTipElement el, Action<WinUI.TeachingTip> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static InfoBarElement Set(this InfoBarElement el, Action<WinUI.InfoBar> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static InfoBadgeElement Set(this InfoBadgeElement el, Action<WinUI.InfoBadge> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // Menus
    public static MenuBarElement Set(this MenuBarElement el, Action<WinUI.MenuBar> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static CommandBarElement Set(this CommandBarElement el, Action<WinUI.CommandBar> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static MenuFlyoutElement Set(this MenuFlyoutElement el, Action<WinUI.MenuFlyout> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // ════════════════════════════════════════════════════════════════
    //  Internal
    // ════════════════════════════════════════════════════════════════

    private static ModifiedElement Modify(Element el, ElementModifiers mods)
    {
        if (el is ModifiedElement existing)
            return existing with { Modifiers = existing.Modifiers.Merge(mods) };
        return new ModifiedElement(el, mods);
    }
}

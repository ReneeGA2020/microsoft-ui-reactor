using System;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Text;
using Windows.UI.Text;
using WinUI = Microsoft.UI.Xaml.Controls;
using WinPrim = Microsoft.UI.Xaml.Controls.Primitives;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace Duct;

/// <summary>
/// Fluent modifier extension methods for elements.
/// Modifiers are stored inline on Element.Modifiers, preserving the concrete type
/// through the entire fluent chain. This means .Set() works after any modifier:
///
///   Text("Hello")
///       .Bold()
///       .Margin(16)
///       .HAlign(HorizontalAlignment.Center)
///       .Set(tb => tb.TextWrapping = TextWrapping.Wrap)  // still TextElement!
///
/// The Set() extension gives strongly-typed native property access:
///   Button("Click", onClick)
///       .Set(b => b.FlowDirection = FlowDirection.RightToLeft)
/// </summary>
public static class ElementExtensions
{
    // ════════════════════════════════════════════════════════════════
    //  Layout modifiers (stored inline on Element.Modifiers)
    // ════════════════════════════════════════════════════════════════

    public static T Margin<T>(this T el, double uniform) where T : Element =>
        Modify(el, new ElementModifiers { Margin = new Thickness(uniform) });

    public static T Margin<T>(this T el, double horizontal, double vertical) where T : Element =>
        Modify(el, new ElementModifiers { Margin = new Thickness(horizontal, vertical, horizontal, vertical) });

    public static T Margin<T>(this T el, double left, double top, double right, double bottom) where T : Element =>
        Modify(el, new ElementModifiers { Margin = new Thickness(left, top, right, bottom) });

    public static T Padding<T>(this T el, double uniform) where T : Element =>
        Modify(el, new ElementModifiers { Padding = new Thickness(uniform) });

    public static T Padding<T>(this T el, double horizontal, double vertical) where T : Element =>
        Modify(el, new ElementModifiers { Padding = new Thickness(horizontal, vertical, horizontal, vertical) });

    public static T Padding<T>(this T el, double left, double top, double right, double bottom) where T : Element =>
        Modify(el, new ElementModifiers { Padding = new Thickness(left, top, right, bottom) });

    public static T Width<T>(this T el, double width) where T : Element =>
        Modify(el, new ElementModifiers { Width = width });

    public static T Height<T>(this T el, double height) where T : Element =>
        Modify(el, new ElementModifiers { Height = height });

    public static T Size<T>(this T el, double width, double height) where T : Element =>
        Modify(el, new ElementModifiers { Width = width, Height = height });

    public static T MinWidth<T>(this T el, double w) where T : Element =>
        Modify(el, new ElementModifiers { MinWidth = w });

    public static T MinHeight<T>(this T el, double h) where T : Element =>
        Modify(el, new ElementModifiers { MinHeight = h });

    public static T MaxWidth<T>(this T el, double w) where T : Element =>
        Modify(el, new ElementModifiers { MaxWidth = w });

    public static T MaxHeight<T>(this T el, double h) where T : Element =>
        Modify(el, new ElementModifiers { MaxHeight = h });

    // ── Alignment ───────────────────────────────────────────────────

    public static T HAlign<T>(this T el, HorizontalAlignment alignment) where T : Element =>
        Modify(el, new ElementModifiers { HorizontalAlignment = alignment });

    public static T VAlign<T>(this T el, VerticalAlignment alignment) where T : Element =>
        Modify(el, new ElementModifiers { VerticalAlignment = alignment });

    public static T Center<T>(this T el) where T : Element =>
        Modify(el, new ElementModifiers
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });

    // ── Visibility ──────────────────────────────────────────────────

    public static T Visible<T>(this T el, bool isVisible) where T : Element =>
        Modify(el, new ElementModifiers { IsVisible = isVisible });

    public static T Opacity<T>(this T el, double opacity) where T : Element =>
        Modify(el, new ElementModifiers { Opacity = opacity });

    // ── Decoration ──────────────────────────────────────────────────

    public static T ToolTip<T>(this T el, string tip) where T : Element =>
        Modify(el, new ElementModifiers { ToolTip = tip });

    // ── Flyout / Context / Rich ToolTip attachments ─────────────
    public static T WithFlyout<T>(this T el, Element flyout) where T : Element =>
        Modify(el, new ElementModifiers { AttachedFlyout = flyout });

    public static T WithContextFlyout<T>(this T el, Element contextFlyout) where T : Element =>
        Modify(el, new ElementModifiers { ContextFlyout = contextFlyout });

    public static T WithToolTip<T>(this T el, Element tooltip) where T : Element =>
        Modify(el, new ElementModifiers { RichToolTip = tooltip });

    // ── Theme / Style ───────────────────────────────────────────────

    /// <summary>
    /// Apply a named WinUI Style to the element's control at mount/update time.
    /// Style is on FrameworkElement — works on any element.
    /// Usage: Text("Hello").ApplyStyle("BodyTextBlockStyle")
    /// </summary>
    public static T ApplyStyle<T>(this T el, string styleName) where T : Element =>
        el.OnMount(fe => fe.Style = (Style)Application.Current.Resources[styleName]);

    // ════════════════════════════════════════════════════════════════
    //  Sugar extensions (typed, return concrete element type)
    // ════════════════════════════════════════════════════════════════

    // ── Text sugar ──────────────────────────────────────────────────

    public static TextElement Bold(this TextElement el) =>
        el with { Weight = Microsoft.UI.Text.FontWeights.Bold };

    public static TextElement SemiBold(this TextElement el) =>
        el with { Weight = Microsoft.UI.Text.FontWeights.SemiBold };

    public static TextElement FontSize(this TextElement el, double size) =>
        el with { FontSize = size };

    public static TextElement FontStyle(this TextElement el, Windows.UI.Text.FontStyle style) =>
        el with { FontStyle = style };

    // ── IsEnabled (on Control — works on buttons, inputs, etc.) ────

    public static T Disabled<T>(this T el, bool disabled = true) where T : Element =>
        Modify(el, new ElementModifiers { IsEnabled = !disabled });

    // ── Background (Panel, Control, Border) ────────────────────────

    public static T Background<T>(this T el, string color) where T : Element =>
        Modify(el, new ElementModifiers { Background = BrushHelper.Parse(color) });

    public static T Background<T>(this T el, Brush brush) where T : Element =>
        Modify(el, new ElementModifiers { Background = brush });

    // ── Foreground (Control, TextBlock) ──────────────────────────

    public static T Foreground<T>(this T el, string color) where T : Element =>
        Modify(el, new ElementModifiers { Foreground = BrushHelper.Parse(color) });

    public static T Foreground<T>(this T el, Brush brush) where T : Element =>
        Modify(el, new ElementModifiers { Foreground = brush });

    // ── CornerRadius (on Control and Border) ────────────────────────

    public static T CornerRadius<T>(this T el, double radius) where T : Element =>
        Modify(el, new ElementModifiers { CornerRadius = new Microsoft.UI.Xaml.CornerRadius(radius) });

    public static T CornerRadius<T>(this T el, double topLeft, double topRight, double bottomRight, double bottomLeft) where T : Element =>
        Modify(el, new ElementModifiers { CornerRadius = new Microsoft.UI.Xaml.CornerRadius(topLeft, topRight, bottomRight, bottomLeft) });

    // ── Border brush/thickness (on Control and Border) ─────────────

    public static T WithBorder<T>(this T el, string color, double thickness = 1) where T : Element =>
        Modify(el, new ElementModifiers { BorderBrush = BrushHelper.Parse(color), BorderThickness = new Thickness(thickness) });

    public static T WithBorder<T>(this T el, Brush brush, double thickness = 1) where T : Element =>
        Modify(el, new ElementModifiers { BorderBrush = brush, BorderThickness = new Thickness(thickness) });

    // ── Stack sugar ─────────────────────────────────────────────────

    public static StackElement Spacing(this StackElement el, double spacing) =>
        el with { Spacing = spacing };

    // ── TextField sugar ─────────────────────────────────────────────

    public static TextFieldElement Header(this TextFieldElement el, string header) =>
        el with { Header = header };

    // ── ComboBox sugar ──────────────────────────────────────────────

    public static ComboBoxElement Placeholder(this ComboBoxElement el, string text) =>
        el with { PlaceholderText = text };

    public static ComboBoxElement Editable(this ComboBoxElement el, bool editable = true) =>
        el with { IsEditable = editable };

    public static ComboBoxElement Header(this ComboBoxElement el, string header) =>
        el with { Header = header };

    // ── NumberBox sugar ─────────────────────────────────────────────

    public static NumberBoxElement Range(this NumberBoxElement el, double min, double max) =>
        el with { Minimum = min, Maximum = max };

    public static NumberBoxElement SpinButtons(this NumberBoxElement el, NumberBoxSpinButtonPlacementMode placement = NumberBoxSpinButtonPlacementMode.Inline) =>
        el with { SpinButtonPlacement = placement };

    // ── Slider sugar ────────────────────────────────────────────────

    public static SliderElement StepFrequency(this SliderElement el, double step) =>
        el with { StepFrequency = step };

    public static SliderElement Header(this SliderElement el, string header) =>
        el with { Header = header };

    // ── ToggleSwitch sugar ──────────────────────────────────────────

    public static ToggleSwitchElement Header(this ToggleSwitchElement el, string header) =>
        el with { Header = header };

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

    public static RichEditBoxElement Set(this RichEditBoxElement el, Action<WinUI.RichEditBox> configure) =>
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

    // Monaco Editor
    public static MonacoEditorElement Set(this MonacoEditorElement el, Action<Monaco.MonacoEditor> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static MonacoEditorElement ReadOnly(this MonacoEditorElement el) =>
        el with { IsReadOnly = true };

    public static MonacoEditorElement EditorFontSize(this MonacoEditorElement el, double size) =>
        el with { FontSize = size };

    public static MonacoEditorElement EditorWordWrap(this MonacoEditorElement el, bool wrap = true) =>
        el with { WordWrap = wrap };

    public static MonacoEditorElement Minimap(this MonacoEditorElement el, bool enabled) =>
        el with { MinimapEnabled = enabled };

    // Layout / Containers
    public static WrapGridElement Set(this WrapGridElement el, Action<WinUI.VariableSizedWrapGrid> configure) =>
        el with { Setters = [.. el.Setters, configure] };

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

    // Shapes
    public static RectangleElement Set(this RectangleElement el, Action<WinShapes.Rectangle> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static EllipseElement Set(this EllipseElement el, Action<WinShapes.Ellipse> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // Additional layout
    public static RelativePanelElement Set(this RelativePanelElement el, Action<WinUI.RelativePanel> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // Additional media
    public static MediaPlayerElementElement Set(this MediaPlayerElementElement el, Action<WinUI.MediaPlayerElement> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static AnimatedVisualPlayerElement Set(this AnimatedVisualPlayerElement el, Action<WinUI.AnimatedVisualPlayer> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // Additional collections
    public static SemanticZoomElement Set(this SemanticZoomElement el, Action<WinUI.SemanticZoom> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static ListBoxElement Set(this ListBoxElement el, Action<WinUI.ListBox> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // Additional navigation
    public static SelectorBarElement Set(this SelectorBarElement el, Action<WinUI.SelectorBar> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static PipsPagerElement Set(this PipsPagerElement el, Action<WinUI.PipsPager> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static AnnotatedScrollBarElement Set(this AnnotatedScrollBarElement el, Action<WinUI.AnnotatedScrollBar> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // Additional overlays / containers
    public static PopupElement Set(this PopupElement el, Action<WinPrim.Popup> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static RefreshContainerElement Set(this RefreshContainerElement el, Action<WinUI.RefreshContainer> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static CommandBarFlyoutElement Set(this CommandBarFlyoutElement el, Action<WinUI.CommandBarFlyout> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // Additional date/time
    public static CalendarViewElement Set(this CalendarViewElement el, Action<WinUI.CalendarView> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // SwipeControl
    public static SwipeControlElement Set(this SwipeControlElement el, Action<WinUI.SwipeControl> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // AnimatedIcon
    public static AnimatedIconElement Set(this AnimatedIconElement el, Action<WinUI.AnimatedIcon> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // ParallaxView
    public static ParallaxViewElement Set(this ParallaxViewElement el, Action<WinUI.ParallaxView> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // MapControl
    public static MapControlElement Set(this MapControlElement el, Action<WinUI.MapControl> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // Frame
    public static FrameElement Set(this FrameElement el, Action<WinUI.Frame> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // ItemsView
    public static ItemsViewElement<T> Set<T>(this ItemsViewElement<T> el, Action<WinUI.ItemsView> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // Typed templated collections
    public static TemplatedListViewElement<T> Set<T>(this TemplatedListViewElement<T> el, Action<WinUI.ListView> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static TemplatedGridViewElement<T> Set<T>(this TemplatedGridViewElement<T> el, Action<WinUI.GridView> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    public static TemplatedFlipViewElement<T> Set<T>(this TemplatedFlipViewElement<T> el, Action<WinUI.FlipView> configure) =>
        el with { Setters = [.. el.Setters, configure] };

    // ── Shape convenience modifiers ─────────────────────────────────

    public static RectangleElement Fill(this RectangleElement el, Brush brush) =>
        el with { Fill = brush };

    public static EllipseElement Fill(this EllipseElement el, Brush brush) =>
        el with { Fill = brush };

    // ── Popup convenience modifiers ─────────────────────────────────

    public static PopupElement LightDismiss(this PopupElement el, bool enabled = true) =>
        el with { IsLightDismissEnabled = enabled };

    public static PopupElement Offset(this PopupElement el, double horizontal, double vertical) =>
        el with { HorizontalOffset = horizontal, VerticalOffset = vertical };

    // Virtualized collections (LazyVStack / LazyHStack)
    // .Set() targets the outer ScrollViewer; .SetRepeater() targets the inner ItemsRepeater
    public static LazyVStackElement<T> Set<T>(this LazyVStackElement<T> el, Action<WinUI.ScrollViewer> configure) =>
        el with { ScrollViewerSetters = [.. el.ScrollViewerSetters, configure] };

    public static LazyVStackElement<T> SetRepeater<T>(this LazyVStackElement<T> el, Action<WinUI.ItemsRepeater> configure) =>
        el with { RepeaterSetters = [.. el.RepeaterSetters, configure] };

    public static LazyHStackElement<T> Set<T>(this LazyHStackElement<T> el, Action<WinUI.ScrollViewer> configure) =>
        el with { ScrollViewerSetters = [.. el.ScrollViewerSetters, configure] };

    public static LazyHStackElement<T> SetRepeater<T>(this LazyHStackElement<T> el, Action<WinUI.ItemsRepeater> configure) =>
        el with { RepeaterSetters = [.. el.RepeaterSetters, configure] };

    // ════════════════════════════════════════════════════════════════
    //  Transitions (first-class, applied by reconciler)
    // ════════════════════════════════════════════════════════════════

    // ── Theme transitions (ChildrenTransitions / ItemContainerTransitions) ──

    /// <summary>
    /// Sets theme transitions declaratively. The reconciler applies ChildrenTransitions
    /// on panels, ChildTransitions on borders, ContentTransitions on content controls.
    /// Works on any element type.
    /// </summary>
    public static T WithTransitions<T>(this T el, params Transition[] transitions) where T : Element =>
        el with { ThemeTransitions = (el.ThemeTransitions ?? new()) with { Children = transitions } };

    /// <summary>
    /// Sets ItemContainerTransitions declaratively on ListView, GridView, etc.
    /// </summary>
    public static T ItemContainerTransitions<T>(this T el, params Transition[] transitions) where T : Element =>
        el with { ThemeTransitions = (el.ThemeTransitions ?? new()) with { ItemContainer = transitions } };

    // ── Implicit transitions (Opacity, Rotation, Scale, Translation, Background) ──

    /// <summary>
    /// Adds an implicit ScalarTransition on Opacity.
    /// Applied by the reconciler after .Set() callbacks — always safe to combine.
    /// </summary>
    public static T OpacityTransition<T>(this T el, TimeSpan? duration = null) where T : Element
    {
        var t = new ScalarTransition();
        if (duration.HasValue) t.Duration = duration.Value;
        return el with { ImplicitTransitions = (el.ImplicitTransitions ?? new()) with { Opacity = t } };
    }

    /// <summary>
    /// Adds an implicit ScalarTransition on Rotation.
    /// </summary>
    public static T RotationTransition<T>(this T el, TimeSpan? duration = null) where T : Element
    {
        var t = new ScalarTransition();
        if (duration.HasValue) t.Duration = duration.Value;
        return el with { ImplicitTransitions = (el.ImplicitTransitions ?? new()) with { Rotation = t } };
    }

    /// <summary>
    /// Adds an implicit Vector3Transition on Scale.
    /// Pass a pre-configured transition to set Components for axis-specific animation.
    /// </summary>
    public static T ScaleTransition<T>(this T el, Vector3Transition? transition = null) where T : Element =>
        el with { ImplicitTransitions = (el.ImplicitTransitions ?? new()) with { Scale = transition ?? new Vector3Transition() } };

    /// <summary>
    /// Adds an implicit Vector3Transition on Translation.
    /// Pass a pre-configured transition to set Components for axis-specific animation.
    /// </summary>
    public static T TranslationTransition<T>(this T el, Vector3Transition? transition = null) where T : Element =>
        el with { ImplicitTransitions = (el.ImplicitTransitions ?? new()) with { Translation = transition ?? new Vector3Transition() } };

    /// <summary>
    /// Adds an implicit BrushTransition on Background (Grid, StackPanel, ContentPresenter).
    /// </summary>
    public static T BackgroundTransition<T>(this T el, TimeSpan? duration = null) where T : Element
    {
        var t = new BrushTransition();
        if (duration.HasValue) t.Duration = duration.Value;
        return el with { ImplicitTransitions = (el.ImplicitTransitions ?? new()) with { Background = t } };
    }

    // ════════════════════════════════════════════════════════════════
    //  ScrollView zoom/scroll modifiers
    // ════════════════════════════════════════════════════════════════

    public static ScrollViewElement ZoomMode(this ScrollViewElement el, WinUI.ZoomMode mode) =>
        el with { ZoomMode = mode };

    public static ScrollViewElement HorizontalScrollMode(this ScrollViewElement el, WinUI.ScrollMode mode) =>
        el with { HorizontalScrollMode = mode };

    public static ScrollViewElement VerticalScrollMode(this ScrollViewElement el, WinUI.ScrollMode mode) =>
        el with { VerticalScrollMode = mode };

    // ════════════════════════════════════════════════════════════════
    //  AutomationProperties / ElementSoundMode / OnMount
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sets AutomationProperties.Name on the element's control.
    /// Usage: Button("Go", onClick).AutomationName("Navigate forward")
    /// </summary>
    public static T AutomationName<T>(this T el, string name) where T : Element =>
        Modify(el, new ElementModifiers { AutomationName = name });

    /// <summary>
    /// Sets ElementSoundMode on the element's control.
    /// Usage: Button("Play", onClick).SoundMode(ElementSoundMode.Off)
    /// </summary>
    public static T SoundMode<T>(this T el, ElementSoundMode mode) where T : Element =>
        Modify(el, new ElementModifiers { ElementSoundMode = mode });

    /// <summary>
    /// Runs an action once when the element is first mounted (not on re-renders).
    /// Use this instead of .Set() when attaching event handlers to avoid accumulation.
    /// Usage: Button("Go", null).OnMount(fe => { ((Button)fe).Click += ...; })
    /// </summary>
    public static T OnMount<T>(this T el, Action<FrameworkElement> action) where T : Element =>
        Modify(el, new ElementModifiers { OnMountAction = action });

    // ════════════════════════════════════════════════════════════════
    //  ThemeShadow / Translation modifiers
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sets the Translation property (Vector3) on the element's control.
    /// Commonly used with ThemeShadow for z-depth effects.
    /// </summary>
    public static T Translation<T>(this T el, float x, float y, float z) where T : Element =>
        el.OnMount(fe => fe.Translation = new System.Numerics.Vector3(x, y, z));

    // ════════════════════════════════════════════════════════════════
    //  Internal
    // ════════════════════════════════════════════════════════════════

    private static T Modify<T>(T el, ElementModifiers mods) where T : Element =>
        el with { Modifiers = el.Modifiers is not null ? el.Modifiers.Merge(mods) : mods };
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUI = Microsoft.UI.Xaml.Controls;
using WinPrim = Microsoft.UI.Xaml.Controls.Primitives;

namespace Patch.Core;

public sealed partial class Reconciler
{
    private UIElement? Update(Element oldEl, Element newEl, UIElement control, Action requestRerender)
    {
        return (oldEl, newEl, control) switch
        {
            (ModifiedElement oldMod, ModifiedElement newMod, FrameworkElement fe)
                => UpdateModified(oldMod, newMod, fe, newEl, requestRerender),
            (TextElement, TextElement n, TextBlock tb)
                => UpdateText(n, tb),
            (RichTextBlockElement, RichTextBlockElement n, WinUI.RichTextBlock rtb)
                => UpdateRichTextBlock(n, rtb),
            (ButtonElement, ButtonElement n, WinUI.Button b)
                => UpdateButton(n, b),
            (HyperlinkButtonElement, HyperlinkButtonElement n, WinUI.HyperlinkButton hb)
                => UpdateHyperlinkButton(n, hb),
            (RepeatButtonElement, RepeatButtonElement n, WinPrim.RepeatButton rb)
                => UpdateRepeatButton(n, rb),
            (ToggleButtonElement, ToggleButtonElement n, WinPrim.ToggleButton tb)
                => UpdateToggleButton(n, tb),
            (DropDownButtonElement, DropDownButtonElement n, WinUI.DropDownButton ddb)
                => UpdateDropDownButton(n, ddb),
            (SplitButtonElement, SplitButtonElement n, WinUI.SplitButton sb)
                => UpdateSplitButton(n, sb),
            (ToggleSplitButtonElement, ToggleSplitButtonElement n, WinUI.ToggleSplitButton tsb)
                => UpdateToggleSplitButton(n, tsb),
            (TextFieldElement o, TextFieldElement n, TextBox tb)
                => UpdateTextField(o, n, tb),
            (PasswordBoxElement, PasswordBoxElement n, WinUI.PasswordBox pb)
                => UpdatePasswordBox(n, pb),
            (NumberBoxElement, NumberBoxElement n, WinUI.NumberBox nb)
                => UpdateNumberBox(n, nb),
            (AutoSuggestBoxElement, AutoSuggestBoxElement n, WinUI.AutoSuggestBox asb)
                => UpdateAutoSuggestBox(n, asb),
            (CheckBoxElement, CheckBoxElement n, WinUI.CheckBox cb)
                => UpdateCheckBox(n, cb),
            (RadioButtonElement, RadioButtonElement n, WinUI.RadioButton rb)
                => UpdateRadioButton(n, rb),
            (RadioButtonsElement, RadioButtonsElement, WinUI.RadioButtons)
                => Mount(newEl, requestRerender),
            (ComboBoxElement, ComboBoxElement, WinUI.ComboBox)
                => Mount(newEl, requestRerender),
            (SliderElement, SliderElement n, WinUI.Slider s)
                => UpdateSlider(n, s),
            (ToggleSwitchElement, ToggleSwitchElement n, WinUI.ToggleSwitch ts)
                => UpdateToggleSwitch(n, ts),
            (RatingControlElement, RatingControlElement n, WinUI.RatingControl r)
                => UpdateRatingControl(n, r),
            (ColorPickerElement, ColorPickerElement n, WinUI.ColorPicker cp)
                => UpdateColorPicker(n, cp),
            (CalendarDatePickerElement, CalendarDatePickerElement n, WinUI.CalendarDatePicker cdp)
                => UpdateCalendarDatePicker(n, cdp),
            (DatePickerElement, DatePickerElement n, WinUI.DatePicker dp)
                => UpdateDatePicker(n, dp),
            (TimePickerElement, TimePickerElement n, WinUI.TimePicker tp)
                => UpdateTimePicker(n, tp),
            (ProgressElement, ProgressElement n, WinUI.ProgressBar pb)
                => UpdateProgress(n, pb),
            (ProgressRingElement, ProgressRingElement n, WinUI.ProgressRing pr)
                => UpdateProgressRing(n, pr),
            (ImageElement o, ImageElement n, WinUI.Image img)
                => UpdateImage(o, n, img),
            (PersonPictureElement, PersonPictureElement n, WinUI.PersonPicture pp)
                => UpdatePersonPicture(n, pp),
            (WebView2Element o, WebView2Element n, WinUI.WebView2 wv)
                => UpdateWebView2(o, n, wv),
            (StackElement o, StackElement n, WinUI.StackPanel sp)
                => UpdateStack(o, n, sp, requestRerender),
            (ScrollViewElement o, ScrollViewElement n, WinUI.ScrollViewer sv)
                => UpdateScrollView(o, n, sv, newEl, requestRerender),
            (BorderElement o, BorderElement n, WinUI.Border b)
                => UpdateBorder(o, n, b, newEl, requestRerender),
            (ExpanderElement, ExpanderElement n, WinUI.Expander exp)
                => UpdateExpander(n, exp),
            (SplitViewElement, SplitViewElement, WinUI.SplitView)
                => Mount(newEl, requestRerender),
            (NavigationViewElement, NavigationViewElement n, WinUI.NavigationView nv)
                => UpdateNavigationView(n, nv, requestRerender),
            (TabViewElement, TabViewElement, WinUI.TabView)
                => Mount(newEl, requestRerender),
            (BreadcrumbBarElement, BreadcrumbBarElement n, WinUI.BreadcrumbBar bcb)
                => UpdateBreadcrumbBar(n, bcb),
            (PivotElement, PivotElement, WinUI.Pivot)
                => Mount(newEl, requestRerender),
            (ListViewElement, ListViewElement, WinUI.ListView)
                => Mount(newEl, requestRerender),
            (GridViewElement, GridViewElement, WinUI.GridView)
                => Mount(newEl, requestRerender),
            (TreeViewElement, TreeViewElement, WinUI.TreeView)
                => Mount(newEl, requestRerender),
            (FlipViewElement, FlipViewElement, WinUI.FlipView)
                => Mount(newEl, requestRerender),
            (InfoBarElement, InfoBarElement n, WinUI.InfoBar ib)
                => UpdateInfoBar(n, ib),
            (InfoBadgeElement, InfoBadgeElement n, WinUI.InfoBadge badge)
                => UpdateInfoBadge(n, badge),
            (ContentDialogElement o, ContentDialogElement n, FrameworkElement fe)
                => UpdateContentDialog(o, n, fe, requestRerender),
            (TeachingTipElement, TeachingTipElement n, WinUI.TeachingTip tip)
                => UpdateTeachingTip(n, tip),
            (MenuBarElement, MenuBarElement, WinUI.MenuBar)
                => Mount(newEl, requestRerender),
            (CommandBarElement, CommandBarElement, WinUI.CommandBar)
                => Mount(newEl, requestRerender),
            (ComponentElement, ComponentElement, _)
                => UpdateComponent(oldEl, newEl, control, requestRerender),
            (FuncElement, FuncElement, _)
                => UpdateComponent(oldEl, newEl, control, requestRerender),
            _ => Mount(newEl, requestRerender),
        };
    }

    // ── Per-control Update methods ──────────────────────────────────

    private UIElement? UpdateModified(ModifiedElement oldMod, ModifiedElement newMod, FrameworkElement fe, Element newEl, Action requestRerender)
    {
        var innerReplacement = Update(oldMod.Inner, newMod.Inner, fe, requestRerender);
        if (innerReplacement is not null) return Mount(newEl, requestRerender);
        ApplyModifiers(fe, newMod.Modifiers);
        return null;
    }

    private static UIElement? UpdateText(TextElement n, TextBlock tb)
    {
        tb.Text = n.Content;
        if (n.FontSize.HasValue) tb.FontSize = n.FontSize.Value;
        if (n.Weight.HasValue) tb.FontWeight = n.Weight.Value;
        if (n.HorizontalAlignment.HasValue) tb.HorizontalAlignment = n.HorizontalAlignment.Value;
        ApplySetters(n.Setters, tb);
        return null;
    }

    private static UIElement? UpdateRichTextBlock(RichTextBlockElement n, WinUI.RichTextBlock rtb)
    {
        rtb.Blocks.Clear();
        var p = new Microsoft.UI.Xaml.Documents.Paragraph();
        p.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = n.Text });
        rtb.Blocks.Add(p);
        if (n.FontSize.HasValue) rtb.FontSize = n.FontSize.Value;
        ApplySetters(n.Setters, rtb);
        return null;
    }

    private static UIElement? UpdateButton(ButtonElement n, WinUI.Button b)
    {
        b.Content = n.Label; b.IsEnabled = n.IsEnabled; b.Tag = n;
        ApplySetters(n.Setters, b);
        return null;
    }

    private static UIElement? UpdateHyperlinkButton(HyperlinkButtonElement n, WinUI.HyperlinkButton hb)
    {
        hb.Content = n.Content;
        if (n.NavigateUri is not null) hb.NavigateUri = n.NavigateUri;
        hb.Tag = n;
        ApplySetters(n.Setters, hb);
        return null;
    }

    private static UIElement? UpdateRepeatButton(RepeatButtonElement n, WinPrim.RepeatButton rb)
    {
        rb.Content = n.Label; rb.Delay = n.Delay; rb.Interval = n.Interval; rb.Tag = n;
        ApplySetters(n.Setters, rb);
        return null;
    }

    private static UIElement? UpdateToggleButton(ToggleButtonElement n, WinPrim.ToggleButton tb)
    {
        tb.Content = n.Label; tb.IsChecked = n.IsChecked; tb.Tag = n;
        ApplySetters(n.Setters, tb);
        return null;
    }

    private static UIElement? UpdateDropDownButton(DropDownButtonElement n, WinUI.DropDownButton ddb)
    {
        ddb.Content = n.Label; ddb.Tag = n;
        ApplySetters(n.Setters, ddb);
        return null;
    }

    private static UIElement? UpdateSplitButton(SplitButtonElement n, WinUI.SplitButton sb)
    {
        sb.Content = n.Label; sb.Tag = n;
        ApplySetters(n.Setters, sb);
        return null;
    }

    private static UIElement? UpdateToggleSplitButton(ToggleSplitButtonElement n, WinUI.ToggleSplitButton tsb)
    {
        tsb.Content = n.Label; tsb.IsChecked = n.IsChecked; tsb.Tag = n;
        ApplySetters(n.Setters, tsb);
        return null;
    }

    private static UIElement? UpdateTextField(TextFieldElement o, TextFieldElement n, TextBox tb)
    {
        if (o.Value != n.Value) tb.Text = n.Value;
        tb.PlaceholderText = n.Placeholder ?? ""; tb.Tag = n;
        ApplySetters(n.Setters, tb);
        return null;
    }

    private static UIElement? UpdatePasswordBox(PasswordBoxElement n, WinUI.PasswordBox pb)
    {
        pb.Password = n.Password; pb.PlaceholderText = n.PlaceholderText ?? ""; pb.Tag = n;
        ApplySetters(n.Setters, pb);
        return null;
    }

    private static UIElement? UpdateNumberBox(NumberBoxElement n, WinUI.NumberBox nb)
    {
        nb.Value = n.Value; nb.Minimum = n.Minimum; nb.Maximum = n.Maximum;
        nb.SmallChange = n.SmallChange; nb.LargeChange = n.LargeChange;
        nb.SpinButtonPlacementMode = n.SpinButtonPlacement;
        if (n.Header is not null) nb.Header = n.Header;
        nb.Tag = n;
        ApplySetters(n.Setters, nb);
        return null;
    }

    private static UIElement? UpdateAutoSuggestBox(AutoSuggestBoxElement n, WinUI.AutoSuggestBox asb)
    {
        asb.Text = n.Text; asb.PlaceholderText = n.PlaceholderText ?? "";
        asb.ItemsSource = n.Suggestions; asb.Tag = n;
        ApplySetters(n.Setters, asb);
        return null;
    }

    private static UIElement? UpdateCheckBox(CheckBoxElement n, WinUI.CheckBox cb)
    {
        cb.IsChecked = n.IsChecked; cb.Content = n.Label; cb.Tag = n;
        ApplySetters(n.Setters, cb);
        return null;
    }

    private static UIElement? UpdateRadioButton(RadioButtonElement n, WinUI.RadioButton rb)
    {
        rb.Content = n.Label; rb.IsChecked = n.IsChecked;
        if (n.GroupName is not null) rb.GroupName = n.GroupName;
        rb.Tag = n;
        ApplySetters(n.Setters, rb);
        return null;
    }

    private static UIElement? UpdateSlider(SliderElement n, WinUI.Slider s)
    {
        s.Minimum = n.Min; s.Maximum = n.Max; s.Value = n.Value;
        s.StepFrequency = n.StepFrequency;
        if (n.Header is not null) s.Header = n.Header;
        s.Tag = n;
        ApplySetters(n.Setters, s);
        return null;
    }

    private static UIElement? UpdateToggleSwitch(ToggleSwitchElement n, WinUI.ToggleSwitch ts)
    {
        ts.IsOn = n.IsOn; ts.OnContent = n.OnContent; ts.OffContent = n.OffContent;
        if (n.Header is not null) ts.Header = n.Header;
        ts.Tag = n;
        ApplySetters(n.Setters, ts);
        return null;
    }

    private static UIElement? UpdateRatingControl(RatingControlElement n, WinUI.RatingControl r)
    {
        r.Value = n.Value; r.MaxRating = n.MaxRating; r.IsReadOnly = n.IsReadOnly;
        r.Caption = n.Caption ?? ""; r.Tag = n;
        ApplySetters(n.Setters, r);
        return null;
    }

    private static UIElement? UpdateColorPicker(ColorPickerElement n, WinUI.ColorPicker cp)
    {
        cp.Color = n.Color; cp.IsAlphaEnabled = n.IsAlphaEnabled; cp.Tag = n;
        ApplySetters(n.Setters, cp);
        return null;
    }

    private static UIElement? UpdateCalendarDatePicker(CalendarDatePickerElement n, WinUI.CalendarDatePicker cdp)
    {
        cdp.Date = n.Date; cdp.Tag = n;
        ApplySetters(n.Setters, cdp);
        return null;
    }

    private static UIElement? UpdateDatePicker(DatePickerElement n, WinUI.DatePicker dp)
    {
        dp.Date = n.Date; dp.Tag = n;
        ApplySetters(n.Setters, dp);
        return null;
    }

    private static UIElement? UpdateTimePicker(TimePickerElement n, WinUI.TimePicker tp)
    {
        tp.Time = n.Time; tp.Tag = n;
        ApplySetters(n.Setters, tp);
        return null;
    }

    private static UIElement? UpdateProgress(ProgressElement n, WinUI.ProgressBar pb)
    {
        pb.IsIndeterminate = n.IsIndeterminate; pb.Minimum = n.Minimum; pb.Maximum = n.Maximum;
        pb.ShowError = n.ShowError; pb.ShowPaused = n.ShowPaused;
        if (n.Value.HasValue) pb.Value = n.Value.Value;
        ApplySetters(n.Setters, pb);
        return null;
    }

    private static UIElement? UpdateProgressRing(ProgressRingElement n, WinUI.ProgressRing pr)
    {
        pr.IsIndeterminate = n.IsIndeterminate; pr.IsActive = n.IsActive;
        if (n.Value.HasValue) pr.Value = n.Value.Value;
        ApplySetters(n.Setters, pr);
        return null;
    }

    private static UIElement? UpdateImage(ImageElement o, ImageElement n, WinUI.Image img)
    {
        if (o.Source != n.Source) img.Source = new BitmapImage(new Uri(n.Source, UriKind.RelativeOrAbsolute));
        if (n.Width.HasValue) img.Width = n.Width.Value;
        if (n.Height.HasValue) img.Height = n.Height.Value;
        ApplySetters(n.Setters, img);
        return null;
    }

    private static UIElement? UpdatePersonPicture(PersonPictureElement n, WinUI.PersonPicture pp)
    {
        if (n.DisplayName is not null) pp.DisplayName = n.DisplayName;
        if (n.Initials is not null) pp.Initials = n.Initials;
        pp.IsGroup = n.IsGroup; pp.BadgeNumber = n.BadgeNumber;
        ApplySetters(n.Setters, pp);
        return null;
    }

    private static UIElement? UpdateWebView2(WebView2Element o, WebView2Element n, WinUI.WebView2 wv)
    {
        if (n.Source is not null && n.Source != o.Source) wv.Source = n.Source;
        wv.Tag = n;
        ApplySetters(n.Setters, wv);
        return null;
    }

    private UIElement? UpdateStack(StackElement o, StackElement n, WinUI.StackPanel sp, Action requestRerender)
    {
        sp.Spacing = n.Spacing;
        var panelReplacement = ReconcileChildren(o.Children, n.Children, sp, requestRerender);
        if (panelReplacement is not null) return panelReplacement;
        sp.Tag = n;
        ApplySetters(n.Setters, sp);
        return null;
    }

    private UIElement? UpdateScrollView(ScrollViewElement o, ScrollViewElement n, WinUI.ScrollViewer sv, Element newEl, Action requestRerender)
    {
        if (CanUpdate(o.Child, n.Child))
        {
            var childRepl = Update(o.Child, n.Child, sv.Content as UIElement ?? new WinUI.Grid(), requestRerender);
            if (childRepl is not null) return Mount(newEl, requestRerender);
        }
        else return Mount(newEl, requestRerender);
        sv.Tag = n;
        ApplySetters(n.Setters, sv);
        return null;
    }

    private UIElement? UpdateBorder(BorderElement o, BorderElement n, WinUI.Border b, Element newEl, Action requestRerender)
    {
        if (CanUpdate(o.Child, n.Child))
        {
            if (b.Child is not null)
            {
                var childRepl = Update(o.Child, n.Child, b.Child, requestRerender);
                if (childRepl is not null) return Mount(newEl, requestRerender);
            }
        }
        else return Mount(newEl, requestRerender);

        if (n.CornerRadius.HasValue) b.CornerRadius = new Microsoft.UI.Xaml.CornerRadius(n.CornerRadius.Value);
        if (n.Padding.HasValue) b.Padding = n.Padding.Value;
        if (n.Background is not null) b.Background = n.Background;
        if (n.BorderBrush is not null) b.BorderBrush = n.BorderBrush;
        if (n.BorderThickness.HasValue) b.BorderThickness = new Microsoft.UI.Xaml.Thickness(n.BorderThickness.Value);
        b.Tag = n;
        ApplySetters(n.Setters, b);
        return null;
    }

    private static UIElement? UpdateExpander(ExpanderElement n, WinUI.Expander exp)
    {
        exp.Header = n.Header; exp.IsExpanded = n.IsExpanded;
        exp.ExpandDirection = n.ExpandDirection; exp.Tag = n;
        ApplySetters(n.Setters, exp);
        return null;
    }

    private UIElement? UpdateNavigationView(NavigationViewElement n, WinUI.NavigationView nv, Action requestRerender)
    {
        nv.IsPaneOpen = n.IsPaneOpen; nv.IsBackEnabled = n.IsBackEnabled;
        if (n.Content is not null) nv.Content = Mount(n.Content, requestRerender);
        nv.Tag = n;
        ApplySetters(n.Setters, nv);
        return null;
    }

    private static UIElement? UpdateBreadcrumbBar(BreadcrumbBarElement n, WinUI.BreadcrumbBar bcb)
    {
        bcb.ItemsSource = n.Items.Select(i => i.Label).ToList();
        bcb.Tag = n;
        ApplySetters(n.Setters, bcb);
        return null;
    }

    private static UIElement? UpdateInfoBar(InfoBarElement n, WinUI.InfoBar ib)
    {
        ib.Title = n.Title ?? ""; ib.Message = n.Message ?? "";
        ib.Severity = n.Severity; ib.IsOpen = n.IsOpen; ib.IsClosable = n.IsClosable;
        ib.Tag = n;
        ApplySetters(n.Setters, ib);
        return null;
    }

    private static UIElement? UpdateInfoBadge(InfoBadgeElement n, WinUI.InfoBadge badge)
    {
        if (n.Value.HasValue) badge.Value = n.Value.Value;
        ApplySetters(n.Setters, badge);
        return null;
    }

    private UIElement? UpdateContentDialog(ContentDialogElement o, ContentDialogElement n, FrameworkElement fe, Action requestRerender)
    {
        if (n.IsOpen && !o.IsOpen) ShowContentDialog(n, requestRerender);
        fe.Tag = n;
        return null;
    }

    private static UIElement? UpdateTeachingTip(TeachingTipElement n, WinUI.TeachingTip tip)
    {
        tip.Title = n.Title; tip.Subtitle = n.Subtitle ?? ""; tip.IsOpen = n.IsOpen;
        tip.Tag = n;
        ApplySetters(n.Setters, tip);
        return null;
    }

    private UIElement? UpdateComponent(Element oldEl, Element newEl, UIElement control, Action requestRerender)
    {
        ReconcileComponent(oldEl, newEl, control, requestRerender);
        return null;
    }
}

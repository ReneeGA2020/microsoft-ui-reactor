using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUI = Microsoft.UI.Xaml.Controls;
using WinPrim = Microsoft.UI.Xaml.Controls.Primitives;

namespace Duct.Core;

public sealed partial class Reconciler
{
    private UIElement? Update(Element oldEl, Element newEl, UIElement control, Action requestRerender)
    {
        // Unwrap legacy ModifiedElement (backward compat)
        ElementModifiers? oldModifiers = oldEl.Modifiers;
        ElementModifiers? modifiers = newEl.Modifiers;
        if (oldEl is ModifiedElement oldMod && newEl is ModifiedElement newMod)
        {
            oldModifiers = oldMod.WrappedModifiers;
            if (oldMod.Inner.Modifiers is not null)
                oldModifiers = oldModifiers.Merge(oldMod.Inner.Modifiers);
            modifiers = newMod.WrappedModifiers;
            if (newMod.Inner.Modifiers is not null)
                modifiers = modifiers.Merge(newMod.Inner.Modifiers);
            oldEl = oldMod.Inner;
            newEl = newMod.Inner;
        }

        UIElement? result;

        // Registered types checked first
        if (_typeRegistry.TryGetValue(newEl.GetType(), out var reg))
        {
            result = reg.Update(oldEl, newEl, control, requestRerender, this);
        }
        else
        {
        result = (oldEl, newEl, control) switch
        {
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
            (RichEditBoxElement, RichEditBoxElement n, WinUI.RichEditBox reb)
                => UpdateRichEditBox(n, reb),
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
            (WrapGridElement o, WrapGridElement n, WinUI.VariableSizedWrapGrid wg)
                => UpdateWrapGrid(o, n, wg, requestRerender),
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
            (ListViewElement o, ListViewElement n, WinUI.ListView lv)
                => UpdateListView(o, n, lv, requestRerender),
            (GridViewElement o, GridViewElement n, WinUI.GridView gv)
                => UpdateGridView(o, n, gv, requestRerender),
            (TreeViewElement o, TreeViewElement n, WinUI.TreeView tv)
                => UpdateTreeView(o, n, tv, requestRerender),
            (FlipViewElement o, FlipViewElement n, WinUI.FlipView fv)
                => UpdateFlipView(o, n, fv, requestRerender),
            (InfoBarElement, InfoBarElement n, WinUI.InfoBar ib)
                => UpdateInfoBar(n, ib),
            (InfoBadgeElement, InfoBadgeElement n, WinUI.InfoBadge badge)
                => UpdateInfoBadge(n, badge),
            (ContentDialogElement o, ContentDialogElement n, FrameworkElement cdFe)
                => UpdateContentDialog(o, n, cdFe, requestRerender),
            (TeachingTipElement, TeachingTipElement n, WinUI.TeachingTip tip)
                => UpdateTeachingTip(n, tip),
            (MenuBarElement, MenuBarElement, WinUI.MenuBar)
                => Mount(newEl, requestRerender),
            (CommandBarElement o, CommandBarElement n, WinUI.CommandBar cb)
                => UpdateCommandBar(o, n, cb, requestRerender),
            (Core.GridElement o, Core.GridElement n, WinUI.Grid g)
                => UpdateGrid(o, n, g, requestRerender),
            (LazyStackElementBase, LazyStackElementBase n, WinUI.ScrollViewer sv)
                => UpdateLazyStack(n, sv, requestRerender),
            (ComponentElement, ComponentElement, _)
                => UpdateComponent(oldEl, newEl, control, requestRerender),
            (FuncElement, FuncElement, _)
                => UpdateComponent(oldEl, newEl, control, requestRerender),
            _ => Mount(newEl, requestRerender),
        };
        }

        // Apply inline modifiers after update
        var target = result ?? control;
        if (modifiers is not null && target is FrameworkElement fe)
            ApplyModifiers(fe, oldModifiers, modifiers, requestRerender);

        return result;
    }

    private UIElement? UpdateText(TextElement n, TextBlock tb)
    {
        if (tb.Text != n.Content) tb.Text = n.Content;
        if (n.FontSize.HasValue && tb.FontSize != n.FontSize.Value) tb.FontSize = n.FontSize.Value;
        if (n.Weight.HasValue) tb.FontWeight = n.Weight.Value;
        if (n.HorizontalAlignment.HasValue) tb.HorizontalAlignment = n.HorizontalAlignment.Value;
        ApplySetters(n.Setters, tb);
        return null;
    }

    private UIElement? UpdateRichTextBlock(RichTextBlockElement n, WinUI.RichTextBlock rtb)
    {
        rtb.Blocks.Clear();
        var p = new Microsoft.UI.Xaml.Documents.Paragraph();
        p.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = n.Text });
        rtb.Blocks.Add(p);
        if (n.FontSize.HasValue) rtb.FontSize = n.FontSize.Value;
        ApplySetters(n.Setters, rtb);
        return null;
    }

    private UIElement? UpdateButton(ButtonElement n, WinUI.Button b)
    {
        b.Content = n.Label; b.IsEnabled = n.IsEnabled; SetElementTag(b, n);
        ApplySetters(n.Setters, b);
        return null;
    }

    private UIElement? UpdateHyperlinkButton(HyperlinkButtonElement n, WinUI.HyperlinkButton hb)
    {
        hb.Content = n.Content;
        if (n.NavigateUri is not null) hb.NavigateUri = n.NavigateUri;
        SetElementTag(hb, n);
        ApplySetters(n.Setters, hb);
        return null;
    }

    private UIElement? UpdateRepeatButton(RepeatButtonElement n, WinPrim.RepeatButton rb)
    {
        rb.Content = n.Label; rb.Delay = n.Delay; rb.Interval = n.Interval; SetElementTag(rb, n);
        ApplySetters(n.Setters, rb);
        return null;
    }

    private UIElement? UpdateToggleButton(ToggleButtonElement n, WinPrim.ToggleButton tb)
    {
        tb.Content = n.Label; tb.IsChecked = n.IsChecked; SetElementTag(tb, n);
        ApplySetters(n.Setters, tb);
        return null;
    }

    private UIElement? UpdateDropDownButton(DropDownButtonElement n, WinUI.DropDownButton ddb)
    {
        if (ddb.Content as string != n.Label) ddb.Content = n.Label;
        SetElementTag(ddb, n);
        ApplySetters(n.Setters, ddb);
        return null;
    }

    private UIElement? UpdateSplitButton(SplitButtonElement n, WinUI.SplitButton sb)
    {
        sb.Content = n.Label; SetElementTag(sb, n);
        ApplySetters(n.Setters, sb);
        return null;
    }

    private UIElement? UpdateToggleSplitButton(ToggleSplitButtonElement n, WinUI.ToggleSplitButton tsb)
    {
        tsb.Content = n.Label; tsb.IsChecked = n.IsChecked; SetElementTag(tsb, n);
        ApplySetters(n.Setters, tsb);
        return null;
    }

    private UIElement? UpdateTextField(TextFieldElement o, TextFieldElement n, TextBox tb)
    {
        if (o.Value != n.Value) tb.Text = n.Value;
        tb.PlaceholderText = n.Placeholder ?? ""; SetElementTag(tb, n);
        ApplySetters(n.Setters, tb);
        return null;
    }

    private UIElement? UpdatePasswordBox(PasswordBoxElement n, WinUI.PasswordBox pb)
    {
        pb.Password = n.Password; pb.PlaceholderText = n.PlaceholderText ?? ""; SetElementTag(pb, n);
        ApplySetters(n.Setters, pb);
        return null;
    }

    private UIElement? UpdateNumberBox(NumberBoxElement n, WinUI.NumberBox nb)
    {
        nb.Value = n.Value; nb.Minimum = n.Minimum; nb.Maximum = n.Maximum;
        nb.SmallChange = n.SmallChange; nb.LargeChange = n.LargeChange;
        nb.SpinButtonPlacementMode = n.SpinButtonPlacement;
        if (n.Header is not null) nb.Header = n.Header;
        SetElementTag(nb, n);
        ApplySetters(n.Setters, nb);
        return null;
    }

    private UIElement? UpdateAutoSuggestBox(AutoSuggestBoxElement n, WinUI.AutoSuggestBox asb)
    {
        asb.Text = n.Text; asb.PlaceholderText = n.PlaceholderText ?? "";
        asb.ItemsSource = n.Suggestions; SetElementTag(asb, n);
        ApplySetters(n.Setters, asb);
        return null;
    }

    private UIElement? UpdateCheckBox(CheckBoxElement n, WinUI.CheckBox cb)
    {
        cb.Content = n.Label;
        cb.IsThreeState = n.IsThreeState;
        cb.IsChecked = n.IsThreeState ? n.CheckedState : n.IsChecked;
        SetElementTag(cb, n);
        ApplySetters(n.Setters, cb);
        return null;
    }

    private UIElement? UpdateRadioButton(RadioButtonElement n, WinUI.RadioButton rb)
    {
        rb.Content = n.Label; rb.IsChecked = n.IsChecked;
        if (n.GroupName is not null) rb.GroupName = n.GroupName;
        SetElementTag(rb, n);
        ApplySetters(n.Setters, rb);
        return null;
    }

    private UIElement? UpdateSlider(SliderElement n, WinUI.Slider s)
    {
        s.Minimum = n.Min; s.Maximum = n.Max; s.Value = n.Value;
        s.StepFrequency = n.StepFrequency;
        if (n.Header is not null) s.Header = n.Header;
        SetElementTag(s, n);
        ApplySetters(n.Setters, s);
        return null;
    }

    private UIElement? UpdateToggleSwitch(ToggleSwitchElement n, WinUI.ToggleSwitch ts)
    {
        ts.IsOn = n.IsOn; ts.OnContent = n.OnContent; ts.OffContent = n.OffContent;
        if (n.Header is not null) ts.Header = n.Header;
        SetElementTag(ts, n);
        ApplySetters(n.Setters, ts);
        return null;
    }

    private UIElement? UpdateRatingControl(RatingControlElement n, WinUI.RatingControl r)
    {
        r.Value = n.Value; r.MaxRating = n.MaxRating; r.IsReadOnly = n.IsReadOnly;
        r.Caption = n.Caption ?? ""; SetElementTag(r, n);
        ApplySetters(n.Setters, r);
        return null;
    }

    private UIElement? UpdateColorPicker(ColorPickerElement n, WinUI.ColorPicker cp)
    {
        cp.Color = n.Color; cp.IsAlphaEnabled = n.IsAlphaEnabled; SetElementTag(cp, n);
        ApplySetters(n.Setters, cp);
        return null;
    }

    private UIElement? UpdateCalendarDatePicker(CalendarDatePickerElement n, WinUI.CalendarDatePicker cdp)
    {
        cdp.Date = n.Date; SetElementTag(cdp, n);
        ApplySetters(n.Setters, cdp);
        return null;
    }

    private UIElement? UpdateDatePicker(DatePickerElement n, WinUI.DatePicker dp)
    {
        dp.Date = n.Date; SetElementTag(dp, n);
        ApplySetters(n.Setters, dp);
        return null;
    }

    private UIElement? UpdateTimePicker(TimePickerElement n, WinUI.TimePicker tp)
    {
        tp.Time = n.Time; SetElementTag(tp, n);
        ApplySetters(n.Setters, tp);
        return null;
    }

    private UIElement? UpdateProgress(ProgressElement n, WinUI.ProgressBar pb)
    {
        pb.IsIndeterminate = n.IsIndeterminate; pb.Minimum = n.Minimum; pb.Maximum = n.Maximum;
        pb.ShowError = n.ShowError; pb.ShowPaused = n.ShowPaused;
        if (n.Value.HasValue) pb.Value = n.Value.Value;
        ApplySetters(n.Setters, pb);
        return null;
    }

    private UIElement? UpdateProgressRing(ProgressRingElement n, WinUI.ProgressRing pr)
    {
        pr.IsIndeterminate = n.IsIndeterminate; pr.IsActive = n.IsActive;
        if (n.Value.HasValue) pr.Value = n.Value.Value;
        ApplySetters(n.Setters, pr);
        return null;
    }

    private UIElement? UpdateImage(ImageElement o, ImageElement n, WinUI.Image img)
    {
        if (o.Source != n.Source) img.Source = new BitmapImage(new Uri(n.Source, UriKind.RelativeOrAbsolute));
        if (n.Width.HasValue) img.Width = n.Width.Value;
        if (n.Height.HasValue) img.Height = n.Height.Value;
        ApplySetters(n.Setters, img);
        return null;
    }

    private UIElement? UpdatePersonPicture(PersonPictureElement n, WinUI.PersonPicture pp)
    {
        if (n.DisplayName is not null) pp.DisplayName = n.DisplayName;
        if (n.Initials is not null) pp.Initials = n.Initials;
        pp.IsGroup = n.IsGroup; pp.BadgeNumber = n.BadgeNumber;
        ApplySetters(n.Setters, pp);
        return null;
    }

    private UIElement? UpdateWebView2(WebView2Element o, WebView2Element n, WinUI.WebView2 wv)
    {
        if (n.Source is not null && n.Source != o.Source) wv.Source = n.Source;
        SetElementTag(wv, n);
        ApplySetters(n.Setters, wv);
        return null;
    }

    private UIElement? UpdateRichEditBox(RichEditBoxElement n, WinUI.RichEditBox reb)
    {
        reb.IsReadOnly = n.IsReadOnly;
        if (n.Header is not null) reb.Header = n.Header;
        if (n.PlaceholderText is not null) reb.PlaceholderText = n.PlaceholderText;
        SetElementTag(reb, n);
        ApplySetters(n.Setters, reb);
        return null;
    }

    private UIElement? UpdateWrapGrid(WrapGridElement o, WrapGridElement n, WinUI.VariableSizedWrapGrid wg, Action requestRerender)
    {
        wg.Orientation = n.Orientation;
        if (n.MaximumRowsOrColumns >= 0) wg.MaximumRowsOrColumns = n.MaximumRowsOrColumns;
        if (!double.IsNaN(n.ItemWidth)) wg.ItemWidth = n.ItemWidth;
        if (!double.IsNaN(n.ItemHeight)) wg.ItemHeight = n.ItemHeight;
        ReconcileChildren(o.Children, n.Children, wg, requestRerender);
        SetElementTag(wg, n);
        ApplySetters(n.Setters, wg);
        return null;
    }

    private UIElement? UpdateStack(StackElement o, StackElement n, WinUI.StackPanel sp, Action requestRerender)
    {
        sp.Spacing = n.Spacing;
        ReconcileChildren(o.Children, n.Children, sp, requestRerender);
        // No Tag set — StackPanel has no event handlers. Avoids expensive COM call.
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
        // No Tag set — ScrollViewer has no event handlers.
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
        // No Tag set — Border has no event handlers.
        ApplySetters(n.Setters, b);
        return null;
    }

    private UIElement? UpdateExpander(ExpanderElement n, WinUI.Expander exp)
    {
        exp.Header = n.Header; exp.IsExpanded = n.IsExpanded;
        exp.ExpandDirection = n.ExpandDirection; SetElementTag(exp, n);
        ApplySetters(n.Setters, exp);
        return null;
    }

    private UIElement? UpdateNavigationView(NavigationViewElement n, WinUI.NavigationView nv, Action requestRerender)
    {
        nv.IsPaneOpen = n.IsPaneOpen; nv.IsBackEnabled = n.IsBackEnabled;
        if (n.Content is not null) nv.Content = Mount(n.Content, requestRerender);
        SetElementTag(nv, n);
        ApplySetters(n.Setters, nv);
        return null;
    }

    private UIElement? UpdateBreadcrumbBar(BreadcrumbBarElement n, WinUI.BreadcrumbBar bcb)
    {
        bcb.ItemsSource = n.Items.Select(i => i.Label).ToList();
        SetElementTag(bcb, n);
        ApplySetters(n.Setters, bcb);
        return null;
    }

    private UIElement? UpdateInfoBar(InfoBarElement n, WinUI.InfoBar ib)
    {
        ib.Title = n.Title ?? ""; ib.Message = n.Message ?? "";
        ib.Severity = n.Severity; ib.IsOpen = n.IsOpen; ib.IsClosable = n.IsClosable;
        SetElementTag(ib, n);
        ApplySetters(n.Setters, ib);
        return null;
    }

    private UIElement? UpdateInfoBadge(InfoBadgeElement n, WinUI.InfoBadge badge)
    {
        if (n.Value.HasValue) badge.Value = n.Value.Value;
        ApplySetters(n.Setters, badge);
        return null;
    }

    private UIElement? UpdateContentDialog(ContentDialogElement o, ContentDialogElement n, FrameworkElement fe, Action requestRerender)
    {
        if (n.IsOpen && !o.IsOpen) ShowContentDialog(n, requestRerender);
        SetElementTag(fe, n);
        return null;
    }

    private UIElement? UpdateTeachingTip(TeachingTipElement n, WinUI.TeachingTip tip)
    {
        tip.Title = n.Title; tip.Subtitle = n.Subtitle ?? ""; tip.IsOpen = n.IsOpen;
        SetElementTag(tip, n);
        ApplySetters(n.Setters, tip);
        return null;
    }

    private UIElement? UpdateListView(ListViewElement o, ListViewElement n, WinUI.ListView lv, Action requestRerender)
    {
        lv.SelectionMode = n.SelectionMode;
        lv.IsItemClickEnabled = n.OnItemClick is not null;
        if (n.Header is not null) lv.Header = n.Header;

        // Update ItemsSource — ContainerContentChanging re-mounts visible items via Tag
        if (o.Items.Length != n.Items.Length)
            lv.ItemsSource = Enumerable.Range(0, n.Items.Length).ToList();

        SetElementTag(lv, n);

        if (n.SelectedIndex >= 0) lv.SelectedIndex = n.SelectedIndex;
        ApplySetters(n.Setters, lv);
        return null;
    }

    private UIElement? UpdateGridView(GridViewElement o, GridViewElement n, WinUI.GridView gv, Action requestRerender)
    {
        gv.SelectionMode = n.SelectionMode;
        gv.IsItemClickEnabled = n.OnItemClick is not null;
        if (n.Header is not null) gv.Header = n.Header;

        if (o.Items.Length != n.Items.Length)
            gv.ItemsSource = Enumerable.Range(0, n.Items.Length).ToList();

        SetElementTag(gv, n);

        if (n.SelectedIndex >= 0) gv.SelectedIndex = n.SelectedIndex;
        ApplySetters(n.Setters, gv);
        return null;
    }

    private UIElement? UpdateFlipView(FlipViewElement o, FlipViewElement n, WinUI.FlipView fv, Action requestRerender)
    {
        ReconcileItemsChildren(o.Items, n.Items, fv, requestRerender);
        fv.SelectedIndex = n.SelectedIndex;
        SetElementTag(fv, n);
        ApplySetters(n.Setters, fv);
        return null;
    }

    private UIElement? UpdateLazyStack(LazyStackElementBase n, WinUI.ScrollViewer sv, Action requestRerender)
    {
        if (sv.Content is WinUI.ItemsRepeater repeater)
        {
            repeater.ItemsSource = n.GetItemsSource();
            repeater.ItemTemplate = n.CreateFactory(this, requestRerender, _pool);
            if (repeater.Layout is WinUI.StackLayout layout)
                layout.Spacing = n.Spacing;
            SetElementTag(repeater, n);
            ApplySetters(n.RepeaterSetters, repeater);
        }
        SetElementTag(sv, n);
        ApplySetters(n.ScrollViewerSetters, sv);
        return null;
    }

    private UIElement? UpdateCommandBar(CommandBarElement o, CommandBarElement n, WinUI.CommandBar cb, Action requestRerender)
    {
        cb.DefaultLabelPosition = n.DefaultLabelPosition;
        cb.IsOpen = n.IsOpen;

        // Update primary commands in-place
        UpdateAppBarItems(cb.PrimaryCommands, n.PrimaryCommands);
        UpdateAppBarItems(cb.SecondaryCommands, n.SecondaryCommands);

        SetElementTag(cb, n);
        ApplySetters(n.Setters, cb);
        return null;
    }

    private static void UpdateAppBarItems(
        System.Collections.Generic.IList<WinUI.ICommandBarElement> target,
        AppBarItemBase[]? source)
    {
        int newCount = source?.Length ?? 0;
        int oldCount = target.Count;

        // Update shared range (only update if types match, otherwise replace)
        int shared = Math.Min(oldCount, newCount);
        for (int i = 0; i < shared; i++)
        {
            if (source is null) continue;
            switch (source[i])
            {
                case AppBarButtonData cmd when target[i] is WinUI.AppBarButton abb:
                    abb.Label = cmd.Label;
                    if (cmd.Icon is not null) abb.Icon = new WinUI.SymbolIcon(ParseSymbol(cmd.Icon));
                    abb.Tag = cmd;
                    break;
                case AppBarToggleButtonData toggle when target[i] is WinUI.AppBarToggleButton atb:
                    atb.Label = toggle.Label;
                    atb.IsChecked = toggle.IsChecked;
                    if (toggle.Icon is not null) atb.Icon = new WinUI.SymbolIcon(ParseSymbol(toggle.Icon));
                    atb.Tag = toggle;
                    break;
                case AppBarSeparatorData when target[i] is WinUI.AppBarSeparator:
                    break; // nothing to update
                default:
                    // Type mismatch — replace
                    target[i] = CreateAppBarItem(source[i]);
                    break;
            }
        }

        // Remove excess
        for (int i = oldCount - 1; i >= shared; i--)
            target.RemoveAt(i);

        // Add new
        if (source is not null)
            for (int i = shared; i < newCount; i++)
                target.Add(CreateAppBarItem(source[i]));
    }

    private UIElement? UpdateGrid(Core.GridElement o, Core.GridElement n, WinUI.Grid g, Action requestRerender)
    {
        g.RowSpacing = n.RowSpacing;
        g.ColumnSpacing = n.ColumnSpacing;

        // Reconcile children positionally
        int oldCount = o.Children.Length;
        int newCount = n.Children.Length;
        int shared = Math.Min(oldCount, newCount);

        for (int i = 0; i < shared; i++)
        {
            var oldChild = o.Children[i];
            var newChild = n.Children[i];
            var existingCtrl = g.Children[i];
            var replacement = Reconcile(oldChild.Element, newChild.Element, existingCtrl, requestRerender);
            if (replacement is not null && replacement != existingCtrl)
            {
                g.Children[i] = replacement;
            }
            // Update grid placement
            if (replacement is FrameworkElement fe || g.Children[i] is FrameworkElement)
            {
                var ctrl = g.Children[i] as FrameworkElement;
                if (ctrl is not null)
                {
                    WinUI.Grid.SetRow(ctrl, newChild.Row);
                    WinUI.Grid.SetColumn(ctrl, newChild.Column);
                    if (newChild.RowSpan > 1) WinUI.Grid.SetRowSpan(ctrl, newChild.RowSpan);
                    if (newChild.ColumnSpan > 1) WinUI.Grid.SetColumnSpan(ctrl, newChild.ColumnSpan);
                }
            }
        }

        // Remove excess old children
        for (int i = oldCount - 1; i >= shared; i--)
        {
            Unmount(g.Children[i]);
            g.Children.RemoveAt(i);
        }

        // Add new children
        for (int i = shared; i < newCount; i++)
        {
            var child = n.Children[i];
            var ctrl = Mount(child.Element, requestRerender);
            if (ctrl is null) continue;
            if (ctrl is FrameworkElement fe)
            {
                WinUI.Grid.SetRow(fe, child.Row);
                WinUI.Grid.SetColumn(fe, child.Column);
                if (child.RowSpan > 1) WinUI.Grid.SetRowSpan(fe, child.RowSpan);
                if (child.ColumnSpan > 1) WinUI.Grid.SetColumnSpan(fe, child.ColumnSpan);
            }
            g.Children.Add(ctrl);
        }

        SetElementTag(g, n);
        ApplySetters(n.Setters, g);
        return null;
    }

    private UIElement? UpdateTreeView(TreeViewElement o, TreeViewElement n, WinUI.TreeView tv, Action requestRerender)
    {
        // If the node data is reference-equal (same arrays), skip entirely
        if (ReferenceEquals(o.Nodes, n.Nodes))
        {
            SetElementTag(tv, n);
            return null;
        }

        // Diff the node tree to minimize WinUI interop calls
        DiffTreeViewNodes(tv.RootNodes, o.Nodes, n.Nodes);

        tv.SelectionMode = n.SelectionMode;
        SetElementTag(tv, n);
        ApplySetters(n.Setters, tv);
        return null;
    }

    /// <summary>
    /// Recursively diff TreeViewNode lists, reusing existing nodes where Content matches.
    /// Only adds/removes/updates nodes that actually changed, minimizing COM interop calls.
    /// </summary>
    private void DiffTreeViewNodes(
        IList<WinUI.TreeViewNode> liveNodes,
        TreeViewNodeData[] oldData,
        TreeViewNodeData[] newData)
    {
        // Build lookup from old data Content → index for matching
        var oldByContent = new Dictionary<string, int>(oldData.Length);
        for (int i = 0; i < oldData.Length; i++)
            oldByContent.TryAdd(oldData[i].Content, i);

        int liveIdx = 0;
        for (int i = 0; i < newData.Length; i++)
        {
            var nd = newData[i];

            if (oldByContent.TryGetValue(nd.Content, out var oldIdx)
                && liveIdx < liveNodes.Count)
            {
                // Reuse existing node — update expand state + diff children
                var liveNode = liveNodes[liveIdx];

                if (liveNode.IsExpanded != nd.IsExpanded)
                    liveNode.IsExpanded = nd.IsExpanded;

                // Diff children
                var oldChildren = oldIdx < oldData.Length ? oldData[oldIdx].Children : null;
                var newChildren = nd.Children;

                if (ReferenceEquals(oldChildren, newChildren))
                {
                    // No change
                }
                else if (newChildren is null)
                {
                    liveNode.Children.Clear();
                }
                else if (oldChildren is null)
                {
                    liveNode.Children.Clear();
                    foreach (var child in newChildren)
                        liveNode.Children.Add(CreateTreeNode(child));
                }
                else
                {
                    DiffTreeViewNodes(liveNode.Children, oldChildren, newChildren);
                }

                liveIdx++;
            }
            else
            {
                // New node — insert at position
                var newNode = CreateTreeNode(nd);
                if (liveIdx < liveNodes.Count)
                    liveNodes.Insert(liveIdx, newNode);
                else
                    liveNodes.Add(newNode);
                liveIdx++;
            }
        }

        // Remove excess nodes from the end
        while (liveNodes.Count > newData.Length)
            liveNodes.RemoveAt(liveNodes.Count - 1);
    }

    private UIElement? UpdateComponent(Element oldEl, Element newEl, UIElement control, Action requestRerender)
    {
        ReconcileComponent(oldEl, newEl, control, requestRerender);
        return null;
    }
}

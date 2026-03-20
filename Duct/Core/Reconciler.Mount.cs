using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUI = Microsoft.UI.Xaml.Controls;
using WinPrim = Microsoft.UI.Xaml.Controls.Primitives;

namespace Duct.Core;

public sealed partial class Reconciler
{
    public UIElement? Mount(Element element, Action requestRerender)
    {
        // Registered types checked first (but after ModifiedElement, handled inside switch)
        if (element is not ModifiedElement && _typeRegistry.TryGetValue(element.GetType(), out var reg))
            return reg.Mount(element, requestRerender, this);

        return element switch
        {
            ModifiedElement mod => MountModified(mod, requestRerender),
            TextElement text => MountText(text),
            RichTextBlockElement richText => MountRichTextBlock(richText),
            ButtonElement btn => MountButton(btn),
            HyperlinkButtonElement hlBtn => MountHyperlinkButton(hlBtn),
            RepeatButtonElement repBtn => MountRepeatButton(repBtn),
            ToggleButtonElement togBtn => MountToggleButton(togBtn),
            DropDownButtonElement ddBtn => MountDropDownButton(ddBtn, requestRerender),
            SplitButtonElement spBtn => MountSplitButton(spBtn, requestRerender),
            ToggleSplitButtonElement tspBtn => MountToggleSplitButton(tspBtn, requestRerender),
            TextFieldElement tf => MountTextField(tf),
            PasswordBoxElement pw => MountPasswordBox(pw),
            NumberBoxElement nb => MountNumberBox(nb),
            AutoSuggestBoxElement asb => MountAutoSuggestBox(asb),
            CheckBoxElement cb => MountCheckBox(cb),
            RadioButtonElement rb => MountRadioButton(rb),
            RadioButtonsElement rbs => MountRadioButtons(rbs),
            ComboBoxElement combo => MountComboBox(combo),
            SliderElement sl => MountSlider(sl),
            ToggleSwitchElement ts => MountToggleSwitch(ts),
            RatingControlElement rc => MountRatingControl(rc),
            ColorPickerElement cp => MountColorPicker(cp),
            CalendarDatePickerElement cdp => MountCalendarDatePicker(cdp),
            DatePickerElement dp => MountDatePicker(dp),
            TimePickerElement tp => MountTimePicker(tp),
            ProgressElement prog => MountProgress(prog),
            ProgressRingElement ring => MountProgressRing(ring),
            ImageElement img => MountImage(img),
            PersonPictureElement pp => MountPersonPicture(pp),
            WebView2Element wv => MountWebView2(wv),
            StackElement stack => MountStack(stack, requestRerender),
            GridElement grid => MountGrid(grid, requestRerender),
            ScrollViewElement scroll => MountScrollView(scroll, requestRerender),
            BorderElement border => MountBorder(border, requestRerender),
            ExpanderElement exp => MountExpander(exp, requestRerender),
            SplitViewElement sv => MountSplitView(sv, requestRerender),
            ViewboxElement vb => MountViewbox(vb, requestRerender),
            CanvasElement cvs => MountCanvas(cvs, requestRerender),
            NavigationViewElement nav => MountNavigationView(nav, requestRerender),
            TabViewElement tab => MountTabView(tab, requestRerender),
            BreadcrumbBarElement bcb => MountBreadcrumbBar(bcb),
            PivotElement pvt => MountPivot(pvt, requestRerender),
            ListViewElement lv => MountListView(lv, requestRerender),
            GridViewElement gv => MountGridView(gv, requestRerender),
            TreeViewElement tv => MountTreeView(tv, requestRerender),
            FlipViewElement fv => MountFlipView(fv, requestRerender),
            InfoBarElement ib => MountInfoBar(ib),
            InfoBadgeElement badge => MountInfoBadge(badge),
            ContentDialogElement cdEl => MountContentDialog(cdEl, requestRerender),
            FlyoutElement flyEl => MountFlyout(flyEl, requestRerender),
            TeachingTipElement ttEl => MountTeachingTip(ttEl, requestRerender),
            MenuBarElement mbEl => MountMenuBar(mbEl),
            CommandBarElement cmdEl => MountCommandBar(cmdEl, requestRerender),
            MenuFlyoutElement mfEl => MountMenuFlyout(mfEl, requestRerender),
            LazyStackElementBase lazy => MountLazyStack(lazy, requestRerender),
            ComponentElement comp => MountComponent(comp, requestRerender),
            FuncElement func => MountFuncComponent(func, requestRerender),
            _ => null,
        };
    }

    private UIElement MountModified(ModifiedElement mod, Action requestRerender)
    {
        var inner = Mount(mod.Inner, requestRerender);
        if (inner is FrameworkElement fe) ApplyModifiers(fe, mod.Modifiers);
        return inner!;
    }

    private TextBlock MountText(TextElement text)
    {
        var tb = _pool.TryRent(typeof(TextBlock)) as TextBlock ?? new TextBlock();
        tb.Text = text.Content;
        if (text.FontSize.HasValue) tb.FontSize = text.FontSize.Value;
        if (text.Weight.HasValue) tb.FontWeight = text.Weight.Value;
        if (text.HorizontalAlignment.HasValue) tb.HorizontalAlignment = text.HorizontalAlignment.Value;
        ApplySetters(text.Setters, tb);
        return tb;
    }

    private WinUI.RichTextBlock MountRichTextBlock(RichTextBlockElement richText)
    {
        var rtb = _pool.TryRent(typeof(WinUI.RichTextBlock)) as WinUI.RichTextBlock ?? new WinUI.RichTextBlock();
        var paragraph = new Microsoft.UI.Xaml.Documents.Paragraph();
        paragraph.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = richText.Text });
        rtb.Blocks.Add(paragraph);
        if (richText.FontSize.HasValue) rtb.FontSize = richText.FontSize.Value;
        ApplySetters(richText.Setters, rtb);
        return rtb;
    }

    private WinUI.Button MountButton(ButtonElement btn)
    {
        var button = new WinUI.Button { Content = btn.Label, IsEnabled = btn.IsEnabled };
        SetElementTag(button, btn);
        button.Click += (s, _) => (GetElementTag((UIElement)s!) as ButtonElement)?.OnClick?.Invoke();
        ApplySetters(btn.Setters, button);
        return button;
    }

    private WinUI.HyperlinkButton MountHyperlinkButton(HyperlinkButtonElement hlBtn)
    {
        var hb = new WinUI.HyperlinkButton { Content = hlBtn.Content };
        if (hlBtn.NavigateUri is not null) hb.NavigateUri = hlBtn.NavigateUri;
        SetElementTag(hb, hlBtn);
        hb.Click += (s, _) => (GetElementTag((UIElement)s!) as HyperlinkButtonElement)?.OnClick?.Invoke();
        ApplySetters(hlBtn.Setters, hb);
        return hb;
    }

    private WinPrim.RepeatButton MountRepeatButton(RepeatButtonElement repBtn)
    {
        var rb = new WinPrim.RepeatButton { Content = repBtn.Label, Delay = repBtn.Delay, Interval = repBtn.Interval };
        SetElementTag(rb, repBtn);
        rb.Click += (s, _) => (GetElementTag((UIElement)s!) as RepeatButtonElement)?.OnClick?.Invoke();
        ApplySetters(repBtn.Setters, rb);
        return rb;
    }

    private WinPrim.ToggleButton MountToggleButton(ToggleButtonElement togBtn)
    {
        var tb = new WinPrim.ToggleButton { Content = togBtn.Label, IsChecked = togBtn.IsChecked };
        SetElementTag(tb, togBtn);
        tb.Checked += (s, _) => (GetElementTag((UIElement)s!) as ToggleButtonElement)?.OnToggled?.Invoke(true);
        tb.Unchecked += (s, _) => (GetElementTag((UIElement)s!) as ToggleButtonElement)?.OnToggled?.Invoke(false);
        ApplySetters(togBtn.Setters, tb);
        return tb;
    }

    private WinUI.DropDownButton MountDropDownButton(DropDownButtonElement ddBtn, Action requestRerender)
    {
        var ddb = new WinUI.DropDownButton { Content = ddBtn.Label };
        if (ddBtn.Flyout is not null)
        {
            var flyoutContent = Mount(ddBtn.Flyout, requestRerender);
            if (flyoutContent is not null) ddb.Flyout = new WinUI.Flyout { Content = flyoutContent };
        }
        ApplySetters(ddBtn.Setters, ddb);
        return ddb;
    }

    private WinUI.SplitButton MountSplitButton(SplitButtonElement spBtn, Action requestRerender)
    {
        var sb = new WinUI.SplitButton { Content = spBtn.Label };
        SetElementTag(sb, spBtn);
        sb.Click += (s, _) => (GetElementTag((UIElement)s!) as SplitButtonElement)?.OnClick?.Invoke();
        if (spBtn.Flyout is not null)
        {
            var flyoutContent = Mount(spBtn.Flyout, requestRerender);
            if (flyoutContent is not null) sb.Flyout = new WinUI.Flyout { Content = flyoutContent };
        }
        ApplySetters(spBtn.Setters, sb);
        return sb;
    }

    private WinUI.ToggleSplitButton MountToggleSplitButton(ToggleSplitButtonElement tspBtn, Action requestRerender)
    {
        var tsb = new WinUI.ToggleSplitButton { Content = tspBtn.Label, IsChecked = tspBtn.IsChecked };
        SetElementTag(tsb, tspBtn);
        tsb.IsCheckedChanged += (s, _) =>
        {
            var t = (WinUI.ToggleSplitButton)s!;
            (GetElementTag(t) as ToggleSplitButtonElement)?.OnIsCheckedChanged?.Invoke(t.IsChecked);
        };
        if (tspBtn.Flyout is not null)
        {
            var flyoutContent = Mount(tspBtn.Flyout, requestRerender);
            if (flyoutContent is not null) tsb.Flyout = new WinUI.Flyout { Content = flyoutContent };
        }
        ApplySetters(tspBtn.Setters, tsb);
        return tsb;
    }

    private TextBox MountTextField(TextFieldElement tf)
    {
        var textBox = new TextBox { Text = tf.Value, PlaceholderText = tf.Placeholder ?? "" };
        SetElementTag(textBox, tf);
        textBox.TextChanged += (_, _) => (GetElementTag(textBox) as TextFieldElement)?.OnChanged?.Invoke(textBox.Text);
        ApplySetters(tf.Setters, textBox);
        return textBox;
    }

    private WinUI.PasswordBox MountPasswordBox(PasswordBoxElement pw)
    {
        var pb = new WinUI.PasswordBox { Password = pw.Password, PlaceholderText = pw.PlaceholderText ?? "" };
        SetElementTag(pb, pw);
        pb.PasswordChanged += (s, _) => (GetElementTag((UIElement)s!) as PasswordBoxElement)?.OnPasswordChanged?.Invoke(((WinUI.PasswordBox)s!).Password);
        ApplySetters(pw.Setters, pb);
        return pb;
    }

    private WinUI.NumberBox MountNumberBox(NumberBoxElement nb)
    {
        var numBox = new WinUI.NumberBox
        {
            Value = nb.Value, Minimum = nb.Minimum, Maximum = nb.Maximum,
            SmallChange = nb.SmallChange, LargeChange = nb.LargeChange,
            PlaceholderText = nb.PlaceholderText ?? "",
            SpinButtonPlacementMode = nb.SpinButtonPlacement,
        };
        if (nb.Header is not null) numBox.Header = nb.Header;
        SetElementTag(numBox, nb);
        numBox.ValueChanged += (s, _) =>
        {
            var box = (WinUI.NumberBox)s!;
            (GetElementTag(box) as NumberBoxElement)?.OnValueChanged?.Invoke(box.Value);
        };
        ApplySetters(nb.Setters, numBox);
        return numBox;
    }

    private WinUI.AutoSuggestBox MountAutoSuggestBox(AutoSuggestBoxElement asb)
    {
        var box = new WinUI.AutoSuggestBox { Text = asb.Text, PlaceholderText = asb.PlaceholderText ?? "" };
        box.ItemsSource = asb.Suggestions;
        SetElementTag(box, asb);
        box.TextChanged += (s, args) =>
        {
            if (args.Reason == WinUI.AutoSuggestionBoxTextChangeReason.UserInput)
                (GetElementTag((UIElement)s!) as AutoSuggestBoxElement)?.OnTextChanged?.Invoke(((WinUI.AutoSuggestBox)s!).Text);
        };
        box.QuerySubmitted += (s, args) =>
            (GetElementTag((UIElement)s!) as AutoSuggestBoxElement)?.OnQuerySubmitted?.Invoke(args.QueryText);
        box.SuggestionChosen += (s, args) =>
            (GetElementTag((UIElement)s!) as AutoSuggestBoxElement)?.OnSuggestionChosen?.Invoke(args.SelectedItem?.ToString() ?? "");
        ApplySetters(asb.Setters, box);
        return box;
    }

    private WinUI.CheckBox MountCheckBox(CheckBoxElement cb)
    {
        var checkBox = new WinUI.CheckBox { IsChecked = cb.IsChecked, Content = cb.Label };
        SetElementTag(checkBox, cb);
        checkBox.Checked += (_, _) => (GetElementTag(checkBox) as CheckBoxElement)?.OnChanged?.Invoke(true);
        checkBox.Unchecked += (_, _) => (GetElementTag(checkBox) as CheckBoxElement)?.OnChanged?.Invoke(false);
        ApplySetters(cb.Setters, checkBox);
        return checkBox;
    }

    private WinUI.RadioButton MountRadioButton(RadioButtonElement rb)
    {
        var radio = new WinUI.RadioButton { Content = rb.Label, IsChecked = rb.IsChecked };
        if (rb.GroupName is not null) radio.GroupName = rb.GroupName;
        SetElementTag(radio, rb);
        radio.Checked += (s, _) => (GetElementTag((UIElement)s!) as RadioButtonElement)?.OnChecked?.Invoke(true);
        radio.Unchecked += (s, _) => (GetElementTag((UIElement)s!) as RadioButtonElement)?.OnChecked?.Invoke(false);
        ApplySetters(rb.Setters, radio);
        return radio;
    }

    private WinUI.RadioButtons MountRadioButtons(RadioButtonsElement rbs)
    {
        var rbGroup = new WinUI.RadioButtons { SelectedIndex = rbs.SelectedIndex };
        if (rbs.Header is not null) rbGroup.Header = rbs.Header;
        foreach (var item in rbs.Items) rbGroup.Items.Add(item);
        SetElementTag(rbGroup, rbs);
        rbGroup.SelectionChanged += (s, _) =>
        {
            var g = (WinUI.RadioButtons)s!;
            (GetElementTag(g) as RadioButtonsElement)?.OnSelectionChanged?.Invoke(g.SelectedIndex);
        };
        ApplySetters(rbs.Setters, rbGroup);
        return rbGroup;
    }

    private WinUI.ComboBox MountComboBox(ComboBoxElement combo)
    {
        var cb = new WinUI.ComboBox
        {
            SelectedIndex = combo.SelectedIndex,
            PlaceholderText = combo.PlaceholderText ?? "",
            IsEditable = combo.IsEditable,
        };
        if (combo.Header is not null) cb.Header = combo.Header;
        foreach (var item in combo.Items) cb.Items.Add(item);
        SetElementTag(cb, combo);
        cb.SelectionChanged += (s, _) =>
        {
            var c = (WinUI.ComboBox)s!;
            (GetElementTag(c) as ComboBoxElement)?.OnSelectionChanged?.Invoke(c.SelectedIndex);
        };
        ApplySetters(combo.Setters, cb);
        return cb;
    }

    private WinUI.Slider MountSlider(SliderElement sl)
    {
        var slider = new WinUI.Slider { Value = sl.Value, Minimum = sl.Min, Maximum = sl.Max, StepFrequency = sl.StepFrequency };
        if (sl.Header is not null) slider.Header = sl.Header;
        SetElementTag(slider, sl);
        slider.ValueChanged += (_, args) => (GetElementTag(slider) as SliderElement)?.OnChanged?.Invoke(args.NewValue);
        ApplySetters(sl.Setters, slider);
        return slider;
    }

    private WinUI.ToggleSwitch MountToggleSwitch(ToggleSwitchElement ts)
    {
        var toggle = new WinUI.ToggleSwitch { IsOn = ts.IsOn, OnContent = ts.OnContent, OffContent = ts.OffContent };
        if (ts.Header is not null) toggle.Header = ts.Header;
        SetElementTag(toggle, ts);
        toggle.Toggled += (s, _) =>
        {
            var t = (WinUI.ToggleSwitch)s!;
            (GetElementTag(t) as ToggleSwitchElement)?.OnChanged?.Invoke(t.IsOn);
        };
        ApplySetters(ts.Setters, toggle);
        return toggle;
    }

    private WinUI.RatingControl MountRatingControl(RatingControlElement rc)
    {
        var rating = new WinUI.RatingControl { Value = rc.Value, MaxRating = rc.MaxRating, IsReadOnly = rc.IsReadOnly, Caption = rc.Caption ?? "" };
        SetElementTag(rating, rc);
        rating.ValueChanged += (s, _) =>
        {
            var r = (WinUI.RatingControl)s!;
            (GetElementTag(r) as RatingControlElement)?.OnValueChanged?.Invoke(r.Value);
        };
        ApplySetters(rc.Setters, rating);
        return rating;
    }

    private WinUI.ColorPicker MountColorPicker(ColorPickerElement cp)
    {
        var picker = new WinUI.ColorPicker
        {
            Color = cp.Color, IsAlphaEnabled = cp.IsAlphaEnabled, IsMoreButtonVisible = cp.IsMoreButtonVisible,
            IsColorSpectrumVisible = cp.IsColorSpectrumVisible, IsColorSliderVisible = cp.IsColorSliderVisible,
            IsColorChannelTextInputVisible = cp.IsColorChannelTextInputVisible, IsHexInputVisible = cp.IsHexInputVisible,
        };
        SetElementTag(picker, cp);
        picker.ColorChanged += (s, args) => (GetElementTag((UIElement)s!) as ColorPickerElement)?.OnColorChanged?.Invoke(args.NewColor);
        ApplySetters(cp.Setters, picker);
        return picker;
    }

    private WinUI.CalendarDatePicker MountCalendarDatePicker(CalendarDatePickerElement cdp)
    {
        var cal = new WinUI.CalendarDatePicker { Date = cdp.Date, PlaceholderText = cdp.PlaceholderText ?? "" };
        if (cdp.Header is not null) cal.Header = cdp.Header;
        if (cdp.MinDate.HasValue) cal.MinDate = cdp.MinDate.Value;
        if (cdp.MaxDate.HasValue) cal.MaxDate = cdp.MaxDate.Value;
        SetElementTag(cal, cdp);
        cal.DateChanged += (s, _) =>
        {
            var c = (WinUI.CalendarDatePicker)s!;
            (GetElementTag(c) as CalendarDatePickerElement)?.OnDateChanged?.Invoke(c.Date);
        };
        ApplySetters(cdp.Setters, cal);
        return cal;
    }

    private WinUI.DatePicker MountDatePicker(DatePickerElement dp)
    {
        var picker = new WinUI.DatePicker { Date = dp.Date, DayVisible = dp.DayVisible, MonthVisible = dp.MonthVisible, YearVisible = dp.YearVisible };
        if (dp.Header is not null) picker.Header = dp.Header;
        if (dp.MinYear.HasValue) picker.MinYear = dp.MinYear.Value;
        if (dp.MaxYear.HasValue) picker.MaxYear = dp.MaxYear.Value;
        SetElementTag(picker, dp);
        picker.DateChanged += (s, args) => (GetElementTag((UIElement)s!) as DatePickerElement)?.OnDateChanged?.Invoke(args.NewDate);
        ApplySetters(dp.Setters, picker);
        return picker;
    }

    private WinUI.TimePicker MountTimePicker(TimePickerElement tp)
    {
        var picker = new WinUI.TimePicker { Time = tp.Time, MinuteIncrement = tp.MinuteIncrement };
        if (tp.Header is not null) picker.Header = tp.Header;
        SetElementTag(picker, tp);
        picker.TimeChanged += (s, args) => (GetElementTag((UIElement)s!) as TimePickerElement)?.OnTimeChanged?.Invoke(args.NewTime);
        ApplySetters(tp.Setters, picker);
        return picker;
    }

    private WinUI.ProgressBar MountProgress(ProgressElement prog)
    {
        var bar = _pool.TryRent(typeof(WinUI.ProgressBar)) as WinUI.ProgressBar ?? new WinUI.ProgressBar();
        bar.IsIndeterminate = prog.IsIndeterminate;
        bar.Minimum = prog.Minimum;
        bar.Maximum = prog.Maximum;
        bar.ShowError = prog.ShowError;
        bar.ShowPaused = prog.ShowPaused;
        if (prog.Value.HasValue) bar.Value = prog.Value.Value;
        ApplySetters(prog.Setters, bar);
        return bar;
    }

    private WinUI.ProgressRing MountProgressRing(ProgressRingElement ring)
    {
        var pr = _pool.TryRent(typeof(WinUI.ProgressRing)) as WinUI.ProgressRing ?? new WinUI.ProgressRing();
        pr.IsIndeterminate = ring.IsIndeterminate;
        pr.IsActive = ring.IsActive;
        pr.Minimum = ring.Minimum;
        pr.Maximum = ring.Maximum;
        if (ring.Value.HasValue) pr.Value = ring.Value.Value;
        ApplySetters(ring.Setters, pr);
        return pr;
    }

    private WinUI.Image MountImage(ImageElement img)
    {
        var image = _pool.TryRent(typeof(WinUI.Image)) as WinUI.Image ?? new WinUI.Image();
        image.Source = new BitmapImage(new Uri(img.Source, UriKind.RelativeOrAbsolute));
        if (img.Width.HasValue) image.Width = img.Width.Value;
        if (img.Height.HasValue) image.Height = img.Height.Value;
        ApplySetters(img.Setters, image);
        return image;
    }

    private WinUI.PersonPicture MountPersonPicture(PersonPictureElement pp)
    {
        var pic = new WinUI.PersonPicture { IsGroup = pp.IsGroup, BadgeNumber = pp.BadgeNumber };
        if (pp.DisplayName is not null) pic.DisplayName = pp.DisplayName;
        if (pp.Initials is not null) pic.Initials = pp.Initials;
        if (pp.ProfilePicture is not null)
            pic.ProfilePicture = new BitmapImage(new Uri(pp.ProfilePicture, UriKind.RelativeOrAbsolute));
        ApplySetters(pp.Setters, pic);
        return pic;
    }

    private WinUI.WebView2 MountWebView2(WebView2Element wv)
    {
        var webView = new WinUI.WebView2();
        if (wv.Source is not null) webView.Source = wv.Source;
        SetElementTag(webView, wv);
        webView.NavigationStarting += (s, args) =>
            (GetElementTag((UIElement)s!) as WebView2Element)?.OnNavigationStarting?.Invoke(new Uri(args.Uri));
        webView.NavigationCompleted += (s, _) =>
            (GetElementTag((UIElement)s!) as WebView2Element)?.OnNavigationCompleted?.Invoke(((WinUI.WebView2)s!).Source);
        ApplySetters(wv.Setters, webView);
        return webView;
    }

    private WinUI.StackPanel MountStack(StackElement stack, Action requestRerender)
    {
        var panel = _pool.TryRent(typeof(WinUI.StackPanel)) as WinUI.StackPanel ?? new WinUI.StackPanel();
        panel.Orientation = stack.Orientation;
        panel.Spacing = stack.Spacing;
        if (stack.HorizontalAlignment.HasValue) panel.HorizontalAlignment = stack.HorizontalAlignment.Value;
        if (stack.VerticalAlignment.HasValue) panel.VerticalAlignment = stack.VerticalAlignment.Value;
        foreach (var child in stack.Children)
        {
            if (child is null or EmptyElement) continue;
            var childControl = Mount(child, requestRerender);
            if (childControl is not null) panel.Children.Add(childControl);
        }
        SetElementTag(panel, stack);
        ApplySetters(stack.Setters, panel);
        return panel;
    }

    private WinUI.Grid MountGrid(GridElement grid, Action requestRerender)
    {
        var g = _pool.TryRent(typeof(WinUI.Grid)) as WinUI.Grid ?? new WinUI.Grid();
        g.RowSpacing = grid.RowSpacing;
        g.ColumnSpacing = grid.ColumnSpacing;
        g.ColumnDefinitions.Clear();
        g.RowDefinitions.Clear();
        foreach (var col in grid.Definition.Columns) g.ColumnDefinitions.Add(ParseColumnDef(col));
        foreach (var row in grid.Definition.Rows) g.RowDefinitions.Add(ParseRowDef(row));
        foreach (var child in grid.Children)
        {
            var ctrl = Mount(child.Element, requestRerender);
            if (ctrl is null) continue;
            WinUI.Grid.SetRow(ctrl as FrameworkElement, child.Row);
            WinUI.Grid.SetColumn(ctrl as FrameworkElement, child.Column);
            if (child.RowSpan > 1) WinUI.Grid.SetRowSpan(ctrl as FrameworkElement, child.RowSpan);
            if (child.ColumnSpan > 1) WinUI.Grid.SetColumnSpan(ctrl as FrameworkElement, child.ColumnSpan);
            g.Children.Add(ctrl);
        }
        SetElementTag(g, grid);
        ApplySetters(grid.Setters, g);
        return g;
    }

    private WinUI.ScrollViewer MountScrollView(ScrollViewElement scroll, Action requestRerender)
    {
        var sv = _pool.TryRent(typeof(WinUI.ScrollViewer)) as WinUI.ScrollViewer ?? new WinUI.ScrollViewer();
        sv.HorizontalScrollBarVisibility = scroll.HorizontalScrollBarVisibility;
        sv.VerticalScrollBarVisibility = scroll.VerticalScrollBarVisibility;
        sv.Content = Mount(scroll.Child, requestRerender);
        SetElementTag(sv, scroll);
        ApplySetters(scroll.Setters, sv);
        return sv;
    }

    private WinUI.Border MountBorder(BorderElement border, Action requestRerender)
    {
        var bdr = _pool.TryRent(typeof(WinUI.Border)) as WinUI.Border ?? new WinUI.Border();
        if (border.CornerRadius.HasValue) bdr.CornerRadius = new Microsoft.UI.Xaml.CornerRadius(border.CornerRadius.Value);
        if (border.Padding.HasValue) bdr.Padding = border.Padding.Value;
        if (border.Background is not null) bdr.Background = border.Background;
        if (border.BorderBrush is not null) bdr.BorderBrush = border.BorderBrush;
        if (border.BorderThickness.HasValue) bdr.BorderThickness = new Microsoft.UI.Xaml.Thickness(border.BorderThickness.Value);
        bdr.Child = Mount(border.Child, requestRerender);
        SetElementTag(bdr, border);
        ApplySetters(border.Setters, bdr);
        return bdr;
    }

    private WinUI.Expander MountExpander(ExpanderElement exp, Action requestRerender)
    {
        var expander = new WinUI.Expander
        {
            Header = exp.Header, IsExpanded = exp.IsExpanded,
            Content = Mount(exp.Content, requestRerender),
            ExpandDirection = exp.ExpandDirection,
        };
        SetElementTag(expander, exp);
        expander.Expanding += (s, _) => (GetElementTag((UIElement)s!) as ExpanderElement)?.OnExpandedChanged?.Invoke(true);
        expander.Collapsed += (s, _) => (GetElementTag((UIElement)s!) as ExpanderElement)?.OnExpandedChanged?.Invoke(false);
        ApplySetters(exp.Setters, expander);
        return expander;
    }

    private WinUI.SplitView MountSplitView(SplitViewElement svEl, Action requestRerender)
    {
        var splitView = new WinUI.SplitView
        {
            IsPaneOpen = svEl.IsPaneOpen, OpenPaneLength = svEl.OpenPaneLength,
            CompactPaneLength = svEl.CompactPaneLength, DisplayMode = svEl.DisplayMode,
        };
        if (svEl.Pane is not null) splitView.Pane = Mount(svEl.Pane, requestRerender);
        if (svEl.Content is not null) splitView.Content = Mount(svEl.Content, requestRerender);
        SetElementTag(splitView, svEl);
        splitView.PaneOpening += (s, _) => (GetElementTag((UIElement)s!) as SplitViewElement)?.OnPaneOpenChanged?.Invoke(true);
        splitView.PaneClosing += (s, _) => (GetElementTag((UIElement)s!) as SplitViewElement)?.OnPaneOpenChanged?.Invoke(false);
        ApplySetters(svEl.Setters, splitView);
        return splitView;
    }

    private WinUI.Viewbox MountViewbox(ViewboxElement vb, Action requestRerender)
    {
        var viewbox = _pool.TryRent(typeof(WinUI.Viewbox)) as WinUI.Viewbox ?? new WinUI.Viewbox();
        viewbox.Child = Mount(vb.Child, requestRerender) as UIElement;
        ApplySetters(vb.Setters, viewbox);
        return viewbox;
    }

    private WinUI.Canvas MountCanvas(CanvasElement cvs, Action requestRerender)
    {
        var canvas = _pool.TryRent(typeof(WinUI.Canvas)) as WinUI.Canvas ?? new WinUI.Canvas();
        if (cvs.Width.HasValue) canvas.Width = cvs.Width.Value;
        if (cvs.Height.HasValue) canvas.Height = cvs.Height.Value;
        if (cvs.Background is not null) canvas.Background = cvs.Background;
        foreach (var child in cvs.Children)
        {
            var ctrl = Mount(child.Element, requestRerender);
            if (ctrl is null) continue;
            WinUI.Canvas.SetLeft(ctrl as FrameworkElement, child.Left);
            WinUI.Canvas.SetTop(ctrl as FrameworkElement, child.Top);
            canvas.Children.Add(ctrl);
        }
        SetElementTag(canvas, cvs);
        ApplySetters(cvs.Setters, canvas);
        return canvas;
    }

    private WinUI.NavigationView MountNavigationView(NavigationViewElement nav, Action requestRerender)
    {
        var nv = new WinUI.NavigationView
        {
            IsPaneOpen = nav.IsPaneOpen, PaneDisplayMode = nav.PaneDisplayMode,
            IsBackEnabled = nav.IsBackEnabled, IsSettingsVisible = nav.IsSettingsVisible,
        };
        if (nav.PaneTitle is not null) nv.PaneTitle = nav.PaneTitle;
        if (nav.Header is not null) nv.Header = Mount(nav.Header, requestRerender);
        foreach (var item in nav.MenuItems) nv.MenuItems.Add(CreateNavItem(item));
        if (nav.Content is not null) nv.Content = Mount(nav.Content, requestRerender);
        if (nav.SelectedTag is not null)
        {
            foreach (var mi in nv.MenuItems.OfType<WinUI.NavigationViewItem>())
                if (mi.Tag as string == nav.SelectedTag) { nv.SelectedItem = mi; break; }
        }
        SetElementTag(nv, nav);
        nv.SelectionChanged += (s, args) =>
        {
            var selected = args.SelectedItem as WinUI.NavigationViewItem;
            (GetElementTag((UIElement)s!) as NavigationViewElement)?.OnSelectionChanged?.Invoke(selected?.Tag as string);
        };
        nv.BackRequested += (s, _) => (GetElementTag((UIElement)s!) as NavigationViewElement)?.OnBackRequested?.Invoke();
        ApplySetters(nav.Setters, nv);
        return nv;
    }

    private static WinUI.NavigationViewItem CreateNavItem(NavigationViewItemData data)
    {
        var item = new WinUI.NavigationViewItem { Content = data.Content, Tag = data.Tag ?? data.Content };
        if (data.Icon is not null) item.Icon = new WinUI.SymbolIcon(ParseSymbol(data.Icon));
        if (data.Children is not null)
            foreach (var child in data.Children) item.MenuItems.Add(CreateNavItem(child));
        return item;
    }

    private WinUI.TabView MountTabView(TabViewElement tab, Action requestRerender)
    {
        var tv = new WinUI.TabView { SelectedIndex = tab.SelectedIndex, IsAddTabButtonVisible = tab.IsAddTabButtonVisible };
        foreach (var tabItem in tab.Tabs)
        {
            var tvi = new WinUI.TabViewItem
            {
                Header = tabItem.Header, IsClosable = tabItem.IsClosable,
                Content = Mount(tabItem.Content, requestRerender),
            };
            if (tabItem.Icon is not null) tvi.IconSource = new WinUI.SymbolIconSource { Symbol = ParseSymbol(tabItem.Icon) };
            tv.TabItems.Add(tvi);
        }
        SetElementTag(tv, tab);
        tv.SelectionChanged += (s, _) =>
        {
            var t = (WinUI.TabView)s!;
            (GetElementTag(t) as TabViewElement)?.OnSelectionChanged?.Invoke(t.SelectedIndex);
        };
        tv.TabCloseRequested += (s, args) =>
        {
            var t = (WinUI.TabView)s!;
            var idx = t.TabItems.IndexOf(args.Tab);
            (GetElementTag(t) as TabViewElement)?.OnTabCloseRequested?.Invoke(idx);
        };
        tv.AddTabButtonClick += (s, _) => (GetElementTag((UIElement)s!) as TabViewElement)?.OnAddTabButtonClick?.Invoke();
        ApplySetters(tab.Setters, tv);
        return tv;
    }

    private WinUI.BreadcrumbBar MountBreadcrumbBar(BreadcrumbBarElement bcb)
    {
        var bar = new WinUI.BreadcrumbBar();
        bar.ItemsSource = bcb.Items.Select(i => i.Label).ToList();
        SetElementTag(bar, bcb);
        bar.ItemClicked += (s, args) =>
        {
            var el = GetElementTag((UIElement)s!) as BreadcrumbBarElement;
            if (el is not null && args.Index >= 0 && args.Index < el.Items.Length) el.OnItemClicked?.Invoke(el.Items[args.Index]);
        };
        ApplySetters(bcb.Setters, bar);
        return bar;
    }

    private WinUI.Pivot MountPivot(PivotElement pvt, Action requestRerender)
    {
        var pivot = new WinUI.Pivot { SelectedIndex = pvt.SelectedIndex };
        if (pvt.Title is not null) pivot.Title = pvt.Title;
        foreach (var item in pvt.Items)
        {
            var pi = new WinUI.PivotItem { Header = item.Header, Content = Mount(item.Content, requestRerender) };
            pivot.Items.Add(pi);
        }
        SetElementTag(pivot, pvt);
        pivot.SelectionChanged += (s, _) =>
        {
            var p = (WinUI.Pivot)s!;
            (GetElementTag(p) as PivotElement)?.OnSelectionChanged?.Invoke(p.SelectedIndex);
        };
        ApplySetters(pvt.Setters, pivot);
        return pivot;
    }

    private WinUI.ListView MountListView(ListViewElement lv, Action requestRerender)
    {
        var listView = new WinUI.ListView
        {
            SelectionMode = lv.SelectionMode,
            IsItemClickEnabled = lv.OnItemClick is not null,
        };
        if (lv.Header is not null) listView.Header = lv.Header;

        SetElementTag(listView, lv);

        // DataTemplate with a ContentControl shell — we populate its Content on demand
        listView.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
            "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
            "<ContentControl HorizontalContentAlignment='Stretch' VerticalContentAlignment='Stretch'/>" +
            "</DataTemplate>");

        listView.ContainerContentChanging += (sender, args) =>
        {
            if (args.InRecycleQueue)
            {
                if (args.ItemContainer.ContentTemplateRoot is ContentControl oldCc)
                {
                    if (oldCc.Content is UIElement oldCtrl)
                        UnmountChild(oldCtrl);
                    oldCc.Content = null;
                }
                return;
            }

            args.Handled = true;
            var items = (GetElementTag((UIElement)sender!) as ListViewElement)?.Items;
            if (items is not null && args.ItemIndex >= 0 && args.ItemIndex < items.Length
                && args.ItemContainer.ContentTemplateRoot is ContentControl cc)
            {
                var ctrl = Mount(items[args.ItemIndex], requestRerender);
                cc.Content = ctrl;
            }
        };

        listView.SelectionChanged += (s, _) =>
        {
            var l = (WinUI.ListView)s!;
            (GetElementTag(l) as ListViewElement)?.OnSelectionChanged?.Invoke(l.SelectedIndex);
        };
        listView.ItemClick += (s, args) =>
        {
            var l = (WinUI.ListView)s!;
            if (args.ClickedItem is int idx)
                (GetElementTag(l) as ListViewElement)?.OnItemClick?.Invoke(idx);
        };

        // Set ItemsSource LAST — triggers container creation which needs the handler above
        listView.ItemsSource = Enumerable.Range(0, lv.Items.Length).ToList();

        if (lv.SelectedIndex >= 0) listView.SelectedIndex = lv.SelectedIndex;
        ApplySetters(lv.Setters, listView);
        return listView;
    }

    private WinUI.GridView MountGridView(GridViewElement gv, Action requestRerender)
    {
        var gridView = new WinUI.GridView
        {
            SelectionMode = gv.SelectionMode,
            IsItemClickEnabled = gv.OnItemClick is not null,
        };
        if (gv.Header is not null) gridView.Header = gv.Header;

        SetElementTag(gridView, gv);

        gridView.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
            "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
            "<ContentControl HorizontalContentAlignment='Stretch' VerticalContentAlignment='Stretch'/>" +
            "</DataTemplate>");

        gridView.ContainerContentChanging += (sender, args) =>
        {
            if (args.InRecycleQueue)
            {
                if (args.ItemContainer.ContentTemplateRoot is ContentControl oldCc)
                {
                    if (oldCc.Content is UIElement oldCtrl)
                        UnmountChild(oldCtrl);
                    oldCc.Content = null;
                }
                return;
            }

            args.Handled = true;
            var items = (GetElementTag((UIElement)sender!) as GridViewElement)?.Items;
            if (items is not null && args.ItemIndex >= 0 && args.ItemIndex < items.Length
                && args.ItemContainer.ContentTemplateRoot is ContentControl cc)
            {
                var ctrl = Mount(items[args.ItemIndex], requestRerender);
                cc.Content = ctrl;
            }
        };

        gridView.SelectionChanged += (s, _) =>
        {
            var g = (WinUI.GridView)s!;
            (GetElementTag(g) as GridViewElement)?.OnSelectionChanged?.Invoke(g.SelectedIndex);
        };
        gridView.ItemClick += (s, args) =>
        {
            var g = (WinUI.GridView)s!;
            if (args.ClickedItem is int idx)
                (GetElementTag(g) as GridViewElement)?.OnItemClick?.Invoke(idx);
        };

        gridView.ItemsSource = Enumerable.Range(0, gv.Items.Length).ToList();

        if (gv.SelectedIndex >= 0) gridView.SelectedIndex = gv.SelectedIndex;
        ApplySetters(gv.Setters, gridView);
        return gridView;
    }

    private WinUI.TreeView MountTreeView(TreeViewElement tv, Action requestRerender)
    {
        var treeView = new WinUI.TreeView
        {
            SelectionMode = tv.SelectionMode,
        };

        // Always store TreeViewNodeData as Content so Expanding/ItemInvoked can retrieve it.
        // Use an ItemTemplate with binding to display the Content string property.
        // In node-mode, DataContext of the template = TreeViewNode,
        // so {Binding Content.Content} resolves TreeViewNode.Content (TreeViewNodeData) → .Content (string).
        treeView.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
            "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
            "<TextBlock Text='{Binding Content.Content}'/>" +
            "</DataTemplate>");

        foreach (var node in tv.Nodes)
            treeView.RootNodes.Add(CreateTreeNode(node));

        SetElementTag(treeView, tv);

        treeView.ItemInvoked += (s, args) =>
        {
            var t = (WinUI.TreeView)s!;
            if (args.InvokedItem is WinUI.TreeViewNode tvn
                && tvn.Content is TreeViewNodeData nodeData)
            {
                (GetElementTag(t) as TreeViewElement)?.OnItemInvoked?.Invoke(nodeData);
            }
        };

        treeView.Expanding += (s, args) =>
        {
            var t = (WinUI.TreeView)s!;
            if (args.Node.Content is TreeViewNodeData nodeData)
                (GetElementTag(t) as TreeViewElement)?.OnExpanding?.Invoke(nodeData);
        };

        ApplySetters(tv.Setters, treeView);
        return treeView;
    }

    private static WinUI.TreeViewNode CreateTreeNode(TreeViewNodeData data)
    {
        var node = new WinUI.TreeViewNode { Content = data, IsExpanded = data.IsExpanded };
        if (data.Children is not null)
            foreach (var child in data.Children) node.Children.Add(CreateTreeNode(child));
        return node;
    }

    private WinUI.FlipView MountFlipView(FlipViewElement fv, Action requestRerender)
    {
        var flipView = new WinUI.FlipView { SelectedIndex = fv.SelectedIndex };
        foreach (var item in fv.Items)
        {
            var ctrl = Mount(item, requestRerender);
            if (ctrl is not null) flipView.Items.Add(ctrl);
        }
        SetElementTag(flipView, fv);
        flipView.SelectionChanged += (s, _) =>
        {
            var f = (WinUI.FlipView)s!;
            (GetElementTag(f) as FlipViewElement)?.OnSelectionChanged?.Invoke(f.SelectedIndex);
        };
        ApplySetters(fv.Setters, flipView);
        return flipView;
    }

    private WinUI.InfoBar MountInfoBar(InfoBarElement ib)
    {
        var infoBar = new WinUI.InfoBar
        {
            Title = ib.Title ?? "", Message = ib.Message ?? "",
            Severity = ib.Severity, IsOpen = ib.IsOpen, IsClosable = ib.IsClosable,
        };
        if (ib.ActionButtonContent is not null)
        {
            infoBar.ActionButton = new WinUI.Button { Content = ib.ActionButtonContent };
            SetElementTag(infoBar.ActionButton, ib);
            ((WinUI.Button)infoBar.ActionButton).Click += (s, _) =>
                (GetElementTag((UIElement)s!) as InfoBarElement)?.OnActionButtonClick?.Invoke();
        }
        SetElementTag(infoBar, ib);
        infoBar.Closed += (s, _) => (GetElementTag((UIElement)s!) as InfoBarElement)?.OnClosed?.Invoke();
        ApplySetters(ib.Setters, infoBar);
        return infoBar;
    }

    private WinUI.InfoBadge MountInfoBadge(InfoBadgeElement badge)
    {
        var ib = _pool.TryRent(typeof(WinUI.InfoBadge)) as WinUI.InfoBadge ?? new WinUI.InfoBadge();
        if (badge.Value.HasValue) ib.Value = badge.Value.Value;
        ApplySetters(badge.Setters, ib);
        return ib;
    }

    private UIElement MountContentDialog(ContentDialogElement cdEl, Action requestRerender)
    {
        var placeholder = new WinUI.StackPanel { Visibility = Visibility.Collapsed };
        SetElementTag(placeholder, cdEl);
        if (cdEl.IsOpen) ShowContentDialog(cdEl, requestRerender);
        return placeholder;
    }

    private async void ShowContentDialog(ContentDialogElement cdEl, Action requestRerender)
    {
        var dialog = new WinUI.ContentDialog
        {
            Title = cdEl.Title, PrimaryButtonText = cdEl.PrimaryButtonText,
            DefaultButton = cdEl.DefaultButton,
            XamlRoot = null,
        };
        if (cdEl.SecondaryButtonText is not null) dialog.SecondaryButtonText = cdEl.SecondaryButtonText;
        if (cdEl.CloseButtonText is not null) dialog.CloseButtonText = cdEl.CloseButtonText;
        dialog.Content = Mount(cdEl.Content, requestRerender);
        ApplySetters(cdEl.Setters, dialog);
        try
        {
            if (dialog.Content is UIElement contentUi && contentUi.XamlRoot is not null)
                dialog.XamlRoot = contentUi.XamlRoot;
            var winUiResult = await dialog.ShowAsync();
            cdEl.OnClosed?.Invoke(winUiResult);
        }
        catch { }
    }

    private UIElement? MountFlyout(FlyoutElement flyEl, Action requestRerender)
    {
        var target = Mount(flyEl.Target, requestRerender);
        if (target is FrameworkElement targetFe)
        {
            var flyoutContent = Mount(flyEl.FlyoutContent, requestRerender);
            var flyout = new WinUI.Flyout { Content = flyoutContent, Placement = flyEl.Placement };
            flyout.Opened += (_, _) => flyEl.OnOpened?.Invoke();
            flyout.Closed += (_, _) => flyEl.OnClosed?.Invoke();
            SetElementTag(targetFe, flyEl);
            WinPrim.FlyoutBase.SetAttachedFlyout(targetFe, flyout);
            ApplySetters(flyEl.Setters, flyout);
            if (flyEl.IsOpen) WinPrim.FlyoutBase.ShowAttachedFlyout(targetFe);
        }
        return target;
    }

    private WinUI.TeachingTip MountTeachingTip(TeachingTipElement ttEl, Action requestRerender)
    {
        var tip = new WinUI.TeachingTip { Title = ttEl.Title, Subtitle = ttEl.Subtitle ?? "", IsOpen = ttEl.IsOpen };
        if (ttEl.Content is not null) tip.Content = Mount(ttEl.Content, requestRerender);
        if (ttEl.ActionButtonContent is not null)
        {
            tip.ActionButtonContent = ttEl.ActionButtonContent;
            tip.ActionButtonClick += (_, _) => ttEl.OnActionButtonClick?.Invoke();
        }
        if (ttEl.CloseButtonContent is not null) tip.CloseButtonContent = ttEl.CloseButtonContent;
        SetElementTag(tip, ttEl);
        tip.Closed += (s, _) => (GetElementTag((UIElement)s!) as TeachingTipElement)?.OnClosed?.Invoke();
        ApplySetters(ttEl.Setters, tip);
        return tip;
    }

    private WinUI.MenuBar MountMenuBar(MenuBarElement mbEl)
    {
        var menuBar = new WinUI.MenuBar();
        foreach (var menuItem in mbEl.Items)
        {
            var mbi = new WinUI.MenuBarItem { Title = menuItem.Title };
            foreach (var flyoutItem in menuItem.Items) mbi.Items.Add(CreateMenuFlyoutItem(flyoutItem));
            menuBar.Items.Add(mbi);
        }
        ApplySetters(mbEl.Setters, menuBar);
        return menuBar;
    }

    private WinUI.CommandBar MountCommandBar(CommandBarElement cmdEl, Action requestRerender)
    {
        var commandBar = new WinUI.CommandBar
        {
            DefaultLabelPosition = cmdEl.DefaultLabelPosition,
            IsOpen = cmdEl.IsOpen,
        };
        if (cmdEl.Content is not null) commandBar.Content = Mount(cmdEl.Content, requestRerender);
        if (cmdEl.PrimaryCommands is not null)
            foreach (var cmd in cmdEl.PrimaryCommands) commandBar.PrimaryCommands.Add(CreateAppBarButton(cmd));
        if (cmdEl.SecondaryCommands is not null)
            foreach (var cmd in cmdEl.SecondaryCommands) commandBar.SecondaryCommands.Add(CreateAppBarButton(cmd));
        SetElementTag(commandBar, cmdEl);
        ApplySetters(cmdEl.Setters, commandBar);
        return commandBar;
    }

    private static WinUI.AppBarButton CreateAppBarButton(AppBarButtonData cmd)
    {
        var abb = new WinUI.AppBarButton { Label = cmd.Label };
        if (cmd.Icon is not null) abb.Icon = new WinUI.SymbolIcon(ParseSymbol(cmd.Icon));
        abb.Tag = cmd;
        abb.Click += (s, _) => ((AppBarButtonData)((WinUI.AppBarButton)s!).Tag!).OnClick?.Invoke();
        return abb;
    }

    private UIElement? MountMenuFlyout(MenuFlyoutElement mfEl, Action requestRerender)
    {
        var target = Mount(mfEl.Target, requestRerender);
        if (target is FrameworkElement targetFe)
        {
            var menuFlyout = new WinUI.MenuFlyout();
            foreach (var item in mfEl.Items) menuFlyout.Items.Add(CreateMenuFlyoutItem(item));
            SetElementTag(targetFe, mfEl);
            WinPrim.FlyoutBase.SetAttachedFlyout(targetFe, menuFlyout);
            ApplySetters(mfEl.Setters, menuFlyout);
        }
        return target;
    }

    private static WinUI.MenuFlyoutItemBase CreateMenuFlyoutItem(MenuFlyoutItemBase item)
    {
        switch (item)
        {
            case MenuFlyoutItemData mfi:
            {
                var flyoutItem = new WinUI.MenuFlyoutItem { Text = mfi.Text };
                if (mfi.Icon is not null) flyoutItem.Icon = new WinUI.SymbolIcon(ParseSymbol(mfi.Icon));
                flyoutItem.Tag = mfi;
                flyoutItem.Click += (s, _) => ((MenuFlyoutItemData)((WinUI.MenuFlyoutItem)s!).Tag!).OnClick?.Invoke();
                return flyoutItem;
            }
            case MenuFlyoutSeparatorData:
                return new WinUI.MenuFlyoutSeparator();
            case MenuFlyoutSubItemData sub:
            {
                var subItem = new WinUI.MenuFlyoutSubItem { Text = sub.Text };
                if (sub.Icon is not null) subItem.Icon = new WinUI.SymbolIcon(ParseSymbol(sub.Icon));
                foreach (var child in sub.Items) subItem.Items.Add(CreateMenuFlyoutItem(child));
                return subItem;
            }
            default:
                return new WinUI.MenuFlyoutSeparator();
        }
    }

    private UIElement MountComponent(ComponentElement compElement, Action requestRerender)
    {
        var component = compElement.CreateInstance();

        if (compElement.Props is not null && component is IPropsReceiver receiver)
            receiver.SetProps(compElement.Props);

        component.Context.BeginRender(requestRerender);
        var childElement = component.Render();
        component.Context.FlushEffects();
        var control = Mount(childElement, requestRerender);
        if (control is not null)
        {
            _componentNodes[control] = new ComponentNode
            {
                Component = component, RenderedElement = childElement, Element = compElement,
            };
        }
        return control!;
    }

    private UIElement MountFuncComponent(FuncElement funcElement, Action requestRerender)
    {
        var ctx = new RenderContext();
        ctx.BeginRender(requestRerender);
        var childElement = funcElement.RenderFunc(ctx);
        ctx.FlushEffects();
        var control = Mount(childElement, requestRerender);
        if (control is not null)
        {
            _componentNodes[control] = new ComponentNode
            {
                Context = ctx, RenderedElement = childElement, Element = funcElement,
            };
        }
        return control!;
    }

    private UIElement MountLazyStack(LazyStackElementBase lazy, Action requestRerender)
    {
        var repeater = new WinUI.ItemsRepeater();

        repeater.Layout = new WinUI.StackLayout
        {
            Orientation = lazy.Orientation,
            Spacing = lazy.Spacing,
        };

        repeater.ItemsSource = lazy.GetItemsSource();
        repeater.ItemTemplate = lazy.CreateFactory(this, requestRerender, _pool);
        SetElementTag(repeater, lazy);
        ApplySetters(lazy.RepeaterSetters, repeater);

        var sv = _pool.TryRent(typeof(WinUI.ScrollViewer)) as WinUI.ScrollViewer ?? new WinUI.ScrollViewer();
        sv.Content = repeater;
        sv.HorizontalScrollBarVisibility = lazy.Orientation == Microsoft.UI.Xaml.Controls.Orientation.Horizontal
            ? WinUI.ScrollBarVisibility.Auto
            : WinUI.ScrollBarVisibility.Disabled;
        sv.VerticalScrollBarVisibility = lazy.Orientation == Microsoft.UI.Xaml.Controls.Orientation.Vertical
            ? WinUI.ScrollBarVisibility.Auto
            : WinUI.ScrollBarVisibility.Disabled;
        SetElementTag(sv, lazy);
        ApplySetters(lazy.ScrollViewerSetters, sv);

        return sv;
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUI = Microsoft.UI.Xaml.Controls;
using WinPrim = Microsoft.UI.Xaml.Controls.Primitives;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace Duct.Core;

public sealed partial class Reconciler
{
    public UIElement? Mount(Element element, Action requestRerender)
    {
        // Unwrap legacy ModifiedElement (backward compat)
        ElementModifiers? modifiers = element.Modifiers;
        if (element is ModifiedElement mod)
        {
            modifiers = mod.WrappedModifiers;
            if (mod.Inner.Modifiers is not null)
                modifiers = modifiers.Merge(mod.Inner.Modifiers);
            element = mod.Inner;
        }

        UIElement? control;

        // Registered types checked first
        if (_typeRegistry.TryGetValue(element.GetType(), out var reg))
        {
            control = reg.Mount(element, requestRerender, this);
        }
        else
        {
        control = element switch
        {
            TextElement text => MountText(text),
            RichTextBlockElement richText => MountRichTextBlock(richText),
            ButtonElement btn => MountButton(btn),
            HyperlinkButtonElement hlBtn => MountHyperlinkButton(hlBtn),
            RepeatButtonElement repBtn => MountRepeatButton(repBtn),
            ToggleButtonElement togBtn => MountToggleButton(togBtn),
            DropDownButtonElement ddBtn => MountDropDownButton(ddBtn, requestRerender),
            SplitButtonElement spBtn => MountSplitButton(spBtn, requestRerender),
            ToggleSplitButtonElement tspBtn => MountToggleSplitButton(tspBtn, requestRerender),
            RichEditBoxElement reb => MountRichEditBox(reb),
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
            WrapGridElement wg => MountWrapGrid(wg, requestRerender),
            StackElement stack => MountStack(stack, requestRerender),
            GridElement grid => MountGrid(grid, requestRerender),
            ScrollViewElement scroll => MountScrollView(scroll, requestRerender),
            BorderElement border => MountBorder(border, requestRerender),
            ExpanderElement exp => MountExpander(exp, requestRerender),
            SplitViewElement sv => MountSplitView(sv, requestRerender),
            ViewboxElement vb => MountViewbox(vb, requestRerender),
            CanvasElement cvs => MountCanvas(cvs, requestRerender),
            FlexElement flex => MountFlex(flex, requestRerender),
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
            TemplatedListElementBase tl => MountTemplatedList(tl, requestRerender),
            LazyStackElementBase lazy => MountLazyStack(lazy, requestRerender),
            RectangleElement rect => MountRectangle(rect),
            EllipseElement ell => MountEllipse(ell),
            RelativePanelElement rp => MountRelativePanel(rp, requestRerender),
            MediaPlayerElementElement mpe => MountMediaPlayerElement(mpe),
            AnimatedVisualPlayerElement avp => MountAnimatedVisualPlayer(avp),
            SemanticZoomElement sz => MountSemanticZoom(sz, requestRerender),
            ListBoxElement lb => MountListBox(lb),
            SelectorBarElement sb => MountSelectorBar(sb),
            PipsPagerElement pp => MountPipsPager(pp),
            AnnotatedScrollBarElement asb => MountAnnotatedScrollBar(asb),
            PopupElement popup => MountPopup(popup, requestRerender),
            RefreshContainerElement rc => MountRefreshContainer(rc, requestRerender),
            CommandBarFlyoutElement cbf => MountCommandBarFlyout(cbf, requestRerender),
            CalendarViewElement cv => MountCalendarView(cv),
            ComponentElement comp => MountComponent(comp, requestRerender),
            FuncElement func => MountFuncComponent(func, requestRerender),
            _ => null,
        };
        }

        // Apply inline modifiers after mounting
        if (modifiers is not null && control is FrameworkElement fe)
            ApplyModifiers(fe, modifiers, requestRerender);

        return control;
    }

    private TextBlock MountText(TextElement text)
    {
        var tb = _pool.TryRent(typeof(TextBlock)) as TextBlock ?? new TextBlock();
        tb.Text = text.Content;
        if (text.FontSize.HasValue) tb.FontSize = text.FontSize.Value;
        if (text.Weight.HasValue) tb.FontWeight = text.Weight.Value;
        if (text.FontStyle.HasValue) tb.FontStyle = text.FontStyle.Value;
        if (text.HorizontalAlignment.HasValue) tb.HorizontalAlignment = text.HorizontalAlignment.Value;
        ApplySetters(text.Setters, tb);
        return tb;
    }

    private WinUI.RichTextBlock MountRichTextBlock(RichTextBlockElement richText)
    {
        var rtb = _pool.TryRent(typeof(WinUI.RichTextBlock)) as WinUI.RichTextBlock ?? new WinUI.RichTextBlock();
        rtb.IsTextSelectionEnabled = richText.IsTextSelectionEnabled;
        if (richText.Paragraphs is not null)
        {
            foreach (var para in richText.Paragraphs)
            {
                var p = new Microsoft.UI.Xaml.Documents.Paragraph();
                foreach (var inline in para.Inlines)
                {
                    switch (inline)
                    {
                        case RichTextRun run:
                            var r = new Microsoft.UI.Xaml.Documents.Run { Text = run.Text };
                            if (run.IsBold) r.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
                            if (run.IsItalic) r.FontStyle = Windows.UI.Text.FontStyle.Italic;
                            if (run.FontSize.HasValue) r.FontSize = run.FontSize.Value;
                            if (run.Foreground is not null) r.Foreground = run.Foreground;
                            p.Inlines.Add(r);
                            break;
                        case RichTextHyperlink link:
                            var hl = new Microsoft.UI.Xaml.Documents.Hyperlink { NavigateUri = link.NavigateUri };
                            hl.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = link.Text });
                            p.Inlines.Add(hl);
                            break;
                    }
                }
                rtb.Blocks.Add(p);
            }
        }
        else
        {
            var paragraph = new Microsoft.UI.Xaml.Documents.Paragraph();
            paragraph.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = richText.Text });
            rtb.Blocks.Add(paragraph);
        }
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
            ddb.Flyout = CreateFlyoutFromElement(ddBtn.Flyout, requestRerender);
        ApplySetters(ddBtn.Setters, ddb);
        return ddb;
    }

    private WinUI.SplitButton MountSplitButton(SplitButtonElement spBtn, Action requestRerender)
    {
        var sb = new WinUI.SplitButton { Content = spBtn.Label };
        SetElementTag(sb, spBtn);
        sb.Click += (s, _) => (GetElementTag((UIElement)s!) as SplitButtonElement)?.OnClick?.Invoke();
        if (spBtn.Flyout is not null)
            sb.Flyout = CreateFlyoutFromElement(spBtn.Flyout, requestRerender);
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
            tsb.Flyout = CreateFlyoutFromElement(tspBtn.Flyout, requestRerender);
        ApplySetters(tspBtn.Setters, tsb);
        return tsb;
    }

    private TextBox MountTextField(TextFieldElement tf)
    {
        var textBox = new TextBox { Text = tf.Value, PlaceholderText = tf.Placeholder ?? "" };
        if (tf.Header is not null) textBox.Header = tf.Header;
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
        var checkBox = new WinUI.CheckBox { Content = cb.Label };
        if (cb.IsThreeState)
        {
            checkBox.IsThreeState = true;
            checkBox.IsChecked = cb.CheckedState;
        }
        else
        {
            checkBox.IsChecked = cb.IsChecked;
        }
        SetElementTag(checkBox, cb);
        checkBox.Checked += (s, _) =>
        {
            var el = GetElementTag((UIElement)s!) as CheckBoxElement;
            el?.OnChanged?.Invoke(true);
            el?.OnCheckedStateChanged?.Invoke(true);
        };
        checkBox.Unchecked += (s, _) =>
        {
            var el = GetElementTag((UIElement)s!) as CheckBoxElement;
            el?.OnChanged?.Invoke(false);
            el?.OnCheckedStateChanged?.Invoke(false);
        };
        checkBox.Indeterminate += (s, _) =>
        {
            var el = GetElementTag((UIElement)s!) as CheckBoxElement;
            el?.OnCheckedStateChanged?.Invoke(null);
        };
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

    private WinUI.RichEditBox MountRichEditBox(RichEditBoxElement reb)
    {
        var box = new WinUI.RichEditBox { IsReadOnly = reb.IsReadOnly };
        if (reb.Header is not null) box.Header = reb.Header;
        if (reb.PlaceholderText is not null) box.PlaceholderText = reb.PlaceholderText;
        if (!string.IsNullOrEmpty(reb.Text))
            box.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, reb.Text);
        SetElementTag(box, reb);
        box.TextChanged += (s, _) =>
        {
            var r = (WinUI.RichEditBox)s!;
            r.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out var text);
            (GetElementTag(r) as RichEditBoxElement)?.OnTextChanged?.Invoke(text?.TrimEnd('\r') ?? "");
        };
        ApplySetters(reb.Setters, box);
        return box;
    }

    private WinUI.VariableSizedWrapGrid MountWrapGrid(WrapGridElement wg, Action requestRerender)
    {
        var grid = new WinUI.VariableSizedWrapGrid { Orientation = wg.Orientation };
        if (wg.MaximumRowsOrColumns >= 0) grid.MaximumRowsOrColumns = wg.MaximumRowsOrColumns;
        if (!double.IsNaN(wg.ItemWidth)) grid.ItemWidth = wg.ItemWidth;
        if (!double.IsNaN(wg.ItemHeight)) grid.ItemHeight = wg.ItemHeight;
        foreach (var child in wg.Children)
        {
            if (child is null or EmptyElement) continue;
            var childControl = Mount(child, requestRerender);
            if (childControl is not null) grid.Children.Add(childControl);
        }
        SetElementTag(grid, wg);
        ApplySetters(wg.Setters, grid);
        return grid;
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
            var ctrl = Mount(child, requestRerender);
            if (ctrl is null) continue;
            var ga = child.GetAttached<GridAttached>();
            if (ga is not null && ctrl is FrameworkElement fe)
            {
                WinUI.Grid.SetRow(fe, ga.Row);
                WinUI.Grid.SetColumn(fe, ga.Column);
                if (ga.RowSpan > 1) WinUI.Grid.SetRowSpan(fe, ga.RowSpan);
                if (ga.ColumnSpan > 1) WinUI.Grid.SetColumnSpan(fe, ga.ColumnSpan);
            }
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
        sv.HorizontalScrollMode = (WinUI.ScrollMode)scroll.HorizontalScrollMode;
        sv.VerticalScrollMode = (WinUI.ScrollMode)scroll.VerticalScrollMode;
        sv.ZoomMode = (WinUI.ZoomMode)scroll.ZoomMode;
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
            var ctrl = Mount(child, requestRerender);
            if (ctrl is null) continue;
            var ca = child.GetAttached<CanvasAttached>();
            if (ca is not null && ctrl is FrameworkElement fe)
            {
                WinUI.Canvas.SetLeft(fe, ca.Left);
                WinUI.Canvas.SetTop(fe, ca.Top);
            }
            canvas.Children.Add(ctrl);
        }
        SetElementTag(canvas, cvs);
        ApplySetters(cvs.Setters, canvas);
        return canvas;
    }

    private Flex.FlexPanel MountFlex(FlexElement flex, Action requestRerender)
    {
        var panel = _pool.TryRent(typeof(Flex.FlexPanel)) as Flex.FlexPanel ?? new Flex.FlexPanel();
        panel.Direction = flex.Direction;
        panel.JustifyContent = flex.JustifyContent;
        panel.AlignItems = flex.AlignItems;
        panel.AlignContent = flex.AlignContent;
        panel.Wrap = flex.Wrap;
        panel.ColumnGap = flex.ColumnGap;
        panel.RowGap = flex.RowGap;
        foreach (var child in flex.Children)
        {
            var ctrl = Mount(child, requestRerender);
            if (ctrl is null) continue;
            var fa = child.GetAttached<FlexAttached>();
            if (fa is not null)
            {
                Flex.FlexPanel.SetGrow(ctrl, fa.Grow);
                Flex.FlexPanel.SetShrink(ctrl, fa.Shrink);
                if (fa.Basis.HasValue) Flex.FlexPanel.SetBasis(ctrl, fa.Basis.Value);
                if (fa.AlignSelf.HasValue) Flex.FlexPanel.SetAlignSelf(ctrl, fa.AlignSelf.Value);
                Flex.FlexPanel.SetPosition(ctrl, fa.Position);
                if (fa.Left.HasValue) Flex.FlexPanel.SetLeft(ctrl, fa.Left.Value);
                if (fa.Top.HasValue) Flex.FlexPanel.SetTop(ctrl, fa.Top.Value);
                if (fa.Right.HasValue) Flex.FlexPanel.SetRight(ctrl, fa.Right.Value);
                if (fa.Bottom.HasValue) Flex.FlexPanel.SetBottom(ctrl, fa.Bottom.Value);
            }
            panel.Children.Add(ctrl);
        }
        SetElementTag(panel, flex);
        ApplySetters(flex.Setters, panel);
        return panel;
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
        listView.ItemTemplate = SharedContentControlTemplate.Value;

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

        gridView.ItemTemplate = SharedContentControlTemplate.Value;

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
            CanDragItems = tv.CanDragItems,
            AllowDrop = tv.AllowDrop,
            CanReorderItems = tv.CanReorderItems,
        };

        // Check if any node uses ContentElement for custom rendering
        bool hasContentElements = HasAnyContentElement(tv.Nodes);

        if (hasContentElements)
        {
            // Use ContentControl template so pre-mounted Elements display in the tree
            treeView.ItemTemplate = SharedContentControlTemplate.Value;
        }
        else
        {
            // Default: text-binding template for efficiency
            // In node-mode, DataContext of the template = TreeViewNode,
            // so {Binding Content.Content} resolves TreeViewNode.Content (TreeViewNodeData) → .Content (string).
            treeView.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
                "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
                "<TextBlock Text='{Binding Content.Content}'/>" +
                "</DataTemplate>");
        }

        foreach (var node in tv.Nodes)
            treeView.RootNodes.Add(CreateTreeNode(node, hasContentElements, requestRerender));

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

    private static bool HasAnyContentElement(TreeViewNodeData[] nodes)
    {
        foreach (var node in nodes)
        {
            if (node.ContentElement is not null) return true;
            if (node.Children is not null && HasAnyContentElement(node.Children)) return true;
        }
        return false;
    }

    private WinUI.TreeViewNode CreateTreeNode(TreeViewNodeData data, bool mountElements, Action requestRerender)
    {
        var node = new WinUI.TreeViewNode { IsExpanded = data.IsExpanded };

        if (mountElements && data.ContentElement is not null)
        {
            // Mount the Element and store the UIElement as Content.
            // The ContentControl template will display it via ContentPresenter.
            var ctrl = Mount(data.ContentElement, requestRerender);
            node.Content = ctrl;
        }
        else
        {
            node.Content = data;
        }

        if (data.Children is not null)
            foreach (var child in data.Children)
                node.Children.Add(CreateTreeNode(child, mountElements, requestRerender));
        return node;
    }

    /// <summary>Backward-compatible overload for non-ContentElement code paths.</summary>
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

    private UIElement MountTemplatedList(TemplatedListElementBase el, Action requestRerender)
    {
        return el.ControlKind switch
        {
            TemplatedControlKind.ListView => MountTemplatedListView(el, requestRerender),
            TemplatedControlKind.GridView => MountTemplatedGridView(el, requestRerender),
            TemplatedControlKind.FlipView => MountTemplatedFlipView(el, requestRerender),
            _ => throw new InvalidOperationException($"Unknown TemplatedControlKind: {el.ControlKind}")
        };
    }

    /// <summary>
    /// Shared ContainerContentChanging handler for all templated items controls.
    /// On materialize: calls viewBuilder, mounts element, stores in ContentControl.
    /// On recycle: unmounts child, clears content.
    /// </summary>
    private void HandleTemplatedContainerContentChanging(object sender, ContainerContentChangingEventArgs args, Action requestRerender)
    {
        if (args.InRecycleQueue)
        {
            if (args.ItemContainer.ContentTemplateRoot is ContentControl oldCc)
            {
                if (oldCc.Content is UIElement oldCtrl)
                    UnmountChild(oldCtrl);
                oldCc.Content = null;
                oldCc.Tag = null;
            }
            return;
        }

        args.Handled = true;
        var currentEl = GetElementTag((UIElement)sender!) as TemplatedListElementBase;
        if (currentEl is not null && args.ItemIndex >= 0 && args.ItemIndex < currentEl.ItemCount
            && args.ItemContainer.ContentTemplateRoot is ContentControl cc)
        {
            var itemElement = currentEl.BuildItemView(args.ItemIndex);
            var ctrl = Mount(itemElement, requestRerender);
            cc.Content = ctrl;
            cc.Tag = itemElement; // Store for later reconciliation
        }
    }

    private WinUI.ListView MountTemplatedListView(TemplatedListElementBase el, Action requestRerender)
    {
        var listView = new WinUI.ListView
        {
            SelectionMode = el.GetSelectionMode(),
            IsItemClickEnabled = el.GetIsItemClickEnabled(),
        };
        var header = el.GetHeader();
        if (header is not null) listView.Header = header;

        SetElementTag(listView, el);
        listView.ItemTemplate = SharedContentControlTemplate.Value;

        listView.ContainerContentChanging += (sender, args) =>
            HandleTemplatedContainerContentChanging(sender, args, requestRerender);

        listView.SelectionChanged += (s, _) =>
        {
            var l = (WinUI.ListView)s!;
            (GetElementTag(l) as TemplatedListElementBase)?.InvokeSelectionChanged(l.SelectedIndex);
        };
        listView.ItemClick += (s, args) =>
        {
            var l = (WinUI.ListView)s!;
            if (args.ClickedItem is int idx)
                (GetElementTag(l) as TemplatedListElementBase)?.InvokeItemClick(idx);
        };

        listView.ItemsSource = Enumerable.Range(0, el.ItemCount).ToList();

        var selectedIndex = el.GetSelectedIndex();
        if (selectedIndex >= 0) listView.SelectedIndex = selectedIndex;
        el.ApplyControlSetters(listView);
        return listView;
    }

    private WinUI.GridView MountTemplatedGridView(TemplatedListElementBase el, Action requestRerender)
    {
        var gridView = new WinUI.GridView
        {
            SelectionMode = el.GetSelectionMode(),
            IsItemClickEnabled = el.GetIsItemClickEnabled(),
        };
        var header = el.GetHeader();
        if (header is not null) gridView.Header = header;

        SetElementTag(gridView, el);
        gridView.ItemTemplate = SharedContentControlTemplate.Value;

        gridView.ContainerContentChanging += (sender, args) =>
            HandleTemplatedContainerContentChanging(sender, args, requestRerender);

        gridView.SelectionChanged += (s, _) =>
        {
            var g = (WinUI.GridView)s!;
            (GetElementTag(g) as TemplatedListElementBase)?.InvokeSelectionChanged(g.SelectedIndex);
        };
        gridView.ItemClick += (s, args) =>
        {
            var g = (WinUI.GridView)s!;
            if (args.ClickedItem is int idx)
                (GetElementTag(g) as TemplatedListElementBase)?.InvokeItemClick(idx);
        };

        gridView.ItemsSource = Enumerable.Range(0, el.ItemCount).ToList();

        var selectedIndex = el.GetSelectedIndex();
        if (selectedIndex >= 0) gridView.SelectedIndex = selectedIndex;
        el.ApplyControlSetters(gridView);
        return gridView;
    }

    private WinUI.FlipView MountTemplatedFlipView(TemplatedListElementBase el, Action requestRerender)
    {
        var flipView = new WinUI.FlipView();

        SetElementTag(flipView, el);

        // FlipView doesn't support ContainerContentChanging (not a ListViewBase).
        // Pre-mount all items — FlipView typically has few items so this is fine.
        for (int i = 0; i < el.ItemCount; i++)
        {
            var itemElement = el.BuildItemView(i);
            var ctrl = Mount(itemElement, requestRerender);
            if (ctrl is not null) flipView.Items.Add(ctrl);
        }

        flipView.SelectionChanged += (s, _) =>
        {
            var f = (WinUI.FlipView)s!;
            (GetElementTag(f) as TemplatedListElementBase)?.InvokeSelectionChanged(f.SelectedIndex);
        };

        var selectedIndex = el.GetSelectedIndex();
        if (selectedIndex >= 0) flipView.SelectedIndex = selectedIndex;
        el.ApplyControlSetters(flipView);
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
            foreach (var cmd in cmdEl.PrimaryCommands) commandBar.PrimaryCommands.Add(CreateAppBarItem(cmd));
        if (cmdEl.SecondaryCommands is not null)
            foreach (var cmd in cmdEl.SecondaryCommands) commandBar.SecondaryCommands.Add(CreateAppBarItem(cmd));
        SetElementTag(commandBar, cmdEl);
        ApplySetters(cmdEl.Setters, commandBar);
        return commandBar;
    }

    private static WinUI.ICommandBarElement CreateAppBarItem(AppBarItemBase item)
    {
        switch (item)
        {
            case AppBarButtonData cmd:
            {
                var abb = new WinUI.AppBarButton { Label = cmd.Label };
                abb.Icon = ResolveIcon(cmd.IconElement, cmd.Icon);
                if (cmd.KeyboardAccelerators is not null)
                    foreach (var ka in cmd.KeyboardAccelerators)
                        abb.KeyboardAccelerators.Add(new Microsoft.UI.Xaml.Input.KeyboardAccelerator { Key = ka.Key, Modifiers = ka.Modifiers });
                abb.Tag = cmd;
                abb.Click += (s, _) => ((AppBarButtonData)((WinUI.AppBarButton)s!).Tag!).OnClick?.Invoke();
                return abb;
            }
            case AppBarToggleButtonData toggle:
            {
                var atb = new WinUI.AppBarToggleButton { Label = toggle.Label, IsChecked = toggle.IsChecked };
                atb.Icon = ResolveIcon(toggle.IconElement, toggle.Icon);
                atb.Tag = toggle;
                atb.Checked += (s, _) => ((AppBarToggleButtonData)((WinUI.AppBarToggleButton)s!).Tag!).OnToggled?.Invoke(true);
                atb.Unchecked += (s, _) => ((AppBarToggleButtonData)((WinUI.AppBarToggleButton)s!).Tag!).OnToggled?.Invoke(false);
                return atb;
            }
            case AppBarSeparatorData:
                return new WinUI.AppBarSeparator();
            default:
                return new WinUI.AppBarSeparator();
        }
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
                flyoutItem.Icon = ResolveIcon(mfi.IconElement, mfi.Icon);
                if (mfi.KeyboardAccelerators is not null)
                    foreach (var ka in mfi.KeyboardAccelerators)
                        flyoutItem.KeyboardAccelerators.Add(new Microsoft.UI.Xaml.Input.KeyboardAccelerator { Key = ka.Key, Modifiers = ka.Modifiers });
                flyoutItem.Tag = mfi;
                flyoutItem.Click += (s, _) => ((MenuFlyoutItemData)((WinUI.MenuFlyoutItem)s!).Tag!).OnClick?.Invoke();
                return flyoutItem;
            }
            case ToggleMenuFlyoutItemData toggle:
            {
                var toggleItem = new WinUI.ToggleMenuFlyoutItem { Text = toggle.Text, IsChecked = toggle.IsChecked };
                toggleItem.Icon = ResolveIcon(toggle.IconElement, toggle.Icon);
                toggleItem.Tag = toggle;
                toggleItem.Click += (s, _) =>
                {
                    var ti = (WinUI.ToggleMenuFlyoutItem)s!;
                    ((ToggleMenuFlyoutItemData)ti.Tag!).OnToggled?.Invoke(ti.IsChecked);
                };
                return toggleItem;
            }
            case RadioMenuFlyoutItemData radio:
            {
                var radioItem = new WinUI.RadioMenuFlyoutItem { Text = radio.Text, GroupName = radio.GroupName, IsChecked = radio.IsChecked };
                radioItem.Tag = radio;
                radioItem.Click += (s, _) => ((RadioMenuFlyoutItemData)((WinUI.RadioMenuFlyoutItem)s!).Tag!).OnClick?.Invoke();
                return radioItem;
            }
            case MenuFlyoutSeparatorData:
                return new WinUI.MenuFlyoutSeparator();
            case MenuFlyoutSubItemData sub:
            {
                var subItem = new WinUI.MenuFlyoutSubItem { Text = sub.Text };
                subItem.Icon = ResolveIcon(sub.IconElement, sub.Icon);
                foreach (var child in sub.Items) subItem.Items.Add(CreateMenuFlyoutItem(child));
                return subItem;
            }
            default:
                return new WinUI.MenuFlyoutSeparator();
        }
    }

    private static WinUI.IconElement? ResolveIcon(IconData? iconData, string? iconSymbol)
    {
        if (iconData is not null)
        {
            return iconData switch
            {
                SymbolIconData sym => new WinUI.SymbolIcon(ParseSymbol(sym.Symbol)),
                FontIconData fi => CreateFontIcon(fi),
                BitmapIconData bi => new WinUI.BitmapIcon { UriSource = bi.Source, ShowAsMonochrome = bi.ShowAsMonochrome },
                PathIconData pi => CreatePathIcon(pi),
                ImageIconData ii => new WinUI.ImageIcon { Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(ii.Source) },
                _ => null,
            };
        }
        if (iconSymbol is not null) return new WinUI.SymbolIcon(ParseSymbol(iconSymbol));
        return null;
    }

    private static WinUI.FontIcon CreateFontIcon(FontIconData fi)
    {
        var icon = new WinUI.FontIcon { Glyph = fi.Glyph };
        if (fi.FontFamily is not null) icon.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily(fi.FontFamily);
        if (fi.FontSize.HasValue) icon.FontSize = fi.FontSize.Value;
        return icon;
    }

    private static WinUI.PathIcon CreatePathIcon(PathIconData pi)
    {
        var icon = new WinUI.PathIcon();
        if (Microsoft.UI.Xaml.Markup.XamlReader.Load(
            $"<Geometry xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>{pi.Data}</Geometry>")
            is Microsoft.UI.Xaml.Media.Geometry geo)
        {
            icon.Data = geo;
        }
        return icon;
    }

    private UIElement MountComponent(ComponentElement compElement, Action requestRerender)
    {
        var component = compElement.CreateInstance();

        if (compElement.Props is not null && component is IPropsReceiver receiver)
            receiver.SetProps(compElement.Props);

        component.Context.BeginRender(requestRerender);
        var childElement = component.Render();
        component.Context.FlushEffects();
        var childControl = Mount(childElement, requestRerender);

        // Each component gets its own Border wrapper as an identity anchor
        // in _componentNodes, preventing key collisions when components nest.
        var wrapper = new Border { Child = childControl };
        _componentNodes[wrapper] = new ComponentNode
        {
            Component = component, RenderedElement = childElement, Element = compElement,
        };
        return wrapper;
    }

    private UIElement MountFuncComponent(FuncElement funcElement, Action requestRerender)
    {
        var ctx = new RenderContext();
        ctx.BeginRender(requestRerender);
        var childElement = funcElement.RenderFunc(ctx);
        ctx.FlushEffects();
        var childControl = Mount(childElement, requestRerender);

        var wrapper = new Border { Child = childControl };
        _componentNodes[wrapper] = new ComponentNode
        {
            Context = ctx, RenderedElement = childElement, Element = funcElement,
        };
        return wrapper;
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

    // ── Shape elements ──────────────────────────────────────────────────

    private WinShapes.Rectangle MountRectangle(RectangleElement rect)
    {
        var r = new WinShapes.Rectangle();
        if (rect.Fill is not null) r.Fill = rect.Fill;
        if (rect.Stroke is not null) r.Stroke = rect.Stroke;
        if (rect.StrokeThickness > 0) r.StrokeThickness = rect.StrokeThickness;
        if (rect.RadiusX > 0) r.RadiusX = rect.RadiusX;
        if (rect.RadiusY > 0) r.RadiusY = rect.RadiusY;
        ApplySetters(rect.Setters, r);
        return r;
    }

    private WinShapes.Ellipse MountEllipse(EllipseElement ell)
    {
        var e = new WinShapes.Ellipse();
        if (ell.Fill is not null) e.Fill = ell.Fill;
        if (ell.Stroke is not null) e.Stroke = ell.Stroke;
        if (ell.StrokeThickness > 0) e.StrokeThickness = ell.StrokeThickness;
        ApplySetters(ell.Setters, e);
        return e;
    }

    // ── RelativePanel ───────────────────────────────────────────────────

    private WinUI.RelativePanel MountRelativePanel(RelativePanelElement rp, Action requestRerender)
    {
        var panel = new WinUI.RelativePanel();
        var nameMap = new Dictionary<string, UIElement>();

        // First pass: mount all children and register names
        foreach (var child in rp.Children)
        {
            var ctrl = Mount(child, requestRerender);
            if (ctrl is null) continue;
            var rpa = child.GetAttached<RelativePanelAttached>();
            if (rpa is not null && ctrl is FrameworkElement fe) fe.Name = rpa.Name;
            if (rpa is not null) nameMap[rpa.Name] = ctrl;
            panel.Children.Add(ctrl);
        }

        // Second pass: apply attached properties using name references
        foreach (var child in rp.Children)
        {
            var rpa = child.GetAttached<RelativePanelAttached>();
            if (rpa is null) continue;
            if (!nameMap.TryGetValue(rpa.Name, out var ctrl)) continue;

            if (rpa.RightOf is not null && nameMap.TryGetValue(rpa.RightOf, out var rightOf))
                WinUI.RelativePanel.SetRightOf(ctrl, rightOf);
            if (rpa.Below is not null && nameMap.TryGetValue(rpa.Below, out var below))
                WinUI.RelativePanel.SetBelow(ctrl, below);
            if (rpa.LeftOf is not null && nameMap.TryGetValue(rpa.LeftOf, out var leftOf))
                WinUI.RelativePanel.SetLeftOf(ctrl, leftOf);
            if (rpa.Above is not null && nameMap.TryGetValue(rpa.Above, out var above))
                WinUI.RelativePanel.SetAbove(ctrl, above);
            if (rpa.AlignLeftWith is not null && nameMap.TryGetValue(rpa.AlignLeftWith, out var alw))
                WinUI.RelativePanel.SetAlignLeftWith(ctrl, alw);
            if (rpa.AlignRightWith is not null && nameMap.TryGetValue(rpa.AlignRightWith, out var arw))
                WinUI.RelativePanel.SetAlignRightWith(ctrl, arw);
            if (rpa.AlignTopWith is not null && nameMap.TryGetValue(rpa.AlignTopWith, out var atw))
                WinUI.RelativePanel.SetAlignTopWith(ctrl, atw);
            if (rpa.AlignBottomWith is not null && nameMap.TryGetValue(rpa.AlignBottomWith, out var abw))
                WinUI.RelativePanel.SetAlignBottomWith(ctrl, abw);
            if (rpa.AlignHorizontalCenterWith is not null && nameMap.TryGetValue(rpa.AlignHorizontalCenterWith, out var ahcw))
                WinUI.RelativePanel.SetAlignHorizontalCenterWith(ctrl, ahcw);
            if (rpa.AlignVerticalCenterWith is not null && nameMap.TryGetValue(rpa.AlignVerticalCenterWith, out var avcw))
                WinUI.RelativePanel.SetAlignVerticalCenterWith(ctrl, avcw);

            if (rpa.AlignLeftWithPanel) WinUI.RelativePanel.SetAlignLeftWithPanel(ctrl, true);
            if (rpa.AlignRightWithPanel) WinUI.RelativePanel.SetAlignRightWithPanel(ctrl, true);
            if (rpa.AlignTopWithPanel) WinUI.RelativePanel.SetAlignTopWithPanel(ctrl, true);
            if (rpa.AlignBottomWithPanel) WinUI.RelativePanel.SetAlignBottomWithPanel(ctrl, true);
            if (rpa.AlignHorizontalCenterWithPanel) WinUI.RelativePanel.SetAlignHorizontalCenterWithPanel(ctrl, true);
            if (rpa.AlignVerticalCenterWithPanel) WinUI.RelativePanel.SetAlignVerticalCenterWithPanel(ctrl, true);
        }

        SetElementTag(panel, rp);
        ApplySetters(rp.Setters, panel);
        return panel;
    }

    // ── MediaPlayerElement ──────────────────────────────────────────────

    private WinUI.MediaPlayerElement MountMediaPlayerElement(MediaPlayerElementElement mpe)
    {
        var player = new WinUI.MediaPlayerElement
        {
            AreTransportControlsEnabled = mpe.AreTransportControlsEnabled,
            AutoPlay = mpe.AutoPlay,
        };
        if (mpe.Source is not null)
            player.Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(mpe.Source, UriKind.RelativeOrAbsolute));
        SetElementTag(player, mpe);
        ApplySetters(mpe.Setters, player);
        return player;
    }

    // ── AnimatedVisualPlayer ────────────────────────────────────────────

    private WinUI.AnimatedVisualPlayer MountAnimatedVisualPlayer(AnimatedVisualPlayerElement avp)
    {
        var player = new WinUI.AnimatedVisualPlayer { AutoPlay = avp.AutoPlay };
        SetElementTag(player, avp);
        ApplySetters(avp.Setters, player);
        return player;
    }

    // ── SemanticZoom ────────────────────────────────────────────────────

    private WinUI.SemanticZoom MountSemanticZoom(SemanticZoomElement sz, Action requestRerender)
    {
        var zoomedIn = Mount(sz.ZoomedInView, requestRerender);
        var zoomedOut = Mount(sz.ZoomedOutView, requestRerender);
        var semanticZoom = new WinUI.SemanticZoom();
        if (zoomedIn is ISemanticZoomInformation szi) semanticZoom.ZoomedInView = szi;
        if (zoomedOut is ISemanticZoomInformation szo) semanticZoom.ZoomedOutView = szo;
        SetElementTag(semanticZoom, sz);
        ApplySetters(sz.Setters, semanticZoom);
        return semanticZoom;
    }

    // ── ListBox ─────────────────────────────────────────────────────────

    private WinUI.ListBox MountListBox(ListBoxElement lb)
    {
        var listBox = new WinUI.ListBox { SelectedIndex = lb.SelectedIndex };
        foreach (var item in lb.Items) listBox.Items.Add(item);
        SetElementTag(listBox, lb);
        listBox.SelectionChanged += (s, _) =>
        {
            var l = (WinUI.ListBox)s!;
            (GetElementTag(l) as ListBoxElement)?.OnSelectionChanged?.Invoke(l.SelectedIndex);
        };
        ApplySetters(lb.Setters, listBox);
        return listBox;
    }

    // ── SelectorBar ─────────────────────────────────────────────────────

    private WinUI.SelectorBar MountSelectorBar(SelectorBarElement sb)
    {
        var selectorBar = new WinUI.SelectorBar();
        foreach (var item in sb.Items)
        {
            var sbi = new WinUI.SelectorBarItem { Text = item.Text };
            if (item.Icon is not null) sbi.Icon = new WinUI.SymbolIcon(ParseSymbol(item.Icon));
            selectorBar.Items.Add(sbi);
        }
        if (sb.SelectedIndex >= 0 && sb.SelectedIndex < selectorBar.Items.Count)
            selectorBar.SelectedItem = selectorBar.Items[sb.SelectedIndex];
        SetElementTag(selectorBar, sb);
        selectorBar.SelectionChanged += (s, _) =>
        {
            var bar = (WinUI.SelectorBar)s!;
            var idx = bar.Items.IndexOf(bar.SelectedItem);
            (GetElementTag(bar) as SelectorBarElement)?.OnSelectionChanged?.Invoke(idx);
        };
        ApplySetters(sb.Setters, selectorBar);
        return selectorBar;
    }

    // ── PipsPager ───────────────────────────────────────────────────────

    private WinUI.PipsPager MountPipsPager(PipsPagerElement pp)
    {
        var pager = new WinUI.PipsPager
        {
            NumberOfPages = pp.NumberOfPages,
            SelectedPageIndex = pp.SelectedPageIndex,
        };
        SetElementTag(pager, pp);
        pager.SelectedIndexChanged += (s, _) =>
        {
            var p = (WinUI.PipsPager)s!;
            (GetElementTag(p) as PipsPagerElement)?.OnSelectedIndexChanged?.Invoke(p.SelectedPageIndex);
        };
        ApplySetters(pp.Setters, pager);
        return pager;
    }

    // ── AnnotatedScrollBar ──────────────────────────────────────────────

    private WinUI.AnnotatedScrollBar MountAnnotatedScrollBar(AnnotatedScrollBarElement asb)
    {
        var scrollBar = new WinUI.AnnotatedScrollBar();
        ApplySetters(asb.Setters, scrollBar);
        return scrollBar;
    }

    // ── Popup ───────────────────────────────────────────────────────────

    private UIElement MountPopup(PopupElement popup, Action requestRerender)
    {
        // Popup is not a UIElement child, so we wrap it in a StackPanel
        var wrapper = new WinUI.StackPanel();
        var p = new WinPrim.Popup
        {
            IsOpen = popup.IsOpen,
            IsLightDismissEnabled = popup.IsLightDismissEnabled,
            HorizontalOffset = popup.HorizontalOffset,
            VerticalOffset = popup.VerticalOffset,
        };
        var child = Mount(popup.Child, requestRerender);
        p.Child = child as UIElement;
        SetElementTag(wrapper, popup);
        p.Closed += (s, _) => (GetElementTag(wrapper) as PopupElement)?.OnClosed?.Invoke();
        ApplySetters(popup.Setters, p);
        wrapper.Children.Add(p);
        return wrapper;
    }

    // ── RefreshContainer ────────────────────────────────────────────────

    private WinUI.RefreshContainer MountRefreshContainer(RefreshContainerElement rc, Action requestRerender)
    {
        var container = new WinUI.RefreshContainer();
        container.Content = Mount(rc.Content, requestRerender);
        SetElementTag(container, rc);
        container.RefreshRequested += (s, _) =>
            (GetElementTag((UIElement)s!) as RefreshContainerElement)?.OnRefreshRequested?.Invoke();
        ApplySetters(rc.Setters, container);
        return container;
    }

    // ── CommandBarFlyout ────────────────────────────────────────────────

    private UIElement? MountCommandBarFlyout(CommandBarFlyoutElement cbf, Action requestRerender)
    {
        var target = Mount(cbf.Target, requestRerender);
        if (target is FrameworkElement targetFe)
        {
            var flyout = new WinUI.CommandBarFlyout { Placement = cbf.Placement };
            if (cbf.PrimaryCommands is not null)
                foreach (var cmd in cbf.PrimaryCommands) flyout.PrimaryCommands.Add(CreateAppBarItem(cmd));
            if (cbf.SecondaryCommands is not null)
                foreach (var cmd in cbf.SecondaryCommands) flyout.SecondaryCommands.Add(CreateAppBarItem(cmd));
            SetElementTag(targetFe, cbf);
            WinPrim.FlyoutBase.SetAttachedFlyout(targetFe, flyout);
            ApplySetters(cbf.Setters, flyout);
        }
        return target;
    }

    // ── CalendarView ────────────────────────────────────────────────────

    private WinUI.CalendarView MountCalendarView(CalendarViewElement cv)
    {
        var calendarView = new WinUI.CalendarView
        {
            SelectionMode = cv.SelectionMode,
            IsGroupLabelVisible = cv.IsGroupLabelVisible,
            IsOutOfScopeEnabled = cv.IsOutOfScopeEnabled,
        };
        if (cv.CalendarIdentifier is not null) calendarView.CalendarIdentifier = cv.CalendarIdentifier;
        if (cv.Language is not null && Windows.Globalization.Language.IsWellFormed(cv.Language))
            calendarView.Language = cv.Language;
        ApplySetters(cv.Setters, calendarView);
        return calendarView;
    }
}

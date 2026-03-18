using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Duct.Core;

/// <summary>
/// Serializes Duct's Element tree into flat ViewNode[]/ViewProp[] arrays
/// for the Rust differ. BFS traversal ensures children are contiguous.
/// </summary>
public sealed class TreeSerializer
{
    private readonly PropValueRegistry _registry;

    public TreeSerializer(PropValueRegistry registry)
    {
        _registry = registry;
    }

    public (ViewNode[] Nodes, ViewProp[] Props) Serialize(Element root)
    {
        _registry.Clear();

        var nodes = new List<ViewNode>();
        var props = new List<ViewProp>();
        var queue = new Queue<(Element Element, int ParentIndex)>();

        // Unwrap root
        var unwrapped = Unwrap(root);
        if (unwrapped is null) return ([], []);

        queue.Enqueue((unwrapped, -1));

        while (queue.Count > 0)
        {
            var (element, parentIndex) = queue.Dequeue();
            int nodeIndex = nodes.Count;

            var elementProps = SerializeProps(element);
            var children = GetChildren(element);

            var node = new ViewNode
            {
                TypeId = ViewDiffer.HashString(element.GetType().Name),
                Key = element.Key is not null ? (long)ViewDiffer.HashString(element.Key) : 0L,
                ParentIndex = parentIndex,
                PropCount = (ushort)elementProps.Length,
                ChildCount = (ushort)children.Length,
                FirstChild = 0, // patched below
                FirstProp = (uint)props.Count,
            };

            nodes.Add(node);
            props.AddRange(elementProps);

            // Enqueue children; FirstChild will be the next node index after all current queue items
            // We patch FirstChild after we know the layout
            foreach (var child in children)
            {
                var unwrappedChild = Unwrap(child);
                if (unwrappedChild is not null)
                    queue.Enqueue((unwrappedChild, nodeIndex));
            }
        }

        // Patch FirstChild: for each node, find its first child by scanning
        PatchFirstChild(nodes);

        return (nodes.ToArray(), props.ToArray());
    }

    private static void PatchFirstChild(List<ViewNode> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node.ChildCount == 0) continue;

            // Find first node whose ParentIndex == i
            for (int j = i + 1; j < nodes.Count; j++)
            {
                if (nodes[j].ParentIndex == i)
                {
                    node.FirstChild = (uint)j;
                    nodes[i] = node;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Unwraps ModifiedElement chains and skips EmptyElement.
    /// Returns the inner concrete element, or null if empty.
    /// </summary>
    internal static Element? Unwrap(Element? element)
    {
        while (element is ModifiedElement mod)
            element = mod.Inner;

        if (element is null or EmptyElement) return null;
        return element;
    }

    /// <summary>
    /// Collects modifiers from the entire ModifiedElement chain.
    /// </summary>
    internal static ElementModifiers? CollectModifiers(Element element)
    {
        ElementModifiers? result = null;
        while (element is ModifiedElement mod)
        {
            result = result is null ? mod.Modifiers : result.Merge(mod.Modifiers);
            element = mod.Inner;
        }
        return result;
    }

    internal ViewProp[] SerializeProps(Element element)
    {
        var propList = new List<ViewProp>();

        switch (element)
        {
            case TextElement text:
                AddStringProp(propList, "Content", text.Content);
                if (text.FontSize.HasValue) AddDoubleProp(propList, "FontSize", text.FontSize.Value);
                if (text.Weight.HasValue) AddHashProp(propList, "FontWeight", (ulong)text.Weight.Value.Weight);
                if (text.HorizontalAlignment.HasValue) AddEnumProp(propList, "HorizontalAlignment", (int)text.HorizontalAlignment.Value);
                break;

            case RichTextBlockElement richText:
                AddStringProp(propList, "Text", richText.Text);
                if (richText.FontSize.HasValue) AddDoubleProp(propList, "FontSize", richText.FontSize.Value);
                break;

            case ButtonElement btn:
                AddStringProp(propList, "Label", btn.Label);
                AddBoolProp(propList, "IsEnabled", btn.IsEnabled);
                if (btn.OnClick is not null) AddComplexProp(propList, "OnClick", btn.OnClick);
                break;

            case HyperlinkButtonElement hlBtn:
                AddStringProp(propList, "Content", hlBtn.Content);
                if (hlBtn.NavigateUri is not null) AddStringProp(propList, "NavigateUri", hlBtn.NavigateUri.ToString());
                if (hlBtn.OnClick is not null) AddComplexProp(propList, "OnClick", hlBtn.OnClick);
                break;

            case RepeatButtonElement repBtn:
                AddStringProp(propList, "Label", repBtn.Label);
                AddIntProp(propList, "Delay", repBtn.Delay);
                AddIntProp(propList, "Interval", repBtn.Interval);
                if (repBtn.OnClick is not null) AddComplexProp(propList, "OnClick", repBtn.OnClick);
                break;

            case ToggleButtonElement togBtn:
                AddStringProp(propList, "Label", togBtn.Label);
                AddBoolProp(propList, "IsChecked", togBtn.IsChecked);
                if (togBtn.OnToggled is not null) AddComplexProp(propList, "OnToggled", togBtn.OnToggled);
                break;

            case DropDownButtonElement ddBtn:
                AddStringProp(propList, "Label", ddBtn.Label);
                break;

            case SplitButtonElement spBtn:
                AddStringProp(propList, "Label", spBtn.Label);
                if (spBtn.OnClick is not null) AddComplexProp(propList, "OnClick", spBtn.OnClick);
                break;

            case ToggleSplitButtonElement tspBtn:
                AddStringProp(propList, "Label", tspBtn.Label);
                AddBoolProp(propList, "IsChecked", tspBtn.IsChecked);
                break;

            case TextFieldElement tf:
                AddStringProp(propList, "Value", tf.Value);
                if (tf.Placeholder is not null) AddStringProp(propList, "Placeholder", tf.Placeholder);
                if (tf.OnChanged is not null) AddComplexProp(propList, "OnChanged", tf.OnChanged);
                break;

            case PasswordBoxElement pw:
                AddStringProp(propList, "Password", pw.Password);
                if (pw.PlaceholderText is not null) AddStringProp(propList, "PlaceholderText", pw.PlaceholderText);
                break;

            case NumberBoxElement nb:
                AddDoubleProp(propList, "Value", nb.Value);
                AddDoubleProp(propList, "Minimum", nb.Minimum);
                AddDoubleProp(propList, "Maximum", nb.Maximum);
                AddDoubleProp(propList, "SmallChange", nb.SmallChange);
                AddDoubleProp(propList, "LargeChange", nb.LargeChange);
                AddEnumProp(propList, "SpinButtonPlacement", (int)nb.SpinButtonPlacement);
                if (nb.Header is not null) AddStringProp(propList, "Header", nb.Header);
                break;

            case AutoSuggestBoxElement asb:
                AddStringProp(propList, "Text", asb.Text);
                if (asb.PlaceholderText is not null) AddStringProp(propList, "PlaceholderText", asb.PlaceholderText);
                break;

            case CheckBoxElement cb:
                AddBoolProp(propList, "IsChecked", cb.IsChecked);
                if (cb.Label is not null) AddStringProp(propList, "Label", cb.Label);
                break;

            case RadioButtonElement rb:
                AddStringProp(propList, "Label", rb.Label);
                AddBoolProp(propList, "IsChecked", rb.IsChecked);
                if (rb.GroupName is not null) AddStringProp(propList, "GroupName", rb.GroupName);
                break;

            case RadioButtonsElement rbs:
                AddIntProp(propList, "SelectedIndex", rbs.SelectedIndex);
                if (rbs.Header is not null) AddStringProp(propList, "Header", rbs.Header);
                break;

            case ComboBoxElement combo:
                AddIntProp(propList, "SelectedIndex", combo.SelectedIndex);
                AddBoolProp(propList, "IsEditable", combo.IsEditable);
                if (combo.PlaceholderText is not null) AddStringProp(propList, "PlaceholderText", combo.PlaceholderText);
                if (combo.Header is not null) AddStringProp(propList, "Header", combo.Header);
                break;

            case SliderElement sl:
                AddDoubleProp(propList, "Value", sl.Value);
                AddDoubleProp(propList, "Min", sl.Min);
                AddDoubleProp(propList, "Max", sl.Max);
                AddDoubleProp(propList, "StepFrequency", sl.StepFrequency);
                if (sl.Header is not null) AddStringProp(propList, "Header", sl.Header);
                break;

            case ToggleSwitchElement ts:
                AddBoolProp(propList, "IsOn", ts.IsOn);
                if (ts.OnContent is not null) AddStringProp(propList, "OnContent", ts.OnContent);
                if (ts.OffContent is not null) AddStringProp(propList, "OffContent", ts.OffContent);
                if (ts.Header is not null) AddStringProp(propList, "Header", ts.Header);
                break;

            case RatingControlElement rc:
                AddDoubleProp(propList, "Value", rc.Value);
                AddIntProp(propList, "MaxRating", rc.MaxRating);
                AddBoolProp(propList, "IsReadOnly", rc.IsReadOnly);
                if (rc.Caption is not null) AddStringProp(propList, "Caption", rc.Caption);
                break;

            case ColorPickerElement cp:
                AddHashProp(propList, "Color", (ulong)cp.Color.GetHashCode());
                AddBoolProp(propList, "IsAlphaEnabled", cp.IsAlphaEnabled);
                break;

            case CalendarDatePickerElement cdp:
                if (cdp.Date.HasValue) AddHashProp(propList, "Date", (ulong)cdp.Date.Value.GetHashCode());
                if (cdp.PlaceholderText is not null) AddStringProp(propList, "PlaceholderText", cdp.PlaceholderText);
                break;

            case DatePickerElement dp:
                AddHashProp(propList, "Date", (ulong)dp.Date.GetHashCode());
                break;

            case TimePickerElement tp:
                AddHashProp(propList, "Time", (ulong)tp.Time.GetHashCode());
                AddIntProp(propList, "MinuteIncrement", tp.MinuteIncrement);
                break;

            case ProgressElement prog:
                AddBoolProp(propList, "IsIndeterminate", prog.IsIndeterminate);
                if (prog.Value.HasValue) AddDoubleProp(propList, "Value", prog.Value.Value);
                AddDoubleProp(propList, "Minimum", prog.Minimum);
                AddDoubleProp(propList, "Maximum", prog.Maximum);
                break;

            case ProgressRingElement ring:
                AddBoolProp(propList, "IsIndeterminate", ring.IsIndeterminate);
                AddBoolProp(propList, "IsActive", ring.IsActive);
                if (ring.Value.HasValue) AddDoubleProp(propList, "Value", ring.Value.Value);
                break;

            case ImageElement img:
                AddStringProp(propList, "Source", img.Source);
                if (img.Width.HasValue) AddDoubleProp(propList, "Width", img.Width.Value);
                if (img.Height.HasValue) AddDoubleProp(propList, "Height", img.Height.Value);
                break;

            case PersonPictureElement pp:
                if (pp.DisplayName is not null) AddStringProp(propList, "DisplayName", pp.DisplayName);
                if (pp.Initials is not null) AddStringProp(propList, "Initials", pp.Initials);
                AddBoolProp(propList, "IsGroup", pp.IsGroup);
                break;

            case WebView2Element wv:
                if (wv.Source is not null) AddStringProp(propList, "Source", wv.Source.ToString());
                break;

            case StackElement stack:
                AddEnumProp(propList, "Orientation", (int)stack.Orientation);
                AddDoubleProp(propList, "Spacing", stack.Spacing);
                break;

            case GridElement grid:
                AddDoubleProp(propList, "RowSpacing", grid.RowSpacing);
                AddDoubleProp(propList, "ColumnSpacing", grid.ColumnSpacing);
                break;

            case ScrollViewElement scroll:
                AddEnumProp(propList, "Orientation", (int)scroll.Orientation);
                break;

            case BorderElement border:
                if (border.CornerRadius.HasValue) AddDoubleProp(propList, "CornerRadius", border.CornerRadius.Value);
                if (border.Background is not null) AddComplexProp(propList, "Background", border.Background);
                if (border.BorderBrush is not null) AddComplexProp(propList, "BorderBrush", border.BorderBrush);
                if (border.BorderThickness.HasValue) AddDoubleProp(propList, "BorderThickness", border.BorderThickness.Value);
                break;

            case ExpanderElement exp:
                AddStringProp(propList, "Header", exp.Header);
                AddBoolProp(propList, "IsExpanded", exp.IsExpanded);
                AddEnumProp(propList, "ExpandDirection", (int)exp.ExpandDirection);
                break;

            case SplitViewElement sv:
                AddBoolProp(propList, "IsPaneOpen", sv.IsPaneOpen);
                AddDoubleProp(propList, "OpenPaneLength", sv.OpenPaneLength);
                AddEnumProp(propList, "DisplayMode", (int)sv.DisplayMode);
                break;

            case ViewboxElement:
                // Viewbox has no serializable scalar props
                break;

            case CanvasElement cvs:
                if (cvs.Width.HasValue) AddDoubleProp(propList, "Width", cvs.Width.Value);
                if (cvs.Height.HasValue) AddDoubleProp(propList, "Height", cvs.Height.Value);
                break;

            case NavigationViewElement nav:
                AddBoolProp(propList, "IsPaneOpen", nav.IsPaneOpen);
                AddEnumProp(propList, "PaneDisplayMode", (int)nav.PaneDisplayMode);
                AddBoolProp(propList, "IsBackEnabled", nav.IsBackEnabled);
                if (nav.SelectedTag is not null) AddStringProp(propList, "SelectedTag", nav.SelectedTag);
                break;

            case TabViewElement tab:
                AddIntProp(propList, "SelectedIndex", tab.SelectedIndex);
                AddBoolProp(propList, "IsAddTabButtonVisible", tab.IsAddTabButtonVisible);
                break;

            case BreadcrumbBarElement:
                break;

            case PivotElement pvt:
                AddIntProp(propList, "SelectedIndex", pvt.SelectedIndex);
                if (pvt.Title is not null) AddStringProp(propList, "Title", pvt.Title);
                break;

            case ListViewElement lv:
                AddIntProp(propList, "SelectedIndex", lv.SelectedIndex);
                AddEnumProp(propList, "SelectionMode", (int)lv.SelectionMode);
                if (lv.Header is not null) AddStringProp(propList, "Header", lv.Header);
                break;

            case GridViewElement gv:
                AddIntProp(propList, "SelectedIndex", gv.SelectedIndex);
                AddEnumProp(propList, "SelectionMode", (int)gv.SelectionMode);
                if (gv.Header is not null) AddStringProp(propList, "Header", gv.Header);
                break;

            case TreeViewElement tv:
                AddEnumProp(propList, "SelectionMode", (int)tv.SelectionMode);
                break;

            case FlipViewElement fv:
                AddIntProp(propList, "SelectedIndex", fv.SelectedIndex);
                break;

            case ContentDialogElement cd:
                AddStringProp(propList, "Title", cd.Title);
                AddStringProp(propList, "PrimaryButtonText", cd.PrimaryButtonText);
                AddBoolProp(propList, "IsOpen", cd.IsOpen);
                break;

            case FlyoutElement fly:
                AddBoolProp(propList, "IsOpen", fly.IsOpen);
                AddEnumProp(propList, "Placement", (int)fly.Placement);
                break;

            case TeachingTipElement tt:
                AddStringProp(propList, "Title", tt.Title);
                if (tt.Subtitle is not null) AddStringProp(propList, "Subtitle", tt.Subtitle);
                AddBoolProp(propList, "IsOpen", tt.IsOpen);
                break;

            case InfoBarElement ib:
                if (ib.Title is not null) AddStringProp(propList, "Title", ib.Title);
                if (ib.Message is not null) AddStringProp(propList, "Message", ib.Message);
                AddEnumProp(propList, "Severity", (int)ib.Severity);
                AddBoolProp(propList, "IsOpen", ib.IsOpen);
                break;

            case InfoBadgeElement badge:
                if (badge.Value.HasValue) AddIntProp(propList, "Value", badge.Value.Value);
                break;

            case MenuBarElement:
            case CommandBarElement:
            case MenuFlyoutElement:
                break;

            case ComponentElement comp:
                AddHashProp(propList, "ComponentType", (ulong)ViewDiffer.HashString(comp.ComponentType.FullName ?? comp.ComponentType.Name));
                break;

            case FuncElement:
                // Opaque leaf — no serializable props
                break;
        }

        return propList.ToArray();
    }

    internal Element[] GetChildren(Element element)
    {
        return element switch
        {
            StackElement stack => stack.Children,
            GridElement grid => grid.Children.Select(c => c.Element).ToArray(),
            ScrollViewElement scroll => [scroll.Child],
            BorderElement border => [border.Child],
            ExpanderElement exp => [exp.Content],
            SplitViewElement sv => new[] { sv.Pane!, sv.Content! }.Where(e => e is not null).ToArray(),
            ViewboxElement vb => [vb.Child],
            CanvasElement cvs => cvs.Children.Select(c => c.Element).ToArray(),
            ListViewElement lv => lv.Items,
            GridViewElement gv => gv.Items,
            FlipViewElement fv => fv.Items,
            FlyoutElement fly => [fly.Target, fly.FlyoutContent],
            ContentDialogElement cd => [cd.Content],
            _ => [],
        };
    }

    // ── Prop helpers ────────────────────────────────────────────────

    private void AddStringProp(List<ViewProp> props, string name, string value)
    {
        props.Add(new ViewProp
        {
            DpId = ViewDiffer.HashString(name),
            ValueHash = _registry.Register(value),
        });
    }

    private static void AddBoolProp(List<ViewProp> props, string name, bool value)
    {
        props.Add(new ViewProp
        {
            DpId = ViewDiffer.HashString(name),
            ValueHash = value ? 1UL : 0UL,
        });
    }

    private static void AddIntProp(List<ViewProp> props, string name, int value)
    {
        props.Add(new ViewProp
        {
            DpId = ViewDiffer.HashString(name),
            ValueHash = (ulong)(uint)value,
        });
    }

    private static void AddDoubleProp(List<ViewProp> props, string name, double value)
    {
        props.Add(new ViewProp
        {
            DpId = ViewDiffer.HashString(name),
            ValueHash = (ulong)BitConverter.DoubleToInt64Bits(value),
        });
    }

    private static void AddEnumProp(List<ViewProp> props, string name, int value)
    {
        props.Add(new ViewProp
        {
            DpId = ViewDiffer.HashString(name),
            ValueHash = (ulong)(uint)value,
        });
    }

    private static void AddHashProp(List<ViewProp> props, string name, ulong hash)
    {
        props.Add(new ViewProp
        {
            DpId = ViewDiffer.HashString(name),
            ValueHash = hash,
        });
    }

    private void AddComplexProp(List<ViewProp> props, string name, object value)
    {
        props.Add(new ViewProp
        {
            DpId = ViewDiffer.HashString(name),
            ValueHash = _registry.Register(value),
        });
    }
}

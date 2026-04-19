using Microsoft.UI.Reactor.AppTests.Host.SelfTest;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Reactor.AppTests.Host.SelfTest.Fixtures;

/// <summary>
/// Validates FlexPanel's implementation of CSS Flexbox, organized as:
///
///   - **Section A — `CssStack_*`**: scenarios where CSS-computed values and
///     WinUI <see cref="StackPanel"/> output are identical. Three-way parity:
///     StackPanel actual size, FlexPanel actual size, and the hand-computed
///     CSS expected value (derived from the CSS Flexbox algorithm and
///     verified against a real browser during fixture design). These define
///     the drop-in-replacement safety zone for StackPanel → FlexPanel.
///
///   - **Section B — `CssDiverge_*`**: scenarios where CSS defines behavior
///     StackPanel cannot express (<c>flex-grow</c>, <c>justify-content</c>,
///     <c>flex-wrap</c>, <c>align-self</c>, explicit size honor). Only
///     FlexPanel is compared against the CSS expected value; fixtures
///     inline-document why StackPanel is excluded.
///
/// See <c>docs/research/FlexPanel-vs-StackPanel.md</c> for the full matrix.
///
/// Note on WebView2: an attempt was made to render each scenario's HTML in a
/// real WebView2 and read <c>getBoundingClientRect()</c> back for dynamic
/// CSS-truth comparison. In the selftest harness's hosting configuration,
/// <c>CoreWebView2</c> does not initialize (confirmed in MarkdownHtmlFixtures
/// as well), so the approach falls back to hand-computed CSS values with
/// inline comments citing the relevant CSS rule. If the harness acquires
/// a compositor-ready WebView2 runtime later, <c>WebViewCssMeasurement</c>
/// is kept in-tree as the plug-in point.
/// </summary>
internal static class FlexPanelCssBehaviorFixtures
{
    private const double Tolerance = 1.5;

    private static bool Near(double a, double b, double tol = Tolerance)
        => Math.Abs(a - b) <= tol;

    private static void CheckNear(Harness H, string name, double actual, double expected, double tol = Tolerance)
    {
        var ok = Near(actual, expected, tol);
        if (!ok)
            Console.WriteLine($"# {name}: expected~{expected:F2} actual={actual:F2} delta={actual - expected:F2}");
        H.Check(name, ok);
    }

    // ─────────────────────────────────────────────────────────────────
    //  Shared: mount two sibling panels inside a parent Grid so both are
    //  laid out with the same outer constraints, then measure.
    // ─────────────────────────────────────────────────────────────────

    private static async Task MountSideBySide(Harness H, Panel? stack, FlexPanel flex,
        double columnWidth = 400, double hostHeight = 500)
    {
        var root = new Grid
        {
            Width = columnWidth * (stack is null ? 1 : 2) + (stack is null ? 0 : 16),
            Height = hostHeight,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };

        if (stack is not null)
        {
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(columnWidth) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        }
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(columnWidth) });

        int col = 0;
        if (stack is not null)
        {
            stack.HorizontalAlignment = HorizontalAlignment.Left;
            stack.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetColumn(stack, col);
            root.Children.Add(stack);
            col += 2;
        }
        flex.VerticalAlignment = VerticalAlignment.Top;
        Grid.SetColumn(flex, col);
        root.Children.Add(flex);

        H.SetContent(root);
        await Harness.Render();
    }

    private static Border MakeChild(double? width, double? height, string tag, Thickness? margin = null)
    {
        var b = new Border
        {
            Tag = tag,
            Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
            Child = new TextBlock { Text = tag },
        };
        if (width is not null) b.Width = width.Value;
        if (height is not null) b.Height = height.Value;
        if (margin is not null) b.Margin = margin.Value;
        return b;
    }

    private static Border? FindByTag(Panel panel, string tag)
    {
        foreach (var child in panel.Children)
            if (child is Border b && (string?)b.Tag == tag) return b;
        return null;
    }

    private static double X(FrameworkElement child, FrameworkElement relativeTo) =>
        child.TransformToVisual(relativeTo).TransformPoint(new global::Windows.Foundation.Point(0, 0)).X;
    private static double Y(FrameworkElement child, FrameworkElement relativeTo) =>
        child.TransformToVisual(relativeTo).TransformPoint(new global::Windows.Foundation.Point(0, 0)).Y;

    // ════════════════════════════════════════════════════════════════════
    //  SECTION A — CSS ↔ StackPanel agreement
    //    Expected CSS values hand-computed from the flexbox algorithm.
    //    Each assertion checks BOTH StackPanel and FlexPanel against the
    //    same CSS-expected number.
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 3 × 120×40 vertical children, no spacing.
    /// CSS: container fit-content = 120 × 120.
    /// </summary>
    internal class CssStack_BasicVerticalNoSpacing(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 0 };
            stack.Children.Add(MakeChild(120, 40, "A"));
            stack.Children.Add(MakeChild(120, 40, "B"));
            stack.Children.Add(MakeChild(120, 40, "C"));

            var flex = new FlexPanel
            {
                Direction = FlexDirection.Column,
                RowGap = 0,
                AlignItems = FlexAlign.FlexStart,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            flex.Children.Add(MakeChild(120, 40, "A"));
            flex.Children.Add(MakeChild(120, 40, "B"));
            flex.Children.Add(MakeChild(120, 40, "C"));

            await MountSideBySide(H, stack, flex);

            // CSS fit-content column with items 120 wide, 40 tall each:
            //   width = max(item widths) = 120, height = sum = 120
            CheckNear(H, "A_BasicVertNoSpacing_Stack_W", stack.ActualWidth, 120);
            CheckNear(H, "A_BasicVertNoSpacing_Flex_W",  flex.ActualWidth,  120);
            CheckNear(H, "A_BasicVertNoSpacing_Stack_H", stack.ActualHeight, 120);
            CheckNear(H, "A_BasicVertNoSpacing_Flex_H",  flex.ActualHeight,  120);
        }
    }

    /// <summary>
    /// Vertical, 3 × 40, gap=8. CSS: 40 × 3 + 8 × 2 = 136.
    /// </summary>
    internal class CssStack_VerticalWithSpacing(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
            stack.Children.Add(MakeChild(120, 40, "A"));
            stack.Children.Add(MakeChild(120, 40, "B"));
            stack.Children.Add(MakeChild(120, 40, "C"));

            var flex = new FlexPanel
            {
                Direction = FlexDirection.Column, RowGap = 8,
                AlignItems = FlexAlign.FlexStart,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            flex.Children.Add(MakeChild(120, 40, "A"));
            flex.Children.Add(MakeChild(120, 40, "B"));
            flex.Children.Add(MakeChild(120, 40, "C"));

            await MountSideBySide(H, stack, flex);

            CheckNear(H, "A_VertSpacing_Stack_H", stack.ActualHeight, 136);
            CheckNear(H, "A_VertSpacing_Flex_H",  flex.ActualHeight,  136);
        }
    }

    /// <summary>
    /// Horizontal, 3 × 60 widths + 10 px gap = 200.
    /// </summary>
    internal class CssStack_HorizontalWithSpacing(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            stack.Children.Add(MakeChild(60, 40, "A"));
            stack.Children.Add(MakeChild(60, 40, "B"));
            stack.Children.Add(MakeChild(60, 40, "C"));

            var flex = new FlexPanel
            {
                Direction = FlexDirection.Row, ColumnGap = 10,
                AlignItems = FlexAlign.FlexStart,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            flex.Children.Add(MakeChild(60, 40, "A"));
            flex.Children.Add(MakeChild(60, 40, "B"));
            flex.Children.Add(MakeChild(60, 40, "C"));

            await MountSideBySide(H, stack, flex);

            CheckNear(H, "A_HorizSpacing_Stack_W", stack.ActualWidth, 200);
            CheckNear(H, "A_HorizSpacing_Flex_W",  flex.ActualWidth,  200);
        }
    }

    /// <summary>
    /// Margins do NOT collapse: 16 + 40 + 16 + 8 + 16 + 40 + 16 = 152.
    /// (XAML additive; CSS flex items also don't collapse block margins.)
    /// </summary>
    internal class CssStack_VerticalChildMarginsAndSpacing(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
            stack.Children.Add(MakeChild(120, 40, "A", new Thickness(0, 16, 0, 16)));
            stack.Children.Add(MakeChild(120, 40, "B", new Thickness(0, 16, 0, 16)));

            var flex = new FlexPanel
            {
                Direction = FlexDirection.Column, RowGap = 8,
                AlignItems = FlexAlign.FlexStart,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            flex.Children.Add(MakeChild(120, 40, "A", new Thickness(0, 16, 0, 16)));
            flex.Children.Add(MakeChild(120, 40, "B", new Thickness(0, 16, 0, 16)));

            await MountSideBySide(H, stack, flex);

            CheckNear(H, "A_Margins_Stack_H", stack.ActualHeight, 152);
            CheckNear(H, "A_Margins_Flex_H",  flex.ActualHeight,  152);
        }
    }

    /// <summary>
    /// Empty panel reports 0 × 0 (CSS: fit-content on an empty block = 0).
    /// </summary>
    internal class CssStack_EmptyPanel(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
            var flex = new FlexPanel
            {
                Direction = FlexDirection.Column, RowGap = 8,
                HorizontalAlignment = HorizontalAlignment.Left,
            };

            await MountSideBySide(H, stack, flex);

            CheckNear(H, "A_Empty_Stack_H", stack.ActualHeight, 0, tol: 0.5);
            CheckNear(H, "A_Empty_Flex_H",  flex.ActualHeight,  0, tol: 0.5);
        }
    }

    /// <summary>
    /// Single child: no spacing/gap applied. 40 in both.
    /// </summary>
    internal class CssStack_SingleChild(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
            stack.Children.Add(MakeChild(120, 40, "A"));

            var flex = new FlexPanel
            {
                Direction = FlexDirection.Column, RowGap = 8,
                AlignItems = FlexAlign.FlexStart,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            flex.Children.Add(MakeChild(120, 40, "A"));

            await MountSideBySide(H, stack, flex);

            CheckNear(H, "A_Single_Stack_H", stack.ActualHeight, 40);
            CheckNear(H, "A_Single_Flex_H",  flex.ActualHeight,  40);
        }
    }

    /// <summary>
    /// WinUI Visibility=Collapsed ≡ CSS display:none.
    /// Both contribute 0 to the main axis AND drop their gap slot.
    /// 40 + 8 + 40 = 88.
    /// </summary>
    internal class CssStack_CollapsedChildExcluded(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
            stack.Children.Add(MakeChild(120, 40, "A"));
            var sHidden = MakeChild(120, 40, "B");
            sHidden.Visibility = Visibility.Collapsed;
            stack.Children.Add(sHidden);
            stack.Children.Add(MakeChild(120, 40, "C"));

            var flex = new FlexPanel
            {
                Direction = FlexDirection.Column, RowGap = 8,
                AlignItems = FlexAlign.FlexStart,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            flex.Children.Add(MakeChild(120, 40, "A"));
            var fHidden = MakeChild(120, 40, "B");
            fHidden.Visibility = Visibility.Collapsed;
            flex.Children.Add(fHidden);
            flex.Children.Add(MakeChild(120, 40, "C"));

            await MountSideBySide(H, stack, flex);

            CheckNear(H, "A_Collapsed_Stack_H", stack.ActualHeight, 88);
            CheckNear(H, "A_Collapsed_Flex_H",  flex.ActualHeight,  88);
        }
    }

    /// <summary>
    /// Horizontal panel Height=80, children no height: both stretch to 80
    /// (XAML default VerticalAlignment=Stretch == CSS align-items: stretch).
    /// </summary>
    internal class CssStack_HorizontalCrossStretch(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 0, Height = 80 };
            stack.Children.Add(MakeChild(60, null, "A"));
            stack.Children.Add(MakeChild(60, null, "B"));

            var flex = new FlexPanel
            {
                Direction = FlexDirection.Row, ColumnGap = 0, Height = 80,
                AlignItems = FlexAlign.Stretch,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            flex.Children.Add(MakeChild(60, null, "A"));
            flex.Children.Add(MakeChild(60, null, "B"));

            await MountSideBySide(H, stack, flex);

            CheckNear(H, "A_XStretch_Stack_ChildH",
                FindByTag(stack, "A")!.ActualHeight, 80);
            CheckNear(H, "A_XStretch_Flex_ChildH",
                FindByTag(flex, "A")!.ActualHeight, 80);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  SECTION B — CSS diverges from StackPanel; FlexPanel follows CSS.
    //  Only FlexPanel is asserted against the CSS expected value.
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Explicit Height < child content. CSS honors the height; StackPanel's
    /// MeasureOverride quirk (see StackPanel.cpp in microsoft-ui-xaml-lift)
    /// returns the unclamped sum instead — so we DON'T reproduce it.
    /// FlexPanel: ActualHeight = 100. Children keep their 40 px.
    /// </summary>
    internal class CssDiverge_ExplicitHeightHonored(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var flex = new FlexPanel
            {
                Direction = FlexDirection.Column, RowGap = 0, Height = 100,
                AlignItems = FlexAlign.FlexStart,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            flex.Children.Add(MakeChild(120, 40, "A"));
            flex.Children.Add(MakeChild(120, 40, "B"));
            flex.Children.Add(MakeChild(120, 40, "C"));
            flex.Children.Add(MakeChild(120, 40, "D"));

            await MountSideBySide(H, stack: null, flex);

            CheckNear(H, "B_ExplicitHeight_Flex_100", flex.ActualHeight, 100);
            CheckNear(H, "B_ExplicitHeight_ChildD_40",
                FindByTag(flex, "D")!.ActualHeight, 40);
        }
    }

    /// <summary>
    /// justify-content: center on a column of fixed Height=300 with 3×40
    /// children → first child at y = (300 - 120) / 2 = 90, last at y=170.
    /// No StackPanel equivalent.
    /// </summary>
    internal class CssDiverge_JustifyContentCenter(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var flex = new FlexPanel
            {
                Direction = FlexDirection.Column, RowGap = 0,
                Width = 200, Height = 300,
                JustifyContent = FlexJustify.Center,
                AlignItems = FlexAlign.FlexStart,
            };
            flex.Children.Add(MakeChild(120, 40, "A"));
            flex.Children.Add(MakeChild(120, 40, "B"));
            flex.Children.Add(MakeChild(120, 40, "C"));

            await MountSideBySide(H, stack: null, flex);

            var flexA = FindByTag(flex, "A")!;
            var flexC = FindByTag(flex, "C")!;
            CheckNear(H, "B_JustifyCenter_FirstY_90",  Y(flexA, flex), 90);
            CheckNear(H, "B_JustifyCenter_LastY_170", Y(flexC, flex), 170);
        }
    }

    /// <summary>
    /// flex-grow: 1 on a middle child. Container width 300, siblings fixed
    /// at 50 each → grow child absorbs the remaining 200.
    /// </summary>
    internal class CssDiverge_FlexGrowDistributesSpace(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var growChild = MakeChild(null, 40, "G");
            FlexPanel.SetGrow(growChild, 1);

            var flex = new FlexPanel
            {
                Direction = FlexDirection.Row, ColumnGap = 0,
                Width = 300, Height = 40,
                AlignItems = FlexAlign.Stretch,
            };
            flex.Children.Add(MakeChild(50, 40, "A"));
            flex.Children.Add(growChild);
            flex.Children.Add(MakeChild(50, 40, "C"));

            await MountSideBySide(H, stack: null, flex);

            CheckNear(H, "B_FlexGrow_GrowWidth_200",
                FindByTag(flex, "G")!.ActualWidth, 200);
        }
    }

    /// <summary>
    /// justify-content: space-between in a 300-wide row with two 50px
    /// children → A at x=0, B at x=250.
    /// </summary>
    internal class CssDiverge_JustifyContentSpaceBetween(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var flex = new FlexPanel
            {
                Direction = FlexDirection.Row, ColumnGap = 0,
                Width = 300, Height = 40,
                JustifyContent = FlexJustify.SpaceBetween,
                AlignItems = FlexAlign.Stretch,
            };
            flex.Children.Add(MakeChild(50, 40, "A"));
            flex.Children.Add(MakeChild(50, 40, "B"));

            await MountSideBySide(H, stack: null, flex);

            var a = FindByTag(flex, "A")!;
            var b = FindByTag(flex, "B")!;
            CheckNear(H, "B_SpaceBetween_AX_0",   X(a, flex), 0);
            CheckNear(H, "B_SpaceBetween_BX_250", X(b, flex), 250);
        }
    }

    /// <summary>
    /// Per-child align-self: center inside a row with align-items:flex-start.
    /// Container 200×80; child B 50×40 with align-self:center → y = 20.
    /// </summary>
    internal class CssDiverge_AlignSelfPerChild(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var selfChild = MakeChild(50, 40, "B");
            FlexPanel.SetAlignSelf(selfChild, FlexAlign.Center);

            var flex = new FlexPanel
            {
                Direction = FlexDirection.Row, ColumnGap = 0,
                Width = 200, Height = 80,
                AlignItems = FlexAlign.FlexStart,
            };
            flex.Children.Add(MakeChild(50, 40, "A"));
            flex.Children.Add(selfChild);

            await MountSideBySide(H, stack: null, flex);

            var b = FindByTag(flex, "B")!;
            CheckNear(H, "B_AlignSelf_BY_20", Y(b, flex), 20);
        }
    }

    /// <summary>
    /// flex-wrap: wrap with 300-wide row and three 120px children: two on
    /// line 1 (240), third wraps to line 2 at y = max(line-1 heights) = 40.
    /// </summary>
    internal class CssDiverge_FlexWrap(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var flex = new FlexPanel
            {
                Direction = FlexDirection.Row, ColumnGap = 0, RowGap = 0,
                Width = 300, Wrap = FlexWrap.Wrap,
                AlignItems = FlexAlign.FlexStart,
                AlignContent = FlexAlign.FlexStart,
            };
            flex.Children.Add(MakeChild(120, 40, "A"));
            flex.Children.Add(MakeChild(120, 40, "B"));
            flex.Children.Add(MakeChild(120, 40, "C"));

            await MountSideBySide(H, stack: null, flex);

            var c = FindByTag(flex, "C")!;
            CheckNear(H, "B_Wrap_CY_40", Y(c, flex), 40);
        }
    }
}

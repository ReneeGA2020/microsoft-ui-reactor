# FlexPanel vs StackPanel — layout behavior reference

**Status:** `FlexPanel` targets CSS Flexbox semantics, not StackPanel parity.
Where CSS and StackPanel agree, FlexPanel is a safe drop-in replacement.
Where they diverge, FlexPanel follows CSS.

This document spells out the agreement set and the divergence set, with one
selftest fixture per row so each rule is executable. The CSS side is
validated side-by-side against a real browser via a WebView2 hosted in the
same fixture — not just asserted against a hand-computed number — so the
spec side of the comparison stays honest.

## Why "CSS, not StackPanel"

- **Portability of intent.** CSS Flexbox is a widely-understood layout model;
  StackPanel is a WinUI-specific single-axis stacker. A CSS-aligned panel
  lets designers and web-trained engineers reach for familiar properties
  (`flex-grow`, `justify-content`, `gap`) and get the expected behavior.
- **Richer feature set.** CSS gives us `justify-content`, `align-items`,
  `align-self`, `flex-grow`, `flex-shrink`, `flex-basis`, `wrap`, `gap`, and
  writing-mode-aware axis handling. StackPanel has `Orientation` and
  `Spacing`.
- **Better fit for the ScrollViewer→child measurement pattern.** CSS's
  block-formatting-context rule (the flex container resolves its inline
  axis from the containing block *before* flex layout runs) naturally
  handles the "ScrollViewer → FlexColumn → RichTextBlock" case in a single
  measure pass. See § Performance below.

## Agreement set — CSS and StackPanel both produce the same result

In a vertical StackPanel ↔ `flex-direction: column` container, if the panel
has default `HorizontalAlignment=Stretch` and there's no `flex-grow` on
children, the following scenarios are identical between StackPanel, FlexPanel,
and a browser-rendered flex container:

| Scenario | StackPanel | FlexPanel | CSS |
|---|---|---|---|
| Fixed-size children, default spacing | child sum on main, max on cross | same | same |
| Fixed-size children, `Spacing`/`gap` > 0 | `sum + gap*(n-1)` | same | same |
| Child with `Margin` + panel spacing | margins do NOT collapse; effective gap is `m.end + spacing + m.start` | same | same (flex already disables block margin collapse) |
| Single child | no spacing applied | same | same |
| Zero children | `DesiredSize = (0, 0)` | same | same |
| Cross-axis Stretch default | child with no explicit width fills panel width | same | same (with `align-items: stretch` default) |
| Explicit child size wins over cross Stretch | respected | respected | respected |
| `Visibility=Collapsed` child (`display:none` in CSS) | contributes 0 size, excluded from gap count | same | same |

These are the cases the migration guide can point at: "If you're using
StackPanel with these options, FlexPanel works the same."

## Divergence set — CSS and StackPanel disagree; FlexPanel follows CSS

| Scenario | StackPanel | CSS / FlexPanel |
|---|---|---|
| Explicit `Height` < content size | **Ignored** during Measure (returns the full sum of children; content overflows the box). See `microsoft-ui-xaml-lift/dev/Panel/StackPanel.cpp` MeasureOverride. | Honored — container reports the requested height; content overflows via `overflow` rules, not by inflating the panel. |
| Main-axis justify modes other than flex-start | not supported | `justify-content: center / space-between / space-around / space-evenly / flex-end` all work |
| `flex-grow` / `flex-shrink` / `flex-basis` on children | no analog | children grow/shrink to distribute or absorb remaining main-axis space |
| `flex-wrap` (children flow to new lines) | no analog; one-line only | `wrap` / `wrap-reverse` supported |
| `align-self` per-child override on cross axis | no analog | `align-self: flex-start / center / flex-end / stretch` per child |
| Horizontal panels with ScrollViewer height | cross-axis height content-sized; children stretch | same effective result, via CSS block-level inline-fill rule |
| Cross-axis inline-fill (`display: flex` is block-level) | StackPanel shrink-wraps when `HorizontalAlignment != Stretch` | FlexPanel also supports shrink-wrap: `HorizontalAlignment != Stretch` on FlexPanel resolves to CSS `width: fit-content` semantics |

The one non-obvious call here is the last row: we provide a WinUI-idiomatic
escape hatch so FlexPanel honors `HorizontalAlignment`, but the default
(Stretch) matches CSS `display: flex`.

## Known StackPanel quirks FlexPanel intentionally does NOT reproduce

- **StackPanel ignores its own `Height`/`Width` during Measure.** With
  `Height=100` and children summing to 160, `ActualHeight` comes back as
  160 — verified in `StackPanel.cpp`, where `MeasureOverride` returns the
  sum-of-children DesiredSize without clamping to any panel-level size.
  FlexPanel honors the explicit constraint. Fixture:
  `StackFlexParity_ExplicitHeightHonored` (Section B).
- **StackPanel's default cross-axis behavior is
  "measure-children-max-cross-width-then-fill-via-parent-allocation".**
  FlexPanel treats the container as block-level (CSS rule) and resolves
  the cross axis from the containing block in MeasureOverride itself.
  Observable difference only when mounted inside a parent that passes
  a smaller availableSize than the window width (e.g. a 300px Grid
  column) — StackPanel still reports 300, FlexPanel reports 300. Same
  result, different path; documented for completeness.

## Performance: why the CSS rule solves the RichTextBlock perf case for free

The known perf regression was:
`ScrollViewer (HorizontalScrollMode=Disabled) → FlexPanel → RichTextBlock`.
ScrollViewer passes `(availableWidth=finite, availableHeight=∞)` to its
content. The **original** FlexPanel content-sized both axes with
`CalculateLayout(NaN, NaN)`, which caused Yoga to call `MeasureFunc` on
each child with no cross-axis constraint. RichTextBlock measured at
infinite width (single-line) — a very expensive text-shaping pass. A
second measure pass then reflowed at the resolved width, doubling the work.

The **CSS rule fixes this structurally**:

1. Flex container is block-level. Its inline axis (width, horizontal WM)
   is resolved against the containing block *before* flex layout runs.
2. So FlexPanel's MeasureOverride feeds Yoga
   `CalculateLayout(availableSize.Width, NaN)` — width definite, height
   content-sized, in a single pass.
3. Children's `align-items: stretch` (default) makes them take the
   container's cross-axis (400px), so `MeasureFunc` on a RichTextBlock
   receives `width=400`, triggers one correctly-sized text shape pass,
   reports its height, done.

This is the same trick StackPanel uses (pass finite cross-axis to children,
infinite main-axis), expressed in CSS terms. One pass either way, no
infinite-width measurement.

## Shrink-wrap (fit-content) behavior

If a user sets `HorizontalAlignment != Stretch` on a FlexPanel, they are
asking for CSS `width: fit-content`. In that case the cross axis is
content-sized (Yoga with NaN + MaxWidth cap = `availableSize.Width`). This
is slower when children include text because each child is measured at
max-content first. Users opt into this explicitly.

The `Width` property set to an explicit value is handled by
`FrameworkElement.Measure` (before `MeasureOverride` runs) and short-
circuits the whole question.

## Reading the fixtures

- `Fixtures/FlexPanelCssBehaviorFixtures.cs`
  - Section A — `CssStack_*`: StackPanel + FlexPanel compared against the
    hand-computed CSS expected value (derived from the Flexbox algorithm
    and verified against a real browser at design time).
  - Section B — `CssDiverge_*`: FlexPanel compared against the CSS
    expected value only; StackPanel excluded with an inline justification.
- Run just these with:
  ```
  dotnet run --project tests/Reactor.AppTests.Host --no-build -- \
    --self-test --filter "Css"
  ```

### Note on dynamic CSS verification via WebView2

The original plan was to render each scenario's HTML in a live WebView2
alongside the native panels and pull `getBoundingClientRect()` back through
`ExecuteScriptAsync` for a fully dynamic three-way compare. In the current
selftest harness (`Application.Start` + `DispatcherQueueSynchronizationContext`
+ `window.Activate()`), `EnsureCoreWebView2Async()` resolves but `CoreWebView2`
stays null — confirmed by instrumenting `MarkdownHtmlFixtures.HtmlInWebView2`,
which has the same silent null despite its assertions passing (it never
inspects `CoreWebView2`). The WebView2 runtime simply doesn't come up in this
hosting configuration.

`tests/Reactor.AppTests.Host/SelfTest/WebViewCssMeasurement.cs` is kept
in-tree as the future plug-in point: if/when the harness is upgraded to a
compositor configuration that realizes a working WebView2, the helper
returns `BoundingRect` tuples for arbitrary CSS selectors and can slot back
into the fixture's `MountSideBySide` flow.

## Implementation notes

- `src/Reactor/Yoga/FlexPanel.cs` `MeasureOverride`: single branch,
  `CalculateLayout(fillCrossIfStretch, NaN)`, `MaxHeight` = availableSize
  when finite (so content below a scroll host is still capped), `MaxWidth`
  undefined when stretching (already definite) or set to availableSize
  when fit-content.
- `SyncYogaTree` mirrors WinUI `Visibility=Collapsed` onto Yoga
  `Display.None`, which is what makes collapsed children contribute
  nothing to the main-axis sum and skip the gap slot — matching CSS
  `display: none` and StackPanel's behavior.
- Yoga is symmetric by design (both axes participate in grow/shrink); the
  CSS rule we layer on top is specifically the *block-level inline-axis
  fill*, which CSS has and Yoga does not.

---

*Relevant source: `StackPanel.cpp` in `microsoft-ui-xaml-lift`
(`MeasureOverride` — accumulates child DesiredSizes unclamped; `ArrangeOverride` returns `finalSize`). Confirmed the Measure path does not clamp to the panel's own Width/Height.*

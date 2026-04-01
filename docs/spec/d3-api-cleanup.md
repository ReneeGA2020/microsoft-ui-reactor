# DuctD3 API Cleanup — Toward Clean `Func<Data, Element>`

Review of all 33 gallery samples for LINQ ergonomics, repeated patterns, and DSL gaps.
Goal: make the common case a single expression `data => D3Canvas(...)` that reads well.

---

## 1. Cross-Cutting Patterns That Need DSL Helpers

### 1.1 Axes and grid are manually assembled everywhere

**Problem:** Nearly every XY chart repeats the same 5-10 lines: two axis lines, Y tick labels via `D3TextRight`, X tick labels via `D3Text`, grid lines. The existing `D3Axes` helper only works with two `LinearScale`s and assumes tick label placement. Samples with `BandScale` on the X axis (all bar charts, grouped/stacked bars) *cannot use `D3Axes` at all* and resort to manual axis construction.

**Affected samples (manual axis assembly):**
- BarChart (lines 66-74) — manually builds Y ticks, X band labels, axis lines
- HorizontalBarChart (lines 63-88) — fully manual X/Y axes for horizontal layout
- StackedBarChart (lines 83-107) — manual X band labels
- GroupedBarChart (lines 74-97) — manual X band labels
- DivergingBarChart (lines 85-111) — manual X ticks and Y band labels
- SlopeChart (lines 59-98) — completely custom dual-column layout
- CandlestickChart (lines 79-104) — manual X labels every 5th tick
- DotPlot (lines 46-64) — manual X axis, horizontal row lines
- BoxPlot (lines 60-107) — manual Y axis, X group labels

**Proposed helpers:**
- `D3BandXAxis(band, top, left, height)` — renders X labels at band centers
- `D3BandYAxis(band, top, left)` — renders Y labels at band centers (for horizontal bars)
- `D3AxisLine(left, top, width, height)` — just the two L-shaped axis lines
- Overload `D3Axes` to accept `BandScale` for X or Y

### 1.2 Legend is built inline every time with magic offsets

**Problem:** 11 samples manually construct legends with `SelectMany` producing `(rect, text)` pairs at pixel-calculated positions. The pattern is always:
```csharp
.. keys.SelectMany((key, i) => new Element[]
{
    D3Rect(legendX, legendY + i * 22, 14, 14) with { Fill = ..., RadiusX = 2, RadiusY = 2 },
    D3Text(legendX + 20, legendY + i * 22, key, 11, Gray(60)),
})
```

**Affected samples:** StackedBarChart, GroupedBarChart, DivergingBarChart, StackedAreaChart, StreamgraphChart, SlopeChart, CandlestickChart, MultiLineChart, DifferenceChart, RidgePlot, SankeyDiagram.

**Proposed helper:**
```csharp
Element[] D3Legend(double x, double y, IEnumerable<(string label, Brush color)> items, double fontSize = 11)
```

### 1.3 Repeated `maxVal` discovery with manual loops

**Problem:** 9 samples use the pattern:
```csharp
double maxVal = 0;
foreach (var v in values) if (v > maxVal) maxVal = v;
```
or nested double-loop variants for stacked series. Meanwhile D3Extent exists but isn't used for flat arrays.

**Affected samples:** BarChart, HorizontalBarChart, GroupedBarChart, StackedBarChart, StreamgraphChart, StackedAreaChart.

**Comment:** These could just be `values.Max()` via LINQ, or `D3Extent.Extent(values)`. The manual loops are a readability drag. For stacked max-Y, a helper `StackSeries[].MaxY()` extension would clean up 4 samples.

### 1.4 The "screen space Y" double-scale pattern

**Problem:** Many samples create two Y scales — one in "data space" `[0, max] -> [plotH, 0]` and then a second "screen space" one `[domain] -> [top + plotH, top]`. The first is used for computing bar heights, the second for positioning on the canvas. This is confusing and error-prone.

**Affected samples:** BarChart (line 45-46), StackedBarChart (line 76), GroupedBarChart (line 67).

**Proposed:** Either:
- Add a `LinearScale.Translate(offset)` method that returns a new scale shifted by a constant
- Document the idiom of creating screen-space scales directly (which most other samples already do)
- Remove the data-space scale entirely and compute bar heights as `ys.Map(0) - ys.Map(value)`

### 1.5 ~~Null-path guards produce noisy ternaries~~ DONE

~~**Problem:** Every sample that uses `LineGenerator` or `AreaGenerator` has to guard against null path data.~~

**Resolved:** `D3Path` and `D3PathTranslated` already accept `null` pathData gracefully (produces a PathElement with null Data that renders nothing). Removed all null-path ternary guards from AreaChart, DifferenceChart, RidgePlot, Sunburst. Removed `.Where(path != null)` from StackedAreaChart, StreamgraphChart, PieChart, DonutChart, SankeyDiagram.

### 1.6 Hierarchical tree traversal is hand-rolled every time

**Problem:** 7 hierarchy samples define a private `Collect(node, list)` or `CollectPartition(node, list)` method — identical recursive pre-order traversal.

**Affected samples:** TidyTree, ClusterDendrogram, CirclePacking, Sunburst, Icicle, IndentedTree (for `TreeNode<T>`, `PackNode<T>`, `PartitionNode<T>`).

**Proposed:** Add `node.Descendants()` returning `IEnumerable<T>` on all hierarchy node types (`TreeNode<T>`, `PackNode<T>`, `PartitionNode<T>`, `TreemapNode<T>`). `TreemapNode` already has `Leaves()` but no general `Descendants()`.

### 1.7 `GetTopBranch` / `GetBranchColor` pattern

**Problem:** 5 hierarchy samples define nearly identical `GetTopBranch(node)` or `GetBranchColor(node)` helpers that walk up to the root's child to determine color index.

**Affected samples:** Treemap, ClusterDendrogram, CirclePacking, Sunburst, Icicle.

**Proposed:** Add `node.Ancestors()` and/or `node.TopAncestor` (child of root) on hierarchy node types. Then coloring becomes:
```csharp
int colorIdx = root.Children.IndexOf(node.TopAncestor);
```

### 1.8 Bezier link path for trees is duplicated

**Problem:** TidyTree and ClusterDendrogram both build identical bezier link paths:
```csharp
double my = (node.Y + child.Y) / 2;
var pb = new PathBuilder(3);
pb.MoveTo(node.X, node.Y);
pb.BezierCurveTo(node.X, my, child.X, my, child.X, child.Y);
```
This exact 4-line block appears in both samples inside a `SelectMany`.

**Proposed:** Add `D3Link(x1, y1, x2, y2, curve: LinkCurve.BezierVertical)` that generates the standard tree link path element directly.

---

## 2. Per-Sample Review

### 2.1 BarChart.cs
- **Good:** Clean use of `BandScale`, nice spread syntax `[.. D3Grid(...), .. bars, ...]`
- ~~**Issue:** Manual `maxVal` loop (line 40-41)~~ DONE — `revenue.Max()`
- ~~**Issue:** `for` loop to build `bars[]` and `xLabels[]` arrays~~ DONE — converted to `.Select()`
- **Issue:** Manually builds axis lines and tick labels instead of using a helper

### 2.2 HorizontalBarChart.cs
- **Good:** Uses `SelectMany` for bars + value labels (line 67-77), LINQ for grid lines
- ~~**Issue:** Manual `maxVal` loop~~ DONE — `populations.Max()`
- **Issue:** No horizontal axis helper, so 6 lines of manual axis code
- **Note:** This is actually one of the better samples for functional style

### 2.3 StackedBarChart.cs
- **Good:** Clean `SelectMany` over series producing bars (lines 87-98)
- ~~**Issue:** Double-nested `foreach` for maxVal discovery~~ DONE — `series.SelectMany().Max()`
- **Issue:** Manual X band labels (lines 106-107)
- ~~**Issue:** Legend inline~~ DONE — D3Legend

### 2.4 GroupedBarChart.cs
- **Good:** Very similar structure to StackedBarChart, clean `SelectMany`
- ~~**Issue:** maxVal and legend~~ DONE — LINQ `.Max()` + D3Legend
- **Issue:** Manual band-label axis
- **Note:** The double `BandScale` for group-within-band is a nice D3 pattern that reads well

### 2.5 DivergingBarChart.cs
- **Good:** `SelectMany` producing bar + value label pairs — ~~now query syntax with 6 `let` bindings~~
- ~~**Issue:** Manual min/max discovery~~ DONE — `scores.Min()/Max()`
- **Issue:** Manual legend at specific pixel positions (horizontal layout, not a D3Legend candidate)

### 2.6 LineChart.cs
- **Good:** Compact, uses `D3Grid` and `D3Axes` successfully
- **Issue:** Manual data array construction with for-loop (lines 42-43) — could be `temps.Select((t, i) => (x: (double)(i+1), y: t)).ToArray()`
- **Issue:** Dot generation uses explicit cast `(Element)` — the `with` on `D3Circle` should already return `Element` compatible type but the cast is needed for the spread. This is a C# limitation but worth noting.

### 2.7 MultiLineChart.cs
- **Good:** Multiple series rendered
- ~~**Issue:** Hand-built data arrays and for-loops for lines, legend, month labels~~ DONE — all 3 loops converted to LINQ `.Select()` + D3Legend
- **Verdict:** Now uses functional style throughout

### 2.8 LineChartMissingData.cs
- **Good:** Interesting use of `SetDefined`, dashed line for gaps, band regions for missing data
- **Issue:** The missing-region bands (lines 82-89) use a clever LINQ chain but the `.Select((r, i) => (r, i)).Where(...).Select(...)` pattern is verbose — a `.SelectWhere` or just `.Index().Where()` would be cleaner when C# gets `Index()`
- **Issue:** Null-path guards for connecting and solid lines

### 2.9 SlopeChart.cs
- **Good:** Elegant `SelectMany` producing line + 2 dots + 2 labels per item (lines 79-91)
- **Good:** Conditional coloring `item.After >= item.Before ? Palette[2] : Palette[3]` reads well
- **Issue:** Grid lines + tick labels produced via `SelectMany` returning 3 elements per tick (lines 67-72) — a `D3GridWithLabels` helper or similar would help
- **Verdict:** One of the cleanest samples overall

### 2.10 CandlestickChart.cs
- **Good:** ~~`SelectMany` per candle~~ now query syntax with 5 `let` bindings — reads very well
- **Issue:** Hand-rolled PRNG for deterministic data — not a DSL issue but noisy
- **Issue:** Manual X labels every 5th day

### 2.11 AreaChart.cs
- **Good:** Uses `D3Grid`, `D3Axes`, `AreaGenerator`, `LineGenerator` with `MonotoneX` curve
- **Issue:** for-loop for dots (lines 74-79) — should be `.Select()`
- ~~**Issue:** Null-path ternaries (lines 84-85)~~ DONE
- **Verdict:** Close to ideal single-expression

### 2.12 StackedAreaChart.cs
- **Good:** LINQ chain: `Enumerable.Range(0, series.Length).Select(...)` to generate area paths
- ~~**Issue:** `.Select(...).Where(path != null).Select((path, si) => ...)` — index bug and null-path noise~~ DONE — inlined D3Path into Select, fixing both

### 2.13 StreamgraphChart.cs
- **Good:** Manual centering offset applied cleanly
- ~~**Issue:** Same `.Select().Where().Select()` index bug as StackedAreaChart~~ DONE
- **Issue:** Legend inline

### 2.14 DifferenceChart.cs
- **Good:** Clean decomposition into greenPts/redPts with LINQ (lines 71-72)
- **Issue:** `areaGreen` and `areaRed` are identical generators constructed twice (lines 75-86) — one instance would suffice
- ~~**Issue:** Heavy null-path ternaries (lines 105-108) — 4 consecutive ternary spreads~~ DONE
- **Issue:** Color strings `"#2ca02c"` / `"#d62728"` parsed repeatedly via `D3Color.Parse` then `Brush` — should just be `Brush("#2ca02c")` directly (which it is in some places but goes through `D3Color.Parse` first in others)

### 2.15 RidgePlot.cs
- **Good:** Complex layout handled in a single `SelectMany` with reverse ordering
- ~~**Issue:** Triple null-path guard block (lines 109-114) producing area + area overlay + line~~ DONE
- **Issue:** The white-background-under-colored-area technique requires two `D3Path` calls for the same path — could a helper `D3PathWithBackground(path, fill, bgFill)` handle this?

### 2.16 PieChart.cs
- **Good:** Clean use of `PieGenerator` + `ArcGenerator` + `D3PathTranslated`
- ~~**Issue:** The `.Select((a, i) => (a, i, pathData: ...)).Where(...).SelectMany(...)` pattern — verbose tuple workaround for null filtering~~ DONE — simplified to direct `SelectMany((a, i) => ...)`
- **Issue:** `new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)` fully qualified (line 56) — should just be `Brush("#ffffff")` or add a `White` constant

### 2.17 DonutChart.cs
- **Good:** Similar to PieChart with `InnerRadius`
- ~~**Issue:** `labelArc.Centroid(...)` called twice for x and y — should destructure~~ DONE — now `var (lx, ly) = labelArc.Centroid(...)`
- ~~**Issue:** Same verbose select/where/selectmany pipeline~~ DONE — simplified to direct `SelectMany((a, i) => ...)`

### 2.18 Scatterplot.cs
- **Good:** Very clean — one of the best samples. Near-ideal functional style.
- **Issue:** Explicit `(Element)` cast in the spread (line 48) — minor C# friction
- **Verdict:** Reference sample for "what good looks like"

### 2.19 BubbleChart.cs
- **Good:** Uses `LinearScale` for radius mapping
- **Issue:** Uses a for-loop to build `bubbles[]` (lines 48-55) when a `.Select()` would be cleaner
- **Verdict:** Should match Scatterplot's functional style

### 2.20 DotPlot.cs
- **Good:** Nested `SelectMany` with spread inside (lines 52-64) — demonstrates complex nesting
- **Issue:** The inner `(Element[])` cast + spread for mixing elements with inner spread (line 57) is a bit awkward but necessary in current C#
- **Verdict:** Good functional style, minor cast noise

### 2.21 Histogram.cs
- **Good:** `BinGenerator` + LINQ `.Select()` for bars (lines 62-69)
- **Issue:** Multi-line local variable computation inside the Select (bx, bw, by, bh) — reads well enough but 4 locals in a lambda is on the edge
- **Verdict:** Clean sample

### 2.22 BoxPlot.cs
- **Good:** `SelectMany` producing 11 elements per box (whiskers, caps, box, median, label)
- **Issue:** 11 elements per group is a *lot* — this is a case where a `D3BoxPlot(cx, bx, boxW, min, q1, median, q3, max, fill, stroke)` composite helper would dramatically simplify the sample
- **Issue:** The box outline is drawn as 4 separate lines (lines 97-101) instead of using `D3Rect` with `Stroke` — but `D3Rect` does support stroke, so the outline lines are redundant
- **Verdict:** Would benefit most from a composite helper

### 2.23 Treemap.cs
- **Good:** Uses `TreemapLayout` + `.Leaves()` cleanly
- **Issue:** Label truncation logic inline — appears in 3 hierarchy samples
- ~~**Issue:** `GetTopFolderIndex` helper~~ DONE — `root.Children.IndexOf(leaf.TopAncestor)`

### 2.24 TidyTree.cs
- **Good:** Two-pass `SelectMany` (links then nodes) reads well
- ~~**Issue:** Manual `Collect` helper~~ DONE — `root.Descendants()`
- ~~**Issue:** Bezier link path construction duplicated with ClusterDendrogram~~ DONE — `D3Link()`
- ~~**Issue:** Label placement mixes `D3Text` with raw `Text(...).Foreground(...).Canvas(...)`~~ DONE — `D3TextCenter()`

### 2.25 ClusterDendrogram.cs
- **Near-identical** structure to TidyTree — ~~same issues~~ all DONE (Descendants, D3Link, TopAncestor)

### 2.26 CirclePacking.cs
- **Good:** `PackLayout` usage is clean
- ~~**Issue:** Manual `CollectPack` + sort by depth~~ DONE — `root.Descendants().OrderBy(n => n.Depth)`
- ~~**Issue:** `GetBranchIndex`~~ DONE — `root.Children.IndexOf(node.TopAncestor)`
- **Issue:** Label truncation + conditional label display logic inline

### 2.27 Sunburst.cs
- **Good:** `PartitionLayout` + `ToPolar` + `ArcGenerator` is a powerful combo
- ~~**Issue:** Manual `CollectPartition`~~ DONE — `root.Descendants()`
- ~~**Issue:** `GetTopBranch`~~ DONE — `root.Children.IndexOf(node.TopAncestor)`
- ~~**Issue:** Deeply nested `SelectMany` with null-path guard~~ DONE — null guard removed
- ~~**Issue:** Falls back to raw `Text(...).Set(tb => ...)`~~ DONE — `D3TextCenter()`

### 2.28 Icicle.cs
- **Good:** Structurally similar to Treemap
- ~~**Issue:** `CollectPartition` + `GetTopBranch`~~ DONE — Descendants + TopAncestor
- **Issue:** Label truncation still inline
- **Issue:** Draws a stroke rect on top of a fill rect — `D3Rect` should support both fill and stroke in one call

### 2.29 IndentedTree.cs
- **Good:** Uses `Stratify` to build from flat data — demonstrates the API well
- ~~**Issue:** `FlattenTree` helper~~ DONE — `root.Descendants()`
- **Issue:** The triangle indicator built with PathBuilder (lines 113-117) — could be a `D3Triangle` or `D3Indicator` helper
- **Issue:** The `.TakeWhile(t => t.y + rowH <= H)` for clipping is a nice functional touch

### 2.30 ForceDirectedGraph.cs
- **Good:** Clean `ForceSimulation` pipeline
- ~~**Issue:** `.Select().SelectMany(e => e)` should be `.SelectMany()`~~ DONE
- ~~**Issue:** Falls back to `Text()...Set(tb => TextAlignment)` for labels~~ DONE — `D3TextCenter()`

### 2.31 ChordDiagram.cs
- **Good:** Clean arc construction with `PathBuilder`, ribbon generation
- **Issue:** The outer arc is built manually with `PathBuilder` (lines 68-72) — could be a `D3Arc(cx, cy, innerR, outerR, startAngle, endAngle)` helper
- **Issue:** Label positioning with manual trig (lines 74-76)

### 2.32 SankeyDiagram.cs
- **Good:** Clean layout pipeline, good LINQ for links and nodes
- **Issue:** The `D3TextRight` usage for right-aligned labels (line 113) with negative offset is awkward — passing `(x - width, y, text, width)` to right-align relative to a node is not intuitive

### 2.33 ArcDiagram.cs
- **Good:** Clean `PathBuilder.Arc` for semicircular connections
- **Good:** Simple functional style throughout
- **Issue:** Minor — label centering uses `.Set(tb => tb.TextAlignment = ...)` which is common enough to warrant `D3TextCenter`

---

## 3. Proposed API Additions (Priority Order)

### P0 — High impact, many samples affected

| Helper | Samples affected | Description |
|--------|-----------------|-------------|
| ~~`node.Descendants()`~~ | ~~7 hierarchy samples~~ | ~~DONE — added to TreeNode; all Collect/Flatten helpers removed~~ |
| ~~`D3Path` accepts null gracefully~~ | ~~12 samples~~ | ~~DONE — null guards removed from all samples~~ |
| ~~`D3Legend(x, y, items)`~~ | ~~11 samples~~ | ~~DONE — migrated 6 vertical legends (StackedBar, GroupedBar, MultiLine, StackedArea, Streamgraph, Difference)~~ |
| `D3BandAxis(band, ...)` | 9 bar/dot samples | X or Y axis tick labels at band centers |
| `D3Rect` with both Fill and Stroke | Icicle, BoxPlot | Already partially supported but samples don't use it; may just need documentation |

### P1 — Medium impact, cleaner expression

| Helper | Samples affected | Description |
|--------|-----------------|-------------|
| ~~`D3Link(x1,y1,x2,y2)`~~ | ~~TidyTree, ClusterDendrogram~~ | ~~DONE — migrated both tree samples~~ |
| ~~`node.TopAncestor`~~ | ~~5 hierarchy samples~~ | ~~DONE — added to all 4 node types; all GetTopBranch/GetBranchColor helpers removed~~ |
| `StackSeries[].MaxY()` | 4 stacked chart samples | Not needed — replaced with `series.SelectMany().Max()` LINQ one-liner |
| ~~`D3TextCenter(x, y, text, width, ...)`~~ | ~~ForceGraph, ArcDiagram, Sunburst, TidyTree, CirclePacking~~ | ~~DONE — migrated all .Set(tb => TextAlignment) patterns~~ |
| `D3BoxPlot(...)` composite | BoxPlot | Render a complete box-and-whisker from 5-number summary |

### P2 — Nice to have

| Helper | Samples affected | Description |
|--------|-----------------|-------------|
| `D3Arc(cx,cy,r0,r1,a0,a1)` | ChordDiagram | Shortcut for arc sector path element |
| Label truncation utility | Treemap, CirclePacking, Icicle | `TruncateLabel(text, maxPx, charWidth=6)` |
| `D3PathWithBackground` | RidgePlot | Two-layer path with bg fill underneath |
| Improve `D3Axes` to accept band scales | 9 samples | Eliminate manual band-axis construction |

---

## 4. ~~Bug: Index Shift After `.Where()` in Stacked Charts~~ DONE

**Resolved:** Inlined `D3Path` call into the first `.Select()` in both StackedAreaChart and StreamgraphChart, eliminating the `.Where().Select()` chain entirely. The series index `si` is now captured before any filtering, fixing the latent color-shift bug.

---

## 5. Style Observations

### What reads well today
- `with { Fill = ..., RadiusX = 2 }` on D3Rect/D3Circle/D3Line — record-style property overrides are ergonomic
- Collection expression spread `[.. grid, .. bars, .. axes]` — excellent for composing flat element lists
- `SelectMany` producing `Element[]` — the workhorse for "N visual elements per data item"
- `BandScale.Create(labels).SetRange(0, plotW).SetPaddingInner(0.2)` — fluent builder reads naturally

### What doesn't read well
- `(Element)` casts required for spread in some contexts — C# limitation, not fixable by DSL
- `.Set(tb => tb.TextAlignment = ...)` callbacks for properties not on `TextElement` — add `.TextAlignment()` fluent method
- Falling back to raw `Text(...).Foreground(...).Canvas(...)` when `D3Text` doesn't support centering — DSL gap
- `Array.Empty<Element>()` in ternary spreads — if `D3Path` handles null, most of these vanish
- Tuple intermediates `(a, i, pathData: ...)` for null-filtering — functional but hard to scan

### The ideal shape
The best samples (Scatterplot, Histogram, SlopeChart) show the ideal:
```csharp
return D3Canvas(W, H,
    [.. D3Grid(ys, left, pw),
     .. D3Axes(xs, ys, left, top, pw, ph),
     .. data.Select(d => D3Circle(xs.Map(d.x), ys.Map(d.y), 4) with { Fill = fill }),
     D3Text(left, 6, "Title", 14, Gray(40))]
);
```
One return, one collection expression, data transforms via LINQ, all primitives from the DSL. The API additions above aim to bring the other 25+ samples closer to this shape.

---

## 6. ~~`GalleryHelpers.G` vs `D3` Static Class — Duplication~~ DONE

**Resolved:** Deleted the `G` class from `GalleryHelpers.cs`. Only the `GallerySample` base class remains.

---

## 7. Query Comprehension Syntax (`from`/`let`/`where`/`select`)

Many samples use multi-statement lambdas inside `.SelectMany()` with 3-10 local
variable bindings before returning elements. These are prime candidates for C#
query comprehension syntax, where `let` makes intermediate computations named and
scannable, and `where` replaces the awkward `.Select(tuple).Where().Select()` pattern.

### 7.1 Where query syntax wins clearly

**Rule of thumb:** If a lambda has 3+ `let`-worthy locals before the final
`select`/element-return, query syntax is cleaner. If it's a simple 1-line
projection, method syntax (`.Select(...)`) is better.

#### BarChart — for-loop -> query

Current (lines 50-57):
```csharp
var bars = new Element[months.Length];
for (int i = 0; i < months.Length; i++)
{
    double x = left + band.Map(months[i]);
    double barH = plotH - ys.Map(revenue[i]);
    double y = top + ys.Map(revenue[i]);
    bars[i] = D3Rect(x, y, band.Bandwidth, barH) with { Fill = fill, RadiusX = 2, RadiusY = 2 };
}
```

Proposed:
```csharp
var bars =
    from i in Enumerable.Range(0, months.Length)
    let x = left + band.Map(months[i])
    let barH = plotH - ys.Map(revenue[i])
    let y = top + ys.Map(revenue[i])
    select D3Rect(x, y, band.Bandwidth, barH) with { Fill = fill, RadiusX = 2, RadiusY = 2 };
```
Three named intermediate values, one final projection. The intent is immediately obvious.

#### MultiLineChart — for-loop -> query (biggest win)

Current (lines 56-68): Three nested for-loops building `lines[]`, `monthLabels[]`, `legend[]`.

Proposed:
```csharp
var lines =
    from s in Enumerable.Range(0, allSeries.Length)
    let data = allSeries[s].Select((v, i) => (x: (double)i, y: v)).ToArray()
    let line = LineGenerator.Create<(double x, double y)>(d => xs.Map(d.x), d => ys.Map(d.y))
    let pathData = line.Generate(data)
    select D3Path(pathData, stroke: Brush(colors[s]), strokeWidth: 2);
```
Replaces an 11-line for-loop with 5 lines. The `let` chain reads as a data pipeline.

#### DivergingBarChart — 5-local SelectMany -> query

Current (lines 90-103): Lambda with `y`, `v`, `barStart`, `barWidth`, `fill`, `labelX`.

Proposed:
```csharp
.. (from entry in items.Zip(scores, (item, score) => (item, score))
    let y = top + band.Map(entry.item)
    let v = entry.score
    let barStart = v >= 0 ? zeroX : left + xs.Map(v)
    let barWidth = v >= 0 ? xs.Map(v) - xs.Map(0) : xs.Map(0) - xs.Map(v)
    let fill = v >= 0 ? posBrush : negBrush
    let labelX = v >= 0 ? barStart + barWidth + 4 : barStart - 30
    from el in new Element[] {
        D3Rect(barStart, y, barWidth, band.Bandwidth) with { Fill = fill, RadiusX = 2, RadiusY = 2 },
        D3Text(labelX, y + band.Bandwidth / 2 - 7, (v >= 0 ? "+" : "") + v.ToString("F0"), fontSize: 10, foreground: Gray(60)),
    }
    select el)
```
Six named computations instead of a dense multi-line lambda. The `from ... from ...` for SelectMany is slightly awkward but the `let` bindings dominate readability.

#### CandlestickChart — 5-local SelectMany -> query

Current (lines 83-96): Lambda with `cx`, `bullish`, `brush`, `bodyTop`, `bodyBot`, `bodyH`.

Proposed:
```csharp
.. (from c in candles.Select((c, i) => (c, i))
    let cx = xs.Map(c.i)
    let bullish = c.c.Close >= c.c.Open
    let brush = bullish ? bullBrush : bearBrush
    let bodyTop = ys.Map(Math.Max(c.c.Open, c.c.Close))
    let bodyH = Math.Max(ys.Map(Math.Min(c.c.Open, c.c.Close)) - bodyTop, 1)
    from el in new Element[] {
        D3Line(cx, ys.Map(c.c.High), cx, ys.Map(c.c.Low)) with { Stroke = brush, StrokeThickness = 1.5 },
        D3Rect(cx - barW / 2, bodyTop, barW, bodyH) with { Fill = brush },
    }
    select el)
```

#### BoxPlot — 12-local lambda -> query (most dramatic win)

The `SelectMany` at line 67 has 12 local variables. Query syntax turns it from
a wall of code into a readable pipeline:
```csharp
.. (from entry in groupData.Select((sorted, g) => (sorted, g))
    let min = entry.sorted[0]
    let max = entry.sorted[^1]
    let q1 = D3Statistics.QuantileSorted(entry.sorted, 0.25)
    let median = D3Statistics.QuantileSorted(entry.sorted, 0.5)
    let q3 = D3Statistics.QuantileSorted(entry.sorted, 0.75)
    let cx = left + entry.g * groupWidth + groupWidth / 2
    let bx = cx - boxWidth / 2
    let color = Palette[entry.g % Palette.Length]
    let fill = Brush(color, opacity: 0.35)
    let stroke = Brush(color)
    let boxY = ys.Map(q3)
    let boxH = ys.Map(q1) - ys.Map(q3)
    from el in new Element[] { /* 11 elements */ }
    select el)
```

### 7.2 Where query syntax fixes the `.Where()` index bug

**StackedAreaChart** and **StreamgraphChart** both have the `.Where().Select()` index
shift bug from section 4. Query syntax fixes it naturally because `si` is bound
before the `where`:

Current (buggy):
```csharp
.. Enumerable.Range(0, series.Length)
    .Select(si => { ...; return area.Generate(pts); })
    .Where(path => path != null)
    .Select((path, si) => D3Path(path!, fill: Brush(Palette[si], 0.75)))
```

Proposed (correct):
```csharp
.. (from si in Enumerable.Range(0, series.Length)
    let s = series[si]
    let pts = s.Points.Select((p, j) => (x: (double)j, y0: p.Y0, y1: p.Y1)).ToArray()
    let area = AreaGenerator.Create<(double x, double y0, double y1)>(
        d => xScale.Map(d.x), d => yScale.Map(d.y0), d => yScale.Map(d.y1))
    let path = area.Generate(pts)
    where path != null
    select D3Path(path, fill: Brush(Palette[si], 0.75)))
```
The `si` is captured in the `from`, so `where` doesn't shift it.

### 7.3 Where query syntax cleans up null-path + label pipelines

**PieChart** (lines 47-61): The `.Select(tuple).Where().SelectMany()` pattern
becomes:
```csharp
.. (from a in arcs.Select((a, i) => (a, i))
    let pathData = arc.Generate(a.a)
    where pathData != null
    let (ox, oy) = labelArc.Centroid(a.a.StartAngle, a.a.EndAngle)
    from el in new Element[] {
        D3PathTranslated(pathData, cx, cy,
            fill: Brush(Palette[a.i % Palette.Length]),
            stroke: Brush("#ffffff"), strokeWidth: 1),
        D3Text(cx + ox - 20, cy + oy - 7,
            $"{a.a.Data.Name} ({a.a.Data.Value}%)", fontSize: 11,
            foreground: Brush(Palette[a.i % Palette.Length])),
    }
    select el)
```
Compare to the current `.Select((a, i) => (a, i, pathData: arc.Generate(a))).Where(t => t.pathData != null).SelectMany(t => { ... })` — the query version is substantially cleaner.

**DonutChart** (lines 53-62): Same pattern. Additionally, `let (lx, ly) = labelArc.Centroid(...)` prevents the current bug where `Centroid` is called twice (lines 59-60).

**Sankey** (lines 93-102):
```csharp
.. (from link in graph.Links
    let pathData = SankeyLayout.LinkPath(link)
    where pathData != null
    let ci = nodeColors.GetValueOrDefault(link.SourceId, 0)
    select D3PathTranslated(pathData, pad, pad,
        fill: Brush(Palette[ci % Palette.Length], opacity: 0.35)))
```

**LineChartMissingData** (lines 82-89): The `.Select((r, i) => (r, i)).Where(...).Select(...)` for missing bands:
```csharp
.. (from t in readings.Select((r, i) => (r, i))
    where double.IsNaN(t.r)
    let x0 = xs.Map(t.i + 0.5)
    let x1 = xs.Map(t.i + 1.5)
    select D3Rect(x0, top, x1 - x0, height) with { Fill = bandBrush })
```

### 7.4 Where method syntax is still better

- **Simple single-expression projections** — `.Select(d => D3Circle(...))` doesn't need `from`/`select` ceremony
- **No intermediate values needed** — Scatterplot, Histogram, simple bar rendering
- **Grid/axis generation** — `ys.Ticks(5).Select(t => D3Line(...))` is already clean
- **Legend blocks** — `keys.SelectMany((key, i) => new Element[] { ... })` is fine as-is (and better with a `D3Legend` helper anyway)

### 7.5 Summary of query syntax candidates

| Sample | Current pattern | `let` count | Status |
|--------|----------------|------------|--------|
| **BarChart** | ~~for-loop~~ → query | 3 | DONE |
| **MultiLineChart** | ~~3 for-loops~~ → LINQ | 4 per loop | DONE (LINQ, not query — already clean) |
| **DivergingBarChart** | ~~6-local SelectMany~~ → query | 6 | DONE |
| **CandlestickChart** | ~~5-local SelectMany~~ → query | 5 | DONE |
| **BoxPlot** | ~~12-local SelectMany~~ → query | 12 | DONE |
| **PieChart** | ~~tuple+Where+SelectMany~~ | 2 + where | DONE (simplified to direct SelectMany, no query needed) |
| **DonutChart** | ~~tuple+Where+SelectMany~~ | 2 + where | DONE (simplified + Centroid fix) |
| **StackedAreaChart** | ~~Select+Where+Select (bug)~~ | 3 + where | DONE (inlined D3Path, no query needed) |
| **StreamgraphChart** | ~~Select+Where+Select (bug)~~ | 3 + where | DONE (inlined D3Path, no query needed) |
| **SankeyDiagram links** | ~~tuple+Where+Select~~ | 2 + where | DONE (simplified to direct Select) |
| **LineChartMissingData** | tuple+Where+Select | 2 + where | Skipped — already clean enough |
| **HorizontalBarChart** | ~~3-local SelectMany~~ → query | 3 | DONE |
| **SlopeChart** | 2-local SelectMany | 2 | Skipped — already clean |
| **Scatterplot** | .Select(d => ...) | 0 | No change — already ideal |

---

## 8. Named Parameters for Sample Readability

The DSL has several methods that accept multiple `double` arguments in sequence.
When reading sample code, it's often unclear what a bare numeric literal means.
Named parameters at *call sites* in the samples can fix this without any API change.

### 8.1 High-impact: ambiguous double sequences

#### `Gray(byte v, byte a = 255)` — call sites should use `alpha:`

Current (appears ~50 times across samples):
```csharp
Gray(100, 180)    // What is 100? What is 180?
Gray(128, 40)     // Is 40 the brightness or the alpha?
```

Proposed:
```csharp
Gray(100, alpha: 180)
Gray(128, alpha: 40)
```
The first argument is clearly the gray value by position. The second needs naming.
**Every two-arg call to `Gray()` in every sample should use `alpha:`.**

#### `Brush(D3Color c, double opacity)` — call sites should use `opacity:`

Current:
```csharp
Brush(Palette[0], 0.85)    // What is 0.85?
Brush(color, 0.35)
```

Proposed:
```csharp
Brush(Palette[0], opacity: 0.85)
Brush(color, opacity: 0.35)
```
Bare floating-point literals next to colors need the name. **Every two-arg
`Brush(color, double)` call should use `opacity:`.**

#### `D3TextRight(double x, double y, string text, double width, double fontSize, Brush? foreground)` — confusing signature

Current (StackedBarChart line 102):
```csharp
D3TextRight(0, ysScreen.Map(t) - 7, Fmt(t), left - 6, 10, axisBrush)
```
Six positional args. Which double is the width? Which is fontSize? This is the
least readable call in the entire gallery.

Proposed:
```csharp
D3TextRight(0, ysScreen.Map(t) - 7, Fmt(t), width: left - 6, fontSize: 10, foreground: axisBrush)
```

#### `D3Text(double x, double y, string text, double fontSize, Brush? foreground)` — name the trailing args

Current:
```csharp
D3Text(left, 4, "Monthly Revenue ($k)", 13, Gray(40))
D3Text(xs.Map(t) - 12, top + plotH + 6, Fmt(t), 10, axisBrush)
```

The x, y, text triple is positionally clear. But when fontSize and foreground are
both present, naming helps:
```csharp
D3Text(left, 4, "Monthly Revenue ($k)", fontSize: 13, foreground: Gray(40))
D3Text(xs.Map(t) - 12, top + plotH + 6, Fmt(t), fontSize: 10, foreground: axisBrush)
```

**Guideline:** Name `fontSize:` and `foreground:` when both are present. When only
fontSize is passed (common with the default foreground), it's optional but helpful
for literal values like `13` or `10`.

### 8.2 Medium-impact: helpful when values are literals

#### `D3Circle(double cx, double cy, double r)` — name `r:` for literal radii

When the radius is a variable or expression it's clear from context:
```csharp
D3Circle(xs.Map(p.x), ys.Map(p.y), rs.Map(p.size))  // fine — rs.Map() is clearly radius
```

When it's a magic number, naming helps:
```csharp
D3Circle(xs.Map(d.x), ys.Map(d.y), r: 3)    // clearer
D3Circle(nodeX[i], baseline, r: 8)            // clearer
```

#### `D3Path` and `D3PathTranslated` — already mostly named

These already use named args at call sites (`stroke:`, `fill:`, `strokeWidth:`).
Keep doing this. The one gap is `D3PathTranslated` where `translateX/translateY`
are positional — but they're usually `cx, cy` which is self-documenting.

### 8.3 Low-impact: clear from position/convention

These are fine as positional:
- `D3Rect(x, y, width, height)` — standard rect convention
- `D3Line(x1, y1, x2, y2)` — standard line convention
- `D3Canvas(width, height, children)` — obvious
- `LinearScale([domain], [range])` — two-array pattern is clear
- `BandScale.Create(labels).SetRange(0, plotW)` — fluent API, method names carry meaning

### 8.4 Specific call sites to fix across all samples

| Pattern | Count | Fix |
|---------|-------|-----|
| `Gray(v, a)` with two args | ~30 | Add `alpha:` to second arg |
| `Brush(color, opacity)` with two args | ~25 | Add `opacity:` to second arg |
| `D3TextRight(..., width, fontSize, foreground)` | ~15 | Name `width:`, `fontSize:`, `foreground:` |
| `D3Text(..., fontSize, foreground)` with both | ~40 | Name `fontSize:`, `foreground:` |
| `D3Circle(x, y, literal)` | ~20 | Name `r:` when radius is a literal number |

---

## 10. Functional Generator Pattern

Generators (LineGenerator, AreaGenerator, ArcGenerator, PieGenerator, etc.) required a
multi-step imperative pattern: create instance → configure with fluent setters → call
`.Generate(data)` → pass result to `D3Path()`. This broke the "single expression per
visual element" ideal that the rest of the DSL achieves.

### 10.1 The problem

A simple line chart required 3 statements just for the line:
```csharp
var line = LineGenerator.Create<(double x, double y)>(d => xs.Map(d.x), d => ys.Map(d.y));
var pathData = line.Generate(data);
D3Path(pathData, stroke: lineBrush, strokeWidth: 2)
```

An area chart with a line overlay needed 8 lines of generator plumbing. Pie charts needed
3 generator instances (pie + arc + label arc).

### 10.2 Solution: DSL-level one-shot helpers

Added to `D3` class (imported via `using static Duct.D3.Charts.D3`):

| Helper | Signature | Returns |
|--------|-----------|---------|
| `D3LinePath<T>` | `(data, x, y, stroke?, strokeWidth?, curve?, defined?)` | `PathElement` |
| `D3AreaPath<T>` | `(data, x, y0, y1, fill?, stroke?, strokeWidth?, curve?)` | `PathElement` |
| `D3ArcPath` | `(startAngle, endAngle, cx, cy, innerRadius?, outerRadius?, padAngle?, fill?, stroke?, strokeWidth?)` | `PathElement` |
| `D3Pie<T>` | `(data, value, cx, cy, outerRadius?, innerRadius?, padAngle?, sort?, stroke?, strokeWidth?)` | `Element[]` |

Each collapses the full create → configure → generate → wrap pipeline into a single expression.

### 10.3 Static convenience methods on generators

For cases where intermediate data is needed (e.g., label positioning on pie slices):

| Method | Purpose |
|--------|---------|
| `PieGenerator.Generate<T>(data, value, sort?, padAngle?)` | One-shot pie arc computation — no instance construction |
| `ArcGenerator.Centroid(startAngle, endAngle, innerRadius?, outerRadius?)` | Static centroid — no instance needed |

### 10.4 Before/after examples

**LineChart** — 3 lines → 1 expression:
```csharp
// Before:
var line = LineGenerator.Create<(double x, double y)>(d => xs.Map(d.x), d => ys.Map(d.y));
var pathData = line.Generate(data);
D3Path(pathData, stroke: lineBrush, strokeWidth: 2)

// After:
D3LinePath(data, x: d => xs.Map(d.x), y: d => ys.Map(d.y), stroke: lineBrush, strokeWidth: 2)
```

**AreaChart** — 8 lines → 2 expressions:
```csharp
// Before:
var area = AreaGenerator.Create<(double x, double y)>(
    d => xScale.Map(d.x), d => yScale.Map(0), d => yScale.Map(d.y));
string? areaPath = area.Generate(data);
var line = LineGenerator.Create<(double x, double y)>(d => xScale.Map(d.x), d => yScale.Map(d.y))
    .SetCurve(D3Curve.MonotoneX);
string? linePath = line.Generate(data);
D3Path(areaPath, fill: Brush(Palette[0], opacity: 0.3)),
D3Path(linePath, stroke: Brush(Palette[0]), strokeWidth: 2),

// After:
D3AreaPath(data, x: d => xScale.Map(d.x), y0: d => yScale.Map(0), y1: d => yScale.Map(d.y),
    fill: Brush(Palette[0], opacity: 0.3)),
D3LinePath(data, x: d => xScale.Map(d.x), y: d => yScale.Map(d.y),
    stroke: Brush(Palette[0]), strokeWidth: 2, curve: D3Curve.MonotoneX),
```

**PieChart** — 3 generator instances + 5-line setup → 1-line setup:
```csharp
// Before:
var pie = PieGenerator.Create<T>(d => d.Value).SetSortValues(null);
var arc = new ArcGenerator().SetOuterRadius(150).SetInnerRadius(0);
var arcs = pie.Generate(data);
var labelArc = new ArcGenerator().SetOuterRadius(180).SetInnerRadius(180);
// then: labelArc.Centroid(...), D3PathTranslated(arc.Generate(a), cx, cy, ...)

// After:
var arcs = PieGenerator.Generate(data, value: d => d.Value, sort: false);
// then: ArcGenerator.Centroid(..., innerRadius: 180, outerRadius: 180),
//       D3ArcPath(a.StartAngle, a.EndAngle, cx, cy, outerRadius: 150, fill: ...)
```

### 10.5 When to use which

- **Single path from data** → `D3LinePath` / `D3AreaPath` (covers 90% of line/area chart cases)
- **Arc sector** → `D3ArcPath` (pie slices, sunburst nodes)
- **Simple pie with palette colors** → `D3Pie` (no labels needed)
- **Pie with custom labels** → `PieGenerator.Generate` + `D3ArcPath` + `ArcGenerator.Centroid`
- **Complex configuration** (e.g., `SetDefined`, `.Set()` on result) → `D3LinePath` supports `defined:` param; for `.Set()` chaining, the result is still a `PathElement` so `.Set()` works

### 10.6 Samples migrated

| Sample | Generators replaced | Lines saved |
|--------|-------------------|-------------|
| LineChart | LineGenerator | 2 |
| MultiLineChart | LineGenerator | 2 |
| AreaChart | AreaGenerator + LineGenerator | 6 |
| LineChartMissingData | 2× LineGenerator | 4 |
| DifferenceChart | 2× AreaGenerator + 2× LineGenerator | 12 |
| RidgePlot | AreaGenerator + LineGenerator (per row) | 8 |
| StackedAreaChart | AreaGenerator (per series) | 5 |
| StreamgraphChart | AreaGenerator (per series) | 5 |
| PieChart | PieGenerator + 2× ArcGenerator | 4 |
| DonutChart | PieGenerator + 2× ArcGenerator | 5 |
| Sunburst | ArcGenerator | 2 |
| ChordDiagram | `.Where().Select()` null guard | 2 |

---

## 9. Summary of Recommended Changes

1. ~~**Make `D3Path(null, ...)` return a benign no-op element**~~ DONE — doc clarified, null guards removed from all affected samples
2. ~~**Add `Descendants()` to all hierarchy node types**~~ DONE — added to TreeNode (others already had it); eliminated 7 Collect/Flatten methods
3. ~~**Add `D3Legend` helper**~~ DONE — migrated 6 vertical legends; horizontal legends not applicable
4. **Add `D3BandAxis` helper** — eliminates manual band tick labels in 9 samples
5. ~~**Add `D3Link` for standard tree bezier links**~~ DONE — migrated TidyTree and ClusterDendrogram
6. ~~**Add `D3TextCenter`**~~ DONE — migrated ForceGraph, ArcDiagram, TidyTree, Sunburst, CirclePacking
7. ~~**Add `TopAncestor` property**~~ DONE — added to all 4 hierarchy node types; eliminated 5 GetTopBranch/GetBranchColor helpers
8. ~~**Fix `.Where().Select()` index bug** in StackedAreaChart and StreamgraphChart~~ DONE — inlined D3Path into Select
9. ~~**Delete or deprecate `GalleryHelpers.G`** class~~ DONE — deleted the G class
10. ~~**Rewrite MultiLineChart**~~ DONE — converted all 3 for-loops to LINQ + D3Legend
11. ~~**Convert 10+ samples to query syntax**~~ DONE — converted BoxPlot (12 let), DivergingBarChart (6), CandlestickChart (5), BarChart (3), HorizontalBarChart (3) to query syntax; others were simplified via direct LINQ in earlier passes
12. ~~**Add named parameters at call sites**~~ DONE — `Gray(alpha:)`, `Brush(opacity:)` sweep across all samples
13. ~~**Functional generator helpers**~~ DONE — `D3LinePath`, `D3AreaPath`, `D3ArcPath`, `D3Pie` DSL helpers + `PieGenerator.Generate` and `ArcGenerator.Centroid` static methods; migrated 12 samples

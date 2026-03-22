// Duct Test App — A single-file WinUI 3 application using the Duct functional projection.
// No XAML. No data binding. No resources. No templates. Just C#.

using System.Diagnostics;
using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using static Duct.UI;

if (args.Contains("--self-test"))
{
    SelfTestRunner.RunAll();
}
else
{
    DuctApp.Run<DemoApp>("Duct Demo", width: 1200, height: 800);
}

// ─── Root application component ────────────────────────────────────────────────

class DemoApp : Component
{
    public override Element Render()
    {
        var (currentTab, setTab) = UseState("Counter");

        return VStack(12,
            // Tab bar
            HStack(8,
                TabButton("Counter", currentTab, setTab),
                TabButton("Todo List", currentTab, setTab),
                TabButton("Conditional UI", currentTab, setTab),
                TabButton("Form", currentTab, setTab),
                TabButton("Dynamic List", currentTab, setTab),
                TabButton("Perf Stress", currentTab, setTab),
                TabButton("Virtualization", currentTab, setTab),
                TabButton("Flyout", currentTab, setTab),
                TabButton("DataTemplate", currentTab, setTab)
            ).Margin(16, 16, 16, 0),

            // Content area with padding
            Border(
                currentTab switch
                {
                    "Counter" => Component<CounterDemo>(),
                    "Todo List" => Component<TodoDemo>(),
                    "Conditional UI" => Component<ConditionalDemo>(),
                    "Form" => Component<FormDemo>(),
                    "Dynamic List" => Component<DynamicListDemo>(),
                    "Perf Stress" => Component<PerfStressDemo>(),
                    "Virtualization" => Component<VirtualizationDemo>(),
                    "Flyout" => Component<FlyoutDemo>(),
                    "DataTemplate" => Component<DataTemplateDemo>(),
                    _ => Text("Select a tab")
                }
            ).Padding(24).Margin(16)
        );
    }

    static Element TabButton(string label, string current, Action<string> setCurrent) =>
        Button(label, () => setCurrent(label))
            .Disabled(label == current);
}

// ─── Counter demo ──────────────────────────────────────────────────────────────

class CounterDemo : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);
        var (step, setStep) = UseState(1);

        return VStack(12,
            Heading("Counter"),
            Text($"Current count: {count}").FontSize(24).SemiBold(),

            HStack(8,
                Button($"- {step}", () => setCount(count - step)),
                Button("Reset", () => setCount(0)).Disabled(count == 0),
                Button($"+ {step}", () => setCount(count + step))
            ),

            HStack(8,
                Text("Step size:"),
                Slider(step, 1, 10, v => setStep((int)v)).Width(200),
                Text($"{step}")
            ),

            // Conditional rendering — shows different messages based on count
            count switch
            {
                0 => Text("Try clicking the buttons!").Opacity(0.6),
                > 0 and < 10 => Text("Going up..."),
                >= 10 and < 50 => Text("Getting bigger!").SemiBold(),
                >= 50 => Text("That's a LOT!").Bold().FontSize(20),
                < 0 and > -10 => Text("Going negative..."),
                _ => Text("Way down there!").Bold()
            }
        );
    }
}

// ─── Todo list demo ────────────────────────────────────────────────────────────

class TodoDemo : Component
{
    record TodoItem(string Text, bool Done);

    public override Element Render()
    {
        var (items, updateItems) = UseReducer(new List<TodoItem>
        {
            new("Build Duct library", true),
            new("Write test app", true),
            new("Add more features", false),
        });
        var (newText, setNewText) = UseState("");

        var doneCount = items.Count(i => i.Done);
        var totalCount = items.Count;

        return VStack(12,
            Heading("Todo List"),
            Text($"{doneCount}/{totalCount} completed"),

            // Add new item
            HStack(8,
                TextField(newText, setNewText, placeholder: "What needs to be done?").Width(300),
                Button("Add", () =>
                {
                    if (!string.IsNullOrWhiteSpace(newText))
                    {
                        updateItems(list => [.. list, new TodoItem(newText.Trim(), false)]);
                        setNewText("");
                    }
                }).Disabled(string.IsNullOrWhiteSpace(newText))
            ),

            // List of items
            VStack(4,
                items.Select((item, index) =>
                    HStack(8,
                        CheckBox(item.Done, done =>
                            updateItems(list =>
                            {
                                var copy = new List<TodoItem>(list);
                                copy[index] = item with { Done = done };
                                return copy;
                            }),
                            label: item.Text
                        ),
                        Button("×", () =>
                            updateItems(list =>
                            {
                                var copy = new List<TodoItem>(list);
                                copy.RemoveAt(index);
                                return copy;
                            })
                        )
                    ).WithKey($"todo-{index}")
                ).ToArray()
            ),

            // Conditional: show "All done!" when everything is checked
            When(totalCount > 0 && doneCount == totalCount,
                () => Text("All done! 🎉").Bold().FontSize(18))
        );
    }
}

// ─── Conditional UI demo ───────────────────────────────────────────────────────
// Shows how plain C# code (if, switch, ternary) drives what gets rendered.
// The checkbox toggles which sub-tree is in the visual tree — the reconciler
// handles mounting/unmounting the different branches automatically.

class ConditionalDemo : Component
{
    enum ViewMode { Simple, Detailed, Custom }

    public override Element Render()
    {
        var (showAdvanced, setShowAdvanced) = UseState(false);
        var (enableFeatureA, setFeatureA) = UseState(false);
        var (enableFeatureB, setFeatureB) = UseState(false);
        var (viewMode, setViewMode) = UseReducer(ViewMode.Simple);
        var (itemCount, setItemCount) = UseState(3);

        return ScrollView(VStack(16,
            Heading("Conditional UI"),
            Text("Every piece of UI below is driven by plain C# expressions."),
            Text("Check the boxes and watch entire sub-trees appear and disappear."),

            // ── 1. Simple if/else via checkbox ──────────────────────────
            SubHeading("1. Checkbox toggles a sub-tree"),
            CheckBox(showAdvanced, setShowAdvanced, label: "Show advanced options"),

            // This is just a C# ternary — when false, the whole VStack is gone
            showAdvanced
                ? Border(
                    VStack(8,
                        Text("Advanced Settings").SemiBold(),
                        CheckBox(enableFeatureA, setFeatureA, label: "Enable Feature A"),
                        CheckBox(enableFeatureB, setFeatureB, label: "Enable Feature B"),

                        // Nested conditionals — each feature shows its own config
                        enableFeatureA
                            ? Border(
                                VStack(4,
                                    Text("Feature A Configuration").SemiBold(),
                                    Text("This sub-tree only exists when Feature A is checked."),
                                    Slider(50, 0, 100).Width(200)
                                )
                              ).CornerRadius(4).Background("#e8f5e9").Padding(12)
                            : null,

                        enableFeatureB
                            ? Border(
                                VStack(4,
                                    Text("Feature B Configuration").SemiBold(),
                                    Text("This sub-tree only exists when Feature B is checked."),
                                    ToggleSwitch(false, null, onContent: "On", offContent: "Off")
                                )
                              ).CornerRadius(4).Background("#e3f2fd").Padding(12)
                            : null
                    )
                  ).CornerRadius(8).Background("#f5f5f5").Padding(16)
                : Text("Check the box above to reveal advanced options.").Opacity(0.6),

            // ── 2. Switch expression → completely different sub-trees ───
            SubHeading("2. Switch expression picks a sub-tree"),
            HStack(8,
                Button("Simple", () => setViewMode(_ => ViewMode.Simple))
                    .Disabled(viewMode == ViewMode.Simple),
                Button("Detailed", () => setViewMode(_ => ViewMode.Detailed))
                    .Disabled(viewMode == ViewMode.Detailed),
                Button("Custom", () => setViewMode(_ => ViewMode.Custom))
                    .Disabled(viewMode == ViewMode.Custom)
            ),

            // Each branch renders a COMPLETELY different control tree
            viewMode switch
            {
                ViewMode.Simple => VStack(4,
                    Text("Simple view — just a summary."),
                    Text($"{itemCount} items in the list.")
                ),

                ViewMode.Detailed => VStack(4,
                    Text("Detailed view — shows every item:").SemiBold(),
                    ForEach(
                        Enumerable.Range(1, itemCount),
                        i => HStack(4,
                            Text($"Item {i}").Width(80),
                            Progress(i * 100.0 / itemCount).Width(150)
                        )
                    )
                ),

                ViewMode.Custom => VStack(8,
                    Text("Custom view — configure the list:").SemiBold(),
                    HStack(8,
                        Text("Item count:"),
                        Slider(itemCount, 1, 10, v => setItemCount((int)v)).Width(200),
                        Text($"{itemCount}")
                    ),
                    ForEach(
                        Enumerable.Range(1, itemCount),
                        i => Border(
                            Text($"Custom item {i}")
                        ).CornerRadius(4).Background("#fff3e0").Padding(8, 4)
                    )
                ),

                _ => Empty()
            },

            // ── 3. Inline computed UI ───────────────────────────────────
            SubHeading("3. Computed UI from expressions"),
            Text("The UI below is generated by a C# expression — no templates needed:"),

            VStack(4,
                // A simple computed summary based on current state
                Text($"Advanced: {(showAdvanced ? "ON" : "OFF")}, " +
                     $"Features: {(enableFeatureA ? "A" : "")}{(enableFeatureB ? "B" : "")}{(!enableFeatureA && !enableFeatureB ? "none" : "")}, " +
                     $"View: {viewMode}")
                    .Opacity(0.7),

                // Conditional warning
                When(showAdvanced && enableFeatureA && enableFeatureB,
                    () => Border(
                        Text("Warning: Both features enabled simultaneously may cause conflicts.")
                    ).CornerRadius(4).Background("#fff9c4").Padding(12)
                )
            )
        ));
    }
}

// ─── Form demo ─────────────────────────────────────────────────────────────────

class FormDemo : Component
{
    public override Element Render()
    {
        var (name, setName) = UseState("");
        var (email, setEmail) = UseState("");
        var (agreeToTerms, setAgree) = UseState(false);
        var (darkMode, setDarkMode) = UseState(false);
        var (fontSize, setFontSize) = UseState(14.0);
        var (submitted, setSubmitted) = UseState(false);

        if (submitted)
        {
            return VStack(12,
                Heading("Form Submitted!"),
                Text($"Name: {name}"),
                Text($"Email: {email}"),
                Text($"Dark mode: {(darkMode ? "Yes" : "No")}"),
                Text($"Font size: {fontSize:F0}px"),
                Button("Back", () => setSubmitted(false))
            );
        }

        var isValid = !string.IsNullOrWhiteSpace(name)
            && !string.IsNullOrWhiteSpace(email)
            && agreeToTerms;

        return VStack(16,
            Heading("Registration Form"),

            VStack(8,
                Text("Name"),
                TextField(name, setName, placeholder: "Enter your name").Width(300)
            ),

            VStack(8,
                Text("Email"),
                TextField(email, setEmail, placeholder: "you@example.com").Width(300)
            ),

            ToggleSwitch(darkMode, setDarkMode, onContent: "Dark", offContent: "Light"),

            HStack(8,
                Text("Font size:"),
                Slider(fontSize, 10, 30, setFontSize).Width(200),
                Text($"{fontSize:F0}px")
            ),

            CheckBox(agreeToTerms, setAgree, label: "I agree to the terms"),

            When(!isValid, () =>
                Text("Please fill all fields and agree to terms").Opacity(0.6)),

            Button("Submit", () => setSubmitted(true)).Disabled(!isValid)
        );
    }
}

// ─── Dynamic list demo ─────────────────────────────────────────────────────────

class DynamicListDemo : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(3);
        var (showIndices, setShowIndices) = UseState(true);

        return VStack(12,
            Heading("Dynamic List"),
            Text("Demonstrates conditional and list rendering"),

            HStack(8,
                Button("Remove", () => setCount(Math.Max(0, count - 1))).Disabled(count == 0),
                Text($"{count} items"),
                Button("Add", () => setCount(count + 1))
            ),

            CheckBox(showIndices, setShowIndices, label: "Show indices"),

            // Dynamic list generated from a range
            VStack(4,
                Enumerable.Range(0, count).Select(i =>
                    Border(
                        HStack(8,
                            When(showIndices, () => Text($"#{i + 1}").SemiBold()),
                            Text($"Item {i + 1}"),
                            Text($"(created dynamically)").Opacity(0.5)
                        )
                    ).CornerRadius(4).Background("#f0f0f0").Padding(12, 8)
                ).ToArray()
            ),

            When(count == 0, () => Text("No items. Click Add to create some.").Opacity(0.6)),
            When(count >= 10, () => Text("That's a lot of items!").Bold())
        );
    }
}

// ─── Performance stress test ─────────────────────────────────────────────────
// Visualizes quicksort on a large array. Each step mutates many elements:
// swaps, color changes, size changes, and conditional controls.
// Use the checkbox to toggle Rust vs C# diffing and compare render times.

class PerfStressDemo : Component
{
    record SortState(int[] Values, int[] Colors, int Pivot, int Left, int Right, bool Sorted);

    static readonly string[] BarColors =
    [
        "#4fc3f7", "#81c784", "#fff176", "#ff8a65", "#ba68c8",
        "#4dd0e1", "#aed581", "#ffd54f", "#e57373", "#9575cd",
    ];

    public override Element Render()
    {
        var (elementCount, setElementCount) = UseState(100);
        var (useNative, setUseNative) = UseState(true);
        var (running, setRunning) = UseState(false);
        var (sortState, setSortState) = UseReducer<SortState?>(null);
        var (renderTimes, setRenderTimes) = UseReducer(new List<double>());
        var (totalSwaps, setTotalSwaps) = UseState(0);
        var (stepCount, setStepCount) = UseState(0);
        var (showLabels, setShowLabels) = UseState(false);
        var (showBorders, setShowBorders) = UseState(true);
        var (tickMs, setTickMs) = UseState(16);
        var (totalSortMs, setTotalSortMs) = UseState(0.0);

        // Apply reconcile mode to the host
        UseEffect(() =>
        {
            if (DuctApp.ActiveHost is not null)
                DuctApp.ActiveHost.ReconcileMode = useNative
                    ? ReconcileMode.NativeDiffTree
                    : ReconcileMode.CSharpFallback;
        }, [useNative]);

        void StartSort()
        {
            var rng = new Random(42); // deterministic seed for reproducible results
            var values = Enumerable.Range(1, elementCount).ToArray();
            // Fisher-Yates shuffle
            for (int i = values.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
            var colors = new int[elementCount];
            setSortState(_ => new SortState(values, colors, -1, -1, -1, false));
            setRenderTimes(_ => new List<double>());
            setTotalSwaps(0);
            setStepCount(0);
            setTotalSortMs(0);
            setRunning(true);
            RunQuicksort(values, colors);
        }

        async void RunQuicksort(int[] values, int[] colors)
        {
            var totalTimer = Stopwatch.StartNew();
            var sw = new Stopwatch();
            int swaps = 0;
            int steps = 0;

            async Task QSort(int lo, int hi)
            {
                if (lo >= hi || lo < 0) return;

                // Partition
                int pivot = values[hi];
                int pivotColor = 1; // green = pivot
                colors[hi] = pivotColor;

                int i = lo;
                for (int j = lo; j < hi; j++)
                {
                    colors[j] = 2; // yellow = comparing
                    if (values[j] <= pivot)
                    {
                        // Swap
                        (values[i], values[j]) = (values[j], values[i]);
                        (colors[i], colors[j]) = (colors[j], colors[i]);
                        colors[i] = 3; // orange = swapped
                        i++;
                        swaps++;
                    }

                    steps++;
                    if (steps % Math.Max(1, elementCount / 20) == 0)
                    {
                        // Emit a render tick
                        sw.Restart();
                        setSortState(_ => new SortState(
                            (int[])values.Clone(),
                            (int[])colors.Clone(),
                            hi, lo, i, false));
                        setTotalSwaps(swaps);
                        setStepCount(steps);
                        sw.Stop();
                        setRenderTimes(list =>
                        {
                            var copy = new List<double>(list) { sw.Elapsed.TotalMilliseconds };
                            if (copy.Count > 200) copy.RemoveAt(0);
                            return copy;
                        });
                        await Task.Delay(tickMs);
                    }
                }

                // Final pivot swap
                (values[i], values[hi]) = (values[hi], values[i]);
                (colors[i], colors[hi]) = (colors[hi], colors[i]);
                swaps++;

                // Mark sorted partition
                colors[i] = 4; // purple = in final position

                sw.Restart();
                setSortState(_ => new SortState(
                    (int[])values.Clone(),
                    (int[])colors.Clone(),
                    i, lo, hi, false));
                setTotalSwaps(swaps);
                setStepCount(steps);
                sw.Stop();
                setRenderTimes(list =>
                {
                    var copy = new List<double>(list) { sw.Elapsed.TotalMilliseconds };
                    if (copy.Count > 200) copy.RemoveAt(0);
                    return copy;
                });
                await Task.Delay(tickMs);

                // Reset colors for next partition
                for (int k = lo; k <= hi; k++)
                    if (colors[k] != 4) colors[k] = 0;

                await QSort(lo, i - 1);
                await QSort(i + 1, hi);
            }

            await QSort(0, values.Length - 1);

            // Mark all sorted
            totalTimer.Stop();
            for (int k = 0; k < colors.Length; k++) colors[k] = 4;
            setSortState(_ => new SortState(
                (int[])values.Clone(),
                (int[])colors.Clone(),
                -1, -1, -1, true));
            setTotalSortMs(totalTimer.Elapsed.TotalMilliseconds);
            setRunning(false);
        }

        // Compute stats
        double avgMs = renderTimes.Count > 0 ? renderTimes.Average() : 0;
        double maxMs = renderTimes.Count > 0 ? renderTimes.Max() : 0;
        double p95Ms = 0;
        if (renderTimes.Count > 0)
        {
            var sorted = renderTimes.OrderBy(x => x).ToList();
            p95Ms = sorted[(int)(sorted.Count * 0.95)];
        }

        // Build the bar visualization
        Element bars;
        if (sortState is not null)
        {
            var barElements = new Element[sortState.Values.Length];
            double maxVal = elementCount;
            for (int i = 0; i < sortState.Values.Length; i++)
            {
                double heightPct = sortState.Values[i] / maxVal * 200;
                int colorIdx = sortState.Colors[i] % BarColors.Length;
                bool isPivot = i == sortState.Pivot;
                bool isActive = i >= sortState.Left && i <= sortState.Right;

                double barWidth = Math.Max(2, 800.0 / elementCount - (showBorders ? 1 : 0));
                double barHeight = Math.Max(4, heightPct);
                double opacity = isPivot ? 1.0 : isActive ? 0.9 : 0.7;
                int val = sortState.Values[i];

                // Each bar contains child controls to stress the reconciler:
                // a tiny progress indicator + a value label + a colored pip
                Element barContent = VStack(0,
                    // Top: small colored indicator pip (changes with sort state)
                    Border(Empty())
                        .Background(isPivot ? "#ffffff" : isActive ? "#ffeb3b" : BarColors[(colorIdx + 1) % BarColors.Length])
                        .CornerRadius(1)
                        .Width(Math.Min(barWidth - 1, 6))
                        .Height(2),
                    // Middle: value label (only when bars are wide enough)
                    barWidth >= 10
                        ? Text($"{val}").FontSize(Math.Min(7, barWidth * 0.8))
                        : Empty(),
                    // Bottom: progress-like fill showing relative position
                    Border(Empty())
                        .Background(BarColors[(colorIdx + 2) % BarColors.Length])
                        .CornerRadius(0)
                        .Width(Math.Max(1, barWidth * 0.6))
                        .Height(Math.Max(1, barHeight * 0.15))
                        .Opacity(0.5)
                );

                Element bar = Border(barContent)
                    .Background(BarColors[colorIdx])
                    .CornerRadius(0)
                    .Width(barWidth)
                    .Height(barHeight)
                    .Opacity(opacity)
                    .VAlign(VerticalAlignment.Bottom);

                if (showBorders)
                    bar = bar.Margin(0, 0, 1, 0);

                barElements[i] = bar;
            }
            bars = HStack(0, barElements).Height(220).VAlign(VerticalAlignment.Bottom);
        }
        else
        {
            bars = Text("Click 'Start Sort' to begin").Opacity(0.5).Height(220);
        }

        return ScrollView(VStack(12,
            Heading("Performance Stress Test"),
            Text("Quicksort visualization — stresses tree diffing with many simultaneous property changes, " +
                 "element creation/removal, and structural mutations."),

            // Controls
            HStack(12,
                VStack(4,
                    Text("Elements:"),
                    HStack(8,
                        Button("10", () => { if (!running) setElementCount(10); }).Disabled(running || elementCount == 10),
                        Button("50", () => { if (!running) setElementCount(50); }).Disabled(running || elementCount == 50),
                        Button("100", () => { if (!running) setElementCount(100); }).Disabled(running || elementCount == 100),
                        Button("250", () => { if (!running) setElementCount(250); }).Disabled(running || elementCount == 250),
                        Button("500", () => { if (!running) setElementCount(500); }).Disabled(running || elementCount == 500),
                        Button("1000", () => { if (!running) setElementCount(1000); }).Disabled(running || elementCount == 1000)
                    )
                ),
                VStack(4,
                    Text("Tick interval:"),
                    HStack(8,
                        Slider(tickMs, 0, 100, v => { if (!running) setTickMs((int)v); }).Width(150),
                        Text($"{tickMs}ms")
                    )
                )
            ),

            HStack(12,
                CheckBox(useNative, v =>
                {
                    if (!running) setUseNative(v);
                }, label: $"Use Rust DiffTrees (currently: {(useNative ? "Rust" : "C#")})"),
                CheckBox(showLabels, v => setShowLabels(v), label: "Show value labels"),
                CheckBox(showBorders, v => setShowBorders(v), label: "Show bar gaps")
            ),

            HStack(8,
                Button("Start Sort", StartSort).Disabled(running),
                Button("Reset", () =>
                {
                    setSortState(_ => null);
                    setRenderTimes(_ => new List<double>());
                    setTotalSwaps(0);
                    setStepCount(0);
                }).Disabled(running)
            ),

            // Status
            sortState?.Sorted == true
                ? Text($"Sorted in {totalSortMs:F0} ms  ({totalSwaps} swaps, {stepCount} steps)")
                    .Bold().FontSize(16)
                : running
                    ? Text($"Sorting... step {stepCount}, {totalSwaps} swaps").Opacity(0.8)
                    : Empty(),

            // Visualization area
            Border(bars)
                .CornerRadius(8)
                .Background("#1a1a2e")
                .Padding(8),

            // Performance stats
            When(renderTimes.Count > 0, () => VStack(4,
                SubHeading("Render Performance"),
                HStack(16,
                    VStack(2,
                        Text("Mode").SemiBold(),
                        Text(useNative ? "Rust DiffTrees" : "C# Imperative")
                    ),
                    VStack(2,
                        Text("Elements").SemiBold(),
                        Text($"{elementCount}")
                    ),
                    VStack(2,
                        Text("Samples").SemiBold(),
                        Text($"{renderTimes.Count}")
                    ),
                    VStack(2,
                        Text("Avg").SemiBold(),
                        Text($"{avgMs:F2} ms")
                    ),
                    VStack(2,
                        Text("P95").SemiBold(),
                        Text($"{p95Ms:F2} ms")
                    ),
                    VStack(2,
                        Text("Max").SemiBold(),
                        Text($"{maxMs:F2} ms")
                    ),
                    VStack(2,
                        Text("Swaps").SemiBold(),
                        Text($"{totalSwaps}")
                    ),
                    VStack(2,
                        Text("Total").SemiBold(),
                        Text($"{totalSortMs:F0} ms")
                    )
                ),

                // Mini histogram of render times
                Text("Render time distribution (last 200 ticks):").Opacity(0.6).Margin(0, 8, 0, 0),
                HStack(0,
                    renderTimes.TakeLast(100).Select((t, i) =>
                    {
                        double h = Math.Min(50, t * 10); // 1ms = 10px
                        string color = t < 2 ? "#81c784" : t < 8 ? "#fff176" : t < 16 ? "#ff8a65" : "#e57373";
                        return (Element)Border(Empty())
                            .Background(color)
                            .CornerRadius(0)
                            .Width(Math.Max(1, 600.0 / 100))
                            .Height(Math.Max(1, h))
                            .VAlign(VerticalAlignment.Bottom);
                    }).ToArray()
                ).Height(60)
            )),

            // Color legend
            HStack(16,
                LegendItem("#4fc3f7", "Default"),
                LegendItem("#81c784", "Pivot"),
                LegendItem("#fff176", "Comparing"),
                LegendItem("#ff8a65", "Swapped"),
                LegendItem("#ba68c8", "Final position")
            ).Margin(0, 8, 0, 0)
        ));
    }

    static Element LegendItem(string color, string label) =>
        HStack(4,
            Border(Empty()).Background(color).CornerRadius(2).Width(12).Height(12),
            Text(label).FontSize(12).Opacity(0.7)
        );
}

// ─── Virtualization test ──────────────────────────────────────────────────
// Verifies that Duct's ListView and LazyVStack preserve WinUI3's built-in
// virtualization with 1000 items. If virtualization is broken, scrolling will
// be janky and memory will balloon.

class VirtualizationDemo : Component
{
    record ItemData(int Id, string Title, string Subtitle);

    public override Element Render()
    {
        var (mode, setMode) = UseState("LazyVStack");
        var (itemCount, setItemCount) = UseState(1000);
        var (selectedIndex, setSelectedIndex) = UseState(-1);

        // Generate item data
        var items = Enumerable.Range(0, itemCount)
            .Select(i => new ItemData(i, $"Item {i}", $"Description for item {i} — this row tests virtualization"))
            .ToList();

        Element list = mode switch
        {
            "LazyVStack" => LazyVStack<ItemData>(
                items,
                item => item.Id.ToString(),
                (item, index) => Border(
                    HStack(12,
                        Border(
                            Text($"{item.Id}").FontSize(12)
                        ).Background("#e3f2fd").CornerRadius(4).Width(48).Height(32).HAlign(HorizontalAlignment.Center),
                        VStack(2,
                            Text(item.Title).SemiBold(),
                            Text(item.Subtitle).FontSize(12).Opacity(0.6)
                        )
                    )
                ).Padding(12, 8).Margin(0, 0, 0, 1)
            ),

            "ListView" => ListView(
                items.Select(item => (Element)Border(
                    HStack(12,
                        Border(
                            Text($"{item.Id}").FontSize(12)
                        ).Background("#e3f2fd").CornerRadius(4).Width(48).Height(32).HAlign(HorizontalAlignment.Center),
                        VStack(2,
                            Text(item.Title).SemiBold(),
                            Text(item.Subtitle).FontSize(12).Opacity(0.6)
                        )
                    )
                ).Padding(12, 8)).ToArray()
            )
            .Set(lv => { lv.Height = 500; lv.SelectionMode = Microsoft.UI.Xaml.Controls.ListViewSelectionMode.Single; }),

            _ => Empty()
        };

        return VStack(12,
            Heading("Virtualization Test"),
            Text($"Renders {itemCount} items. If virtualization is working, scrolling should be smooth " +
                 "and only visible items should be realized in the visual tree."),

            HStack(12,
                VStack(4,
                    Text("Mode:"),
                    HStack(8,
                        Button("LazyVStack", () => setMode("LazyVStack"))
                            .Disabled(mode == "LazyVStack"),
                        Button("ListView", () => setMode("ListView"))
                            .Disabled(mode == "ListView")
                    )
                ),
                VStack(4,
                    Text("Items:"),
                    HStack(8,
                        Button("100", () => setItemCount(100)).Disabled(itemCount == 100),
                        Button("1000", () => setItemCount(1000)).Disabled(itemCount == 1000),
                        Button("5000", () => setItemCount(5000)).Disabled(itemCount == 5000),
                        Button("10000", () => setItemCount(10000)).Disabled(itemCount == 10000)
                    )
                )
            ),

            Text($"Mode: {mode} | Items: {itemCount}").Opacity(0.6),

            // The list itself
            Border(list)
                .CornerRadius(8)
                .Background("#ffffff")
                .Height(500)
        );
    }
}

// ─── Flyout demo ──────────────────────────────────────────────────────────────
// Tests declarative flyout attachments with dynamic content that updates on a timer.

class FlyoutDemo : Component
{
    public override Element Render()
    {
        var (tick, updateTick) = UseReducer(0);
        var (color, setColor) = UseState("Red");

        // Timer ticks every second to test dynamic flyout content.
        UseEffect(() =>
        {
            var timer = new Microsoft.UI.Xaml.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => updateTick(t => t + 1);
            timer.Start();
            return () => timer.Stop();
        }, []);

        var colors = new[] { "Red", "Orange", "Yellow", "Green", "Blue", "Purple" };
        var currentColorHex = color switch
        {
            "Red" => "#e57373",
            "Orange" => "#ffb74d",
            "Yellow" => "#fff176",
            "Green" => "#81c784",
            "Blue" => "#64b5f6",
            "Purple" => "#ba68c8",
            _ => "#e0e0e0"
        };

        return ScrollView(VStack(16,
            Heading("Flyout Attachments"),
            Text("Tests declarative .WithFlyout(), .WithContextFlyout(), and .WithToolTip(Element) modifiers."),
            Text($"Timer tick: {tick} (flyout content updates every second)").Opacity(0.6),

            // ── 1. ContentFlyout on a Button via .WithFlyout() ──
            SubHeading("1. Button with ContentFlyout (dynamic content)"),
            Text("Click the button to see a flyout with a live-updating counter."),
            Button("Open Flyout", null)
                .WithFlyout(ContentFlyout(
                    VStack(12,
                        Text("Dynamic Flyout Content").SemiBold(),
                        Text($"Timer tick: {tick}").FontSize(20),
                        Border(
                            Text($"Elapsed: {tick} seconds")
                        ).CornerRadius(4).Background("#e3f2fd").Padding(12, 8),
                        HStack(8,
                            Enumerable.Range(0, Math.Min(tick % 10, 8)).Select(i =>
                                (Element)Border(Empty())
                                    .Background(colors[i % colors.Length] switch
                                    {
                                        "Red" => "#e57373",
                                        "Orange" => "#ffb74d",
                                        "Yellow" => "#fff176",
                                        "Green" => "#81c784",
                                        "Blue" => "#64b5f6",
                                        _ => "#ba68c8"
                                    })
                                    .CornerRadius(4)
                                    .Size(24, 24)
                            ).ToArray()
                        )
                    ),
                    placement: Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom
                )),

            // ── 2. MenuFlyout on DropDownButton ──
            SubHeading("2. DropDownButton with MenuItems"),
            Text("A DropDownButton with a declarative menu flyout."),
            DropDownButton("Pick a color", flyout:
                MenuItems(
                    MenuItem("Red", () => setColor("Red")),
                    MenuItem("Orange", () => setColor("Orange")),
                    MenuItem("Yellow", () => setColor("Yellow")),
                    MenuSeparator(),
                    MenuItem("Green", () => setColor("Green")),
                    MenuItem("Blue", () => setColor("Blue")),
                    MenuItem("Purple", () => setColor("Purple"))
                )
            ),
            HStack(8,
                Text($"Selected: {color}"),
                Border(Empty()).Background(currentColorHex).CornerRadius(4).Size(24, 24)
            ),

            // ── 3. SplitButton with ContentFlyout ──
            SubHeading("3. SplitButton with ContentFlyout"),
            Text("SplitButton with a declarative color grid flyout."),
            SplitButton($"Apply {color}", () => { /* primary action */ }, flyout:
                ContentFlyout(
                    VStack(8,
                        Text("Pick a color:").SemiBold(),
                        HStack(4,
                            colors.Select(c =>
                            {
                                var hex = c switch
                                {
                                    "Red" => "#e57373",
                                    "Orange" => "#ffb74d",
                                    "Yellow" => "#fff176",
                                    "Green" => "#81c784",
                                    "Blue" => "#64b5f6",
                                    "Purple" => "#ba68c8",
                                    _ => "#e0e0e0"
                                };
                                return (Element)Button("", () => setColor(c))
                                    .Set(b =>
                                    {
                                        b.Content = new Microsoft.UI.Xaml.Controls.Border
                                        {
                                            Width = 32, Height = 32,
                                            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                                                Microsoft.UI.Colors.Transparent),
                                            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(4)
                                        };
                                        ((Microsoft.UI.Xaml.Controls.Border)b.Content).Background =
                                            BrushHelper.Parse(hex);
                                        b.Padding = new Microsoft.UI.Xaml.Thickness(0);
                                        b.MinWidth = 0;
                                        b.MinHeight = 0;
                                    });
                            }).ToArray()
                        )
                    ),
                    placement: Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom
                )
            ),

            // ── 4. ContextFlyout on any element ──
            SubHeading("4. ContextFlyout (right-click menu)"),
            Text("Right-click the box below to see a context menu."),
            Border(
                VStack(8,
                    Text("Right-click me!").SemiBold(),
                    Text($"Color: {color} | Tick: {tick}")
                )
            ).CornerRadius(8).Background("#f5f5f5").Padding(24)
             .WithContextFlyout(MenuItems(
                MenuItem("Reset color", () => setColor("Red")),
                MenuItem("Reset timer", () => updateTick(_ => 0)),
                MenuSeparator(),
                MenuItem("Set Blue", () => setColor("Blue")),
                MenuItem("Set Green", () => setColor("Green"))
             )),

            // ── 5. Rich ToolTip ──
            SubHeading("5. Rich ToolTip (Element content)"),
            Text("Hover over the button below for a rich tooltip with dynamic content."),
            Button("Hover me", null)
                .WithToolTip(
                    VStack(8,
                        Text("Rich ToolTip").SemiBold(),
                        Text($"Current color: {color}"),
                        Text($"Timer: {tick}s"),
                        Border(Empty())
                            .Background(currentColorHex)
                            .CornerRadius(4)
                            .Size(80, 16)
                    )
                )
        ));
    }
}

// ─── DataTemplate demo ────────────────────────────────────────────────────────
// Demonstrates typed ListView<T>, GridView<T>, FlipView<T>, and TreeView ContentElement.
// These use the new Func<T, int, Element> viewBuilder pattern so the reconciler
// drives mounting/updating/recycling of templated items natively.

class DataTemplateDemo : Component
{
    record Animal(int Id, string Name, string Species, string Emoji);

    static readonly List<Animal> AllAnimals =
    [
        new(1, "Luna", "Cat", "\U0001F431"),
        new(2, "Max", "Dog", "\U0001F436"),
        new(3, "Bella", "Cat", "\U0001F431"),
        new(4, "Charlie", "Dog", "\U0001F436"),
        new(5, "Oliver", "Rabbit", "\U0001F430"),
        new(6, "Lucy", "Cat", "\U0001F431"),
        new(7, "Buddy", "Dog", "\U0001F436"),
        new(8, "Daisy", "Hamster", "\U0001F439"),
        new(9, "Rocky", "Dog", "\U0001F436"),
        new(10, "Coco", "Parrot", "\U0001F99C"),
    ];

    public override Element Render()
    {
        var (animals, updateAnimals) = UseReducer(AllAnimals);
        var (selectedListIndex, setSelectedListIndex) = UseState(-1);
        var (selectedGridIndex, setSelectedGridIndex) = UseState(-1);
        var (flipIndex, setFlipIndex) = UseState(0);
        var (filter, setFilter) = UseState("");

        var filtered = string.IsNullOrWhiteSpace(filter)
            ? animals
            : animals.Where(a => a.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                                 || a.Species.Contains(filter, StringComparison.OrdinalIgnoreCase))
                     .ToList();

        return ScrollView(VStack(16,
            Heading("DataTemplate Demo"),
            Text("Typed ListView<T>, GridView<T>, FlipView<T> with viewBuilder, plus TreeView ContentElement."),

            // Filter + add/remove controls
            HStack(12,
                TextField(filter, setFilter, placeholder: "Filter animals...").Width(200),
                Button("Add Random", () => updateAnimals(list =>
                {
                    var id = list.Count + 1;
                    var species = new[] { "Cat", "Dog", "Rabbit", "Hamster", "Parrot" };
                    var emojis = new[] { "\U0001F431", "\U0001F436", "\U0001F430", "\U0001F439", "\U0001F99C" };
                    var rng = new Random();
                    var si = rng.Next(species.Length);
                    return [.. list, new Animal(id, $"Pet #{id}", species[si], emojis[si])];
                })),
                Button("Remove Last", () => updateAnimals(list =>
                    list.Count > 0 ? list.Take(list.Count - 1).ToList() : list
                )).Disabled(animals.Count == 0)
            ),

            Text($"{filtered.Count} animals shown").Opacity(0.6),

            // ── 1. Typed ListView<T> ──
            SubHeading("1. Typed ListView<T>"),
            Text("Each row uses pattern matching on Species for a DataTemplateSelector-equivalent."),
            Border(
                ListView<Animal>(
                    filtered,
                    a => a.Id.ToString(),
                    (animal, i) => HStack(12,
                        Text(animal.Emoji).FontSize(24),
                        VStack(2,
                            Text(animal.Name).SemiBold(),
                            animal.Species switch
                            {
                                "Cat" => Text($"Feline - {animal.Species}").FontSize(12).Opacity(0.7),
                                "Dog" => Text($"Canine - {animal.Species}").FontSize(12).Opacity(0.7),
                                _ => Text(animal.Species).FontSize(12).Opacity(0.5),
                            }
                        ),
                        Text($"#{animal.Id}").Opacity(0.3)
                    ).Margin(4)
                ) with
                {
                    SelectedIndex = selectedListIndex,
                    OnSelectionChanged = setSelectedListIndex,
                    OnItemClick = a => setSelectedListIndex(filtered.IndexOf(a)),
                    Header = "Animals"
                }
            ).CornerRadius(8).Height(250),

            When(selectedListIndex >= 0 && selectedListIndex < filtered.Count,
                () => Text($"Selected: {filtered[Math.Min(selectedListIndex, filtered.Count - 1)].Name}").SemiBold()),

            // ── 2. Typed GridView<T> ──
            SubHeading("2. Typed GridView<T>"),
            Text("Card layout with per-species colors."),
            Border(
                GridView<Animal>(
                    filtered,
                    a => a.Id.ToString(),
                    (animal, i) =>
                    {
                        var bg = animal.Species switch
                        {
                            "Cat" => "#fff3e0",
                            "Dog" => "#e3f2fd",
                            "Rabbit" => "#f3e5f5",
                            "Hamster" => "#fff9c4",
                            "Parrot" => "#e8f5e9",
                            _ => "#f5f5f5"
                        };
                        return Border(
                            VStack(4,
                                Text(animal.Emoji).FontSize(32).HAlign(HorizontalAlignment.Center),
                                Text(animal.Name).SemiBold().HAlign(HorizontalAlignment.Center),
                                Text(animal.Species).FontSize(11).Opacity(0.6).HAlign(HorizontalAlignment.Center)
                            )
                        ).CornerRadius(8).Background(bg).Padding(12).Width(120).Height(120);
                    }
                ) with
                {
                    SelectedIndex = selectedGridIndex,
                    OnSelectionChanged = setSelectedGridIndex,
                    Header = "Gallery"
                }
            ).CornerRadius(8).Height(300),

            // ── 3. Typed FlipView<T> ──
            SubHeading("3. Typed FlipView<T>"),
            Text("Swipe through animal cards."),
            Border(
                FlipView<Animal>(
                    filtered,
                    a => a.Id.ToString(),
                    (animal, i) => Border(
                        VStack(12,
                            Text(animal.Emoji).FontSize(64).HAlign(HorizontalAlignment.Center),
                            Text(animal.Name).FontSize(24).SemiBold().HAlign(HorizontalAlignment.Center),
                            Text($"{animal.Species} (#{animal.Id})").Opacity(0.6).HAlign(HorizontalAlignment.Center)
                        ).HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center)
                    ).Background("#f5f5f5").Padding(32)
                ) with
                {
                    SelectedIndex = flipIndex,
                    OnSelectionChanged = setFlipIndex,
                }
            ).CornerRadius(8).Height(250).Width(400),

            Text($"Showing {flipIndex + 1} of {filtered.Count}").Opacity(0.6),

            // ── 4. TreeView with ContentElement ──
            SubHeading("4. TreeView with ContentElement"),
            Text("Tree nodes render custom Duct elements instead of plain text."),
            Border(
                TreeView(
                    new TreeViewNodeData("Pets") { IsExpanded = true,
                        ContentElement = HStack(8,
                            Text("\U0001F3E0").FontSize(16),
                            Text("All Pets").Bold()
                        ),
                        Children = new[] { "Cat", "Dog", "Rabbit", "Hamster", "Parrot" }
                            .Where(species => filtered.Any(a => a.Species == species))
                            .Select(species => new TreeViewNodeData(species)
                            {
                                IsExpanded = true,
                                ContentElement = HStack(8,
                                    Text(species switch
                                    {
                                        "Cat" => "\U0001F431",
                                        "Dog" => "\U0001F436",
                                        "Rabbit" => "\U0001F430",
                                        "Hamster" => "\U0001F439",
                                        "Parrot" => "\U0001F99C",
                                        _ => "\U0001F43E"
                                    }),
                                    Text(species).SemiBold(),
                                    Text($"({filtered.Count(a => a.Species == species)})").Opacity(0.5)
                                ),
                                Children = filtered
                                    .Where(a => a.Species == species)
                                    .Select(a => new TreeViewNodeData(a.Name)
                                    {
                                        ContentElement = HStack(8,
                                            Text(a.Emoji),
                                            Text(a.Name),
                                            Text($"#{a.Id}").FontSize(11).Opacity(0.3)
                                        )
                                    }).ToArray()
                            }).ToArray()
                    }
                )
            ).CornerRadius(8).Height(300)
        ));
    }
}


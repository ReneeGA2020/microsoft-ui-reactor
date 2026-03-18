// Patch Test App — A single-file WinUI 3 application using the Patch functional projection.
// No XAML. No data binding. No resources. No templates. Just C#.

using Patch;
using Patch.Core;
using Microsoft.UI.Xaml;
using static Patch.UI;

PatchApp.Run<DemoApp>("Patch Demo", width: 1200, height: 800);

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
                TabButton("Dynamic List", currentTab, setTab)
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
                    _ => Text("Select a tab")
                }
            ).Padding(new Thickness(24)).Margin(16)
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
            new("Build Patch library", true),
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
                              ).CornerRadius(4).Background("#e8f5e9").Padding(new Thickness(12))
                            : null,

                        enableFeatureB
                            ? Border(
                                VStack(4,
                                    Text("Feature B Configuration").SemiBold(),
                                    Text("This sub-tree only exists when Feature B is checked."),
                                    ToggleSwitch(false, null, onContent: "On", offContent: "Off")
                                )
                              ).CornerRadius(4).Background("#e3f2fd").Padding(new Thickness(12))
                            : null
                    )
                  ).CornerRadius(8).Background("#f5f5f5").Padding(new Thickness(16))
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
                        ).CornerRadius(4).Background("#fff3e0").Padding(new Thickness(8, 4, 8, 4))
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
                    ).CornerRadius(4).Background("#fff9c4").Padding(new Thickness(12))
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
                    ).CornerRadius(4).Background("#f0f0f0").Padding(new Thickness(12, 8, 12, 8))
                ).ToArray()
            ),

            When(count == 0, () => Text("No items. Click Add to create some.").Opacity(0.6)),
            When(count >= 10, () => Text("That's a lot of items!").Bold())
        );
    }
}

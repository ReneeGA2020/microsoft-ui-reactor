// ════════════════════════════════════════════════════════════════════════════
//  A11y Showcase — intentionally broken accessibility sample
//
//  PURPOSE: This sample is a realistic-looking task tracker that follows
//  best practices for layout, theming, and state management — but
//  intentionally omits or misuses accessibility attributes. It exists
//  to demonstrate how Reactor's accessibility tooling (Roslyn analyzers +
//  runtime AccessibilityScanner) catches these issues.
//
//  DO NOT FIX THE ACCESSIBILITY ISSUES IN THIS FILE.
//  They are the whole point of the sample.
//
//  Expected diagnostics (runtime AccessibilityScanner):
//    A11Y_001  — Icon-only buttons without .AutomationName()       (6 instances)
//    A11Y_002  — Images without .AutomationName() or .AccessibilityHidden()  (2+)
//    A11Y_003  — Form fields without labels                        (1 instance)
//    A11Y_004  — Heading-styled text without .HeadingLevel()       (1 instance)
//    A11Y_005  — Concrete brush on interactive control              (2 instances)
//    A11Y_006  — No .Landmark(Main) in the tree                    (1 instance)
//    A11Y_008  — .LabeledBy() referencing a missing AutomationId   (1 instance)
//
//  Expected diagnostics (Roslyn compile-time analyzers):
//    REACTOR_A11Y_001 — Icon-only buttons without .AutomationName()
//    REACTOR_A11Y_003 — Form fields without header/AutomationName/LabeledBy
//
//  To run the scanner at runtime, call AccessibilityScanner.Scan() on
//  the rendered element tree and inspect the structured JSON output.
// ════════════════════════════════════════════════════════════════════════════

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;

namespace A11yShowcase;

record TaskItem(string Id, string Title, string Assignee, string Priority, bool Done);

sealed class App : Component
{
    // Icon helpers — uses Image elements so the scanner sees ContentElement
    static Element Icon(string name) => Image($"ms-appx:///Assets/{name}.png").Size(16, 16);

    public override Element Render()
    {
        var (tasks, updateTasks) = UseReducer(new List<TaskItem>
        {
            new("1", "Set up CI pipeline", "Alice", "High", false),
            new("2", "Write unit tests", "Bob", "Medium", true),
            new("3", "Design landing page", "Carol", "High", false),
            new("4", "Fix login timeout", "Dave", "Low", true),
            new("5", "Review PR #247", "Alice", "Medium", false),
        });

        var (newTitle, setNewTitle) = UseState("");
        var (filter, setFilter) = UseState("All");
        var (statusMsg, setStatusMsg) = UseState("");

        var filtered = UseMemo(() => filter switch
        {
            "Active" => tasks.Where(t => !t.Done).ToList(),
            "Done" => tasks.Where(t => t.Done).ToList(),
            _ => tasks,
        }, tasks, filter);

        var doneCount = tasks.Count(t => t.Done);
        var totalCount = tasks.Count;
        var progress = totalCount > 0 ? (double)doneCount / totalCount * 100 : 0;

        var tree = VStack(0,
            // ── Header ──────────────────────────────────────────────
            Border(
                HStack(12,
                    // A11Y_002: Image without alt text or AccessibilityHidden
                    Image("ms-appx:///Assets/logo.png").Size(28, 28),

                    // A11Y_004: Large bold text styled as heading but no HeadingLevel set
                    Factories.Text("Task Tracker").Bold().FontSize(24),

                    HStack(4,
                        // A11Y_001: Icon-only buttons without AutomationName
                        Button(Icon("settings"), () => { }),
                        Button(Icon("people"), () => { }),
                        Button(Icon("refresh"), () =>
                        {
                            setStatusMsg("Tasks refreshed");
                        })
                    ).HAlign(HorizontalAlignment.Right)
                ).Padding(16, 12, 16, 12)
            ).Background(Theme.CardBackground),

            // ── Toolbar ─────────────────────────────────────────────
            Border(
                HStack(12,
                    // Filter buttons with concrete brush — A11Y_005
                    HStack(4,
                        FilterBtn("All", filter, setFilter),
                        FilterBtn("Active", filter, setFilter),
                        FilterBtn("Done", filter, setFilter)
                    ),

                    // A11Y_003: TextField without header or AutomationName
                    TextField(newTitle, setNewTitle, placeholder: "New task...")
                        .Width(280),

                    // A11Y_001: Icon-only button without AutomationName
                    Button(Icon("add"), () =>
                    {
                        if (!string.IsNullOrWhiteSpace(newTitle))
                        {
                            var id = Guid.NewGuid().ToString("N")[..8];
                            updateTasks(list => [.. list, new TaskItem(id, newTitle.Trim(), "Unassigned", "Medium", false)]);
                            setNewTitle("");
                            setStatusMsg($"Added: {newTitle.Trim()}");
                        }
                    }),

                    // Status text (no LiveRegion — screen readers won't hear updates)
                    Factories.Text(statusMsg).Opacity(0.6).HAlign(HorizontalAlignment.Right)
                ).Padding(12, 8, 12, 8)
            ).Background(Theme.LayerFill).WithBorder(Theme.Ref("DividerStrokeColorDefaultBrush"), 1),

            // ── Progress bar ────────────────────────────────────────
            HStack(8,
                Caption($"{doneCount}/{totalCount} complete"),
                Progress(progress).HAlign(HorizontalAlignment.Stretch)
            ).Margin(16, 8, 16, 8),

            // ── Task list ── (no .Landmark(Main) — A11Y_006 at tree level)
            ScrollView(
                VStack(4,
                    filtered.Select((task, i) =>
                        TaskRow(task, i, updateTasks, setStatusMsg)
                    ).ToArray()
                ).Padding(16, 4, 16, 16)
            ),

            // ── Footer ──────────────────────────────────────────────
            Border(
                HStack(12,
                    // A11Y_003 + A11Y_008: TextField with .LabeledBy()
                    // referencing a missing AutomationId (typo in the ID)
                    Factories.Text("Quick note:"),
                    TextField("", _ => { })
                        .LabeledBy("FooterNoteLabel_TYPO")
                        .Width(300),

                    Caption("v1.0.0 — A11y Showcase").Opacity(0.5)
                        .HAlign(HorizontalAlignment.Right)
                ).Padding(12, 8, 12, 8)
            ).Background(Theme.CardBackground)
        );

        // ── Runtime accessibility scan (DEBUG only) ─────────────────
        // Run on first render, print diagnostics to VS Output window.
        var scanRan = UseRef(false);
        if (!scanRan.Current)
        {
            scanRan.Current = true;
            var findings = AccessibilityScanner.Scan(tree);
            global::System.Diagnostics.Debug.WriteLine($"");
            global::System.Diagnostics.Debug.WriteLine($"═══ AccessibilityScanner: {findings.Count} finding(s) ═══");
            foreach (var f in findings)
                global::System.Diagnostics.Debug.WriteLine($"  [{f.Id}] {f.Severity.ToUpper()}: {f.Message}");
            global::System.Diagnostics.Debug.WriteLine($"");

            // Also export structured JSON for AI-agent consumption
            var jsonPath = global::System.IO.Path.Combine(
                global::System.IO.Path.GetTempPath(), "a11y-showcase-diagnostics.json");
            AccessibilityScanner.ExportJson(findings, jsonPath);
            global::System.Diagnostics.Debug.WriteLine($"  JSON report: {jsonPath}");
            global::System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════════════════════");
        }

        return tree;
    }

    // ── Task row (icon-heavy, missing automation names) ─────────────
    static Element TaskRow(
        TaskItem task,
        int index,
        Action<Func<List<TaskItem>, List<TaskItem>>> updateTasks,
        Action<string> setStatusMsg)
    {
        return Border(
            HStack(12,
                CheckBox(task.Done, done =>
                {
                    updateTasks(list =>
                    {
                        var copy = new List<TaskItem>(list);
                        var idx = copy.FindIndex(t => t.Id == task.Id);
                        if (idx >= 0) copy[idx] = task with { Done = done };
                        return copy;
                    });
                    setStatusMsg(done ? $"Completed: {task.Title}" : $"Reopened: {task.Title}");
                }),

                VStack(2,
                    Factories.Text(task.Title).SemiBold()
                        .Opacity(task.Done ? 0.5 : 1.0),
                    Caption($"{task.Assignee} · {task.Priority}")
                        .Opacity(0.6)
                ),

                HStack(4,
                    PriorityBadge(task.Priority),

                    // A11Y_001: Icon-only buttons without AutomationName
                    Button(Icon("edit"), () =>
                    {
                        setStatusMsg($"Editing: {task.Title}");
                    }),
                    Button(Icon("delete"), () =>
                    {
                        updateTasks(list => list.Where(t => t.Id != task.Id).ToList());
                        setStatusMsg($"Deleted: {task.Title}");
                    })
                ).HAlign(HorizontalAlignment.Right)
            ).Padding(8, 6, 8, 6)
        )
        .CornerRadius(4)
        .Background(Theme.ControlFill)
        // No .WithKey() on list items — not a11y, but a reconciler best-practice gap
        ;
    }

    // ── Filter button with concrete brush (A11Y_005) ────────────────
    static Element FilterBtn(string label, string current, Action<string> set) =>
        Button(label, () => set(label))
            .Background(label == current ? "#0078d4" : "#00000000");

    // ── Priority badge (decorative image not hidden — A11Y_002) ─────
    static Element PriorityBadge(string priority)
    {
        var (color, icon) = priority switch
        {
            "High" => ("#d13438", "Important"),
            "Low" => ("#498205", "Download"),
            _ => ("#8a8886", "Remove"),
        };

        // A11Y_002: Icon used decoratively but not hidden from UIA
        return Border(
            HStack(4,
                Image($"ms-appx:///Assets/{icon}.png").Size(14, 14),
                Caption(priority)
            ).Padding(4, 2, 8, 2)
        ).CornerRadius(4).Background(color).Opacity(0.85);
    }
}

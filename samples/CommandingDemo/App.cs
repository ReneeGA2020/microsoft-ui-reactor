// CommandingDemo — Demonstrates Reactor's commanding system.
// Shows: StandardCommand, Command, UseCommand, CommandHost, parameterized commands,
// per-site overrides with `with`, context-based command sharing, and ICommand interop.

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;
using static Microsoft.UI.Reactor.Core.Theme;
using Windows.System;

ReactorApp.Run<CommandingDemoApp>("Commanding Demo", width: 900, height: 700);

// ─── Root component ───────────────────────────────────────────────────────────

class CommandingDemoApp : Component
{
    public override Element Render()
    {
        var (selectedTab, setSelectedTab) = UseState(0);

        var tabs = new[] { "Standard Commands", "Async / UseCommand", "Parameterized", "CommandHost", "Per-Site Override", "Context Sharing" };

        return VStack(
            TextBlock("Reactor Commanding Demo").FontSize(24).Bold().Margin(16, 16, 16, 8),
            HStack(8,
                tabs.Select((tab, i) =>
                    Button(tab, () => setSelectedTab(i))
                        .Background(i == selectedTab ? Accent : SubtleFill)
                        .Margin(0, 0, 0, 8)
                ).ToArray()
            ).Margin(16, 0),
            selectedTab switch
            {
                0 => Component<StandardCommandsDemo>(),
                1 => Component<AsyncCommandDemo>(),
                2 => Component<ParameterizedCommandDemo>(),
                3 => Component<CommandHostDemo>(),
                4 => Component<PerSiteOverrideDemo>(),
                5 => Component<ContextSharingDemo>(),
                _ => Empty(),
            }
        );
    }
}

// ─── 1. Standard Commands Demo ────────────────────────────────────────────────

class StandardCommandsDemo : Component
{
    public override Element Render()
    {
        var (text, setText) = UseState("Select some text and use Cut/Copy/Paste from the toolbar or menu.");
        var (clipboard, setClipboard) = UseState("");
        var (selectedText, setSelectedText) = UseState("");
        var (selStart, setSelStart) = UseState<int?>(null);
        var (selLength, setSelLength) = UseState<int?>(null);

        var hasSelection = selectedText.Length > 0;

        // Define commands once — use in toolbar, menu, and context menu
        var cut = StandardCommand.Cut(() =>
        {
            if (selStart is not null)
            {
                var start = selStart.Value;
                var len = selectedText.Length;
                setClipboard(selectedText);
                setText(text.Remove(start, len));
                setSelStart(start);        // keep caret at cut position
                setSelLength(0);
                setSelectedText("");
            }
        }, canExecute: hasSelection);

        var copy = StandardCommand.Copy(() =>
        {
            setClipboard(selectedText);
        }, canExecute: hasSelection);

        var paste = StandardCommand.Paste(() =>
        {
            if (selStart is not null && selLength is not null)
            {
                // Replace selection with clipboard content
                var start = selStart.Value;
                var len = selLength.Value;
                var newText = text.Remove(start, len).Insert(start, clipboard);
                setText(newText);
                setSelStart(start + clipboard.Length);
                setSelLength(0);
                setSelectedText("");
            }
            else
            {
                setText(text + clipboard);
            }
        }, canExecute: clipboard.Length > 0);

        var selectAll = StandardCommand.SelectAll(() =>
        {
            setSelStart(0);
            setSelLength(text.Length);
            setSelectedText(text);
        });

        return VStack(8,
            TextBlock("Standard Commands — define once, use everywhere").Bold().Margin(16, 8),

            // CommandBar with same commands
            CommandBar(primaryCommands: [
                AppBarButton(cut),
                AppBarButton(copy),
                AppBarButton(paste),
                AppBarSeparator(),
                AppBarButton(selectAll),
            ]),

            // MenuBar with same commands
            MenuBar(
                Menu("Edit",
                    MenuItem(cut),
                    MenuItem(copy),
                    MenuItem(paste),
                    MenuSeparator(),
                    MenuItem(selectAll)
                )
            ),

            // Text area — OnSelectionChanged fires with (selectedText, selectionStart, selectionLength)
            (TextField(text, setText, placeholder: "Type here...") with
            {
                OnSelectionChanged = (sel, start, len) =>
                {
                    setSelectedText(sel);
                    setSelStart(start);
                    setSelLength(len);
                },
                SelectionStart = selStart,
                SelectionLength = selLength,
            }).Margin(16, 8),

            // Status
            TextBlock($"Clipboard: \"{clipboard}\"").Margin(16, 4).FontSize(12),
            TextBlock($"Selected: \"{selectedText}\"").Margin(16, 4).FontSize(12),
            TextBlock($"Has selection: {hasSelection}").Margin(16, 4).FontSize(12)
        );
    }
}

// ─── 2. Async Command Demo (UseCommand) ──────────────────────────────────────

class AsyncCommandDemo : Component
{
    public override Element Render()
    {
        var (saveCount, setSaveCount) = UseState(0);
        var (lastStatus, setLastStatus) = UseState("Ready");

        // Define async command — UseCommand wraps it with IsExecuting tracking
        var saveCmd = UseCommand(StandardCommand.Save(async () =>
        {
            setLastStatus("Saving...");
            await Task.Delay(2000); // Simulate save
            setSaveCount(saveCount + 1);
            setLastStatus($"Saved! (total: {saveCount + 1})");
        }));

        return VStack(8,
            TextBlock("Async Commands — UseCommand auto-tracks IsExecuting").Bold().Margin(16, 8),

            HStack(8,
                Button(saveCmd).Margin(16, 0),
                saveCmd.IsExecuting
                    ? ProgressRing().Width(20).Height(20)
                    : Empty()
            ),

            TextBlock($"Status: {lastStatus}").Margin(16, 4),
            TextBlock($"IsExecuting: {saveCmd.IsExecuting}").Margin(16, 4).FontSize(12),
            TextBlock($"IsEnabled: {saveCmd.IsEnabled}").Margin(16, 4).FontSize(12),
            TextBlock("Try clicking Save — the button auto-disables during the 2-second operation.").Margin(16, 8).FontSize(12)
        );
    }
}

// ─── 3. Parameterized Command Demo ───────────────────────────────────────────

class ParameterizedCommandDemo : Component
{
    record Item(string Name, string Id);

    public override Element Render()
    {
        var (items, updateItems) = UseReducer(new Item[]
        {
            new("Apple", "1"), new("Banana", "2"), new("Cherry", "3"),
            new("Date", "4"), new("Elderberry", "5"),
        });

        // Single command definition — parameter bound per-item
        var deleteCmd = new Command<Item>
        {
            Label = "Delete",
            Icon = new SymbolIconData("Delete"),
            Execute = item => updateItems(list => list.Where(i => i.Id != item.Id).ToArray()),
        };

        return VStack(8,
            TextBlock("Parameterized Commands — Command<T> with per-item binding").Bold().Margin(16, 8),

            ForEach(items, (item, i) =>
                HStack(8,
                    TextBlock($"{item.Name}").Width(120),
                    Button($"Delete {item.Name}", () => deleteCmd.Execute?.Invoke(item))
                ).Margin(16, 2)
            ),

            TextBlock($"Items remaining: {items.Length}").Margin(16, 8).FontSize(12)
        );
    }
}

// ─── 4. CommandHost Demo ─────────────────────────────────────────────────────

class CommandHostDemo : Component
{
    public override Element Render()
    {
        var (log, setLog) = UseState("Press Ctrl+S, Ctrl+Z, or Ctrl+Y within the blue region.");

        var save = StandardCommand.Save(() => setLog($"[{DateTime.Now:HH:mm:ss}] Save triggered via Ctrl+S"));
        var undo = StandardCommand.Undo(() => setLog($"[{DateTime.Now:HH:mm:ss}] Undo triggered via Ctrl+Z"));
        var redo = StandardCommand.Redo(() => setLog($"[{DateTime.Now:HH:mm:ss}] Redo triggered via Ctrl+Y"));

        return VStack(8,
            TextBlock("CommandHost — keyboard accelerators scoped to a subtree").Bold().Margin(16, 8),

            // INSIDE scope — shortcuts should work here
            CommandHost([save, undo, redo],
                VStack(8,
                    TextBlock("INSIDE CommandHost scope (blue) — Ctrl+S / Ctrl+Z / Ctrl+Y work here:").Bold(),
                    TextField("", _ => { }, placeholder: "Click here and press Ctrl+S..."),
                    TextBlock(log).FontSize(12)
                ).Padding(16).Background(new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    global::Windows.UI.Color.FromArgb(30, 100, 149, 237)))
                .CornerRadius(8)
            ).Margin(16, 0),

            // OUTSIDE scope — shortcuts should NOT work here
            VStack(8,
                TextBlock("OUTSIDE CommandHost scope (red) — Ctrl+S / Ctrl+Z / Ctrl+Y should NOT work here:").Bold(),
                TextField("", _ => { }, placeholder: "Click here and press Ctrl+S — nothing should happen...")
            ).Padding(16).Margin(16, 0).Background(new Microsoft.UI.Xaml.Media.SolidColorBrush(
                global::Windows.UI.Color.FromArgb(30, 237, 100, 100)))
            .CornerRadius(8)
        );
    }
}

// ─── 5. Per-Site Override Demo ───────────────────────────────────────────────

class PerSiteOverrideDemo : Component
{
    public override Element Render()
    {
        var (lastAction, setLastAction) = UseState("(none)");

        var deleteCmd = StandardCommand.Delete(() => setLastAction("Deleted!"));

        return VStack(8,
            TextBlock("Per-Site Overrides — same command, different presentation with `with`").Bold().Margin(16, 8),

            HStack(16,
                VStack(4,
                    TextBlock("Toolbar:").FontSize(12),
                    CommandBar(primaryCommands: [
                        AppBarButton(deleteCmd),  // Shows "Delete" with icon
                    ])
                ),
                VStack(4,
                    TextBlock("Context menu (overridden label):").FontSize(12),
                    MenuBar(
                        Menu("Actions",
                            MenuItem(deleteCmd with { Label = "Remove selected item" }),
                            MenuItem(deleteCmd with { Label = "Erase permanently", Icon = new SymbolIconData("Clear") })
                        )
                    )
                )
            ).Margin(16, 0),

            TextBlock($"Last action: {lastAction}").Margin(16, 8)
        );
    }
}

// ─── 6. Context-Based Command Sharing Demo ──────────────────────────────────

record EditorCommands(Command Save, Command Undo, Command Redo);

static class EditorCommandsContext
{
    public static readonly Context<EditorCommands?> Instance = new(null);
}

class ToolbarComponent : Component
{
    public override Element Render()
    {
        var commands = UseContext(EditorCommandsContext.Instance);

        if (commands is null)
            return TextBlock("No editor commands available").FontSize(12);

        return VStack(4,
            TextBlock("Toolbar (consumes commands via context):").FontSize(12).Bold(),
            CommandBar(primaryCommands: [
                AppBarButton(commands.Save),
                AppBarButton(commands.Undo),
                AppBarButton(commands.Redo),
            ])
        );
    }
}

class EditorComponent : Component
{
    public override Element Render()
    {
        var (content, setContent) = UseState("Edit this text...");
        var (history, updateHistory) = UseReducer(new string[] { "Edit this text..." });

        return VStack(4,
            TextBlock("Editor (provides text + history state):").FontSize(12).Bold(),
            TextField(content, newText =>
            {
                setContent(newText);
                updateHistory(h => [.. h, newText]);
            })
        );
    }
}

class ContextSharingDemo : Component
{
    public override Element Render()
    {
        // Command state lives here so context wraps BOTH toolbar and editor
        var (content, setContent) = UseState("Edit this text...");
        var (history, updateHistory) = UseReducer(new string[] { "Edit this text..." });
        var (saveStatus, setSaveStatus) = UseState("");

        var save = UseCommand(StandardCommand.Save(async () =>
        {
            setSaveStatus("Saving...");
            await Task.Delay(500);
            setSaveStatus($"Saved at {DateTime.Now:HH:mm:ss}");
        }));
        var undo = StandardCommand.Undo(() =>
        {
            if (history.Length > 1)
            {
                var prev = history[^2];
                setContent(prev);
                updateHistory(h => h[..^1]);
            }
        }, canExecute: history.Length > 1);
        var redo = StandardCommand.Redo(() => { }, canExecute: false);

        var commands = new EditorCommands(save, undo, redo);

        // Provide context so both toolbar and editor can access commands
        return VStack(8,
            TextBlock("Context Sharing — parent provides, children consume").Bold().Margin(16, 8),

            VStack(8,
                Component<ToolbarComponent>(),
                VStack(4,
                    TextBlock("Editor:").FontSize(12).Bold(),
                    TextField(content, newText =>
                    {
                        setContent(newText);
                        updateHistory(h => [.. h, newText]);
                    })
                ),
                saveStatus.Length > 0 ? TextBlock(saveStatus).Margin(0, 4).FontSize(12) : Empty()
            ).Margin(16, 0).Provide(EditorCommandsContext.Instance, commands),

            TextBlock("The parent component owns the commands and provides them via Context.").Margin(16, 8).FontSize(12),
            TextBlock("The toolbar consumes them via UseContext — no prop drilling needed.").Margin(16, 4).FontSize(12)
        );
    }
}

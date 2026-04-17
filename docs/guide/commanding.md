
# Commanding

A `ReactorCommand` bundles an action with its label, icon, keyboard accelerator,
and enabled state into a single object. Define it once and use it across
buttons, menus, and toolbars — the metadata stays consistent everywhere.

## Defining a Command

Create a command with the properties you need:

```csharp
class BasicCommandExample : Component
{
    public override Element Render()
    {
        var (text, setText) = UseState("Hello, World!");
        var (saved, setSaved) = UseState(false);

        var saveCmd = new Command
        {
            Label = "Save",
            Execute = () => setSaved(true),
            CanExecute = !saved,
            Icon = SymbolIcon("Save"),
            Accelerator = Accelerator(VirtualKey.S, VirtualKeyModifiers.Control)
        };

        return VStack(12,
            TextField(text, v => { setText(v); setSaved(false); })
                .Width(400),
            HStack(8,
                Button(saveCmd),
                When(saved, () => Text("Saved!").Foreground(Theme.SystemSuccess))
            )
        ).Padding(24);
    }
}
```

![Editor with save command](images/commanding/basic-command.png)

Pass a `ReactorCommand` to `Button()`, `MenuItem()`, or `AppBarButton()` and the
label, icon, accelerator, and enabled state are all wired automatically. See
[Components](components.md) for the full set of controls that accept commands.
You don't set them individually on each control.

## Standard Commands

`StandardCommand` provides factory methods for the 16 most common application
commands. Each comes with a label, icon, and keyboard accelerator preset:

```csharp
class StandardCommandsExample : Component
{
    public override Element Render()
    {
        var (log, updateLog) = UseReducer(new List<string>());

        var cut = StandardCommand.Cut(() => updateLog(l => [.. l, "Cut"]));
        var copy = StandardCommand.Copy(() => updateLog(l => [.. l, "Copy"]));
        var paste = StandardCommand.Paste(() => updateLog(l => [.. l, "Paste"]));
        var undo = StandardCommand.Undo(
            () => updateLog(l => [.. l, "Undo"]),
            canExecute: log.Count > 0);

        return VStack(12,
            CommandBar(
                primaryCommands: new[] { AppBarButton(cut), AppBarButton(copy),
                    AppBarButton(paste), AppBarButton(undo) }
            ),
            Text($"Actions: {string.Join(", ", log)}").Padding(12)
        ).Padding(24);
    }
}
```

![Command bar with standard commands](images/commanding/standard-commands.png)

Available standard commands: `Cut`, `Copy`, `Paste`, `Undo`, `Redo`, `Delete`,
`SelectAll`, `Save`, `Open`, `Close`, `Share`, `Play`, `Pause`, `Stop`,
`Forward`, `Backward`.

## Async Commands and UseCommand

When a command has an `ExecuteAsync` action, use the `UseCommand`
[hook](hooks.md) to get automatic `IsExecuting` tracking. The button disables
itself while the async operation runs:

```csharp
class AsyncCommandExample : Component
{
    public override Element Render()
    {
        var (status, setStatus) = UseState("Ready");

        var saveCmd = UseCommand(new Command
        {
            Label = "Save to Cloud",
            ExecuteAsync = async () =>
            {
                setStatus("Saving...");
                await Task.Delay(2000);
                setStatus("Saved at " + DateTime.Now.ToString("HH:mm:ss"));
            },
            Icon = SymbolIcon("Save")
        });

        return VStack(12,
            HStack(8,
                Button(saveCmd),
                Text(status).Foreground(Theme.SecondaryText)
            ),
            When(saveCmd.IsExecuting, () =>
                ProgressRing().Width(20).Height(20))
        ).Padding(24);
    }
}
```

![Save button disabled during async operation](images/commanding/async-command.png)

`UseCommand` wraps the async action with a re-entrance guard — if the user
clicks again while the command is executing, the click is ignored. It also
sets `IsExecuting = true` during execution so you can show progress indicators.

## Command Bar Integration

Use `CommandBar` with `AppBarButton` to build a toolbar. Each button can be
driven by a `ReactorCommand`:

```csharp
class CommandBarExample : Component
{
    public override Element Render()
    {
        var (text, setText) = UseState("Edit me");

        var save = StandardCommand.Save(() => { });
        var copy = StandardCommand.Copy(() => { });
        var delete = StandardCommand.Delete(
            () => setText(""), canExecute: text.Length > 0);

        return VStack(0,
            CommandBar(
                primaryCommands: new[] {
                    AppBarButton(save), AppBarButton(copy) },
                secondaryCommands: new[] {
                    AppBarButton(delete) }
            ),
            TextField(text, setText).Margin(16)
        );
    }
}
```

![Command bar with primary and secondary commands](images/commanding/command-bar.png)

Primary commands appear as icon buttons in the toolbar. Secondary commands
go into the overflow menu.

## Menu Integration

Commands work in menu bars too:

```csharp
class MenuBarExample : Component
{
    public override Element Render()
    {
        var (text, setText) = UseState("Document text");

        var save = StandardCommand.Save(() => { });
        var close = StandardCommand.Close(() => setText(""));
        var undo = StandardCommand.Undo(() => { });
        var redo = StandardCommand.Redo(() => { });

        return VStack(0,
            MenuBar(
                Menu("File", MenuItem(save), MenuItem(close)),
                Menu("Edit", MenuItem(undo), MenuItem(redo))
            ),
            Text(text).Padding(16)
        );
    }
}
```

![Menu bar with File and Edit menus](images/commanding/menu-bar.png)

The accelerator text (like Ctrl+S) appears automatically in the menu item.

## Tips

**Use `StandardCommand` for common operations.** It saves you from manually
specifying icons and keyboard accelerators for the 16 most common actions.

**Always use `UseCommand` for async commands.** It prevents double-execution,
tracks `IsExecuting`, and triggers re-renders at the right times.

**Check `command.IsExecuting` for loading indicators.** After `UseCommand`
wraps an async command, read `.IsExecuting` to show spinners or progress text.

**Commands are records — use `with` to customize.** Override the label for
[localization](localization.md): `StandardCommand.Save(action) with { Label = "Guardar" }`.

**One command, many surfaces.** Define a command once and pass it to `Button`,
`MenuItem`, `AppBarButton`, and `CommandBarFlyout`. The metadata is always
consistent.

## Next Steps

- **[Effects and Lifecycle](effects.md)** — Previous: run side effects for timers, data loading, and cleanup
- **[Context](context.md)** — Next: share data across the component tree without prop drilling
- **[Hooks](hooks.md)** — Learn about UseCommand and other hooks that power commanding
- **[Navigation](navigation.md)** — Wire commands to page transitions and routing actions
- **[Localization](localization.md)** — Localize command labels and accelerator text

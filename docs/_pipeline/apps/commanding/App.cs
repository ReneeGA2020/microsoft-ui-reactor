using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;
using Microsoft.UI.Xaml;
using Windows.System;

ReactorApp.Run<CommandingApp>("Commanding", width: 650, height: 550
#if DEBUG
    , preview: true
#endif
);

// <snippet:basic-command>
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
// </snippet:basic-command>

// <snippet:standard-commands>
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
// </snippet:standard-commands>

// <snippet:async-command>
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
// </snippet:async-command>

// <snippet:command-bar>
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
// </snippet:command-bar>

// <snippet:menu-bar>
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
// </snippet:menu-bar>

// Main app
class CommandingApp : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(24,
                Heading("Commanding"),
                Component<BasicCommandExample>(),
                Component<StandardCommandsExample>(),
                Component<AsyncCommandExample>(),
                Component<CommandBarExample>(),
                Component<MenuBarExample>()
            ).Padding(24)
        );
    }
}

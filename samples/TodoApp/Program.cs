// TodoApp — A real starter template demonstrating Reactor's component model.
// Shows: UseReducer for state management, Command for actions, Theme tokens.

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;
using static Microsoft.UI.Reactor.Core.Theme;

ReactorApp.Run<TodoApp>("Todo App", width: 700, height: 550);

// ─── State ───────────────────────────────────────────────────────────────────

record TodoItem(string Id, string Text, bool IsCompleted);

record TodoState(IReadOnlyList<TodoItem> Items, string NewItemText, string Filter)
{
    public static readonly TodoState Initial = new([], "", "all");
}

abstract record TodoAction;
record AddItem : TodoAction;
record ToggleItem(string Id) : TodoAction;
record DeleteItem(string Id) : TodoAction;
record SetNewItemText(string Text) : TodoAction;
record SetFilter(string Filter) : TodoAction;

// ─── Reducer ─────────────────────────────────────────────────────────────────

static class TodoReducer
{
    public static TodoState Reduce(TodoState state, TodoAction action) => action switch
    {
        AddItem when !string.IsNullOrWhiteSpace(state.NewItemText) =>
            state with
            {
                Items = [.. state.Items, new(Guid.NewGuid().ToString(), state.NewItemText.Trim(), false)],
                NewItemText = ""
            },
        ToggleItem t => state with
        {
            Items = state.Items.Select(i =>
                i.Id == t.Id ? i with { IsCompleted = !i.IsCompleted } : i).ToList()
        },
        DeleteItem d => state with
        {
            Items = state.Items.Where(i => i.Id != d.Id).ToList()
        },
        SetNewItemText s => state with { NewItemText = s.Text },
        SetFilter f => state with { Filter = f.Filter },
        _ => state
    };
}

// ─── Root component ──────────────────────────────────────────────────────────

class TodoApp : Component
{
    public override Element Render()
    {
        var (state, dispatch) = UseReducer<TodoState, TodoAction>(TodoReducer.Reduce, TodoState.Initial);

        var filtered = state.Filter switch
        {
            "active" => state.Items.Where(i => !i.IsCompleted).ToArray(),
            "completed" => state.Items.Where(i => i.IsCompleted).ToArray(),
            _ => state.Items.ToArray()
        };

        var remaining = state.Items.Count(i => !i.IsCompleted);

        var addCmd = new Command
        {
            Label = "Add",
            Execute = () => dispatch(new AddItem()),
            CanExecute = !string.IsNullOrWhiteSpace(state.NewItemText),
        };

        return VStack(0,
            // Header
            TextBlock("todos").FontSize(36)
                .Set(t => t.FontWeight = Microsoft.UI.Text.FontWeights.Light)
                .Foreground(AccentText)
                .HAlign(HorizontalAlignment.Center)
                .Margin(0, 16, 0, 8),

            // Input bar
            HStack(8,
                TextField(state.NewItemText, v => dispatch(new SetNewItemText(v)))
                    .Set(tb => tb.PlaceholderText = "What needs to be done?")
                    .HAlign(HorizontalAlignment.Stretch),
                Button(addCmd)
            ).Padding(16, 8, 16, 8)
             .Background(CardBackground),

            // List
            ScrollView(
                VStack(0,
                    filtered.Select(item =>
                        TodoRow(item, dispatch)
                    ).ToArray()
                )
            ).Flex(grow: 1, basis: 0),

            // Footer
            HStack(8,
                TextBlock($"{remaining} item{(remaining == 1 ? "" : "s")} left")
                    .FontSize(12).Foreground(SecondaryText)
                    .VAlign(VerticalAlignment.Center),
                Empty().HAlign(HorizontalAlignment.Stretch),
                FilterButton("All", "all", state.Filter, dispatch),
                FilterButton("Active", "active", state.Filter, dispatch),
                FilterButton("Completed", "completed", state.Filter, dispatch)
            ).Padding(12, 8, 12, 8)
             .WithBorder(DividerStroke)
        ).Background(SolidBackground)
         .MaxWidth(600)
         .HAlign(HorizontalAlignment.Center);
    }

    static Element TodoRow(TodoItem item, Action<TodoAction> dispatch) =>
        HStack(8,
            CheckBox(item.IsCompleted, _ => dispatch(new ToggleItem(item.Id))),
            TextBlock(item.Text)
                .FontSize(14)
                .Opacity(item.IsCompleted ? 0.5 : 1)
                .Set(t =>
                {
                    if (item.IsCompleted)
                        t.TextDecorations = global::Windows.UI.Text.TextDecorations.Strikethrough;
                })
                .VAlign(VerticalAlignment.Center),
            Empty().HAlign(HorizontalAlignment.Stretch),
            Button("✕", () => dispatch(new DeleteItem(item.Id)))
                .Set(b =>
                {
                    b.Padding = new Thickness(6, 2, 6, 2);
                    b.MinWidth = 0;
                    b.MinHeight = 0;
                })
        ).Padding(12, 6, 12, 6)
         .WithKey(item.Id);

    static Element FilterButton(string label, string filter, string active, Action<TodoAction> dispatch) =>
        Button(label, () => dispatch(new SetFilter(filter)))
            .Background(filter == active ? Accent : SubtleFill)
            .Set(b =>
            {
                b.Padding = new Thickness(10, 4, 10, 4);
                b.MinHeight = 0;
            });
}

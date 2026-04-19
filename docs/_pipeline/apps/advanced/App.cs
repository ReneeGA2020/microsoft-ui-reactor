using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Component = Microsoft.UI.Reactor.Core.Component;

ReactorApp.Run<AdvancedApp>("Advanced Patterns", width: 650, height: 700
#if DEBUG
    , preview: true
#endif
);

// <snippet:error-boundary>
class ErrorBoundaryDemo : Component
{
    public override Element Render()
    {
        return VStack(12,
            SubHeading("Error Boundary"),
            ErrorBoundary(
                Component<BuggyComponent>(),
                (Exception ex) => VStack(8,
                    TextBlock("Something went wrong").Bold()
                        .Foreground("#d13438"),
                    TextBlock(ex.Message).FontSize(12).Opacity(0.7)
                ).Padding(12)
                 .Background("#fde7e9")
                 .CornerRadius(8)
            )
        ).Padding(24);
    }
}

class BuggyComponent : Component
{
    public override Element Render()
    {
        var (crash, setCrash) = UseState(false);
        if (crash) throw new InvalidOperationException("Oops!");
        return Button("Click to crash", () => setCrash(true));
    }
}
// </snippet:error-boundary>

// <snippet:memo-subtree>
class MemoSubtreeDemo : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);
        var (label, setLabel) = UseState("Expensive");

        return VStack(12,
            SubHeading("Memo"),
            TextBlock($"Parent renders: click count = {count}"),
            Button("Increment", () => setCount(count + 1)),
            Memo(ctx =>
            {
                // This subtree only re-renders when label changes
                return Border(
                    VStack(4,
                        TextBlock($"Memoized: {label}").Bold(),
                        TextBlock("Skips re-render when deps unchanged")
                            .FontSize(12).Opacity(0.6)
                    ).Padding(12)
                ).Background("#f0f0f0").CornerRadius(8);
            }, label)
        ).Padding(24);
    }
}
// </snippet:memo-subtree>

// <snippet:set-escape-hatch>
class SetEscapeHatchDemo : Component
{
    public override Element Render()
    {
        return VStack(12,
            SubHeading(".Set() Escape Hatch"),
            Button("Custom Tooltip", () => { })
                .Set(btn =>
                {
                    Microsoft.UI.Xaml.Controls.ToolTipService
                        .SetToolTip(btn, "This is a native tooltip");
                    btn.Padding = new Thickness(20, 10, 20, 10);
                }),
            TextBlock("Styled via .Set()")
                .Set(tb =>
                {
                    tb.TextWrapping = TextWrapping.WrapWholeWords;
                    tb.CharacterSpacing = 80;
                    tb.IsTextSelectionEnabled = true;
                })
        ).Padding(24);
    }
}
// </snippet:set-escape-hatch>

// <snippet:observable-viewmodel-class>
class SettingsViewModel : INotifyPropertyChanged
{
    private string _userName = "Alice";
    private bool _darkMode;
    private int _fontSize = 14;

    public string UserName
    {
        get => _userName;
        set { _userName = value; Notify(nameof(UserName)); }
    }
    public bool DarkMode
    {
        get => _darkMode;
        set { _darkMode = value; Notify(nameof(DarkMode)); }
    }
    public int FontSize
    {
        get => _fontSize;
        set { _fontSize = value; Notify(nameof(FontSize)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify(string n) =>
        PropertyChanged?.Invoke(this, new(n));
}
// </snippet:observable-viewmodel-class>

// <snippet:observable-viewmodel>
class ObservableTreeDemo : Component
{
    private static readonly SettingsViewModel _vm = new();

    public override Element Render()
    {
        var vm = UseObservableTree(_vm);

        return VStack(12,
            SubHeading("UseObservableTree"),
            TextField(vm.UserName, v => vm.UserName = v,
                header: "User Name"),
            ToggleSwitch(vm.DarkMode, v => vm.DarkMode = v,
                header: "Dark Mode"),
            Slider(vm.FontSize, 10, 32, v => vm.FontSize = (int)v),
            TextBlock($"Preview: {vm.UserName}")
                .FontSize(vm.FontSize).Bold()
        ).Padding(24);
    }
}
// </snippet:observable-viewmodel>

// <snippet:observable-collection>
class ObservableCollectionDemo : Component
{
    private static readonly ObservableCollection<string> _tasks = new()
        { "Review pull request", "Update documentation" };

    public override Element Render()
    {
        var tasks = UseCollection(_tasks);
        var (input, setInput) = UseState("");

        return VStack(12,
            SubHeading("UseCollection"),
            HStack(8,
                TextField(input, setInput, placeholder: "New task")
                    .Width(200),
                Button("Add", () => {
                    if (!string.IsNullOrWhiteSpace(input))
                    { _tasks.Add(input.Trim()); setInput(""); }
                })
            ),
            TextBlock($"{tasks.Count} tasks:").SemiBold(),
            VStack(4, tasks.Select((task, i) =>
                HStack(8,
                    TextBlock($"{i + 1}. {task}"),
                    Button("Remove", () => _tasks.RemoveAt(i))
                ).WithKey($"task-{i}-{task}")
            ).ToArray())
        ).Padding(24);
    }
}
// </snippet:observable-collection>

// Main app
class AdvancedApp : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(24,
                Heading("Advanced Patterns"),
                Component<ErrorBoundaryDemo>(),
                Component<MemoSubtreeDemo>(),
                Component<SetEscapeHatchDemo>(),
                Component<ObservableTreeDemo>(),
                Component<ObservableCollectionDemo>()
            ).Padding(24)
        );
    }
}

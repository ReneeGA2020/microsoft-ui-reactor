using System.Diagnostics;
using CmdPerf.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace CmdPerf.XamlCmd;

public sealed partial class MainWindow : Window
{
    private readonly CmdPerfTracker _perf = new();
    private readonly Dictionary<string, XamlUICommand> _xamlCommandMap = new(500);
    private readonly List<XamlUICommand> _xamlCommands = new(500);
    private EnableFlags _currentFlags = EnableFlags.None;
    private readonly CheckBox[] _flagCheckboxes = new CheckBox[CommandSet.FlagNames.Length];
    private bool _suppressCheckboxEvents;
    private DispatcherTimer? _autoToggleTimer;
    private int _autoToggleIndex;

    private TextBlock _lastCmdText = null!;
    private TextBlock _commandCountText = null!;
    private TextBlock _enabledCountText = null!;
    private TextBlock _toggleMsText = null!;
    private TextBlock _memoryText = null!;

    public MainWindow()
    {
        this.InitializeComponent();
        Title = "CmdPerf.XamlCmd - XamlUICommand Benchmark";
        _perf.BeginMount();
        BuildUI();
        _perf.EndMount();
        UpdateStatusBar();

        if (App.Options.Headless)
        {
            RunHeadlessScenarios();
        }
    }

    private void BuildUI()
    {
        // Create all 500 XamlUICommand instances
        CreateCommands();

        // Root layout
        Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // MenuBar
        Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // CommandBar
        Root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
        Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Status bar

        // MenuBar
        var menuBar = BuildMenuBar();
        Grid.SetRow(menuBar, 0);
        Root.Children.Add(menuBar);

        // CommandBar (toolbar)
        var commandBar = BuildCommandBar();
        Grid.SetRow(commandBar, 1);
        Root.Children.Add(commandBar);

        // Content area: checkboxes + last command
        var contentPanel = BuildContentPanel();
        Grid.SetRow(contentPanel, 2);
        Root.Children.Add(contentPanel);

        // Status bar
        var statusBar = BuildStatusBar();
        Grid.SetRow(statusBar, 3);
        Root.Children.Add(statusBar);
    }

    // ── Command creation ────────────────────────────────────────────

    private void CreateCommands()
    {
        foreach (var cmd in CommandSet.All)
        {
            var xamlCmd = new XamlUICommand
            {
                Label = cmd.Label,
                Description = cmd.Id,
            };

            if (cmd.IconGlyph != null && Enum.TryParse<Symbol>(cmd.IconGlyph, out var symbol))
            {
                xamlCmd.IconSource = new SymbolIconSource { Symbol = symbol };
            }

            // Capture for closure
            var capturedCmd = cmd;

            xamlCmd.ExecuteRequested += (sender, e) =>
            {
                _lastCmdText.Text = $"Executed: {capturedCmd.Id} ({capturedCmd.Label})";
            };

            xamlCmd.CanExecuteRequested += (sender, e) =>
            {
                e.CanExecute = CommandSet.IsEnabled(capturedCmd, _currentFlags);
            };

            _xamlCommandMap[cmd.Id] = xamlCmd;
            _xamlCommands.Add(xamlCmd);
        }
    }

    // ── MenuBar ─────────────────────────────────────────────────────

    private MenuBar BuildMenuBar()
    {
        var menuBar = new MenuBar();
        var menuDef = MenuLayout.GetMenuBar();

        foreach (var (title, items) in menuDef)
        {
            var menuBarItem = new MenuBarItem { Title = title };
            AddMenuItems(menuBarItem.Items, items);
            menuBar.Items.Add(menuBarItem);
        }

        return menuBar;
    }

    private void AddMenuItems(IList<MenuFlyoutItemBase> target, object[] items)
    {
        foreach (var item in items)
        {
            switch (item)
            {
                case MenuLayout.MenuItem mi:
                    if (_xamlCommandMap.TryGetValue(mi.CommandId, out var cmd))
                    {
                        target.Add(new MenuFlyoutItem { Command = cmd });
                    }
                    break;

                case MenuLayout.Separator:
                    target.Add(new MenuFlyoutSeparator());
                    break;

                case MenuLayout.SubMenu sub:
                    var subItem = new MenuFlyoutSubItem { Text = sub.Label };
                    AddMenuItems(subItem.Items, sub.Items);
                    target.Add(subItem);
                    break;
            }
        }
    }

    // ── CommandBar (toolbar) ────────────────────────────────────────

    private CommandBar BuildCommandBar()
    {
        var commandBar = new CommandBar { DefaultLabelPosition = CommandBarDefaultLabelPosition.Right };

        foreach (var id in MenuLayout.ToolbarCommandIds)
        {
            if (_xamlCommandMap.TryGetValue(id, out var cmd))
            {
                commandBar.PrimaryCommands.Add(new AppBarButton { Command = cmd });
            }
        }

        return commandBar;
    }

    // ── Content panel ───────────────────────────────────────────────

    private StackPanel BuildContentPanel()
    {
        var panel = new StackPanel
        {
            Padding = new Thickness(16),
            Spacing = 8,
        };

        var header = new TextBlock
        {
            Text = "Enable Flags (toggle to re-evaluate CanExecute on all 500 commands)",
            Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
        };
        panel.Children.Add(header);

        // One checkbox per EnableFlag
        for (int i = 0; i < CommandSet.FlagNames.Length; i++)
        {
            var flagName = CommandSet.FlagNames[i];
            var flagValue = CommandSet.FlagValues[i];

            var checkBox = new CheckBox
            {
                Content = flagName,
                IsChecked = (_currentFlags & flagValue) != 0,
            };

            checkBox.Checked += (s, e) => { if (!_suppressCheckboxEvents) ToggleFlag(flagName, flagValue); };
            checkBox.Unchecked += (s, e) => { if (!_suppressCheckboxEvents) ToggleFlag(flagName, flagValue); };

            _flagCheckboxes[i] = checkBox;
            panel.Children.Add(checkBox);
        }

        // Auto toggle button
        var autoToggleBtn = new Button
        {
            Content = "Auto Toggle",
            Margin = new Thickness(0, 8, 0, 0),
        };
        autoToggleBtn.Click += (s, e) => OnAutoToggleClicked(autoToggleBtn);
        panel.Children.Add(autoToggleBtn);

        // Last executed command
        _lastCmdText = new TextBlock
        {
            Text = "Last command: (none)",
            Margin = new Thickness(0, 16, 0, 0),
        };
        panel.Children.Add(_lastCmdText);

        return panel;
    }

    // ── Status bar ──────────────────────────────────────────────────

    private StackPanel BuildStatusBar()
    {
        var statusBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Padding = new Thickness(16, 8, 16, 8),
            Spacing = 24,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.LightGray),
        };

        _commandCountText = new TextBlock { Text = $"Commands: {CommandSet.All.Length}" };
        _enabledCountText = new TextBlock { Text = "Enabled: --" };
        _toggleMsText = new TextBlock { Text = "Toggle: -- ms" };
        _memoryText = new TextBlock { Text = $"Memory: {Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024):F1} MB" };

        statusBar.Children.Add(_commandCountText);
        statusBar.Children.Add(_enabledCountText);
        statusBar.Children.Add(_toggleMsText);
        statusBar.Children.Add(_memoryText);

        return statusBar;
    }

    // ── Toggle logic ────────────────────────────────────────────────

    private void ToggleFlag(string flagName, EnableFlags flagValue)
    {
        _perf.BeginToggle();
        _currentFlags ^= flagValue;
        foreach (var cmd in _xamlCommands)
        {
            cmd.NotifyCanExecuteChanged();
        }
        _perf.EndToggle(flagName);

        UpdateStatusBar();
    }

    private void UpdateStatusBar()
    {
        var enabledCount = CommandSet.CountEnabled(_currentFlags);
        _commandCountText.Text = $"Commands: {CommandSet.All.Length}";
        _enabledCountText.Text = $"Enabled: {enabledCount}";
        _toggleMsText.Text = $"Toggle: {_perf.LastToggleMs:F2} ms";
        _memoryText.Text = $"Memory: {Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024):F1} MB";
    }

    // ── Auto toggle ────────────────────────────────────────────────

    private void OnAutoToggleClicked(Button btn)
    {
        if (_autoToggleTimer is not null)
        {
            _autoToggleTimer.Stop();
            _autoToggleTimer = null;
            btn.Content = "Auto Toggle";
            return;
        }

        _autoToggleIndex = 0;
        btn.Content = "Stop Auto Toggle";

        _autoToggleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _autoToggleTimer.Tick += (_, _) =>
        {
            int idx = _autoToggleIndex % CommandSet.FlagNames.Length;
            var flagName = CommandSet.FlagNames[idx];
            var flagValue = CommandSet.FlagValues[idx];
            _autoToggleIndex++;

            _perf.BeginToggle();
            _currentFlags ^= flagValue;
            foreach (var cmd in _xamlCommands)
                cmd.NotifyCanExecuteChanged();
            _perf.EndToggle(flagName);

            // Sync checkbox visual state
            _suppressCheckboxEvents = true;
            _flagCheckboxes[idx].IsChecked = (_currentFlags & flagValue) != 0;
            _suppressCheckboxEvents = false;

            UpdateStatusBar();
        };
        _autoToggleTimer.Start();
    }

    // ── Headless automation ─────────────────────────────────────────

    private void RunHeadlessScenarios()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            ExecuteScenarios();
        };
        timer.Start();
    }

    private void ExecuteScenarios()
    {
        var opts = App.Options;
        var scenario = opts.Scenario.ToLowerInvariant();

        if (scenario is "mount" or "all")
        {
            // Mount time already recorded in constructor
            if (scenario == "mount")
            {
                _perf.WriteReportFile("CmdPerf.XamlCmd", "mount");
                Close();
                return;
            }
        }

        if (scenario is "toggle" or "all")
        {
            RunToggleScenario(opts.Iterations);
        }

        if (scenario is "bulk" or "all")
        {
            RunBulkToggleScenario(opts.Iterations);
        }

        _perf.RecordMemoryAfterToggles();
        _perf.WriteReportFile("CmdPerf.XamlCmd", scenario);
        Close();
    }

    private void RunToggleScenario(int iterations)
    {
        for (int n = 0; n < iterations; n++)
        {
            for (int i = 0; i < CommandSet.FlagNames.Length; i++)
            {
                var flagName = CommandSet.FlagNames[i];
                var flagValue = CommandSet.FlagValues[i];

                _perf.BeginToggle();
                _currentFlags ^= flagValue;
                foreach (var cmd in _xamlCommands)
                {
                    cmd.NotifyCanExecuteChanged();
                }
                _perf.EndToggle(flagName);
            }
        }
    }

    private void RunBulkToggleScenario(int iterations)
    {
        for (int n = 0; n < iterations; n++)
        {
            _perf.BeginToggle();

            // Toggle all flags at once
            for (int i = 0; i < CommandSet.FlagValues.Length; i++)
            {
                _currentFlags ^= CommandSet.FlagValues[i];
            }

            foreach (var cmd in _xamlCommands)
            {
                cmd.NotifyCanExecuteChanged();
            }

            _perf.EndBulkToggle();
        }
    }
}

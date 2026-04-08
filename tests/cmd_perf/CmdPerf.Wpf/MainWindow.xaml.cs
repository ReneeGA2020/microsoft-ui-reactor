using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using CmdPerf.Shared;

namespace CmdPerf.Wpf;

public partial class MainWindow : Window
{
    private const string AppName = "CmdPerf.Wpf";

    private readonly CmdCliOptions _options;
    private readonly CmdPerfTracker _perf = new();
    private readonly Dictionary<string, RoutedUICommand> _routedCommands = new(500);
    private readonly Dictionary<string, CommandDef> _cmdById;

    private EnableFlags _currentFlags = EnableFlags.None;
    private TextBlock? _lastCmdText;
    private TextBlock? _mountTimeText;
    private TextBlock? _toggleTimeText;
    private TextBlock? _enabledCountText;
    private TextBlock? _memoryText;
    private readonly CheckBox[] _flagCheckboxes = new CheckBox[CommandSet.FlagNames.Length];
    private bool _suppressCheckboxEvents;
    private int _headlessStep;

    public MainWindow()
    {
        InitializeComponent();
        _options = App.Options;
        _cmdById = CommandSet.All.ToDictionary(c => c.Id);

        _perf.BeginMount();
        BuildCommands();
        BuildUI();
        _perf.EndMount();

        UpdateStatusBar();

        if (_options.Headless)
        {
            Loaded += OnLoaded_Headless;
        }
    }

    // ── Step 1: Create 500 RoutedUICommand instances + CommandBindings ──

    private void BuildCommands()
    {
        foreach (var cmd in CommandSet.All)
        {
            var routed = new RoutedUICommand(cmd.Label, cmd.Id, typeof(MainWindow));
            _routedCommands[cmd.Id] = routed;

            var binding = new CommandBinding(
                routed,
                (sender, e) =>
                {
                    if (_lastCmdText is not null)
                        _lastCmdText.Text = $"Last: {cmd.Label} ({cmd.Id})";
                },
                (sender, e) =>
                {
                    e.CanExecute = CommandSet.IsEnabled(cmd, _currentFlags);
                    e.Handled = true;
                });

            CommandBindings.Add(binding);
        }
    }

    // ── Step 2: Build the full UI programmatically ─────────────────────

    private void BuildUI()
    {
        // Main layout: DockPanel inside the Root grid
        var dock = new DockPanel();
        Root.Children.Add(dock);

        // Menu bar (top)
        var menuBar = BuildMenuBar();
        DockPanel.SetDock(menuBar, Dock.Top);
        dock.Children.Add(menuBar);

        // Toolbar (below menu)
        var toolBarTray = BuildToolBar();
        DockPanel.SetDock(toolBarTray, Dock.Top);
        dock.Children.Add(toolBarTray);

        // Status bar (bottom)
        var statusBar = BuildStatusBar();
        DockPanel.SetDock(statusBar, Dock.Bottom);
        dock.Children.Add(statusBar);

        // Control panel with checkboxes (fills remaining space)
        var controlPanel = BuildControlPanel();
        dock.Children.Add(controlPanel);
    }

    // ── Step 3: Menu bar from MenuLayout ───────────────────────────────

    private Menu BuildMenuBar()
    {
        var menuBar = new Menu();
        var menuDef = MenuLayout.GetMenuBar();

        foreach (var (title, items) in menuDef)
        {
            var topItem = new MenuItem { Header = title };
            BuildMenuItems(topItem.Items, items);
            menuBar.Items.Add(topItem);
        }

        return menuBar;
    }

    private void BuildMenuItems(ItemCollection target, object[] items)
    {
        foreach (var item in items)
        {
            switch (item)
            {
                case MenuLayout.MenuItem mi:
                    if (_routedCommands.TryGetValue(mi.CommandId, out var routed) &&
                        _cmdById.TryGetValue(mi.CommandId, out var cmdDef))
                    {
                        target.Add(new MenuItem { Command = routed, Header = cmdDef.Label });
                    }
                    break;

                case MenuLayout.Separator:
                    target.Add(new Separator());
                    break;

                case MenuLayout.SubMenu sub:
                    var subMenuItem = new MenuItem { Header = sub.Label };
                    BuildMenuItems(subMenuItem.Items, sub.Items);
                    target.Add(subMenuItem);
                    break;
            }
        }
    }

    // ── Step 4: Toolbar with 30 buttons ────────────────────────────────

    private ToolBarTray BuildToolBar()
    {
        var tray = new ToolBarTray();
        var toolBar = new ToolBar();

        foreach (var id in MenuLayout.ToolbarCommandIds)
        {
            if (_routedCommands.TryGetValue(id, out var routed) &&
                _cmdById.TryGetValue(id, out var cmdDef))
            {
                toolBar.Items.Add(new Button { Command = routed, Content = cmdDef.Label });
            }
        }

        tray.ToolBars.Add(toolBar);
        return tray;
    }

    // ── Step 5: Control panel with 10 flag checkboxes ──────────────────

    private StackPanel BuildControlPanel()
    {
        var panel = new StackPanel { Margin = new Thickness(12) };

        panel.Children.Add(new TextBlock
        {
            Text = "Enable Flags",
            FontWeight = FontWeights.Bold,
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 8),
        });

        var checkPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };

        for (int i = 0; i < CommandSet.FlagNames.Length; i++)
        {
            var flagName = CommandSet.FlagNames[i];
            var flagValue = CommandSet.FlagValues[i];

            var cb = new CheckBox
            {
                Content = flagName,
                Margin = new Thickness(0, 0, 16, 6),
                VerticalContentAlignment = VerticalAlignment.Center,
            };

            cb.Checked += (_, _) => { if (!_suppressCheckboxEvents) OnFlagToggled(flagName, flagValue, true); };
            cb.Unchecked += (_, _) => { if (!_suppressCheckboxEvents) OnFlagToggled(flagName, flagValue, false); };

            _flagCheckboxes[i] = cb;
            checkPanel.Children.Add(cb);
        }

        panel.Children.Add(checkPanel);

        var autoToggleBtn = new Button
        {
            Content = "Auto Toggle",
            Padding = new Thickness(16, 6, 16, 6),
            Margin = new Thickness(0, 0, 0, 8),
        };
        autoToggleBtn.Click += OnAutoToggleClicked;
        panel.Children.Add(autoToggleBtn);

        _lastCmdText = new TextBlock
        {
            Text = "Last: (none)",
            FontSize = 14,
            Margin = new Thickness(0, 8, 0, 0),
        };
        panel.Children.Add(_lastCmdText);

        return panel;
    }

    private DispatcherTimer? _autoToggleTimer;
    private int _autoToggleIndex;

    private void OnAutoToggleClicked(object sender, RoutedEventArgs e)
    {
        if (_autoToggleTimer is not null)
        {
            _autoToggleTimer.Stop();
            _autoToggleTimer = null;
            ((Button)sender).Content = "Auto Toggle";
            return;
        }

        _autoToggleIndex = 0;
        ((Button)sender).Content = "Stop Auto Toggle";

        _autoToggleTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(100),
        };
        _autoToggleTimer.Tick += (_, _) =>
        {
            int idx = _autoToggleIndex % CommandSet.FlagNames.Length;
            var flagName = CommandSet.FlagNames[idx];
            var flagValue = CommandSet.FlagValues[idx];
            _autoToggleIndex++;

            _perf.BeginToggle();
            _currentFlags ^= flagValue;
            CommandManager.InvalidateRequerySuggested();
            Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            _perf.EndToggle(flagName);

            // Sync checkbox visual state
            _suppressCheckboxEvents = true;
            _flagCheckboxes[idx].IsChecked = (_currentFlags & flagValue) != 0;
            _suppressCheckboxEvents = false;

            UpdateStatusBar();
        };
        _autoToggleTimer.Start();
    }

    // ── Step 6: Status bar ─────────────────────────────────────────────

    private StatusBar BuildStatusBar()
    {
        var bar = new StatusBar();

        _mountTimeText = new TextBlock { Text = $"Mount: {_perf.MountTimeMs:F2} ms" };
        bar.Items.Add(new StatusBarItem { Content = _mountTimeText });

        bar.Items.Add(new Separator());

        _toggleTimeText = new TextBlock { Text = "Toggle: -- ms" };
        bar.Items.Add(new StatusBarItem { Content = _toggleTimeText });

        bar.Items.Add(new Separator());

        _enabledCountText = new TextBlock { Text = $"Enabled: {CommandSet.CountEnabled(_currentFlags)} / {CommandSet.All.Length}" };
        bar.Items.Add(new StatusBarItem { Content = _enabledCountText });

        bar.Items.Add(new Separator());

        _memoryText = new TextBlock { Text = "Mem: -- MB" };
        bar.Items.Add(new StatusBarItem { Content = _memoryText });

        return bar;
    }

    // ── Flag toggle with measurement ───────────────────────────────────

    private void OnFlagToggled(string flagName, EnableFlags flagValue, bool isChecked)
    {
        _perf.BeginToggle();

        if (isChecked)
            _currentFlags |= flagValue;
        else
            _currentFlags &= ~flagValue;

        CommandManager.InvalidateRequerySuggested();

        // WPF's CommandManager requeries lazily on idle, so force it:
        Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

        _perf.EndToggle(flagName);
        UpdateStatusBar();
    }

    private void UpdateStatusBar()
    {
        if (_mountTimeText is not null)
            _mountTimeText.Text = $"Mount: {_perf.MountTimeMs:F2} ms";
        if (_toggleTimeText is not null)
            _toggleTimeText.Text = $"Toggle: {_perf.LastToggleMs:F3} ms";
        if (_enabledCountText is not null)
            _enabledCountText.Text = $"Enabled: {CommandSet.CountEnabled(_currentFlags)} / {CommandSet.All.Length}";
        if (_memoryText is not null)
        {
            long mem = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
            _memoryText.Text = $"Mem: {mem / (1024.0 * 1024):F1} MB";
        }
    }

    // ── Headless automation ────────────────────────────────────────────

    private void OnLoaded_Headless(object sender, RoutedEventArgs e)
    {
        _headlessStep = 0;

        var timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(10),
        };

        timer.Tick += (_, _) =>
        {
            RunHeadlessStep(timer);
        };

        timer.Start();
    }

    private void RunHeadlessStep(DispatcherTimer timer)
    {
        int iterations = _options.Iterations;
        int totalSteps = iterations * CommandSet.FlagNames.Length;

        if (_headlessStep >= totalSteps)
        {
            timer.Stop();
            _perf.RecordMemoryAfterToggles();
            _perf.WriteReportFile(AppName, _options.Scenario);
            Application.Current.Shutdown();
            return;
        }

        int flagIndex = _headlessStep % CommandSet.FlagNames.Length;
        var flagName = CommandSet.FlagNames[flagIndex];
        var flagValue = CommandSet.FlagValues[flagIndex];

        _perf.BeginToggle();
        _currentFlags ^= flagValue;
        CommandManager.InvalidateRequerySuggested();
        Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        _perf.EndToggle(flagName);

        UpdateStatusBar();
        _headlessStep++;
    }
}

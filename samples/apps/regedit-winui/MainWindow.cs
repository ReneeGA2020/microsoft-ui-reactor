using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using RegeditWinUI.Dialogs;
using RegeditWinUI.Models;
using RegeditWinUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace RegeditWinUI;

public sealed partial class MainWindow : Window
{
    // UI elements
    private TreeView _treeView = null!;
    private ListView _valueList = null!;
    private AutoSuggestBox _addressBar = null!;
    private TextBlock _statusText = null!;
    private ProgressRing _progressRing = null!;
    private Grid _addressBarRow = null!;
    private Grid _statusBarRow = null!;
    private MenuBarItem _favoritesMenu = null!;

    // State
    private string _currentKeyPath = string.Empty;
    private string? _lastSearchText;
    private FindFlags _lastSearchFlags = FindFlags.Keys | FindFlags.Values | FindFlags.Data;
    private readonly SearchService _searchService = new();
    private CancellationTokenSource? _searchCts;
    private string? _lastFoundValueName;

    public MainWindow()
    {
        Title = Strings.AppTitle;
        RestoreWindowState();
        BuildUI();
        LoadRootKeys();
        NavigateToLastKey();
    }

    private void RestoreWindowState()
    {
        try
        {
            var (x, y, w, h) = SettingsService.GetWindowPosition();
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, w, h));
        }
        catch { }
    }

    private void SaveWindowState()
    {
        try
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            var pos = appWindow.Position;
            var size = appWindow.Size;
            SettingsService.SetWindowPosition(pos.X, pos.Y, size.Width, size.Height);
            SettingsService.SetLastKey(_currentKeyPath);
        }
        catch { }
    }

    private void BuildUI()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // MenuBar
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Address bar
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Status bar

        // Row 0: Menu bar
        var menuBar = BuildMenuBar();
        Grid.SetRow(menuBar, 0);
        root.Children.Add(menuBar);

        // Row 1: Address bar
        _addressBarRow = BuildAddressBar();
        Grid.SetRow(_addressBarRow, 1);
        root.Children.Add(_addressBarRow);

        // Row 2: Content (tree + splitter + values)
        var contentGrid = BuildContentArea();
        Grid.SetRow(contentGrid, 2);
        root.Children.Add(contentGrid);

        // Row 3: Status bar
        _statusBarRow = BuildStatusBar();
        Grid.SetRow(_statusBarRow, 3);
        root.Children.Add(_statusBarRow);

        Content = root;

        // Handle drag-drop
        root.AllowDrop = true;
        root.DragOver += (s, e) =>
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        };
        root.Drop += OnFileDrop;

        Closed += (s, e) => SaveWindowState();
    }

    // ── Menu Bar ────────────────────────────────────────────────────────

    private MenuBar BuildMenuBar()
    {
        var menuBar = new MenuBar();

        // File
        var fileMenu = new MenuBarItem { Title = Strings.MenuFile };
        fileMenu.Items.Add(MenuItem(Strings.MenuImport, OnImport, Windows.System.VirtualKey.I, Windows.System.VirtualKeyModifiers.Control));
        fileMenu.Items.Add(MenuItem(Strings.MenuExport, OnExport, Windows.System.VirtualKey.E, Windows.System.VirtualKeyModifiers.Control));
        fileMenu.Items.Add(new MenuFlyoutSeparator());
        fileMenu.Items.Add(MenuItem(Strings.MenuLoadHive, OnStub));
        fileMenu.Items.Add(MenuItem(Strings.MenuUnloadHive, OnStub));
        fileMenu.Items.Add(new MenuFlyoutSeparator());
        fileMenu.Items.Add(MenuItem(Strings.MenuConnectNetworkRegistry, OnStub));
        fileMenu.Items.Add(MenuItem(Strings.MenuDisconnectNetworkRegistry, OnStub));
        fileMenu.Items.Add(new MenuFlyoutSeparator());
        fileMenu.Items.Add(MenuItem(Strings.MenuPrint, OnStub, Windows.System.VirtualKey.P, Windows.System.VirtualKeyModifiers.Control));
        fileMenu.Items.Add(new MenuFlyoutSeparator());
        fileMenu.Items.Add(MenuItem(Strings.MenuExit, (s, e) => Close()));
        menuBar.Items.Add(fileMenu);

        // Edit
        var editMenu = new MenuBarItem { Title = Strings.MenuEdit };
        editMenu.Items.Add(MenuItem(Strings.MenuModify, OnModifyValue));
        editMenu.Items.Add(MenuItem(Strings.MenuModifyBinary, OnModifyBinary));
        editMenu.Items.Add(new MenuFlyoutSeparator());
        var newSub = new MenuFlyoutSubItem { Text = Strings.MenuNew };
        newSub.Items.Add(MenuItem(Strings.MenuNewKey, OnNewKey));
        newSub.Items.Add(new MenuFlyoutSeparator());
        newSub.Items.Add(MenuItem(Strings.MenuNewStringValue, (s, e) => OnNewValue(RegistryValueKind.String)));
        newSub.Items.Add(MenuItem(Strings.MenuNewBinaryValue, (s, e) => OnNewValue(RegistryValueKind.Binary)));
        newSub.Items.Add(MenuItem(Strings.MenuNewDWORDValue, (s, e) => OnNewValue(RegistryValueKind.DWord)));
        newSub.Items.Add(MenuItem(Strings.MenuNewQWORDValue, (s, e) => OnNewValue(RegistryValueKind.QWord)));
        newSub.Items.Add(MenuItem(Strings.MenuNewMultiStringValue, (s, e) => OnNewValue(RegistryValueKind.MultiString)));
        newSub.Items.Add(MenuItem(Strings.MenuNewExpandStringValue, (s, e) => OnNewValue(RegistryValueKind.ExpandString)));
        editMenu.Items.Add(newSub);
        editMenu.Items.Add(new MenuFlyoutSeparator());
        editMenu.Items.Add(MenuItem(Strings.MenuDelete, OnDelete, Windows.System.VirtualKey.Delete));
        editMenu.Items.Add(MenuItem(Strings.MenuRename, OnRename, Windows.System.VirtualKey.F2));
        editMenu.Items.Add(new MenuFlyoutSeparator());
        editMenu.Items.Add(MenuItem(Strings.MenuCopyKeyName, OnCopyKeyName));
        editMenu.Items.Add(new MenuFlyoutSeparator());
        editMenu.Items.Add(MenuItem(Strings.MenuFind, OnFind, Windows.System.VirtualKey.F, Windows.System.VirtualKeyModifiers.Control));
        editMenu.Items.Add(MenuItem(Strings.MenuFindNext, OnFindNext, Windows.System.VirtualKey.F3));
        editMenu.Items.Add(new MenuFlyoutSeparator());
        editMenu.Items.Add(MenuItem(Strings.MenuPermissions, OnStub));
        menuBar.Items.Add(editMenu);

        // View
        var viewMenu = new MenuBarItem { Title = Strings.MenuView };
        var statusBarToggle = new ToggleMenuFlyoutItem { Text = Strings.MenuStatusBar, IsChecked = true };
        statusBarToggle.Click += (s, e) => _statusBarRow.Visibility = statusBarToggle.IsChecked ? Visibility.Visible : Visibility.Collapsed;
        viewMenu.Items.Add(statusBarToggle);
        var addressBarToggle = new ToggleMenuFlyoutItem { Text = Strings.MenuAddressBar, IsChecked = true };
        addressBarToggle.Click += (s, e) => _addressBarRow.Visibility = addressBarToggle.IsChecked ? Visibility.Visible : Visibility.Collapsed;
        viewMenu.Items.Add(addressBarToggle);
        viewMenu.Items.Add(new MenuFlyoutSeparator());
        viewMenu.Items.Add(MenuItem(Strings.MenuRefresh, OnRefresh, Windows.System.VirtualKey.F5));
        menuBar.Items.Add(viewMenu);

        // Favorites
        _favoritesMenu = new MenuBarItem { Title = Strings.MenuFavorites };
        _favoritesMenu.Items.Add(MenuItem(Strings.MenuAddToFavorites, OnAddFavorite));
        _favoritesMenu.Items.Add(MenuItem(Strings.MenuRemoveFavorite, OnRemoveFavorite));
        _favoritesMenu.Items.Add(new MenuFlyoutSeparator());
        RefreshFavoritesMenu();
        menuBar.Items.Add(_favoritesMenu);

        // Help
        var helpMenu = new MenuBarItem { Title = Strings.MenuHelp };
        helpMenu.Items.Add(MenuItem(Strings.MenuAbout, OnAbout));
        menuBar.Items.Add(helpMenu);

        return menuBar;
    }

    private static MenuFlyoutItem MenuItem(string text, RoutedEventHandler handler,
        Windows.System.VirtualKey? key = null, Windows.System.VirtualKeyModifiers modifiers = Windows.System.VirtualKeyModifiers.None)
    {
        var item = new MenuFlyoutItem { Text = text };
        item.Click += handler;
        if (key.HasValue)
        {
            item.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = key.Value,
                Modifiers = modifiers
            });
        }
        return item;
    }

    // ── Address Bar ─────────────────────────────────────────────────────

    private Grid BuildAddressBar()
    {
        var grid = new Grid
        {
            Padding = new Thickness(4, 2, 4, 2),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }
        };

        _addressBar = new AutoSuggestBox
        {
            QueryIcon = new SymbolIcon(Symbol.Forward),
            PlaceholderText = "HKEY_CURRENT_USER\\..."
        };
        _addressBar.QuerySubmitted += (s, e) => NavigateToPath(s.Text);
        grid.Children.Add(_addressBar);

        return grid;
    }

    // ── Content Area (Tree + Splitter + Values) ─────────────────────────

    private Grid BuildContentArea()
    {
        double splitterPos = SettingsService.GetSplitterPosition();

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(splitterPos, GridUnitType.Pixel), MinWidth = 100 },
                new ColumnDefinition { Width = new GridLength(4, GridUnitType.Pixel) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 100 }
            }
        };

        // Tree view (column 0)
        _treeView = new TreeView
        {
            SelectionMode = TreeViewSelectionMode.Single,
        };
        _treeView.Expanding += OnTreeExpanding;
        _treeView.ItemInvoked += OnTreeItemInvoked;
        BuildTreeContextMenu();

        var treeScroll = new ScrollViewer
        {
            Content = _treeView,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        Grid.SetColumn(treeScroll, 0);
        grid.Children.Add(treeScroll);

        // Splitter (column 1)
        var splitter = new SplitterGrid(grid);
        Grid.SetColumn(splitter, 1);
        grid.Children.Add(splitter);

        // Value list (column 2)
        var valuePanel = BuildValuePanel();
        Grid.SetColumn(valuePanel, 2);
        grid.Children.Add(valuePanel);

        return grid;
    }

    private void BuildTreeContextMenu()
    {
        var menu = new MenuFlyout();
        menu.Items.Add(MenuItem(Strings.MenuModify, OnModifyValue));
        menu.Items.Add(new MenuFlyoutSeparator());

        var newSub = new MenuFlyoutSubItem { Text = Strings.MenuNew };
        newSub.Items.Add(MenuItem(Strings.MenuNewKey, OnNewKey));
        newSub.Items.Add(new MenuFlyoutSeparator());
        newSub.Items.Add(MenuItem(Strings.MenuNewStringValue, (s, e) => OnNewValue(RegistryValueKind.String)));
        newSub.Items.Add(MenuItem(Strings.MenuNewBinaryValue, (s, e) => OnNewValue(RegistryValueKind.Binary)));
        newSub.Items.Add(MenuItem(Strings.MenuNewDWORDValue, (s, e) => OnNewValue(RegistryValueKind.DWord)));
        newSub.Items.Add(MenuItem(Strings.MenuNewQWORDValue, (s, e) => OnNewValue(RegistryValueKind.QWord)));
        newSub.Items.Add(MenuItem(Strings.MenuNewMultiStringValue, (s, e) => OnNewValue(RegistryValueKind.MultiString)));
        newSub.Items.Add(MenuItem(Strings.MenuNewExpandStringValue, (s, e) => OnNewValue(RegistryValueKind.ExpandString)));
        menu.Items.Add(newSub);

        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(MenuItem(Strings.MenuFind, OnFind));
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(MenuItem(Strings.MenuDelete, OnDelete));
        menu.Items.Add(MenuItem(Strings.MenuRename, OnRename));
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(MenuItem(Strings.MenuExport, OnExport));
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(MenuItem(Strings.MenuPermissions, OnStub));
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(MenuItem(Strings.MenuCopyKeyName, OnCopyKeyName));

        _treeView.ContextFlyout = menu;
    }

    private Grid BuildValuePanel()
    {
        var panel = new Grid();
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // header
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // list

        // Column header
        var header = new Grid
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(12, 6, 12, 6),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(200, GridUnitType.Pixel) },
                new ColumnDefinition { Width = new GridLength(120, GridUnitType.Pixel) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }
        };

        var nameHeader = new TextBlock { Text = Strings.ColumnName, FontWeight = FontWeights.SemiBold };
        Grid.SetColumn(nameHeader, 0);
        header.Children.Add(nameHeader);

        var typeHeader = new TextBlock { Text = Strings.ColumnType, FontWeight = FontWeights.SemiBold };
        Grid.SetColumn(typeHeader, 1);
        header.Children.Add(typeHeader);

        var dataHeader = new TextBlock { Text = Strings.ColumnData, FontWeight = FontWeights.SemiBold };
        Grid.SetColumn(dataHeader, 2);
        header.Children.Add(dataHeader);

        Grid.SetRow(header, 0);
        panel.Children.Add(header);

        // Value list
        _valueList = new ListView
        {
            SelectionMode = ListViewSelectionMode.Single,
        };
        _valueList.DoubleTapped += (s, e) => OnModifyValue(s, e);
        BuildValueContextMenu();

        Grid.SetRow(_valueList, 1);
        panel.Children.Add(_valueList);

        return panel;
    }

    private void BuildValueContextMenu()
    {
        var menu = new MenuFlyout();
        menu.Items.Add(MenuItem(Strings.MenuModify, OnModifyValue));
        menu.Items.Add(MenuItem(Strings.MenuModifyBinary, OnModifyBinary));
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(MenuItem(Strings.MenuDelete, OnDeleteValue));
        menu.Items.Add(MenuItem(Strings.MenuRename, OnRenameValue));
        _valueList.ContextFlyout = menu;
    }

    private Grid BuildValueRow(RegistryValueEntry val)
    {
        var grid = new Grid
        {
            Padding = new Thickness(0, 2, 0, 2),
            Tag = val, // store reference for selection lookup
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(200, GridUnitType.Pixel) },
                new ColumnDefinition { Width = new GridLength(120, GridUnitType.Pixel) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }
        };

        var nameText = new TextBlock
        {
            Text = val.DisplayName,
            TextTrimming = TextTrimming.CharacterEllipsis,
            FontStyle = val.IsDefault ? Windows.UI.Text.FontStyle.Italic : Windows.UI.Text.FontStyle.Normal
        };
        Grid.SetColumn(nameText, 0);
        grid.Children.Add(nameText);

        var typeText = new TextBlock { Text = val.DisplayType, TextTrimming = TextTrimming.CharacterEllipsis };
        Grid.SetColumn(typeText, 1);
        grid.Children.Add(typeText);

        var dataText = new TextBlock { Text = val.DisplayData, TextTrimming = TextTrimming.CharacterEllipsis };
        Grid.SetColumn(dataText, 2);
        grid.Children.Add(dataText);

        return grid;
    }

    // ── Status Bar ──────────────────────────────────────────────────────

    private Grid BuildStatusBar()
    {
        var grid = new Grid
        {
            Padding = new Thickness(8, 4, 8, 4),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
            BorderThickness = new Thickness(0, 1, 0, 0),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        _statusText = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(_statusText, 0);
        grid.Children.Add(_statusText);

        _progressRing = new ProgressRing
        {
            IsActive = false,
            Width = 16,
            Height = 16,
            Visibility = Visibility.Collapsed
        };
        Grid.SetColumn(_progressRing, 1);
        grid.Children.Add(_progressRing);

        return grid;
    }

    // ── Tree Operations ─────────────────────────────────────────────────

    private void LoadRootKeys()
    {
        var computerNode = new TreeViewNode
        {
            Content = Strings.Computer,
            IsExpanded = true,
            HasUnrealizedChildren = false
        };

        foreach (var root in RegistryService.GetRootKeys())
        {
            var node = new TreeViewNode
            {
                Content = root,
                HasUnrealizedChildren = root.HasChildren
            };
            computerNode.Children.Add(node);
        }

        _treeView.RootNodes.Add(computerNode);
    }

    private void OnTreeExpanding(TreeView sender, TreeViewExpandingEventArgs args)
    {
        if (!args.Node.HasUnrealizedChildren) return;
        args.Node.HasUnrealizedChildren = false;

        if (args.Node.Content is RegistryKeyEntry entry)
        {
            var children = RegistryService.GetSubKeys(entry.FullPath);
            foreach (var child in children)
            {
                args.Node.Children.Add(new TreeViewNode
                {
                    Content = child,
                    HasUnrealizedChildren = child.HasChildren
                });
            }
        }
    }

    private void OnTreeItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is TreeViewNode node && node.Content is RegistryKeyEntry entry)
        {
            SelectKey(entry.FullPath);
        }
    }

    private void SelectKey(string keyPath)
    {
        _currentKeyPath = keyPath;
        _addressBar.Text = keyPath;
        _statusText.Text = keyPath;
        LoadValues(keyPath);
    }

    private List<RegistryValueEntry> _currentValues = new();

    private void LoadValues(string keyPath)
    {
        _currentValues = RegistryService.GetValues(keyPath);
        _valueList.Items.Clear();
        foreach (var val in _currentValues)
        {
            _valueList.Items.Add(BuildValueRow(val));
        }
    }

    private RegistryValueEntry? GetSelectedValue()
    {
        if (_valueList.SelectedItem is Grid g && g.Tag is RegistryValueEntry val)
            return val;
        return null;
    }

    // ── Navigation ──────────────────────────────────────────────────────

    private void NavigateToPath(string path)
    {
        path = RegistryService.NormalizePath(path);
        if (string.IsNullOrEmpty(path)) return;

        // Walk the tree to find/expand the right nodes
        if (_treeView.RootNodes.Count == 0) return;
        var computerNode = _treeView.RootNodes[0];

        string[] parts = path.Split('\\');
        TreeViewNode? current = null;

        // Find the hive node
        foreach (var child in computerNode.Children)
        {
            if (child.Content is RegistryKeyEntry entry &&
                entry.Name.Equals(parts[0], StringComparison.OrdinalIgnoreCase))
            {
                current = child;
                break;
            }
        }

        if (current == null) return;

        // Expand through the path
        for (int i = 1; i < parts.Length; i++)
        {
            // Force expand to load children
            if (current.HasUnrealizedChildren)
            {
                current.HasUnrealizedChildren = false;
                if (current.Content is RegistryKeyEntry parentEntry)
                {
                    var children = RegistryService.GetSubKeys(parentEntry.FullPath);
                    foreach (var child in children)
                    {
                        current.Children.Add(new TreeViewNode
                        {
                            Content = child,
                            HasUnrealizedChildren = child.HasChildren
                        });
                    }
                }
            }

            current.IsExpanded = true;
            TreeViewNode? next = null;
            foreach (var child in current.Children)
            {
                if (child.Content is RegistryKeyEntry childEntry &&
                    childEntry.Name.Equals(parts[i], StringComparison.OrdinalIgnoreCase))
                {
                    next = child;
                    break;
                }
            }

            if (next == null) break;
            current = next;
        }

        if (current != null)
        {
            _treeView.SelectedNode = current;
            if (current.Content is RegistryKeyEntry selectedEntry)
                SelectKey(selectedEntry.FullPath);
        }
    }

    private void NavigateToLastKey()
    {
        string lastKey = SettingsService.GetLastKey();
        if (!string.IsNullOrEmpty(lastKey))
        {
            NavigateToPath(lastKey);
        }
    }

    // ── Menu Actions ────────────────────────────────────────────────────

    private async void OnImport(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow(picker);
        picker.FileTypeFilter.Add(".reg");
        picker.FileTypeFilter.Add("*");

        var file = await picker.PickSingleFileAsync();
        if (file == null) return;

        string content = await FileIO.ReadTextAsync(file);
        var result = RegFileService.Import(content);

        string message = result.Errors == 0
            ? Strings.ImportSuccess
            : string.Format(Strings.ImportError, result.ErrorMessage ?? $"{result.Errors} error(s)");

        await ShowMessageAsync(Strings.SearchTitle, message);
        OnRefresh(sender, e);
    }

    private async void OnExport(object sender, RoutedEventArgs e)
    {
        var options = await ExportDialog.ShowAsync(Content.XamlRoot, _currentKeyPath);
        if (options == null) return;

        var picker = new FileSavePicker();
        InitializeWithWindow(picker);
        picker.SuggestedFileName = "export";
        picker.FileTypeChoices.Add("Registration Files", new List<string> { ".reg" });

        var file = await picker.PickSaveFileAsync();
        if (file == null) return;

        SetBusy(true);
        try
        {
            string content = await Task.Run(() =>
                RegFileService.Export(options.Value.BranchPath, options.Value.ExportAll));
            await FileIO.WriteTextAsync(file, content);
        }
        finally { SetBusy(false); }
    }

    private async void OnModifyValue(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentKeyPath)) return;
        var val = GetSelectedValue();
        if (val == null) return;

        bool changed = false;
        switch (val.Kind)
        {
            case RegistryValueKind.String:
            case RegistryValueKind.ExpandString:
            {
                var result = await EditStringDialog.ShowAsync(
                    Content.XamlRoot, val.Name, val.Data?.ToString(), val.Kind == RegistryValueKind.ExpandString);
                if (result != null)
                    changed = RegistryService.SetValue(_currentKeyPath, val.Name, result, val.Kind);
                break;
            }
            case RegistryValueKind.MultiString:
            {
                var result = await EditMultiStringDialog.ShowAsync(
                    Content.XamlRoot, val.Name, val.Data as string[]);
                if (result != null)
                    changed = RegistryService.SetValue(_currentKeyPath, val.Name, result, RegistryValueKind.MultiString);
                break;
            }
            case RegistryValueKind.DWord:
            {
                var result = await EditDwordDialog.ShowAsync(
                    Content.XamlRoot, val.Name, val.Data is int i ? i : 0);
                if (result != null)
                    changed = RegistryService.SetValue(_currentKeyPath, val.Name, result.Value, RegistryValueKind.DWord);
                break;
            }
            case RegistryValueKind.QWord:
            {
                var result = await EditQwordDialog.ShowAsync(
                    Content.XamlRoot, val.Name, val.Data is long l ? l : 0);
                if (result != null)
                    changed = RegistryService.SetValue(_currentKeyPath, val.Name, result.Value, RegistryValueKind.QWord);
                break;
            }
            case RegistryValueKind.Binary:
            default:
            {
                var result = await EditBinaryDialog.ShowAsync(
                    Content.XamlRoot, val.Name, val.Data is byte[] b ? b : Array.Empty<byte>());
                if (result != null)
                    changed = RegistryService.SetValue(_currentKeyPath, val.Name, result, val.Kind == RegistryValueKind.None ? RegistryValueKind.None : RegistryValueKind.Binary);
                break;
            }
        }

        if (changed) LoadValues(_currentKeyPath);
    }

    private async void OnModifyBinary(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentKeyPath)) return;
        var val = GetSelectedValue();
        if (val == null) return;

        byte[] data = val.Kind switch
        {
            RegistryValueKind.Binary => val.Data as byte[] ?? Array.Empty<byte>(),
            RegistryValueKind.DWord => BitConverter.GetBytes(val.Data is int i ? i : 0),
            RegistryValueKind.QWord => BitConverter.GetBytes(val.Data is long l ? l : 0L),
            RegistryValueKind.None => val.Data as byte[] ?? Array.Empty<byte>(),
            _ => System.Text.Encoding.Unicode.GetBytes(val.Data?.ToString() ?? string.Empty)
        };

        var result = await EditBinaryDialog.ShowAsync(Content.XamlRoot, val.Name, data);
        if (result != null)
        {
            RegistryService.SetValue(_currentKeyPath, val.Name, result, val.Kind);
            LoadValues(_currentKeyPath);
        }
    }

    private void OnNewKey(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentKeyPath)) return;

        string baseName = "New Key #";
        int num = 1;
        var existing = RegistryService.GetSubKeys(_currentKeyPath);
        while (existing.Any(k => k.Name.Equals(baseName + num, StringComparison.OrdinalIgnoreCase)))
            num++;

        string name = baseName + num;
        if (RegistryService.CreateKey(_currentKeyPath, name))
        {
            // Refresh tree node
            RefreshCurrentTreeNode();
        }
    }

    private void OnNewValue(RegistryValueKind kind)
    {
        if (string.IsNullOrEmpty(_currentKeyPath)) return;

        string baseName = "New Value #";
        int num = 1;
        var existing = RegistryService.GetValues(_currentKeyPath);
        while (existing.Any(v => v.Name.Equals(baseName + num, StringComparison.OrdinalIgnoreCase)))
            num++;

        string name = baseName + num;
        object defaultValue = kind switch
        {
            RegistryValueKind.String or RegistryValueKind.ExpandString => string.Empty,
            RegistryValueKind.DWord => 0,
            RegistryValueKind.QWord => 0L,
            RegistryValueKind.Binary => Array.Empty<byte>(),
            RegistryValueKind.MultiString => new string[] { string.Empty },
            _ => Array.Empty<byte>()
        };

        if (RegistryService.SetValue(_currentKeyPath, name, defaultValue, kind))
            LoadValues(_currentKeyPath);
    }

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentKeyPath)) return;

        // If focus is on value list, delete value
        if (GetSelectedValue() != null)
        {
            OnDeleteValue(sender, e);
            return;
        }

        // Delete key
        var result = await ShowConfirmAsync(Strings.ConfirmDelete, Strings.ConfirmDeleteKey);
        if (!result) return;

        string parent = RegistryService.GetParentPath(_currentKeyPath);
        string name = RegistryService.GetKeyName(_currentKeyPath);

        if (!string.IsNullOrEmpty(parent) && RegistryService.DeleteKey(parent, name))
        {
            NavigateToPath(parent);
        }
    }

    private async void OnDeleteValue(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentKeyPath)) return;
        var val = GetSelectedValue();
        if (val == null) return;

        var result = await ShowConfirmAsync(Strings.ConfirmDelete, Strings.ConfirmDeleteValue);
        if (!result) return;

        if (RegistryService.DeleteValue(_currentKeyPath, val.Name))
            LoadValues(_currentKeyPath);
    }

    private async void OnRename(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentKeyPath)) return;

        // If a value is selected, rename it
        var selectedVal = GetSelectedValue();
        if (selectedVal != null && !selectedVal.IsDefault)
        {
            OnRenameValue(sender, e);
            return;
        }

        // Rename key
        string oldName = RegistryService.GetKeyName(_currentKeyPath);
        string? newName = await ShowInputAsync("Rename", "New name:", oldName);
        if (newName == null || newName == oldName) return;

        string parent = RegistryService.GetParentPath(_currentKeyPath);
        if (RegistryService.RenameKey(parent, oldName, newName))
        {
            NavigateToPath(parent + "\\" + newName);
        }
        else
        {
            await ShowMessageAsync(Strings.SearchTitle, Strings.ErrorCannotRename);
        }
    }

    private async void OnRenameValue(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentKeyPath)) return;
        var val = GetSelectedValue();
        if (val == null || val.IsDefault) return;

        string? newName = await ShowInputAsync("Rename", "New name:", val.Name);
        if (newName == null || newName == val.Name) return;

        if (RegistryService.RenameValue(_currentKeyPath, val.Name, newName))
            LoadValues(_currentKeyPath);
        else
            await ShowMessageAsync(Strings.SearchTitle, Strings.ErrorCannotRename);
    }

    private void OnCopyKeyName(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentKeyPath)) return;
        var dp = new DataPackage();
        dp.SetText(_currentKeyPath);
        Clipboard.SetContent(dp);
    }

    private async void OnFind(object sender, RoutedEventArgs e)
    {
        var result = await FindDialog.ShowAsync(Content.XamlRoot, _lastSearchText, _lastSearchFlags);
        if (result == null) return;

        _lastSearchText = result.Value.Text;
        _lastSearchFlags = result.Value.Flags;
        _lastFoundValueName = null;
        await PerformSearch();
    }

    private async void OnFindNext(object sender, RoutedEventArgs e)
    {
        if (_lastSearchText == null)
        {
            OnFind(sender, e);
            return;
        }
        await PerformSearch();
    }

    private async Task PerformSearch()
    {
        if (string.IsNullOrEmpty(_lastSearchText)) return;

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        SetBusy(true, Strings.Searching);
        try
        {
            var result = await _searchService.FindNextAsync(
                string.IsNullOrEmpty(_currentKeyPath) ? "HKEY_CLASSES_ROOT" : _currentKeyPath,
                _lastFoundValueName,
                _lastSearchText,
                _lastSearchFlags,
                ct,
                progress => DispatcherQueue.TryEnqueue(() => _statusText.Text = progress));

            if (result != null)
            {
                _lastFoundValueName = result.ValueName;
                NavigateToPath(result.KeyPath);

                // Select the matched value if applicable
                if (result.ValueName != null)
                {
                    foreach (var item in _valueList.Items)
                    {
                        if (item is Grid g && g.Tag is RegistryValueEntry v && v.Name == result.ValueName)
                        {
                            _valueList.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
            else
            {
                await ShowMessageAsync(Strings.SearchTitle, Strings.SearchComplete);
                _lastFoundValueName = null;
            }
        }
        catch (OperationCanceledException) { }
        finally { SetBusy(false); }
    }

    private void OnRefresh(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentKeyPath))
        {
            RefreshCurrentTreeNode();
            LoadValues(_currentKeyPath);
        }
    }

    private void RefreshCurrentTreeNode()
    {
        var node = FindTreeNode(_currentKeyPath);
        if (node == null) return;

        node.Children.Clear();
        node.HasUnrealizedChildren = false;

        if (node.Content is RegistryKeyEntry entry)
        {
            var children = RegistryService.GetSubKeys(entry.FullPath);
            foreach (var child in children)
            {
                node.Children.Add(new TreeViewNode
                {
                    Content = child,
                    HasUnrealizedChildren = child.HasChildren
                });
            }
        }
        node.IsExpanded = true;
    }

    private TreeViewNode? FindTreeNode(string keyPath)
    {
        if (_treeView.RootNodes.Count == 0) return null;
        var computerNode = _treeView.RootNodes[0];

        string[] parts = keyPath.Split('\\');
        TreeViewNode? current = null;

        foreach (var child in computerNode.Children)
        {
            if (child.Content is RegistryKeyEntry entry &&
                entry.Name.Equals(parts[0], StringComparison.OrdinalIgnoreCase))
            {
                current = child;
                break;
            }
        }

        if (current == null) return null;

        for (int i = 1; i < parts.Length; i++)
        {
            TreeViewNode? next = null;
            foreach (var child in current.Children)
            {
                if (child.Content is RegistryKeyEntry childEntry &&
                    childEntry.Name.Equals(parts[i], StringComparison.OrdinalIgnoreCase))
                {
                    next = child;
                    break;
                }
            }
            if (next == null) return current;
            current = next;
        }
        return current;
    }

    // ── Favorites ───────────────────────────────────────────────────────

    private async void OnAddFavorite(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentKeyPath)) return;
        var name = await FavoritesDialog.ShowAddAsync(Content.XamlRoot, _currentKeyPath);
        if (name != null)
        {
            FavoritesService.AddFavorite(name, _currentKeyPath);
            RefreshFavoritesMenu();
        }
    }

    private async void OnRemoveFavorite(object sender, RoutedEventArgs e)
    {
        var name = await FavoritesDialog.ShowRemoveAsync(Content.XamlRoot);
        if (name != null)
        {
            FavoritesService.RemoveFavorite(name);
            RefreshFavoritesMenu();
        }
    }

    private void RefreshFavoritesMenu()
    {
        // Remove dynamic items (everything after the separator)
        while (_favoritesMenu.Items.Count > 3)
            _favoritesMenu.Items.RemoveAt(_favoritesMenu.Items.Count - 1);

        var favorites = FavoritesService.GetFavorites();
        foreach (var (name, path) in favorites)
        {
            var item = new MenuFlyoutItem { Text = name, Tag = path };
            item.Click += (s, e) =>
            {
                if (s is MenuFlyoutItem mi && mi.Tag is string favPath)
                    NavigateToPath(favPath);
            };
            _favoritesMenu.Items.Add(item);
        }
    }

    // ── Help ────────────────────────────────────────────────────────────

    private async void OnAbout(object sender, RoutedEventArgs e)
    {
        await ShowMessageAsync(Strings.AppTitle, Strings.AboutText.Replace("\\n", "\n"));
    }

    private async void OnStub(object sender, RoutedEventArgs e)
    {
        await ShowMessageAsync(Strings.AppTitle, Strings.NotImplemented);
    }

    // ── Drag & Drop ─────────────────────────────────────────────────────

    private async void OnFileDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
        var items = await e.DataView.GetStorageItemsAsync();
        foreach (var item in items)
        {
            if (item is StorageFile file && file.FileType.Equals(".reg", StringComparison.OrdinalIgnoreCase))
            {
                string content = await FileIO.ReadTextAsync(file);
                var result = RegFileService.Import(content);
                string message = result.Errors == 0
                    ? Strings.ImportSuccess
                    : string.Format(Strings.ImportError, result.ErrorMessage ?? $"{result.Errors} error(s)");
                await ShowMessageAsync(Strings.SearchTitle, message);
                OnRefresh(sender, new RoutedEventArgs());
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private void SetBusy(bool busy, string? status = null)
    {
        _progressRing.IsActive = busy;
        _progressRing.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        if (status != null) _statusText.Text = status;
        else if (!busy) _statusText.Text = _currentKeyPath;
    }

    private void InitializeWithWindow(object picker)
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow(picker, hwnd);
    }

    private static void InitializeWithWindow(object target, IntPtr hwnd)
    {
        WinRT.Interop.InitializeWithWindow.Initialize(target, hwnd);
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
            CloseButtonText = Strings.ButtonOK,
            XamlRoot = Content.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private async Task<string?> ShowInputAsync(string title, string label, string defaultValue)
    {
        var textBox = new TextBox { Text = defaultValue };
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = label });
        panel.Children.Add(textBox);

        var dialog = new ContentDialog
        {
            Title = title,
            Content = panel,
            PrimaryButtonText = Strings.ButtonOK,
            CloseButtonText = Strings.ButtonCancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary ? textBox.Text : null;
    }

    private void SaveSplitterPosition(double pos)
    {
        SettingsService.SetSplitterPosition(pos);
    }

    // ── Splitter Control ────────────────────────────────────────────────

    private sealed partial class SplitterGrid : Grid
    {
        private readonly Grid _parentGrid;
        private bool _dragging;
        private double _startX;
        private double _startWidth;

        public SplitterGrid(Grid parentGrid)
        {
            _parentGrid = parentGrid;
            Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray);
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);

            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;
            PointerReleased += OnPointerReleased;
            PointerCaptureLost += (s, e) => _dragging = false;
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _dragging = true;
            _startX = e.GetCurrentPoint(_parentGrid).Position.X;
            _startWidth = _parentGrid.ColumnDefinitions[0].ActualWidth;
            CapturePointer(e.Pointer);
            e.Handled = true;
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_dragging) return;
            double currentX = e.GetCurrentPoint(_parentGrid).Position.X;
            double delta = currentX - _startX;
            double newWidth = Math.Max(100, _startWidth + delta);
            _parentGrid.ColumnDefinitions[0].Width = new GridLength(newWidth, GridUnitType.Pixel);
            e.Handled = true;
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_dragging) return;
            _dragging = false;
            ReleasePointerCapture(e.Pointer);
            e.Handled = true;

            // Persist
            double pos = _parentGrid.ColumnDefinitions[0].ActualWidth;
            SettingsService.SetSplitterPosition(pos);
        }
    }
}

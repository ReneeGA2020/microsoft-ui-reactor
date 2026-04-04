// Self-test harness for Duct.TestApp
// Launched via `--self-test` flag. Starts the app, waits for render,
// walks the VisualTreeHelper-based WinUI tree, and outputs TAP results.

using Duct;
using Duct.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

static class SelfTestRunner
{
    static Window? _window;
    static int _failures;

    public static void RunAll()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new DuctApplication();
            var dispatcher = DispatcherQueue.GetForCurrentThread();

            _window = new Window { Title = "Duct Demo" };
            _window.AppWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));
            var host = new DuctHost(_window);
            host.Mount(new DemoApp());
            _window.Activate();

            // Schedule tests after initial render — async so awaits yield to dispatcher
            dispatcher.TryEnqueue(async () =>
            {
                await Task.Delay(1500);

                RunAppLaunchTests();
                RunNestedComponentTests();

                await RunCounterTests();
                await RunNavigationTests();
                await RunConditionalUITests();
                await RunPropertyGridTests();

                Console.Out.Flush();
                Environment.Exit(_failures > 0 ? 1 : 0);
            });
        });
    }

    static void RunAppLaunchTests()
    {
        Check("App_Launches_And_Window_Is_Visible",
            _window != null);

        Check("App_Shows_Tab_Bar",
            FindButton("Counter") != null &&
            FindButton("Todo List") != null &&
            FindButton("Conditional UI") != null &&
            FindButton("Form") != null);

        Check("Default_Tab_Is_Counter", () =>
        {
            var btn = FindButton("Counter");
            return btn != null && !btn.IsEnabled;
        });

        Check("Counter_Demo_Shows_Initial_State",
            FindText("Current count: 0") != null &&
            FindText("Try clicking the buttons!") != null);
    }

    static async Task RunCounterTests()
    {
        ClickButton("Counter");
        await Render();

        await CheckAsync("Increment_Button_Updates_Count", async () =>
        {
            ClickButton("+ 1");
            await Render();
            var result = FindText("Current count: 1") != null;
            ClickButton("Reset");
            await Render();
            return result;
        });

        await CheckAsync("Decrement_Button_Updates_Count", async () =>
        {
            ClickButton("+ 1");
            await Render();
            ClickButton("- 1");
            await Render();
            return FindText("Current count: 0") != null;
        });

        await CheckAsync("Reset_Button_Clears_Count", async () =>
        {
            ClickButton("+ 1");
            ClickButton("+ 1");
            ClickButton("+ 1");
            await Render();
            ClickButton("Reset");
            await Render();
            return FindText("Current count: 0") != null;
        });

        Check("Reset_Button_Disabled_When_Count_Is_Zero", () =>
        {
            var resetBtn = FindButton("Reset");
            return resetBtn != null && !resetBtn.IsEnabled;
        });

        await CheckAsync("Conditional_Text_Changes_With_Count", async () =>
        {
            var before = FindText("Try clicking the buttons!") != null;
            ClickButton("+ 1");
            await Render();
            var after = FindText("Going up...") != null;
            ClickButton("Reset");
            await Render();
            return before && after;
        });
    }

    static async Task RunNavigationTests()
    {
        await CheckAsync("Navigate_To_TodoList_Shows_Todo_Content", async () =>
        {
            ClickButton("Todo List");
            await Render();
            // CheckBox content may need an extra layout pass to materialize the TextBlock
            if (FindText("Build Duct library") != null) return true;
            await Render();
            return FindText("Build Duct library") != null;
        });

        await CheckAsync("Navigate_To_ConditionalUI_Shows_Content", async () =>
        {
            ClickButton("Conditional UI");
            await Render();
            return FindText("Conditional UI") != null;
        });

        await CheckAsync("Navigate_To_Form_Shows_Content", async () =>
        {
            ClickButton("Form");
            await Render();
            return FindText("Form") != null;
        });

        await CheckAsync("Navigate_Back_To_Counter_Preserves_Tab_State", async () =>
        {
            ClickButton("Todo List");
            await Render();
            ClickButton("Counter");
            await Render();
            return FindText("Current count: 0") != null;
        });

        await CheckAsync("Selected_Tab_Is_Disabled", async () =>
        {
            ClickButton("Todo List");
            await Render();
            var todoBtn = FindButton("Todo List");
            var counterBtn = FindButton("Counter");
            return todoBtn != null && !todoBtn.IsEnabled &&
                   counterBtn != null && counterBtn.IsEnabled;
        });

        ClickButton("Counter");
        await Render();
    }

    static async Task RunConditionalUITests()
    {
        ClickButton("Conditional UI");
        await Render();

        Check("Advanced_Options_Hidden_By_Default",
            FindText("Advanced Settings") == null);

        await CheckAsync("Toggling_Checkbox_Shows_Advanced_Options", async () =>
        {
            ToggleCheckBox("Show advanced options");
            await Render();
            var result = FindText("Advanced Settings") != null;
            ToggleCheckBox("Show advanced options");
            await Render();
            return result;
        });

        await CheckAsync("Toggling_Checkbox_Off_Hides_Advanced_Options", async () =>
        {
            ToggleCheckBox("Show advanced options");
            await Render();
            var shown = FindText("Advanced Settings") != null;
            ToggleCheckBox("Show advanced options");
            await Render();
            var hidden = FindText("Advanced Settings") == null;
            return shown && hidden;
        });

        ClickButton("Counter");
        await Render();
    }

    // ─── Nested component regression test ──────────────────────────────

    private class InnerComponent : Component
    {
        public override Element Render() => UI.Text("Hello from inner");
    }

    private class OuterComponent : Component
    {
        public override Element Render() => UI.Component<InnerComponent>();
    }

    static void RunNestedComponentTests()
    {
        Check("Nested_Components_Mount_Without_Crashing", () =>
        {
            var reconciler = new Reconciler();
            var element = new ComponentElement(typeof(OuterComponent));
            var control = reconciler.Mount(element, () => { });
            return control != null;
        });

        Check("Nested_Components_Reconcile_Without_StackOverflow", () =>
        {
            var reconciler = new Reconciler();
            var element = new ComponentElement(typeof(OuterComponent));
            var control = reconciler.Mount(element, () => { });
            // Without the fix this stack-overflows due to infinite recursion
            var result = reconciler.Reconcile(element, element, control, () => { });
            return result != null;
        });
    }

    // ─── Helpers ─────────────────────────────────────────────────────

    static void Check(string name, bool result)
    {
        if (result)
            Console.WriteLine($"ok {name}");
        else
        {
            Console.WriteLine($"not ok {name} - assertion failed");
            _failures++;
        }
    }

    static void Check(string name, Func<bool> test)
    {
        try { Check(name, test()); }
        catch (Exception ex)
        {
            Console.WriteLine($"not ok {name} - {ex.GetType().Name}: {ex.Message}");
            _failures++;
        }
    }

    static async Task CheckAsync(string name, Func<Task<bool>> test)
    {
        try { Check(name, await test()); }
        catch (Exception ex)
        {
            Console.WriteLine($"not ok {name} - {ex.GetType().Name}: {ex.Message}");
            _failures++;
        }
    }

    /// <summary>
    /// Yields to the dispatcher so DuctHost's enqueued render pass can execute,
    /// then waits a moment for layout to complete.
    /// </summary>
    static Task Render() => Task.Delay(200);

    static void ClickButton(string label)
    {
        var btn = FindButton(label);
        if (btn != null && btn.IsEnabled)
        {
            var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(btn);
            var invokeProvider = (Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)
                peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke);
            invokeProvider.Invoke();
        }
    }

    static void ToggleCheckBox(string label)
    {
        var cb = FindControl<CheckBox>(c => c.Content is string s && s == label);
        if (cb != null)
            cb.IsChecked = cb.IsChecked != true;
    }

    static Button? FindButton(string label)
        => FindControl<Button>(b => b.Content is string s && s == label);

    static TextBlock? FindText(string text)
        => FindControl<TextBlock>(tb => tb.Text == text);

    static TextBlock? FindTextContaining(string substring)
        => FindControl<TextBlock>(tb => tb.Text.Contains(substring, StringComparison.Ordinal));

    static T? FindControl<T>(Func<T, bool> predicate) where T : DependencyObject
    {
        var content = _window?.Content;
        if (content == null) return default;
        return FindInTree(content, predicate);
    }

    static T? FindInTree<T>(DependencyObject root, Func<T, bool> predicate) where T : DependencyObject
    {
        if (root is T match && predicate(match)) return match;

        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var found = FindInTree(VisualTreeHelper.GetChild(root, i), predicate);
            if (found != null) return found;
        }

        return null;
    }

    static void SelectComboBoxItem(int index)
    {
        var cb = FindControl<ComboBox>(_ => true);
        if (cb != null)
            cb.SelectedIndex = index;
    }

    static int CountTextsInTree(Func<string, bool> predicate)
    {
        int count = 0;
        void Walk(DependencyObject node)
        {
            if (node is TextBlock tb && predicate(tb.Text)) count++;
            int n = VisualTreeHelper.GetChildrenCount(node);
            for (int i = 0; i < n; i++) Walk(VisualTreeHelper.GetChild(node, i));
        }
        if (_window?.Content is not null) Walk(_window.Content);
        return count;
    }

    static int CountGridChildren()
    {
        // Find the first Grid that has RowDefinitions (the PropertyGrid's main grid)
        var grid = FindControl<Grid>(g => g.RowDefinitions.Count > 0 && g.ColumnDefinitions.Count == 2);
        return grid?.Children.Count ?? 0;
    }

    // ─── PropertyGrid E2E Tests ─────────────────────────────────────

    static async Task RunPropertyGridTests()
    {
        ClickButton("PropertyGrid");
        await Render();

        await CheckAsync("PropertyGrid_Sprite_Shows_Properties", async () =>
        {
            // Default target is "Sprite"
            await Render();
            // Should show property labels from SpriteSettings
            return FindText("Name") != null &&
                   FindText("Visible") != null;
        });

        await CheckAsync("PropertyGrid_Sprite_Shows_Category_Headers", async () =>
        {
            await Render();
            // Category headers are HyperlinkButtons with text like "▼ Appearance"
            return FindTextContaining("Appearance") != null &&
                   FindTextContaining("Transform") != null;
        });

        await CheckAsync("PropertyGrid_Switch_To_Material_Shows_Properties", async () =>
        {
            SelectComboBoxItem(1); // Material
            await Render();
            await Render(); // extra render for layout
            return FindText("MaterialName") != null &&
                   FindText("Blend") != null &&
                   FindText("Opacity") != null &&
                   FindText("CastShadow") != null;
        });

        await CheckAsync("PropertyGrid_Switch_To_Material_Grid_Has_Children", async () =>
        {
            // The grid should have real children (not be empty)
            await Render();
            return CountGridChildren() > 0;
        });

        await CheckAsync("PropertyGrid_Switch_To_Color_Shows_Properties", async () =>
        {
            SelectComboBoxItem(2); // Color
            await Render();
            await Render();
            return FindText("R") != null &&
                   FindText("G") != null &&
                   FindText("B") != null;
        });

        await CheckAsync("PropertyGrid_Switch_Back_To_Sprite_Still_Works", async () =>
        {
            SelectComboBoxItem(0); // Sprite
            await Render();
            await Render();
            return FindText("Name") != null &&
                   FindText("Visible") != null &&
                   FindTextContaining("Appearance") != null;
        });

        await CheckAsync("PropertyGrid_Round_Trip_Material_Then_Sprite", async () =>
        {
            SelectComboBoxItem(1); // Material
            await Render();
            await Render();
            var hasMaterial = FindText("MaterialName") != null;

            SelectComboBoxItem(0); // Sprite
            await Render();
            await Render();
            var hasSprite = FindText("Name") != null && FindTextContaining("Appearance") != null;

            return hasMaterial && hasSprite;
        });

        await CheckAsync("PropertyGrid_Rapid_Switching_No_Crash", async () =>
        {
            // Rapidly switch between all targets
            for (int i = 0; i < 3; i++)
            {
                SelectComboBoxItem(0); await Render();
                SelectComboBoxItem(1); await Render();
                SelectComboBoxItem(2); await Render();
            }
            // If we got here without crashing, pass
            return true;
        });

        // Return to Counter tab
        ClickButton("Counter");
        await Render();
    }
}

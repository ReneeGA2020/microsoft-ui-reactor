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
            DuctApp.ActiveHost = host;
            host.Mount(new DemoApp());
            _window.Activate();

            // Schedule tests after initial render — async so awaits yield to dispatcher
            dispatcher.TryEnqueue(async () =>
            {
                await Task.Delay(1500);

                RunAppLaunchTests();

                await RunCounterTests();
                await RunNavigationTests();
                await RunConditionalUITests();

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
}

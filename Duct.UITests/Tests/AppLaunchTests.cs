using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Duct.UITests.Tests;

/// <summary>
/// Tests that the Duct TestApp launches and the initial UI renders correctly.
/// Validates the full Duct pipeline: Element DSL → Reconciler → WinUI control tree.
/// </summary>
[TestClass]
public class AppLaunchTests : TestBase
{
    [TestMethod]
    public void App_Launches_And_Window_Is_Visible()
    {
        Assert.IsNotNull(Session, "Session should be initialized");
        Assert.IsNotNull(Session.CurrentWindowHandle, "Window should have a handle");
    }

    [TestMethod]
    public void App_Shows_Tab_Bar()
    {
        // The demo app renders tab buttons: Counter, Todo List, Conditional UI, etc.
        AssertElementExists("Counter", "Counter tab should be visible");
        AssertElementExists("Todo List", "Todo List tab should be visible");
        AssertElementExists("Conditional UI", "Conditional UI tab should be visible");
        AssertElementExists("Form", "Form tab should be visible");
    }

    [TestMethod]
    public void Default_Tab_Is_Counter()
    {
        // Counter tab is selected by default (disabled = selected in this UI)
        var counterTab = WaitForElement("Counter");
        Assert.IsNotNull(counterTab);
        // The Counter tab should be disabled (indicating it's the current tab)
        Assert.IsFalse(counterTab!.Enabled, "Counter tab should be disabled (selected)");
    }

    [TestMethod]
    public void Counter_Demo_Shows_Initial_State()
    {
        // Counter demo should show "Current count: 0" initially
        AssertElementExists("Current count: 0", "Counter should start at 0");
        AssertElementExists("Try clicking the buttons!", "Help text should be shown");
    }
}

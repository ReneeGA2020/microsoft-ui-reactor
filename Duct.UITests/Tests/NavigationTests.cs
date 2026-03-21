using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Duct.UITests.Tests;

/// <summary>
/// Tests tab navigation in the Duct TestApp.
/// Verifies that the Reconciler correctly unmounts one component tree
/// and mounts another when switching tabs.
/// </summary>
[TestClass]
public class NavigationTests : TestBase
{
    [TestMethod]
    public void Navigate_To_TodoList_Shows_Todo_Content()
    {
        NavigateToTab("Todo List");
        Thread.Sleep(500);

        AssertElementExists("Todo List", "Todo List heading should be visible");
        // The todo demo starts with preset items
        AssertElementExists("Build Duct library", "First preset todo should be visible");
    }

    [TestMethod]
    public void Navigate_To_ConditionalUI_Shows_Content()
    {
        NavigateToTab("Conditional UI");
        Thread.Sleep(500);

        AssertElementExists("Conditional UI", "Conditional UI heading should exist");
        AssertElementExists("Show advanced options", "Checkbox label should be visible");
    }

    [TestMethod]
    public void Navigate_To_Form_Shows_Content()
    {
        NavigateToTab("Form");
        Thread.Sleep(500);

        AssertElementExists("Form", "Form heading should exist");
    }

    [TestMethod]
    public void Navigate_Back_To_Counter_Preserves_Tab_State()
    {
        // Navigate away and back — Counter component remounts with fresh state
        NavigateToTab("Todo List");
        Thread.Sleep(300);

        NavigateToTab("Counter");
        Thread.Sleep(300);

        AssertElementExists("Current count: 0", "Counter should reset to 0 when remounted");
    }

    [TestMethod]
    public void Selected_Tab_Is_Disabled()
    {
        NavigateToTab("Todo List");
        Thread.Sleep(300);

        var todoTab = WaitForElement("Todo List");
        Assert.IsNotNull(todoTab);
        Assert.IsFalse(todoTab!.Enabled, "Selected tab should be disabled");

        // Counter tab should now be enabled (clickable)
        var counterTab = WaitForElement("Counter");
        Assert.IsNotNull(counterTab);
        Assert.IsTrue(counterTab!.Enabled, "Non-selected tab should be enabled");
    }
}

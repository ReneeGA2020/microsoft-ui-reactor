using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Duct.UITests.Tests;

/// <summary>
/// Tests the Counter demo in the Duct TestApp.
/// Verifies that state management (UseState) and re-rendering work correctly
/// when the user interacts with buttons and sliders.
/// </summary>
[TestClass]
public class CounterTests : TestBase
{
    [TestInitialize]
    public void NavigateToCounter()
    {
        // Ensure we're on the Counter tab
        NavigateToTab("Counter");
        Thread.Sleep(300);
    }

    [TestMethod]
    public void Increment_Button_Updates_Count()
    {
        // Click the "+ 1" button
        var plusBtn = WaitForElement("+ 1");
        Assert.IsNotNull(plusBtn, "Plus button should exist");
        plusBtn!.Click();
        Thread.Sleep(300);

        // Count should now be 1
        AssertElementExists("Current count: 1", "Count should be 1 after clicking +");
    }

    [TestMethod]
    public void Decrement_Button_Updates_Count()
    {
        // Click "+ 1" first to get to 1, then "- 1" to go back to 0
        var plusBtn = WaitForElement("+ 1");
        Assert.IsNotNull(plusBtn);
        plusBtn!.Click();
        Thread.Sleep(200);

        var minusBtn = WaitForElement("- 1");
        Assert.IsNotNull(minusBtn, "Minus button should exist");
        minusBtn!.Click();
        Thread.Sleep(300);

        AssertElementExists("Current count: 0", "Count should be back to 0");
    }

    [TestMethod]
    public void Reset_Button_Clears_Count()
    {
        // Click + a few times, then reset
        var plusBtn = WaitForElement("+ 1");
        Assert.IsNotNull(plusBtn);
        plusBtn!.Click();
        plusBtn.Click();
        plusBtn.Click();
        Thread.Sleep(200);

        var resetBtn = WaitForElement("Reset");
        Assert.IsNotNull(resetBtn, "Reset button should exist");
        resetBtn!.Click();
        Thread.Sleep(300);

        AssertElementExists("Current count: 0", "Count should be reset to 0");
    }

    [TestMethod]
    public void Reset_Button_Disabled_When_Count_Is_Zero()
    {
        // At count 0, Reset should be disabled
        var resetBtn = WaitForElement("Reset");
        Assert.IsNotNull(resetBtn);
        Assert.IsFalse(resetBtn!.Enabled, "Reset should be disabled when count is 0");
    }

    [TestMethod]
    public void Conditional_Text_Changes_With_Count()
    {
        // At 0, shows "Try clicking the buttons!"
        AssertElementExists("Try clicking the buttons!");

        // Click + once — should show "Going up..."
        var plusBtn = WaitForElement("+ 1");
        Assert.IsNotNull(plusBtn);
        plusBtn!.Click();
        Thread.Sleep(300);

        AssertElementExists("Going up...", "Should show 'Going up...' for small positive counts");
    }
}

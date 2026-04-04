using Microsoft.VisualStudio.TestTools.UnitTesting;
using Duct.AppTests.Infrastructure;

namespace Duct.AppTests.Tests;

/// <summary>
/// Interactive tests that use Appium/WinAppDriver to simulate real user input.
/// These are the ~4 scenarios where cross-process input injection is the point
/// of the test: button clicks, checkbox toggles, navigation.
///
/// All other tests run in-process via SelfTestBatch at CPU speed.
/// </summary>
[TestClass]
public class InteractiveTests : AppTestBase
{
    [ClassInitialize]
    public static void StartAppSession(TestContext context)
    {
        TestSession.AssemblyInit(context);
    }

    [ClassCleanup]
    public static void StopAppSession()
    {
        TestSession.AssemblyCleanup();
    }

    /// <summary>
    /// Counter: click increment/decrement/reset buttons via WinAppDriver,
    /// verify the count display updates through the full UI pipeline.
    /// </summary>
    [TestMethod]
    public void Interactive_Counter()
    {
        NavigateToFixture("Demo_Counter");

        // Initial state
        var display = WaitForElement("CountDisplay");
        Assert.IsTrue(display.Text.Contains("0"), $"Expected initial count 0, got: {display.Text}");

        // Increment
        ClickButton("IncrementBtn");
        Thread.Sleep(300);
        display = FindById("CountDisplay");
        Assert.IsTrue(display.Text.Contains("1"), $"Expected count 1 after increment, got: {display.Text}");

        // Increment again
        ClickButton("IncrementBtn");
        Thread.Sleep(300);
        display = FindById("CountDisplay");
        Assert.IsTrue(display.Text.Contains("2"), $"Expected count 2, got: {display.Text}");

        // Decrement
        ClickButton("DecrementBtn");
        Thread.Sleep(300);
        display = FindById("CountDisplay");
        Assert.IsTrue(display.Text.Contains("1"), $"Expected count 1 after decrement, got: {display.Text}");

        // Reset
        ClickButton("ResetBtn");
        Thread.Sleep(300);
        display = FindById("CountDisplay");
        Assert.IsTrue(display.Text.Contains("0"), $"Expected count 0 after reset, got: {display.Text}");
    }

    /// <summary>
    /// Observable: click mutation button via WinAppDriver, verify INPC propagates to UI.
    /// </summary>
    [TestMethod]
    public void Interactive_ObservableMutation()
    {
        NavigateToFixture("Observable_UseObservable_Rerender");

        // Initial state
        var nameDisplay = WaitForElement("NameDisplay");
        Assert.IsTrue(nameDisplay.Text.Contains("Alice"), $"Expected initial name Alice, got: {nameDisplay.Text}");

        // Click mutation button
        ClickButton("ChangeNameBtn");
        Thread.Sleep(300);
        nameDisplay = FindById("NameDisplay");
        Assert.IsTrue(nameDisplay.Text.Contains("Bob"), $"Expected name Bob after mutation, got: {nameDisplay.Text}");
    }
}

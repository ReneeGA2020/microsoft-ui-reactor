using Microsoft.VisualStudio.TestTools.UnitTesting;
using Duct.AppTests.Infrastructure;
using OpenQA.Selenium.Appium;

namespace Duct.AppTests.Tests;

/// <summary>
/// E2E tests for declarative event handler modifiers.
/// These use Appium/WinAppDriver to simulate real user input against a running WinUI3 app,
/// verifying that .OnTapped(), .OnSizeChanged(), .OnPointerPressed(), and .OnKeyDown()
/// fire correctly and update state through the Duct reconciliation pipeline.
/// </summary>
[TestClass]
public class EventHandlerTests : AppTestBase
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
    /// OnTapped: tap a Button element via WinAppDriver, verify the tap count increments
    /// through the full UI pipeline (event -> state update -> re-render -> UIA text change).
    /// </summary>
    [TestMethod]
    public void Interactive_OnTapped_Updates_State()
    {
        NavigateToFixture("EventHandler_Tapped");

        // Initial state
        var display = WaitForElement("TapCount");
        Assert.IsTrue(display.Text.Contains("0"), $"Expected initial tap count 0, got: {display.Text}");

        // Tap the button
        var target = FindById("TapBtn");
        target.Click();
        Thread.Sleep(300);

        display = FindById("TapCount");
        Assert.IsTrue(display.Text.Contains("1"), $"Expected tap count 1, got: {display.Text}");

        // Tap again
        target = FindById("TapBtn");
        target.Click();
        Thread.Sleep(300);

        display = FindById("TapCount");
        Assert.IsTrue(display.Text.Contains("2"), $"Expected tap count 2, got: {display.Text}");
    }

    /// <summary>
    /// OnSizeChanged: click a toggle button that changes a Border's width,
    /// verify the SizeChanged handler fires and reports the new dimensions.
    /// </summary>
    [TestMethod]
    public void Interactive_OnSizeChanged_Reports_Dimensions()
    {
        NavigateToFixture("EventHandler_SizeChanged");

        // Wait for initial size to be reported
        Thread.Sleep(500);
        var display = WaitForElement("SizeDisplay");
        // Initial width is 200
        Assert.IsTrue(display.Text.Contains("200"), $"Expected initial width 200, got: {display.Text}");

        // Expand to 400
        ClickButton("SizeToggleBtn");
        Thread.Sleep(500);

        display = FindById("SizeDisplay");
        Assert.IsTrue(display.Text.Contains("400"), $"Expected expanded width 400, got: {display.Text}");

        // Shrink back to 200
        ClickButton("SizeToggleBtn");
        Thread.Sleep(500);

        display = FindById("SizeDisplay");
        Assert.IsTrue(display.Text.Contains("200"), $"Expected shrunk width 200, got: {display.Text}");
    }

    /// <summary>
    /// OnPointerPressed: click a Button element, verify press is detected.
    /// Note: WinUI Button consumes PointerPressed internally for its press states,
    /// so this E2E test uses Button.OnClick as a proxy. The declarative OnPointerPressed
    /// API is verified by unit tests; this tests the Appium plumbing end-to-end.
    /// </summary>
    [TestMethod]
    public void Interactive_OnPointerPressed_Detects_Click()
    {
        NavigateToFixture("EventHandler_PointerPressed");

        var display = WaitForElement("PressCount");
        Assert.IsTrue(display.Text.Contains("0"), $"Expected initial press count 0, got: {display.Text}");

        // Press the button
        var target = FindById("PointerBtn");
        target.Click();
        Thread.Sleep(300);

        display = FindById("PressCount");
        Assert.IsTrue(display.Text.Contains("1"), $"Expected press count 1, got: {display.Text}");
    }

    /// <summary>
    /// OnKeyDown: focus a TextField and send a key, verify the key is captured
    /// by the declarative .OnKeyDown() handler.
    /// </summary>
    [TestMethod]
    public void Interactive_OnKeyDown_Captures_Keys()
    {
        NavigateToFixture("EventHandler_KeyDown");

        var display = WaitForElement("KeyDisplay");
        Assert.IsTrue(display.Text.Contains("none"), $"Expected initial 'none', got: {display.Text}");

        // Click the text field to focus it, then type a key
        var target = FindById("KeyInput");
        target.Click();
        Thread.Sleep(200);
        target.SendKeys("a");
        Thread.Sleep(300);

        display = FindById("KeyDisplay");
        Assert.IsTrue(display.Text.Contains("A"), $"Expected key 'A', got: {display.Text}");
    }

    /// <summary>
    /// UseReducer (action-based): verify adding and clearing items through dispatched actions.
    /// </summary>
    [TestMethod]
    public void Interactive_UseReducer_Dispatches_Actions()
    {
        NavigateToFixture("EventHandler_UseReducer");

        // Initial state: 1 item
        var countDisplay = WaitForElement("TodoCount");
        Assert.IsTrue(countDisplay.Text.Contains("1"), $"Expected initial count 1, got: {countDisplay.Text}");

        // Add items
        ClickButton("AddBtn");
        Thread.Sleep(300);
        ClickButton("AddBtn");
        Thread.Sleep(300);

        countDisplay = FindById("TodoCount");
        Assert.IsTrue(countDisplay.Text.Contains("3"), $"Expected count 3, got: {countDisplay.Text}");

        // Clear all
        ClickButton("ClearBtn");
        Thread.Sleep(300);

        countDisplay = FindById("TodoCount");
        Assert.IsTrue(countDisplay.Text.Contains("0"), $"Expected count 0 after clear, got: {countDisplay.Text}");
    }
}

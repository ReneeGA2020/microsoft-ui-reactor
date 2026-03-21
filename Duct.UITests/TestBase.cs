using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;

namespace Duct.UITests;

/// <summary>
/// Base class for Duct UI tests. Provides helper methods for finding elements,
/// waiting for conditions, and interacting with the test app.
/// </summary>
public class TestBase
{
    public static WindowsDriver<WindowsElement> Session => SessionManager.Session;

    /// <summary>
    /// Clicks a tab button by its text label.
    /// </summary>
    protected static void NavigateToTab(string tabName)
    {
        var tab = WaitForElement(tabName);
        Assert.IsNotNull(tab, $"Tab '{tabName}' not found");
        tab!.Click();
        Thread.Sleep(500); // Allow render
    }

    /// <summary>
    /// Finds an element by name with retry/timeout.
    /// </summary>
    protected static WindowsElement? WaitForElement(string name, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var el = Session.FindElementByName(name);
                if (el != null) return el;
            }
            catch { }
            Thread.Sleep(100);
        }
        return null;
    }

    /// <summary>
    /// Finds an element by AutomationId with retry/timeout.
    /// </summary>
    protected static WindowsElement? WaitForElementById(string automationId, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var el = Session.FindElementByAccessibilityId(automationId);
                if (el != null) return el;
            }
            catch { }
            Thread.Sleep(100);
        }
        return null;
    }

    /// <summary>
    /// Types text via keyboard.
    /// </summary>
    protected static void TypeText(string text)
    {
        new Actions(Session).SendKeys(text).Perform();
    }

    /// <summary>
    /// Verifies an element with the given text exists on screen.
    /// </summary>
    protected static void AssertElementExists(string name, string message = "")
    {
        var el = WaitForElement(name);
        Assert.IsNotNull(el, string.IsNullOrEmpty(message)
            ? $"Expected element '{name}' to exist"
            : message);
    }

    /// <summary>
    /// Verifies an element with the given text is displayed on screen.
    /// </summary>
    protected static void AssertElementDisplayed(string name)
    {
        var el = WaitForElement(name);
        Assert.IsNotNull(el, $"Element '{name}' not found");
        Assert.IsTrue(el!.Displayed, $"Element '{name}' found but not displayed");
    }
}

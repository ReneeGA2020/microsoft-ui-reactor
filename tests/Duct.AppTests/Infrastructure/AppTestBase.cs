using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Support.UI;

namespace Duct.AppTests.Infrastructure;

/// <summary>
/// Base class for all Appium-based UI test classes.
/// Provides helpers for navigation, element lookup, waiting, and DPI-aware assertions.
/// </summary>
public class AppTestBase
{
    /// <summary>
    /// The active WindowsDriver session.
    /// </summary>
    protected static WindowsDriver<WindowsElement> Session => TestSession.Session;

    // No TestInitialize/TestCleanup — NavigateToFixture handles fixture switching.
    // Resetting between every test wastes ~5s on the implicit wait timeout.

    /// <summary>
    /// Navigates to a named test fixture by clicking its nav element and waiting
    /// for the fixture status to indicate it has loaded.
    /// </summary>
    protected void NavigateToFixture(string name)
    {
        var navElement = Session.FindElement(MobileBy.AccessibilityId($"Nav_{name}"));
        navElement.Click();
        WaitForText("FixtureStatus", $"Loaded: {name}");
    }

    /// <summary>
    /// Resets the current fixture to its default state.
    /// </summary>
    protected void ResetFixture()
    {
        try
        {
            var reset = Session.FindElement(MobileBy.AccessibilityId("ResetFixture"));
            reset.Click();
            WaitForText("FixtureStatus", "Ready", timeoutMs: 3000);
        }
        catch (WebDriverException)
        {
            // Reset button may not be present yet (e.g., before first navigation).
        }
    }

    /// <summary>
    /// Finds an element by its AutomationId (UIA accessibility identifier).
    /// </summary>
    protected WindowsElement FindById(string automationId)
    {
        return Session.FindElement(MobileBy.AccessibilityId(automationId));
    }

    /// <summary>
    /// Finds an element by its Name property.
    /// </summary>
    protected WindowsElement FindByName(string name)
    {
        return Session.FindElement(MobileBy.Name(name));
    }

    /// <summary>
    /// Waits for an element with the given AutomationId to appear.
    /// </summary>
    protected WindowsElement WaitForElement(string automationId, int timeoutMs = 5000)
    {
        var wait = new DefaultWait<WindowsDriver<WindowsElement>>(Session)
        {
            Timeout = TimeSpan.FromMilliseconds(timeoutMs),
            PollingInterval = TimeSpan.FromMilliseconds(200),
        };
        wait.IgnoreExceptionTypes(typeof(WebDriverException));

        return wait.Until(driver => driver.FindElement(MobileBy.AccessibilityId(automationId)));
    }

    /// <summary>
    /// Waits until the element with the given AutomationId displays the expected text.
    /// </summary>
    protected void WaitForText(string automationId, string expectedText, int timeoutMs = 5000)
    {
        var wait = new DefaultWait<WindowsDriver<WindowsElement>>(Session)
        {
            Timeout = TimeSpan.FromMilliseconds(timeoutMs),
            PollingInterval = TimeSpan.FromMilliseconds(200),
        };
        wait.IgnoreExceptionTypes(typeof(WebDriverException));

        wait.Until(driver =>
        {
            var element = driver.FindElement(MobileBy.AccessibilityId(automationId));
            return element.Text == expectedText ? element : null;
        });
    }

    /// <summary>
    /// Reads the DPI scale factor from the TestHostRoot element.
    /// The Host app sets its Name property to "DpiScale:X.XXXX".
    /// </summary>
    protected double GetDpiScale()
    {
        var root = Session.FindElement(MobileBy.AccessibilityId("TestHostRoot"));
        var name = root.GetAttribute("Name");

        // Expected format: "DpiScale:1.5000"
        if (name != null && name.StartsWith("DpiScale:") &&
            double.TryParse(name["DpiScale:".Length..],
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var scale))
        {
            return scale;
        }

        // Default to 1.0 if not available.
        return 1.0;
    }

    /// <summary>
    /// Asserts that <paramref name="actual"/> is within <paramref name="tolerance"/>
    /// of <paramref name="expected"/>.
    /// </summary>
    protected static void AssertNear(double actual, double expected, double tolerance)
    {
        var diff = Math.Abs(actual - expected);
        Assert.IsTrue(
            diff <= tolerance,
            $"Expected {expected} ± {tolerance}, but got {actual} (off by {diff}).");
    }

    /// <summary>
    /// Returns the UIA BoundingRectangle of the element as a <see cref="Rectangle"/>.
    /// </summary>
    protected Rectangle GetElementRect(string automationId)
    {
        var element = FindById(automationId);
        return element.Rect;
    }

    /// <summary>
    /// Returns the logical (DPI-independent) size of an element as (width, height).
    /// </summary>
    protected (double Width, double Height) GetLogicalSize(string automationId)
    {
        var rect = GetElementRect(automationId);
        var dpi = GetDpiScale();
        return (rect.Width / dpi, rect.Height / dpi);
    }

    /// <summary>
    /// Clicks a button by Name first, falling back to AccessibilityId.
    /// </summary>
    protected void ClickButton(string nameOrId)
    {
        try
        {
            var element = Session.FindElement(MobileBy.Name(nameOrId));
            element.Click();
        }
        catch (WebDriverException)
        {
            var element = Session.FindElement(MobileBy.AccessibilityId(nameOrId));
            element.Click();
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Duct.UITests.Tests;

/// <summary>
/// Tests the Conditional UI demo in the Duct TestApp.
/// Verifies that conditional rendering (C# if/switch/ternary in Render())
/// correctly mounts and unmounts sub-trees via the Reconciler.
/// </summary>
[TestClass]
public class ConditionalUITests : TestBase
{
    [TestInitialize]
    public void NavigateToConditional()
    {
        NavigateToTab("Conditional UI");
        Thread.Sleep(500);
    }

    [TestMethod]
    public void Advanced_Options_Hidden_By_Default()
    {
        // "Advanced Settings" panel should not be visible initially
        var advSettings = WaitForElement("Advanced Settings", timeoutMs: 1000);
        Assert.IsNull(advSettings, "Advanced Settings should be hidden initially");
    }

    [TestMethod]
    public void Toggling_Checkbox_Shows_Advanced_Options()
    {
        // Click "Show advanced options" checkbox
        var checkbox = WaitForElement("Show advanced options");
        Assert.IsNotNull(checkbox, "Checkbox should exist");
        checkbox!.Click();
        Thread.Sleep(500);

        // Now "Advanced Settings" sub-tree should be mounted
        AssertElementExists("Advanced Settings", "Advanced Settings should appear after toggling checkbox");
        AssertElementExists("Enable Feature A", "Feature A checkbox should be visible");
        AssertElementExists("Enable Feature B", "Feature B checkbox should be visible");
    }

    [TestMethod]
    public void Toggling_Checkbox_Off_Hides_Advanced_Options()
    {
        // Show advanced options
        var checkbox = WaitForElement("Show advanced options");
        Assert.IsNotNull(checkbox);
        checkbox!.Click();
        Thread.Sleep(300);

        // Verify they appeared
        AssertElementExists("Advanced Settings");

        // Toggle off
        checkbox.Click();
        Thread.Sleep(300);

        // Should be gone
        var advSettings = WaitForElement("Advanced Settings", timeoutMs: 1000);
        Assert.IsNull(advSettings, "Advanced Settings should be hidden after toggling off");
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Duct.UITests.Tests;

[TestClass]
public class AppLaunchTests : TestBase
{
    [TestMethod] public void App_Launches_And_Window_Is_Visible() => AssertSelfTest("App_Launches_And_Window_Is_Visible");
    [TestMethod] public void App_Shows_Tab_Bar() => AssertSelfTest("App_Shows_Tab_Bar");
    [TestMethod] public void Default_Tab_Is_Counter() => AssertSelfTest("Default_Tab_Is_Counter");
    [TestMethod] public void Counter_Demo_Shows_Initial_State() => AssertSelfTest("Counter_Demo_Shows_Initial_State");
}

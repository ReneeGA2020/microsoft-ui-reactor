using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Duct.UITests.Tests;

[TestClass]
public class NavigationTests : TestBase
{
    [TestMethod] public void Navigate_To_TodoList_Shows_Todo_Content() => AssertSelfTest("Navigate_To_TodoList_Shows_Todo_Content");
    [TestMethod] public void Navigate_To_ConditionalUI_Shows_Content() => AssertSelfTest("Navigate_To_ConditionalUI_Shows_Content");
    [TestMethod] public void Navigate_To_Form_Shows_Content() => AssertSelfTest("Navigate_To_Form_Shows_Content");
    [TestMethod] public void Navigate_Back_To_Counter_Preserves_Tab_State() => AssertSelfTest("Navigate_Back_To_Counter_Preserves_Tab_State");
    [TestMethod] public void Selected_Tab_Is_Disabled() => AssertSelfTest("Selected_Tab_Is_Disabled");
}

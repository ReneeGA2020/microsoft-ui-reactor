using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Duct.UITests.Tests;

[TestClass]
public class CounterTests : TestBase
{
    [TestMethod] public void Increment_Button_Updates_Count() => AssertSelfTest("Increment_Button_Updates_Count");
    [TestMethod] public void Decrement_Button_Updates_Count() => AssertSelfTest("Decrement_Button_Updates_Count");
    [TestMethod] public void Reset_Button_Clears_Count() => AssertSelfTest("Reset_Button_Clears_Count");
    [TestMethod] public void Reset_Button_Disabled_When_Count_Is_Zero() => AssertSelfTest("Reset_Button_Disabled_When_Count_Is_Zero");
    [TestMethod] public void Conditional_Text_Changes_With_Count() => AssertSelfTest("Conditional_Text_Changes_With_Count");
}

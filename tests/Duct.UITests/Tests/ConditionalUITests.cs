using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Duct.UITests.Tests;

[TestClass]
public class ConditionalUITests : TestBase
{
    [TestMethod] public void Advanced_Options_Hidden_By_Default() => AssertSelfTest("Advanced_Options_Hidden_By_Default");
    [TestMethod] public void Toggling_Checkbox_Shows_Advanced_Options() => AssertSelfTest("Toggling_Checkbox_Shows_Advanced_Options");
    [TestMethod] public void Toggling_Checkbox_Off_Hides_Advanced_Options() => AssertSelfTest("Toggling_Checkbox_Off_Hides_Advanced_Options");
}

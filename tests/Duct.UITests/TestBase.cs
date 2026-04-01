using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Duct.UITests;

/// <summary>
/// Base class for Duct UI tests that delegate to the TestApp self-test harness.
/// Each [TestMethod] maps to a named self-test in the TestApp.
/// </summary>
public class TestBase
{
    protected static void AssertSelfTest(string name) => SessionManager.AssertTestPassed(name);
}

// Duct AppTests Host — unified test host app for Appium/WinAppDriver UI tests.
// Normal mode: Shows fixture navigator UI for manual testing and Appium interactive tests.
// Self-test mode (--self-test): Runs all non-interactive fixtures in-process at CPU speed
//   using VisualTreeHelper, outputs TAP results to stdout, exits.

using Duct;
using Duct.AppTests.Host;
using Duct.AppTests.Host.SelfTest;

if (args.Contains("--self-test"))
    SelfTestRunner.RunAll();
else
    DuctApp.Run<TestHost>("Duct Test Host", width: 1200, height: 800);

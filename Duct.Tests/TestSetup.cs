using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Duct.Tests;

/// <summary>
/// Initializes WinRT COM support and the Windows App SDK runtime before any tests run.
/// This enables tests to create WinUI types (SolidColorBrush, FontWeights, etc.)
/// without a full Application host.
/// </summary>
internal static class TestSetup
{
    [DllImport("Microsoft.WindowsAppRuntime.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int WindowsAppRuntime_EnsureIsLoaded();

    [ModuleInitializer]
    internal static void Initialize()
    {
        // Set base directory so the runtime DLL can be found
        Environment.SetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY", AppContext.BaseDirectory);

        // Load the Windows App SDK runtime (registers WinUI activation factories)
        WindowsAppRuntime_EnsureIsLoaded();

        // Initialize COM wrappers for WinRT interop
        WinRT.ComWrappersSupport.InitializeComWrappers();
    }
}

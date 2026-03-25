using System.Runtime.InteropServices;

namespace StressPerf.Shared;

public static class ConsoleHelper
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachConsole(int dwProcessId);

    private const int ATTACH_PARENT_PROCESS = -1;

    /// <summary>
    /// Attach to the parent process console so Console.WriteLine works from a WinExe.
    /// </summary>
    public static void EnsureConsole()
    {
        AttachConsole(ATTACH_PARENT_PROCESS);
    }
}

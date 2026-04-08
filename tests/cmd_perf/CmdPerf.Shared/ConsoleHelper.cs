using System.Runtime.InteropServices;

namespace CmdPerf.Shared;

public static partial class ConsoleHelper
{
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsole(int dwProcessId);

    private const int ATTACH_PARENT_PROCESS = -1;

    public static void EnsureConsole()
    {
        AttachConsole(ATTACH_PARENT_PROCESS);
    }
}

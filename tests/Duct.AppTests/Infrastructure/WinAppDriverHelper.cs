using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Duct.AppTests.Infrastructure;

/// <summary>
/// Static helper to start and stop WinAppDriver.exe for UI automation tests.
/// </summary>
public static class WinAppDriverHelper
{
    private const string WinAppDriverPath =
        @"C:\Program Files (x86)\Windows Application Driver\WinAppDriver.exe";

    private const int WinAppDriverPort = 4723;

    private static Process? _wadProcess;

    public static void Start()
    {
        if (IsAlreadyRunning())
        {
            Console.WriteLine("WinAppDriver is already running.");
            return;
        }

        if (!File.Exists(WinAppDriverPath))
        {
            throw new FileNotFoundException(
                "WinAppDriver.exe not found. Install it from " +
                "https://github.com/microsoft/WinAppDriver/releases",
                WinAppDriverPath);
        }

        // Start WinAppDriver with its own console window. It needs a console
        // to stay alive — using CreateNoWindow or redirecting streams causes
        // it to exit immediately.
        var psi = new ProcessStartInfo(WinAppDriverPath)
        {
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Minimized,
        };

        _wadProcess = Process.Start(psi);

        if (_wadProcess == null || _wadProcess.HasExited)
        {
            throw new InvalidOperationException("Failed to start WinAppDriver.");
        }

        // Wait for WinAppDriver to start listening
        for (int i = 0; i < 20; i++)
        {
            Thread.Sleep(250);
            if (IsAlreadyRunning()) break;
        }

        Console.WriteLine($"WinAppDriver started (PID {_wadProcess.Id}).");
    }

    public static void Stop()
    {
        if (_wadProcess == null) return;

        try
        {
            if (!_wadProcess.HasExited)
            {
                _wadProcess.Kill();
                _wadProcess.WaitForExit(5000);
                Console.WriteLine("WinAppDriver stopped.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: failed to stop WinAppDriver: {ex.Message}");
        }
        finally
        {
            _wadProcess.Dispose();
            _wadProcess = null;
        }
    }

    private static bool IsAlreadyRunning()
    {
        try
        {
            using var client = new TcpClient();
            client.Connect(IPAddress.Loopback, WinAppDriverPort);
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}

using System.Diagnostics;

namespace Microsoft.UI.Reactor.Cli.Docs;

/// <summary>
/// Captures screenshots from a running Reactor doc app via the PreviewCaptureServer HTTP API.
/// Launches the app with <c>--preview --vscode</c> to enable the capture endpoint,
/// waits for the startup delay, then captures frames via <c>GET /frame</c>.
/// </summary>
internal static class ScreenshotCapture
{
    public static async Task<bool> CaptureAsync(
        string appDir,
        string topicId,
        DocManifest manifest,
        string outputImagesDir)
    {
        var csprojFiles = Directory.GetFiles(appDir, "*.csproj");
        if (csprojFiles.Length == 0)
        {
            Console.Error.WriteLine($"    ✗ No .csproj found in {appDir}");
            return false;
        }

        var csproj = csprojFiles[0];
        Console.WriteLine($"    Launching {Path.GetFileName(csproj)} for capture...");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{csproj}\" -- --preview --vscode --fps 5",
            RedirectStandardOutput = true,
            RedirectStandardError = false,
            UseShellExecute = false,
            CreateNoWindow = false,
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            Console.Error.WriteLine("    ✗ Failed to start process");
            return false;
        }

        try
        {
            var port = await WaitForCapturePort(process, TimeSpan.FromSeconds(30));
            if (port < 0)
            {
                Console.Error.WriteLine("    ✗ Timed out waiting for capture port");
                return false;
            }

            Console.WriteLine($"    Capture server on port {port}");

            var delay = manifest.App.StartupDelay;
            Console.WriteLine($"    Waiting {delay}ms for app startup...");
            await Task.Delay(delay);

            var topicDir = Path.Combine(outputImagesDir, topicId);
            Directory.CreateDirectory(topicDir);

            using var http = new HttpClient();
            foreach (var screenshot in manifest.Screenshots)
            {
                Console.Write($"    Capturing {screenshot.Id}...");
                try
                {
                    // Switch to the target component if specified
                    if (!string.IsNullOrEmpty(screenshot.Component))
                    {
                        var json = $"{{\"component\":\"{screenshot.Component}\"}}";
                        var content = new StringContent(json, global::System.Text.Encoding.UTF8, "application/json");
                        var switchResp = await http.PostAsync($"http://localhost:{port}/preview", content);
                        if (!switchResp.IsSuccessStatusCode)
                        {
                            Console.Error.WriteLine($" ✗ Failed to switch to component '{screenshot.Component}' ({switchResp.StatusCode})");
                            continue;
                        }
                        // Wait for the component to render and a new frame to be captured
                        // At 5 fps, frames arrive every 200ms; wait long enough for
                        // the switch + layout + at least one fresh capture cycle.
                        await Task.Delay(1000);
                    }

                    var frameBytes = await http.GetByteArrayAsync($"http://localhost:{port}/frame");
                    var outputPath = Path.GetFullPath(Path.Combine(topicDir, $"{screenshot.Id}.{screenshot.Format}"));
                    if (!outputPath.StartsWith(Path.GetFullPath(topicDir) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"Screenshot id '{screenshot.Id}' would escape output directory");
                    // Auto-crop whitespace, add border + drop shadow, convert to PNG
                    var processed = ImageProcessor.Process(frameBytes);
                    File.WriteAllBytes(outputPath, processed);
                    Console.WriteLine(" ✓");
                }
                catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException)
                {
                    Console.Error.WriteLine($" ✗ {ex}");
                }
            }

            return true;
        }
        finally
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync();
                }
            }
            catch { }
        }
    }

    private static async Task<int> WaitForCapturePort(Process process, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var line = await process.StandardOutput.ReadLineAsync(cts.Token);
                if (line == null) break;

                if (line.StartsWith("CAPTURE_PORT=") &&
                    int.TryParse(line.AsSpan("CAPTURE_PORT=".Length), out var port))
                {
                    // Drain stdout in background to prevent buffer deadlock
                    _ = Task.Run(async () =>
                    {
                        while (await process.StandardOutput.ReadLineAsync() != null) { }
                    });
                    return port;
                }
            }
        }
        catch (OperationCanceledException) { }

        return -1;
    }
}

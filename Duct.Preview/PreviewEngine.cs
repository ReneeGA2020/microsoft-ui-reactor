using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using Duct.Core;
using Microsoft.UI.Dispatching;
using IOPath = System.IO.Path;

namespace Duct.Preview;

/// <summary>
/// Orchestrates the build -> load -> mount -> watch cycle for live component preview.
/// </summary>
sealed class PreviewEngine : IDisposable
{
    private readonly string _projectPath;
    private readonly string _projectDir;
    private readonly string _componentTypeName;

    private DuctHost? _host;
    private DispatcherQueue? _dispatcherQueue;
    private FileSystemWatcher? _watcher;

    private PreviewLoadContext? _currentAlc;
    private Component? _currentComponent;
    private CancellationTokenSource? _debounceCts;

    // Visible state read by the shell during render
    internal bool IsBuilding;
    internal string? ErrorTitle;
    internal string? ErrorDetails;

    private const int DebounceMs = 500;

    private static readonly string BuildPlatform =
        System.Runtime.InteropServices.RuntimeInformation.OSArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.Arm64 => "ARM64",
            _ => "x64",
        };

    public PreviewEngine(string projectPath, string componentTypeName)
    {
        _projectPath = IOPath.GetFullPath(projectPath);
        _projectDir = IOPath.GetDirectoryName(_projectPath)!;
        _componentTypeName = componentTypeName;
    }

    /// <summary>
    /// Kicks off the initial build and starts file watching.
    /// Must be called on the UI thread after DuctApp has initialized.
    /// </summary>
    public void Start()
    {
        _host = DuctApp.ActiveHost ?? throw new InvalidOperationException("DuctApp not initialized");
        _dispatcherQueue = _host.Window.DispatcherQueue;
        StartWatcher();
        _ = BuildAndReloadAsync();
    }

    public void Stop()
    {
        _watcher?.Dispose();
        _watcher = null;
        _debounceCts?.Cancel();
        UnloadCurrent();
    }

    public void Dispose() => Stop();

    /// <summary>
    /// Called by the shell function component each render to get the content layer.
    /// Returns the loaded component's element tree, or an error panel, or a loading message.
    /// </summary>
    internal Element RenderContent(RenderContext ctx)
    {
        if (ErrorTitle != null)
        {
            return UI.ScrollView(
                UI.VStack(16,
                    UI.Text(ErrorTitle)
                        .FontSize(24)
                        .Bold()
                        .Foreground("#ff4444")
                        .TextWrapping()
                        .Margin(20, 20, 20, 0),
                    UI.Text(ErrorDetails ?? "")
                        .FontSize(13)
                        .Foreground("#cccccc")
                        .TextWrapping()
                        .Selectable()
                        .Margin(20, 0, 20, 20)
                )
            );
        }

        if (_currentComponent != null)
        {
            _currentComponent.Context.BeginRender(_host!.RequestRender);
            var tree = _currentComponent.Render();
            _currentComponent.Context.FlushEffects();
            return tree;
        }

        return UI.Text("Starting...").FontSize(18).Margin(20).Foreground("#888888");
    }

    // ── Build ────────────────────────────────────────────────────────

    private async Task BuildAndReloadAsync()
    {
        if (IsBuilding) return;
        IsBuilding = true;
        ErrorTitle = null;
        ErrorDetails = null;

        UpdateTitle("Building...");
        RequestRender();
        Console.WriteLine($"[preview] Building {IOPath.GetFileName(_projectPath)}...");

        try
        {
            var (success, output, outputDll) = await RunBuildAsync();

            if (!success)
            {
                Console.Error.WriteLine($"[preview] Build FAILED:\n{output}");
                UpdateTitle("Build Failed");
                SetError("Build Failed", output);
                return;
            }

            if (outputDll == null || !File.Exists(outputDll))
            {
                Console.Error.WriteLine($"[preview] Build succeeded but output DLL not found.");
                UpdateTitle("Build Failed");
                SetError("Output Not Found", $"Could not locate output assembly.\n\nBuild output:\n{output}");
                return;
            }

            Console.WriteLine($"[preview] Build OK: {outputDll}");

            // Load the new assembly and find the component
            var (component, error) = LoadComponent(outputDll);

            if (component == null)
            {
                Console.Error.WriteLine($"[preview] Load FAILED: {error}");
                UpdateTitle("Load Failed");
                SetError("Component Load Failed", error!);
                return;
            }

            // Success — swap in the new component
            UnloadCurrent();
            _currentComponent = component;
            UpdateTitle("OK");
            Console.WriteLine($"[preview] Mounted {_componentTypeName}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[preview] Unexpected error: {ex}");
            UpdateTitle("Error");
            SetError("Unexpected Error", ex.ToString());
        }
        finally
        {
            IsBuilding = false;
            RequestRender();
        }
    }

    private void SetError(string title, string details)
    {
        ErrorTitle = title;
        ErrorDetails = details;
    }

    private async Task<(bool Success, string Output, string? OutputDll)> RunBuildAsync()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{_projectPath}\" -p:Platform={BuildPlatform} -v quiet -nologo",
            WorkingDirectory = _projectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var proc = Process.Start(psi)!;
        var stdout = await proc.StandardOutput.ReadToEndAsync();
        var stderr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();

        var combinedOutput = (stdout + "\n" + stderr).Trim();
        bool success = proc.ExitCode == 0;

        // Try to find the output DLL path from the project
        string? outputDll = null;
        if (success)
        {
            outputDll = await FindOutputDllAsync();
        }

        return (success, combinedOutput, outputDll);
    }

    private async Task<string?> FindOutputDllAsync()
    {
        // Use dotnet build to get the output path via MSBuild property
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{_projectPath}\" -p:Platform={BuildPlatform} --no-build -getProperty:TargetPath",
            WorkingDirectory = _projectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var proc = Process.Start(psi)!;
        var stdout = (await proc.StandardOutput.ReadToEndAsync()).Trim();
        await proc.WaitForExitAsync();

        if (proc.ExitCode == 0 && !string.IsNullOrEmpty(stdout) && File.Exists(stdout))
            return stdout;

        // Fallback: guess from project name and common output layouts
        var projectName = IOPath.GetFileNameWithoutExtension(_projectPath);
        var tfm = "net9.0-windows10.0.22621.0";
        var candidates = new[]
        {
            // Platform-specific output (dotnet build -p:Platform=x64)
            IOPath.Combine(_projectDir, "bin", "x64", "Debug", tfm, $"{projectName}.dll"),
            IOPath.Combine(_projectDir, "bin", "x64", "Release", tfm, $"{projectName}.dll"),
            IOPath.Combine(_projectDir, "bin", "ARM64", "Debug", tfm, $"{projectName}.dll"),
            IOPath.Combine(_projectDir, "bin", "ARM64", "Release", tfm, $"{projectName}.dll"),
            // AnyCPU / no platform
            IOPath.Combine(_projectDir, "bin", "Debug", tfm, $"{projectName}.dll"),
            IOPath.Combine(_projectDir, "bin", "Release", tfm, $"{projectName}.dll"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    // ── Assembly Loading ─────────────────────────────────────────────

    private (Component? Component, string? Error) LoadComponent(string dllPath)
    {
        try
        {
            var alc = new PreviewLoadContext(dllPath);

            // Load from memory — no file lock on the DLL
            var bytes = File.ReadAllBytes(dllPath);
            var pdbPath = IOPath.ChangeExtension(dllPath, ".pdb");
            byte[]? pdbBytes = File.Exists(pdbPath) ? File.ReadAllBytes(pdbPath) : null;

            Assembly asm;
            if (pdbBytes != null)
                asm = alc.LoadFromStream(new MemoryStream(bytes), new MemoryStream(pdbBytes));
            else
                asm = alc.LoadFromStream(new MemoryStream(bytes));

            // Find the component type
            Type? componentType = null;

            // Try exact match first (fully qualified)
            componentType = asm.GetType(_componentTypeName);

            // Try simple name match
            if (componentType == null)
            {
                componentType = asm.GetExportedTypes()
                    .FirstOrDefault(t =>
                        t.Name == _componentTypeName &&
                        typeof(Component).IsAssignableFrom(t) &&
                        !t.IsAbstract);
            }

            // Try case-insensitive
            if (componentType == null)
            {
                componentType = asm.GetExportedTypes()
                    .FirstOrDefault(t =>
                        string.Equals(t.Name, _componentTypeName, StringComparison.OrdinalIgnoreCase) &&
                        typeof(Component).IsAssignableFrom(t) &&
                        !t.IsAbstract);
            }

            if (componentType == null)
            {
                var available = asm.GetExportedTypes()
                    .Where(t => typeof(Component).IsAssignableFrom(t) && !t.IsAbstract)
                    .Select(t => t.FullName)
                    .ToList();

                var msg = $"Component '{_componentTypeName}' not found in {IOPath.GetFileName(dllPath)}.";
                if (available.Count > 0)
                    msg += $"\n\nAvailable components:\n  {string.Join("\n  ", available)}";
                else
                    msg += "\n\nNo Component subclasses found in assembly.";

                alc.Unload();
                return (null, msg);
            }

            if (!typeof(Component).IsAssignableFrom(componentType))
            {
                alc.Unload();
                return (null, $"Type '{componentType.FullName}' does not inherit from Duct.Core.Component.");
            }

            var instance = (Component)Activator.CreateInstance(componentType)!;
            _currentAlc = alc;
            return (instance, null);
        }
        catch (Exception ex)
        {
            return (null, $"Failed to load assembly: {ex.Message}\n\n{ex}");
        }
    }

    private void UnloadCurrent()
    {
        if (_currentComponent != null)
        {
            try { _currentComponent.Context.RunCleanups(); }
            catch { /* best-effort cleanup */ }
            _currentComponent = null;
        }

        if (_currentAlc != null)
        {
            try { _currentAlc.Unload(); }
            catch { /* best-effort */ }
            _currentAlc = null;
        }
    }

    // ── UI thread helpers ────────────────────────────────────────────

    private void RequestRender()
    {
        _dispatcherQueue?.TryEnqueue(() => _host?.RequestRender());
    }

    private void UpdateTitle(string status)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            if (_host?.Window != null)
                _host.Window.Title = $"Duct Preview — {IOPath.GetFileName(_projectPath)} [{status}]";
        });
    }

    // ── File Watching ────────────────────────────────────────────────

    private void StartWatcher()
    {
        _watcher = new FileSystemWatcher(_projectDir)
        {
            Filter = "*.cs",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Renamed += (s, e) => OnFileChanged(s, e);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Ignore bin/obj directories
        if (e.FullPath.Contains(IOPath.DirectorySeparatorChar + "bin" + IOPath.DirectorySeparatorChar) ||
            e.FullPath.Contains(IOPath.DirectorySeparatorChar + "obj" + IOPath.DirectorySeparatorChar) ||
            e.FullPath.Contains("/" + "bin" + "/") ||
            e.FullPath.Contains("/" + "obj" + "/"))
            return;

        // Debounce — cancel previous pending rebuild, schedule a new one
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceMs, token);
                if (!token.IsCancellationRequested)
                    await BuildAndReloadAsync();
            }
            catch (OperationCanceledException) { /* debounced away */ }
        });
    }
}

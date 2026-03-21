namespace Duct.Core;

/// <summary>
/// Logging severity levels for the Duct framework.
/// </summary>
public enum DuctLogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
}

/// <summary>
/// Logging abstraction for the Duct framework.
/// Implement this interface to integrate Duct logging with your application's logging pipeline.
/// Use <see cref="DebugDuctLogger"/> for development or <see cref="NullDuctLogger"/> to suppress output.
/// </summary>
public interface IDuctLogger
{
    void Log(DuctLogLevel level, string message);
    void Log(DuctLogLevel level, string message, Exception? exception);
}

/// <summary>
/// Default logger that writes to <see cref="System.Diagnostics.Debug.WriteLine"/>.
/// Output is only visible in debug builds attached to a debugger.
/// </summary>
public sealed class DebugDuctLogger : IDuctLogger
{
    public void Log(DuctLogLevel level, string message)
    {
        System.Diagnostics.Debug.WriteLine($"[Duct:{level}] {message}");
    }

    public void Log(DuctLogLevel level, string message, Exception? exception)
    {
        if (exception is not null)
            System.Diagnostics.Debug.WriteLine($"[Duct:{level}] {message}: {exception}");
        else
            System.Diagnostics.Debug.WriteLine($"[Duct:{level}] {message}");
    }
}

/// <summary>
/// No-op logger that discards all messages. Use when no logging is desired.
/// </summary>
public sealed class NullDuctLogger : IDuctLogger
{
    public static NullDuctLogger Instance { get; } = new();
    public void Log(DuctLogLevel level, string message) { }
    public void Log(DuctLogLevel level, string message, Exception? exception) { }
}

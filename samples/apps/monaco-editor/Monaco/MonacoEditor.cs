using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace MonacoEditorApp;

// AI-HINT: WebView2 wrapper around VS Code's Monaco editor.
//   Communication: C# ↔ JS via CoreWebView2.WebMessageReceived / ExecuteScriptAsync.
//   Initialization: OnLoaded → EnsureCoreWebView2Async → SetVirtualHostNameToFolderMapping
//     → Navigate to monaco-editor.html with config in URL fragment.
//   Two-way sync: _lastKnownText prevents echo loops between DependencyProperty and JS.
//   _pendingCommands queue: commands issued before editor is ready are replayed on "ready" message.
//   Recycling: OnLoaded detects already-initialized WebView2 and pushes state without re-init.

/// <summary>
/// A standalone WinUI 3 control that hosts the Monaco code editor inside a WebView2.
/// Can be used directly in XAML without any Reactor dependency.
/// </summary>
public sealed partial class MonacoEditor : UserControl, IDisposable
{
    private bool _disposed;
    private WebView2? _webView;
    private bool _isEditorReady;
    private readonly Queue<Func<Task>> _pendingCommands = new();

    // Track last-known text to avoid roundtrip echo during two-way sync
    private string _lastKnownText = "";

    public MonacoEditor()
    {
        global::System.Diagnostics.Debug.WriteLine($"[MonacoEditor] CONSTRUCTOR called — new instance {GetHashCode()}");
        _webView = new WebView2();
        Content = _webView;
        _webView.CoreWebView2Initialized += OnCoreWebView2Initialized;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    // ── Dependency Properties ─────────────────────────────────────────

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(MonacoEditor),
            new PropertyMetadata("", OnTextChanged));

    public static readonly DependencyProperty EditorLanguageProperty =
        DependencyProperty.Register(nameof(EditorLanguage), typeof(string), typeof(MonacoEditor),
            new PropertyMetadata("plaintext", OnLanguageChanged));

    public static readonly DependencyProperty ThemeProperty =
        DependencyProperty.Register(nameof(Theme), typeof(string), typeof(MonacoEditor),
            new PropertyMetadata("vs", OnThemeChanged));

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(MonacoEditor),
            new PropertyMetadata(false, OnIsReadOnlyChanged));

    public static readonly DependencyProperty EditorFontSizeProperty =
        DependencyProperty.Register(nameof(EditorFontSize), typeof(double), typeof(MonacoEditor),
            new PropertyMetadata(14.0, OnEditorFontSizeChanged));

    public static readonly DependencyProperty WordWrapProperty =
        DependencyProperty.Register(nameof(WordWrap), typeof(bool), typeof(MonacoEditor),
            new PropertyMetadata(false, OnWordWrapChanged));

    public static readonly DependencyProperty MinimapEnabledProperty =
        DependencyProperty.Register(nameof(MinimapEnabled), typeof(bool), typeof(MonacoEditor),
            new PropertyMetadata(true, OnMinimapEnabledChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string EditorLanguage
    {
        get => (string)GetValue(EditorLanguageProperty);
        set => SetValue(EditorLanguageProperty, value);
    }

    public string Theme
    {
        get => (string)GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public double EditorFontSize
    {
        get => (double)GetValue(EditorFontSizeProperty);
        set => SetValue(EditorFontSizeProperty, value);
    }

    public bool WordWrap
    {
        get => (bool)GetValue(WordWrapProperty);
        set => SetValue(WordWrapProperty, value);
    }

    public bool MinimapEnabled
    {
        get => (bool)GetValue(MinimapEnabledProperty);
        set => SetValue(MinimapEnabledProperty, value);
    }

    // ── Events ────────────────────────────────────────────────────────

    public event EventHandler<MonacoTextChangedEventArgs>? TextChanged;
    public event EventHandler<MonacoCursorChangedEventArgs>? CursorPositionChanged;
    public event EventHandler? EditorReady;

    // ── Lifecycle ─────────────────────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        global::System.Diagnostics.Debug.WriteLine($"[MonacoEditor] OnLoaded {GetHashCode()} — CoreWebView2={_webView?.CoreWebView2 is not null}, _isEditorReady={_isEditorReady}");
        if (_webView?.CoreWebView2 is not null)
        {
            global::System.Diagnostics.Debug.WriteLine($"[MonacoEditor] RECYCLED {GetHashCode()} — reattaching handler and pushing state");
            _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            if (_isEditorReady)
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    try { await PushAllStateAsync(); }
                    catch (Exception ex) { global::System.Diagnostics.Debug.WriteLine($"MonacoEditor: PushAllStateAsync failed: {ex}"); }
                });
            }
            return;
        }

        // Defer WebView2 init to avoid COM threading issues when called during Reactor's render pass.
        DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                await _webView!.EnsureCoreWebView2Async();
            }
            catch (Exception ex)
            {
                global::System.Diagnostics.Debug.WriteLine($"MonacoEditor: EnsureCoreWebView2Async failed: {ex}");
            }
        });
    }

    private void OnCoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
    {
        if (args.Exception is not null)
        {
            global::System.Diagnostics.Debug.WriteLine($"MonacoEditor: CoreWebView2 init failed: {args.Exception}");
            return;
        }

        var coreWv = _webView!.CoreWebView2;

        // Map the entire Monaco folder (HTML + Assets) to a virtual hostname.
        // This lets the HTML load JS/CSS from relative paths within the same origin.
        var monacoPath = Path.Combine(AppContext.BaseDirectory, "Monaco");
        coreWv.SetVirtualHostNameToFolderMapping(
            "monaco.local", monacoPath, CoreWebView2HostResourceAccessKind.Allow);

        coreWv.WebMessageReceived -= OnWebMessageReceived;
        coreWv.WebMessageReceived += OnWebMessageReceived;

#if DEBUG
        // Enable DevTools for debugging (F12)
        coreWv.Settings.AreDevToolsEnabled = true;
#endif

        // Build the initial config and navigate via virtual host URL
        var config = new MonacoInitConfig
        {
            Value = Text ?? "",
            Language = EditorLanguage ?? "plaintext",
            Theme = Theme ?? "vs",
            ReadOnly = IsReadOnly,
            FontSize = EditorFontSize,
            WordWrap = WordWrap,
            Minimap = MinimapEnabled,
        };
        var configJson = JsonSerializer.Serialize(config, MonacoJsonContext.Default.MonacoInitConfig);
        var encodedConfig = Uri.EscapeDataString(configJson);

        // Navigate to the HTML via virtual host (same origin as JS assets)
        coreWv.Navigate($"https://monaco.local/monaco-editor.html#{encodedConfig}");
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        global::System.Diagnostics.Debug.WriteLine($"[MonacoEditor] OnUnloaded {GetHashCode()}");
        if (_webView?.CoreWebView2 is not null)
        {
            _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_webView?.CoreWebView2 is not null)
        {
            _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
        }
        _webView?.Close();
        _webView = null;
    }

    // ── WebView2 message handling ─────────────────────────────────────

    private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        var json = args.TryGetWebMessageAsString();
        if (json is null) return;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (!root.TryGetProperty("type", out var typeProp)) return;
        var type = typeProp.GetString();

        switch (type)
        {
            case "ready":
                _isEditorReady = true;
                _lastKnownText = Text ?? "";
                EditorReady?.Invoke(this, EventArgs.Empty);
                _ = ExecuteWithErrorHandling(FlushPendingCommandsAsync);
                break;

            case "contentChanged":
                if (!root.TryGetProperty("value", out var valueProp) ||
                    !root.TryGetProperty("isFlush", out var flushProp)) return;
                var newText = valueProp.GetString() ?? "";
                _lastKnownText = newText;
                // Update the DP without triggering a roundtrip back to JS
                SetValue(TextProperty, newText);
                TextChanged?.Invoke(this, new MonacoTextChangedEventArgs(newText,
                    flushProp.GetBoolean()));
                break;

            case "cursorChanged":
                if (!root.TryGetProperty("lineNumber", out var lineProp) ||
                    !root.TryGetProperty("column", out var colProp)) return;
                CursorPositionChanged?.Invoke(this, new MonacoCursorChangedEventArgs(
                    lineProp.GetInt32(),
                    colProp.GetInt32()));
                break;
        }
    }

    /// <summary>
    /// Push all current property values to the already-initialized editor.
    /// Used when a pooled MonacoEditor is re-attached to the visual tree.
    /// </summary>
    private async Task PushAllStateAsync()
    {
        var text = Text ?? "";
        _lastKnownText = text;
        await ExecuteScriptAsync($"monacoSetValue({JsonSerializer.Serialize(text, MonacoJsonContext.Default.String)})");
        await ExecuteScriptAsync($"monacoSetLanguage({JsonSerializer.Serialize(EditorLanguage ?? "plaintext", MonacoJsonContext.Default.String)})");
        await ExecuteScriptAsync($"monacoSetTheme({JsonSerializer.Serialize(Theme ?? "vs", MonacoJsonContext.Default.String)})");
        await ExecuteScriptAsync($"monacoSetReadOnly({IsReadOnly.ToString().ToLowerInvariant()})");
        await ExecuteScriptAsync($"monacoSetFontSize({EditorFontSize.ToString(global::System.Globalization.CultureInfo.InvariantCulture)})");
        await ExecuteScriptAsync($"monacoSetWordWrap({WordWrap.ToString().ToLowerInvariant()})");
        await ExecuteScriptAsync($"monacoSetMinimap({MinimapEnabled.ToString().ToLowerInvariant()})");
    }

    // ── DP change callbacks ───────────────────────────────────────────

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (MonacoEditor)d;
        var newText = (string)e.NewValue ?? "";
        // Skip if this update came from JS (the editor already has this text)
        if (newText == editor._lastKnownText) return;
        editor._lastKnownText = newText;
        editor.EnqueueCommand(() => editor.ExecuteScriptAsync($"monacoSetValue({JsonSerializer.Serialize(newText, MonacoJsonContext.Default.String)})"));
    }

    private static void OnLanguageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (MonacoEditor)d;
        editor.EnqueueCommand(() => editor.ExecuteScriptAsync($"monacoSetLanguage({JsonSerializer.Serialize((string)e.NewValue, MonacoJsonContext.Default.String)})"));
    }

    private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (MonacoEditor)d;
        editor.EnqueueCommand(() => editor.ExecuteScriptAsync($"monacoSetTheme({JsonSerializer.Serialize((string)e.NewValue, MonacoJsonContext.Default.String)})"));
    }

    private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (MonacoEditor)d;
        editor.EnqueueCommand(() => editor.ExecuteScriptAsync($"monacoSetReadOnly({((bool)e.NewValue).ToString().ToLowerInvariant()})"));
    }

    private static void OnEditorFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (MonacoEditor)d;
        editor.EnqueueCommand(() => editor.ExecuteScriptAsync($"monacoSetFontSize({((double)e.NewValue).ToString(global::System.Globalization.CultureInfo.InvariantCulture)})"));
    }

    private static void OnWordWrapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (MonacoEditor)d;
        editor.EnqueueCommand(() => editor.ExecuteScriptAsync($"monacoSetWordWrap({((bool)e.NewValue).ToString().ToLowerInvariant()})"));
    }

    private static void OnMinimapEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (MonacoEditor)d;
        editor.EnqueueCommand(() => editor.ExecuteScriptAsync($"monacoSetMinimap({((bool)e.NewValue).ToString().ToLowerInvariant()})"));
    }

    // ── Command queue (buffers commands until editor is ready) ────────

    private void EnqueueCommand(Func<Task> command)
    {
        if (_isEditorReady)
        {
            _ = ExecuteWithErrorHandling(command);
        }
        else
        {
            _pendingCommands.Enqueue(command);
        }
    }

    private async Task FlushPendingCommandsAsync()
    {
        while (_pendingCommands.Count > 0)
        {
            var cmd = _pendingCommands.Dequeue();
            await cmd();
        }
    }

    private static async Task ExecuteWithErrorHandling(Func<Task> command)
    {
        try
        {
            await command();
        }
        catch (InvalidOperationException ex)
        {
            global::System.Diagnostics.Trace.TraceWarning($"MonacoEditor: async command failed: {ex}");
        }
        catch (global::System.Runtime.InteropServices.COMException ex)
        {
            global::System.Diagnostics.Trace.TraceWarning($"MonacoEditor: async command failed: {ex}");
        }
    }

    private async Task ExecuteScriptAsync(string script)
    {
        if (_webView?.CoreWebView2 is not null)
        {
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
        }
    }

    // ── Public methods ────────────────────────────────────────────────

    /// <summary>
    /// Reveals the specified line in the center of the editor viewport.
    /// </summary>
    public void RevealLine(int lineNumber) =>
        EnqueueCommand(() => ExecuteScriptAsync($"monacoRevealLine({lineNumber.ToString(global::System.Globalization.CultureInfo.InvariantCulture)})"));

    /// <summary>
    /// Sets the cursor position and focuses the editor.
    /// </summary>
    public void SetCursorPosition(int lineNumber, int column) =>
        EnqueueCommand(() => ExecuteScriptAsync($"monacoSetPosition({lineNumber.ToString(global::System.Globalization.CultureInfo.InvariantCulture)},{column.ToString(global::System.Globalization.CultureInfo.InvariantCulture)})"));

    /// <summary>
    /// Sends arbitrary Monaco editor options as a JSON string.
    /// </summary>
    public void UpdateOptions(string optionsJson) =>
        EnqueueCommand(() => ExecuteScriptAsync($"monacoUpdateOptions(JSON.parse({JsonSerializer.Serialize(optionsJson, MonacoJsonContext.Default.String)}))"));

    /// <summary>
    /// Opens the find widget with the given search string.
    /// </summary>
    public void FindText(string searchString) =>
        EnqueueCommand(() => ExecuteScriptAsync($"monacoFindText({JsonSerializer.Serialize(searchString, MonacoJsonContext.Default.String)})"));
}

// ── Event args ────────────────────────────────────────────────────────

public class MonacoTextChangedEventArgs : EventArgs
{
    public string Text { get; }
    public bool IsFlush { get; }
    internal MonacoTextChangedEventArgs(string text, bool isFlush) { Text = text; IsFlush = isFlush; }
}

public class MonacoCursorChangedEventArgs : EventArgs
{
    public int LineNumber { get; }
    public int Column { get; }
    internal MonacoCursorChangedEventArgs(int lineNumber, int column) { LineNumber = lineNumber; Column = column; }
}

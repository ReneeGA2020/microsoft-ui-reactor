using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;

namespace MonacoEditorApp;

// Reactor element type for the MonacoEditor WinUI UserControl. Registering this
// with the reconciler (see MonacoEditorRegistration) makes it behave like any
// built-in Reactor element: reactive props, proper mount/update/unmount lifecycle,
// no direct manipulation of WinUI children collections.

/// <summary>
/// Declarative Reactor element that renders a <see cref="MonacoEditor"/> UserControl.
/// Register with the reconciler once via <see cref="MonacoEditorRegistration.Register"/>.
/// </summary>
public record MonacoEditorElement(
    string Text = "",
    Action<string>? OnTextChanged = null
) : Element
{
    public string Language { get; init; } = "plaintext";
    public string Theme { get; init; } = "vs";
    public bool IsReadOnly { get; init; }
    public double FontSize { get; init; } = 14;
    public bool WordWrap { get; init; }
    public bool MinimapEnabled { get; init; } = true;
    /// <summary>Invoked once when the underlying control is created. Use for imperative APIs (e.g., FindText).</summary>
    public Action<MonacoEditor>? OnControlMounted { get; init; }
}

public static class MonacoEditorRegistration
{
    /// <summary>
    /// Registers <see cref="MonacoEditorElement"/> with the reconciler so it can be
    /// used anywhere in a Reactor element tree.
    /// </summary>
    public static void Register(Reconciler reconciler)
    {
        reconciler.RegisterType<MonacoEditorElement, MonacoEditor>(
            mount: static (r, el, _) =>
            {
                var editor = new MonacoEditor
                {
                    Text = el.Text,
                    EditorLanguage = el.Language,
                    Theme = el.Theme,
                    IsReadOnly = el.IsReadOnly,
                    EditorFontSize = el.FontSize,
                    WordWrap = el.WordWrap,
                    MinimapEnabled = el.MinimapEnabled,
                    Tag = el,
                };
                editor.TextChanged += (s, args) =>
                {
                    if (args.IsFlush) return;
                    (((FrameworkElement)s!).Tag as MonacoEditorElement)?.OnTextChanged?.Invoke(args.Text);
                };
                el.OnControlMounted?.Invoke(editor);
                return editor;
            },
            update: static (r, oldEl, newEl, editor, _) =>
            {
                if (newEl.Text != oldEl.Text) editor.Text = newEl.Text;
                if (newEl.Language != oldEl.Language) editor.EditorLanguage = newEl.Language;
                if (newEl.Theme != oldEl.Theme) editor.Theme = newEl.Theme;
                if (newEl.IsReadOnly != oldEl.IsReadOnly) editor.IsReadOnly = newEl.IsReadOnly;
                if (newEl.FontSize != oldEl.FontSize) editor.EditorFontSize = newEl.FontSize;
                if (newEl.WordWrap != oldEl.WordWrap) editor.WordWrap = newEl.WordWrap;
                if (newEl.MinimapEnabled != oldEl.MinimapEnabled) editor.MinimapEnabled = newEl.MinimapEnabled;
                editor.Tag = newEl;
                return null;
            },
            unmount: static (r, editor) =>
            {
                editor.Tag = null;
                editor.Dispose();
            });
    }
}

/// <summary>DSL helpers for <see cref="MonacoEditorElement"/>. Mirrors the Reactor factory style.</summary>
public static class MonacoDsl
{
    public static MonacoEditorElement MonacoEditor(
        string text = "",
        Action<string>? onTextChanged = null,
        string language = "plaintext",
        string theme = "vs") =>
        new(text, onTextChanged) { Language = language, Theme = theme };
}

// Monaco Editor sample — a simple text editor for testing the MonacoEditor control.
// Supports file open, save, drag-and-drop, and language selection.

using Duct;
using Duct.Core;
using Duct.Flex;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;

public static class Program
{
    [STAThread]
    static void Main() => DuctApp.Run<EditorApp>("Monaco Editor", width: 1200, height: 800);
}

class EditorApp : Component
{
    static readonly (string Label, string Id)[] Languages =
    [
        ("Plain Text", "plaintext"),
        ("C#", "csharp"),
        ("C/C++", "cpp"),
        ("CSS", "css"),
        ("Go", "go"),
        ("HTML", "html"),
        ("Java", "java"),
        ("JavaScript", "javascript"),
        ("JSON", "json"),
        ("Markdown", "markdown"),
        ("PowerShell", "powershell"),
        ("Python", "python"),
        ("Rust", "rust"),
        ("Shell", "shell"),
        ("SQL", "sql"),
        ("TypeScript", "typescript"),
        ("XML", "xml"),
        ("YAML", "yaml"),
    ];

    static readonly Dictionary<string, string> ExtToLang = new(StringComparer.OrdinalIgnoreCase)
    {
        [".cs"] = "csharp", [".csx"] = "csharp",
        [".c"] = "cpp", [".cpp"] = "cpp", [".cc"] = "cpp", [".h"] = "cpp", [".hpp"] = "cpp",
        [".css"] = "css",
        [".go"] = "go",
        [".html"] = "html", [".htm"] = "html",
        [".java"] = "java",
        [".js"] = "javascript", [".mjs"] = "javascript",
        [".json"] = "json",
        [".md"] = "markdown",
        [".ps1"] = "powershell", [".psm1"] = "powershell",
        [".py"] = "python",
        [".rs"] = "rust",
        [".sh"] = "shell", [".bash"] = "shell",
        [".sql"] = "sql",
        [".ts"] = "typescript", [".tsx"] = "typescript",
        [".xml"] = "xml", [".xaml"] = "xml", [".csproj"] = "xml", [".sln"] = "xml",
        [".yaml"] = "yaml", [".yml"] = "yaml",
    };

    static int LangIndex(string langId) =>
        Math.Max(0, Array.FindIndex(Languages, l => l.Id == langId));

    static string DetectLanguage(string path) =>
        ExtToLang.TryGetValue(Path.GetExtension(path), out var lang) ? lang : "plaintext";

    public override Element Render()
    {
        var (text, setText) = UseState("");
        var (langIndex, setLangIndex) = UseState(0);
        var (theme, setTheme) = UseState("vs-dark");
        var (filePath, setFilePath) = UseState<string?>(null);
        var (isDirty, setIsDirty) = UseState(false);
        var (status, setStatus) = UseState("Ready");

        var language = Languages[langIndex].Id;

        void OnTextChanged(string newText) { setText(newText); setIsDirty(true); }

        void LoadFile(string path, string content)
        {
            setText(content);
            setFilePath(path);
            setIsDirty(false);
            setLangIndex(LangIndex(DetectLanguage(path)));
            setStatus($"Opened: {path}");
        }

        async void OnOpen()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add("*");
            InitPicker(picker);

            var file = await picker.PickSingleFileAsync();
            if (file is null) return;

            var content = await File.ReadAllTextAsync(file.Path);
            LoadFile(file.Path, content);
        }

        async void OnSave()
        {
            if (filePath is not null)
            {
                await File.WriteAllTextAsync(filePath, text);
                setIsDirty(false);
                setStatus($"Saved: {filePath}");
            }
            else
            {
                OnSaveAs();
            }
        }

        async void OnSaveAs()
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("All Files", [".txt", ".cs", ".js", ".json", ".xml", ".md"]);
            if (filePath is not null)
                picker.SuggestedFileName = Path.GetFileName(filePath);
            InitPicker(picker);

            var file = await picker.PickSaveFileAsync();
            if (file is null) return;

            await File.WriteAllTextAsync(file.Path, text);
            setFilePath(file.Path);
            setIsDirty(false);
            setLangIndex(LangIndex(DetectLanguage(file.Path)));
            setStatus($"Saved: {file.Path}");
        }

        void OnNew() { setText(""); setFilePath(null); setIsDirty(false); setLangIndex(0); setStatus("New file"); }

        var title = filePath is not null
            ? $"{Path.GetFileName(filePath)}{(isDirty ? " *" : "")}"
            : isDirty ? "Untitled *" : "Untitled";

        bool showPreview = language == "markdown";
        string[] columns = showPreview ? ["*", "Auto", "*"] : ["*"];

        var toolbar = (FlexRow(
            Button("New", OnNew),
            Button("Open...", OnOpen),
            Button("Save", OnSave),
            Button("Save As...", OnSaveAs),
            Text("").Flex(grow: 1),
            Text("Language:").VAlign(VerticalAlignment.Center),
            ComboBox(
                Languages.Select(l => l.Label).ToArray(),
                langIndex,
                i => setLangIndex(i)
            ).Width(160),
            Text("").Width(8),
            Text("Theme:").VAlign(VerticalAlignment.Center),
            ComboBox(
                ["Light", "Dark", "High Contrast"],
                theme switch { "vs" => 0, "vs-dark" => 1, "hc-black" => 2, _ => 1 },
                i => setTheme(i switch { 0 => "vs", 1 => "vs-dark", 2 => "hc-black", _ => "vs-dark" })
            ).Width(140)
        ) with { AlignItems = FlexAlign.Center, ColumnGap = 6 })
        .Padding(8)
        .Grid(row: 0, columnSpan: columns.Length);

        var statusBar = (FlexRow(
            Text(title).FontSize(12),
            Text("").Flex(grow: 1),
            Text(status).FontSize(12).Opacity(0.7)
        ) with { ColumnGap = 12 })
        .Padding(8, 4)
        .Grid(row: 2, columnSpan: columns.Length);

        var editor = MonacoEditor(text, OnTextChanged, language, theme)
            .Grid(row: 1, column: 0);

        if (!showPreview)
        {
            return Grid(columns, ["Auto", "*", "Auto"],
                toolbar, editor, statusBar
            );
        }

        var previewPane = ScrollView(
            Markdown(text).Margin(16).Foreground("#D4D4D4")
        ).Set(sv => sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled)
         .Background("White");

        return FlexColumn(
            VStack(toolbar).Flex(shrink:0),
            FlexRow(editor.Flex(grow:1, basis:0), previewPane.Flex(grow: 1, basis:0)).Flex(grow:1, basis:0),
            VStack(statusBar).Flex(shrink:0)
        );
    }

    static void InitPicker(object picker)
    {
        var window = DuctApp.ActiveHost?.Window;
        if (window is null) return;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
    }
}

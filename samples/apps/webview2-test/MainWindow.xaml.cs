using Microsoft.UI.Xaml;
using System.Diagnostics;

namespace WebView2Test;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 600));

        // Add MonacoEditor directly (not through Duct reconciler)
        var editor = new Duct.Monaco.MonacoEditor
        {
            Text = "// Hello from Monaco!\nconsole.log('It works!');",
            EditorLanguage = "javascript",
            Theme = "vs-dark",
        };
        editor.EditorReady += (s, e) =>
        {
            StatusText.Text = "Monaco editor ready!";
        };

        Microsoft.UI.Xaml.Controls.Grid.SetRow(editor, 1);
        ((Microsoft.UI.Xaml.Controls.Grid)Content).Children.Add(editor);
    }
}

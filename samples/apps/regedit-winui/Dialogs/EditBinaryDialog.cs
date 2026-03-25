using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegeditWinUI.Dialogs;

public static class EditBinaryDialog
{
    public static async Task<byte[]?> ShowAsync(XamlRoot xamlRoot, string valueName, byte[] currentData)
    {
        var nameBox = new TextBox
        {
            Text = string.IsNullOrEmpty(valueName) ? "(Default)" : valueName,
            IsReadOnly = true,
            Margin = new Thickness(0, 0, 0, 12)
        };

        // Format as hex dump: offset  hex bytes  ASCII
        var dataBox = new TextBox
        {
            Text = FormatHexDump(currentData),
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Consolas"),
            Height = 300,
            IsSpellCheckEnabled = false
        };

        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock { Text = Strings.LabelValueName });
        panel.Children.Add(nameBox);
        panel.Children.Add(new TextBlock { Text = Strings.LabelValueData });
        panel.Children.Add(dataBox);

        var dialog = new ContentDialog
        {
            Title = Strings.DialogEditBinary,
            Content = panel,
            PrimaryButtonText = Strings.ButtonOK,
            CloseButtonText = Strings.ButtonCancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return null;

        return ParseHexDump(dataBox.Text);
    }

    private static string FormatHexDump(byte[] data)
    {
        var sb = new StringBuilder();
        for (int offset = 0; offset < data.Length; offset += 8)
        {
            sb.Append($"{offset:X4}  ");
            int count = Math.Min(8, data.Length - offset);
            for (int i = 0; i < 8; i++)
            {
                if (i < count)
                    sb.Append($"{data[offset + i]:X2} ");
                else
                    sb.Append("   ");
            }
            sb.Append(" ");
            for (int i = 0; i < count; i++)
            {
                byte b = data[offset + i];
                sb.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
            }
            sb.AppendLine();
        }
        if (data.Length == 0)
            sb.AppendLine("0000  ");
        return sb.ToString().TrimEnd();
    }

    private static byte[] ParseHexDump(string text)
    {
        var bytes = new List<byte>();
        foreach (string line in text.Split('\n'))
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            // Skip the offset (first 4+ hex chars + spaces)
            int hexStart = trimmed.IndexOf("  ");
            if (hexStart < 0) continue;

            string hexPart = trimmed.Substring(hexStart).Trim();
            // Take only the hex bytes portion (before the ASCII section)
            // ASCII section starts after double space or after 8 hex pairs
            var parts = hexPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                if (part.Length == 2 && byte.TryParse(part, NumberStyles.HexNumber, null, out byte b))
                    bytes.Add(b);
                else
                    break; // hit ASCII portion
            }
        }
        return bytes.ToArray();
    }
}

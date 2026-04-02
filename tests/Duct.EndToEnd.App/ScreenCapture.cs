using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Duct.EndToEnd.App;

/// <summary>
/// Captures a WinUI 3 window to a PNG file using PrintWindow P/Invoke.
/// Uses PW_RENDERFULLCONTENT (2) which captures DirectComposition content
/// including XAML islands and WinUI 3 rendered content.
/// </summary>
internal static class ScreenCapture
{
    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint start, uint lines,
        byte[] buffer, ref BITMAPINFO bi, uint usage);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    private const uint PW_RENDERFULLCONTENT = 2;
    private const uint DIB_RGB_COLORS = 0;
    private const int BI_RGB = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth, biHeight;
        public ushort biPlanes, biBitCount;
        public uint biCompression, biSizeImage;
        public int biXPelsPerMeter, biYPelsPerMeter;
        public uint biClrUsed, biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
    }

    public static async Task CaptureWindowAsync(Window window, string outputPath)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        if (!GetWindowRect(hwnd, out var rect)) return;

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0) return;

        var screenDc = GetDC(IntPtr.Zero);
        var memDc = CreateCompatibleDC(screenDc);
        var hBitmap = CreateCompatibleBitmap(screenDc, width, height);
        var oldBitmap = SelectObject(memDc, hBitmap);

        try
        {
            PrintWindow(hwnd, memDc, PW_RENDERFULLCONTENT);

            // Extract pixel data via GetDIBits
            var bi = new BITMAPINFO
            {
                bmiHeader = new BITMAPINFOHEADER
                {
                    biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                    biWidth = width,
                    biHeight = -height, // top-down
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = BI_RGB,
                }
            };

            var pixels = new byte[width * height * 4];
            GetDIBits(memDc, hBitmap, 0, (uint)height, pixels, ref bi, DIB_RGB_COLORS);

            // BGRA → RGBA swap
            for (int i = 0; i < pixels.Length; i += 4)
                (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);

            // Write PNG via BitmapEncoder
            using var stream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied,
                (uint)width, (uint)height, 96, 96, pixels);
            await encoder.FlushAsync();

            stream.Seek(0);
            using var fileStream = File.Create(outputPath);
            var reader = new DataReader(stream);
            await reader.LoadAsync((uint)stream.Size);
            var fileBytes = new byte[stream.Size];
            reader.ReadBytes(fileBytes);
            await fileStream.WriteAsync(fileBytes);
        }
        finally
        {
            SelectObject(memDc, oldBitmap);
            DeleteObject(hBitmap);
            DeleteDC(memDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }
    }
}

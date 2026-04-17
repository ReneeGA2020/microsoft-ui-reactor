using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Microsoft.UI.Reactor.Cli.Docs;

/// <summary>
/// Post-processes captured screenshots: auto-crops whitespace,
/// adds a subtle border and drop shadow so images don't blend into the page.
/// </summary>
internal static class ImageProcessor
{
    private const int ContentPadding = 8;   // breathing room inside the border
    private const int ShadowOffset = 2;     // shadow offset (right + down)
    private const int ShadowBlur = 6;       // number of graduated shadow layers
    private const float ShadowMaxAlpha = 0.12f;

    /// <summary>
    /// Auto-crops whitespace from a captured frame, adds border + drop shadow,
    /// and returns PNG bytes.
    /// </summary>
    public static byte[] Process(byte[] frameBytes)
    {
        using var ms = new MemoryStream(frameBytes);
        using var source = new Bitmap(ms);

        // 1. Find content bounds (trim white edges)
        var bounds = FindContentBounds(source, threshold: 248);

        // Add small padding around content so the border isn't flush
        bounds = InflateClamp(bounds, ContentPadding, source.Width, source.Height);

        using var cropped = source.Clone(bounds, PixelFormat.Format32bppArgb);

        // 2. Add border + shadow
        using var result = AddBorderAndShadow(cropped);

        // 3. Encode as PNG
        using var output = new MemoryStream();
        result.Save(output, ImageFormat.Png);
        return output.ToArray();
    }

    private static Rectangle FindContentBounds(Bitmap bmp, int threshold)
    {
        int top = 0, bottom = bmp.Height - 1;
        int left = 0, right = bmp.Width - 1;

        // Scan from top
        for (int y = 0; y < bmp.Height; y++)
        {
            if (RowHasContent(bmp, y, threshold)) { top = y; break; }
        }

        // Scan from bottom
        for (int y = bmp.Height - 1; y >= top; y--)
        {
            if (RowHasContent(bmp, y, threshold)) { bottom = y; break; }
        }

        // Scan from left
        for (int x = 0; x < bmp.Width; x++)
        {
            if (ColumnHasContent(bmp, x, top, bottom, threshold)) { left = x; break; }
        }

        // Scan from right
        for (int x = bmp.Width - 1; x >= left; x--)
        {
            if (ColumnHasContent(bmp, x, top, bottom, threshold)) { right = x; break; }
        }

        return new Rectangle(left, top, right - left + 1, bottom - top + 1);
    }

    private static bool RowHasContent(Bitmap bmp, int y, int threshold)
    {
        for (int x = 0; x < bmp.Width; x += 2) // sample every other pixel for speed
        {
            var p = bmp.GetPixel(x, y);
            if (p.R < threshold || p.G < threshold || p.B < threshold) return true;
        }
        return false;
    }

    private static bool ColumnHasContent(Bitmap bmp, int x, int yStart, int yEnd, int threshold)
    {
        for (int y = yStart; y <= yEnd; y += 2)
        {
            var p = bmp.GetPixel(x, y);
            if (p.R < threshold || p.G < threshold || p.B < threshold) return true;
        }
        return false;
    }

    private static Bitmap AddBorderAndShadow(Bitmap source)
    {
        int w = source.Width;
        int h = source.Height;

        // Canvas: image + space for shadow on right/bottom edges
        int canvasW = w + ShadowOffset + ShadowBlur;
        int canvasH = h + ShadowOffset + ShadowBlur;

        var result = new Bitmap(canvasW, canvasH, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(result);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.White);

        // Draw drop shadow: graduated semi-transparent rectangles offset behind the image
        for (int i = ShadowBlur; i >= 1; i--)
        {
            float t = (float)i / ShadowBlur;               // 1.0 → 0.0 as we get closer
            int alpha = (int)(ShadowMaxAlpha * (1f - t) * 255);
            if (alpha <= 0) continue;

            using var brush = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0));
            g.FillRectangle(brush,
                ShadowOffset + i,
                ShadowOffset + i,
                w - 1,
                h - 1);
        }

        // Draw the image
        g.DrawImage(source, 0, 0, w, h);

        // Draw 1px border
        using var borderPen = new Pen(Color.FromArgb(209, 213, 219), 1); // gray-300
        g.DrawRectangle(borderPen, 0, 0, w - 1, h - 1);

        return result;
    }

    private static Rectangle InflateClamp(Rectangle r, int padding, int maxW, int maxH)
    {
        int x = Math.Max(0, r.X - padding);
        int y = Math.Max(0, r.Y - padding);
        int right = Math.Min(maxW, r.Right + padding);
        int bottom = Math.Min(maxH, r.Bottom + padding);
        return new Rectangle(x, y, right - x, bottom - y);
    }
}

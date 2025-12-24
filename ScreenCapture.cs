using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Cropalicious
{
    public static class ScreenCapture
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmFlush();

        [DllImport("user32.dll")]
        private static extern bool ShowCursor(bool bShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private const int SRCCOPY = 0x00CC0020;

        public static void SaveScreenshot(Rectangle captureArea, string outputFolder)
        {
            // Hide cursor to prevent it from appearing in screenshot
            ShowCursor(false);

            // Ensure desktop composition is complete and overlay is hidden before copying
            try { DwmFlush(); } catch { /* ignore if unavailable */ }
            System.Threading.Thread.Sleep(10);

            // Use raw BitBlt instead of CopyFromScreen to avoid any GDI+ filtering
            IntPtr screenDC = GetDC(IntPtr.Zero);
            IntPtr memDC = CreateCompatibleDC(screenDC);
            IntPtr hBitmap = CreateCompatibleBitmap(screenDC, captureArea.Width, captureArea.Height);
            IntPtr oldBitmap = SelectObject(memDC, hBitmap);

            // Direct pixel copy with no filtering
            BitBlt(memDC, 0, 0, captureArea.Width, captureArea.Height,
                   screenDC, captureArea.X, captureArea.Y, SRCCOPY);

            // Convert to managed bitmap
            Bitmap bitmap = Image.FromHbitmap(hBitmap);

            // Cleanup GDI objects
            SelectObject(memDC, oldBitmap);
            DeleteObject(hBitmap);
            DeleteDC(memDC);
            ReleaseDC(IntPtr.Zero, screenDC);

            // Restore cursor immediately after capture
            ShowCursor(true);

            Directory.CreateDirectory(outputFolder);

            var fileName = GenerateFileName();
            var filePath = Path.Combine(outputFolder, fileName);

            bitmap.Save(filePath, ImageFormat.Png);
            bitmap.Dispose();
        }

        private static string GenerateFileName()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            return $"cropalicious_{timestamp}.png";
        }
    }
}
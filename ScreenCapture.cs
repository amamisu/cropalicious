using System;
using System.ComponentModel;
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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        private const int SRCCOPY = 0x00CC0020;

        public static void SaveScreenshot(Rectangle captureArea, string outputFolder)
        {
            IntPtr screenDC = IntPtr.Zero;
            IntPtr memDC = IntPtr.Zero;
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;
            Bitmap? bitmap = null;
            bool cursorHidden = false;

            try
            {
                ShowCursor(false);
                cursorHidden = true;

                try { DwmFlush(); } catch { }
                System.Threading.Thread.Sleep(10);

                screenDC = GetDC(IntPtr.Zero);
                if (screenDC == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get screen DC.");

                memDC = CreateCompatibleDC(screenDC);
                if (memDC == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create memory DC.");

                hBitmap = CreateCompatibleBitmap(screenDC, captureArea.Width, captureArea.Height);
                if (hBitmap == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create capture bitmap.");

                oldBitmap = SelectObject(memDC, hBitmap);
                if (oldBitmap == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to select capture bitmap.");

                if (!BitBlt(memDC, 0, 0, captureArea.Width, captureArea.Height,
                       screenDC, captureArea.X, captureArea.Y, SRCCOPY))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to copy screen pixels.");
                }

                bitmap = Image.FromHbitmap(hBitmap);

                Directory.CreateDirectory(outputFolder);

                var fileName = GenerateFileName();
                var filePath = Path.Combine(outputFolder, fileName);

                bitmap.Save(filePath, ImageFormat.Png);
            }
            finally
            {
                bitmap?.Dispose();

                if (oldBitmap != IntPtr.Zero && memDC != IntPtr.Zero)
                    SelectObject(memDC, oldBitmap);
                if (hBitmap != IntPtr.Zero)
                    DeleteObject(hBitmap);
                if (memDC != IntPtr.Zero)
                    DeleteDC(memDC);
                if (screenDC != IntPtr.Zero)
                    ReleaseDC(IntPtr.Zero, screenDC);

                if (cursorHidden)
                    ShowCursor(true);
            }
        }

        private static string GenerateFileName()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            return $"cropalicious_{timestamp}.png";
        }
    }
}

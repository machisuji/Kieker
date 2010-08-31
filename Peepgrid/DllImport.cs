using System.Runtime.InteropServices;
using System;
using System.Text;

namespace Kieker
{
    namespace DllImport
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct PSIZE
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DWM_THUMBNAIL_PROPERTIES
        {
            public int dwFlags;
            public Rect rcDestination;
            public Rect rcSource;
            public byte opacity;
            public bool fVisible;
            public bool fSourceClientAreaOnly;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            internal Rect(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public override string ToString()
            {
                return "[" + Left + ", " + Top + ", " + Right + ", " + Bottom + "]";
            }
        }

        public class Constants
        {
            public const int SW_HIDE = 0;
            public const int SW_SHOWNOACTIVATE = 4;
            public const int SW_SHOW = 5;
            public const int SW_MINIMIZE = 6;
            public const int SW_SHOWMINNOACTIVE = 7;
            public const int SW_RESTORE = 9;
            public const int SW_FORCEMINIMIZE = 11;

            public const int GWL_STYLE = -16;

            public const ulong WS_VISIBLE = 0x10000000L;
            public const ulong WS_BORDER = 0x00800000L;
            public const ulong TARGETWINDOW = WS_BORDER | WS_VISIBLE;

            public const int DWM_TNP_VISIBLE = 0x8;
            public const int DWM_TNP_OPACITY = 0x4;
            public const int DWM_TNP_RECTDESTINATION = 0x1;
            public const int DWM_TNP_RECTSOURCE = 0x2;
        }

        public class User32
        {
            [DllImport("user32.dll")]
            public static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            public static extern int SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern int BringWindowToTop(IntPtr hWnd);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern int GetWindowText(HandleRef hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll")]
            public static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern int GetWindowTextLength(HandleRef hWnd);

            public delegate bool EnumWindowsCallback(IntPtr hwnd, int lParam);

            [DllImport("user32.dll")]
            public static extern int EnumWindows(EnumWindowsCallback lpEnumFunc, int lParam);

            [DllImport("user32.dll")]
            public static extern void GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll")]
            public static extern ulong GetWindowLongA(IntPtr hWnd, int nIndex);
        }

        public class DwmApi
        {
            [DllImport("dwmapi.dll")]
            public static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);

            [DllImport("dwmapi.dll")]
            public static extern int DwmUnregisterThumbnail(IntPtr thumb);

            [DllImport("dwmapi.dll")]
            public static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out PSIZE size);

            [DllImport("dwmapi.dll")]
            public static extern int DwmUpdateThumbnailProperties(IntPtr hThumb, 
                ref DWM_THUMBNAIL_PROPERTIES props);
        }

        public class Kernel32
        {
            [DllImport("kernel32.dll")]
            public static extern int GetLastError();
        }
    }
}
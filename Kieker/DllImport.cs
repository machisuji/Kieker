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
            public const int SW_SHOWDEFAULT = 10;
            public const int SW_FORCEMINIMIZE = 11;

            public const int GWL_STYLE = -16;

            public const ulong WS_VISIBLE = 0x10000000L;
            public const ulong WS_BORDER = 0x00800000L;
            public const ulong WS_ICONIC = 0x20000000L;

            public const int DWM_TNP_VISIBLE = 0x8;
            public const int DWM_TNP_OPACITY = 0x4;
            public const int DWM_TNP_RECTDESTINATION = 0x1;
            public const int DWM_TNP_RECTSOURCE = 0x2;

            public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
            public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
            public static readonly IntPtr HWND_TOP = new IntPtr(0);
            public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

            public const uint SWP_ASYNCWINDOWPOS = 0x4000;
            public const uint SWP_DEFERERASE = 0x2000;
            public const uint SWP_DRAWFRAME = 0x0020;
            public const uint SWP_FRAMECHANGED = 0x0020;
            public const uint SWP_HIDEWINDOW = 0x0080;
            public const uint SWP_NOACTIVATE = 0x0010;
            public const uint SWP_NOCOPYBITS = 0x0100;
            public const uint SWP_NOMOVE = 0x0002;
            public const uint SWP_NOOWNERZORDER = 0x0200;
            public const uint SWP_NOREDRAW = 0x0008;
            public const uint SWP_NOREPOSITION = 0x0200;
            public const uint SWP_NOSENDCHANGING = 0x0400;
            public const uint SWP_NOSIZE = 0x0001;
            public const uint SWP_NOZORDER = 0x0004;
            public const uint SWP_SHOWWINDOW = 0x0040;
        }

        public class User32
        {
            [DllImport("user32.dll")]
            public static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter,
                int x, int y, int width, int height, uint uFlags);

            [DllImport("user32.dll")]
            public static extern bool IsIconic(IntPtr hwnd);

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
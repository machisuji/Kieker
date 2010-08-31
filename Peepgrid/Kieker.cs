using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Threading;

namespace Kieker
{
    public partial class ThumbView : Form
    {
        private const int SW_HIDE = 0;
        private const int SW_SHOWNOACTIVATE = 4;
        private const int SW_SHOW = 5;
        private const int SW_MINIMIZE = 6;
        private const int SW_SHOWMINNOACTIVE = 7;
        private const int SW_RESTORE = 9;
        private const int SW_FORCEMINIMIZE = 11;

        [DllImport("user32.dll")] 
        private static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int BringWindowToTop(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        public static extern int GetLastError();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(HandleRef hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(HandleRef hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);

        [DllImport("dwmapi.dll")]
        static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);

        [DllImport("dwmapi.dll")]
        static extern int DwmUnregisterThumbnail(IntPtr thumb);

        [DllImport("dwmapi.dll")]
        static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out PSIZE size);

        [DllImport("dwmapi.dll")]
        static extern int DwmUpdateThumbnailProperties(IntPtr hThumb, ref DWM_THUMBNAIL_PROPERTIES props);

        [DllImport("user32.dll")]
        static extern int EnumWindows(EnumWindowsCallback lpEnumFunc, int lParam);

        [DllImport("user32.dll")]
        public static extern void GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        static extern ulong GetWindowLongA(IntPtr hWnd, int nIndex);

        static readonly int GWL_STYLE = -16;

        static readonly ulong WS_VISIBLE = 0x10000000L;
        static readonly ulong WS_BORDER = 0x00800000L;
        static readonly ulong TARGETWINDOW = WS_BORDER | WS_VISIBLE;

        static readonly int DWM_TNP_VISIBLE = 0x8;
        static readonly int DWM_TNP_OPACITY = 0x4;
        static readonly int DWM_TNP_RECTDESTINATION = 0x1;
        static readonly int DWM_TNP_RECTSOURCE = 0x2;

        delegate bool EnumWindowsCallback(IntPtr hwnd, int lParam);

        [StructLayout(LayoutKind.Sequential)]
        internal struct PSIZE
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DWM_THUMBNAIL_PROPERTIES
        {
            public int dwFlags;
            public Rect rcDestination;
            public Rect rcSource;
            public byte opacity;
            public bool fVisible;
            public bool fSourceClientAreaOnly;
        }

        private string ToString(Rect rect)
        {
            return "[" + rect.Left + ", " + rect.Top + ", " + rect.Right + ", " + rect.Bottom + "]";
        }

        internal class Window
        {
            public string Title;
            public IntPtr Handle;
            public Thumb Thumb;
            public Rectangle Rect;

            public override string ToString()
            {
                return Title;
            }
        }

        internal class Thumb
        {
            public IntPtr Value;
            public Rectangle Destination;
            public Rectangle Rect;

            public Thumb(IntPtr value, Rectangle destination)
            {
                Value = value;
                Destination = destination;
            }

            public override string ToString()
            {
                return "Thumb " + Value.ToString() + " @" + Destination.ToString();
            }
        }

        private List<Window> windows;
        private List<Thumb> thumbs = new List<Thumb>();
        private RectNode thumbRects;
        private bool debug = false;
        private Shell32.ShellClass shell = new Shell32.ShellClass();

        public ThumbView()
        {
            InitializeComponent();

            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(this.Peepgrid_KeyDown);
            this.MouseClick += new MouseEventHandler(Peepgrid_MouseClick);
            this.Paint += new PaintEventHandler(Peepgrid_Paint);
            this.TopMost = true;
        }

        private Rectangle Area()
        {
            return System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
        }

        private string GetCurrentWallpaper()
        {
            RegistryKey rkWallPaper = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", false);
            string WallpaperPath = rkWallPaper.GetValue("WallPaper").ToString();
            rkWallPaper.Close();
            return WallpaperPath;
        }

        void Peepgrid_Paint(object sender, PaintEventArgs e)
        {
            if (debug)
            {
                Graphics g = this.CreateGraphics();
                foreach (Window window in windows)
                {
                    if (window.Thumb != null)
                    {
                        Rectangle rect = window.Thumb.Rect;
                        g.FillRectangle(new SolidBrush(Color.FromArgb(100, 100, 100, 100)), rect);
                    }
                }
            }
        }

        private int Byte(int value)
        {
            if (value < 0) return 0;
            else if (value > 255) return 255;
            else return value;
        }

        private void Peepgrid_MouseClick(object sender, MouseEventArgs e)
        {
            Window target = null;
            foreach (Window window in windows)
            {
                if (window.Thumb != null && window.Thumb.Rect.Contains(new Point(e.X, e.Y)))
                {
                    target = window;
                    break;
                }
            }
            Unaction();
            SetForegroundWindow(target.Handle);
            Hide();
            notifyIcon.ShowBalloonTip(2000, "Debug", "Activating " + target.Title +
                " @" + target.Thumb.Rect.ToString(), ToolTipIcon.None);
        }

        private void MoveLast(List<Window> windows, Window window)
        {
            windows.Remove(window);
            windows.Add(window);
        }

        private void MoveFirst(List<Window> windows, Window window)
        {
            windows.Remove(window);
            windows.Insert(0, window);
        }

        private string GetWindowText(IntPtr hwnd)
        {
            int capacity = GetWindowTextLength(new HandleRef(this, hwnd)) * 2;
            StringBuilder stringBuilder = new StringBuilder(capacity);
            GetWindowText(new HandleRef(this, hwnd), stringBuilder, stringBuilder.Capacity);

            return stringBuilder.ToString();
        }

        private bool contains(Rect rect, Point point)
        {
            return point.X >= rect.Left && point.X <= rect.Right &&
                point.Y >= rect.Top && point.Y <= rect.Bottom;
        }

        private void Exit()
        {
            ClearThumbnails();
            Application.Exit();
        }

        private void GetWindows()
        {
            windows = new List<Window>();
            EnumWindows(Callback, 0);
        }

        private bool Callback(IntPtr hwnd, int lParam)
        {
            if (this.Handle != hwnd && (GetWindowLongA(hwnd, GWL_STYLE) & TARGETWINDOW) == TARGETWINDOW)
            {
                StringBuilder sb = new StringBuilder(200);
                GetWindowText(hwnd, sb, sb.Capacity);
                Window t = new Window();
                t.Handle = hwnd;
                t.Title = sb.ToString();
                windows.Add(t);
            }

            return true; //continue enumeration
        }

        private void Peepgrid_Load(object sender, EventArgs e)
        {
            //this.BackgroundImage = Image.FromFile(GetCurrentWallpaper());
            //ShowThumbnailsAnimated();
            //shell.MinimizeAll();
            Action();
        }

        private void Peepgrid_Resize(object sender, EventArgs e)
        {
            UpdateThumbs();
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            GetWindows();
        }

        private void ClearThumbnails()
        {
            foreach (Thumb thumb in thumbs)
            {
                if (thumb.Value != IntPtr.Zero) DwmUnregisterThumbnail(thumb.Value);
            }
            thumbs.Clear();
        }

        private void ShowThumbnails()
        {
            GetWindows();
            ClearThumbnails();
            Rectangle image = Area();
            thumbRects = new RectNode(new Rect(image.Left, image.Top, 
                image.Right, image.Bottom).ToRectangle());
            List<Rect> destinations = CalculateThumbDestinations(new Rect(image.Left, image.Top, 
                image.Right, image.Bottom), windows.Count);
            List<Rect>.Enumerator de = destinations.GetEnumerator();
            de.MoveNext();
            foreach (Window w in windows)
            {
                IntPtr thumb = new IntPtr();
                int i = DwmRegisterThumbnail(this.Handle, w.Handle, out thumb);
                if (i == 0)
                {
                    Thumb t = new Thumb(thumb, de.Current.ToRectangle());
                    thumbs.Add(t);
                    w.Thumb = t;
                    UpdateThumb(t);
                    de.MoveNext();
                }
            }
        }

        private void ShowThumbnailsAnimated()
        {
            GetWindows();
            ClearThumbnails();
            Rectangle image = Area();
            List<Rect> destinations = CalculateThumbDestinations(new Rect(image.Left, image.Top,
                image.Right, image.Bottom), windows.Count);
            List<Rect>.Enumerator dest = destinations.GetEnumerator();
            foreach (Window window in windows)
            {
                if (dest.MoveNext())
                {
                    Rect source = new Rect();
                    IntPtr thumb = new IntPtr();
                    int i = DwmRegisterThumbnail(this.Handle, window.Handle, out thumb);
                    if (i == 0)
                    {
                        Thumb t = new Thumb(thumb, dest.Current.ToRectangle());
                        GetWindowRect(window.Handle, out source);
                        thumbs.Add(t);
                        window.Thumb = t;
                        window.Rect = source.ToRectangle();
                    }
                }
            }
            new Thread(new ThreadStart(DoMoveThumbs)).Start();
        }

        void DoMoveThumbs()
        {
            MoveThumbs(windows);
        }

        void DoMoveThumbsBack()
        {
            MoveThumbsBack(windows);
        }

        private void UpdateThumbs()
        {
            Rectangle image = Area();
            RecalculateThumbDestinations(new Rect(image.Left, image.Top, image.Right, image.Bottom));
            foreach (Thumb thumb in thumbs)
            {
                UpdateThumb(thumb);
            }
        }

        private List<Rect> CalculateThumbDestinations(Rect dest, int thumbCount)
        {
            List<Rect> rects = new List<Rect>(thumbCount);
            int margin = 5; // pixels
            int destWidth = dest.Right - dest.Left;
            int destHeight = dest.Bottom - dest.Top;
            int cols = (int)Math.Sqrt(thumbCount);
            int rows = cols;
            // if a square arrangement is not possible, make it wider
            if (cols * cols < thumbCount) cols += (thumbCount - cols * cols);
            int itemWidth = GetSubsegmentWidth(destWidth, cols, margin);
            int itemHeight = GetSubsegmentWidth(destHeight, rows, margin);
            for (int row = 0; row < rows; ++row)
            {
                for (int col = 0; col < cols; ++col)
                {
                    int top = dest.Top + row * (itemHeight + margin);
                    int left = dest.Left + col * (itemWidth + margin);
                    Rect rect = new Rect(left, top, left + itemWidth, top + itemHeight);
                    rects.Add(rect);
                }
            }
            return rects;
        }

        private void RecalculateThumbDestinations(Rect dest)
        {
            List<Rect> newDestinations = CalculateThumbDestinations(dest, thumbs.Count);
            List<Rect>.Enumerator de = newDestinations.GetEnumerator();
            de.MoveNext();
            foreach (Thumb thumb in thumbs)
            {
                thumb.Destination = de.Current.ToRectangle();
                de.MoveNext();
            }
        }

        private int GetSubsegmentWidth(int segment, int subsegments, int margin)
        {
            return subsegments <= 1 ? segment : (segment - (subsegments - 1) * margin) / subsegments;
        }

        private PSIZE GetSourceSize(IntPtr thumb)
        {
            PSIZE size;
            DwmQueryThumbnailSourceSize(thumb, out size);
            return size;
        }

        private void MoveThumbs(List<Window> windows)
        {
            MoveRects((from window in windows select window.Thumb).ToList(),
                (from window in windows select window.Rect).ToList(),
                (from window in windows select window.Thumb.Destination).ToList());
        }

        private void MoveThumbsBack(List<Window> windows)
        {
            MoveRects((from window in windows select window.Thumb).ToList(),
                (from window in windows select window.Thumb.Destination).ToList(),
                (from window in windows select window.Rect).ToList());
        }

        private void MoveRects(List<Thumb> thumbs, List<Rectangle> starts, List<Rectangle> ends)
        {
            if (thumbs.Count != starts.Count && starts.Count != ends.Count)
                throw new ArgumentException(
                    "There must be as many start rects as end rects and thumbs of course.");
            for (int n = 0; n <= 10; ++n)
            {
                double f = Math.Round(1d * n / 10d, 1);
                List<Thumb>.Enumerator thumb = thumbs.GetEnumerator();
                List<Rectangle>.Enumerator start = starts.GetEnumerator();
                List<Rectangle>.Enumerator end = ends.GetEnumerator();
                while (thumb.MoveNext() && start.MoveNext() && end.MoveNext())
                {
                    if (n < 10)
                    {
                        Rectangle intermediate = GetIntermediate(start.Current, end.Current, (float)f);
                        UpdateThumb(thumb.Current.Value, intermediate.ToRect());
                    }
                    else
                    {
                        thumb.Current.Destination = end.Current;
                        UpdateThumb(thumb.Current);
                    }
                }
                int ms = (n == 0) ? 100 : 50;
                System.Threading.Thread.Sleep(ms);
            }
        }

        private PSIZE UpdateThumb(IntPtr thumb, Rect dest)
        {
            if (thumb != IntPtr.Zero)
            {
                PSIZE size = GetSourceSize(thumb);
                DWM_THUMBNAIL_PROPERTIES props = new DWM_THUMBNAIL_PROPERTIES();
                props.dwFlags = DWM_TNP_VISIBLE | DWM_TNP_RECTDESTINATION;
                props.fVisible = true;
                props.rcDestination = new Rect(dest.Left, dest.Top, dest.Right, dest.Bottom);
                if (size.x < dest.Right - dest.Left)
                    props.rcDestination.Right = props.rcDestination.Left + size.x;
                if (size.y < dest.Bottom - dest.Top)
                    props.rcDestination.Bottom = props.rcDestination.Top + size.y;
                DwmUpdateThumbnailProperties(thumb, ref props);
                return size;
            }
            else
            {
                return new PSIZE();
            }
        }

        private void UpdateThumb(Thumb thumb)
        {
            PSIZE size = UpdateThumb(thumb.Value, thumb.Destination.ToRect());
            PSIZE thumbSize = GetThumbSize(size, thumb.Destination);
            thumb.Rect = new Rectangle(thumb.Destination.Left, thumb.Destination.Top, 
                thumbSize.x, thumbSize.y);
        }

        private PSIZE GetThumbSize(PSIZE sourceSize, Rectangle destination)
        {
            if (sourceSize.x < destination.Width && sourceSize.y < destination.Height)
            {
                return sourceSize;
            }
            else
            {
                PSIZE size = new PSIZE();
                float whq = sourceSize.x / (float)sourceSize.y; // width-height quotient
                int respectiveHeight = (int)(destination.Width / whq);
                int respectiveWidth = (int)(destination.Height * whq);
                if ((whq < 1 && respectiveHeight <= destination.Height) || 
                    respectiveWidth > destination.Width)
                {
                    size.x = destination.Width;
                    size.y = respectiveHeight;
                }
                else
                {
                    size.x = respectiveWidth;
                    size.y = destination.Height;
                }
                return size;
            }
        }

        /// <summary>
        /// Calculates the intermediate step between start and end.
        /// </summary>
        /// <param name="start">start rectangle</param>
        /// <param name="end">end rectangle</param>
        /// <param name="step">Step, that is a value between 0.0 (start) and 1.0 (end).</param>
        /// <returns>A new rectangle representing the intermediate step between start and end.</returns>
        private Rectangle GetIntermediate(Rectangle start, Rectangle end, float step)
        {
            Rectangle ret = new Rectangle();
            PointF posVector = new PointF(end.X - start.X, end.Y - start.Y);
            PointF sizeVector = new PointF(end.Width - start.Width, end.Height - start.Height);
            ret.X = start.X + (int)(posVector.X * step);
            ret.Y = start.Y + (int)(posVector.Y * step);
            ret.Width = start.Width + (int)(sizeVector.X * step);
            ret.Height = start.Height + (int)(sizeVector.Y * step);

            return ret;
        }

        private void opacity_Scroll(object sender, EventArgs e)
        {
            UpdateThumbs();
        }

        private void showAllButton_Click(object sender, EventArgs e)
        {
            ShowThumbnails();
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            ClearThumbnails();
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Exit();
        }

        void Peepgrid_KeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine(e.KeyCode.ToString());
            e.Handled = true;
            if (e.Control && e.KeyCode == Keys.Q)
            {
                Exit();
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                ClearThumbnails();
            }
            else if (e.Control && e.KeyCode == Keys.D)
            {
                debug = !debug;
                Invalidate();
            }
            else if (e.Control && e.KeyCode == Keys.S)
            {
                ShowThumbnails();
            }
        }

        private void testButton_Click(object sender, EventArgs e)
        {
            this.Hide();
            notifyIcon.ShowBalloonTip(1000, "Attention!", "Peepgrid has been minimized.", ToolTipIcon.Info);
        }

        private List<Rectangle> packRects(Rectangle enclosingRect, List<Rectangle> rects)
        {
            List<Rectangle> packedRects = new List<Rectangle>();
            rects.Sort(new RectSizeComparer());

            return packedRects;
        }

        internal class RectSizeComparer : IComparer<Rectangle> {

            private bool reverseOrder = true;

            public RectSizeComparer(bool reverseOrder)
            {
                this.reverseOrder = reverseOrder;
            }

            public RectSizeComparer() { }

            public int Compare(Rectangle a, Rectangle b)
            {
                int areaA = a.Width * a.Height;
                int areaB = b.Width * b.Height;
                return reverseOrder ? areaA - areaB : areaB - areaA;
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Action();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Exit();
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Action();
        }

        void Action()
        {
            ClearThumbnails();
            this.Show();
            ShowThumbnailsAnimated();
            shell.MinimizeAll();
        }

        void Unaction()
        {
            new Thread(new ThreadStart(DoMoveThumbsBack)).Start();
            shell.UndoMinimizeALL();
            System.Threading.Thread.Sleep(400);
            ClearThumbnails();
        }

        void ShowWindows(List<Window> windows, int cmd)
        {
            foreach (Window window in windows)
            {
                ShowWindow(window.Handle, cmd);
            }
        }
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
    }

    class RectNode
    {
        private Rectangle rect;
        private RectNode childA;
        private RectNode childB;
        private bool taken = false;

        public RectNode(Rectangle rect, RectNode childA, RectNode childB)
        {
            this.rect = rect;
            this.childA = childA;
            this.childB = childB;
        }

        public RectNode(Rectangle rect)
        {
            this.rect = rect;
        }

        public Rectangle GetRectangle()
        {
            return rect;
        }

        public RectNode GetChildA()
        {
            return childA;
        }

        public RectNode GetChildB()
        {
            return childB;
        }

        public bool IsTaken() { return taken; }

        public bool IsLeaf() { return childA == null && childB == null; }

        public void Normalize(Rectangle rect)
        {
            rect.X = 0;
            rect.Y = 0;
        }

        public bool Insert(Rectangle rect)
        {
            Normalize(rect);
            if (IsLeaf()) // insert rect here
            {
                if (!IsTaken() && this.rect.GetNormalized().Contains(rect)) // check if it fits
                {
                    int dw = this.rect.Width - rect.Width;
                    int dh = this.rect.Height - rect.Height;
                    if (dw > dh) // split horizontally
                    {
                        childA = new RectNode(new Rectangle(this.rect.X, this.rect.Y, 
                            rect.Width, this.rect.Height));
                        childB = new RectNode(new Rectangle(this.rect.X + rect.Width, this.rect.Y,
                            this.rect.Width - rect.Width, this.rect.Height));
                    }
                    else // split vertically
                    {
                        childA = new RectNode(new Rectangle(this.rect.X, this.rect.Y, 
                            this.rect.Width, rect.Height));
                        childB = new RectNode(new Rectangle(this.rect.X, this.rect.Y + rect.Height,
                            this.rect.Width, this.rect.Height - rect.Height));
                    }
                    childA.childA = new RectNode(new Rectangle(this.rect.X, this.rect.Y, 
                        rect.Width, rect.Height));
                    childA.childA.taken = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return childA.Insert(rect) || childB.Insert(rect);
            }
        }

        /// <summary>
        /// Collects all taken (inserted) rects in this tree.
        /// </summary>
        /// <returns></returns>
        public List<Rectangle> CollectRects()
        {
            if (IsTaken())
            {
                List<Rectangle> ret = new List<Rectangle>(1);
                ret.Add(rect);
                return ret;
            }
            else
            {
                List<Rectangle> ret = new List<Rectangle>();
                if (childA != null) ret.AddRange(childA.CollectRects());
                if (childB != null) ret.AddRange(childB.CollectRects());
                return ret;
            }
        }
    }

    public static class ExtensionsClass
    {
        public static string Max(this string s, int length)
        {
            return length <= s.Length ? s.Substring(0, length) : s;
        }

        /// <summary>
        /// Returns this String with a maximum length of <i>length</i> characters.
        /// </summary>
        /// <param name="s">this</param>
        /// <param name="length">max length</param>
        /// <param name="append">string to append if the string had to be truncated</param>
        /// <returns>The string with the given string to append in case it had to be truncated.</returns>
        public static string Max(this string s, int length, string append)
        {
            if (length <= s.Length)
            {
                return s.Substring(0, length) + (append != null ? append : "");
            }
            else
            {
                return s;
            }
        }

        public static Rectangle ToRectangle(this Rect rect)
        {
            return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        public static Rect ToRect(this Rectangle rect)
        {
            return new Rect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        public static Rectangle GetNormalized(this Rectangle rect)
        {
            return new Rectangle(0, 0, rect.Width, rect.Height);
        }
    }
}

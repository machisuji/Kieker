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
using Kieker.DllImport;

namespace Kieker
{
    public partial class ThumbView : Form
    {
        private List<Window> windows;
        private List<Thumb> thumbs = new List<Thumb>();
        private RectNode thumbRects;
        private bool debug = false;
        private Shell32.ShellClass shell = new Shell32.ShellClass();

        public ThumbView()
        {
            InitializeComponent();

            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(this.Kieker_KeyDown);
            this.MouseClick += new MouseEventHandler(Kieker_MouseClick);
            this.Paint += new PaintEventHandler(Kieker_Paint);
            this.TopMost = true;
        }

        public Rectangle Area
        {
            get { return System.Windows.Forms.Screen.PrimaryScreen.WorkingArea; }
        }

        private string GetCurrentWallpaper()
        {
            RegistryKey rkWallPaper = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", false);
            string WallpaperPath = rkWallPaper.GetValue("WallPaper").ToString();
            rkWallPaper.Close();
            return WallpaperPath;
        }

        void Kieker_Paint(object sender, PaintEventArgs e)
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

        private void Kieker_MouseClick(object sender, MouseEventArgs e)
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
            if (target != null)
            {
                Unaction();
                User32.SetForegroundWindow(target.Handle);
                Hide();
                notifyIcon.ShowBalloonTip(2000, "Debug", "Activating " + target.Title +
                    " @" + target.Thumb.Rect.ToString(), ToolTipIcon.None);
            }
        }

        private string GetWindowText(IntPtr hwnd)
        {
            int capacity = User32.GetWindowTextLength(new HandleRef(this, hwnd)) * 2;
            StringBuilder stringBuilder = new StringBuilder(capacity);
            User32.GetWindowText(new HandleRef(this, hwnd), stringBuilder, stringBuilder.Capacity);

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
            User32.EnumWindowsCallback callback = (hwnd, lParam) =>
            {
                if (this.Handle != hwnd && AcceptWindow(hwnd))
                {
                    StringBuilder sb = new StringBuilder(200);
                    User32.GetWindowText(hwnd, sb, sb.Capacity);
                    Window window = new Window();
                    window.Handle = hwnd;
                    window.Title = sb.ToString();
                    if (!SkipWindow(window))
                        windows.Add(window);
                }
                return true;
            };
            User32.EnumWindows(callback, 0);
        }

        private bool AcceptWindow(IntPtr hwnd)
        {
            ulong require = Constants.WS_BORDER | Constants.WS_VISIBLE;
            ulong refuse = Constants.WS_ICONIC;
            return //!User32.IsIconic(hwnd) &&
                (User32.GetWindowLongA(hwnd, Constants.GWL_STYLE) & require) == require &&
                (User32.GetWindowLongA(hwnd, Constants.GWL_STYLE) & refuse) != refuse;
        }

        private bool SkipWindow(Window window)
        {
            return window.Title.Equals("AMD:CCC-AEMCapturingWindow");
        }

        private void Kieker_Load(object sender, EventArgs e)
        {
            //this.BackgroundImage = Image.FromFile(GetCurrentWallpaper());
            //ShowThumbnailsAnimated();
            //shell.MinimizeAll();
            Action();
        }

        private void ClearThumbnails()
        {
            foreach (Thumb thumb in thumbs)
            {
                if (thumb.Value != IntPtr.Zero) DwmApi.DwmUnregisterThumbnail(thumb.Value);
            }
            thumbs.Clear();
        }

        private void ShowThumbnails()
        {
            GetWindows();
            ClearThumbnails();
            thumbRects = new RectNode(new Rect(Area.Left, Area.Top, 
                Area.Right, Area.Bottom).ToRectangle());
            List<Rect> destinations = CalculateThumbDestinations(new Rect(Area.Left, Area.Top, 
                Area.Right, Area.Bottom), windows.Count);
            List<Rect>.Enumerator de = destinations.GetEnumerator();
            de.MoveNext();
            foreach (Window w in windows)
            {
                IntPtr thumb = new IntPtr();
                int i = DwmApi.DwmRegisterThumbnail(this.Handle, w.Handle, out thumb);
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
            List<Rect> destinations = CalculateThumbDestinations(new Rect(Area.Left, Area.Top,
                Area.Right, Area.Bottom), windows.Count);
            List<Rect>.Enumerator dest = destinations.GetEnumerator();
            foreach (Window window in windows)
            {
                if (dest.MoveNext())
                {
                    Rect source = new Rect();
                    IntPtr thumb = new IntPtr();
                    int i = DwmApi.DwmRegisterThumbnail(this.Handle, window.Handle, out thumb);
                    if (i == 0)
                    {
                        Thumb t = new Thumb(thumb, dest.Current.ToRectangle());
                        User32.GetWindowRect(window.Handle, out source);
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
            RecalculateThumbDestinations(new Rect(Area.Left, Area.Top, Area.Right, Area.Bottom));
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
            DwmApi.DwmQueryThumbnailSourceSize(thumb, out size);
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
                props.dwFlags = Constants.DWM_TNP_VISIBLE | Constants.DWM_TNP_RECTDESTINATION;
                props.fVisible = true;
                props.rcDestination = new Rect(dest.Left, dest.Top, dest.Right, dest.Bottom);
                if (size.x < dest.Right - dest.Left)
                    props.rcDestination.Right = props.rcDestination.Left + size.x;
                if (size.y < dest.Bottom - dest.Top)
                    props.rcDestination.Bottom = props.rcDestination.Top + size.y;
                DwmApi.DwmUpdateThumbnailProperties(thumb, ref props);
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

        void Kieker_KeyDown(object sender, KeyEventArgs e)
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
                User32.ShowWindow(window.Handle, cmd);
            }
        }
    }
}

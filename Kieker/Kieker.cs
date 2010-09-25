﻿using System;
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
using Gma.UserActivityMonitor;

namespace Kieker
{
    public partial class ThumbView : Form
    {
        private Settings settings;

        private List<Window> windows = new List<Window>();
        private RectNode thumbRects;
        private bool debug = false;
        private Shell32.ShellClass shell = new Shell32.ShellClass();
        private bool modifier = false;
        private bool key = false;
        private bool action = false;
        private bool unaction = false;
        private readonly Object animationLock = new Object();
        private bool pauseAnimation = false;
        private delegate void VoidDelegate();
        private delegate void ArgRectangle(Rectangle rect);
        private IntPtr windowHandle;
        private RectPainter rectPainter;

        public ThumbView()
        {
            InitializeComponent();

            this.rectPainter = new RectPainter(this);
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(this.Kieker_KeyDown);
            this.MouseClick += new MouseEventHandler(Kieker_MouseClick);
            this.Paint += new PaintEventHandler(Kieker_Paint);

            rectPainter.Enabled = false;
        }

        private void Kieker_Load(object sender, EventArgs e)
        {
            this.settings = new Settings();
            this.windowHandle = this.Handle;
            HookManager.KeyDown += new KeyEventHandler(HookManager_KeyDown);
            HookManager.KeyUp += new KeyEventHandler(HookManager_KeyUp);
            Action();
        }

        void HookManager_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LWin) modifier = false;
            else if (e.KeyCode == Keys.Oem5) key = false;
        }

        void HookManager_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LWin) modifier = true;
            else if (e.KeyCode == Keys.Oem5) key = true;
            if (!Visible && !action && modifier && key)
            {
                e.Handled = true;
                Action();
            }
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
            rectPainter.Paint(sender, e);
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
            if (target != null && !unaction) Unaction(target);
        }

        void Kieker_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (e.Control && e.KeyCode == Keys.Q)
            {
                Exit();
            }
            else if (e.Control && e.KeyCode == Keys.A)
            {
                Action();
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
            else if (e.KeyCode == Keys.Space)
            {
                if (!pauseAnimation) pauseAnimation = true;
                else
                {
                    lock (animationLock)
                    {
                        Monitor.Pulse(animationLock);
                    }
                }
            }
            else if (e.Control && e.KeyCode == Keys.R)
            {
                rectPainter.Enabled = !rectPainter.Enabled;
                if (rectPainter.Enabled)
                {
                    GetSomeRects();
                    rectPainter.AnimateRects();
                }
                else
                {
                    rectPainter.HideRects();
                }
            }
            else if (e.Control && e.KeyCode == Keys.H)
            {
                Hide();
            }
        }

        protected void AssignThumbnails(IEnumerable<Window> windows)
        {
            IEnumerable<Window> sortedWindows = windows.OrderBy(w => w.Rect.Area()).Reverse();
            double factor = 1d;
            bool allFit = false;
            while (!allFit)
            {
                allFit = true;
                RectNode tree = new RectNode(Area.GetExpanded(-30, -30));
                if (factor <= 0.1)
                {
                    // @TODO fall back to grid arrangement
                    break;
                }
                foreach (Window window in sortedWindows)
                {
                    Rectangle scaledRect = window.Rect.GetScaled(factor);
                    allFit &= tree.InsertAndUpdate(ref scaledRect);
                    if (allFit) window.Thumb.Destination = scaledRect.GetExpanded(-10, -10);
                    else break;
                }
                factor -= 0.1;
            }
            Console.WriteLine("Factor: " + (factor + 0.1));
            foreach (Window window in windows)
            {
                int i = DwmApi.DwmRegisterThumbnail(this.windowHandle, window.Handle, out window.Thumb.Value);
                if (i != 0)
                {
                    // Thumbnail konnte nicht registriert werden, bisher ignorieren wir das einfach ...
                }
            }
        }


        protected void Scatter(IEnumerable<Rectangle> rects, Rectangle bounds)
        {
            
        }

        private void GetSomeRects()
        {
            List<Rectangle> someRects = new List<Rectangle>();
            if (windows.Count == 0) GetWindows();
            List<Rectangle> rects = windows.Select(w => w.Rect).OrderBy(r => r.Area()).Reverse().ToList();
            double factor = 1d;
            while (someRects.Count < rects.Count)
            {
                RectNode tree = new RectNode(Area);
                someRects.Clear();
                if (factor <= 0.1)
                {
                    Console.WriteLine("That's impossible!");
                    break;
                }
                bool ok = true;
                foreach (Rectangle rect in rects)
                {
                    Rectangle scaledRect = rect.GetScaled(factor);
                    ok &= tree.Insert(scaledRect);
                    if (!ok) break;
                }
                if (ok)
                    someRects.AddRange(tree.GetStructure());
                factor -= 0.1;
            }
            rectPainter.Rects.Clear();
            rectPainter.Rects.AddRange(someRects);
            Console.WriteLine("Factor: " + factor);
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
            settings.Show();
        }

        void Action()
        {
            Action theAction = () =>
            {
                action = true;
                IntPtr hforegroundWindow = User32.GetForegroundWindow();
                ClearThumbnails();
                Invoke(new VoidDelegate(Show));
                ShowThumbnailsAnimated(hforegroundWindow);
                HideWindows();
                action = false;
            };
            theAction.Fork();
        }

        void Unaction(Window target)
        {
            Action theAction = () => {
                unaction = true;
                SetForegroundThumb(target);
                DoMoveThumbsBack();
                UnhideWindows();
                User32.SetForegroundWindow(target.Handle);
                System.Threading.Thread.Sleep(100);
                Invoke(new VoidDelegate(Hide));
                ClearThumbnails();
                unaction = false;
            };
            theAction.Fork();
        }

        private void HideWindows()
        {
            foreach (Window window in windows)
            {
                Rect bounds = new Rect();
                User32.GetWindowRect(window.Handle, out bounds);
                Console.WriteLine(bounds.ToString() + " <- " + window.Title);
                window.LastPosition = new Nullable<Point>(new Point(bounds.Left, bounds.Top));
                User32.SetWindowPos(window.Handle, Constants.HWND_BOTTOM, -16000, -16000, -1, -1,
                    Constants.SWP_NOZORDER | Constants.SWP_NOSIZE | Constants.SWP_NOREDRAW |
                    Constants.SWP_NOSENDCHANGING | Constants.SWP_NOACTIVATE);
            }
        }

        private void UnhideWindows()
        {
            foreach (Window window in windows)
            {
                if (window.LastPosition.HasValue)
                {
                    Point pos = window.LastPosition.Value;
                    User32.SetWindowPos(window.Handle, Constants.HWND_BOTTOM, pos.X, pos.Y, 
                        -1, -1, Constants.SWP_NOSIZE);
                    window.LastPosition = new Nullable<Point>();
                }
            }
        }

        private void GetWindows()
        {
            windows = new List<Window>();
            User32.EnumWindowsCallback callback = (hwnd, lParam) =>
            {
                if (windowHandle != hwnd && AcceptWindow(hwnd))
                {
                    StringBuilder sb = new StringBuilder(200);
                    User32.GetWindowText(hwnd, sb, sb.Capacity);
                    Rect source = new Rect();
                    Window window = new Window();
                    window.Handle = hwnd;
                    window.Title = sb.ToString();
                    User32.GetWindowRect(window.Handle, out source);
                    window.Rect = source.ToRectangle();
                    if (!SkipWindow(window))
                    {
                        windows.Add(window);
                        Console.WriteLine("Added Window '" + window.Title + "'");
                    }
                }
                return true;
            };
            Console.WriteLine();
            User32.EnumWindows(callback, 0);
            windows.Reverse();
        }

        private bool AcceptWindow(IntPtr hwnd)
        {
            bool ok = true;
            ulong wl = User32.GetWindowLong(hwnd, Constants.GWL_STYLE);
            ulong required = Constants.WS_BORDER;
            List<ulong> accept = new List<ulong>();

            accept.Add(Constants.WS_VISIBLE);
            ok &= settings.IncludeMinimizedWindows || !User32.IsIconic(hwnd);

            return ok && (wl & required) == required && 
                (accept.Count == 0 || accept.Any((op) => (wl & op) == op));
        }

        private bool SkipWindow(Window window)
        {
            return window.Title.Equals("AMD:CCC-AEMCapturingWindow");
        }

        private void ClearThumbnails()
        {
            foreach (Thumb thumb in windows.Select(w => w.Thumb))
            {
                if (thumb.Value != IntPtr.Zero) DwmApi.DwmUnregisterThumbnail(thumb.Value);
            }
        }

        private void Exit()
        {
            ClearThumbnails();
            settings.Dispose();
            settings = null;
            Application.Exit();
        }

        public Rectangle Area
        {
            get { return System.Windows.Forms.Screen.PrimaryScreen.WorkingArea; }
        }

        private void SortByZOrder(List<Window> windows)
        {
            List<Window> unsorted = new List<Window>(windows);
            List<Window> sorted = new List<Window>(windows.Count);
            IntPtr hwnd = User32.GetTopWindow(IntPtr.Zero);
            while (hwnd != null && !hwnd.Equals(IntPtr.Zero))
            {
                List<Window>.Enumerator window = unsorted.GetEnumerator();
                while (window.MoveNext())
                {
                    if (hwnd.Equals(window.Current.Handle))
                    {
                        sorted.Add(window.Current);
                        unsorted.Remove(window.Current);
                        break;
                    }
                }
                hwnd = User32.GetWindow(hwnd, Constants.GW_HWNDNEXT);
            }
            sorted.AddRange(unsorted); // in case we missed something
            Console.WriteLine("Windows ordered by z-order beginning with the topmost one: ");
            foreach (Window window in sorted)
            {
                Console.WriteLine(window.Title);
            }
        }

        private string GetCurrentWallpaper()
        {
            RegistryKey rkWallPaper = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", false);
            string WallpaperPath = rkWallPaper.GetValue("WallPaper").ToString();
            rkWallPaper.Close();
            return WallpaperPath;
        }

        private int Byte(int value)
        {
            if (value < 0) return 0;
            else if (value > 255) return 255;
            else return value;
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

        protected bool MakeCompletelyGlassy(IntPtr hwnd)
        {
            bool enabled = false;
            DwmApi.DwmIsCompositionEnabled(out enabled);
            if (enabled)
            {
                MARGINS margins = new MARGINS(-1, -1, -1, -1);
                int hr = DwmApi.DwmExtendFrameIntoClientArea(hwnd, ref margins);
                return hr == 0;
            }
            return false;
        }


        protected void MakeGlassy()
        {

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
                int i = DwmApi.DwmRegisterThumbnail(windowHandle, w.Handle, out thumb);
                if (i == 0)
                {
                    Thumb t = new Thumb(thumb, de.Current.ToRectangle());
                    w.Thumb = t;
                    UpdateThumb(t);
                    de.MoveNext();
                }
            }
        }

        private void SetForegroundThumb(Window window)
        {
            IntPtr handle = window.Thumb.Value;
            DwmApi.DwmUnregisterThumbnail(handle);
            int ret = DwmApi.DwmRegisterThumbnail(windowHandle, window.Handle, out handle);
            if (ret == 0)
            {
                window.Thumb.Value = handle;
                UpdateThumb(handle, window.Thumb.Destination.ToRect());
            }
        }

        private void ShowThumbnailsAnimated(IntPtr hforegroundWindow)
        {
            GetWindows();
            ClearThumbnails();
            List<Rect> destinations = CalculateThumbDestinations(new Rect(Area.Left, Area.Top,
                Area.Right, Area.Bottom), windows.Count);
            Window previousForegroundWindow = null;
            foreach (Window window in windows)
            {
                if (window.Handle.Equals(hforegroundWindow))
                    previousForegroundWindow = window;
            }
            AssignThumbnails(windows);
            if (previousForegroundWindow != null)
            {
                SetForegroundThumb(previousForegroundWindow);
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
            
            int steps = 25;
            for (int n = 0; n <= steps; ++n)
            {
                if (pauseAnimation)
                {
                    lock (animationLock)
                    {
                        Monitor.Wait(animationLock, 30000);
                        pauseAnimation = false;
                    }
                }
                double f = (1d * n / (1d * steps)).EaseInOut(3);
                List<Thumb>.Enumerator thumb = thumbs.GetEnumerator();
                List<Rectangle>.Enumerator start = starts.GetEnumerator();
                List<Rectangle>.Enumerator end = ends.GetEnumerator();
                while (thumb.MoveNext() && start.MoveNext() && end.MoveNext())
                {
                    if (n < steps)
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
                int ms = (n == 0) ? 100 : 20;
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

        void ShowWindows(List<Window> windows, int cmd)
        {
            foreach (Window window in windows)
            {
                User32.ShowWindow(window.Handle, cmd);
            }
        }
    }
}

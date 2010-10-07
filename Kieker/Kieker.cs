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
using Gma.UserActivityMonitor;

namespace Kieker
{
    public partial class ThumbView : Form
    {
        private Settings settings;

        private List<Window> windows = new List<Window>();
        private RectNode thumbRects;
        private bool debug = false;
        private bool modifier = false;
        private bool key = false;
        private bool action = false;
        private bool unaction = false;
        private bool hotkeyEnabled = true;
        /// <summary>
        /// The selection mode is active when the initial thumb animation has finished
        /// and the thumbs are in their final position, waiting for the user's selection.
        /// </summary>
        private bool selectionActive = false;
        private readonly Object animationLock = new Object();
        private bool pauseAnimation = false;
        private delegate void VoidDelegate();
        private delegate void ArgRectangle(Rectangle rect);
        private IntPtr windowHandle;
        private RectPainter rectPainter;

        private Rectangle? highlightRect = new Nullable<Rectangle>();
        private Window focusedWindow;

        private KeyEventHandler globalKeyUp;
        private KeyEventHandler globalKeyDown;

        private bool dwmEnabled = false;

        public ThumbView()
        {
            this.globalKeyUp = new KeyEventHandler(HookManager_Dummy);
            this.globalKeyDown = new KeyEventHandler(HookManager_Dummy);

            InitializeComponent();

            this.rectPainter = new RectPainter(this);
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(this.Kieker_KeyDown);
            this.MouseClick += new MouseEventHandler(Kieker_MouseClick);
            this.Paint += new PaintEventHandler(Kieker_Paint);
            this.MouseMove += new MouseEventHandler(ThumbView_MouseMove);

            rectPainter.Enabled = false;
            DwmApi.DwmIsCompositionEnabled(out dwmEnabled);
        }

        protected override void OnLoad(EventArgs e)
        {
            this.Visible = false;
            base.OnLoad(e);
        }

        private void Kieker_Load(object sender, EventArgs e)
        {
            this.settings = new Settings(this);
            this.windowHandle = this.Handle;
            HookManager.KeyDown += new KeyEventHandler(HookManager_KeyDown);
            HookManager.KeyUp += new KeyEventHandler(HookManager_KeyUp);

            settings.LoadSettings();
        }

        void ThumbView_MouseMove(object sender, MouseEventArgs e)
        {
            if (selectionActive)
            {
                bool none = true;
                foreach (Window window in windows)
                {
                    if (window.Thumb.Rect.Contains(e.Location))
                    {
                        if (focusedWindow != window)
                        {
                            highlightRect = new Nullable<Rectangle>(window.Thumb.Rect.GetExpanded(5, 5));
                            focusedWindow = window;
                            Invalidate();
                        }
                        none = false;
                    }
                }
                if (none)
                {
                    focusedWindow = null;
                    highlightRect = new Nullable<Rectangle>();
                    Invalidate();
                }
            }
        }

        void HookManager_KeyUp(object sender, KeyEventArgs e)
        {
            globalKeyUp.Invoke(sender, e);
            if (e.KeyCode == settings.Modifier) modifier = false;
            else if (e.KeyCode == settings.Hotkey) key = false;
        }

        protected void HookManager_Dummy(object sender, KeyEventArgs e)
        {
            // do nuttin
        }

        void HookManager_KeyDown(object sender, KeyEventArgs e)
        {
            globalKeyDown.Invoke(sender, e);
            if (e.KeyCode == settings.Modifier) modifier = true;
            else if (e.KeyCode == settings.Hotkey) key = true;
            if (hotkeyEnabled && !Visible && !action && modifier && key)
            {
                e.Handled = true;
                Action();
            }
        }

        void Kieker_Paint(object sender, PaintEventArgs e)
        {
            if (debug)
            {
                Graphics g = e.Graphics;
                foreach (Window window in windows)
                {
                    if (window.Thumb != null)
                    {
                        Rectangle rect = window.Thumb.Rect;
                        g.DrawRectangle(new Pen(Color.Red), rect.GetExpanded(-1, -1));
                    }
                }
            }
            if (selectionActive)
            {
                if (settings.IndicateMinimizedwindows)
                {
                    foreach (Window window in windows.FindAll(w => w.IsIconic()))
                    {
                        if (window.Thumb != null)
                        {
                            Rectangle rect = window.Thumb.Rect;
                            e.Graphics.FillRectangle(new SolidBrush(Color.Gray), rect);
                        }
                    }
                }
                if (highlightRect.HasValue)
                {
                    e.Graphics.DrawRectangle(new Pen(SystemColors.ControlLight, 2), highlightRect.Value);
                }
            }
            rectPainter.Paint(sender, e);
            if (!dwmEnabled)
            {
                Graphics g = e.Graphics;
                g.DrawRectangle(new Pen(Color.Red), Area.GetExpanded(-20, -20));
                foreach (Window window in windows)
                {
                    Thumb thumb = window.Thumb;
                    if (thumb.Handle.Equals(IntPtr.Zero) && window.Rect.HasValue)
                    {
                        if (thumb.Rect == null)
                        {
                            g.FillEllipse(new SolidBrush(Color.Blue), new Rectangle(100, 100, 320, 240));
                        }
                        else
                        {
                            if (window.StaticThumb != null)
                                g.DrawImage(window.StaticThumb, thumb.Rect);
                            else
                                g.FillRectangle(new SolidBrush(Color.Red), thumb.Rect);
                        }
                    }
                }
            }
        }

        private void Kieker_MouseClick(object sender, MouseEventArgs e)
        {
            Window target = null;
            foreach (Window window in windows)
            {
                if (window.Thumb != null && window.Thumb.Rect.Contains(new Point(e.X, e.Y)))
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        target = window;
                    }
                    else
                    {

                    }
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
            else if (e.Control && e.KeyCode == Keys.U)
            {
                UnhideWindows();
            }
            else if (e.Control && e.KeyCode == Keys.I)
            {
                Invalidate();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Unaction(null);
            }
        }

        protected void AssignThumbnails(IEnumerable<Window> windows)
        {
            IEnumerable<Window> sortedWindows = windows.OrderBy(w => 
                w.GetBounds().Area()).Reverse();
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
                    Rectangle scaledRect = window.GetBounds().GetScaled(factor);
                    allFit &= tree.InsertAndUpdate(ref scaledRect);
                    if (allFit) window.Thumb.Destination = scaledRect.GetExpanded(-10, -10);
                    else break;
                }
                factor -= 0.1;
            }
            //Console.WriteLine("Factor: " + (factor + 0.1));
            foreach (Window window in windows)
            {
                if (dwmEnabled)
                {
                    int i = DwmApi.DwmRegisterThumbnail(this.windowHandle, window.Handle,
                        out window.Thumb.Handle);
                    if (i != 0)
                    {
                        Console.WriteLine("Could not register Thumbnail for '" + window.Title + "'");
                    }
                }
                else
                {
                    window.GetStaticThumb(true);
                }
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
                Invoke(new VoidDelegate(Activate));
                //CoverMinimizedWindows(windows.FindAll(w => User32.IsIconic(w.Handle)));
                ShowThumbnailsAnimated(hforegroundWindow);
                HideWindows(windows);
                action = false;
            };
            theAction.Fork();
        }

        void Unaction(Window target)
        {
            Action theAction = () => {
                selectionActive = false;
                Invalidate();
                unaction = true;
                if (target != null)
                {
                    if (dwmEnabled)
                        SetForegroundThumb(target);
                    else
                        windows.MoveToEnd(target);
                    if (target.IsIconic())
                    {
                        User32.ShowWindow(target.Handle, Constants.SW_RESTORE);
                    }
                }
                MoveThumbs(windows, false, target);
                UnhideWindows();
                if (target != null)
                    User32.SetForegroundWindow(target.Handle);
                Invoke(new VoidDelegate(Hide));
                ClearThumbnails();
                unaction = false;
            };
            theAction.Fork();
        }

        private void HideWindows(IEnumerable<Window> windows)
        {
            foreach (Window window in windows)
            {
                if (!window.IsIconic())
                {
                    Rectangle bounds = window.GetRect(true).Value;
                    window.LastPosition = new Nullable<Point>(new Point(bounds.Left, bounds.Top));
                    User32.SetWindowPos(window.Handle, Constants.HWND_BOTTOM, -16019, -16087, -1, -1,
                        Constants.SWP_NOZORDER | Constants.SWP_NOSIZE | /*Constants.SWP_NOREDRAW |*/
                        Constants.SWP_NOSENDCHANGING | Constants.SWP_NOACTIVATE);
                }
            }
        }

        private void UnhideWindows()
        {
            foreach (Window window in windows)
            {
                if (window.LastPosition.HasValue) // normal window
                {
                    Point pos = window.LastPosition.Value;
                    User32.SetWindowPos(window.Handle, Constants.HWND_BOTTOM, pos.X, pos.Y, 
                        -1, -1, Constants.SWP_NOSIZE | Constants.SWP_NOACTIVATE | Constants.SWP_NOZORDER |
                        Constants.SWP_NOSENDCHANGING | Constants.SWP_NOREDRAW);
                    window.LastPosition = new Nullable<Point>();
                }
            }
        }

        private void GetWindows()
        {
            windows.Clear();
            User32.EnumWindowsCallback callback = (hwnd, lParam) =>
            {
                if (windowHandle != hwnd && AcceptWindow(hwnd))
                {
                    Window window = new Window(hwnd);
                    if (!SkipWindow(window))
                    {
                        windows.Add(window);
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
            return window.GetTitle(true).Equals("AMD:CCC-AEMCapturingWindow");
        }

        private void ClearThumbnails()
        {
            foreach (Thumb thumb in windows.Select(w => w.Thumb))
            {
                if (thumb.Handle != IntPtr.Zero) DwmApi.DwmUnregisterThumbnail(thumb.Handle);
            }
        }

        private void Exit()
        {
            settings.SaveSettings();
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

        private bool contains(RECT rect, Point point)
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
            thumbRects = new RectNode(new RECT(Area.Left, Area.Top, 
                Area.Right, Area.Bottom).ToRectangle());
            List<RECT> destinations = CalculateThumbDestinations(new RECT(Area.Left, Area.Top, 
                Area.Right, Area.Bottom), windows.Count);
            List<RECT>.Enumerator de = destinations.GetEnumerator();
            de.MoveNext();
            foreach (Window w in windows)
            {
                IntPtr thumb = new IntPtr();
                int i = DwmApi.DwmRegisterThumbnail(windowHandle, w.Handle, out thumb);
                if (i == 0)
                {
                    Thumb t = new Thumb(thumb, de.Current.ToRectangle());
                    w.Thumb = t;
                    w.Thumb.Update();
                    de.MoveNext();
                }
            }
        }

        private void SetForegroundThumb(Window window)
        {
            IntPtr handle = window.Thumb.Handle;
            DwmApi.DwmUnregisterThumbnail(handle);
            int ret = DwmApi.DwmRegisterThumbnail(windowHandle, window.Handle, out handle);
            if (ret == 0)
            {
                window.Thumb.Handle = handle;
                window.Thumb.Update();
            }
        }

        private void ShowThumbnailsAnimated(IntPtr hforegroundWindow)
        {
            GetWindows();
            ClearThumbnails();
            List<RECT> destinations = CalculateThumbDestinations(new RECT(Area.Left, Area.Top,
                Area.Right, Area.Bottom), windows.Count);
            Window previousForegroundWindow = windows.Find(w => w.Handle.Equals(hforegroundWindow));
            AssignThumbnails(windows);
            if (previousForegroundWindow != null && dwmEnabled)
            {
                SetForegroundThumb(previousForegroundWindow);
            }
            Action animation = () =>
            {
                MoveThumbs(windows, true, null);
                selectionActive = true;
                Invalidate();
            };
            animation.Fork();
        }

        private List<RECT> CalculateThumbDestinations(RECT dest, int thumbCount)
        {
            List<RECT> rects = new List<RECT>(thumbCount);
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
                    RECT rect = new RECT(left, top, left + itemWidth, top + itemHeight);
                    rects.Add(rect);
                }
            }
            return rects;
        }

        private int GetSubsegmentWidth(int segment, int subsegments, int margin)
        {
            return subsegments <= 1 ? segment : (segment - (subsegments - 1) * margin) / subsegments;
        }

        private void MoveThumbs(List<Window> windows, bool forth, Window target)
        {
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
                foreach (Window window in windows)
                {
                    Thumb thumb = window.Thumb;
                    Rectangle start = forth ? window.GetAnimationBounds() : thumb.Destination;
                    Rectangle end = forth ?
                        thumb.Destination :
                        (window != target ? window.GetAnimationBounds() : window.GetBounds());
                    if (n < steps)
                    {
                        Rectangle intermediate = GetIntermediate(start, end, (float)f);
                        if (dwmEnabled)
                            thumb.Update(intermediate);
                        else
                            thumb.Rect = intermediate; // we're gonna paint it ourselves
                    }
                    else
                    {
                        if (dwmEnabled)
                        {
                            thumb.Destination = end;
                            thumb.Update();
                        }
                        else
                        {
                            thumb.Rect = end;
                        }
                    }
                }
                if (!dwmEnabled)
                    Invoke(new VoidDelegate(Invalidate));
                int ms = (n == 0) ? 100 : 15;
                System.Threading.Thread.Sleep(ms);
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

        public bool HotkeyEnabled
        {
            get { return hotkeyEnabled; }
            set { this.hotkeyEnabled = value; }
        }

        public KeyEventHandler GlobalKeyUp
        {
            get { return globalKeyUp; }
            set { globalKeyUp = value; }
        }

        public KeyEventHandler GlobalKeyDown
        {
            get { return globalKeyDown; }
            set { globalKeyDown = value; }
        }
    }
}

﻿using System;
using System.Drawing;
using System.Collections.Generic;
using Kieker.DllImport;
using System.Text;

namespace Kieker
{
    public delegate void VoidDelegate();
    public delegate void ArgRectangle(Rectangle rect);

    public class Thumb
    {
        /// <summary>
        /// Handle to the DWM Thumb
        /// </summary>
        public IntPtr Handle;

        /// <summary>
        /// Destination rect
        /// </summary>
        public Rectangle Destination;

        /// <summary>
        /// Actual rect within the destination rect, which may be smaller due to
        /// the aspect ratio of the window.
        /// </summary>
        public Rectangle Rect;

        public Thumb(IntPtr handle, Rectangle destination)
        {
            Handle = handle;
            Destination = destination;
        }

        public void Unregister()
        {
            if (Handle != IntPtr.Zero) DwmApi.DwmUnregisterThumbnail(Handle);
        }

        /// <summary>
        /// Grows the thumb without actually changing the destination.
        /// Only the current thumb view will grow.
        /// Upon the next call of #Update() it will resume its original size.
        /// </summary>
        /// <param name="percent"></param>
        public void GrowBy(double percent)
        {
            int width = Destination.Width;
            int height = Destination.Height;
            Update(Destination.GetExpanded((int)(width * percent), (int)(height * percent)));
        }

        /// <summary>
        /// Shows the thumb at its current destination and updates the thumb's rect field accordingly.
        /// </summary>
        public void Update()
        {
            UpdateThumb(this);
        }

        /// <summary>
        /// Shows the thumb at the given rectangle without changing the thumb's rect field.
        /// </summary>
        /// <param name="dest"></param>
        public void Update(Rectangle dest)
        {
            UpdateThumb(this.Handle, dest.ToRect());
        }

        private void UpdateThumb(Thumb thumb)
        {
            PSIZE size = UpdateThumb(thumb.Handle, thumb.Destination.ToRect());
            PSIZE thumbSize = GetThumbSize(size, thumb.Destination);
            thumb.Rect = new Rectangle(thumb.Destination.Left, thumb.Destination.Top,
                thumbSize.x, thumbSize.y);
        }

        private PSIZE UpdateThumb(IntPtr thumb, RECT dest)
        {
            if (thumb != IntPtr.Zero)
            {
                PSIZE size = GetSourceSize(thumb);
                DWM_THUMBNAIL_PROPERTIES props = new DWM_THUMBNAIL_PROPERTIES();
                props.dwFlags = Constants.DWM_TNP_VISIBLE | Constants.DWM_TNP_RECTDESTINATION;
                props.fVisible = true;
                props.rcDestination = new RECT(dest.Left, dest.Top, dest.Right, dest.Bottom);
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

        private PSIZE GetSourceSize(IntPtr thumb)
        {
            PSIZE size;
            DwmApi.DwmQueryThumbnailSourceSize(thumb, out size);
            return size;
        }

        public override string ToString()
        {
            return "Thumb " + Handle.ToString() + " @" + Destination.ToString();
        }
    }

    /// <summary>
    /// This class represents a Window.
    /// It has several attributes, such as the rect of the window, its title
    /// and its placement, which are only detected on demand, that is by
    /// calling the respective getter with true as an argument.
    /// </summary>
    public class Window
    {
        private int ProcessId;
        private string title;
        private IntPtr handle;
        private Thumb thumb = new Thumb(IntPtr.Zero, Rectangle.Empty);
        private volatile Bitmap staticThumb;

        /// <summary>
        /// The original rect of this window on the screen.
        /// </summary>
        private Nullable<Rectangle> rect = new Nullable<Rectangle>();
        private Nullable<Point> lastPosition = new Nullable<Point>();
        private Nullable<WINDOWPLACEMENT> placement = new Nullable<WINDOWPLACEMENT>();

        public Window(IntPtr handle)
        {
            this.handle = handle;
            GetTitle(true);
        }

        /// <summary>
        /// Sends WM_CLOSE to this window, which should prompt it to close.
        /// </summary>
        /// <returns>Returns true if the window has processed the message.</returns>
        public bool Close()
        {
            return User32.SendMessage(this.handle, Constants.WM_CLOSE, 0, 0) == 0;
        }

        public Thumb Thumb
        {
            get { return thumb; }
            set { thumb = value; }
        }

        public String Title
        {
            get { return title; }
        }

        public IntPtr Handle
        {
            get { return handle; }
            set { handle = value; }
        }

        public Bitmap StaticThumb
        {
            get { return staticThumb; }
            set { staticThumb = value; }
        }

        public Rectangle? Rect
        {
            get { return GetRect(); }
        }

        public Point? LastPosition
        {
            get { return lastPosition; }
            set { lastPosition = value; }
        }

        public WINDOWPLACEMENT? Placement
        {
            get { return GetPlacement(); }
            set { placement = value; }
        }

        public int GetProcessId()
        {
            return GetProcessId(false);
        }

        public int GetProcessId(bool detect)
        {
            if (ProcessId == 0 && detect)
            {
                User32.GetWindowThreadProcessId(this.handle, out ProcessId);
            }
            return ProcessId;
        }

        public String GetTitle()
        {
            return GetTitle(false);
        }

        public String GetTitle(bool detect)
        {
            if (this.title == null && detect)
            {
                StringBuilder sb = new StringBuilder(200);
                User32.GetWindowText(this.handle, sb, sb.Capacity);
                this.title = sb.ToString();
            }
            return this.title;
        }

        public WINDOWPLACEMENT? GetPlacement()
        {
            return GetPlacement(false);
        }

        public WINDOWPLACEMENT? GetPlacement(bool detect)
        {
            if (!this.placement.HasValue && detect)
            {
                WINDOWPLACEMENT windowPlacement = WINDOWPLACEMENT.New();
                if (!User32.GetWindowPlacement(this.handle, out windowPlacement))
                {
                    Console.WriteLine("GetWindowPlacement FAIL!");
                }
                this.placement = new Nullable<WINDOWPLACEMENT>(windowPlacement);
            }
            return this.placement;
        }

        public Rectangle? GetRect()
        {
            return GetRect(false);
        }

        public Rectangle? GetRect(bool detect)
        {
            if (!this.rect.HasValue && detect)
            {
                RECT rect = new RECT();
                User32.GetWindowRect(this.handle, out rect);
                this.rect = new Nullable<Rectangle>(rect.ToRectangle());
            }
            return this.rect;
        }

        /// <summary>
        /// Retrieves this window's bounds,
        /// which are a result of either a call to GetRect or GetPlacement
        /// depending on the window state.
        /// </summary>
        /// <returns></returns>
        public Rectangle GetBounds()
        {
            if (IsIconic())
            {
                WINDOWPLACEMENT placement = GetPlacement(true).Value;
                return placement.normalPosition.ToRectangle();
            }
            else
            {
                return GetRect(true).Value;
            }
        }

        /// <summary>
        /// Returns the (initial) bounds as used by the thumb animation.
        /// If the window is normal this is the actual rectangle the window currently is in.
        /// On the other hand, if the window is minimized, the bounds will not be the actual
        /// bounds at -32k x -32k, but instead something else best fitting for the animation.
        /// </summary>
        /// <returns></returns>
        public Rectangle GetAnimationBounds()
        {
            Rectangle bounds = GetBounds();
            if (IsIconic())
            {
                bounds.X = -bounds.Width;
                bounds.Y = -bounds.Height;
            }
            return bounds;
        }

        public Bitmap GetStaticThumb()
        {
            return GetStaticThumb(false);
        }

        public Bitmap GetStaticThumb(bool create)
        {
            if (this.staticThumb == null && create)
            {
                this.staticThumb = FetchStaticThumbnail();
            }
            return this.staticThumb;
        }

        protected Bitmap FetchStaticThumbnail()
        {
            IntPtr windowContext = User32.GetDC(this.handle);
            if (!IntPtr.Zero.Equals(windowContext))
            {
                try
                {
                    IntPtr hdc = Gdi32.CreateCompatibleDC(windowContext);
                    if (!IntPtr.Zero.Equals(hdc))
                    {
                        try
                        {
                            Rectangle bounds = GetRect(true).Value;
                            IntPtr hbitmap = Gdi32.CreateCompatibleBitmap(windowContext,
                                    bounds.Width, bounds.Height);
                            Gdi32.SelectObject(hdc, hbitmap);
                            if (User32.PrintWindow(this.handle, hdc, 0))
                            {
                                Bitmap thumb = Image.FromHbitmap(hbitmap);
                                Gdi32.DeleteObject(hbitmap);
                                return thumb;
                            }
                            else
                            {
                                Gdi32.DeleteObject(hbitmap);
                                Console.WriteLine("Could not print window '" + this.title + "'");
                            }
                        }
                        finally
                        {
                            Gdi32.DeleteDC(hdc);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Could not create compatible device context for '" + 
                            this.title + "'");
                    }
                }
                finally
                {
                    User32.ReleaseDC(this.handle, windowContext);
                }
            }
            else
            {
                Console.WriteLine("Could not create device context for '" + this.title + "'");
            }
            return null;
        }

        public bool IsIconic()
        {
            return User32.IsIconic(this.Handle);
        }

        public override string ToString()
        {
            return title;
        }
    }

    public class RectNode
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

        public void Normalize(ref Rectangle rect)
        {
            rect.X = 0;
            rect.Y = 0;
        }

        public bool Insert(Rectangle rect)
        {
            return InsertAndUpdate(ref rect);
        }

        public bool InsertAndUpdate(ref Rectangle rect)
        {
            Normalize(ref rect);
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
                    rect.X = this.rect.X;
                    rect.Y = this.rect.Y;
                    if (childA.childA.rect.Width == childA.rect.Width)
                    {
                        childA.childB = new RectNode(new Rectangle(
                            childA.rect.X,
                            childA.rect.Y + childA.childA.rect.Height,
                            childA.rect.Width,
                            childA.rect.Height - childA.childA.rect.Height));
                    }
                    else if (childA.childA.rect.Height == childA.rect.Height)
                    {
                        childA.childB = new RectNode(new Rectangle(
                            childA.rect.X + childA.childA.rect.Width,
                            childA.rect.Y,
                            childA.rect.Width - childA.childA.rect.Width,
                            childA.rect.Height));
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return (childA != null && childA.InsertAndUpdate(ref rect)) || 
                    (childB != null && childB.InsertAndUpdate(ref rect));
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

        public IEnumerable<Rectangle> GetStructure()
        {
            return GetStructureFoo();
        }

        public IEnumerable<Rectangle> GetStructureFoo()
        {
            LinkedList<Rectangle> rects = new LinkedList<Rectangle>();
            LinkedList<RectNode> nodes = new LinkedList<RectNode>();
            if (childA != null) nodes.AddLast(childA);
            if (childB != null) nodes.AddLast(childB);
            rects.AddLast(this.rect);
            while (nodes.Count > 0)
            {
                foreach (RectNode node in nodes)
                {
                    rects.AddLast(node.rect);
                }
                nodes = new LinkedList<RectNode>(GetChildren(nodes)); // get next level
            }
            return rects;
        }

        protected void AddStructureTo(LinkedList<Rectangle> rects)
        {
            if (childA != null) childA.AddStructureTo(rects);
            if (childB != null) childB.AddStructureTo(rects);
            rects.AddFirst(this.rect);
        }

        public IEnumerable<RectNode> GetChildren(IEnumerable<RectNode> parents)
        {
            List<RectNode> children = new List<RectNode>();
            foreach (RectNode parent in parents)
            {
                children.AddRange(parent.GetChildren());
            }
            return children;
        }

        /// <summary>
        /// Returns the direct children of this node, which are max. 2.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RectNode> GetChildren()
        {
            List<RectNode> children = new List<RectNode>(2);
            if (childA != null) children.Add(childA);
            if (childB != null) children.Add(childB);

            return children;
        }
    }
}
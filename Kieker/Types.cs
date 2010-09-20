using System;
using System.Drawing;
using System.Collections.Generic;
using Kieker.DllImport;
namespace Kieker
{
    public class Thumb
    {
        /// <summary>
        /// Handle to the DWM Thumb
        /// </summary>
        public IntPtr Value;

        /// <summary>
        /// Destination rect
        /// </summary>
        public Rectangle Destination;

        /// <summary>
        /// Actual rect within the destination rect, which may be smaller due to
        /// the aspect ratio of the window.
        /// </summary>
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

    public class Window
    {
        public string Title;
        public IntPtr Handle;
        public Thumb Thumb = new Thumb(IntPtr.Zero, Rectangle.Empty);

        /// <summary>
        /// The original rect of this window on the screen.
        /// </summary>
        public Rectangle Rect;
        public Nullable<Point> LastPosition;

        public override string ToString()
        {
            return Title;
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
using System;
using System.Drawing;
using System.Collections.Generic;
namespace Kieker
{
    public class Thumb
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

    public class Window
    {
        public string Title;
        public IntPtr Handle;
        public Thumb Thumb;
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
}
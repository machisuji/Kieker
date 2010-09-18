using System.Windows.Forms;
using System;
using System.Drawing;
using System.Collections.Generic;

namespace Kieker
{
    class RectPainter
    {
        private Form form;
        private List<Rectangle> rects;
        private List<Rectangle> rectsToPaint = new List<Rectangle>();
        private int transitionTime = 2000;
        private Array colors = Enum.GetValues(typeof(KnownColor));
        private bool enabled = true;

        public RectPainter(List<Rectangle> rects, Form form)
        {
            this.rects = rects;
            this.form = form;
        }

        public RectPainter(Form form)
        {
            this.rects = new List<Rectangle>();
            this.form = form;
        }

        public void Paint(object sender, PaintEventArgs e)
        {
            if (enabled)
            {
                Graphics g = e.Graphics;
                int ci = 0;
                int counter = 1;
                foreach (Rectangle rect in rectsToPaint)
                {
                    Color color = Color.Black;
                    while (Color.Black.Equals(color))
                    {
                        color = Color.FromKnownColor((KnownColor)colors.GetValue(ci++ % colors.Length));
                    }
                    color = Color.FromArgb(150, color);
                    g.FillRectangle(new SolidBrush(color), rect);
                    g.DrawRectangle(new Pen(Color.Black), rect);
                    g.DrawString(counter.ToString(), new Font("Verdana", 32f, FontStyle.Bold),
                        new SolidBrush(Color.White), rect.X + 10, rect.Y + 10);
                    ++counter;
                }
            }
        }

        public void AnimateRects()
        {
            Action action = () =>
            {
                rectsToPaint.Clear();
                foreach (Rectangle rect in rects)
                {
                    rectsToPaint.Add(rect);
                    form.Invalidate(rect);
                    System.Threading.Thread.Sleep(transitionTime);
                }
            };
            action.Fork();
        }

        public void ShowRects()
        {
            rectsToPaint.Clear();
            rectsToPaint.AddRange(rects);
            form.Invalidate();
        }

        public void HideRects()
        {
            rectsToPaint.Clear();
            form.Invalidate();
        }

        public List<Rectangle> Rects
        {
            get { return rects; }
        }

        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        public PaintEventHandler ToPaintEventHandler()
        {
            return new PaintEventHandler(Paint);
        }
    }
}
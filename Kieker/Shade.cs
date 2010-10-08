using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Kieker
{
    public partial class Shade : Form
    {
        private ICollection<Rectangle> holes;

        public Shade()
        {
            InitializeComponent();

            this.Paint += new PaintEventHandler(Shade_Paint);
        }

        void Shade_Paint(object sender, PaintEventArgs e)
        {
            if (holes != null)
            {
                Graphics g = e.Graphics;
                Brush transparent = new SolidBrush(Color.White); // white ^= transparency key
                foreach (Rectangle hole in holes)
                {
                    g.FillRectangle(transparent, hole);
                }
            }
        }

        private void Shade_Load(object sender, EventArgs e)
        {

        }

        public ICollection<Rectangle> Holes
        {
            get { return holes; }
            set { holes = value; }
        }
    }
}

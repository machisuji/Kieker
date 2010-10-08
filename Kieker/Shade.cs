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
        private volatile bool fadeIn = true;
        private int steps = 30;
        private double transparency = 0.25;
        private Timer fadeTimer;

        public Shade()
        {
            InitializeComponent();

            this.VisibleChanged += new EventHandler(Shade_VisibleChanged);
            this.fadeTimer = CreateTimer();
        }

        protected Timer CreateTimer()
        {
            Timer timer = new Timer();
            timer.Tick += new EventHandler(fadeTimer_Tick);
            timer.Interval = 15;

            return timer;
        }

        void fadeTimer_Tick(object sender, EventArgs e)
        {
            if (fadeIn)
            {
                double op = Opacity + GetStep();
                if (Opacity >= GetOpacity())
                {
                    Opacity = GetOpacity();
                    ((Timer)sender).Stop();
                }
                else
                {
                    Opacity = op;
                }
            }
            else
            {
                double op = Opacity - GetStep();
                if (op <= 0)
                {
                    Opacity = 0;
                    ((Timer)sender).Stop();
                }
                else
                {
                    Opacity = op;
                }
            }
        }

        void Shade_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                fadeTimer.Start();
            }
            else
            {
                Opacity = 0;
                fadeIn = true;
            }
        }

        public void FadeOut()
        {
            fadeIn = false;
            fadeTimer.Start();
        }

        private void Shade_Load(object sender, EventArgs e)
        {

        }

        protected double GetStep()
        {
            return GetOpacity() / steps;
        }

        protected double GetOpacity()
        {
            return 1 - transparency;
        }

        public double Transparency
        {
            get { return transparency; }
            set { transparency = value; }
        }
    }
}

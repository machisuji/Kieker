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
        private Settings settings;

        private volatile bool fadeIn = true;
        private int steps = 30;
        private double transparency = 0.25;
        private Timer fadeTimer;

        public Shade(Settings settings)
        {
            this.settings = settings;

            InitializeComponent();

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

        public void FadeIn()
        {
            Opacity = 0;
            fadeIn = true;
            StartTimer();
        }

        public void FadeOut()
        {
            Opacity = GetOpacity();
            fadeIn = false;
            StartTimer();
        }

        protected void StartTimer()
        {
            fadeTimer.Interval = (int)Math.Round((settings.AnimationDuration - 50) / (double)steps, 0d);
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

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
    public partial class Settings : Form
    {
        private bool includeMinimizedWindows = true;
        private bool indicateMinimizedWindows = true;

        public bool IncludeMinimizedWindows
        {
            get { return includeMinimizedWindows; }
            set
            {
                includeMinimizedWindows = value;
                cbIncludeMinimizedWindows.Checked = value;
            }
        }

        /// <summary>
        /// This property can only be set true, if IncludeMinimizedWindows property is true as well.
        /// </summary>
        public bool IndicateMinimizedwindows
        {
            get { return indicateMinimizedWindows; }
            set
            {
                if (includeMinimizedWindows && value)
                    indicateMinimizedWindows = true;
            }
        }

        public Settings()
        {
            InitializeComponent();

            this.FormClosing += new FormClosingEventHandler(Settings_FormClosing);
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            cbIncludeMinimizedWindows.Checked = includeMinimizedWindows;
            cbIndicateMinimizedWindows.Checked = indicateMinimizedWindows;
        }

        void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {

        }

        private void cbIncludeMinimizedWindows_CheckedChanged(object sender, EventArgs e)
        {
            includeMinimizedWindows = cbIncludeMinimizedWindows.Checked;
            cbIndicateMinimizedWindows.Enabled = cbIncludeMinimizedWindows.Checked;
        }

        private void cbIndicateMinimizedWindows_CheckedChanged(object sender, EventArgs e)
        {
            indicateMinimizedWindows = cbIndicateMinimizedWindows.Checked;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Kieker
{
    public partial class Settings : Form
    {
        private Dictionary<String, Action<String>> loadActions = new Dictionary<String, Action<String>>();

        private bool includeMinimizedWindows = true;
        private bool indicateMinimizedWindows = true;

        public Settings()
        {
            InitializeComponent();

            this.FormClosing += new FormClosingEventHandler(Settings_FormClosing);

            loadActions.Add("includeMinimizedWindows",
                (value) => includeMinimizedWindows = Boolean.Parse(value));
            loadActions.Add("indicateMinimizedWindows",
                (value) => { indicateMinimizedWindows = Boolean.Parse(value); Console.WriteLine("val: " + value); });
        }

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

        private void Settings_Load(object sender, EventArgs e)
        {
            cbIncludeMinimizedWindows.Checked = includeMinimizedWindows;
            cbIndicateMinimizedWindows.Checked = indicateMinimizedWindows;
        }

        public void LoadSettings()
        {
            foreach (String line in File.ReadAllLines("kieker.ini"))
            {
                try
                {
                    String[] tokens = line.Split('=');
                    String key = tokens[0].Trim();
                    String value = tokens[1].Trim();
                    loadActions[key].Invoke(value);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not parse settings line: " + line +
                        " (" + e.Message + ")");
                }
            }
        }

        public void SaveSettings()
        {
            String[] lines = new String[]
            {
                "includeMinimizedWindows=" + includeMinimizedWindows.ToString(),
                "indicateMinimizedWindows=" + indicateMinimizedWindows.ToString()
            };
            File.WriteAllLines("kieker.ini", lines);
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

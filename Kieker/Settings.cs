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

        /// <summary>
        /// Duration of the window arrangement animation in milliseconds.
        /// Due to the current implementation, the minimum value for this
        /// is 124.
        /// </summary>
        private int animationDuration = 460;

        private bool includeMinimizedWindows = true;
        private bool indicateMinimizedWindows = true;
        private bool dimBackground = true;

        private Keys[] hotkey = new Keys[] {
            Keys.LMenu, Keys.Oem5
        };

        private bool changeHotkey = false;
        private bool changeModifier = false;

        private ThumbView view;

        public Settings()
        {
            InitializeComponent();

            this.FormClosing += new FormClosingEventHandler(Settings_FormClosing);

            loadActions.Add("includeMinimizedWindows",
                (value) => includeMinimizedWindows = Boolean.Parse(value));
            loadActions.Add("indicateMinimizedWindows",
                (value) => indicateMinimizedWindows = Boolean.Parse(value));
            loadActions.Add("dimBackground",
                (value) => dimBackground = Boolean.Parse(value));

            loadActions.Add("hotkey",
                (value) => hotkey = Hotkeys.Parse(value));

            loadActions.Add("animationDuration",
                (value) => animationDuration = Int32.Parse(value));
        }

        public Settings(ThumbView view) : this()
        {
            this.view = view;
            Hotkeys.Add((keys) =>
            {
                if (changeHotkey)
                {
                    txtHotkey.Text = Hotkeys.ToString(keys);
                    hotkey = keys;
                }
                else
                {
                    return false;
                }
                return true;
            });
        }

        protected override bool ProcessDialogKey(Keys keys)
        {
            if (changeHotkey && false)
            {
                Keys modifiers = keys & Keys.Modifiers;
                Keys keyCode = keys & Keys.KeyCode;
                txtHotkey.Text = modifiers.ToString() + " + " + keyCode.ToString();
            }
            return !changeHotkey ? base.ProcessDialogKey(keys) : false;
        }

        public Keys[] Hotkey
        {
            get { return hotkey; }
            set { hotkey = value; }
        }

        public bool DimBackground
        {
            get { return dimBackground; }
            set { dimBackground = value; }
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
            cbDimBackground.Checked = dimBackground;
            txtHotkey.Text = Hotkeys.ToString(hotkey);
            txtAnimationDuration.Text = animationDuration.ToString();
            tbAnimationDuration.Value = animationDuration;
        }

        public void LoadSettings()
        {
            try
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
            catch (FileNotFoundException e)
            {
                Console.WriteLine("kieker.ini not found");
            }
        }

        public void SaveSettings()
        {
            String[] lines = new String[]
            {
                "includeMinimizedWindows=" + includeMinimizedWindows.ToString(),
                "indicateMinimizedWindows=" + indicateMinimizedWindows.ToString(),
                "dimBackground=" + dimBackground.ToString(),
                "hotkey=" + Hotkeys.ToString(hotkey),
                "animationDuration=" + animationDuration.ToString()
            };
            try
            {
                File.WriteAllLines("kieker.ini", lines);
            }
            catch (Exception e)
            {
                MessageBox.Show("Kieker settings could not be saved due to: " + e.Message);
            }
        }

        void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {

        }

        public int AnimationDuration
        {
            get { return animationDuration; }
            set
            {
                if (value >= 124)
                    animationDuration = value;
                else
                    throw new ArgumentException("The duration must be at least 124ms long.");
            }
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

        private void cbDimBackground_CheckedChanged(object sender, EventArgs e)
        {
            dimBackground = cbDimBackground.Checked;
        }

        private void btnHotkey_Click(object sender, EventArgs e)
        {
            if (changeModifier) return;
            if (!changeHotkey)
            {
                changeHotkey = true;
                btnHotkey.Text = "Apply";
                DisableHotkey();
            }
            else
            {
                changeHotkey = false;
                btnHotkey.Text = "Change";
                EnableHotkey();
            }
        }

        protected void EnableHotkey()
        {
            view.HotkeyEnabled = true;
        }

        protected void DisableHotkey()
        {
            view.HotkeyEnabled = false;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            txtAnimationDuration.Text = tbAnimationDuration.Value.ToString();
            animationDuration = tbAnimationDuration.Value;
        }
    }
}

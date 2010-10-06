﻿using System;
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

        private Keys hotkey = Keys.Oem5;
        private Keys modifier = Keys.LControlKey;

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
            loadActions.Add("hotkey",
                (value) => hotkey = (Keys)Enum.Parse(typeof(Keys), value));
            loadActions.Add("modifier",
                (value) => modifier = (Keys)Enum.Parse(typeof(Keys), value));
        }

        public Settings(ThumbView view) : this()
        {
            this.view = view;
            view.GlobalKeyDown += new KeyEventHandler(Settings_KeyDown);
        }

        protected void Settings_KeyDown(object sender, KeyEventArgs args)
        {
            if (changeHotkey)
            {
                txtHotkey.Text = args.KeyCode.ToString();
                hotkey = args.KeyCode;
            }
            else if (changeModifier)
            {
                txtModifier.Text = args.KeyData.ToString();
                modifier = args.KeyCode;
            }
        }

        public bool IsHotkey(Keys keys)
        {
            Keys modifiers = keys & Keys.Modifiers;
            Keys keyCode = keys & Keys.KeyCode;
            Keys hkModifiers = hotkey & Keys.Modifiers;
            Keys hkKeyCode = hotkey & Keys.KeyCode;
            Console.WriteLine("HK[" + hkModifiers + " + " + hkKeyCode + "] vs KD[" + 
                modifiers + " + " + keyCode + "]"); 
            return (keys & hotkey) == hotkey;
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

        public Keys Hotkey
        {
            get { return hotkey; }
            set { hotkey = value; }
        }

        public Keys Modifier
        {
            get { return modifier; }
            set { modifier = value; }
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
            txtHotkey.Text = hotkey.ToString();
            txtModifier.Text = modifier.ToString();
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
                "indicateMinimizedWindows=" + indicateMinimizedWindows.ToString(),
                "hotkey=" + hotkey.ToString(),
                "modifier=" + modifier.ToString()
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

        private void btnModifier_Click(object sender, EventArgs e)
        {
            if (changeHotkey) return;
            if (!changeModifier)
            {
                changeModifier = true;
                btnModifier.Text = "Apply";
                DisableHotkey();
            }
            else
            {
                changeModifier = false;
                btnModifier.Text = "Change";
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
    }
}

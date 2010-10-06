﻿namespace Kieker
{
    partial class Settings
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Settings));
            this.cbIncludeMinimizedWindows = new System.Windows.Forms.CheckBox();
            this.ttIncludeMinimizedWindows = new System.Windows.Forms.ToolTip(this.components);
            this.cbIndicateMinimizedWindows = new System.Windows.Forms.CheckBox();
            this.txtHotkey = new System.Windows.Forms.TextBox();
            this.btnHotkey = new System.Windows.Forms.Button();
            this.lblHotkey = new System.Windows.Forms.Label();
            this.lblModifier = new System.Windows.Forms.Label();
            this.txtModifier = new System.Windows.Forms.TextBox();
            this.btnModifier = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // cbIncludeMinimizedWindows
            // 
            this.cbIncludeMinimizedWindows.AutoSize = true;
            this.cbIncludeMinimizedWindows.Location = new System.Drawing.Point(12, 12);
            this.cbIncludeMinimizedWindows.Name = "cbIncludeMinimizedWindows";
            this.cbIncludeMinimizedWindows.Size = new System.Drawing.Size(157, 17);
            this.cbIncludeMinimizedWindows.TabIndex = 1;
            this.cbIncludeMinimizedWindows.Text = "Include Minimized Windows";
            this.ttIncludeMinimizedWindows.SetToolTip(this.cbIncludeMinimizedWindows, "If this box is checked, Kieker will also include minimized windows,\r\nwhich it doe" +
                    "sn\'t per default.");
            this.cbIncludeMinimizedWindows.UseVisualStyleBackColor = true;
            this.cbIncludeMinimizedWindows.CheckedChanged += new System.EventHandler(this.cbIncludeMinimizedWindows_CheckedChanged);
            // 
            // ttIncludeMinimizedWindows
            // 
            this.ttIncludeMinimizedWindows.Popup += new System.Windows.Forms.PopupEventHandler(this.toolTip1_Popup);
            // 
            // cbIndicateMinimizedWindows
            // 
            this.cbIndicateMinimizedWindows.AutoSize = true;
            this.cbIndicateMinimizedWindows.Enabled = false;
            this.cbIndicateMinimizedWindows.Location = new System.Drawing.Point(12, 35);
            this.cbIndicateMinimizedWindows.Name = "cbIndicateMinimizedWindows";
            this.cbIndicateMinimizedWindows.Size = new System.Drawing.Size(160, 17);
            this.cbIndicateMinimizedWindows.TabIndex = 2;
            this.cbIndicateMinimizedWindows.Text = "Indicate Minimized Windows";
            this.ttIncludeMinimizedWindows.SetToolTip(this.cbIndicateMinimizedWindows, "Check this box if you want minimized windows to be indicated.\r\nThis only makes se" +
                    "nse when those windows are included.");
            this.cbIndicateMinimizedWindows.UseVisualStyleBackColor = true;
            this.cbIndicateMinimizedWindows.CheckedChanged += new System.EventHandler(this.cbIndicateMinimizedWindows_CheckedChanged);
            // 
            // txtHotkey
            // 
            this.txtHotkey.Enabled = false;
            this.txtHotkey.Location = new System.Drawing.Point(56, 115);
            this.txtHotkey.Name = "txtHotkey";
            this.txtHotkey.Size = new System.Drawing.Size(84, 20);
            this.txtHotkey.TabIndex = 3;
            // 
            // btnHotkey
            // 
            this.btnHotkey.Location = new System.Drawing.Point(146, 112);
            this.btnHotkey.Name = "btnHotkey";
            this.btnHotkey.Size = new System.Drawing.Size(75, 23);
            this.btnHotkey.TabIndex = 4;
            this.btnHotkey.Text = "Change";
            this.btnHotkey.UseVisualStyleBackColor = true;
            this.btnHotkey.Click += new System.EventHandler(this.btnHotkey_Click);
            // 
            // lblHotkey
            // 
            this.lblHotkey.AutoSize = true;
            this.lblHotkey.Location = new System.Drawing.Point(6, 118);
            this.lblHotkey.Name = "lblHotkey";
            this.lblHotkey.Size = new System.Drawing.Size(44, 13);
            this.lblHotkey.TabIndex = 5;
            this.lblHotkey.Text = "Hotkey:";
            // 
            // lblModifier
            // 
            this.lblModifier.AutoSize = true;
            this.lblModifier.Location = new System.Drawing.Point(6, 144);
            this.lblModifier.Name = "lblModifier";
            this.lblModifier.Size = new System.Drawing.Size(47, 13);
            this.lblModifier.TabIndex = 7;
            this.lblModifier.Text = "Modifier:";
            // 
            // txtModifier
            // 
            this.txtModifier.Enabled = false;
            this.txtModifier.Location = new System.Drawing.Point(56, 141);
            this.txtModifier.Name = "txtModifier";
            this.txtModifier.Size = new System.Drawing.Size(84, 20);
            this.txtModifier.TabIndex = 6;
            // 
            // btnModifier
            // 
            this.btnModifier.Location = new System.Drawing.Point(146, 139);
            this.btnModifier.Name = "btnModifier";
            this.btnModifier.Size = new System.Drawing.Size(75, 23);
            this.btnModifier.TabIndex = 8;
            this.btnModifier.Text = "Change";
            this.btnModifier.UseVisualStyleBackColor = true;
            this.btnModifier.Click += new System.EventHandler(this.btnModifier_Click);
            // 
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(233, 246);
            this.Controls.Add(this.btnModifier);
            this.Controls.Add(this.lblModifier);
            this.Controls.Add(this.txtModifier);
            this.Controls.Add(this.lblHotkey);
            this.Controls.Add(this.btnHotkey);
            this.Controls.Add(this.txtHotkey);
            this.Controls.Add(this.cbIndicateMinimizedWindows);
            this.Controls.Add(this.cbIncludeMinimizedWindows);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Settings";
            this.Text = "Kieker Settings";
            this.Load += new System.EventHandler(this.Settings_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox cbIncludeMinimizedWindows;
        private System.Windows.Forms.ToolTip ttIncludeMinimizedWindows;
        private System.Windows.Forms.CheckBox cbIndicateMinimizedWindows;
        private System.Windows.Forms.TextBox txtHotkey;
        private System.Windows.Forms.Button btnHotkey;
        private System.Windows.Forms.Label lblHotkey;
        private System.Windows.Forms.Label lblModifier;
        private System.Windows.Forms.TextBox txtModifier;
        private System.Windows.Forms.Button btnModifier;
    }
}
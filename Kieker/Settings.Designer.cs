namespace Kieker
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
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(193, 286);
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
    }
}
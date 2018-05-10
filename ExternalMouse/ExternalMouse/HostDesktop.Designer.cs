namespace ExternalMouse
{
    partial class HostDesktop
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.ScreensPanel = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Margin = new System.Windows.Forms.Padding(3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(141, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "label1";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.label1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.HomeDesktop_MouseDown);
            this.label1.MouseEnter += new System.EventHandler(this.HomeDesktop_MouseEnter);
            this.label1.MouseLeave += new System.EventHandler(this.HomeDesktop_MouseLeave);
            this.label1.MouseHover += new System.EventHandler(this.HomeDesktop_MouseEnter);
            // 
            // ScreensPanel
            // 
            this.ScreensPanel.AutoSize = true;
            this.ScreensPanel.Location = new System.Drawing.Point(3, 16);
            this.ScreensPanel.Name = "ScreensPanel";
            this.ScreensPanel.Size = new System.Drawing.Size(135, 90);
            this.ScreensPanel.TabIndex = 2;
            this.ScreensPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.HomeDesktop_MouseDown);
            this.ScreensPanel.MouseEnter += new System.EventHandler(this.HomeDesktop_MouseEnter);
            this.ScreensPanel.MouseLeave += new System.EventHandler(this.HomeDesktop_MouseLeave);
            this.ScreensPanel.MouseHover += new System.EventHandler(this.HomeDesktop_MouseEnter);
            // 
            // label2
            // 
            this.label2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.label2.Location = new System.Drawing.Point(0, 113);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(141, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "localhost";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // HostDesktop
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ScreensPanel);
            this.Name = "HostDesktop";
            this.Size = new System.Drawing.Size(141, 126);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.HomeDesktop_MouseDown);
            this.MouseEnter += new System.EventHandler(this.HomeDesktop_MouseEnter);
            this.MouseLeave += new System.EventHandler(this.HomeDesktop_MouseLeave);
            this.MouseHover += new System.EventHandler(this.HomeDesktop_MouseEnter);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel ScreensPanel;
        private System.Windows.Forms.Label label2;
    }
}

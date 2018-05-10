namespace ExternalMouse
{
    partial class DesktopsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DesktopsForm));
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.NotifyIconMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.connectNewDesktopToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FlowPanel = new System.Windows.Forms.Panel();
            this.AddNewDesktopButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label_GroupName = new System.Windows.Forms.Label();
            this.BottomPanel = new System.Windows.Forms.Panel();
            this.NotifyIconMenu.SuspendLayout();
            this.BottomPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.notifyIcon1.BalloonTipText = "asdas";
            this.notifyIcon1.BalloonTipTitle = "asdas";
            this.notifyIcon1.ContextMenuStrip = this.NotifyIconMenu;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "Remote Mouse";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // NotifyIconMenu
            // 
            this.NotifyIconMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.connectNewDesktopToolStripMenuItem});
            this.NotifyIconMenu.Name = "NotifyIconMenu";
            this.NotifyIconMenu.Size = new System.Drawing.Size(190, 26);
            // 
            // connectNewDesktopToolStripMenuItem
            // 
            this.connectNewDesktopToolStripMenuItem.Name = "connectNewDesktopToolStripMenuItem";
            this.connectNewDesktopToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.connectNewDesktopToolStripMenuItem.Text = "Connect new desktop";
            this.connectNewDesktopToolStripMenuItem.Click += new System.EventHandler(this.connectNewDesktopToolStripMenuItem_Click);
            // 
            // FlowPanel
            // 
            this.FlowPanel.AllowDrop = true;
            this.FlowPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.FlowPanel.Location = new System.Drawing.Point(0, 5);
            this.FlowPanel.Name = "FlowPanel";
            this.FlowPanel.Size = new System.Drawing.Size(724, 87);
            this.FlowPanel.TabIndex = 5;
            this.FlowPanel.DragDrop += new System.Windows.Forms.DragEventHandler(this.FlowPanel_DragDrop);
            this.FlowPanel.DragOver += new System.Windows.Forms.DragEventHandler(this.FlowPanel_DragOver);
            // 
            // AddNewDesktopButton
            // 
            this.AddNewDesktopButton.Location = new System.Drawing.Point(319, 14);
            this.AddNewDesktopButton.Name = "AddNewDesktopButton";
            this.AddNewDesktopButton.Size = new System.Drawing.Size(75, 23);
            this.AddNewDesktopButton.TabIndex = 2;
            this.AddNewDesktopButton.Text = "Add New";
            this.AddNewDesktopButton.UseVisualStyleBackColor = true;
            this.AddNewDesktopButton.Click += new System.EventHandler(this.AddNewDesktopButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Group name";
            // 
            // label_GroupName
            // 
            this.label_GroupName.AutoSize = true;
            this.label_GroupName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label_GroupName.Location = new System.Drawing.Point(85, 19);
            this.label_GroupName.Name = "label_GroupName";
            this.label_GroupName.Size = new System.Drawing.Size(41, 13);
            this.label_GroupName.TabIndex = 4;
            this.label_GroupName.Text = "label2";
            // 
            // BottomPanel
            // 
            this.BottomPanel.AutoSize = true;
            this.BottomPanel.Controls.Add(this.AddNewDesktopButton);
            this.BottomPanel.Controls.Add(this.label_GroupName);
            this.BottomPanel.Controls.Add(this.label1);
            this.BottomPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.BottomPanel.Location = new System.Drawing.Point(0, 92);
            this.BottomPanel.Name = "BottomPanel";
            this.BottomPanel.Size = new System.Drawing.Size(724, 45);
            this.BottomPanel.TabIndex = 7;
            // 
            // DesktopsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(724, 137);
            this.Controls.Add(this.BottomPanel);
            this.Controls.Add(this.FlowPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DesktopsForm";
            this.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
            this.Text = "DesktopsForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DesktopsForm_FormClosing);
            this.Load += new System.EventHandler(this.DesktopsForm_Load);
            this.NotifyIconMenu.ResumeLayout(false);
            this.BottomPanel.ResumeLayout(false);
            this.BottomPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip NotifyIconMenu;
        private System.Windows.Forms.ToolStripMenuItem connectNewDesktopToolStripMenuItem;
        private System.Windows.Forms.Panel FlowPanel;
        private System.Windows.Forms.Button AddNewDesktopButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label_GroupName;
        private System.Windows.Forms.Panel BottomPanel;
    }
}
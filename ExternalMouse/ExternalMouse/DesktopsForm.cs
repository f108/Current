using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExternalMouse
{
    public partial class DesktopsForm : Form
    {
        public DesktopsForm()
        {
            InitializeComponent();

            this.FlowPanel.Controls.Add(new HostDesktop("111"));
            this.FlowPanel.Controls.Add(new HostDesktop("222"));
            this.FlowPanel.Controls.Add(new HostDesktop("333"));
            this.FlowPanel.Controls.Add(new HostDesktop("444"));

            //this.panel1.Controls.Add(new Button());
            //this.panel1.Controls.Add(new Button());

            foreach (Control c in this.FlowPanel.Controls)
            {
                c.MouseDown += new MouseEventHandler(c_MouseDown);
            }

            FlowPanel.AutoSize = true;
            notifyIcon1.Visible = true;
        }

        public new void Show()
        {
            label_GroupName.Text = Program.GroupName;
            base.Show();
            base.BringToFront();
        }
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
        }

        private void connectNewDesktopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show();
        }

        private void AddNewDesktopButton_Click(object sender, EventArgs e)
        {
            AddingNewHost addingNewHost = new AddingNewHost();
            addingNewHost.Show();
        }

        private void DesktopsForm_Load(object sender, EventArgs e)
        {

        }

        private void DesktopsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            base.Hide();
            e.Cancel = true;
        }

        void c_MouseDown(object sender, MouseEventArgs e)
        {
            Control c = sender as Control;
            c.DoDragDrop(c, DragDropEffects.Move);
        }

        void FlowPanel_DragDrop(object sender, DragEventArgs e)
        {
            Control c = e.Data.GetData(e.Data.GetFormats()[0]) as Control;
            if (c != null)
            {
                c.Location = this.FlowPanel.PointToClient(new Point(e.X, c.Location.Y));
                this.FlowPanel.Controls.Add(c);
            }
        }

        void FlowPanel_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace ExternalMouse
{
    public partial class DesktopsForm : Form
    {
        public ControlReorderCallback controlReorderCallback;
        public DesktopsForm()
        {
            InitializeComponent();

            FlowPanel.AutoSize = true;
            notifyIcon1.Visible = true;
        }

        public void AddOrUpdate(Host host)
        {
            if (FlowPanel.InvokeRequired)
            {
                Invoke(new MethodInvoker(() => _addOrUpdate(host)));
            }
            else _addOrUpdate(host);
        }

        public void _addOrUpdate(Host host)
        {
            Reorder();

            if (FlowPanel.Controls.ContainsKey(host.ipAddress.ToString()))
            {
                ((HostDesktop) FlowPanel.Controls[host.ipAddress.ToString()]).Update();
                return;
            }

            FlowPanel.Controls.Add(new HostDesktop(host));

/*            foreach (Control c in this.FlowPanel.Controls)
            {
                c.MouseDown += new MouseEventHandler(c_MouseDown);
            }*/
        }

        public void PopupBroadcastNotification(IPAddress address, byte[] bytes)
        {
            DialogResult dialogResult = MessageBox.Show(address.ToString()+" wants to connect you to his mouse group", "Connection request", MessageBoxButtons.YesNo );
            if (dialogResult==DialogResult.Yes)
            {
                Program.pairedHosts.AddHostByIP(address);
                Program.pairedHosts.InitiateKeyExchange(address);
                Program.pairedHosts.Ping(address);
            }
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
            c.BringToFront();
            //c.DoDragDrop(c, DragDropEffects.Move);
            Program.PostLog("MouseDown " + " " + Cursor.Position.ToString());
        }

        void FlowPanel_DragDrop(object sender, DragEventArgs e)
        {
            HostDesktop c = e.Data.GetData(e.Data.GetFormats()[0]) as HostDesktop;
            if (c != null)
            {
                //c.Location = new Point(FlowPanel.PointToClient(new Point(e.X, e.Y)).X - c.mouseLDownPoint.X, 0);
                //c.BringToFront();
                c.Capture = false;
                Reorder();
                //this.FlowPanel.Controls.Add(c);
            }
        }

        void FlowPanel_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
            HostDesktop c = e.Data.GetData(e.Data.GetFormats()[0]) as HostDesktop;
            c.Location = new Point(FlowPanel.PointToClient(new Point(e.X, e.Y)).X - c.mouseLDownPoint.X, 0);
            c.Capture = true;
            Reorder();
        }

        
        void Reorder()
        {
            if (InvokeRequired)
                Invoke(new MethodInvoker(() => _reorder()));
            else _reorder();
        }

        void _reorder()
        {
            int offset = 0;
            foreach (HostDesktop hd in FlowPanel.Controls.OfType<HostDesktop>().OrderBy(c => c.Location.X+c.Width/2))
            {
                if (!hd.Capture && hd.Location.X!=offset)
                    hd.Location = new Point(offset, 0);
                offset += hd.Width+10;
            }

            List<string> controls = new List<string>();

            foreach (HostDesktop hd in FlowPanel.Controls.OfType<HostDesktop>().OrderBy(c => c.Location.X))
            {
                controls.Add(hd.Name);
            }
            controlReorderCallback(controls);
        }

    }
}

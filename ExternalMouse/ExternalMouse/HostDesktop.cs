using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ExternalMouse
{
    public partial class HostDesktop : UserControl
    {
        Host host;
        public Point mouseLDownPoint;
        public HostDesktop(Host _host)
        {
            InitializeComponent();
            host = _host;
            host.controlHostUpdateCallback = Update;
            Name = host.ipAddress.ToString();

            FillData();
        }

        private void _update()
        {
            label1.Text = host.MachineName;
            label2.Text = host.ipAddress.ToString();
            if (ScreensPanel.Controls.Count!= host.screenInfo.Count)
            {
                FillData();
                base.Update();
                return;
            }
            for (int i = 0; i < host.screenInfo.Count; i++)
            {
                ((PictureBox)ScreensPanel.Controls[i]).Image = host.screenInfo[i].Screenshot;
            };
            base.Update();
        }
        public new void Update()
        {
            Program.PostLog("Host desktop updated:");
            if (InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() =>
                {
                    _update();
                }));
            }
            else
            {
                _update();
            }
        }
        public void FillData()
        {
            int currentLeft = 5;
            int ownerHeight = ScreensPanel.Height;
            int index = 0;
            label1.Text = host.MachineName;
            if (host.screenInfo.Count < 1) return;
            ScreensPanel.Controls.Clear();
            int maxHeight = host.screenInfo.Max(x => x.Height);
            int dh = maxHeight / ownerHeight;
            for (int i = 0; i < host.screenInfo.Count; i++)
            {
                PictureBox screenFrame = new PictureBox();
                screenFrame.Left = currentLeft;
                screenFrame.Height = host.screenInfo[i].Height / dh;
                screenFrame.Width = host.screenInfo[i].Width / dh;
                screenFrame.BorderStyle = BorderStyle.FixedSingle;
                currentLeft += screenFrame.Width + 10;
                screenFrame.Image = host.screenInfo[i].Screenshot == null ? new Bitmap(1, 1) : host.screenInfo[i].Screenshot;
                screenFrame.MouseEnter += new System.EventHandler(this.HomeDesktop_MouseEnter);
                screenFrame.MouseLeave += new System.EventHandler(this.HomeDesktop_MouseLeave);
                screenFrame.MouseDown += new System.Windows.Forms.MouseEventHandler(this.HomeDesktop_MouseDown);
                ScreensPanel.Controls.Add(screenFrame);
                index++;
            }
            //new Thread(() => miniatureWorker()).Start();
        }

        void miniatureWorker()
        {
            while (Program.inProgress)
            {
                Thread.Sleep(100);
                for (int i = 0; i < ScreensPanel.Controls.Count; i++)
                {
                    ((PictureBox)ScreensPanel.Controls[i]).Image = Helpers.GetScreenshot(i, ScreensPanel.Controls[i].Width, ScreensPanel.Controls[i].Height);
                }
            }
        }

        private void HomeDesktop_MouseEnter(object sender, EventArgs e)
        {
            //Program.PostLog("Enter");
            BackColor = SystemColors.ControlDark;
            Cursor = Cursors.Hand;
        }

        private void HomeDesktop_MouseLeave(object sender, EventArgs e)
        {
            //Program.PostLog("Leave "+ RectangleToScreen(ClientRectangle).ToString()                + " " + Cursor.Position.ToString());
            BackColor = SystemColors.Control;
            Cursor = Cursors.Default;
        }

        public const int WM_LBUTTONDOWN = 0x0201; //WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        private void HomeDesktop_MouseDown(object sender, MouseEventArgs e)
        {
            this.BringToFront();
            //Capture = true;
            mouseLDownPoint = new Point(e.X, e.Y);
            this.DoDragDrop(this, DragDropEffects.Move);
        }
    }
}

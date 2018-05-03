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
        string host;
        public HostDesktop(string hostName)
        {
            host = hostName;
            InitializeComponent();
            FillData();
        }

        public void FillData()
        {
            int currentLeft = 10;
            int ownerHeight = ScreensPanel.Height-10;
            int index = 0;
            int maxHeight = Screen.AllScreens.Max(x => x.Bounds.Height);
            int dh = maxHeight / ownerHeight;
            label1.Text = host;
            foreach (Screen screen in Screen.AllScreens)
            {
                Rectangle bounds = screen.Bounds;
                PictureBox screenFrame = new PictureBox();
                screenFrame.Left = currentLeft;
                screenFrame.Height = bounds.Height / dh;
                screenFrame.Width = bounds.Width / dh;
                screenFrame.BorderStyle = BorderStyle.FixedSingle;
                currentLeft += screenFrame.Width + 10;
                screenFrame.Image = Helpers.GetScreenshot(index, screenFrame.Width, screenFrame.Height);
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

        private void panel1_MouseEnter(object sender, EventArgs e)
        {
            BackColor = SystemColors.ControlDark;
            Cursor = Cursors.Hand;
        }

        private void panel1_MouseLeave(object sender, EventArgs e)
        {
            BackColor = SystemColors.Control;
            Cursor = Cursors.Default;
        }
    }
}

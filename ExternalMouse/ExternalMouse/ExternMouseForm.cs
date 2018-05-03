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
    public partial class ExternMouseForm : Form
    {
        public ExternMouseForm()
        {
            InitializeComponent();
            PostLog("Paired host " + Properties.Settings.Default.pairedHost);
            PostLog("Port " + Properties.Settings.Default.listenPort);
        }

        public void PostLog(string str)
        {
            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke((MethodInvoker)(()=> listBox1.Items.Insert(0, str)));
            }
            else
                listBox1.Items.Insert(0,str);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Bitmap bmp = new Bitmap(500, 500);
            //Program.Hook.SendScreenShot("192.168.1.10", bmp);
        }
    }
}

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
    public partial class ShowPasskey : Form
    {
        public ShowPasskey()
        {
            InitializeComponent();
        }

        public new void Show()
        {
            Random rnd = new Random();
            Program.passkey = rnd.Next(10000, 99999).ToString();
            //Program.udpConnector.SetCodeword(Program.passkey);
            if (!this.IsHandleCreated) this.CreateHandle();
            this.Invoke( new MethodInvoker(() =>
            {
                label2.Text = Program.passkey;
                base.ShowDialog();
            }));
        }
    }
}

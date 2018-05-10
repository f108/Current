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
    public partial class CodewordInputDialog : Form
    {
        public CodewordInputDialog()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
           // Program.udpConnector.SetCodeword(textBox1.Text);
            Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;
using System.Windows.Input;
using System.Reflection;
using Microsoft.Win32;

namespace gmusic
{

    public partial class PlayerForm : Form
    {
        int oneGoogleWrapperHeight;
        Rectangle oneGoogleWrapperRect;
        bool oneGoogleWrapperMustBeHidden = true;
        bool isFirstTimeActivation = true;
        int F11LastDown;
        int ZoomFactor = 100;

        private void RegisterHotKey()
        {

        }

        public PlayerForm()
        {
            InitializeComponent();
            //SendKeys.SendWait("^-");
        }

        private bool isWidthExist()
        {
            Microsoft.Win32.RegistryKey rk;
            try
            {
                rk = Registry.CurrentUser.OpenSubKey(Program.regpath);
            }
            catch { return false; };
            try
            {
                int p = (int)rk.GetValue("Width");
            }
            catch { return false; };
            return true;
        }
        private void Init()
        {
            Microsoft.Win32.RegistryKey rk;
            try
            {
                rk = Registry.CurrentUser.OpenSubKey(Program.regpath);
            }
            catch { return; };
            try
            {
                Width = (int)rk.GetValue("Width");
                Height = (int)rk.GetValue("Height");
            }
            catch { };
            try
            {
                Top = (int)rk.GetValue("Top");
                Left = (int)rk.GetValue("Left");
            }
            catch { };
            try
            {
                ZoomFactor = (int)rk.GetValue("Zoom");
            }
            catch { };
        }

        public void AddLine(string str)
        {
            textBox1.Invoke((MethodInvoker)(() => textBox1.AppendText(str)));
        }

        public void pressKey(string keyname)
        {
            try
            {
                HtmlElement el = webBrowser1.Document.GetElementById(keyname);//
                el.InvokeMember("click");
            }
            catch { };
        }

        public void pressSpecKey()
        {
            HtmlElement el = webBrowser1.Document.GetElementById("doc");//
            Object[] args = new Object[2];
            args[0] = (Object)"-1";
            args[1] = (Object)"0";
            el.InvokeMember("keydown", args);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int k = 0;
            showGoogleWrapper();

        }

        private void hideGoogleWrapper()
        {
            string str = webBrowser1.Document.GetElementById("oneGoogleWrapper").GetAttribute("clientheight");
            //AddLine("leave "+str);
            if (str.Equals("0")) return;
            oneGoogleWrapperHeight = Int32.Parse(str);
            webBrowser1.Document.GetElementById("oneGoogleWrapper").Style = "display:none; height: 0px;";// visibility: hidden;";

            str = webBrowser1.Document.GetElementById("doc").GetAttribute("clientheight");
            webBrowser1.Document.GetElementById("doc").Style = "height: " + (Int32.Parse(str) + oneGoogleWrapperHeight).ToString() + "px; "; //transform:scale(0.5);";
            //webBrowser1.Document.GetElementsByTagName("body")[0].SetAttribute("font-size", "75%");//zoom: 0.75;";
            oneGoogleWrapperMustBeHidden = true;
        }
        private void showGoogleWrapper()
        {
            string str = webBrowser1.Document.GetElementById("oneGoogleWrapper").GetAttribute("clientheight");
            if (!str.Equals("0")) return;
            webBrowser1.Document.GetElementById("oneGoogleWrapper").Style = "height: " + oneGoogleWrapperHeight + "px; display:block;";
            str = webBrowser1.Document.GetElementById("doc").GetAttribute("clientheight");
            webBrowser1.Document.GetElementById("doc").Style = "height: " + (Int32.Parse(str) - oneGoogleWrapperHeight).ToString() + "px;";
            oneGoogleWrapperRect = webBrowser1.Document.GetElementById("oneGoogleWrapper").ClientRectangle;
            oneGoogleWrapperMustBeHidden = false;
        }
        private void document_MouseLeave(object sender, HtmlElementEventArgs e)
        {
            HtmlElement element = e.FromElement;
            //AddLine("leave");
            //if (!oneGoogleWrapperRect.Contains(e.MousePosition)) 
                hideGoogleWrapper();
        }

        private void navbar_MouseHover(object sender, HtmlElementEventArgs e)
        {
            int width = webBrowser1.Document.GetElementById("headerBar").ClientRectangle.Width;
            int x = e.MousePosition.X;
            //AddLine("\r\nMouseMove "+width+" "+x);
            if (x > width - 26) showGoogleWrapper();
            //hideGoogleWrapper();
        }
        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            try
            {
                Rectangle width = webBrowser1.Document.GetElementById("doc").ClientRectangle;

                Rectangle cc = webBrowser1.ClientRectangle;

                
            }
            catch { };

            if (isFirstTimeActivation)
            {
                Zoom(ZoomFactor);

               isFirstTimeActivation = false;
                //SendKeys.SendWait("^-");
                Rectangle docRect = webBrowser1.Document.GetElementById("doc").ClientRectangle;
                Rectangle clRect = webBrowser1.ClientRectangle;
                if (clRect.Width < 940 && !isWidthExist())
                {
                    this.Width += (940 - clRect.Width) * clRect.Width / docRect.Width;
                }


                //if (webBrowser1. webBrowser1.ClientRectangle.Width)
               //webBrowser1.Document.Body.Style = "zoom: 120%";//-ms-transform: scale(2);";//scale:80%";
                   //webBrowser1.Scale(new SizeF(10, 10));
            }

            try
            {
                string Song = webBrowser1.Document.GetElementById("playerSongTitle").InnerText;
                string Artist = webBrowser1.Document.GetElementById("player-artist").InnerText;
                Text = Artist + " " + Song;
            }
            catch { };

            try
            {
                webBrowser1.Document.GetElementById("oneGoogleWrapper").MouseLeave -= new HtmlElementEventHandler(document_MouseLeave);
                webBrowser1.Document.GetElementById("oneGoogleWrapper").MouseLeave += new HtmlElementEventHandler(document_MouseLeave);
                if (oneGoogleWrapperMustBeHidden) hideGoogleWrapper();
            }
            catch { };

            try
            {
                webBrowser1.Document.GetElementById("headerBar").MouseMove -= new HtmlElementEventHandler(navbar_MouseHover);
                webBrowser1.Document.GetElementById("headerBar").MouseMove += new HtmlElementEventHandler(navbar_MouseHover);
            }
            catch { };

            try
            {
                HtmlElementCollection coll = webBrowser1.Document.GetElementById("player").GetElementsByTagName("button");
                string str;
                if (coll.Count != 0)
                {
                    int k = 0;
                    for (int i = 0; i < coll.Count; i++)
                    {
                        try
                        {
                            str = coll[i].GetAttribute("data-id");
                            coll[i].SetAttribute("id", str);
                        }
                        catch { };
                    }
                }
            }
            catch { };
        }

        private void webBrowser1_DocumentTitleChanged(object sender, EventArgs e)
        {
            try
            {
                string str = webBrowser1.Document.Title;
                //string Song = webBrowser1.Document.GetElementById("playerSongTitle").InnerText;
                //string Artist = webBrowser1.Document.GetElementById("player-artist").InnerText;
                //Text = Artist + " " + Song;
                Text = str.Replace(" - Google Play Music","");
            }
            catch { };
        }
        public void Zoom(int factor)
        {
            int pr = factor;
            Type obj = webBrowser1.ActiveXInstance.GetType();
            obj.InvokeMember(@"ExecWB", BindingFlags.InvokeMethod, null, 
                webBrowser1.ActiveXInstance, new object[] { 63, 2, pr, IntPtr.Zero });
        }
        private void webBrowser1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.F11)
            {
                if (Environment.TickCount - F11LastDown > 100)
                {
                    F11LastDown = Environment.TickCount;
                    if (WindowState != FormWindowState.Maximized)
                    {
                        //Bounds = Screen.PrimaryScreen.Bounds;
                        FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                        WindowState = FormWindowState.Normal;
                        TopMost = true;
                        WindowState = FormWindowState.Maximized;
                    }
                    else
                    {
                        WindowState = FormWindowState.Normal;
                        TopMost = false;
                        FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
                    }
                    return;
                }
            }

            if (e.KeyData == ( Keys.OemMinus | Keys.Control))
            {
                ZoomFactor -= 2;
                Zoom(ZoomFactor);
                e.IsInputKey = true;
                Microsoft.Win32.RegistryKey rk = Registry.CurrentUser.CreateSubKey(Program.regpath);
                rk.SetValue("Zoom", ZoomFactor);
            }
            else if (e.KeyData == (Keys.Oemplus | Keys.Control))
            {
                ZoomFactor += 2;
                Zoom(ZoomFactor);
                e.IsInputKey = true;
                Microsoft.Win32.RegistryKey rk = Registry.CurrentUser.CreateSubKey(Program.regpath);
                rk.SetValue("Zoom", ZoomFactor);
            }


            if (e.KeyCode != Keys.ShiftKey)
            {
                int k = 0;
            }

           // if (e.KeyCode == Keys.OemMinus && e.Modifiers == ModifierKeys.)

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F11)
            {
                if (WindowState != FormWindowState.Maximized)
                {
                    WindowState = FormWindowState.Normal;
                }
                else
                {
                    WindowState = FormWindowState.Maximized;
                }

            }
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (isFirstTimeActivation) return;
            try
            {
                int w = Width;
                int h = Height;
                Microsoft.Win32.RegistryKey rk = Registry.CurrentUser.CreateSubKey(Program.regpath);
                rk.SetValue("Width", w);
                rk.SetValue("Height", h);
            }
            catch { };
        }

        private void Form1_LocationChanged(object sender, EventArgs e)
        {
            if (isFirstTimeActivation) return;
            try
            {
                int t = Top;
                int l = Left;
                Microsoft.Win32.RegistryKey rk = Registry.CurrentUser.CreateSubKey(Program.regpath);
                rk.SetValue("Top", t);
                rk.SetValue("Left", l);
            }
            catch { };
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            Init();
            
        }

    }
}


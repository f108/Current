using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace gmusic
{
    static class Program
    {
        static globalKeyboardHook Hook = new globalKeyboardHook();
        static bool inProgress = true;
        public static PlayerForm form;
        public const string regpath = "Software\\indepico\\GMO Player";
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Thread workerProc = new Thread(new ThreadStart(HookLoop));
            workerProc.Start();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new PlayerForm();
            Application.Run(form);
            inProgress = false;
            Hook.PowerPressed.Set();
        }
        static private void HookLoop()
        {
            for (; inProgress; )
            {
                Hook.PowerPressed.WaitOne();
                //form.SetText(Hook.strq.Dequeue());

            };
        }
        static public void PlayPause()
        {
            form.Invoke((MethodInvoker)(() => form.pressKey("play-pause")));
            //new PopupImage(global::PowerCfgAcer.Properties.Resources.Settings_Wi_Fi_icon).Show();
        }
        static public void NextTrack()
        {
            form.Invoke((MethodInvoker)(() => form.pressKey("forward")));
            //new PopupImage(global::PowerCfgAcer.Properties.Resources.Settings_Wi_Fi_icon).Show();
        }
        static public void PrevTrack()
        {
            form.Invoke((MethodInvoker)(() => form.pressKey("rewind")));
            //new PopupImage(global::PowerCfgAcer.Properties.Resources.Settings_Wi_Fi_icon).Show();
        }
        static public void Forward()
        {
            //shiftLeftRight(0x27);   
            //new PopupImage(global::PowerCfgAcer.Properties.Resources.Settings_Wi_Fi_icon).Show();
        }
        static public void Backward()
        {
            //shiftLeftRight(0x25);   
            //new PopupImage(global::PowerCfgAcer.Properties.Resources.Settings_Wi_Fi_icon).Show();
        }

        [DllImport("User32.Dll", EntryPoint = "PostMessageA")]
        static extern bool PostMessage(
            IntPtr hWnd,
            uint msg,
            uint wParam,
            uint lParam
            );

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        static private IntPtr GetIEServer(IntPtr current)
        {
            IntPtr hWnd = FindWindowEx(current, IntPtr.Zero, "Internet Explorer_Server", null);
            if (hWnd!=IntPtr.Zero) return hWnd;
            hWnd = FindWindowEx(current, IntPtr.Zero, null, null);
            IntPtr ret;
            for (; hWnd!=IntPtr.Zero; )
            {
                ret = GetIEServer(hWnd);
                if (ret != IntPtr.Zero) return ret;
                hWnd = FindWindowEx(current, hWnd, null, null);
            }
            return IntPtr.Zero;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        static private void shiftLeftRight(int key)
        {
            form.pressSpecKey();
            return;

            unsafe
            {
                form.AddLine("hi");

                IntPtr prevFocus = GetForegroundWindow();

                SetForegroundWindow(form.Handle);

                SendKeys.SendWait("+{RIGHT}");

                /*IntPtr phWnd = (IntPtr)form.Handle.ToPointer();
                IntPtr hWnd = GetIEServer(phWnd); //"Internet Explorer_Server"
                PostMessage(hWnd, 0x0100, 0x10, 0x002A0001);
                PostMessage(hWnd, 0x0100, 0x47, 0x014D0001);
                PostMessage(hWnd, 0x0101, 0x47, 0xC14D0001);
                PostMessage(hWnd, 0x0101, 0x10, 0xC02A0001);*/

                SetForegroundWindow(prevFocus);
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics; 

namespace PowerCfgAcer
{
    static class Program
    {
        static globalKeyboardHook Hook = new globalKeyboardHook();
        static Form1 form;
        static int lockTrigger = 0;
        
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
            form = new Form1();
            //form.Show();
            Application.Run();
        }

        static private void HookLoop()
        {
            for (; ; )
            {
                Hook.PowerPressed.WaitOne();
                //form.SetText(Hook.strq.Dequeue());

            };
        }

        static void ExecCmd(String command, String arguments)
        {
            var p = new Process();
            p.StartInfo.FileName = command;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.UseShellExecute = false;  
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
        }

        static public void SetPower0()
        {
            ExecCmd(@"C:\\Windows\\system32\\powercfg.exe", "-SETACTIVE " + "a1841308-3541-4fab-bc81-f71556f20b4a");
            new PopupImage(global::PowerCfgAcer.Properties.Resources.web_icon).Show();
        }

        static public void SetPower1()
        {
            ExecCmd(@"C:\\Windows\\system32\\powercfg.exe", "-SETACTIVE " + "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
            new PopupImage(global::PowerCfgAcer.Properties.Resources.internet).Show();
        }

        static public void SetWiFiOn()
        {
            new PopupImage(global::PowerCfgAcer.Properties.Resources.Settings_Wi_Fi_icon).Show();
        }

        static public void SetWiFiOff()
        {
            new PopupImage(global::PowerCfgAcer.Properties.Resources.WiFi_Disable).Show();
        }

        static public void SetBthOn()
        {
            new PopupImage(global::PowerCfgAcer.Properties.Resources.Bluetooth_Enable).Show();
        }

        static public void SetBthOff()
        {
            new PopupImage(global::PowerCfgAcer.Properties.Resources.Bluetooth_Disable).Show();
        }

        static public void StartBackup()
        {
            new PopupImage(global::PowerCfgAcer.Properties.Resources.Backup_center_icon).Show();
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool LockWorkStation();

        static public void LockWs(int wParam, int time)
        {
            if (wParam == 0x100) { lockTrigger = time; return; };
            if (wParam == 0x101)
            {
                if (time - lockTrigger > 1000) SetPower0();
                new PopupImage(global::PowerCfgAcer.Properties.Resources._20120817051135634_easyicon_cn_256).Show();
                LockWorkStation();
            }
        }

    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExternalMouse
{
    static class Program
    {
        public static int ScreenshotHeight = 90;
        static public bool inProgress = true;
        public static MouseKeyboardControl Hook = new MouseKeyboardControl();
        public static UdpConnector udpConnector = new UdpConnector();
        public static ExternMouseForm emf;
        public static DesktopsForm destopsForm;
        public static ShowPasskey showPasskey;
        public static PairedHosts pairedHosts;// = new PairedHosts();

        public static string GroupName;
        public static string passkey;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            GroupName = Properties.Settings.Default.GroupName;

            udpConnector.Open();

            showPasskey = new ShowPasskey();
            destopsForm = new DesktopsForm();
            destopsForm.Show();

            pairedHosts = new PairedHosts();

            emf = new ExternMouseForm();
            Application.Run(emf);
            inProgress = false;

            udpConnector.Close();
        }

        public static void ShowPasskey()
        {
            showPasskey.Show();
        }
        static public void PostLog(string str)
        {
            try
            {
                emf.PostLog(str);
            } catch { };
        }

    }
}

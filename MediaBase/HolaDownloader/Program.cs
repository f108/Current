using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace HolaDownloader
{
    static class Program
    {
        public const string connectionString = "";
        public static string RootPath_FavCommunity = "";
        public static string RootPath_FavUsers = "";
        public static string RootPath_Fav = "";
        public static bool AppWasClosed = false;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern ulong GetTickCount64();
        

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new HolaDownloader());
        }
    }
}

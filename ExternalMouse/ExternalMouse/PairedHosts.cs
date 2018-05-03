using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;

namespace ExternalMouse
{
    class PairedHosts
    {
        public class Host
        {
            public struct ScreenInfo
            {
                public int Width;
                public int Height;
            };

            public string Name;
            public IPAddress ipAddress;
            public int Width = 0;
            public int MaxHeight;
            public int ScreenCount = 0;
            public List<ScreenInfo> screenInfo = new List<ScreenInfo>();
            public bool IsActive = false;
            string AEScodeword;
            AES AESCrypto = new AES();

            public string GetConfig()
            {
                string ret = "";
                ret += Name + "|" + "ip" + "|" + AEScodeword + "|" + Width + "|" + ScreenCount + "|";
                foreach(ScreenInfo sc in screenInfo)
                {
                    ret += sc.Width + "|" + sc.Height;
                }
                return ret;
            }
        };

        private ArrayList _pairedHost = new ArrayList();
        private int LocalDesktopIndex = 0;

        public string GetLocalHostInfo()
        {
            return ((Host)_pairedHost[LocalDesktopIndex]).GetConfig();
        }
        public PairedHosts()
        {
            Host host = new Host();
            host.Name = Environment.MachineName;
            host.MaxHeight = Screen.AllScreens.Max(x => x.Bounds.Height);
            foreach (Screen screen in Screen.AllScreens)
            {
                host.ScreenCount++;
                host.Width += screen.WorkingArea.Width;
                host.screenInfo.Add(new Host.ScreenInfo
                {
                    Width = screen.WorkingArea.Width,
                    Height = screen.WorkingArea.Height
                });
            }
            _pairedHost.Add(host);
            SaveConfig();
        }

        public void SaveConfig()
        {
            System.Collections.Specialized.StringCollection stringCollection = new System.Collections.Specialized.StringCollection();
            string config;
            foreach (Host host in _pairedHost)
            {
                config = host.GetConfig();
                stringCollection.Add(config);
            }
            Properties.Settings.Default.PairedHosts = stringCollection;
            Properties.Settings.Default.Save();
        }
    }
}

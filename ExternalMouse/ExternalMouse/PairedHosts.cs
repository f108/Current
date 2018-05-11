using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;

namespace ExternalMouse
{
    public delegate void ControlReorderCallback(List<string> neworder);
    public class PairedHosts
    {
        public const byte MSG_PING                     = 0x01;
        public const byte MSG_PINGRESPONSE             = 0x02;
        public const byte MSG_REQUESTDESKTOPPARAMS     = 0x03;
        public const byte MSG_RESPONSEDESKTOPPARAMS    = 0x04;
        public const byte MSG_REQUESTSCREENSHOT        = 0x05;
        public const byte MSG_RESPONSESCREENSHOT       = 0x06;

        private Dictionary<IPAddress, Host> _pairedHost = new Dictionary<IPAddress, Host>();

        public int LeftBound;
        public int RightBound;
        public int LocalLeftBound;
        public int LocalRightBound;

        public string GetLocalHostInfo()
        {
            return _pairedHost[IPAddress.Loopback].GetConfig();
        }
        public PairedHosts()
        {
            Program.destopsForm.controlReorderCallback = ControlReorderCallback;
            Host host = new Host
            {
                MachineName = Environment.MachineName,
                ipAddress = IPAddress.Loopback,
                MaxHeight = Screen.AllScreens.Max(x => x.Bounds.Height)
            };
            int dh = host.MaxHeight / Program.ScreenshotHeight;
            foreach (Screen screen in Screen.AllScreens)
            {
                host.Width += screen.WorkingArea.Width;
                host.screenInfo.Add(new Host.ScreenInfo
                {
                    Width = screen.WorkingArea.Width,
                    Height = screen.WorkingArea.Height,
                    Screenshot = Helpers.GetScreenshot(host.ScreenCount, screen.WorkingArea.Width / dh, screen.WorkingArea.Height / dh)
                });
                host.ScreenCount++;
            }
            host.isLocalhost = true;
            _pairedHost.Add(host.ipAddress, host);
            LocalRightBound = _pairedHost[IPAddress.Loopback].Width;
            Program.destopsForm.AddOrUpdate(host);
            SaveConfig();
        }

        void ControlReorderCallback(List<string> neworder)
        {
            int localhostIndex = neworder.IndexOf(IPAddress.Loopback.ToString());
            if (localhostIndex < 0) return;
            LocalLeftBound = 0;
            LocalRightBound = _pairedHost[IPAddress.Loopback].Width;
            int bound = 0;
            for (int i=localhostIndex; i<neworder.Count; i++)
            {
                Host host = _pairedHost[IPAddress.Parse(neworder[i])];
                host.Position = i - localhostIndex;
                host.LeftBound = bound;
                bound += host.Width;
                host.RightBound = bound;
                bound += 1;
                Program.PostLog("Reorder: host " + host.ipAddress.ToString() + " " + host.LeftBound + "-" + host.RightBound);
            }
            RightBound = bound;
            bound = -1;
            for (int i = localhostIndex-1; i>=0; i--)
            {
                Host host = _pairedHost[IPAddress.Parse(neworder[i])];
                host.Position = i - localhostIndex;
                host.RightBound = bound;
                bound -= host.Width;
                host.LeftBound = bound;
                bound -= 1;
                Program.PostLog("Reorder: host " + host.ipAddress.ToString() + " " + host.LeftBound + "-" + host.RightBound);
            }
            LeftBound = bound+1;
        }

        public bool isLocalDesktop(int x, int y)
        {
            //Program.PostLog("Local area: " + LocalLeftBound + "-" + LocalRightBound + " check:"+x);
            return LocalLeftBound <= x && x <= LocalRightBound;
        }

        public bool CheckAndSendIfExternalDesktop(byte[] data, int x, int y)
        {
            Program.PostLog("Bounds: " + LeftBound + "-" + RightBound + " check:" + x);
            IEnumerable<Host> host = _pairedHost.Values.Where(h => h.LeftBound <= x && x <= h.RightBound && !h.isLocalhost);
            if (host.Count() < 1 || host.First().isLocalhost) return false;
            Program.PostLog("SEND: " + host.First().ipAddress.ToString()+ "  x=" + x);
            host.First().Send(data);
            return true;
        }
        public void SaveConfig()
        {
            System.Collections.Specialized.StringCollection stringCollection = new System.Collections.Specialized.StringCollection();
            string config;
            /*foreach (Host host in _pairedHost)
            {
                config = host.GetConfig();
                stringCollection.Add(config);
            }*/
            Properties.Settings.Default.PairedHosts = stringCollection;
            Properties.Settings.Default.Save();
        }
        
        public void AddHostByIP(IPAddress address)
        {
            if (_pairedHost.ContainsKey(address)) return;
            Host host = new Host
            {
                ipAddress = address,
                Name = address.ToString()
            };

            _pairedHost.Add(host.ipAddress, host);
            Program.destopsForm.AddOrUpdate(host);
        }

        public void InitiateKeyExchange(IPAddress address)
        {
            _pairedHost[address]?.InitiateKeyExchange();
        }

        public void Ping(IPAddress address)
        {
            try
            {
                _pairedHost[address]?.Ping();
            }
            catch { };
        }

        public bool Contains(IPAddress address)
        {
            return _pairedHost.ContainsKey(address) ? true : false;
        }
        public bool TryProcessMessage(IPAddress ipaddress, byte[] bytes)
        {

            if (!Contains(ipaddress)) return false;
            Host host = _pairedHost[ipaddress];
            byte[] data;

            try // try to decrypt data as AES
            {
                Program.PostLog("Try to decrypt as AES");
                data = host.AESCrypto.Decrypt(bytes);
                try
                {
                    if (!CheckAndProcessServiceMessage(ipaddress, data))
                        MouseKeyboardControl.ProcessReceivedMessage(data);
                }
                catch (Exception E)
                {
                    Program.PostLog("Parsing message; " + E.Message);
                };
                return true;
            }
            catch (Exception E) {
                
                Program.PostLog("Exception @AES: " + E.Message);
            };

            try // try to decrypt data as RSA and import AES
            {
                data = host.RSA.Decrypt(bytes, true);
                host.AESCrypto.SetKey(data);
                Program.PostLog("AES key imported "+ data[0]);
                host.Ping();
                host.MakeRequestDesktopParams();
                return true;
            }
            catch (Exception E)
            {
                Program.PostLog("Exception @RSA import: " + E.Message);
            };

            try // try to import public RSA key and return AES key
            {
                string remotePublicKey = Encoding.ASCII.GetString(bytes);
                Program.PostLog("Try to import public key " + remotePublicKey);
                RSACryptoServiceProvider RemoteRSA = new RSACryptoServiceProvider();
                RemoteRSA.FromXmlString(remotePublicKey);
                data = host.AESCrypto.GenerateAndSetRandomKey();
                Program.PostLog("AES key generated and send " + data[0]);
                data = RemoteRSA.Encrypt(data, true);
                Program.udpConnector.Send(ipaddress, data);
                return true;
            }
            catch (Exception E)
            {
                Program.PostLog("Exception @RSA: " + E.Message);
            };

            return false;
        }

        private bool CheckAndProcessServiceMessage(IPAddress address, byte[] data)
        {
            if (data[0] != (byte)MouseKeyboardControl.InputType.CONTROL) return false;
            Host host = _pairedHost[address];
            Program.PostLog("CMD byte "+ data[1]);
            switch (data[1])
            {
                case MSG_PING:
                    Program.PostLog("ping received ");
                    host.Send(new byte[] { (byte)MouseKeyboardControl.InputType.CONTROL, MSG_PINGRESPONSE, 0x00, 0x00 });
                    break;
                case MSG_PINGRESPONSE:
                    Program.PostLog("ping response received ");
                    host.LastPing = Environment.TickCount;
                    break;
                case MSG_REQUESTDESKTOPPARAMS:
                    Program.PostLog("MSG_REQUESTDESKTOPPARAMS received ");
                    host.DoResponseDesktopParams(_pairedHost[IPAddress.Loopback]);
                    break;
                case MSG_RESPONSEDESKTOPPARAMS:
                    Program.PostLog("MSG_DESKTOPPARAMS received ");
                    host.DoParseDesktopParams(data);
                    Program.destopsForm.AddOrUpdate(host);
                    break;
                case MSG_REQUESTSCREENSHOT:
                    Program.PostLog("MSG_REQUESTSCREENSHOT received ");
                    host.DoResponseScreenshotRequest(_pairedHost[IPAddress.Loopback], data);
                    break;
                case MSG_RESPONSESCREENSHOT:
                    Program.PostLog("========MSG_RESPONSESCREENSHOT received ");
                    host.DoParseScreenshotResponse(data);
                    break;


            }

            return true;
        }
    }
}

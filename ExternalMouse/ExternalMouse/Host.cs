using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.IO;

namespace ExternalMouse
{
    public delegate void ControlHostUpdateCallback();
    public class Host
    {
        public ControlHostUpdateCallback controlHostUpdateCallback;

        public class ScreenInfo
        {
            public int Width;
            public int Height;
            public Image Screenshot;

            public void SetScreenshot(Image img)
            {
                Screenshot = img;

            }
        };

        public string Name;
        public string MachineName;
        public IPAddress ipAddress;
        public int Position;
        public int Width = 0;
        public int LeftBound;
        public int RightBound;
        public int MaxHeight;
        public int ScreenCount = 0;
        public List<ScreenInfo> screenInfo = new List<ScreenInfo>();
        public bool IsActive = false;
        public AES AESCrypto = new AES();
        public RSACryptoServiceProvider RSA = new RSACryptoServiceProvider(1024);
        public int LastPing;
        public string GetConfig()
        {
            string ret = "";
            ret += Name + "|" + "ip" + "|" + "" + "|" + Width + "|" + ScreenCount + "|";
            foreach (ScreenInfo sc in screenInfo)
            {
                ret += sc.Width + "|" + sc.Height;
            }
            return ret;
        }

        public void SaveToBinaryWriter(BinaryWriter bw)
        {
            bw.Write(MachineName);
            bw.Write((Int32)Width);
            bw.Write((Int32)MaxHeight);
            bw.Write((Int32)ScreenCount);
            foreach (ScreenInfo si in screenInfo)
            {
                bw.Write((Int32)si.Width);
                bw.Write((Int32)si.Height);
            }
        }

        public void LoadFromBinaryReader(BinaryReader br)
        {
            MachineName = br.ReadString();
            Width = br.ReadInt32();
            MaxHeight = br.ReadInt32();
            ScreenCount = br.ReadInt32();
            screenInfo.Clear();
            for (int i = 0; i < ScreenCount; i++)
            {
                screenInfo.Add(new ScreenInfo
                {
                    Width = br.ReadInt32(),
                    Height = br.ReadInt32()
                });

            }
        }

        static ImageCodecInfo myImageCodecInfo;
        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }
        public void SaveScreenShot(BinaryWriter bw, int RemoteMaxHeight)
        {
            bw.Write((Int32)Screen.AllScreens.Count());

            int ownerHeight = RemoteMaxHeight;
            int index = 0;
            int maxHeight = Screen.AllScreens.Max(x => x.Bounds.Height);
            int dh = maxHeight / ownerHeight;
            bw.Flush();
            foreach (Screen screen in Screen.AllScreens)
            {
                Rectangle bounds = screen.Bounds;
                Image img = Helpers.GetScreenshot(index, bounds.Width / dh, bounds.Height / dh);
                index++;
                MemoryStream ms = new MemoryStream();
                EncoderParameter myEncoderParameter;
                EncoderParameters myEncoderParameters = new EncoderParameters(1);
                System.Drawing.Imaging.Encoder myEncoder;
                myEncoder = System.Drawing.Imaging.Encoder.Quality;
                myEncoderParameter = new EncoderParameter(myEncoder, 75L);
                myEncoderParameters.Param[0] = myEncoderParameter;
                img.Save(ms, GetEncoderInfo("image/jpeg"), myEncoderParameters);
                byte[] buf = ms.ToArray();

                bw.Write((UInt32)buf.Length);
                bw.Write(buf, 0, (int)ms.Length);
                bw.Flush();
            }
        }

        public void InitiateKeyExchange()
        {
            string str = RSA.ToXmlString(false);
            byte[] publickey = Encoding.ASCII.GetBytes(str);
            Program.udpConnector.Send(ipAddress, publickey);
            Program.PostLog("Public key sended" + str);
        }

        public void Ping()
        {
            byte[] data = { (byte)MouseKeyboardControl.InputType.CONTROL, PairedHosts.MSG_PING, 0x00, 0x00 };
            Send(data);
            Program.PostLog("ping send");
        }

        public void MakeRequestDesktopParams()
        {
            byte[] data = { (byte)MouseKeyboardControl.InputType.CONTROL, PairedHosts.MSG_REQUESTDESKTOPPARAMS, 0x00, 0x00 };
            Send(data);
            Program.PostLog("ping send");
        }

        public void DoResponseDesktopParams(Host host)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((byte)MouseKeyboardControl.InputType.CONTROL);
                bw.Write((byte)PairedHosts.MSG_RESPONSEDESKTOPPARAMS);
                bw.Write((byte)0);
                bw.Write((byte)0);
                host.SaveToBinaryWriter(bw);
                Send(ms.ToArray());
            }
            if (screenInfo.Count<1)
                MakeRequestDesktopParams();
            Program.PostLog("ResponseDesktopParams send");
        }

        public void DoParseDesktopParams(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(ms))
            {
                br.ReadUInt32();
                LoadFromBinaryReader(br);
            };
            Program.destopsForm.AddOrUpdate(this);
            MakeScreenshotRequest();
            Program.PostLog("ping send");
        }

        public void MakeScreenshotRequest()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((byte)MouseKeyboardControl.InputType.CONTROL);
                bw.Write((byte)PairedHosts.MSG_REQUESTSCREENSHOT);
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((Int32)Program.ScreenshotHeight);
                Send(ms.ToArray());
            }
            Program.PostLog("MakeScreenshotRequest send");
        }

        public void DoResponseScreenshotRequest(Host host, byte[] data)
        {
            int RemoteMaxHeight;
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(ms))
            {
                br.ReadUInt32();
                RemoteMaxHeight = br.ReadInt32();
            };

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((byte)MouseKeyboardControl.InputType.CONTROL);
                bw.Write((byte)PairedHosts.MSG_RESPONSESCREENSHOT);
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Flush();
                SaveScreenShot(bw, RemoteMaxHeight);
                Send(ms.ToArray());
            }
            Program.PostLog("DoResponseScreenshotRequest send");
        }

        public void DoParseScreenshotResponse(byte[] data)
        {
            int ScreenCount;
            Image img;
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(ms))
            {
                br.ReadUInt32();
                ScreenCount = br.ReadInt32();
                for (int i=0; i<ScreenCount; i++)
                {
                    int msLength = (int)br.ReadUInt32();
                    byte[] buf = br.ReadBytes(msLength);
                    MemoryStream mss = new MemoryStream(buf);
                    img = new Bitmap(mss);
                    screenInfo[i].SetScreenshot(img);
                }
            };
            controlHostUpdateCallback();
            Program.destopsForm.AddOrUpdate(this);
        }

        public void Send(byte[] data)
        {
            byte[] buf = AESCrypto.Encrypt(data);
            Program.udpConnector.Send(ipAddress, buf);
        }
    };
}

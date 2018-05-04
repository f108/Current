using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace ExternalMouse
{
    static class Helpers
    {
        static public byte[] GetHostInfo()
        {
            byte[] ret;
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(Environment.MachineName);
                bw.Write((Int16)Screen.AllScreens.Length);
                for (int i=0; i<Screen.AllScreens.Length; i++)
                {
                    bw.Write((Int32)Screen.AllScreens[i].Bounds.Width);
                    bw.Write((Int32)Screen.AllScreens[i].Bounds.Height);
                };
                ret = ms.ToArray();
            }
            return ret;
        }
        static public Bitmap GetScreenshot(int ScreenIndex, int Width, int Height)
        {
            Bitmap ret = new Bitmap(Width, Height);
            Screen screen = Screen.AllScreens[ScreenIndex];
            using (Bitmap bmpScreen = new Bitmap(screen.Bounds.Width, screen.Bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bmpScreen))
                {
                    g.CopyFromScreen(screen.Bounds.X, screen.Bounds.Y, 0, 0, bmpScreen.Size, CopyPixelOperation.SourceCopy);
                };

                using (Graphics g = Graphics.FromImage(ret))
                {
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.Default;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                    Rectangle rectangle = new Rectangle(0, 0, Width, Height);
                    g.DrawImage(bmpScreen, rectangle);
                }
            }
            return ret;
        }

    }
}

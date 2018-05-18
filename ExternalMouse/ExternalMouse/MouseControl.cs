using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ExternalMouse
{
    class MouseKeyboardControl
    {
        public AutoResetEvent PowerPressed;
        public Queue<String> strq = new Queue<string>();

        public delegate int mouseHookProc(int code, uint wParam, ref MSLLHOOKSTRUCT lParam);
        public delegate int keyboardHookProc(int code, uint wParam, ref KBDLLHOOKSTRUCT lParam);

        const int WH_MOUSE_LL = 14;
        const int WH_KEYBOARD_LL = 13;

        const int WM_MOUSEMOVE = 0x200;
        const int WM_LBUTTONDOWN = 0x201;
        const int WM_LBUTTONUP = 0x202;
        const int WM_LBUTTONDBLCLK = 0x203;
        const int WM_RBUTTONDOWN = 0x204;
        const int WM_RBUTTONUP = 0x205;
        const int WM_RBUTTONDBLCLK = 0x206;
        const int WM_MBUTTONDOWN = 0x207;
        const int WM_MBUTTONUP = 0x208;
        const int WM_MBUTTONDBLCLK = 0x209;
        const int WM_MOUSEWHEEL = 0x20A;
        const int WM_XBUTTONDOWN = 0x20B;
        const int WM_XBUTTONUP = 0x20C;
        const int WM_XBUTTONDBLCLK = 0x20D;
        const int WM_MOUSEHWHEEL = 0x20E;
        

        const uint MOUSEEVENTF_MOVE = 0x0001;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        const uint MOUSEEVENTF_XDOWN = 0x0080;
        const uint MOUSEEVENTF_XUP = 0x0100;
        const uint MOUSEEVENTF_WHEEL = 0x0800;
        const uint MOUSEEVENTF_HWHEEL = 0x01000;
        const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        static readonly uint[] MouseEventFlag = { MOUSEEVENTF_MOVE, MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, 0,
            MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP, 0, MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP, 0,
            MOUSEEVENTF_WHEEL, MOUSEEVENTF_XDOWN, MOUSEEVENTF_XUP, 0, MOUSEEVENTF_HWHEEL};

        const int WM_KEYUP = 0x101;
        const int WM_SYSKEYDOWN = 0x104;
        const int WM_SYSKEYUP = 0x105;

        int Xe=0;
        int Ye=0;

        bool isLocalScreen = true;

        public struct POINT
        {
            public int x;
            public int y;
        }
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        IntPtr mouse_hook = IntPtr.Zero;
        IntPtr keyboard_hook = IntPtr.Zero;

        public mouseHookProc mouse_delegate_callback;
        public keyboardHookProc keyboard_delegate_callback;

        public MouseKeyboardControl()
        {
            PowerPressed = new AutoResetEvent(false);
            hook();
        }

        ~MouseKeyboardControl()
        {
            unhook();
        }

        public void hook()
        {
            IntPtr hInstance = LoadLibrary("User32");
            mouse_delegate_callback = new mouseHookProc(hookProc);
            mouse_hook = SetWindowsHookEx(WH_MOUSE_LL, mouse_delegate_callback, hInstance, 0);

            keyboard_delegate_callback = new keyboardHookProc(hookKbdProc);
            //keyboard_hook = SetWindowsHookEx(WH_KEYBOARD_LL, keyboard_delegate_callback, hInstance, 0);

            //Xprev = Cursor.Position.X;
            //Yprev = Cursor.Position.Y;
        }

        public void unhook()
        {
            UnhookWindowsHookEx(mouse_hook);
            UnhookWindowsHookEx(keyboard_hook);
        }

        int Xm = 0;
        int Ym = 0;
        int Xdelta = 0, Ydelta = 0, Xprev, Yprev;
        public int hookProc(int code, uint wParam, ref MSLLHOOKSTRUCT lParam)
        {
            //Xdelta = lParam.pt.x - Xprev;
            //Ydelta = lParam.pt.y - Yprev;

            //Xprev = lParam.pt.x;
            //Yprev = lParam.pt.y;

            try
            {
                //Program.PostLog("local bounds= " + Program.pairedHosts.LocalLeftBound+ " "+Program.pairedHosts.LocalRightBound);

                if (Xe > Program.pairedHosts.LocalRightBound-1 || lParam.pt.x > Program.pairedHosts.LocalRightBound || Xe<0 || lParam.pt.x<1)
                {
                    isLocalScreen = false;
                    Program.PostLog("Xe=" + Xe + "  Ye=" + Ye + " " + "Xm=" + Xm + "  Ym=" + Ym);

                    if (Xe <= 0 && lParam.pt.x < 1)
                        Xe -= lParam.pt.x + Xm;
                    else
                        Xe += lParam.pt.x - Xm;

                    Ye += lParam.pt.y - Ym;
                    if (Ye < 0) Ye = 0;

                    Program.PostLog("Xe=" + Xe + "  Ye=" + Ye + " " + "Xm=" + Xm + "  Ym=" + Ym);

                    SendMouseMessage(wParam, ref lParam, Xe, Ye);
                    return 1;
                }
                else
                {
                    isLocalScreen = true;
                }
                Xe = lParam.pt.x;
                Ye = lParam.pt.y;// 0;
            }
            catch { };
            Xm = lParam.pt.x;
            Ym = lParam.pt.y;

            
            return CallNextHookEx(mouse_hook, code, wParam,  ref lParam);
        }

        public int hookKbdProc(int code, uint wParam, ref KBDLLHOOKSTRUCT lParam)
        {
            if (!isLocalScreen)
            try
            {
                //Program.PostLog("KBD: "+lParam.vkCode.ToString("X") + " " + lParam.scanCode.ToString("X")+" "+lParam.flags.ToString("X"));
                SendKeyboardMessage(wParam, ref lParam, Xe, Ye);
                    return 1;
            }
            catch { };
            return CallNextHookEx(keyboard_hook, code, wParam, ref lParam);
        }

        static private void SendMouseMessage(uint wParam, ref MSLLHOOKSTRUCT lParam, int Xe, int Ye)
        {
            Host host = Program.pairedHosts.CheckHost(Xe, Ye);
            if (host == null) return;
            int XeReal;
            if (Xe < 0)
                XeReal = host.Width + Xe - host.RightBound ;
            else XeReal = Xe - host.LeftBound;
            Program.PostLog("XeReal=" + XeReal + "  Ye=" + Ye );

            UInt32 dwFlags=0;
            Int32 mouseData = (Int32)lParam.mouseData;
            dwFlags = MouseEventFlag[wParam - 0x200];

            if (dwFlags == MOUSEEVENTF_WHEEL) mouseData /= 0x10000;

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((UInt32)InputType.MOUSE);
                bw.Write((Int32)XeReal);
                bw.Write((Int32)Ye);
                bw.Write(mouseData);
                bw.Write(dwFlags);
                bw.Write(lParam.time);
                host.Send(ms.ToArray());
            }
        }

        static private void SendKeyboardMessage(uint wParam, ref KBDLLHOOKSTRUCT lParam, int Xe, int Ye)
        {
            UInt32 dwFlags = 0;
            dwFlags = (lParam.flags & 0x80) >> 6;
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((UInt32)InputType.KEYBOARD);
                bw.Write((UInt16)lParam.vkCode);
                bw.Write((UInt16)lParam.scanCode);
                bw.Write(dwFlags);
                bw.Write(lParam.time);
                Program.pairedHosts.CheckAndSendIfExternalDesktop(ms.ToArray(), Xe, Ye);
            }
        }

        static public void ProcessReceivedMessage(byte[] buf)
        {
            INPUT input = new INPUT();
            using (MemoryStream ms = new MemoryStream(buf))
            using (BinaryReader br = new BinaryReader(ms))
            {
                for (; ms.Position < ms.Length;)
                {
                    input.Type = br.ReadUInt32();
                    if (input.Type == (uint)InputType.MOUSE)
                    {
                        input.Data.Mouse.X = br.ReadInt32();
                        input.Data.Mouse.Y = br.ReadInt32();
                        input.Data.Mouse.MouseData = br.ReadUInt32();
                        input.Data.Mouse.Flags = br.ReadUInt32();
                        input.Data.Mouse.Time = br.ReadUInt32();
                        input.Data.Mouse.Time = (uint)Environment.TickCount;
                        if (input.Data.Mouse.Flags == MOUSEEVENTF_MOVE) SetCursorPos(input.Data.Mouse.X, input.Data.Mouse.Y);
                        else SendInput(1, ref input, Marshal.SizeOf(input));
                        Program.PostLog("pt=" + input.Data.Mouse.X + ", " + input.Data.Mouse.Y);
                    }
                    else if (input.Type == (uint)InputType.KEYBOARD)
                    {
                        input.Data.Keyboard.Vk = br.ReadUInt16();
                        input.Data.Keyboard.Scan = br.ReadUInt16();
                        input.Data.Keyboard.Flags = br.ReadUInt32();
                        input.Data.Mouse.Time = (uint)Environment.TickCount;
                        SendInput(1, ref input, Marshal.SizeOf(input));
                    }

                }
            }
        }

        struct MOUSEINPUT
        {
            public Int32 X;
            public Int32 Y;
            public UInt32 MouseData;
            public UInt32 Flags;
            public UInt32 Time;
            public IntPtr ExtraInfo;
        }
        struct KEYBDINPUT
        {
            public UInt16 Vk;
            public UInt16 Scan;
            public UInt32 Flags;
            public UInt32 Time;
            public IntPtr ExtraInfo;
        }
        struct HARDWAREINPUT
        {
            public UInt32 Msg;
            public UInt16 ParamL;
            public UInt16 ParamH;
        }

        struct CONTROLINPUT
        {
            public UInt32 Msg;
            //public UInt16 ParamL;
            //public UInt16 ParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;

            [FieldOffset(0)]
            public KEYBDINPUT Keyboard;

            [FieldOffset(0)]
            public HARDWAREINPUT Hardware;

            [FieldOffset(0)]
            public CONTROLINPUT Control;
        }
        public enum InputType : UInt32
        {
            MOUSE = 0,
            KEYBOARD = 1,
            HARDWARE = 2,
            CONTROL = 255
        }
        struct INPUT
        {
            public UInt32 Type;
            public MOUSEKEYBDHARDWAREINPUT Data;
        }

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, mouseHookProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, keyboardHookProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        static extern int CallNextHookEx(IntPtr idHook, int nCode, uint wParam, ref MSLLHOOKSTRUCT lParam);

        [DllImport("user32.dll")]
        static extern int CallNextHookEx(IntPtr idHook, int nCode, uint wParam, ref KBDLLHOOKSTRUCT lParam);

        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll")]
        static extern bool PostMessage(uint hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern uint WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        static extern bool ScreenToClient(uint hwnd, out POINT point);

        [DllImport("user32.dll", SetLastError = true)]
        static extern UInt32 SendInput(UInt32 nInputs, ref INPUT Inputs, Int32 cbSize);

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;

namespace gmusic
{
    class globalKeyboardHook
    {
        public AutoResetEvent PowerPressed;
        public Queue<String> strq = new Queue<string>();

        public delegate int keyboardHookProc(int code, int wParam, ref keyboardHookStruct lParam);

        public struct keyboardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }
        
        const int WH_KEYBOARD_LL = 13;
        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;
        const int WM_SYSKEYDOWN = 0x104;
        const int WM_SYSKEYUP = 0x105;

        public List<Keys> HookedKeys = new List<Keys>();

        IntPtr hhook = IntPtr.Zero;

        public keyboardHookProc SAFE_delegate_callback;
        public event KeyEventHandler KeyDown;

        public event KeyEventHandler KeyUp;

        public globalKeyboardHook()
        {
            PowerPressed = new AutoResetEvent(false);
            hook();
        }

        ~globalKeyboardHook()
        {
            unhook();
        }

        public void hook()
        {
            IntPtr hInstance = LoadLibrary("User32");
            SAFE_delegate_callback = new keyboardHookProc(hookProc);
            hhook = SetWindowsHookEx(WH_KEYBOARD_LL, SAFE_delegate_callback, hInstance, 0);
            
        }

        public void unhook()
        {
            UnhookWindowsHookEx(hhook);
        }

        public int hookProc(int code, int wParam, ref keyboardHookStruct lParam)
        {

            try
            {
                Program.form.AddLine("\r\n" + lParam.scanCode.ToString("X") + " " + lParam.vkCode.ToString("X") + " " + lParam.flags.ToString("X"));

                if (code >= 0)
                {
                    switch (lParam.vkCode)
                    {
                        case 0xB3: //VK_MEDIA_PLAY_PAUSE
                            if (wParam == 0x100) { Program.PlayPause(); return 1; };
                            break;
                        case 0xB0: //VK_MEDIA_NEXT_TRACK
                            if (wParam == 0x100) { Program.NextTrack(); return 1; };
                            break;
                        case 0xB1: //VK_MEDIA_PREV_TRACK
                            if (wParam == 0x100) { Program.PrevTrack(); return 1; };
                            break;
                        case 0xFF: //Forward
                            if (wParam == 0x100) {
                                if (lParam.scanCode==9) { Program.Forward(); return 1; };
                                if (lParam.scanCode==3) { Program.Backward(); return 1; };
                            };
                            if (wParam == 0x101) {
                                if (lParam.scanCode==9) {return 1; };
                                if (lParam.scanCode==3) {return 1; };
                            };
                            break;
                    }

                }


        return CallNextHookEx(hhook, code, wParam, ref lParam);
            }
            catch { return 0; }
        }


        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, keyboardHookProc callback, IntPtr hInstance, uint threadId);
        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hInstance);
        [DllImport("user32.dll")]
        static extern int CallNextHookEx(IntPtr idHook, int nCode, int wParam, ref keyboardHookStruct lParam);
        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

    }
}

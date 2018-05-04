using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;

namespace PowerCfgAcer
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
                if (code >= 0)
                {
                    switch (lParam.scanCode)
                    {
                        case 0x0a:
                            if (wParam == 0x100 && lParam.vkCode==0xFF) { Program.SetPower0(); return 1; };
                            break;
                        case 0x0b:
                            if (wParam == 0x100 && lParam.vkCode == 0xFF) { Program.SetPower1(); return 1; };
                            break;
                        case 0x55:
                            if (wParam == 0x100 && lParam.vkCode == 0xFF) { Program.SetWiFiOn(); return 1; };
                            break;
                        case 0x56:
                            if (wParam == 0x100 && lParam.vkCode == 0xFF) { Program.SetWiFiOff(); return 1; };
                            break;
                        case 0x54:
                            if (wParam == 0x100 && lParam.vkCode == 0xFF) { Program.SetBthOn(); return 1; };
                            break;
                        case 0x59:
                            if (wParam == 0x100 && lParam.vkCode == 0xFF) { Program.SetBthOff(); return 1; };
                            break;
                        case 0x62:
                            if (lParam.vkCode == 0xFF) { Program.LockWs(wParam, lParam.time); return 1; };
                            break;
                    }
                }

                strq.Enqueue(String.Format("w:{0:x} fl:{1:d} scan:0x{2:x} vk:0x{3:x}",wParam, lParam.flags, lParam.scanCode, lParam.vkCode)); 
                PowerPressed.Set();

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

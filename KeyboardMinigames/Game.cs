using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyboardMinigames
{
    public abstract class Game
    {
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, KeyboardHookProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        
        public delegate void KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam);
        
        private KeyboardHookProc _KeyboardHookDelegate;
        private IntPtr _Hook;

        public Game() { }

        protected abstract void KeyboardInput(int keyCode);

        public abstract void Run();

        public virtual void InitializeInput()
        {
            if (_Hook != IntPtr.Zero) { return; }
            HookInput();
        }

        private void HookInput()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _KeyboardHookDelegate = new KeyboardHookProc(KeyboardHook);
                _Hook = SetWindowsHookEx(13, _KeyboardHookDelegate, GetModuleHandle(curModule.ModuleName), 0);
            }

            Application.Run();
        }

        private void KeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)0x0100)
            {
                int code = Marshal.ReadInt32(lParam);
                KeyboardInput(code);
            }
            CallNextHookEx(_Hook, nCode, wParam, lParam);
        }
    }
}

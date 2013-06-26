using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Runtime.InteropServices;

namespace FloWin
{
    /// <summary>
    /// グローバルキーフック
    /// </summary>
    public class GlobalHook
    {
        /// <summary>キーが押された</summary>
        public static event EventHandler<KeyEventArgs> KeyDown;
        /// <summary>キーが離された</summary>
        public static event EventHandler<KeyEventArgs> KeyUp;

        /// <summary>現在キャプチャされているか否か</summary>
        private static bool IsKeyHookAvailable { get { return _keyHook != IntPtr.Zero; } }


        private static IntPtr _keyHook;
        private static LowLevelKeyboardProc _keyProc;

        // 現在の修飾キー
        private static ModifierKeys _curModifiers = ModifierKeys.None;
        // Key値とModifierKeys値の変換マップ
        private static Dictionary<Key, ModifierKeys> _modKeyMap = new Dictionary<Key, ModifierKeys>
        {
            { Key.LeftCtrl, ModifierKeys.Control },
            { Key.RightCtrl, ModifierKeys.Control },
            { Key.LeftAlt, ModifierKeys.Alt },
            { Key.RightAlt, ModifierKeys.Alt },
            { Key.LeftShift, ModifierKeys.Shift },
            { Key.RightShift, ModifierKeys.Shift },
            { Key.LWin, ModifierKeys.Windows },
            { Key.RWin, ModifierKeys.Windows }
        };

        /// <summary>キャプチャを開始する</summary>
        public static void Hook()
        {
            _keyProc = new LowLevelKeyboardProc(keyHookProc);
            _keyHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyProc,
#if DEBUG
                GetModuleHandle(null)
#else
                Marshal.GetHINSTANCE(typeof(GlobalHook).Module)
#endif
                , 0);

            AppDomain.CurrentDomain.DomainUnload += (s, e) => Unhook();
        }

        /// <summary>キャプチャを終了する</summary>
        public static void Unhook()
        {
            if(_keyHook != IntPtr.Zero) UnhookWindowsHookEx(_keyHook);
        }


        /// <summary>フックプロシージャ</summary>
        private static IntPtr keyHookProc(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam)
        {
            bool cancel = false;
            if (nCode == HC_ACTION)
            {
                Key key = KeyInterop.KeyFromVirtualKey(lParam.vkCode);
                KeyEventArgs ev = new KeyEventArgs(lParam, key);

                switch (wParam.ToInt32())
                {
                    case WM_KEYDOWN:
                    case WM_SYSKEYDOWN:
                        if (_modKeyMap.ContainsKey(key)) _curModifiers |= _modKeyMap[key];
                        var ed = KeyDown;
                        if (ed != null) ed(null, ev);
                        break;
                    case WM_KEYUP:
                    case WM_SYSKEYUP:
                        if (_modKeyMap.ContainsKey(key)) _curModifiers &= ~_modKeyMap[key];
                        var eu = KeyUp;
                        if (eu != null) eu(null, ev);
                        break;
                }
                cancel = ev.Cancel;
            }
            return cancel ? (IntPtr)1 : CallNextHookEx(_keyHook, nCode, wParam, ref lParam);
        }

        /// <summary>キーイベントクラス</summary>
        public sealed class KeyEventArgs : EventArgs
        {
            private int _keyCode;
            private Key _key;
            private ModifierKeys _modifiers;
            private int _timeStamp;
            private bool _cancel;

            internal KeyEventArgs(KBDLLHOOKSTRUCT ev, Key key)
            {
                _keyCode = ev.vkCode;
                _key = key;
                _modifiers = _curModifiers;
                _timeStamp = ev.time;
                _cancel = false;
            }

            /// <summary>virtual-code</summary>
            public int KeyCode { get { return _keyCode; } }
            /// <summary>Key</summary>
            public Key Key { get { return _key; } }
            /// <summary>同時に押されている修飾キーのセット</summary>
            public ModifierKeys Modifiers { get { return _modifiers; } }
            /// <summary>イベント発生のタイムスタンプ</summary>
            public int TimeStamp { get { return _timeStamp; } }

            /// <summary>キーイベントをキャンセルする場合にtrueにする</summary>
            public bool Cancel
            {
                get { return _cancel; }
                set { _cancel = value; }
            }
        }

        #region Win32
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }


        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;

        private const int HC_ACTION = 0;

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const int WM_LBUTTONDOWN = 0x201;
        private const int WM_LBUTTONUP = 0x202;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_MBUTTONDOWN = 0x207;
        private const int WM_MBUTTONUP = 0x208;
        private const int WM_MBUTTONDBLCLK = 0x0209;
        private const int WM_RBUTTONDOWN = 0x205;
        private const int WM_RBUTTONUP = 0x206;
        private const int WM_RBUTTONDBLCLK = 0x204;

        private const int WM_MOUSEMOVE = 0x200;
        private const int WM_MOUSEWHEEL = 0x20A;

        private const int WM_XBUTTONDOWN = 0x20B;
        private const int WM_XBUTTONUP = 0x20C;
        private const int WM_XBUTTONDBLCLK = 0x20D;
        private const int WM_NCXBUTTONDOWN = 0x0AB;
        private const int WM_NCXBUTTONUP = 0x00AC;
        private const int WM_NCXBUTTONDBLCLK = 0x00AD;

        private const int XBUTTON1 = 0x1;
        private const int XBUTTON2 = 0x2;


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, int dwThreadId);
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hHook, int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);
        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hHook);

        #endregion Win32
    }
}

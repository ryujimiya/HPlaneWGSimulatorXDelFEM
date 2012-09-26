using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;

namespace MyUtilLib
{
    class WinAPI
    {
        //////////////////////////////////////////////////////////////
        //   定数
        //////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        public enum WHConstants : int
        {
            WH_MOUSE_LL = 14
        }
        /// <summary>
        /// 
        /// </summary>
        public enum WMConstants : int
        {
            WM_LBUTTONDOWN = 0x0201
          , WM_LBUTTONUP   = 0x0202
          , WM_MOUSEMOVE   = 0x0200
        }

        //////////////////////////////////////////////////////////////
        //   型
        //////////////////////////////////////////////////////////////
        /// <summary>
        /// POINT構造体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }
        /// <summary>
        /// 低レベルフック構造体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public class MsLLHookStructure
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // フックプロシージャのためのデリゲート
        public delegate IntPtr HookProcedureDelegate(int nCode, IntPtr wParam, IntPtr lParam);

        // フックプロシージャ"lpfn"をフックチェーン内にインストールする
        // 返り値はフックプロシージャのハンドル
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr SetWindowsHookEx(WHConstants idHook, HookProcedureDelegate lpfn, IntPtr hInstance, int threadId);

        // "SetWindowHookEx"でインポートされたフックプロシージャを削除する
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnhookWindowsHookEx(IntPtr idHook);

        // 次のフックプロシージャにフック情報を渡す
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(String lpModuleName);

        //////////////////////////////////////////////////////////////
        //   型
        //////////////////////////////////////////////////////////////
        /// <summary>
        ///        BOOL ShowWindow(
        ///            HWND hWnd,     // ウィンドウのハンドル
        ///            int nCmdShow   // 表示状態
        ///            );
        /// </summary>
        public enum SWConstants
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11,
            SW_MAX = 11,
        }
        public enum HWNDConstants
        {
            HWND_TOP = 0,
            HWND_TOPMOST = -1,
            HWND_NOTOPMOST = -2,
            HWND_BOTTOM = 1,
        }
        public enum SWPConstants
        {
            SWP_ASYNCWINDOWPOS =0x4000,
            SWP_NOMOVE = 0x0002,
            SWP_NOSIZE = 0x0001,
            SWP_NOZORDER = 0x0004,
            SWP_FRAMECHANGED = 0x0001,
            SWP_NOACTIVATE = 0x0010,
            SWP_DRAWFRAME = 0x0020,
            SWP_NOCOPYBITS = 0x0100,
            SWP_SHOWWINDOW = 0x0040,
            SWP_HIDEWINDOW = 0x0080,
        }

        public enum GWLConstants
        {
            GWL_STYLE = -16,
            GWL_EXSTYLE = -20,
        }

        public enum WSConstants : uint
        {
            WS_BORDER = 0x00800000,
            WS_POPUP = 0x80000000,
            WS_CAPTION = 0x00C00000,
            WS_DISABLED = 0x08000000,
            WS_DLGFRAME = 0x00400000,
            WS_HSCROLL = 0x00100000,
            WS_MAXIMIZE = 0x01000000,
            WS_MAXIMIZEBOX = 0x00010000,
            WS_MINIMIZE = 0x20000000,
            WS_MINIMIZEBOX = 0x00020000,
            WS_OVERLAPPED = 0,
            WS_OVERLAPPEDWINDOW = 0x00CF0000,
            WS_POPUPWINDOW = 0x80880000,
            WS_SIZEBOX = 0x0000F2C0,
            WS_SYSMENU = 0x00080000,
            WS_THICKFRAME = 0x00040000,
            WS_VSCROLL = 0x00200000,
            WS_VISIBLE = 0x10000000,
            WS_CHILD = 0x40000000,
            WS_GROUP = 0x00020000,

            //WS_CLIPSIBLINGS = 0x04000000,
        }

        public enum WSEXConstants : uint
        {
            WS_EX_TOOLWINDOW = 0x00000080,
            WS_EX_APPWINDOW = 0x00040000,
        }

        //////////////////////////////////////////////////////////////
        //   型
        //////////////////////////////////////////////////////////////
        /// <summary>
        /// RECT構造体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpClassName, string lpWindowName);

        [DllImport("User32.Dll")]
        public static extern int ShowWindow(IntPtr hWnd, SWConstants nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, int bRepaint);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32")]
        public static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll")]
        public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("USER32.DLL", CharSet = CharSet.Auto)]
        public static extern IntPtr SetParent(IntPtr hWndChild,IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        //////////////////////////////////////////////////////////////
        // GDI
        //////////////////////////////////////////////////////////////
        [System.Runtime.InteropServices.DllImport("gdi32.dll", EntryPoint = "SelectObject")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObj);
        [System.Runtime.InteropServices.DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        public static extern IntPtr DeleteObject(IntPtr hObj);
        
    }
}

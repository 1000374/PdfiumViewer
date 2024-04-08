using Pdfium.Net.Native.Structs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PdfiumViewer
{
    internal class Win32
    {
        [DllImport("user32.dll", EntryPoint = "ScrollWindowEx")]
        public static extern int ScrollWindowEx(HandleRef hWnd, int dx, int dy, IntPtr prcScroll, ref RECT prcClip, IntPtr hrgnUpdate, ref RECT prcUpdate, uint flags);
        [DllImport("user32.dll", EntryPoint = "GetScrollInfo")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetScrollInfo(HandleRef hwnd, int fnBar, ScrollInfo lpsi);

        [DllImport("user32.dll", EntryPoint = "SetScrollInfo")]
        public static extern int SetScrollInfo(HandleRef hwnd, int fnBar, [In] ScrollInfo lpsi, bool fRedraw);
        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        public static extern UIntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", EntryPoint = "WindowFromPoint")]
        public static extern IntPtr WindowFromPoint(Point pt);

    }
}

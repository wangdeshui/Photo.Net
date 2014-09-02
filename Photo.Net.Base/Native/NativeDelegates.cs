using System;
using System.Runtime.InteropServices;

namespace Photo.Net.Base.Native
{
    internal static class NativeDelegates
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
    }
}

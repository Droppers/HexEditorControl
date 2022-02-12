using System;
using System.Runtime.InteropServices;

namespace HexControl.Wpf.D2D;

internal static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = false)]
    public static extern IntPtr GetDesktopWindow();
}
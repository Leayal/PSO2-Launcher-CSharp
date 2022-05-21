using System;
using System.Runtime.InteropServices;

namespace Leayal.Shared
{
    /// <summary>A class providing convenient methods to interact with other windows through unmanaged code.</summary>
    public static class UnmanagedWindowsHelper
    {
        /// <summary>Bring a window to foreground in desktop.</summary>
        /// <param name="hWnd">The handle of the window which will be brought to foreground.</param>
        /// <returns>True if the operation successes. Otherwise, false.</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}

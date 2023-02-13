using System;
using System.Windows;
using System.Windows.Interop;
using MSWin32 = global::Windows.Win32;
using PInvoke = global::Windows.Win32.PInvoke;

namespace Leayal.Shared.Windows
{
    /// <summary>A class providing convenient methods to interact with other windows through unmanaged code.</summary>
    public static class UnmanagedWindowsHelper
    {
        /// <summary>Brings the thread that created the specified window into the foreground and activates the window.</summary>
		/// <param name="window">
		/// <para>The window that should be activated and brought to the foreground.</para>
		/// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/nf-winuser-setforegroundwindow#parameters">Read more on docs.microsoft.com</see>.</para>
		/// </param>
		/// <returns><see langword="true"/> if the operation successes. Otherwise, <see langword="false"/>.</returns>
		/// <remarks>
		/// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/nf-winuser-setforegroundwindow">Learn more about this API from docs.microsoft.com</see>.</para>
		/// </remarks>
        public static bool SetForegroundWindow(IWin32Window window) => PInvoke.SetForegroundWindow(new MSWin32.Foundation.HWND(window.Handle));

        /// <summary>Brings the thread that created the specified window into the foreground and activates the window.</summary>
        /// <param name="window">
        /// <para>The window that should be activated and brought to the foreground.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/nf-winuser-setforegroundwindow#parameters">Read more on docs.microsoft.com</see>.</para>
        /// </param>
        /// <returns><see langword="true"/> if the operation successes. Otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/nf-winuser-setforegroundwindow">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        public static bool SetForegroundWindow(System.Windows.Forms.IWin32Window window) => PInvoke.SetForegroundWindow(new MSWin32.Foundation.HWND(window.Handle));

        /// <summary>Brings the thread that created the specified window into the foreground and activates the window.</summary>
        /// <param name="window">
        /// <para>The window that should be activated and brought to the foreground.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/nf-winuser-setforegroundwindow#parameters">Read more on docs.microsoft.com</see>.</para>
        /// </param>
        /// <returns><see langword="true"/> if the operation successes. Otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/nf-winuser-setforegroundwindow">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        public static bool SetForegroundWindow(Window window)
        {
            return PInvoke.SetForegroundWindow(new MSWin32.Foundation.HWND(new WindowInteropHelper(window).Handle));
        }

        /// <summary>Brings the thread that created the specified window into the foreground and activates the window.</summary>
        /// <param name="hWnd">
        /// <para>A handle to the window that should be activated and brought to the foreground.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/nf-winuser-setforegroundwindow#parameters">Read more on docs.microsoft.com</see>.</para>
        /// </param>
        /// <returns><see langword="true"/> if the operation successes. Otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/nf-winuser-setforegroundwindow">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        public static bool SetForegroundWindow(IntPtr hWnd) => PInvoke.SetForegroundWindow(new MSWin32.Foundation.HWND(hWnd));
    }
}

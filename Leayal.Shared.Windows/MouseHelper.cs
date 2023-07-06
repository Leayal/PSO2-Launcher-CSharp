using System;
using System.Windows;
using MSWin32 = global::Windows.Win32;
using RawInput = global::Windows.Win32.UI.Input;
using PInvoke = global::Windows.Win32.PInvoke;
using System.Runtime.InteropServices;

namespace Leayal.Shared.Windows
{
    /// <summary>Convenient methods for dealing with low-level mouse.</summary>
    public static partial class MouseHelper
    {
        /// <summary>Translates the hardware-based units to pixel-based units on screen.</summary>
        /// <param name="hardwareX">The hardware-based X.</param>
        /// <param name="hardwareY">The hardware-based Y.</param>
        /// <param name="mousePosFlags">The mouse input flags.</param>
        /// <returns>A pixel-based unit on screen.</returns>
        public static Point TranslateRawInputCoordinate(int hardwareX, int hardwareY, RawMouseInputFlags mousePosFlags)
        {
            bool isVirtualDesktop = (mousePosFlags & RawMouseInputFlags.MOUSE_VIRTUAL_DESKTOP) == RawMouseInputFlags.MOUSE_VIRTUAL_DESKTOP;
            int width = Convert.ToInt32(isVirtualDesktop ? SystemParameters.VirtualScreenWidth : SystemParameters.PrimaryScreenWidth),
                height = Convert.ToInt32(isVirtualDesktop ? SystemParameters.VirtualScreenHeight : SystemParameters.PrimaryScreenHeight);

            double screenX = ((hardwareX / 65535.0f) * width),
                screenY = ((hardwareY / 65535.0f) * height);

            return new Point(screenX, screenY);
        }

        /// <summary>Delegate to define RawMouseInput message hook signature.</summary>
        /// <param name="data">The mouse data received from WM_INPUT message.</param>
        /// <param name="handled">Flag indicates whether the input was handled by one of the hooks.</param>
        public delegate void RawMouseInputHook(in RawMouseInputData data, ref bool handled);

        /// <summary>Register RawInput windows messages to be sent to the WPF application's main window.</summary>
        /// <returns>A <seealso cref="RegisteredRawMouseInput"/> if the registration is completed successfully. Otherwise, <see langword="null"/>.</returns>
        /// <remarks>
        /// <para>
        /// <b><u>This method has poor performance on WinForms</u></b>. Considering using <seealso cref="HookRawMouseInputUnsafe"/>
        /// along with <seealso cref="RegisteredRawMouseInput.TryGetRawMouseInputData(int, IntPtr, IntPtr, out RawMouseInputData)"/> in <seealso cref="System.Windows.Forms.Control.WndProc(ref System.Windows.Forms.Message)"/>.
        /// </para>
        /// <para>As of now, it's hardcoded to register with <see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-rawinputdevice#members">RIDEV_EXINPUTSINK</see> flag.</para>
        /// <para>- Despite of being sent to thread's message loop, it's still coming from a window associated with the thread.</para>
        /// <para>- The message will come to whichever "window" (a Win32 control has a "window handle", too) Windows is focusing on.</para>
        /// <para>- You MUST make sure to call <seealso cref="RegisteredRawMouseInput.Dispose()"/> method to avoid memory leaks when you no longer want to receive RawInput windows messages.</para>
        /// </remarks>
        public static RegisteredRawMouseInput? HookRawMouseInput()
        {
            if (Application.Current.MainWindow is MetroWindowEx window)
            {
                return HookRawMouseInput(window.Handle);
            }
            return null;
        }

        /// <summary>Register RawInput windows messages to be sent to the window which the <paramref name="windowHandle"/> points to.</summary>
        /// <param name="windowHandle">The handle pointing to a window which will receive RawInput windows messages. Can be <seealso cref="IntPtr.Zero"/>, which results in same effect as using <seealso cref="HookRawMouseInput()"/>.</param>
        /// <returns>A <seealso cref="RegisteredRawMouseInput"/> if the registration is completed successfully. Otherwise, <see langword="null"/>.</returns>
        /// <remarks>
        /// <para>
        /// <b><u>This method has poor performance on WinForms</u></b>. Considering using <seealso cref="HookRawMouseInputUnsafe"/>
        /// along with <seealso cref="RegisteredRawMouseInput.TryGetRawMouseInputData(int, IntPtr, IntPtr, out RawMouseInputData)"/> in <seealso cref="System.Windows.Forms.Control.WndProc(ref System.Windows.Forms.Message)"/>.
        /// </para>
        /// <para>As of now, it's hardcoded to register with <see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-rawinputdevice#members">RIDEV_EXINPUTSINK</see> flag.</para>
        /// <para>You can only register one window per process. As such, only one window will receive the RawInput windows messages at a time. If you call this method multiple times, only the <paramref name="windowHandle"/> from the last call will be used.</para>
        /// <para>In case <paramref name="windowHandle"/> is <seealso cref="IntPtr.Zero"/>, you MUST make sure to call <seealso cref="RegisteredRawMouseInput.Dispose()"/> method to avoid memory leaks when you no longer want to receive RawInput windows messages.</para>
        /// </remarks>
        public static RegisteredRawMouseInput? HookRawMouseInput(IntPtr windowHandle)
        {
            if (!RegisteredRawMouseInput.TryCreate(windowHandle, out var registration))
            {
                return null;
            }
            if (HookRawMouseInputUnsafe(windowHandle))
            {
                return registration;
            }
            return null;
        }

        /// <summary>Register RawInput windows messages to be sent to the window which the <paramref name="windowHandle"/> points to.</summary>
        /// <param name="windowHandle">The handle pointing to a window which will receive RawInput windows messages. Can be <seealso cref="IntPtr.Zero"/>.</param>
        /// <returns><see langword="true"/> if the registration is completed successfully. Otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>As of now, it's hardcoded to register with <see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-rawinputdevice#members">RIDEV_EXINPUTSINK</see> flag.</para>
        /// <para>You can only register one window per process. As such, only one window will receive the RawInput windows messages at a time. If you call this method multiple times, only the <paramref name="windowHandle"/> from the last call will be used.</para>
        /// <para>You MUST make sure to call <seealso cref="UnhookRawMouseInputUnsafe"/> method to avoid memory leaks when you no longer want to receive RawInput windows messages.</para>
        /// </remarks>
        public static bool HookRawMouseInputUnsafe(IntPtr windowHandle)
        {
            Span<RawInput.RAWINPUTDEVICE> devices = new RawInput.RAWINPUTDEVICE[]
            {
                new RawInput.RAWINPUTDEVICE()
                {
                    // Has to use "RIDEV_(EX)INPUTSINK" because WebView2 doesn't propagate messages.
                    // MUST NOT use "RIDEV_NOLEGACY" flag because .NET uses legacy messages (WM_KEY* and WM_MOUSE*) to handle keyboard and mouse inputs.
                    dwFlags = RawInput.RAWINPUTDEVICE_FLAGS.RIDEV_EXINPUTSINK,
                    usUsagePage = PInvoke.HID_USAGE_PAGE_GENERIC,
                    usUsage = PInvoke.HID_USAGE_GENERIC_MOUSE,
                    hwndTarget = new MSWin32.Foundation.HWND(windowHandle)
                }
            };

            return PInvoke.RegisterRawInputDevices(devices, (uint)(MemoryMarshal.AsBytes(devices).Length));
        }

        /// <summary>Unregister all RawInput windows messages registrations associated with current process.</summary>
        /// <returns><see langword="true"/> if the registration is completed successfully. Otherwise, <see langword="false"/>.</returns>
        public static bool UnhookRawMouseInputUnsafe()
        {
            Span<RawInput.RAWINPUTDEVICE> devices = new RawInput.RAWINPUTDEVICE[]
            {
                new RawInput.RAWINPUTDEVICE()
                {
                    dwFlags = RawInput.RAWINPUTDEVICE_FLAGS.RIDEV_REMOVE,
                    usUsagePage = PInvoke.HID_USAGE_PAGE_GENERIC,
                    usUsage = PInvoke.HID_USAGE_GENERIC_MOUSE,
                    hwndTarget = new MSWin32.Foundation.HWND(IntPtr.Zero)
                }
            };

            return (PInvoke.RegisterRawInputDevices(devices, (uint)(MemoryMarshal.AsBytes(devices).Length)));
        }
    }
}

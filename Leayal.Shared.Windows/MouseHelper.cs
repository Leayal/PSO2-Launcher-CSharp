using System;
using System.Windows;
using System.Windows.Input;
using MSWin32 = global::Windows.Win32;
using RawInput = global::Windows.Win32.UI.Input;
using PInvoke = global::Windows.Win32.PInvoke;
using System.Windows.Interop;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Buffers;

namespace Leayal.Shared.Windows
{
    /// <summary>Convenient methods for mouse.</summary>
    public static partial class MouseHelper
    {
        private static readonly Func<Point>? mouseFunc_GetScreenPosition;
        static MouseHelper()
        {
            // GetScreenPosition

            var device = Mouse.PrimaryDevice;
            var t = device.GetType();
            var mouseMethod_GetScreenPosition = t.GetMethod("GetScreenPosition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.InvokeMethod, Array.Empty<Type>());
            if (mouseMethod_GetScreenPosition != null)
            {
                var @delegate = Delegate.CreateDelegate(typeof(Func<Point>), device, mouseMethod_GetScreenPosition, false);
                if (@delegate != null)
                {
                    mouseFunc_GetScreenPosition = (Func<Point>)@delegate;
                }
            }
        }

        /// <summary>Gets mouse position on desktop.</summary>
        /// <returns>A <seealso cref="Point"/> represents the mouse's absolute coordination on desktop. (Not related to any windows)</returns>
        public static Point GetMousePositionOnDesktop()
        {
            if (mouseFunc_GetScreenPosition != null)
            {
                return mouseFunc_GetScreenPosition.Invoke();
            }
            else
            {
                var wpfApp = Application.Current;
                if (wpfApp != null && wpfApp.MainWindow != null)
                {
                    var window = wpfApp.MainWindow;
                    return window.PointToScreen(Mouse.GetPosition(window));
                }
                else
                {
                    var pos = System.Windows.Forms.Control.MousePosition;
                    return new Point(pos.X, pos.Y);
                }
            }
        }

        // Proof of concept. It works and can get over the issue where WebView2 shallows the WM_MOUSE* windows messages.
        // However, Raw input doesn't have bound check or anything to check if the mouse is on an UI element.
        // Will do more later.
        public static void A()
        {
            if (Application.Current.MainWindow is MetroWindowEx window)
            {
                Span<RawInput.RAWINPUTDEVICE> devices = new RawInput.RAWINPUTDEVICE[]
                {
                    new RawInput.RAWINPUTDEVICE()
                    {
                        dwFlags = RawInput.RAWINPUTDEVICE_FLAGS.RIDEV_EXINPUTSINK, // Can use 0, but exInputSink will let us handle Raw Mouse Input only when the foreground application(s) don't process it.
                        usUsagePage = PInvoke.HID_USAGE_PAGE_GENERIC,
                        usUsage = PInvoke.HID_USAGE_GENERIC_MOUSE,
                        hwndTarget = new MSWin32.Foundation.HWND(window.Handle)
                    }
                };
               
                if (PInvoke.RegisterRawInputDevices(devices, (uint)(MemoryMarshal.AsBytes(devices).Length)))
                {
                    // ComponentDispatcher.ThreadFilterMessage += ComponentDispatcher_ThreadFilterMessage;

                    var src = HwndSource.FromHwnd(window.Handle);
                    src.AddHook(new HwndSourceHook(Hoooook));
                }
            }
        }

        const int WM_MOUSEWHEEL = 0x020A;

        private static unsafe IntPtr Hoooook(IntPtr hwnd, int message, IntPtr wparam, IntPtr lParam, ref bool handled)
        {
            handled = false;
            if (message == (int)PInvoke.WM_INPUT)
            {
                handled = true;
                uint dwSize = 0;
                uint headerSize = (uint)sizeof(RawInput.RAWINPUTHEADER);
                PInvoke.GetRawInputData(new RawInput.HRAWINPUT(lParam), RawInput.RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, pcbSize: &dwSize, cbSizeHeader: headerSize);

                var borrowedBuffer = ArrayPool<byte>.Shared.Rent((int)dwSize);
                try
                {
                    Span<byte> allocatedForMsg = borrowedBuffer;
                    if (PInvoke.GetRawInputData(new RawInput.HRAWINPUT(lParam), RawInput.RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, Unsafe.AsPointer(ref allocatedForMsg.GetPinnableReference()), &dwSize, headerSize) > 0
                        && MemoryMarshal.TryRead<RawInput.RAWINPUT>(allocatedForMsg, out var inputMsg))
                    {
                        if (inputMsg.header.dwType == (uint)RawInput.RID_DEVICE_INFO_TYPE.RIM_TYPEMOUSE)
                        {
                            ref var mouseData = ref inputMsg.data.mouse;
                            if (mouseData.Anonymous.Anonymous.usButtonFlags == (ushort)RawInputMouseFlags.RI_MOUSE_WHEEL)
                            {
                                var wheelCount = mouseData.Anonymous.Anonymous.usButtonData;

                            }
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(borrowedBuffer);
                }
            }
            return IntPtr.Zero;
        }

        private static unsafe void ComponentDispatcher_ThreadFilterMessage(ref MSG msg, ref bool handled)
        {
            handled = false;
            if (msg.message == PInvoke.WM_INPUT)
            {
                handled = true;
                uint dwSize = 0;
                uint headerSize = (uint)sizeof(RawInput.RAWINPUTHEADER);
                PInvoke.GetRawInputData(new RawInput.HRAWINPUT(msg.lParam), RawInput.RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, pcbSize: &dwSize, cbSizeHeader: headerSize);

                var borrowedBuffer = ArrayPool<byte>.Shared.Rent((int)dwSize);
                try
                {
                    Span<byte> allocatedForMsg = borrowedBuffer;
                    if (PInvoke.GetRawInputData(new RawInput.HRAWINPUT(msg.lParam), RawInput.RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, Unsafe.AsPointer(ref allocatedForMsg.GetPinnableReference()), &dwSize, headerSize) > 0
                        && MemoryMarshal.TryRead<RawInput.RAWINPUT>(allocatedForMsg, out var inputMsg))
                    {
                        if (inputMsg.header.dwType == (uint)RawInput.RID_DEVICE_INFO_TYPE.RIM_TYPEMOUSE)
                        {
                            ref var mouseData = ref inputMsg.data.mouse;
                            if (mouseData.Anonymous.Anonymous.usButtonFlags == (ushort)RawInputMouseFlags.RI_MOUSE_WHEEL)
                            {
                                var wheelCount = mouseData.Anonymous.Anonymous.usButtonData;
                            }
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(borrowedBuffer);
                }
            }
        }
    }
}

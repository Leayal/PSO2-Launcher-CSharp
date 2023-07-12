using System;
using RawInput = global::Windows.Win32.UI.Input;
using PInvoke = global::Windows.Win32.PInvoke;
using Win32Foundation = global::Windows.Win32.Foundation;
using System.Windows.Interop;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Buffers;
using static Leayal.Shared.Windows.MouseHelper;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace Leayal.Shared.Windows
{
    /// <summary>Managing raw mouse input registration.</summary>
    public sealed class RegisteredRawMouseInput : IDisposable
    {
        const int WM_INPUT = unchecked((int)PInvoke.WM_INPUT);
        internal static bool TryCreate(IntPtr windowHandle, [NotNullWhen(true)] out RegisteredRawMouseInput? registration)
        {
            if (windowHandle == IntPtr.Zero)
            {
                registration = new RegisteredRawMouseInput(Application.MessageLoop);
                return true;
            }
            else
            {

                var hwndSrc = HwndSource.FromHwnd(windowHandle);
                if (hwndSrc == null)
                {
                    registration = new RegisteredRawMouseInput(Application.MessageLoop);
                }
                else
                {
                    registration = new RegisteredRawMouseInput(hwndSrc);
                }
                return true;
            }
            // registration = null;
            // return false;
        }

        private bool disposedValue;

        private readonly HwndSource? hwndSrc;
        private readonly HwndSourceHook? hook;

        private readonly RawInputMessageFilter? msgFilter;

        private readonly Action Unregister;

        private RegisteredRawMouseInput()
        {
            this.disposedValue = false;
            this.hwndSrc = null;
            this.hook = null;
            this.Unregister = new Action(() => { });
        }

        private RegisteredRawMouseInput(bool trueForWinForm_FalseForWPF) : this()
        {
            if (trueForWinForm_FalseForWPF)
            {
                this.msgFilter = new RawInputMessageFilter(this);
                Application.AddMessageFilter(this.msgFilter);
                this.Unregister = this.Unregister_MsgFilter;
            }
            else
            {
                ComponentDispatcher.ThreadFilterMessage += this.ComponentDispatcher_ThreadFilterMessage;
                this.Unregister = this.Unregister_ThreadFilterMsg;
            }
        }

        private RegisteredRawMouseInput(HwndSource src) : this()
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            this.hwndSrc = src;
            this.hook = new HwndSourceHook(this.HookForWM_INPUT);
            this.hwndSrc.AddHook(this.hook);
            this.Unregister = this.Unregister_HwndSource;
        }

        /// <summary>Adds or removes hook that listen for WM_INPUT windows message that relates to Raw Mouse Input.</summary>
        public event RawMouseInputHook? MessageHook;

        private void Unregister_HwndSource() => this.hwndSrc?.RemoveHook(this.hook);
        private void Unregister_MsgFilter()
        {
            var filter = this.msgFilter;
            if (filter != null) Application.RemoveMessageFilter(filter);
        }
        private void Unregister_ThreadFilterMsg() => ComponentDispatcher.ThreadFilterMessage -= this.ComponentDispatcher_ThreadFilterMessage;

        /// <summary>Try to parse the windows procedure message and return RawMouseInput data.</summary>
        /// <param name="msg">The message ID of the window procedure.</param>
        /// <param name="lParam">The additional information or data about the message.</param>
        /// <param name="rawMouseData">If the method returns <see langword="true"/>, this parameter output a <seealso cref="RawMouseInputData"/> containing raw mouse input data.</param>
        /// <returns><see langword="true"/> if the message came from a HID-compliant mouse, and has been parsed. Otherwise, <see langword="false"/>.</returns>
        /// <remarks>You must ensure to call this function when the message is WM_INPUT or you will get corrupted data. To check whether the window message is WM_INPUT or not, use <seealso cref="MouseHelper.IsRawInputWindowMessage"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static unsafe bool TryGetRawMouseInputData(IntPtr lParam, out RawMouseInputData rawMouseData)
        {
            uint dwSize = 0;
            uint headerSize = (uint)sizeof(RawInput.RAWINPUTHEADER);
            PInvoke.GetRawInputData(new RawInput.HRAWINPUT(lParam), RawInput.RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, pcbSize: &dwSize, cbSizeHeader: headerSize);

            bool isSafeToDirectAccess = dwSize == (uint)sizeof(RawInput.RAWINPUT);
            if (isSafeToDirectAccess)
            {
                // It should reach here.
                var @struct = new RawInput.RAWINPUT();
                if (PInvoke.GetRawInputData(new RawInput.HRAWINPUT(lParam), RawInput.RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, &@struct, &dwSize, headerSize) > 0)
                {
                    if (@struct.header.dwType == (uint)RawInput.RID_DEVICE_INFO_TYPE.RIM_TYPEMOUSE)
                    {
                        rawMouseData = new RawMouseInputData(in @struct.data.mouse);
                        return true;
                    }
                }
            }
            else
            {
                var managedSize = unchecked((int)dwSize);
                // Fall-back to unmanaged allocation instead.
                var borrowedBuffer = ArrayPool<byte>.Shared.Rent(managedSize < 4096 ? 4096 : managedSize);
                try
                {
                    var span = borrowedBuffer.AsSpan(0, managedSize);
                    if (PInvoke.GetRawInputData(new RawInput.HRAWINPUT(lParam), RawInput.RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, Unsafe.AsPointer(ref span.GetPinnableReference()), &dwSize, headerSize) > 0)
                    {
                        // Zero-copy
                        // MemoryMarshal.TryRead<RawInput.RAWINPUT>(new ReadOnlySpan<byte>(allocatedForMsg.ToPointer(), (int)dwSize), out var inputMsg)

                        // Copying via Marshal
                        // var copiedStruct = Marshal.PtrToStructure<RawInput.RAWINPUT>(allocatedForMsg);
                        if (MemoryMarshal.TryRead<RawInput.RAWINPUT>(span, out var inputMsg))
                        {
                            if (inputMsg.header.dwType == (uint)RawInput.RID_DEVICE_INFO_TYPE.RIM_TYPEMOUSE)
                            {
                                rawMouseData = new RawMouseInputData(in inputMsg.data.mouse);
                                return true;
                            }
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(borrowedBuffer);
                }
            }
            rawMouseData = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        private void ComponentDispatcher_ThreadFilterMessage(ref MSG msg, ref bool handled)
        {
            this.HookForWM_INPUT(msg.hwnd, msg.message, msg.wParam, msg.lParam, ref handled);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        private IntPtr HookForWM_INPUT(IntPtr hwnd, int message, IntPtr wparam, IntPtr lParam, ref bool handled)
        {
            if (MouseHelper.IsRawInputWindowMessage(message))
            {
                if (TryGetRawMouseInputData(lParam, out var mouseData))
                    this.MessageHook?.Invoke(in mouseData, ref handled);
                return MouseHelper.CleanUpRawInputMessage(hwnd, message, wparam, lParam);
            }
            return IntPtr.Zero;
        }


        private void Dispose(bool disposing)
        {
            if (this.disposedValue) return;
            this.disposedValue = true;
            if (disposing)
            {
                this.MessageHook = null;
                // TODO: dispose managed state (managed objects)
            }
            this.Unregister.Invoke();
            MouseHelper.UnhookRawMouseInputUnsafe();
        }

        /// <summary>Destructor.</summary>
        ~RegisteredRawMouseInput()
        {
            Dispose(disposing: false);
        }

        /// <summary>Unregister from receiving RawInput windows messages and clean up unmanaged resources.</summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        sealed class RawInputMessageFilter : IMessageFilter
        {
            private readonly RegisteredRawMouseInput parent;

            public RawInputMessageFilter(RegisteredRawMouseInput who)
            {
                this.parent = who;
            }

            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            public bool PreFilterMessage(ref Message m)
            {
                bool handled = false;
                this.parent.HookForWM_INPUT(m.HWnd, m.Msg, m.WParam, m.LParam, ref handled);
                return handled;
            }
        }
    }
}

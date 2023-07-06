using System;
using RawInput = global::Windows.Win32.UI.Input;
using PInvoke = global::Windows.Win32.PInvoke;
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
        internal static bool TryCreate(IntPtr windowHandle, [NotNullWhen(true)] out RegisteredRawMouseInput? registration)
        {
            if (windowHandle == IntPtr.Zero)
            {
                registration = new RegisteredRawMouseInput(System.Windows.Forms.Application.MessageLoop);
                return true;
            }
            else
            {

                var hwndSrc = HwndSource.FromHwnd(windowHandle);
                if (hwndSrc == null)
                {
                    registration = new RegisteredRawMouseInput(System.Windows.Forms.Application.MessageLoop);
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
                System.Windows.Forms.Application.AddMessageFilter(this.msgFilter);
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
            if (filter != null) System.Windows.Forms.Application.RemoveMessageFilter(filter);
        }
        private void Unregister_ThreadFilterMsg() => ComponentDispatcher.ThreadFilterMessage -= this.ComponentDispatcher_ThreadFilterMessage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="rawMouseData"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static unsafe bool TryGetRawMouseInputData(int msg, IntPtr wParam, IntPtr lParam, out RawMouseInputData rawMouseData)
        {
            if (msg == (int)PInvoke.WM_INPUT)
            {
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
                            rawMouseData = new RawMouseInputData(in inputMsg.data.mouse);
                            return true;
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

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ComponentDispatcher_ThreadFilterMessage(ref MSG msg, ref bool handled)
        {
            this.HookForWM_INPUT(msg.hwnd, msg.message, msg.wParam, msg.lParam, ref handled);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe IntPtr HookForWM_INPUT(IntPtr hwnd, int message, IntPtr wparam, IntPtr lParam, ref bool handled)
        {
            if (TryGetRawMouseInputData(message, wparam, lParam, out var mouseData))
            {
                bool isHandled = false;
                this.MessageHook?.Invoke(in mouseData, ref isHandled);
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

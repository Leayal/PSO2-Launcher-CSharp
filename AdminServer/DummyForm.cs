using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Leayal.PSO2Launcher.AdminProcess
{
    class DummyForm : Form
    {
        private Dictionary<ReadOnlyMemory<byte>, TaskCompletionSource<ReadOnlyMemory<byte>>> task_dictionary;
        private const int WM_COPYDATA = 0x004A;
        public DummyForm()
        {
            this.task_dictionary = new Dictionary<ReadOnlyMemory<byte>, TaskCompletionSource<ReadOnlyMemory<byte>>>();
        }

        public bool ListenForMessage()
        {
            if (!this.IsHandleCreated)
            {
                this.CreateHandle();
            }
            return ChangeWindowMessageFilterEx(this.Handle, (uint)WM_COPYDATA, MessageFilterAction.Allow, IntPtr.Zero);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_COPYDATA)
            {
                // sender
                var senderWindowHandle = m.WParam;

                var dataHeaderStructPointer = m.LParam;
                var header = Marshal.PtrToStructure<COPYDATASTRUCT>(dataHeaderStructPointer);
                var size = Convert.ToInt32(header.cbData);
                var buffer = new BorrowedBuffer(size);
                Marshal.Copy(buffer.Buffer, 0, header.lpData, size);
                var callback = this.IPCBufferReceived;
                if (callback != null)
                {
                    callback.BeginInvoke(senderWindowHandle, buffer, callback.EndInvoke, null);
                }

            }
            base.WndProc(ref m);
        }

        public void SendDataTo(IntPtr windowHandle, int dataType, ReadOnlyMemory<byte> data)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new _InnerSendDataTo(this.InnerSendDataTo), windowHandle, dataType, data);
            }
            else
            {
                this.InnerSendDataTo(windowHandle, dataType, data);
            }
        }

        private void InnerSendDataTo(IntPtr windowHandle, int dataType, ReadOnlyMemory<byte> data)
        {
            var dataWrapped = new COPYDATASTRUCT();
            dataWrapped.cbData = data.Length;
            using (var pinned = data.Pin())
            {
                dataWrapped.dwData = new IntPtr(dataType);
                unsafe
                {
                    dataWrapped.lpData = new IntPtr(pinned.Pointer);
                }
                SendMessage(windowHandle, WM_COPYDATA, this.Handle, ref dataWrapped);
            }
        }

        public async Task SendDataToAsync(IntPtr windowHandle, int dataType, ReadOnlyMemory<byte> data)
        {
            if (this.InvokeRequired)
            {
                var ar = this.BeginInvoke(new _InnerSendDataToAsync(this.InnerSendDataToAsync), windowHandle, dataType, data);
                var obj = await Task.Factory.FromAsync(ar, this.EndInvoke);
                var task = (Task)obj;
                await task;
            }
            else
            {
                await this.InnerSendDataToAsync(windowHandle, dataType, data);
            }
        }

        private Task<ReadOnlyMemory<byte>> InnerSendDataToAsync(IntPtr windowHandle, int dataType, ReadOnlyMemory<byte> data)
        {
            TaskCompletionSource<ReadOnlyMemory<byte>> tSrc;
            if (!this.task_dictionary.TryGetValue(data, out tSrc))
            {
                tSrc = new TaskCompletionSource<ReadOnlyMemory<byte>>();
                var dataWrapped = new COPYDATASTRUCT();
                dataWrapped.cbData = data.Length;
                using (var pinned = data.Pin())
                {
                    dataWrapped.dwData = new IntPtr(dataType);
                    unsafe
                    {
                        dataWrapped.lpData = new IntPtr(pinned.Pointer);
                    }
                    PostMessage(windowHandle, WM_COPYDATA, this.Handle, ref dataWrapped);
                }
            }
            return tSrc.Task;
        }

        private delegate Task _InnerSendDataToAsync(IntPtr windowHandle, int dataType, ReadOnlyMemory<byte> data);
        private delegate void _InnerSendDataTo(IntPtr windowHandle, int dataType, ReadOnlyMemory<byte> data);

        public event IPCBufferReceivedCallback IPCBufferReceived;

        public delegate void IPCBufferReceivedCallback(IntPtr senderWindowHandle, BorrowedBuffer buffer);

        public class BorrowedBuffer : IDisposable
        {
            private bool disposed;
            public readonly byte[] Buffer;
            public readonly int Size;

            public BorrowedBuffer(int size)
            {
                this.disposed = false;
                this.Buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(size);
                this.Size = size;
            }

            public void Dispose()
            {
                if (this.disposed) return;
                this.disposed = true;
                System.Buffers.ArrayPool<byte>.Shared.Return(this.Buffer);
                GC.SuppressFinalize(this);
            }

            ~BorrowedBuffer()
            {
                this.Dispose();
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ChangeWindowMessageFilterEx(IntPtr handle, uint messageID, [MarshalAs(UnmanagedType.U4)] MessageFilterAction action, ref PCHANGEFILTERSTRUCT properties);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ChangeWindowMessageFilterEx(IntPtr handle, uint messageID, [MarshalAs(UnmanagedType.U4)] MessageFilterAction action, IntPtr propPointer);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT data);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT data);

        enum MessageFilterAction : uint
        {
            Reset = 0,
            Allow,
            Disallow
        }

        enum MSGFLTINFO : uint
        {
            MSGFLTINFO_NONE,

            /// <summary>The message has already been allowed by this window's message filter, and the function thus succeeded with no change to the window's message filter</summary>
            MSGFLTINFO_ALREADYALLOWED_FORWND,

            /// <summary>The message has already been blocked by this window's message filter, and the function thus succeeded with no change to the window's message filter</summary>
            MSGFLTINFO_ALREADYDISALLOWED_FORWND,

            /// <summary>The message is allowed at a scope higher than the window</summary>
            MSGFLTINFO_ALLOWED_HIGHER
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PCHANGEFILTERSTRUCT
        {
            uint cbSize;
            public MSGFLTINFO ExtStatus;

            public static PCHANGEFILTERSTRUCT Create()
            {
                var result = new PCHANGEFILTERSTRUCT();
                result.cbSize = (uint)Marshal.SizeOf<PCHANGEFILTERSTRUCT>();
                result.ExtStatus = MSGFLTINFO.MSGFLTINFO_NONE;
                return result;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT : IDisposable
        {
            public IntPtr dwData;
            [MarshalAs(UnmanagedType.U4)]
            public int cbData;
            public IntPtr lpData;

            public COPYDATASTRUCT(IntPtr dw, int cb, IntPtr lp)
            {
                this.dwData = dw;
                this.cbData = cb;
                this.lpData = lp;
            }

            /// <summary>
            /// Only dispose COPYDATASTRUCT if you were the one who allocated it
            /// </summary>
            public void Dispose()
            {
                if (lpData != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(lpData);
                    lpData = IntPtr.Zero;
                    cbData = 0;
                }
            }

            public string AsString()
            {
                if (this.lpData == IntPtr.Zero) throw new ObjectDisposedException(nameof(COPYDATASTRUCT));

                return Marshal.PtrToStringUni(lpData);
            }

            public unsafe ReadOnlySpan<byte> AsBinary()
            {
                if (this.lpData == IntPtr.Zero) throw new ObjectDisposedException(nameof(COPYDATASTRUCT));
                if (this.cbData == 0) return Array.Empty<byte>();
                return new ReadOnlySpan<byte>(this.lpData.ToPointer(), this.cbData);
            }

            public static COPYDATASTRUCT Create(int dwData, string value)
            {
                var allocated = Marshal.StringToCoTaskMemUni(value);
                return new COPYDATASTRUCT(new IntPtr(dwData), value.Length + 1, allocated);
            }

            public static COPYDATASTRUCT Create(int dwData, byte[] value)
            {
                var len = value.Length;
                var mem = IntPtr.Zero;
                try
                {
                    mem = Marshal.AllocCoTaskMem(len);
                    Marshal.Copy(value, 0, mem, len);
                }
                catch
                {
                    if (mem != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(mem);
                    }
                    throw;
                }
                return new COPYDATASTRUCT(new IntPtr(dwData), len, mem);
            }
        }

    }
}

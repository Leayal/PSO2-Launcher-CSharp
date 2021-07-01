using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Leayal.PSO2Launcher.AdminProcess
{
    public partial class DummyForm : Form
    {
        private const int WM_COPYDATA = 0x004A;
        public DummyForm()
        {
            InitializeComponent();
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
            dataWrapped.cbData = (uint)data.Length;
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

        private delegate void _InnerSendDataTo(IntPtr windowHandle, int dataType, ReadOnlyMemory<byte> data);

        public IPCBufferReceivedCallback IPCBufferReceived;

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
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

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
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public uint cbData;
            public IntPtr lpData;
        }
    }
}

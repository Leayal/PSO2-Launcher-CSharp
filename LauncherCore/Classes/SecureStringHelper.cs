using System;
using System.Security;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Leayal.PSO2Launcher.Core.Classes
{
    static class SecureStringHelper
    {
        public static void UseAsString(this SecureString myself, SecretRevealedText revealed)
        {
            throw new NotImplementedException();
        }

        public static void EncodeTo(this SecureString myself, System.Text.Encoding encoding, Stream stream, out int writtenChars, out int writtenBytes)
        {
            if (!stream.CanWrite)
            {
                throw new ArgumentException(nameof(stream));
            }

            var encoder = encoding.GetEncoder();
            {
                int charUsed = 0, byteWritten = 0;
                UseRaw(myself, Encoding.Unicode, new SecretRevealedRaw((in ReadOnlySpan<byte> buffer) =>
                {
                    // Another copy
                    IntPtr pointer = IntPtr.Zero;
                    try
                    {
                        int bufferLength = buffer.Length;
                        pointer = Marshal.AllocHGlobal(bufferLength);
                        Span<byte> tmpBuffer;
                        unsafe
                        {
                            tmpBuffer = new Span<byte>(pointer.ToPointer(), bufferLength);
                            bool c;
                            fixed (byte* pinned = buffer)
                            {
                                char* cc = (char*)pinned;
                                encoder.Convert(cc, myself.Length, (byte*)(pointer.ToPointer()), bufferLength, true, out charUsed, out byteWritten, out c);
                            }
                            stream.Write(tmpBuffer.Slice(0, byteWritten));
                        }
                        
                    }
                    finally
                    {
                        if (pointer != IntPtr.Zero)
                        {
                            Marshal.ZeroFreeGlobalAllocUnicode(pointer);
                        }
                        encoder.Reset();
                    }
                }));
                writtenBytes = byteWritten;
                writtenChars = charUsed;
            }
            encoder = null;
            // GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
        }

        public static void UseRaw(this SecureString myself, Encoding encoding, SecretRevealedRaw revealed)
        {
            // SecureStringMarshal.SecureStringToGlobalAllocUnicode
            IntPtr allocated;
            switch (encoding)
            {
                case Encoding.Unicode:
                    allocated = SecureStringMarshal.SecureStringToGlobalAllocUnicode(myself);
                    break;
                case Encoding.ANSI:
                    allocated = SecureStringMarshal.SecureStringToGlobalAllocAnsi(myself);
                    break;
                default:
                    throw new NotSupportedException();
            }
            if (allocated == IntPtr.Zero)
            {
                throw new ArgumentNullException();
            }
            try
            {
                ReadOnlySpan<byte> span;
                unsafe
                {
                    switch (encoding)
                    {
                        case Encoding.Unicode:
                            span = new ReadOnlySpan<byte>(allocated.ToPointer(), myself.Length * 2);
                            break;
                        case Encoding.ANSI:
                            span = new ReadOnlySpan<byte>(allocated.ToPointer(), myself.Length);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
                revealed.Invoke(in span);
            }
            finally
            {
                switch (encoding)
                {
                    case Encoding.Unicode:
                        Marshal.ZeroFreeGlobalAllocUnicode(allocated);
                        break;
                    case Encoding.ANSI:
                        Marshal.ZeroFreeGlobalAllocAnsi(allocated);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        // Would this create a copy of string?
        public delegate void SecretRevealedText(in string text);

        public delegate void SecretRevealedRaw(in ReadOnlySpan<byte> data);

        public enum Encoding
        {
            Unicode,
            ANSI
        }
    }
}

using System;
using System.Security;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

namespace Leayal.PSO2Launcher.Core.Classes
{
    /// <summary>Provides convenient extended methods to deal with <seealso cref="SecureString"/>.</summary>
    /// <remarks>It's highly inefficient in term of memory usage.</remarks>
    static class SecureStringHelper
    {
        private static readonly byte[] _entropy = Encoding.ASCII.GetBytes("LeaTypicalEntro");

        /// <summary>Invoke the <see cref="SecretRevealedText"/> delegate with the revealed password in form of an unmanaged array of <seealso cref="char"/>.</summary>
        /// <param name="myself">The <seealso cref="SecureString"/> to reveal.</param>
        /// <param name="revealed">The delegate to invoke.</param>
        /// <remarks>The unmanaged array will be safelty cleared and deallocated after when the delegate exits for whatever reasons. (Unhandled exceptions or returned void).</remarks>
        /// <exception cref="ObjectDisposedException">The <seealso cref="SecureString"/> has already been disposed.</exception>
        public static void Reveal(this SecureString myself, SecretRevealedText revealed)
        {
            IntPtr pointer = IntPtr.Zero;
            try
            {
                pointer = Marshal.SecureStringToGlobalAllocUnicode(myself);
                ReadOnlySpan<char> span;
                unsafe
                {
                    span = new ReadOnlySpan<char>(pointer.ToPointer(), myself.Length);
                }
                revealed.Invoke(span);
            }
            finally
            {
                if (pointer != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(pointer);
                }
            }
        }

        public static void Import(this SecureString myself, byte[] data) => Import(myself, data, null);

        public static void Import(this SecureString myself, byte[] data, byte[] entropy)
        {
            byte[] buffer = null;
            try
            {
                buffer = ProtectedData.Unprotect(data, (entropy == null || entropy.Length == 0) ? _entropy : entropy, DataProtectionScope.CurrentUser);
                char[] chars = null;
                try
                {
                    chars = Encoding.Unicode.GetChars(buffer);
                    var span = chars.AsSpan();
                    myself.Clear();
                    for (int i = 0; i < chars.Length; i++)
                    {
                        myself.AppendChar(chars[i]);
                    }
                }
                finally
                {
                    if (chars != null)
                    {
                        Array.Fill<char>(chars, char.MinValue);
                    }
                }
            }
            finally
            {
                if (buffer != null)
                {
                    Array.Fill<byte>(buffer, 0);
                }
            }
        }

        public static SecureString Import(byte[] data) => Import(data, null);

        public static SecureString Import(byte[] data, byte[] entropy)
        {
            SecureString result = null;
            byte[] buffer = null;
            try
            {
                buffer = ProtectedData.Unprotect(data, (entropy == null || entropy.Length == 0) ? _entropy : entropy, DataProtectionScope.CurrentUser);
                char[] chars = null;
                try
                {
                    chars = Encoding.Unicode.GetChars(buffer);
                    var span = chars.AsSpan();
                    result = new SecureString();
                    for (int i = 0; i < chars.Length; i++)
                    {
                        result.AppendChar(chars[i]);
                    }
                }
                catch
                {
                    if (result != null)
                    {
                        result.Dispose();
                    }
                    throw;
                }
                finally
                {
                    if (chars != null)
                    {
                        Array.Fill<char>(chars, char.MinValue);
                    }   
                }
            }
            finally
            {
                if (buffer != null)
                {
                    Array.Fill<byte>(buffer, 0);
                }
            }
            return result;
        }

        public static byte[] Export(this SecureString myself) => Export(myself, null);

        public static byte[] Export(this SecureString myself, byte[] entropy)
        {
            byte[] result = null;
            Reveal(myself, (in ReadOnlySpan<char> chars) =>
            {
                byte[] buffer = null;
                try
                {
                    var writtenBytes = Encoding.Unicode.GetByteCount(chars);
                    buffer = new byte[writtenBytes];
                    Encoding.Unicode.GetBytes(chars, buffer);
                    result = ProtectedData.Protect(buffer, (entropy == null || entropy.Length == 0) ? _entropy : entropy, DataProtectionScope.CurrentUser);
                }
                finally
                {
                    if (buffer != null)
                    {
                        Array.Fill<byte>(buffer, 0);
                    }
                }
                
            });
            return result;
        }

        public static void EncodeTo(this SecureString myself, Stream stream, out int writtenBytes)
            => EncodeTo(myself, stream, Encoding.UTF8, out writtenBytes);

        public static void EncodeTo(this SecureString myself, Stream stream, Encoding encoding, out int writtenBytes)
        {
            if (myself == null)
            {
                throw new ArgumentNullException(nameof(myself));
            }
            if (!stream.CanWrite)
            {
                throw new ArgumentException(nameof(stream));
            }

            int byteWritten = 0;
            Reveal(myself, (in ReadOnlySpan<char> span) =>
            {
                byteWritten = encoding.GetByteCount(span);
                byte[] buffer = null;
                try
                {
                    buffer = new byte[byteWritten];
                    encoding.GetBytes(span, buffer);
                    stream.Write(buffer, 0, byteWritten);
                }
                finally
                {
                    if (buffer != null)
                    {
                        Array.Fill<byte>(buffer, 0);
                    }
                }
            });
            writtenBytes = byteWritten;
        }

        public static int GetByteCount(this SecureString myself, Encoding encoding)
        {
            if (myself == null)
            {
                throw new ArgumentNullException(nameof(myself));
            }

            int byteWritten = 0;
            Reveal(myself, (in ReadOnlySpan<char> span) =>
            {
                byteWritten = encoding.GetByteCount(span);
            });
            return byteWritten;
        }

        public delegate void SecretRevealedText(in ReadOnlySpan<char> characters);
    }
}

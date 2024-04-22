using System;
using System.Security;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

#nullable enable
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

        /// <summary>Invoke the <see cref="SecretRevealedText"/> delegate with the revealed password in form of an unmanaged array of <seealso cref="char"/>.</summary>
        /// <param name="myself">The <seealso cref="SecureString"/> to reveal.</param>
        /// <param name="revealed">The delegate to invoke.</param>
        /// <remarks>The unmanaged array will be safelty cleared and deallocated after when the delegate exits for whatever reasons. (Unhandled exceptions or returned void).</remarks>
        /// <exception cref="ObjectDisposedException">The <seealso cref="SecureString"/> has already been disposed.</exception>
        public static void Reveal<TArg>(this SecureString myself, SecretRevealedText<TArg> revealed, TArg arg)
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
                revealed.Invoke(span, arg);
            }
            finally
            {
                if (pointer != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(pointer);
                }
            }
        }

        /// <summary>Invoke the <see cref="SecretRevealedText"/> delegate with the revealed password in form of an unmanaged array of <seealso cref="char"/>.</summary>
        /// <param name="myself">The <seealso cref="SecureString"/> to reveal.</param>
        /// <param name="revealed">The delegate to invoke.</param>
        /// <remarks>The unmanaged array will be safelty cleared and deallocated after when the delegate exits for whatever reasons. (Unhandled exceptions or returned void).</remarks>
        /// <exception cref="ObjectDisposedException">The <seealso cref="SecureString"/> has already been disposed.</exception>
        public static TResult Reveal<TArg, TResult>(this SecureString myself, SecretRevealedText<TArg, TResult> revealed, TArg arg)
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
                return revealed.Invoke(span, arg);
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

        public static void Import(this SecureString myself, byte[] data, byte[]? entropy)
        {
            byte[]? buffer = null;
            char[]? chars = null;
            try
            {
                buffer = ProtectedData.Unprotect(data, (entropy == null || entropy.Length == 0) ? _entropy : entropy, DataProtectionScope.CurrentUser);
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
                    Array.Clear(chars);
                }
                if (buffer != null)
                {
                    Array.Clear(buffer);
                }
            }
        }

        public static SecureString Import(byte[] data) => Import(data, null);

        public static SecureString Import(byte[] data, byte[]? entropy)
        {
            SecureString? result = null;
            byte[]? buffer = null;
            try
            {
                buffer = ProtectedData.Unprotect(data, (entropy == null || entropy.Length == 0) ? _entropy : entropy, DataProtectionScope.CurrentUser);
                char[]? chars = null;
                int i = 0;
                try
                {
                    chars = Encoding.Unicode.GetChars(buffer);
                    var span = chars.AsSpan();
                    result = new SecureString();
                    for (i = 0; i < chars.Length; i++)
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
                        Array.Clear(chars, 0, i);
                    }   
                }
            }
            finally
            {
                if (buffer != null)
                {
                    Array.Clear(buffer);
                }
            }
            return result;
        }

        public static byte[] Export(this SecureString myself) => Export(myself, null);

        public static byte[] Export(this SecureString myself, byte[]? entropy)
            => Reveal(myself, (in ReadOnlySpan<char> chars, byte[] __entropy) =>
            {
                byte[]? buffer = null;
                try
                {
                    var writtenBytes = Encoding.Unicode.GetByteCount(chars);
                    buffer = new byte[writtenBytes];
                    Encoding.Unicode.GetBytes(chars, buffer);
                    
                    return ProtectedData.Protect(buffer, __entropy, DataProtectionScope.CurrentUser);
                }
                finally
                {
                    if (buffer != null)
                    {
                        Array.Clear(buffer);
                    }
                }
            }, (entropy == null || entropy.Length == 0) ? _entropy : entropy);

        public static bool Equals(this SecureString myself, SecureString other, StringComparison comparison)
        {
            IntPtr pointer1 = IntPtr.Zero, pointer2 = IntPtr.Zero;
            try
            {
                pointer1 = Marshal.SecureStringToGlobalAllocUnicode(myself);
                pointer2 = Marshal.SecureStringToGlobalAllocUnicode(other);
                ReadOnlySpan<char> span1, span2;
                unsafe
                {
                    span1 = new ReadOnlySpan<char>(pointer1.ToPointer(), myself.Length);
                    span2 = new ReadOnlySpan<char>(pointer2.ToPointer(), other.Length);
                }
                return MemoryExtensions.Equals(span1, span2, comparison);
            }
            finally
            {
                if (pointer1 != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(pointer1);
                }
                if (pointer2 != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(pointer2);
                }
            }
        }

        public static bool BinaryEquals(this SecureString myself, SecureString other)
        {
            IntPtr pointer1 = IntPtr.Zero, pointer2 = IntPtr.Zero;
            try
            {
                pointer1 = Marshal.SecureStringToGlobalAllocUnicode(myself);
                pointer2 = Marshal.SecureStringToGlobalAllocUnicode(other);
                ReadOnlySpan<char> span1, span2;
                unsafe
                {
                    span1 = new ReadOnlySpan<char>(pointer1.ToPointer(), myself.Length);
                    span2 = new ReadOnlySpan<char>(pointer2.ToPointer(), other.Length);
                }
                return MemoryExtensions.SequenceEqual(MemoryMarshal.Cast<char, byte>(span1), MemoryMarshal.Cast<char, byte>(span2));
            }
            finally
            {
                if (pointer1 != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(pointer1);
                }
                if (pointer2 != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(pointer2);
                }
            }
        }

        public static bool Equals(this SecureString myself, byte[] protectedData, StringComparison comparison) => Equals(myself, protectedData, null, comparison);

        public static bool Equals(this SecureString myself, byte[] protectedData, byte[]? entropy, StringComparison comparison)
        {
            byte[]? _buffer = null;
            try
            {
                _buffer = ProtectedData.Unprotect(protectedData, (entropy == null || entropy.Length == 0) ? _entropy : entropy, DataProtectionScope.CurrentUser);
                return Reveal(myself, (in ReadOnlySpan<char> myselfChars, (byte[] buffer, StringComparison comparison) args) =>
                {
                    char[]? chars = null;
                    try
                    {
                        chars = Encoding.Unicode.GetChars(args.buffer);
                        return MemoryExtensions.Equals(myselfChars, chars, args.comparison);
                    }
                    finally
                    {
                        if (chars != null)
                        {
                            Array.Clear(chars);
                        }
                    }
                }, (_buffer, comparison));
            }
            catch
            {
                return false;
            }
            finally
            {
                if (_buffer != null)
                {
                    Array.Clear(_buffer);
                }
            }
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

            IntPtr pointer = IntPtr.Zero;
            byte[]? buffer = null;
            try
            {
                pointer = Marshal.SecureStringToGlobalAllocUnicode(myself);
                ReadOnlySpan<char> span;
                unsafe
                {
                    span = new ReadOnlySpan<char>(pointer.ToPointer(), myself.Length);
                }
                writtenBytes = encoding.GetByteCount(span);
                buffer = new byte[writtenBytes];
                encoding.GetBytes(span, buffer);
                stream.Write(buffer, 0, writtenBytes);
            }
            finally
            {
                if (buffer != null)
                {
                    Array.Clear(buffer);
                }

                if (pointer != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(pointer);
                }
            }
        }

        public static int GetByteCount(this SecureString myself, Encoding encoding)
        {
            if (myself == null)
            {
                throw new ArgumentNullException(nameof(myself));
            }

            IntPtr pointer = IntPtr.Zero;
            try
            {
                pointer = Marshal.SecureStringToGlobalAllocUnicode(myself);
                ReadOnlySpan<char> span;
                unsafe
                {
                    span = new ReadOnlySpan<char>(pointer.ToPointer(), myself.Length);
                }
                return encoding.GetByteCount(span);
            }
            finally
            {
                if (pointer != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(pointer);
                }
            }
        }

        public delegate void SecretRevealedText(in ReadOnlySpan<char> characters);
        public delegate void SecretRevealedText<TArg>(in ReadOnlySpan<char> characters, TArg arg);
        public delegate TResult SecretRevealedText<TArg, TResult>(in ReadOnlySpan<char> characters, TArg arg);
    }
}
#nullable restore

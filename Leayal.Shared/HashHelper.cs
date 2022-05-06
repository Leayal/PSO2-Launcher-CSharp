using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.Shared
{
    public static class HashHelper
    {
        private static readonly EncodeToUtf16? m_EncodeToUtf16;
        // private static readonly uint m_EncodeToUtf32_case;

        private static readonly Lazy<char[]> hexChars = new Lazy<char[]>(() => new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' });

        static HashHelper()
        {
            if (Type.GetType("System.HexConverter", false, false) is Type t_HexConverter
                && Type.GetType("System.HexConverter+Casing", false, false) is Type t_Casing
                && t_HexConverter.GetMethod("EncodeToUtf16", new Type[] { typeof(ReadOnlySpan<byte>), typeof(Span<char>), t_Casing }) is MethodInfo methodInfo)
            {
                // This will be likely a disaster
                m_EncodeToUtf16 = methodInfo.CreateDelegate<EncodeToUtf16>();
            }
        }

        private delegate void EncodeToUtf16(ReadOnlySpan<byte> bytes, Span<char> chars, uint casing);

        public static bool IsFastMethodSupported => (m_EncodeToUtf16 != null);

        /// <summary>Encodes the hash from <paramref name="hash"/> into the back buffer of <paramref name="buffer"/> object.</summary>
        /// <param name="buffer">The object to find the back buffer.</param>
        /// <param name="hash">The computed hash to encode.</param>
        /// <returns>A boolean. True if the back buffer has enough space to store the encoded hash. Otherwise, false.</returns>
        public static bool TryWriteHashToHexString(string buffer, in ReadOnlySpan<byte> hash)
            => TryWriteHashToHexString(buffer, in hash, out _);

        /// <summary>Encodes the hash from <paramref name="hash"/> into the back buffer of <paramref name="buffer"/> object.</summary>
        /// <param name="buffer">The object to find the back buffer.</param>
        /// <param name="hash">The computed hash to encode.</param>
        /// <param name="writtenByteCount">The number of bytes written to the <paramref name="buffer"/></param>
        /// <returns>A boolean. True if the back buffer has enough space to store the encoded hash. Otherwise, false.</returns>
        public static bool TryWriteHashToHexString(string buffer, in ReadOnlySpan<byte> hash, out int writtenByteCount)
        {
            unsafe
            {
                fixed (char* c = buffer)
                {
                    var span = new Span<char>(c, buffer.Length);
                    return TryWriteHashToHexString(in span, in hash, out writtenByteCount);
                }
            }
        }

        /// <summary>Encodes the hash from <paramref name="hash"/> into the <paramref name="buffer"/> buffer.</summary>
        /// <param name="buffer">The buffer store the hex-encoded hash.</param>
        /// <param name="hash">The computed hash to encode.</param>
        /// <returns>A boolean. True if the buffer has enough space to store the encoded hash. Otherwise, false.</returns>
        public static bool TryWriteHashToHexString(in Span<char> buffer, in ReadOnlySpan<byte> hash)
            => TryWriteHashToHexString(in buffer, in hash, out _);

        /// <summary>Encodes the hash from <paramref name="hash"/> into the <paramref name="buffer"/> buffer.</summary>
        /// <param name="buffer">The buffer store the hex-encoded hash.</param>
        /// <param name="hash">The computed hash to encode.</param>
        /// <param name="writtenByteCount">The number of bytes written to the <paramref name="buffer"/></param>
        /// <returns>A boolean. True if the buffer has enough space to store the encoded hash. Otherwise, false.</returns>
        public static bool TryWriteHashToHexString(in Span<char> buffer, in ReadOnlySpan<byte> hash, out int writtenByteCount)
        {
            var lenInHex = hash.Length * 2;
            if (buffer == null || buffer.Length < lenInHex)
            {
                writtenByteCount = 0;
                return false;
            }
            
            if (m_EncodeToUtf16 == null)
            {
                var arr = hexChars.Value;
                for (int i = 0; i < hash.Length; i++)
                {
                    ref readonly var b = ref hash[i];
                    var y = i * 2;
                    buffer[y] = (arr[((b & 0xf0) >> 4)]);
                    buffer[y + 1] = (arr[(b & 0x0f)]);
                }
            } 
            else
            {
                m_EncodeToUtf16.Invoke(hash, buffer, 0u);
            }

            writtenByteCount = (lenInHex * 2);
            return true;
        }
    }
}

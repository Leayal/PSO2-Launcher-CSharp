using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace Leayal.Shared
{
    public static class Sha1StringHelper
    {
        public static string GenerateFromString(ReadOnlySpan<char> data) => GenerateFromString(data, Encoding.UTF8);

        public static string GenerateFromString(ReadOnlySpan<char> data, Encoding encoding) => GenerateFromString(data, encoding, false);

        public static string GenerateFromString(ReadOnlySpan<char> data, Encoding encoding, bool clearInternalBuffer)
        {
            using (var sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1))
            {
                var bufferLength = encoding.GetByteCount(data) + sha1.HashLengthInBytes;
                var buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
                try
                {
                    var encodedlen = encoding.GetBytes(data, buffer);
                    sha1.AppendData(buffer.AsSpan(0, encodedlen));
                    if (sha1.TryGetHashAndReset(buffer.AsSpan(encodedlen), out var hashsize))
                    {
                        return Convert.ToHexString(buffer.AsSpan(encodedlen, hashsize));
                    }
                    else
                    {
                        if (clearInternalBuffer)
                        {
                            var hashbuffer = sha1.GetCurrentHash();
                            try
                            {
                                return Convert.ToHexString(hashbuffer);
                            }
                            finally
                            {
                                Array.Clear(hashbuffer);
                            }
                        }
                        else
                        {
                            return Convert.ToHexString(sha1.GetCurrentHash());
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer, clearInternalBuffer);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2.Modding
{
    static class HelperMethods
    {
        public static string ComputeHashFromFile(Stream stream)
        {
            if (!stream.CanRead)
            {
                throw new ArgumentException("The stream must be readable.", nameof(stream));
            }

            using (var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5))
            using (var borrowedMemory = System.Buffers.MemoryPool<byte>.Shared.Rent(4096))
            {
                var buffer = borrowedMemory.Memory;
                int read;
                while ((read = stream.Read(buffer.Span)) != 0)
                {
                    md5.AppendData(buffer.Slice(0, read).Span);
                }
                if (md5.TryGetCurrentHash(buffer.Span, out var writtenBytes))
                {
                    return Convert.ToHexString(buffer.Slice(0, writtenBytes).Span);
                }
                else
                {
                    return Convert.ToHexString(md5.GetCurrentHash());
                }
            }
        }
    }
}

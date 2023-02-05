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
            {
                var borrowedMemory = System.Buffers.ArrayPool<byte>.Shared.Rent(4096);
                try
                {
                    var buffer = borrowedMemory.AsSpan();
                    int read;
                    while ((read = stream.Read(buffer)) != 0)
                    {
                        md5.AppendData(buffer.Slice(0, read));
                    }
                    if (md5.TryGetCurrentHash(buffer, out var writtenBytes))
                    {
                        return Convert.ToHexString(buffer.Slice(0, writtenBytes));
                    }
                    else
                    {
                        return Convert.ToHexString(md5.GetCurrentHash());
                    }
                }
                finally
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(borrowedMemory);
                }
            }
        }
    }
}

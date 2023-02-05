using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace Leayal.PSO2Launcher.Helper
{
    public static class SHA1Hash
    {
        public static Task<string> ComputeHashFromFileAsync(string filename) => ComputeHashFromFileAsync(filename, CancellationToken.None);

        public static async Task<string> ComputeHashFromFileAsync(string filename, CancellationToken cancellationToken)
        {
            using (var fs = File.OpenRead(filename))
            {
                return await ComputeHashFromFileAsync(fs, cancellationToken);
            }
        }

        public static Task<string> ComputeHashFromFileAsync(Stream stream) => ComputeHashFromFileAsync(stream, CancellationToken.None);

        public static async Task<string> ComputeHashFromFileAsync(Stream stream, CancellationToken cancellationToken)
        {
            if (!stream.CanRead)
            {
                throw new ArgumentException("The stream must be readable.", nameof(stream));
            }

            using (var md5 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1))
            {
                var borrowedMemory = System.Buffers.ArrayPool<byte>.Shared.Rent(4096);
                try
                {
                    var buffer = new Memory<byte>(borrowedMemory);
                    int read;
                    while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
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
                finally
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(borrowedMemory);
                }
            }
        }

        public static string ComputeHashFromFile(string filename)
        {
            using (var fs = File.OpenRead(filename))
            {
                return ComputeHashFromFile(fs);
            }
        }

        public static string ComputeHashFromFile(Stream stream)
        {
            if (!stream.CanRead)
            {
                throw new ArgumentException("The stream must be readable.", nameof(stream));
            }

            using (var md5 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1))
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

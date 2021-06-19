using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
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
                return await ComputeHashFromFileAsync(fs);
            }
        }

        public static Task<string> ComputeHashFromFileAsync(Stream stream) => ComputeHashFromFileAsync(stream, CancellationToken.None);

        public static async Task<string> ComputeHashFromFileAsync(Stream stream, CancellationToken cancellationToken)
        {
            if (!stream.CanRead)
            {
                throw new ArgumentException("The stream must be readable.", nameof(stream));
            }

            SHA1 sha1;
            byte[] bytes;
            try
            {
                sha1 = new SHA1Managed();
            }
            catch (InvalidOperationException)
            {
                sha1 = SHA1.Create();
            }
            try
            {
                bytes = await sha1.ComputeHashAsync(stream, cancellationToken);
            }
            finally
            {
                sha1.Dispose();
            }
            return Convert.ToHexString(bytes);
        }
    }
}

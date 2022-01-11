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

            using (var sha1 = SHA1.Create())
            {
                var bytes = await sha1.ComputeHashAsync(stream, cancellationToken);
                return Convert.ToHexString(bytes);
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

            using (var sha1 = SHA1.Create())
            {
                var bytes = sha1.ComputeHash(stream);
                return Convert.ToHexString(bytes);
            }
        }
    }
}

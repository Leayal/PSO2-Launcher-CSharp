using System;
using System.IO;

namespace Leayal.Shared
{
    public static class IOHelper
    {
        /// <summary>Reads a stream. Returns only when the required numbers of bytes has been read or when the stream is closed beforehand.</summary>
        /// <param name="stream">The stream to read data.</param>
        /// <param name="buffer">The buffer to fill the data in.</param>
        /// <param name="offset">The offset of the buffer to start filling data to.</param>
        /// <param name="buffer">The required number of bytes to read from the stream.</param>
        /// <returns>The number of bytes that have been read. If the stream is closed beforehand, the number of bytes maybe smaller than the required number.</returns>
        /// <exception cref="ArgumentException">The <seealso cref="Stream.CanRead"/> of <paramref name="stream"/> is <see langword="false"/>.</exception>
        public static int ReadEnsuredLength(this Stream stream, byte[] buffer, int offset, int length)
            => ReadEnsuredLength(stream, new Span<byte>(buffer, offset, length));

        /// <summary>Reads a stream. Returns only when the required numbers of bytes has been read or when the stream is closed beforehand.</summary>
        /// <param name="stream">The stream to read data.</param>
        /// <param name="buffer">The buffer to fill the data in. The length of this buffer is the required number of bytes to read.</param>
        /// <returns>The number of bytes that have been read. If the stream is closed beforehand, the number of bytes maybe smaller than the required number.</returns>
        /// <exception cref="ArgumentException">The <seealso cref="Stream.CanRead"/> of <paramref name="stream"/> is <see langword="false"/>.</exception>
        public static int ReadEnsuredLength(this Stream stream, Span<byte> buffer)
        {
            if (!stream.CanRead) throw new ArgumentException(nameof(stream));

            int read, lengthLeft = buffer.Length;
            var currentOffsetBuffer = buffer;
            while ((read = stream.Read(currentOffsetBuffer)) != 0)
            {
                lengthLeft -= read;
                if (lengthLeft == 0) break;
                currentOffsetBuffer = currentOffsetBuffer.Slice(read);
            }

            return buffer.Length - lengthLeft;
        }
    }
}

using System;
using System.IO;
using System.Text;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;

namespace Leayal.Shared
{
    public static class FileHelper
    {
        /// <summary>Checks whether the file is either not existed or is empty.</summary>
        /// <param name="filepath">The path to the file to check.</param>
        /// <returns>If the file is not existed or is existed but empty, return true. Otherwise, false.</returns>
        public static bool IsNotExistsOrZeroLength(string filepath)
        {
            if (File.Exists(filepath))
            {
                return (GetFileSize(filepath) == 0L);
            }
            else
            {
                return true;
            }
        }

        /// <summary>Gets the size (in bytes) of a file from the path.</summary>
        /// <param name="path">A relative or absolute path for the file.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"><paramref name="path" /> refers to a non-file device, such as <c>CON:</c>, <c>COM1:</c>, <c>LPT1:</c>, etc. in a non-NTFS environment.</exception>
        /// <exception cref="FileNotFoundException">The file cannot be found, such as when <see cref="FileStreamOptions.Mode" /> is <see langword="FileMode.Truncate" /> or <see langword="FileMode.Open" />, and the file specified by <paramref name="path" /> does not exist. The file must already exist in these modes.</exception>
        /// <exception cref="IOException">An I/O error, such as specifying <see langword="FileMode.CreateNew" /> when the file specified by <paramref name="path" /> already exists, occurred.
        ///  -or-
        ///  The stream has been closed.
        ///  -or-
        ///  The disk was full (when <see cref="FileStreamOptions.PreallocationSize" /> was provided and <paramref name="path" /> was pointing to a regular file).
        ///  -or-
        ///  The file was too large (when <see cref="FileStreamOptions.PreallocationSize" /> was provided and <paramref name="path" /> was pointing to a regular file).</exception>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. </exception>
        public static long GetFileSize(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 0))
            {
                return fs.Length;
            }
        }

        /// <summary>Reads character data from a file into a memory buffer of <seealso cref="char"/>.</summary>
        /// <param name="filepath">The file to read everything from.</param>
        /// <param name="encoding">The encoding which will be used to decode the data.</param>
        /// <returns>An immutable memory which contains the all character data read from the <paramref name="filepath"/>.</returns>
        public static ReadOnlyMemory<char> ReadAllTexts(string filepath, Encoding encoding)
        {
            using (var fs = File.OpenRead(filepath))
            using (var stream = new StreamReader(fs, encoding))
                return ReadAllTexts(stream, stream.CurrentEncoding, fs.Length);
        }

        /// <summary>Reads character data from a file into a memory buffer of <seealso cref="char"/>.</summary>
        /// <param name="filepath">The file to read everything from.</param>
        /// <returns>An immutable memory which contains the all character data read from the <paramref name="filepath"/>.</returns>
        public static ReadOnlyMemory<char> ReadAllTexts(string filepath)
        {
            using (var fs = File.OpenRead(filepath))
            using (var stream = new StreamReader(fs))
                return ReadAllTexts(stream, stream.CurrentEncoding, fs.Length);
        }

        /// <summary>Reads character data from a text reader into a memory buffer of <seealso cref="char"/>.</summary>
        /// <param name="reader">The source to read from.</param>
        /// <param name="encoding">The encoding which will be used to decode the data.</param>
        /// <param name="length">The number of bytes to read and be decoded into <seealso cref="char"/>.</param>
        /// <returns>An immutable memory which contains the character data read from the <paramref name="reader"/> with the given number of bytes from <paramref name="length"/>.</returns>
        private static ReadOnlyMemory<char> ReadAllTexts(TextReader reader, Encoding encoding, long length)
        {
            ArrayBufferWriter<char> buffer;
            if (encoding.IsSingleByte)
            {
                buffer = new ArrayBufferWriter<char>((int)length);
            }
            else
            {
                double len = length / 2;
                int bufferLenth = (int)Math.Ceiling(len);
                buffer = new ArrayBufferWriter<char>(bufferLenth);
            }
            int readCount = reader.Read(buffer.GetSpan());
            while (readCount > 0)
            {
                buffer.Advance(readCount);
                readCount = reader.Read(buffer.GetSpan());
            }

            return buffer.WrittenMemory;
        }

        /// <summary>Reads character data from a file into a memory buffer of <seealso cref="char"/>.</summary>
        /// <param name="filepath">The file to read everything from.</param>
        /// <param name="encoding">The encoding which will be used to decode the data.</param>
        /// <returns>A mutable memory which contains the all character data read from the <paramref name="filepath"/>.</returns>
        public static Memory<char> TakeAllTexts(string filepath, Encoding encoding)
        {
            using (var fs = File.OpenRead(filepath))
            using (var stream = new StreamReader(fs, encoding))
                return TakeAllTexts(stream, stream.CurrentEncoding, fs.Length);
        }

        /// <summary>Reads character data from a file into a memory buffer of <seealso cref="char"/>.</summary>
        /// <param name="filepath">The file to read everything from.</param>
        /// <returns>A mutable memory which contains the all character data read from the <paramref name="filepath"/>.</returns>
        public static Memory<char> TakeAllTexts(string filepath)
        {
            using (var fs = File.OpenRead(filepath))
            using (var stream = new StreamReader(fs))
                return TakeAllTexts(stream, stream.CurrentEncoding, fs.Length);
        }

        /// <summary>Reads character data from a text reader into a memory buffer of <seealso cref="char"/>.</summary>
        /// <param name="reader">The source to read from.</param>
        /// <param name="encoding">The encoding which will be used to decode the data.</param>
        /// <param name="length">The number of bytes to read and be decoded into <seealso cref="char"/>.</param>
        /// <returns>A mutable memory which contains the character data read from the <paramref name="reader"/> with the given number of bytes from <paramref name="length"/>.</returns>
        private static Memory<char> TakeAllTexts(TextReader reader, Encoding encoding, long length)
        {
            char[] buffer;
            if (encoding.IsSingleByte)
            {
                buffer = new char[length];
            }
            else
            {
                double len = length / 2;
                int bufferLenth = (int)Math.Ceiling(len);
                buffer = new char[bufferLenth];
            }
            var span = buffer.AsSpan();
            int offset = 0;
            int readCount = reader.Read(span);
            while (readCount > 0)
            {
                offset += readCount;
                readCount = reader.Read(span.Slice(offset));
            }

            return new Memory<char>(buffer, 0, offset);
        }

        /// <summary>Buffering character data from a file.</summary>
        /// <param name="filepath">The file to buffer all text.</param>
        /// <returns>A enumerable of immutable memory which contains the character data read from the file.</returns>
        public static IEnumerable<ReadOnlyMemory<char>> EnumerateAllChars(string filepath) => EnumerateAllChars(filepath, null);

        /// <summary>Buffering character data from a file.</summary>
        /// <param name="filepath">The file to buffer all text.</param>
        /// <param name="pool">The pool to rent and return buffers. If null, allocate a new buffer instead of renting.</param>
        /// <returns>A enumerable of immutable memory which contains the character data read from the file.</returns>
        public static IEnumerable<ReadOnlyMemory<char>> EnumerateAllChars(string filepath, ArrayPool<char>? pool) => EnumerateAllChars(filepath, 2048, pool);

        /// <summary>Buffering character data from a file.</summary>
        /// <param name="filepath">The file to buffer all text.</param>
        /// <param name="bufferSize">The minimum size (in bytes) per each buffer.</param>
        /// <returns>A enumerable of immutable memory which contains the character data read from the file.</returns>
        public static IEnumerable<ReadOnlyMemory<char>> EnumerateAllChars(string filepath, int bufferSize) => EnumerateAllChars(filepath, bufferSize, null);

        /// <summary>Buffering character data from a file.</summary>
        /// <param name="filepath">The file to buffer all text.</param>
        /// <param name="bufferSize">The minimum size (in bytes) per each buffer.</param>
        /// <param name="pool">The pool to rent and return buffers. If null, allocate a new buffer instead of renting.</param>
        /// <returns>A enumerable of immutable memory which contains the character data read from the file.</returns>
        public static IEnumerable<ReadOnlyMemory<char>> EnumerateAllChars(string filepath, int bufferSize, ArrayPool<char>? pool) => new TextEnumberableStuff(filepath, bufferSize, pool);

        private readonly struct TextEnumberableStuff : IEnumerable<ReadOnlyMemory<char>>
        {
            private readonly string filepath;
            private readonly int buffersize;
            private readonly ArrayPool<char>? pool;

            public TextEnumberableStuff(string filepath, int buffersize, ArrayPool<char>? pool)
            {
                this.filepath = filepath;
                this.buffersize = buffersize;
                this.pool = pool;
            }

            public IEnumerator<ReadOnlyMemory<char>> GetEnumerator() => new TextBufferer(new StreamReader(this.filepath), this.buffersize, this.pool);

            IEnumerator IEnumerable.GetEnumerator() => new TextBufferer(new StreamReader(this.filepath), this.buffersize, this.pool);
        }

        public static bool HasReadOnlyFlag(this FileAttributes attributes) => ((attributes & FileAttributes.ReadOnly) != 0);

        private class TextBufferer : IEnumerator<ReadOnlyMemory<char>>
        {
            private readonly char[] buffer;
            private readonly ArrayPool<char>? _pool;
            private readonly StreamReader reader;
            private readonly long initialpos;

            private int len;

            public TextBufferer(StreamReader sr, int bufferSize) : this(sr, bufferSize, null) { }

            public TextBufferer(StreamReader sr, int bufferSize, ArrayPool<char>? pool)
            {
                if (bufferSize <= 8) bufferSize = 8;
                this.reader = sr ?? throw new ArgumentNullException(nameof(sr));
                this._pool = pool;
                if (pool == null)
                {
                    this.buffer = new char[bufferSize];
                }
                else
                {
                    this.buffer = pool.Rent(bufferSize);
                }
                this.len = 0;
                var stream = sr.BaseStream;
                if (stream.CanSeek)
                {
                    this.initialpos = stream.Position;
                }
                else
                {
                    this.initialpos = -1;
                }
            }

            public ReadOnlyMemory<char> Current => this.buffer.AsMemory(0, this.len);

            object IEnumerator.Current => this.buffer.AsMemory(0, this.len);

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                this.Dispose(true);
            }

            private void Dispose(bool disposing)
            {
                this._pool?.Return(this.buffer);
                this.reader.Dispose();
            }

            ~TextBufferer()
            {
                this.Dispose(false);
            }

            public bool MoveNext()
            {
                this.len = this.reader.Read(this.buffer, 0, this.buffer.Length);
                return (this.len != 0);
            }

            public void Reset()
            {
                if (this.initialpos == -1)
                {
                    throw new NotSupportedException();
                }
                else
                {
                    this.reader.BaseStream.Seek(this.initialpos, SeekOrigin.Begin);
                }
            }
        }
    }
}

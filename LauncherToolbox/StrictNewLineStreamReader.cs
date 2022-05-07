using System;
using System.Collections.Generic;
using System.Buffers;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Toolbox
{
    /// <summary>Implements a <seealso cref="TextReader"/> that reads characters from a byte stream in a particular encoding and with a strict line-ending read.</summary>
    public class StrictNewLineStreamReader : StreamReader
    {
        private readonly Bufferer<char> bufferer;

        /// <inheritdoc/>
        public StrictNewLineStreamReader(Stream stream) : base(stream)
        {
            this.bufferer = new Bufferer<char>(ArrayPool<char>.Shared);
        }

        /// <inheritdoc/>
        public StrictNewLineStreamReader(string path) : base(path)
        {
            this.bufferer = new Bufferer<char>(ArrayPool<char>.Shared);
        }

        /// <inheritdoc/>
        public StrictNewLineStreamReader(Stream stream, bool detectEncodingFromByteOrderMarks) : base(stream, detectEncodingFromByteOrderMarks)
        {
            this.bufferer = new Bufferer<char>(ArrayPool<char>.Shared);
        }
        
        /// <inheritdoc/>
        public StrictNewLineStreamReader(Stream stream, Encoding encoding) : base(stream, encoding)
        {
            this.bufferer = new Bufferer<char>(ArrayPool<char>.Shared);
        }

        /// <inheritdoc/>
        public StrictNewLineStreamReader(string path, FileStreamOptions options) : base(path, options)
        {
            this.bufferer = new Bufferer<char>(ArrayPool<char>.Shared);
        }

        /// <inheritdoc/>
        public StrictNewLineStreamReader(string path, bool detectEncodingFromByteOrderMarks) : base(path, detectEncodingFromByteOrderMarks)
        {
            this.bufferer = new Bufferer<char>(ArrayPool<char>.Shared);
        }

        /// <inheritdoc/>
        public StrictNewLineStreamReader(string path, Encoding encoding) : base(path, encoding)
        {
            this.bufferer = new Bufferer<char>(ArrayPool<char>.Shared);
        }

        /// <inheritdoc/>
        public StrictNewLineStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks) : base(stream, encoding, detectEncodingFromByteOrderMarks)
        {
            this.bufferer = new Bufferer<char>(ArrayPool<char>.Shared);
        }

        /// <inheritdoc/>
        public StrictNewLineStreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks) : base(path, encoding, detectEncodingFromByteOrderMarks)
        {
            this.bufferer = new Bufferer<char>(ArrayPool<char>.Shared);
        }

        /// <inheritdoc/>
        public StrictNewLineStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize) : base(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize)
        {
            this.bufferer = new Bufferer<char>(bufferSize < 0 ? 0 : bufferSize, ArrayPool<char>.Shared);
        }

        /// <inheritdoc/>
        public StrictNewLineStreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize) : base(path, encoding, detectEncodingFromByteOrderMarks, bufferSize)
        {
            this.bufferer = new Bufferer<char>(bufferSize < 0 ? 0 : bufferSize, ArrayPool<char>.Shared);
        }

        /// <inheritdoc/>
        public StrictNewLineStreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, FileStreamOptions options) : base(path, encoding, detectEncodingFromByteOrderMarks, options)
        {
            this.bufferer = new Bufferer<char>(ArrayPool<char>.Shared);
        }

        /// <inheritdoc/>
        public StrictNewLineStreamReader(Stream stream, Encoding? encoding = null, bool detectEncodingFromByteOrderMarks = true, int bufferSize = -1, bool leaveOpen = false) : base(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen)
        {
            this.bufferer = new Bufferer<char>(bufferSize < 0 ? 0 : bufferSize, ArrayPool<char>.Shared);
        }

        /// <summary>Reads a line of characters asynchronously from the current stream and returns the data as a string.</summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <seealso cref="CancellationToken.None"/>.</param>
        /// <returns>A task that represents the asynchronous read operation. The value of the task parameter contains the next line from the stream, or is null if the operation is cancelled.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The number of characters in the next line is larger than <seealso cref="int.MaxValue"/>.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader is currently in use by a previous read operation.</exception>
        public async Task<string?> StrictLineReadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }
                else
                {
                    var readMem = this.bufferer.WrittenMemory;
                    var takeOutLen = readMem.Span.IndexOf('\n');
                    if (takeOutLen != -1)
                    {
                        var result = new string(readMem.Slice(0, takeOutLen).Span.TrimEnd('\r'));
                        this.bufferer.Clear(0, takeOutLen + 1);
                        return result;
                    }
                }
                while (!cancellationToken.IsCancellationRequested)
                {
                    var mem = this.bufferer.GetMemory();
                    var readCount = await base.ReadAsync(mem, cancellationToken).ConfigureAwait(false);
                    while (!cancellationToken.IsCancellationRequested && readCount == 0)
                    {
                        await Task.Delay(30, cancellationToken).ConfigureAwait(false);
                        readCount = await base.ReadAsync(mem, cancellationToken).ConfigureAwait(false);
                    }
                    this.bufferer.Advance(readCount);
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        var readMem = mem.Slice(0, readCount);
                        var indexOfNewLine = readMem.Span.IndexOf('\n');
                        if (indexOfNewLine != -1)
                        {
                            var totalLen = this.bufferer.WrittenCount;
                            var takeOutLen = totalLen - (readCount - indexOfNewLine);
                            var result = new string(this.bufferer.WrittenMemory.Slice(0, takeOutLen).Span.TrimEnd('\r'));
                            this.bufferer.Clear(0, takeOutLen + 1);
                            return result;
                        }
                    }
                }
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        /// <summary>Reads a line of characters asynchronously from the current stream and returns the data as a string.</summary>
        /// <param name="callbackOnNewLineFound">The callback method which will be invoked once a line of characters is found.</param>
        /// <param name="args">The argument which will be passed onto the callback.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <seealso cref="CancellationToken.None"/>.</param>
        /// <returns>A task that represents the asynchronous read operation. The value of the task parameter contains the next line from the stream, or is null if the operation is cancelled.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The number of characters in the next line is larger than <seealso cref="int.MaxValue"/>.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader is currently in use by a previous read operation.</exception>
        public async Task UseStrictLineReadAsync<TArgs>(Func<ReadOnlyMemory<char>, TArgs, Task> callbackOnNewLineFound, TArgs args, CancellationToken cancellationToken)
        {
            if (callbackOnNewLineFound == null)
            {
                throw new ArgumentNullException(nameof(callbackOnNewLineFound));
            }
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }
            else
            {
                var readMem = this.bufferer.WrittenMemory;
                var takeOutLen = readMem.Span.IndexOf('\n');
                if (takeOutLen != -1)
                {
                    var callbackMem = readMem.Slice(0, takeOutLen);
                    var realLength = callbackMem.Span.TrimEnd('\r').Length;
                    if (callbackMem.Length != realLength)
                    {
                        callbackMem = callbackMem.Slice(0, realLength);
                    }
                    await callbackOnNewLineFound.Invoke(callbackMem, args);
                    this.bufferer.Clear(0, takeOutLen + 1);
                }
            }
            while (!cancellationToken.IsCancellationRequested)
            {
                var mem = this.bufferer.GetMemory();
                var readCount = await base.ReadAsync(mem, cancellationToken).ConfigureAwait(false);
                while (!cancellationToken.IsCancellationRequested && readCount == 0)
                {
                    await Task.Delay(30, cancellationToken).ConfigureAwait(false);
                    readCount = await base.ReadAsync(mem, cancellationToken).ConfigureAwait(false);
                }
                this.bufferer.Advance(readCount);
                if (!cancellationToken.IsCancellationRequested)
                {
                    var readMem = mem.Slice(0, readCount);
                    var indexOfNewLine = readMem.Span.IndexOf('\n');
                    if (indexOfNewLine != -1)
                    {
                        var totalLen = this.bufferer.WrittenCount;
                        var takeOutLen = totalLen - (readCount - indexOfNewLine);
                        var callbackMem = this.bufferer.WrittenMemory.Slice(0, takeOutLen);
                        var realLength = callbackMem.Span.TrimEnd('\r').Length;
                        if (callbackMem.Length != realLength)
                        {
                            callbackMem = callbackMem.Slice(0, realLength);
                        }
                        await callbackOnNewLineFound.Invoke(callbackMem, args);
                        this.bufferer.Clear(0, takeOutLen + 1);
                    }
                }
            }
        }

        /// <summary>Reads a line of characters asynchronously from the current stream and returns the data as a string.</summary>
        /// <param name="callbackOnNewLineFound">The callback method which will be invoked once a line of characters is found.</param>
        /// <param name="args">The argument which will be passed onto the callback.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <seealso cref="CancellationToken.None"/>.</param>
        /// <returns>A task that represents the asynchronous read operation. The value of the task parameter contains the next line from the stream, or is null if the operation is cancelled.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The number of characters in the next line is larger than <seealso cref="int.MaxValue"/>.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader is currently in use by a previous read operation.</exception>
        public async Task UseStrictLineReadAsync<TArgs>(Action<ReadOnlyMemory<char>, TArgs> callbackOnNewLineFound, TArgs args, CancellationToken cancellationToken)
        {
            if (callbackOnNewLineFound == null)
            {
                throw new ArgumentNullException(nameof(callbackOnNewLineFound));
            }
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }
            else
            {
                var readMem = this.bufferer.WrittenMemory;
                var takeOutLen = readMem.Span.IndexOf('\n');
                if (takeOutLen != -1)
                {
                    var callbackMem = readMem.Slice(0, takeOutLen);
                    var realLength = callbackMem.Span.TrimEnd('\r').Length;
                    if (callbackMem.Length != realLength)
                    {
                        callbackMem = callbackMem.Slice(0, realLength);
                    }
                    callbackOnNewLineFound.Invoke(callbackMem, args);
                    this.bufferer.Clear(0, takeOutLen + 1);
                }
            }
            while (!cancellationToken.IsCancellationRequested)
            {
                var mem = this.bufferer.GetMemory();
                var readCount = await base.ReadAsync(mem, cancellationToken).ConfigureAwait(false);
                while (!cancellationToken.IsCancellationRequested && readCount == 0)
                {
                    await Task.Delay(30, cancellationToken).ConfigureAwait(false);
                    readCount = await base.ReadAsync(mem, cancellationToken).ConfigureAwait(false);
                }
                this.bufferer.Advance(readCount);
                if (!cancellationToken.IsCancellationRequested)
                {
                    var readMem = mem.Slice(0, readCount);
                    var indexOfNewLine = readMem.Span.IndexOf('\n');
                    if (indexOfNewLine != -1)
                    {
                        var totalLen = this.bufferer.WrittenCount;
                        var takeOutLen = totalLen - (readCount - indexOfNewLine);
                        var callbackMem = this.bufferer.WrittenMemory.Slice(0, takeOutLen);
                        var realLength = callbackMem.Span.TrimEnd('\r').Length;
                        if (callbackMem.Length != realLength)
                        {
                            callbackMem = callbackMem.Slice(0, realLength);
                        }
                        callbackOnNewLineFound.Invoke(callbackMem, args);
                        this.bufferer.Clear(0, takeOutLen + 1);
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.bufferer.Dispose();
        }
    }
}

using System.Buffers;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;

namespace Leayal.PSO2Launcher.Toolbox
{
    /// <summary>Provides an implementation to listen for data from a PSO2 log file in async manner.</summary>
    /// <remarks>
    /// <para>The class will keep trying to read for more data when it reach the end of file. To stop waiting for incoming data, call <seealso cref="Dispose()"/>.</para>
    /// <para>This class is single-use. Once started, it can only be stopped by calling <seealso cref="Dispose()"/>. Likewise, once stopped, it can't be started again.</para>
    /// </remarks>
    public class PSO2LogAsyncListener : IDisposable
    {
        /// <summary>Open the log file to read all log data and then close the file.</summary>
        /// <param name="filepath">The file path to the log file.</param>
        /// <param name="onLogDataFound">The callback which will be invoked when a log data is read.</param>
        /// <returns>An enumerator that provides asynchronous iteration over log data wrapped by <seealso cref="PSO2LogData"/>.</returns>
        /// <remarks>This static method will open the file, read to end of file and then close the file. Any log data appeared after closing will not be read.</remarks>
        public static Task FetchAllData(string filepath, DataReceivedCallback onLogDataFound)
            => FetchAllData(new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), false, CancellationToken.None, onLogDataFound);

        /// <summary>Open the log file to read all log data and then close the file.</summary>
        /// <param name="fs">The stream of the log file.</param>
        /// <param name="onLogDataFound">The callback which will be invoked when a log data is read.</param>
        /// <returns>An enumerator that provides asynchronous iteration over log data wrapped by <seealso cref="PSO2LogData"/>.</returns>
        /// <remarks>This static method will open the file, read to end of file and then close the file. Any log data appeared after closing will not be read.</remarks>
        public static Task FetchAllData(FileStream fs, DataReceivedCallback onLogDataFound)
            => FetchAllData(fs, false, CancellationToken.None, onLogDataFound);

        /// <summary>Open the log file to read all log data and then close the file.</summary>
        /// <param name="filepath">The file path to the log file.</param>
        /// <param name="cancellationToken">The cancellation token to notify that the operation should be cancelled.</param>
        /// <param name="onLogDataFound">The callback which will be invoked when a log data is read.</param>
        /// <returns>An enumerator that provides asynchronous iteration over log data wrapped by <seealso cref="PSO2LogData"/>.</returns>
        /// <remarks>This static method will open the file, read to end of file and then close the file. Any log data appeared after closing will not be read.</remarks>
        public static Task FetchAllData(string filepath, CancellationToken cancellationToken, DataReceivedCallback onLogDataFound)
            => FetchAllData(new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), false, cancellationToken, onLogDataFound);

        /// <summary>Open the log file to read all log data and then close the file.</summary>
        /// <param name="fs">The stream of the log file.</param>
        /// <param name="cancellationToken">The cancellation token to notify that the operation should be cancelled.</param>
        /// <param name="onLogDataFound">The callback which will be invoked when a log data is read.</param>
        /// <returns>An enumerator that provides asynchronous iteration over log data wrapped by <seealso cref="PSO2LogData"/>.</returns>
        /// <remarks>This static method will open the file, read to end of file and then close the file. Any log data appeared after closing will not be read.</remarks>
        public static Task FetchAllData(FileStream fs, CancellationToken cancellationToken, DataReceivedCallback onLogDataFound)
            => FetchAllData(fs, false, cancellationToken, onLogDataFound);

        /// <summary>Open the log file to read all log data and then close the file if <paramref name="keepOpen"/> is false.</summary>
        /// <param name="fs">The stream of the log file.</param>
        /// <param name="keepOpen">A boolean determines whether <paramref name="fs"/> should be keep open after the read..</param>
        /// <param name="cancellationToken">The cancellation token to notify that the operation should be cancelled.</param>
        /// <param name="onLogDataFound">The callback which will be invoked when a log data is read.</param>
        /// <returns>An enumerator that provides asynchronous iteration over log data wrapped by <seealso cref="PSO2LogData"/>.</returns>
        /// <remarks>This static method will open the file, read to end of file and then close the file. Any log data appeared after closing will not be read.</remarks>
        public static async Task FetchAllData(FileStream fs, bool keepOpen, CancellationToken cancellationToken, DataReceivedCallback onLogDataFound)
        {
            await using (var reader = new PSO2LogAsyncReader(fs, keepOpen, onLogDataFound, cancellationToken))
            {
                await reader.StartOperation().ConfigureAwait(false);
            }
        }

        // private readonly PeriodicTimer delay;
        private readonly CancellationTokenSource cancelSrc;
        private readonly FileStream fs;
        private readonly bool _leaveOpen;
        private bool _disposed;
        private int _state;

        /// <summary>Gets the full path to the log file.</summary>
        public string Fullpath => this.fs.Name;

        /// <summary>Creates a new instance with the given file path.</summary>
        /// <param name="filepath">The path to the log file.</param>
        public PSO2LogAsyncListener(string filepath) : this(new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true), false) { }

        /// <summary>Creates a new instance with the given <seealso cref="FileStream"/> that will close the <paramref name="fs"/> stream when the instance is disposed.</summary>
        /// <param name="fs">The file stream to read the log.</param>
        /// <remarks>This is equal to with <code>PSO2LogAsyncReader(FileStream, false)</code>.</remarks>
        public PSO2LogAsyncListener(FileStream fs) : this(fs, false) { }

        /// <summary>Creates a new instance with the given <seealso cref="FileStream"/></summary>
        /// <param name="fs">The file stream to read the log.</param>
        /// <param name="keepOpen">The boolean to determine whether the <paramref name="fs"/> stream should be closed when this instance is disposed.</param>
        public PSO2LogAsyncListener(FileStream fs, bool keepOpen)
        {
            // this.delay = new PeriodicTimer(TimeSpan.FromMilliseconds(10));
            this.cancelSrc = new CancellationTokenSource();
            this._leaveOpen = keepOpen;
            this.fs = fs;
            this._state = 0;
        }

        /// <summary>Begin the async operation.</summary>
        /// <remarks>When log data is available, the <seealso cref="DataReceived"/> event will be invoked with the given data.</remarks>
        public void StartReceiving()
        {
            if (Interlocked.CompareExchange(ref this._state, 1, 0) == 0)
            {
                // Because Overlapped File IO operation doesn't guarantee to be async.
                // We will use a background thread to ensure all the work won't be on the UI thread (avoid blocking responsiveness of the UI)
                Task.Factory.StartNew(this.ReadFileAsync, TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        private async Task ReadFileAsync()
        {
            // All the "ConfigureAwait(false)" below are unnecessary as this method/function is run on a background thread/threadpool's thread.
            // And background thread has no sync context. Thus, ConfigureAwait with false or true here doesn't mean anything.
            // But the intention is there so I will explictly put them here.

            // Use 2048 char[] buffer (aka 4096-byte buffer, as .NET char is a 2-byte Unicode-encoded char) to ensure we will never need to allocate a second time.
            // This is also to avoid the async IO overhead as we will read a big bulk at once per async IO call.
            using (var bufferer = new Bufferer<char>(2048, ArrayPool<char>.Shared)) // of v3 draft, put everything into this class instead of making a new StreamReader-devired class.
            using (var sr = new StreamReader(this.fs, leaveOpen: true))
            {
                var token = this.cancelSrc.Token;
                var workspace = new List<ReadOnlyMemory<char>>(16);

                // v3 draft. Not quite alright in term of design but it's the best for now.

                // CanSeek indicate the FileStream can access .Length and .Position.
                // Which also means StreamReader.EndOfStream is usable in the case.
                // In the other words, if we are checking EOF, we actually will reduce CPU usage with the same polling rate, or we can go even faster polling rate (however, it should be around 15ms~100ms rate).
                var canCheckForEOF = this.fs.CanSeek;
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var bufferedData = bufferer.WrittenMemory;
                        var bufferedDataLen = bufferedData.Length;
                        if (bufferedDataLen != 0)
                        {
                            // Check whether our buffered characters still have some "lines of characters" in the buffer.
                            // And if there is/are, we will use them right away instead of attempt more IO calls.
                            var pos = 0;
                            var bufferedOutLen = bufferedData.Span.IndexOf('\n');
                            while (bufferedOutLen != -1)
                            {
                                var callbackMem = bufferedData.Slice(pos, bufferedOutLen);
                                var realLength = callbackMem.Span.TrimEnd('\r').Length;
                                if (callbackMem.Length != realLength)
                                {
                                    callbackMem = callbackMem.Slice(0, realLength);
                                }
                                this.OnDataReceived(callbackMem, workspace);
                                pos += (bufferedOutLen + 1);

                                // Don't clear per lines as we're doing unnecessary memory copying/moving.
                                // bufferer.Clear(0, bufferedOutLen + 1);

                                if (pos >= bufferedDataLen)
                                {
                                    break;
                                }
                                else
                                {
                                    bufferedOutLen = bufferedData.Slice(pos).Span.IndexOf('\n');
                                }
                            }

                            // Clear all the consumed buffered characters at once.
                            // One memory clear for all the lines we've found above.
                            // In case where there are no lines found above, no clearing happens.
                            if (pos != 0)
                            {
                                bufferer.Clear(0, pos);
                            }
                        }
                        if (!token.IsCancellationRequested)
                        {
                            // Prefer using EndOfStream because we can avoid an empty OverlappedIO call (async file IO operation) create unnecessary overhead.
                            if (canCheckForEOF)
                            {
                                // We will check end-of-file (EOF) again and again until the FileStream.Position no longer at the EOF.
                                while (!token.IsCancellationRequested && sr.EndOfStream)
                                {
                                    // Take a 50-milisecond delay per poll to save CPU cycles before attempt another EOF check.
                                    await Task.Delay(50, token).ConfigureAwait(false);
                                }

                                // Make async IO read call because we are sure there are more data to read and we will not get an empty data.
                                var mem = bufferer.GetMemory();
                                var readCount = await sr.ReadAsync(mem, token).ConfigureAwait(false);

                                // However, we will never know so we still need to double check for safety reason.
                                if (readCount != 0)
                                {
                                    bufferer.Advance(readCount);
                                }
                            }
                            else
                            {
                                // If the code run here instead, this means the file is either a network or a special case happened (which I don't even know how).
                                // But still do it anyway for compatibility, albeit not very optimal (very wasteful) for performance.
                                var mem = bufferer.GetMemory();
                                var readCount = await sr.ReadAsync(mem, token).ConfigureAwait(false);

                                // We will attempt to make async IO calls again and again until we actually get character data.
                                // This is very wasteful, however, in case the FileStream doesn't support Seeking, we have no other options.
                                while (!token.IsCancellationRequested && readCount == 0)
                                {
                                    // Take a 50-milisecond delay poll to save CPU cycles before attempt another read.
                                    await Task.Delay(50, token).ConfigureAwait(false);
                                    readCount = await sr.ReadAsync(mem, token).ConfigureAwait(false);
                                }
                                bufferer.Advance(readCount);
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (ObjectDisposedException) { }

                /*
                // Another draft (v2). But abandoned due to the odd design.
                try
                {
                    var func = new Action<ReadOnlyMemory<char>, List<ReadOnlyMemory<char>>>(this.OnDataReceived);
                    while (!token.IsCancellationRequested)
                    {
                        await sr.UseStrictLineReadAsync(func, workspace, token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) { }
                catch (ObjectDisposedException) { }
                */

                /*
                // Initial draft
                try
                {
                    var func = new Action<ReadOnlyMemory<char>, object?>(this.OnDataReceived);
                    while (!token.IsCancellationRequested)
                    {
                        var line = await sr.ReadLineAsync().ConfigureAwait(false);
                        if (!token.IsCancellationRequested)
                        {
                            if (string.IsNullOrEmpty(line))
                            {
                                await Task.Delay(30, token).ConfigureAwait(false);
                            }
                            else
                            {
                                var data = new PSO2LogData(line, this.workspace);
                                this.DataReceived?.Invoke(this, in data);
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (ObjectDisposedException) { }
                */
            }
        }

        private void OnDataReceived(ReadOnlyMemory<char> line, List<ReadOnlyMemory<char>> workspace)
        {
            var data = new PSO2LogData(line, workspace);
            this.DataReceived?.Invoke(this, in data);
        }

        /// <summary>Close the reader and clean up resources used by this instance.</summary>
        public void Dispose()
        {
            if (this._disposed) return;
            this._disposed = true;
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Default implementation for cleanup resources allocated by this instance.</summary>
        protected virtual void Dispose(bool disposing)
        {
            this.DataReceived = null;
            this.cancelSrc.Cancel();
            // this.delay.Dispose();
            // Trigger ObjectDisposedException on the Task.
            if (!this._leaveOpen)
            {
                this.fs.Dispose();
            }
        }

        /// <summary>Destructor for the class.</summary>
        ~PSO2LogAsyncListener()
        {
            this.Dispose(false);
        }

        /// <summary>Occurs when new log data is found and read.</summary>
        /// <remarks>
        /// <para>This event is invoked on a ThreadPool's thread. Therefore, you may need to dispatch calls to UI thread to avoid cross-thread error when accessing UI elements/controls.</para>
        /// <para>The data's lifetime will only remain within the event's invocation. To use the data after the invocation, allocation and copying the data to the allocated memory is the only way.</para>
        /// </remarks>
        public event DataReceivedEventHandler? DataReceived;
    }
}
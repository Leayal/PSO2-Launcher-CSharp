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
        private readonly List<ReadOnlyMemory<char>> workspace;
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
            this.workspace = new List<ReadOnlyMemory<char>>(16);
        }

        /// <summary>Begin the async operation.</summary>
        /// <remarks>When log data is available, the <seealso cref="DataReceived"/> event will be invoked with the given data.</remarks>
        public void StartReceiving()
        {
            if (Interlocked.CompareExchange(ref this._state, 1, 0) == 0)
            {
                Task.Factory.StartNew(this.ReadFileAsync, TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        private async Task ReadFileAsync()
        {
            using (var sr = new StrictNewLineStreamReader(this.fs, bufferSize: 512, leaveOpen: true))
            {
                var token = this.cancelSrc.Token;
                try
                {
                    var func = new Action<ReadOnlyMemory<char>, object?>(this.OnDataReceived);
                    while (!token.IsCancellationRequested)
                    {
                        await sr.UseStrictLineReadAsync(func, null, token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) { }
                catch (ObjectDisposedException) { }

                /*
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

        private void OnDataReceived(ReadOnlyMemory<char> line, object? obj)
        {
            var data = new PSO2LogData(line, this.workspace);
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
        public event DataReceivedEventHandler? DataReceived;
    }
}
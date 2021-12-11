using System.Threading;

namespace Leayal.PSO2Launcher.Toolbox
{
    /// <summary>Provides an implementation to read a text file in async manner.</summary>
    /// <remarks>This class is single-use. Once started, it can only be stopped by calling <seealso cref="DisposeAsync"/>. Likewise, once stopped, it can't be started again.</remarks>
    public class PSO2LogAsyncReader : IDisposable
    {
        private readonly CancellationTokenSource cancelSrc;
        private readonly FileStream fs;
        private Task? t_readFile;
        private readonly bool _leaveOpen;

        /// <summary>Gets the full path to the log file.</summary>
        public string Fullpath => this.fs.Name;

        /// <summary>Creates a new instance with the given file path.</summary>
        /// <param name="filepath">The path to the log file.</param>
        public PSO2LogAsyncReader(string filepath) : this(new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), false) { }

        /// <summary>Creates a new instance with the given <seealso cref="FileStream"/> that will close the <paramref name="fs"/> stream when the instance is disposed.</summary>
        /// <param name="fs">The file stream to read the log.</param>
        /// <remarks>This is equal to with <code>PSO2LogAsyncReader(FileStream, false)</code>.</remarks>
        public PSO2LogAsyncReader(FileStream fs) : this(fs, false) { }

        /// <summary>Creates a new instance with the given <seealso cref="FileStream"/></summary>
        /// <param name="fs">The file stream to read the log.</param>
        /// <param name="keepOpen">The boolean to determine whether the <paramref name="fs"/> stream should be closed when this instance is disposed.</param>
        public PSO2LogAsyncReader(FileStream fs, bool keepOpen)
        {
            this.cancelSrc = new CancellationTokenSource();
            this._leaveOpen = keepOpen;
            this.fs = fs;
        }

        /// <summary>Begin the async operation.</summary>
        /// <remarks>When log data is available, the <seealso cref="DataReceived"/> event will be invoked with the given data.</remarks>
        public void StartReceiving()
        {
            if (this.t_readFile is not null) return;
            this.t_readFile = Task.Factory.StartNew(this.ReadFileAsync, TaskCreationOptions.LongRunning).Unwrap();
        }

        private async Task ReadFileAsync()
        {
            using (var cancel = this.cancelSrc)
            using (var sr = new StreamReader(this.fs))
            {
                var token = this.cancelSrc.Token;
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var line = sr.ReadLine();
                        if (string.IsNullOrEmpty(line))
                        {
                            await Task.Delay(5, token);
                        }
                        else
                        {
                            this.DataReceived?.Invoke(this, new LogReaderDataReceivedEventArgs(line));
                        }
                    }
                }
                catch (ObjectDisposedException) { }
                catch (TaskCanceledException) { }
            }
        }

        /// <summary>Close the reader</summary>
        public void Dispose()
        {
            this.DataReceived = null;
            this.cancelSrc.Cancel();
            // Trigger ObjectDisposedException on the Task.
            if (!this._leaveOpen)
            {
                this.fs.Dispose();
            }
        }

        /// <summary>Occurs when new log data is read.</summary>
        public event Action<PSO2LogAsyncReader, LogReaderDataReceivedEventArgs>? DataReceived;
    }
}
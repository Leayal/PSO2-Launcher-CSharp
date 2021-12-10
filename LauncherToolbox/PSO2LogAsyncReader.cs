using System.Threading;

namespace Leayal.PSO2Launcher.Toolbox
{
    /// <summary>Provides an implementation to read a text file in async manner.</summary>
    /// <remarks>This class is single-use. Once started, it can only be stopped by calling <seealso cref="DisposeAsync"/>. Likewise, once stopped, it can't be started again.</remarks>
    public class PSO2LogAsyncReader : IAsyncDisposable
    {
        private readonly StreamReader sr;
        private Task? t_readFile;

        /// <summary>Creates a new instance with the given <seealso cref="FileStream"/> that will close the <paramref name="fs"/> stream when the instance is disposed.</summary>
        /// <param name="fs">The file stream to read the log.</param>
        /// <remarks>This is equal to with <code>PSO2LogAsyncReader(FileStream, false)</code>.</remarks>
        public PSO2LogAsyncReader(FileStream fs) : this(fs, false) { }

        /// <summary>Creates a new instance with the given <seealso cref="FileStream"/></summary>
        /// <param name="fs">The file stream to read the log.</param>
        /// <param name="keepOpen">The boolean to determine whether the <paramref name="fs"/> stream should be closed when this instance is disposed.</param>
        public PSO2LogAsyncReader(FileStream fs, bool keepOpen)
        {
            this.sr = new StreamReader(fs, leaveOpen: keepOpen);
        }

        /// <summary>Begin the async operation.</summary>
        /// <remarks>When log data is available, the <seealso cref="DataReceived"/> event will be invoked with the given data.</remarks>
        public void StartReceiving()
        {
            if (this.t_readFile is not null) return;
            this.t_readFile = Task.Run(this.ReadFileAsync);
        }

        private async Task ReadFileAsync()
        {
            while (true)
            {
                try
                {
                    var line = await this.sr.ReadLineAsync();
                    if (string.IsNullOrEmpty(line))
                    {
                        await Task.Delay(10);
                    }
                    else
                    {
                        this.DataReceived?.Invoke(this, new LogReaderDataReceivedEventArgs(line));
                    }
                }
                catch (ObjectDisposedException) // Silent the disposed exception as we will actually use it to "cancel" the async ReadLine above.
                {
                    // Exit the infinite loop
                    break;
                }
            }
        }

        /// <summary>Close the reader</summary>
        public async ValueTask DisposeAsync()
        {
            // Trigger ObjectDisposedException on the Task.
            this.sr.Dispose();

            if (this.t_readFile is not null)
            {
                // If the async operation has been started. Gracefully wait for it to be finished so that the GC can consider this whole thing is instance is no longer used.
                // This also provides some kind of safe code flow if there is a need of ensuring the FileHandle is closed before doing anything else.
                await this.t_readFile;
            }
        }

        /// <summary>Occurs when new log data is read.</summary>
        public event Action<PSO2LogAsyncReader, LogReaderDataReceivedEventArgs>? DataReceived;
    }
}
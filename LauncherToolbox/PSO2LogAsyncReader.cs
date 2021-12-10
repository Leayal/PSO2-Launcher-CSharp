using System.Threading;

namespace Leayal.PSO2Launcher.Toolbox
{
    public class PSO2LogAsyncReader : IAsyncDisposable
    {
        private readonly CancellationTokenSource rootCancelSrc;
        private readonly StreamReader sr;
        private Task t_readFile;

        public PSO2LogAsyncReader(FileStream fs) : this(fs, false) { }

        public PSO2LogAsyncReader(FileStream fs, bool keepOpen)
        {
            this.sr = new StreamReader(fs, leaveOpen: keepOpen);
            this.rootCancelSrc = new CancellationTokenSource();
        }

        public void StartReceiving()
        {
            if (this.t_readFile is not null) return;
            this.t_readFile = Task.Run(this.ReadFileAsync);
        }

        private async Task ReadFileAsync()
        {

            string? line;
            var token = this.rootCancelSrc.Token;
            var ev = this.DataReceived;
            while (!token.IsCancellationRequested)
            {
                line = await this.sr.ReadLineAsync();
                if (string.IsNullOrEmpty(line))
                {
                    await Task.Delay(10);
                }
                else
                {
                    ev?.Invoke(this, new LogReaderDataReceivedEventArgs(line));
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            this.rootCancelSrc.Cancel();
            if (this.t_readFile is not null)
            {
                await this.t_readFile;
            }
            this.rootCancelSrc.Dispose();
        }

        /// <summary>Occurs when new line is found.</summary>
        public event Action<PSO2LogAsyncReader, LogReaderDataReceivedEventArgs> DataReceived;
    }
}
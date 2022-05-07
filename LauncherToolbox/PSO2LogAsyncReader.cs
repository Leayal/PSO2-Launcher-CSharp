using System.Threading;

namespace Leayal.PSO2Launcher.Toolbox
{
    sealed class PSO2LogAsyncReader : IAsyncDisposable
    {
        private readonly bool _leaveopen;
        private readonly StreamReader sr;
        private readonly DataReceivedCallback callback;
        private readonly CancellationToken cancelToken;
        private Task? t;

        public PSO2LogAsyncReader(FileStream fs, bool keepOpen, DataReceivedCallback callback, CancellationToken token)
        {
            this.t = null;
            this.callback = callback;
            this.cancelToken = token;
            this._leaveopen = keepOpen;
            this.sr = new StreamReader(fs, encoding: null, detectEncodingFromByteOrderMarks: true, bufferSize: -1, leaveOpen: true);
        }

        public Task StartOperation()
        {
            if (this.t == null)
            {
                this.t = Task.Factory.StartNew(this.InternalStartOperation, TaskCreationOptions.LongRunning);
            }
            return this.t;
        }

        private void InternalStartOperation()
        {
            while (!this.cancelToken.IsCancellationRequested)
            {
                var line = this.sr.ReadLine();
                var workspace = new List<ReadOnlyMemory<char>>(16);
                if (line == null)
                {
                    break;
                }
                else
                {
                    var data = new PSO2LogData(line, workspace);
                    this.callback.Invoke(in data);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            this.sr.Dispose();
            if (!this._leaveopen)
            {
                var fs = (FileStream)this.sr.BaseStream;
                if (fs.IsAsync)
                {
                    await fs.DisposeAsync();
                }
                else
                {
                    fs.Dispose();
                }
            }
        }
    }
}
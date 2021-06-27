using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes
{
    public abstract class AsyncDisposeObject : IAsyncDisposable
    {
        private int flag_disposed;
        private Task t_dispose;

        protected AsyncDisposeObject()
        {
            this.flag_disposed = 0;
        }

        public event Action<AsyncDisposeObject> Disposed;

        protected abstract Task OnDisposeAsync();

        public ValueTask DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref this.flag_disposed, 1, 0) == 0)
            {
                this.t_dispose = this.OnDisposeAsync();
                this.t_dispose.ContinueWith(t => this.Disposed?.Invoke(this));
            }

            return new ValueTask(this.t_dispose);
        }
    }
}

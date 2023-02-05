using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Threading;
using System.Collections.Generic;

namespace Leayal.PSO2Launcher.Core.Classes
{
    abstract class SimpleDispatcherQueue : IDisposable
    {
        public static SimpleDispatcherQueue CreateDefault(in TimeSpan tickInterval)
            => CreateDefault(in tickInterval, DispatcherPriority.Normal, Dispatcher.CurrentDispatcher, true);

        public static SimpleDispatcherQueue CreateDefault(in TimeSpan tickInterval, bool isThreadSafe)
            => CreateDefault(in tickInterval, DispatcherPriority.Normal, Dispatcher.CurrentDispatcher, isThreadSafe);

        public static SimpleDispatcherQueue CreateDefault(in TimeSpan tickInterval, Dispatcher dispatcher)
            => CreateDefault(in tickInterval, DispatcherPriority.Normal, dispatcher, true);

        public static SimpleDispatcherQueue CreateDefault(in TimeSpan tickInterval, Dispatcher dispatcher, bool isThreadSafe)
            => CreateDefault(in tickInterval, DispatcherPriority.Normal, dispatcher, isThreadSafe);

        public static SimpleDispatcherQueue CreateDefault(in TimeSpan tickInterval, DispatcherPriority dispatcherPriority)
            => CreateDefault(in tickInterval, dispatcherPriority, Dispatcher.CurrentDispatcher, true);

        public static SimpleDispatcherQueue CreateDefault(in TimeSpan tickInterval, DispatcherPriority dispatcherPriority, Dispatcher dispatcher)
            => CreateDefault(in tickInterval, dispatcherPriority, dispatcher, true);

        public static SimpleDispatcherQueue CreateDefault(in TimeSpan tickInterval, DispatcherPriority dispatcherPriority, Dispatcher dispatcher, bool isThreadSafe)
            => (isThreadSafe ? new SimpleThreadSafeDispatcherQueue(in tickInterval, dispatcherPriority, dispatcher) : new SimpleSyncDispatcherQueue(in tickInterval, dispatcherPriority, dispatcher));

        private readonly DispatcherTimer timer;

        protected SimpleDispatcherQueue(in TimeSpan tickInterval, DispatcherPriority dispatcherPriority, Dispatcher dispatcher)
        {
            this.timer = new DispatcherTimer(tickInterval, dispatcherPriority, this.Timer_Tick, dispatcher)
            {
                IsEnabled = false
            };
        }

        public void Start() => this.timer.Start();
        public void Stop() => this.timer.Stop();

        private void Timer_Tick(object? sender, EventArgs e)
            => this.DispatcherTick(this.timer.Dispatcher);

        protected abstract void DispatcherTick(Dispatcher dispatcher);

        public virtual DispatcherQueueItem RegisterToTick(Action action)
            => this.RegisterToTick(new DispatcherQueueItem(action));

        public virtual DispatcherQueueItem RegisterToTick(Delegate @delegate, params object[] @params)
            => this.RegisterToTick(new DispatcherQueueItem(@delegate, @params));

        public virtual DispatcherQueueItem RegisterToTick(DispatcherQueueItem item)
        {
            this.AddItemToQueue(item);
            return item;
        }

        protected abstract void AddItemToQueue(DispatcherQueueItem item);

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) 
        {
            if (disposing)
            {
                this.timer.Tick -= this.Timer_Tick;
            }
            if (this.timer.IsEnabled)
            {
                this.timer.Stop();
            }
        }

        ~SimpleDispatcherQueue()
        {
            this.Dispose(false);
        }


        #region "| Default Implementation |
        class SimpleSyncDispatcherQueue : SimpleDispatcherQueue
        {
            private readonly List<DispatcherQueueItem> queue;
            private readonly Action<DispatcherQueueItem> _unregistered;

            public SimpleSyncDispatcherQueue(in TimeSpan tickInterval, DispatcherPriority dispatcherPriority, Dispatcher dispatcher)
                : base(in tickInterval, dispatcherPriority, dispatcher)
            {
                this._unregistered = new Action<DispatcherQueueItem>(this.OnItemUnregistered);
                this.queue = new List<DispatcherQueueItem>();
            }

            protected override void DispatcherTick(Dispatcher dispatcher)
            {
                DispatcherQueueItem[] fetched;
                lock (this.queue)
                {
                    fetched = this.queue.ToArray();
                    this.queue.Clear();
                }
                for (int i = 0; i < fetched.Length; i++)
                {
                    fetched[i].Invoke(dispatcher);
                }
            }

            protected override void AddItemToQueue(DispatcherQueueItem item)
            {
                lock (this.queue)
                {
                    this.queue.Add(item);
                }
                item.Unregistered += this.OnItemUnregistered;
            }

            private void OnItemUnregistered(DispatcherQueueItem item)
            {
                item.Unregistered -= this.OnItemUnregistered;
                lock (this.queue)
                {
                    this.queue.Remove(item);
                }
            }
        }

        class SimpleThreadSafeDispatcherQueue : SimpleDispatcherQueue
        {
            private readonly ConcurrentBag<DispatcherQueueItem> queue;

            public SimpleThreadSafeDispatcherQueue(in TimeSpan tickInterval, DispatcherPriority dispatcherPriority, Dispatcher dispatcher)
                : base(in tickInterval, dispatcherPriority, dispatcher)
            {
                this.queue = new ConcurrentBag<DispatcherQueueItem>();
            }

            protected override void DispatcherTick(Dispatcher dispatcher)
            {
                while (this.queue.TryTake(out var item))
                {
                    item.Invoke(dispatcher);
                }
            }

            protected override void AddItemToQueue(DispatcherQueueItem item)
            {
                this.queue.Add(item);
            }
        }
        #endregion
    }

    class DispatcherQueueItem
    {
        private readonly Delegate _action;
        private readonly object[] _params;
        private int state;
        public event Action<DispatcherQueueItem>? Unregistered;

        public DispatcherQueueItem(Action action) : this(action, Array.Empty<object>()) { }

        public DispatcherQueueItem(Delegate action, object[] _params)
        {
            if (_params == null)
            {
                throw new ArgumentNullException(nameof(_params));
            }
            this.state = 0;
            this._action = action;
            this._params = _params;
        }

        public bool Unregister()
        {
            if (Interlocked.CompareExchange(ref this.state, -1, 0) == 0)
            {
                if (this._params.Length != 0)
                {
                    Array.Clear(this._params, 0, this._params.Length);
                }

                this.Unregistered?.Invoke(this);

                return true;
            }
            else
            {
                return false;
            }
        }

        public void Invoke(Dispatcher dispatcher)
        {
            if (Interlocked.CompareExchange(ref this.state, 1, 0) == 0)
            {
                if (this._action is Action act)
                {
                    dispatcher.InvokeAsync(act);
                }
                else if (this._params.Length != 0)
                {
                    dispatcher.BeginInvoke(this._action, this._params);
                    // Array.Clear(this._params, 0, this._params.Length);
                }
                else
                {
                    dispatcher.BeginInvoke(this._action);
                }
            }
        }
    }
}

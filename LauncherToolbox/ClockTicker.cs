using System;
using Leayal.Shared;

namespace Leayal.PSO2Launcher.Toolbox
{
    /// <summary>ClockTicker's callback which will be invoked once per second.</summary>
    /// <param name="oldTime">The timer of the previous clock's tick.</param>
    /// <param name="newTime">The timer of the current clock's tick</param>
    public delegate void ClockTickerCallback(in DateTime oldTime, in DateTime newTime);

    /// <summary>A timer which will tick once per second. Heart and soul of a time clock.</summary>
    /// <remarks>The time is fetched from operating system's clock. This also means the time and time zone is local.</remarks>
    public class ClockTicker : ActivationBasedObject, IDisposable
    {
        private DateTime lastTick;
        private readonly Timer timer;
        private bool _disposed;
        private ClockTickerCallback? callbacks;

        /// <summary>Creates a new instance of this class.</summary>
        public ClockTicker() : base()
        {
            this.callbacks = null;
            this._disposed = false;
            this.lastTick = DateTime.Now;
            this.timer = new Timer(Timer_Tick, this, Timeout.Infinite, Timeout.Infinite);
        }

        private static void Timer_Tick(object? obj)
        {
            if (obj is ClockTicker ticker)
            {
                var theNow = DateTime.Now;
                var theOld = ticker.lastTick;
                ticker.lastTick = theNow;
                if (ticker.IsCurrentlyActive)
                {
                    if ((theNow - theOld).TotalMilliseconds > 1000)
                    {
                        ticker.timer.Change(0, Timeout.Infinite);
                    }
                    else
                    {
                        ticker.TryToFixTimerRes(in theNow);
                    }
                    ticker.OnClockTick(in theOld, in theNow);
                }
            }
        }

        /// <summary>Clock's tick. This method will be invoked once per second. Or immediately if the previous execution's time is longer than 1 second.</summary>
        /// <param name="oldTime">The time of the previous tick.</param>
        /// <param name="newTime">The time of the current tick.</param>
        protected virtual void OnClockTick(in DateTime oldTime, in DateTime newTime)
        {
            this.InvokeCallbacks(in oldTime, in newTime);
        }

        /// <summary>Invokes all registered callbacks.</summary>
        /// <remarks>
        /// <para>This may incurs a lot of overhead due to locking operation on the registered callbacks.</para>
        /// <para><seealso cref="Register(ClockTickerCallback)"/> and <seealso cref="Unregister(ClockTickerCallback)"/> also use lock.</para>
        /// </remarks>
        /// <param name="oldTime">The time of the previous tick.</param>
        /// <param name="newTime">The time of the current tick.</param>
        protected virtual void InvokeCallbacks(in DateTime oldTime, in DateTime newTime)
        {
            this.callbacks?.Invoke(in oldTime, in newTime);
        }

        /// <summary>Register a clock's ticking callback.</summary>
        /// <param name="callback">The tick callback to register.</param>
        /// <remarks>This will also start the clock if the clock is currently inactive.</remarks>
        public void Register(ClockTickerCallback callback)
        {
            this.callbacks += callback;
            this.RequestActive();
        }

        /// <summary>Unregister a clock's ticking callback.</summary>
        /// <param name="callback">The tick callback to unregister.</param>
        /// <remarks>This will also stop the clock if there is no callback for the clock to invoke.</remarks>
        public void Unregister(ClockTickerCallback callback)
        {
            this.callbacks -= callback;
            this.RequestDeactive();
        }

        /// <summary>Starts the clock's ticking.</summary>
        protected override void OnActivation()
        {
            var now = DateTime.Now;
            this.InvokeCallbacks(in this.lastTick, in now);
            this.TryToFixTimerRes(in now);
        }

        /// <summary>Stops the clock's ticking.</summary>
        protected override void OnDeactivation()
        {
            this.timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        // private void TryToFixTimerRes() => this.TryToFixTimerRes(DateTime.Now);

        private void TryToFixTimerRes(in DateTime currentlocaltime)
        {
            this.timer.Change(1000 - currentlocaltime.Millisecond, Timeout.Infinite);
        }

        /// <summary>Stops the clock and dispose all the timers.</summary>
        public void Dispose()
        {
            if (this._disposed) return;
            this._disposed = true;
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Disposing the timer associated with this instance.</summary>
        /// <param name="disposing">Dunno.</param>
        protected virtual void Dispose(bool disposing)
        {
            this.timer.Change(Timeout.Infinite, Timeout.Infinite);
            this.timer.Dispose();
        }

        /// <summary>Destructor</summary>
        ~ClockTicker()
        {
            this.Dispose(false);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Leayal.PSO2Launcher.Core.Classes
{
    /// <summary>
    /// Provides Debounce() and Throttle() methods.
    /// Use these methods to ensure that events aren't handled too frequently.
    /// 
    /// Throttle() ensures that events are throttled by the interval specified.
    /// Only the last event in the interval sequence of events fires.
    /// 
    /// Debounce() fires an event only after the specified interval has passed
    /// in which no other pending event has fired. Only the last event in the
    /// sequence is fired.
    /// </summary>
    public class DebounceDispatcher : IDisposable
    {
        private readonly Dispatcher _dispatcher;
        private DispatcherTimer timer;

        public DebounceDispatcher(Dispatcher dispatcher)
        {
            this._dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
        }

        public void Dispose() => this.Stop();

        public void Stop()
        {
            var timer = this.timer;
            this.timer = null;
            timer?.Stop();
        }

        /// <summary>
        /// Debounce an event by resetting the event timeout every time the event is 
        /// fired. The behavior is that the Action passed is fired only after events
        /// stop firing for the given timeout period.
        /// 
        /// Use Debounce when you want events to fire only after events stop firing
        /// after the given interval timeout period.
        /// 
        /// Wrap the logic you would normally use in your event code into
        /// the  Action you pass to this method to debounce the event.
        /// Example: https://gist.github.com/RickStrahl/0519b678f3294e27891f4d4f0608519a
        /// </summary>
        /// <param name="interval">Timeout in Milliseconds</param>
        /// <param name="action">Action<object> to fire when debounced event fires</object></param>
        /// <param name="param">optional parameter</param>
        /// <param name="priority">optional priorty for the dispatcher</param>
        /// <param name="disp">optional dispatcher. If not passed or null CurrentDispatcher is used.</param>        
        public void Debounce(int interval, Action action, DispatcherPriority priority = DispatcherPriority.ApplicationIdle, Dispatcher disp = null)
        {
            // kill pending timer and pending ticks
            timer?.Stop();

            // timer is recreated for each event and effectively
            // resets the timeout. Action only fires after timeout has fully
            // elapsed without other events firing in between

            Interlocked.Exchange(ref this.lastKnownAction, action);
            timer = new DispatcherTimer(TimeSpan.FromMilliseconds(interval), priority, DebounceInvocation, disp ?? this._dispatcher);

            timer.Start();
        }

        private static void DebounceInvocation(object sender, EventArgs e)
        {
            if (sender is DispatcherTimer timer && timer.Tag is DebounceDispatcher debouncer)
            {
                timer?.Stop();
                var act = Interlocked.Exchange(ref debouncer.lastKnownAction, null);
                if (act != null)
                {
                    timer.Dispatcher.InvokeAsync(act);
                }
            }
        }

        /*
        /// <summary>
        /// This method throttles events by allowing only 1 event to fire for the given
        /// timeout period. Only the last event fired is handled - all others are ignored.
        /// Throttle will fire events every timeout ms even if additional events are pending.
        /// 
        /// Use Throttle where you need to ensure that events fire at given intervals.
        /// </summary>
        /// <param name="interval">Timeout in Milliseconds</param>
        /// <param name="action">Action<object> to fire when debounced event fires</object></param>
        /// <param name="param">optional parameter</param>
        /// <param name="priority">optional priorty for the dispatcher</param>
        /// <param name="disp">optional dispatcher. If not passed or null CurrentDispatcher is used.</param>
        public void Throttle(int interval, Action<object> action,
            object param = null,
            DispatcherPriority priority = DispatcherPriority.ApplicationIdle,
            Dispatcher disp = null)
        {
            // kill pending timer and pending ticks
            timer?.Stop();
            timer = null;

            if (disp == null)
                disp = this._dispatcher;

            var curTime = DateTime.UtcNow;

            // if timeout is not up yet - adjust timeout to fire 
            // with potentially new Action parameters           
            if (curTime.Subtract(timerStarted).TotalMilliseconds < interval)
                interval -= (int)curTime.Subtract(timerStarted).TotalMilliseconds;

            timer = new DispatcherTimer(TimeSpan.FromMilliseconds(interval), priority, (s, e) =>
            {
                if (timer == null)
                    return;

                timer?.Stop();
                timer = null;
                action.Invoke(param);
            }, disp);

            timer.Start();
            timerStarted = curTime;
        }
        */

        private Action lastKnownAction;

        public void ThrottleEx(int interval, Action action, DispatcherPriority priority = DispatcherPriority.Normal, Dispatcher disp = null)
        {
           // if timeout is not up yet - adjust timeout to fire 
            // with potentially new Action parameters

            Interlocked.Exchange<Action>(ref this.lastKnownAction, action);
            if (timer == null)
            {
                timer = new DispatcherTimer(TimeSpan.FromMilliseconds(interval), priority, ThrottleInvocation, disp ?? this._dispatcher) { Tag = this };
            }
            if (!timer.IsEnabled)
            {
                timer.Start();
            }
        }

        public void Throttle(int interval, Action action, DispatcherPriority priority = DispatcherPriority.Normal, Dispatcher disp = null)
        {
            // if timeout is not up yet - adjust timeout to fire 
            // with potentially new Action parameters

            Interlocked.CompareExchange<Action>(ref this.lastKnownAction, action, null);
            if (timer == null)
            {
                timer = new DispatcherTimer(TimeSpan.FromMilliseconds(interval), priority, ThrottleInvocation, disp ?? this._dispatcher) { Tag = this };
            }
            if (!timer.IsEnabled)
            {
                timer.Start();
            }
        }

        private static void ThrottleInvocation(object sender, EventArgs e)
        {
            if (sender is DispatcherTimer timer && timer.Tag is DebounceDispatcher debouncer)
            {
                var okayToGo = Interlocked.Exchange<Action>(ref debouncer.lastKnownAction, null);
                if (okayToGo != null && timer.Dispatcher != null)
                {
                    timer.Dispatcher.InvokeAsync(okayToGo);
                }
                else
                {
                    okayToGo?.Invoke();
                }
            }
        }
    }
}

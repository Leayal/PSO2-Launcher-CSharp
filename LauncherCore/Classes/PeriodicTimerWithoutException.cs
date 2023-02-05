using System;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes
{
    /// <summary>Provides a periodic timer that enables waiting asynchronously for timer ticks.</summary>
    class PeriodicTimerWithoutException : IDisposable
    {
        private readonly PeriodicTimer timer;

        /// <summary>Initializes the timer.</summary>
        /// <param name="period">The time interval in milliseconds between invocations of the callback.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="period"/> is less than or equal to 0, or greater than <seealso cref="System.UInt32.MaxValue"/></exception>
        public PeriodicTimerWithoutException(TimeSpan period)
        {
            this.timer = new PeriodicTimer(period);
        }

        public override int GetHashCode()
        {
            return this.timer.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj is PeriodicTimer timer)
            {
                return object.ReferenceEquals(timer, this.timer);
            }
            else if (obj is PeriodicTimerWithoutException noEx)
            {
                return object.ReferenceEquals(this, noEx);
            }
            return false;
        }

        /// <summary>Waits for the next tick of the timer, or for the timer to be stopped.</summary>
        /// <param name="cancellationToken">A System.Threading.CancellationToken for cancelling the asynchronous wait. If cancellation is requested, it affects only the single wait operation; the underlying timer continues firing.</param>
        /// <returns>
        /// <para>A task that will be completed due to the timer firing, <seealso cref="Dispose"/> being called to stop the timer, or cancellation being requested.</para>
        /// <para>Task value will be true if the timer is firing.</para>
        /// <para>Task value will be false if the timer is stopping.</para>
        /// </returns>
        public async ValueTask<bool> WaitForNextTickAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await this.timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        /// <summary>Stops the timer and releases the associated managed resources.</summary>
        public void Dispose() => this.timer.Dispose();
    }
}

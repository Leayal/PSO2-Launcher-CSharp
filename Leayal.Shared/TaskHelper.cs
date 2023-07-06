using System;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.Shared
{
    public static class TaskHelper
    {
        public static Task<bool> WaitAsync(this WaitHandle handle) => WaitAsync(handle, Timeout.Infinite);

        public static Task<bool> WaitAsync(this WaitHandle handle, TimeSpan timeout) => WaitAsync(handle, Convert.ToInt32(timeout.TotalMilliseconds));

        public static Task<bool> WaitAsync(this WaitHandle handle, int timeoutInMiliseconds)
        {
            // Handle synchronous cases.
            var alreadySignalled = handle.WaitOne(0);
            if (alreadySignalled)
                return Task.FromResult(true);
            if (timeoutInMiliseconds == 0)
                return Task.FromResult(false);

            // Register all asynchronous cases.
            var tcs = new TaskCompletionSource<bool>();
            var threadPoolRegistration = ThreadPool.RegisterWaitForSingleObject(handle,
                (state, timedOut) => ((TaskCompletionSource<bool>?)state)?.TrySetResult(!timedOut),
                tcs, timeoutInMiliseconds, true);
            tcs.Task.ContinueWith((_, regObj) =>
            {
                ((RegisteredWaitHandle?)regObj)?.Unregister(null);
            }, threadPoolRegistration, TaskScheduler.Default);
            return tcs.Task;
        }
    }
}

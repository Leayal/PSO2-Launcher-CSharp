using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Leayal.PSO2Launcher.Core.Classes
{
    static class DispatcherHelper
    {
        public static void TryInvoke(this Dispatcher dispatcher, Action action)
        {
            if (dispatcher.CheckAccess())
            {
                action.Invoke();
            }
            else
            {
                dispatcher.InvokeAsync(action);
            }
        }

        public static void TryInvokeSync(this Dispatcher dispatcher, Action action)
        {
            if (dispatcher.CheckAccess())
            {
                action.Invoke();
            }
            else
            {
                dispatcher.Invoke(action);
            }
        }

        public static async Task<TResult> TryInvokeAsync<TResult>(this Dispatcher dispatcher, Func<TResult> action)
        {
            if (dispatcher.CheckAccess())
            {
                return action.Invoke();
            }
            else
            {
                return await dispatcher.InvokeAsync(action);
            }
        }

        public static async Task<TResult> TryInvokeAsync<TResult>(this Dispatcher dispatcher, Func<Task<TResult>> action)
        {
            if (dispatcher.CheckAccess())
            {
                return await action.Invoke();
            }
            else
            {
                // Evil?
                return await await dispatcher.InvokeAsync(action);
            }
        }
    }
}

using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Helper
{
    public static class PipesHelper
    {
        public static async Task WaitForConnectionAsyncThatActuallyReturnWhenCancelled(this NamedPipeServerStream pipe, CancellationToken token)
        {
            var tTermination = new TaskCompletionSource();
            token.Register(tTermination.SetResult);
            using (var tCancel = tTermination.Task)
            {
                var t_pipe = pipe.WaitForConnectionAsync();
                var t = await Task.WhenAny(t_pipe, tCancel);
                if (tCancel == t)
                {
                    if (t_pipe.IsCompleted)
                    {
                        t_pipe.Dispose();
                    }
                }
            }
        }
    }
}

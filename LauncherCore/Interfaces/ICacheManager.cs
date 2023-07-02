using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Interfaces
{
    interface ICacheManager<T> : IAsyncDisposable
    {
        ValueTask Load();

        ValueTask<T?> TryGet(string name);

        ValueTask<T> GetOrAdd(string name, Func<Task<T>> factoryIfNotFoundOrInvalidCache);
    }
}

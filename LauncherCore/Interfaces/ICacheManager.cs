using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Interfaces
{
    interface ICacheManager<T> : IAsyncDisposable
    {
        Task Load();

        Task<T> TryGet(string name);

        Task<T> GetOrAdd(string name, Func<Task<T>> factoryIfNotFoundOrInvalidCache);
    }
}

using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Leayal.PSO2Launcher.Core.Interfaces;

namespace Leayal.PSO2Launcher.Core.Classes
{
    sealed class ObjectShortCacheManager<T> : ICacheManager<T>
    {
        private readonly ConcurrentDictionary<string, Lazy<ObjectShortCacheAsyncItem>> innerCache;

        public ObjectShortCacheManager() : this(StringComparer.Ordinal) { }

        public ObjectShortCacheManager(StringComparer nameComparer)
        {
            this.innerCache = new ConcurrentDictionary<string, Lazy<ObjectShortCacheAsyncItem>>(nameComparer);
        }

        public ValueTask Load() => ValueTask.CompletedTask;

        public async ValueTask<T?> TryGet(string name)
        {
            if (this.innerCache.TryGetValue(name, out var cached))
            {
                var val = cached.Value;
                if (DateTime.UtcNow <= val.ttl)
                {
                    return await val.ObjectData;
                }
            }

            return default(T);
        }

        public async ValueTask<T> GetOrAdd(string name, Func<Task<T>> factory)
            => await this.GetOrAdd(name, factory, TimeSpan.FromSeconds(30));

        public async ValueTask<T> GetOrAdd(string name, Func<Task<T>> factory, TimeSpan howLongWillILive)
        {
            return await this.innerCache.AddOrUpdate(name, (cachedName) => new Lazy<ObjectShortCacheAsyncItem>(() => new ObjectShortCacheAsyncItem(factory.Invoke(), howLongWillILive)),
                (cachedName, cachedValue) =>
                {
                    if (cachedValue.IsValueCreated)
                    {
                        var cached = cachedValue.Value;
                        if (DateTime.UtcNow > cached.ttl)
                        {
                            return new Lazy<ObjectShortCacheAsyncItem>(() => new ObjectShortCacheAsyncItem(factory.Invoke(), howLongWillILive));
                        }
                        else
                        {
                            return cachedValue;
                        }
                    }
                    else
                    {
                        return cachedValue;
                    }
                    
                }).Value.ObjectData;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        class ObjectShortCacheAsyncItem
        {
            public readonly Task<T> ObjectData;
            public readonly DateTime ttl;

            public ObjectShortCacheAsyncItem(Task<T> obj) : this(obj, TimeSpan.FromSeconds(30)) { }

            public ObjectShortCacheAsyncItem(Task<T> obj, TimeSpan timeToLive)
            {
                this.ObjectData = obj;
                this.ttl = DateTime.UtcNow.Add(timeToLive);
            }
        }
    }
}

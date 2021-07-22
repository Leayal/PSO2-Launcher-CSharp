using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Leayal.PSO2Launcher.Core.Interfaces;

namespace Leayal.PSO2Launcher.Core.Classes
{
    class ObjectShortCacheManager<T> : ICacheManager<T>
    {
        private readonly ConcurrentDictionary<string, ObjectShortCacheAsyncItem> innerCache;

        public ObjectShortCacheManager() : this(StringComparer.Ordinal) { }

        public ObjectShortCacheManager(StringComparer nameComparer)
        {
            this.innerCache = new ConcurrentDictionary<string, ObjectShortCacheAsyncItem>(nameComparer);
        }

        public Task Load() => Task.CompletedTask;

        public Task<T> TryGet(string name)
        {
            if (this.innerCache.TryGetValue(name, out var cached))
            {
                if (DateTime.UtcNow <= cached.ttl)
                {
                    return cached.ObjectData;
                }
            }

            return Task.FromResult(default(T));
        }

        public Task<T> GetOrAdd(string name, Func<Task<T>> factory)
            => this.GetOrAdd(name, factory, TimeSpan.FromSeconds(30));

        public Task<T> GetOrAdd(string name, Func<Task<T>> factory, TimeSpan howLongWillILive)
        {
            return this.innerCache.AddOrUpdate(name, (cachedName) =>
            {
                return new ObjectShortCacheAsyncItem(factory.Invoke(), howLongWillILive);
            }, (cachedName, cachedValue) =>
            {
                if (DateTime.UtcNow > cachedValue.ttl)
                {
                    return new ObjectShortCacheAsyncItem(factory.Invoke(), howLongWillILive);
                }
                else
                {
                    return cachedValue;
                }
            }).ObjectData;
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

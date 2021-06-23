using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Leayal.PSO2Launcher.Core.Classes
{
    class ObjectShortCacheManager<T>
    {
        private readonly ConcurrentDictionary<string, ObjectShortCacheAsyncItem<T>> innerCache;

        public ObjectShortCacheManager() : this(StringComparer.Ordinal) { }

        public ObjectShortCacheManager(StringComparer nameComparer)
        {
            this.innerCache = new ConcurrentDictionary<string, ObjectShortCacheAsyncItem<T>>(nameComparer);
        }

        public bool TryGet(string name, out Task<T> value)
        {
            if (this.innerCache.TryGetValue(name, out var cached))
            {
                if (DateTime.UtcNow <= cached.ttl)
                {
                    value = cached.ObjectData;
                    return false;
                }
            }

            value = default;
            return false;
        }

        public Task<T> GetOrAdd(string name, Func<Task<T>> factory)
            => this.GetOrAdd(name, factory, TimeSpan.FromSeconds(30));

        public Task<T> GetOrAdd(string name, Func<Task<T>> factory, TimeSpan howLongWillILive)
        {
            return this.innerCache.AddOrUpdate(name, (cachedName) =>
            {
                return new ObjectShortCacheAsyncItem<T>(factory.Invoke(), howLongWillILive);
            }, (cachedName, cachedValue) =>
            {
                if (DateTime.UtcNow > cachedValue.ttl)
                {
                    return new ObjectShortCacheAsyncItem<T>(factory.Invoke(), howLongWillILive);
                }
                else
                {
                    return cachedValue;
                }
            }).ObjectData;
        }

        class ObjectShortCacheAsyncItem<T>
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

using System;

namespace Leayal.PSO2Launcher.Core.Classes
{
    sealed class ShortCacheObject<T>
    {
        public readonly T cacheObj;
        private readonly DateTime ttl;

        public ShortCacheObject(T obj) : this(obj, TimeSpan.FromSeconds(30)) { }

        public ShortCacheObject(T obj, TimeSpan timeToLive)
        {
            this.cacheObj = obj;
            this.ttl = DateTime.UtcNow.Add(timeToLive);
        }

        public bool IsOutdated => (DateTime.UtcNow > this.ttl);
    }
}

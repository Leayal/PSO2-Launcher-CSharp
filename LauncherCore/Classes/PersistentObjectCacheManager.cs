using Leayal.PSO2Launcher.Core.Interfaces;
using SQLite;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes
{
    class PersistentObjectCacheManager<T> : ICacheManager<T>
    {
        private const int LatestVersion = 1;

        private readonly string _directory;
        private SQLiteAsyncConnection connection;
        private int state;
        private Task t_load;
        private readonly ConcurrentDictionary<string, Task<T>> memCache;

        public PersistentObjectCacheManager(string cacheDirectory)
        {
            this._directory = cacheDirectory;
            this.state = 0;
            this.memCache = new ConcurrentDictionary<string, Task<T>>();
        }

        public async Task Load()
        {
            if (Interlocked.CompareExchange(ref this.state, 1, 0) == 0)
            {
                this.t_load = this.Init();
            }
            await this.t_load;
        }

        private async Task Init()
        {
            var connectString = new SQLiteConnectionString(this._directory, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.Create | SQLiteOpenFlags.PrivateCache, true, "lea-persistcache");
            SQLiteAsyncConnection conneck = null;
            try
            {
                conneck = new SQLiteAsyncConnection(connectString);
                var version = await GetVersion(conneck);
                if (version != LatestVersion)
                {
                    await this.Upgrade(version);
                }
            }
            catch
            {
                if (conneck != null)
                {
                    await conneck.CloseAsync();
                }
                File.Delete(this._directory);
                conneck = new SQLiteAsyncConnection(connectString);
                var version = await GetVersion(conneck);
                if (version != LatestVersion)
                {
                    await this.Upgrade(version);
                }
            }
            this.connection = conneck;
        }

        private static async Task<int> GetVersion(SQLiteAsyncConnection connect)
        {
            var vv = new Version() { Id = 1, Number = LatestVersion };
            var v = await connect.FindAsync<Version>(vv);
            if (v == null)
            {
                await connect.InsertAsync(vv);
                return vv.Number;
            }
            else
            {
                return v.Number;
            }
        }

        private async Task Upgrade(int version)
        {
            if (version == LatestVersion)
            {
                return;
            }
        }

        public async Task<T> GetOrAdd(string name, Func<Task<T>> factoryIfNotFoundOrInvalidCache)
        {
            await this.Load();

            return await this.memCache.AddOrUpdate(name, async (key) =>
            {
                return await factoryIfNotFoundOrInvalidCache.Invoke();
            }, async (key, cached) =>
            {
                return await cached;
            });
        }

        public async Task<T> TryGet(string name)
        {
            return default;
        }

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }

        class Version
        {
            [PrimaryKey]
            public int Id { get; set; }
            public int Number { get; set; }
        }
    }
}

using Leayal.PSO2.Modding.Cache;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.PSO2.Modding
{
    /// <summary>Dealing with file management.</summary>
    public sealed class ModEngine : IAsyncDisposable
    {
        private readonly List<ModDefinition> _mods;

        private readonly ConcurrentDictionary<ModDefinition, LazyModCacheFile> _modCache;

        public ModEngine(string workingdirectory)
        {

        }

        public async Task<ModCacheFile> BuildCache(ModDefinition mod, CancellationToken cancellationToken = default)
        {
            var modcache = await this.LoadCache(mod);
            var files = mod.Files;
            await modcache.ClearCache();
            foreach (var file in files)
            {
                await modcache.SetFileCacheData(file.Key, file.Value.TargetFileMD5, null, cancellationToken);
            }
            return modcache;
        }

        public async Task<ModCacheFile> LoadCache(ModDefinition mod)
        {
            var modcache = this._modCache.GetOrAdd(mod, x => new LazyModCacheFile(mod)).Value;
            await modcache.Init();
            return modcache;
        }

        public async Task BuildModList()
        {

        }

        public async ValueTask DisposeAsync()
        {
            var old = this._modCache.ToArray();
            this._modCache.Clear();
            LazyModCacheFile val;
            foreach (var mod in old)
            {
                val = mod.Value;
                if (val.IsValueCreated)
                {
                    await val.Value.DisposeAsync();
                }
            }
        }

        sealed class LazyModCacheFile
        {
            private readonly ModDefinition mod;
            private readonly Lazy<ModCacheFile> lazy;

            public ModCacheFile Value => this.lazy.Value;
            public bool IsValueCreated => this.lazy.IsValueCreated;

            private ModCacheFile Create() => new ModCacheFile(Path.Combine(this.mod.ModDirectory, "metadata-cache.db"));

            public LazyModCacheFile(ModDefinition mod)
            {
                this.mod = mod;
                this.lazy = new Lazy<ModCacheFile>(this.Create);
            }
        }
    }
}

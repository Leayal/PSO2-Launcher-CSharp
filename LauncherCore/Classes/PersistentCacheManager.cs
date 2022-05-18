using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes
{
    public abstract partial class PersistentCacheManager : IDisposable
    {
        protected const string dbName = "__.db";
        public readonly string CacheRootDirectory;
        private readonly ConcurrentDictionary<string, LazySemaphoreSlimEx> lockObjs;
        private readonly SemaphoreSlim dbLock;
        private bool _disposed;

        public static PersistentCacheManager Create(string rootDirectory)  => Environment.Is64BitProcess ? new PersistentCacheManagerX64(rootDirectory) : new PersistentCacheManagerX86(rootDirectory);

        protected PersistentCacheManager(string rootDirectory, bool earlyInitDatabase)
        {
            this._disposed = false;
            this.lockObjs = new ConcurrentDictionary<string, LazySemaphoreSlimEx>(StringComparer.OrdinalIgnoreCase);
            this.dbLock = new SemaphoreSlim(0, 1);
            this.CacheRootDirectory = Directory.CreateDirectory(rootDirectory).FullName;
            
            if (earlyInitDatabase)
            {
                this.InitHeadersDatabase();
            }
        }

        /// <summary>Fetch a cache entry.</summary>
        /// <typeparam name="TArg">The type of argument object which will be passed to the callbacks.</typeparam>
        /// <param name="entryName">Entry name is case-insensitive.</param>
        /// <param name="factory">The callback which will be invoked when the cache is invalid or non-existed.</param>
        /// <param name="verifyCache">The callback which will be invoked to verify whether cache is valid or not.</param>
        /// <param name="args">The argument object to pass the the callbacks.</param>
        /// <returns>A task which will complete when cache verification or creation is finished.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="entryName"/> is null or empty string.</exception>
        /// <exception cref="ArgumentException"><paramref name="entryName"/> is equal to '__.db', which was used to store cache headers.</exception>
        public Task<Stream> Fetch<TArg>(string entryName, Func<string, Utf8JsonWriter, TArg, Stream, CancellationToken, Task<bool>> factory, Func<string, JsonDocument, Stream, TArg, CancellationToken, Task<bool>> verifyCache, TArg args, CancellationToken cancellationToken)
        {
            if (this._disposed) throw new ObjectDisposedException("PersistentCacheManager");

            if (string.IsNullOrEmpty(entryName)) throw new ArgumentNullException(null, nameof(entryName));
            if (string.Equals(entryName, dbName, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException(null, nameof(entryName));
            
            var safe_lockObj = this.lockObjs.GetOrAdd(entryName, this.CreateNewSemaphore);

            return Task.Factory.StartNew(async obj =>
            {
                var (myself, lockObj, entryName, factory, verifyCache, args, cancellationToken) = (ValueTuple<PersistentCacheManager, SemaphoreSlimEx, string, Func<string, Utf8JsonWriter, TArg, Stream, CancellationToken, Task<bool>>, Func<string, JsonDocument, Stream, TArg, CancellationToken, Task<bool>>, TArg, CancellationToken>)obj;

                try
                {
                    await lockObj.WaitForIt(cancellationToken).ConfigureAwait(false);
                    bool isSuccess;

                    Stream result;

                    var cachePath = Path.Combine(myself.CacheRootDirectory, entryName);
                    if (File.Exists(cachePath))
                    {
                        result = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        bool isCacheValid = false;
                        using (var entryHeader = await myself.FetchCacheHeader(entryName, cancellationToken).ConfigureAwait(false))
                        {
                            if (entryHeader != null)
                            {
                                isCacheValid = await verifyCache.Invoke(entryName, entryHeader, result, args, cancellationToken).ConfigureAwait(false);
                            }
                        }
                        if (!isCacheValid)
                        {
                            result.Dispose();
                            result = null;
                            var buffer = new System.Buffers.ArrayBufferWriter<byte>(256);
                            using (var headerWriter = new Utf8JsonWriter(buffer))
                            {
                                headerWriter.WriteStartObject();

                                using (var fs = new FileStream(cachePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                                {
                                    isSuccess = await factory.Invoke(entryName, headerWriter, args, fs, cancellationToken).ConfigureAwait(false);
                                }

                                headerWriter.WriteEndObject();
                                headerWriter.Flush();
                            }
                            if (isSuccess)
                            {
                                await myself.WriteCacheHeader(entryName, buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
                                result = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            }
                            else
                            {
                                File.Delete(cachePath);
                                await myself.DeleteCacheHeader(entryName, cancellationToken).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            isSuccess = true;
                            try
                            {
                                result.Position = 0;
                            }
                            catch (ObjectDisposedException)
                            {
                                // In case user call Dispose() on the stream, can't re-use, only re-create.
                                result = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            }
                        }
                    }
                    else
                    {
                        var buffer = new System.Buffers.ArrayBufferWriter<byte>(256);
                        using (var headerWriter = new Utf8JsonWriter(buffer))
                        {
                            headerWriter.WriteStartObject();

                            using (var fs = File.Create(cachePath))
                            {
                                isSuccess = await factory.Invoke(entryName, headerWriter, args, fs, cancellationToken).ConfigureAwait(false);
                            }

                            headerWriter.WriteEndObject();
                            headerWriter.Flush();
                        }
                        if (isSuccess)
                        {
                            await myself.WriteCacheHeader(entryName, buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
                            result = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        }
                        else
                        {
                            result = null;
                            File.Delete(cachePath);
                            await myself.DeleteCacheHeader(entryName, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    if (isSuccess)
                    {
                        return result;
                    }
                    else
                    {
                        return null;
                    }
                }
                finally
                {
                    lockObj.Done();
                }
            }, new ValueTuple<PersistentCacheManager, SemaphoreSlimEx, string, Func<string, Utf8JsonWriter, TArg, Stream, CancellationToken, Task<bool>>, Func<string, JsonDocument, Stream, TArg, CancellationToken, Task<bool>>, TArg, CancellationToken>(this, safe_lockObj.Value, entryName, factory, verifyCache, args, cancellationToken), cancellationToken, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).Unwrap();
        }

        private LazySemaphoreSlimEx CreateNewSemaphore(string entryName) => new LazySemaphoreSlimEx(this.lockObjs, entryName);

        protected virtual void InitHeadersDatabase()
        {
            this.dbLock.Release();
        }
        protected abstract Task<JsonDocument> FetchCacheHeader(string entryName, CancellationToken cancellationToken);
        protected abstract Task WriteCacheHeader(string entryName, ReadOnlyMemory<byte> entryHeaderData, CancellationToken cancellationToken);
        protected abstract Task DeleteCacheHeader(string entryName, CancellationToken cancellationToken);

        protected async Task<IDisposable> EnterDatabaseLock(CancellationToken cancellationToken)
        {
            var result = new DatabaseAsyncLock(this.dbLock);
            await this.dbLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }

        public void Dispose()
        {
            if (this._disposed) return;

            this._disposed = true;

            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.dbLock.Dispose();
            var locks = lockObjs.ToArray();
            lockObjs.Clear();
            for (int i = 0; i < locks.Length; i++)
            {
                locks[i].Value.Dispose();
            }
        }

        ~PersistentCacheManager() => this.Dispose(false);

        readonly struct DatabaseAsyncLock : IDisposable
        {
            private readonly SemaphoreSlim sema;
            public DatabaseAsyncLock(SemaphoreSlim sema)
            {
                this.sema = sema;
            }

            public void Dispose()
            {
                this.sema.Release();
            }
        }

        protected class LazySemaphoreSlimEx : IDisposable
        {
            private readonly Lazy<SemaphoreSlimEx> _lazy;
            private readonly ConcurrentDictionary<string, LazySemaphoreSlimEx> lockObjs;
            private readonly string entryName;
            public LazySemaphoreSlimEx(ConcurrentDictionary<string, LazySemaphoreSlimEx> lockObjs, string entryName)
            {
                this.lockObjs = lockObjs;
                this.entryName = entryName;
                this._lazy = new Lazy<SemaphoreSlimEx>(this.Create);
            }

            private SemaphoreSlimEx Create() => new SemaphoreSlimEx(this.lockObjs, this.entryName);

            public bool IsValueCreated => this._lazy.IsValueCreated;

            public SemaphoreSlimEx Value => this._lazy.Value;

            public void Dispose()
            {
                if (this._lazy.IsValueCreated)
                {
                    this._lazy.Value.Dispose();
                }
            }
        }

        protected class SemaphoreSlimEx : SemaphoreSlim
        {
            private int _count;
            private readonly ConcurrentDictionary<string, LazySemaphoreSlimEx> lockObjs;
            private readonly string entryName;

            public SemaphoreSlimEx(ConcurrentDictionary<string, LazySemaphoreSlimEx> lockObjs, string entryName) : base(1, 1)
            {
                this.entryName = entryName;
                this.lockObjs = lockObjs;
                this._count = 0;
            }

            public Task WaitForIt(CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref this._count);
                return this.WaitAsync(cancellationToken);
            }

            public void Done()
            {
                if (Interlocked.Decrement(ref this._count) == 0)
                {
                    this.Release();
                    if (this.lockObjs.TryRemove(this.entryName, out var _lazy))
                    {
                        _lazy.Dispose();
                    }
                    else
                    {
                        this.Dispose();
                    }
                }
                else
                {
                    this.Release();
                }
            }
        }
    }
}

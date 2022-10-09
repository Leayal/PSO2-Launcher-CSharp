using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SQLite;
using SQLitePCL;

namespace Leayal.PSO2.Modding.Cache
{
    /// <summary>A class to deal with mod cache file.</summary>
    public sealed class ModCacheFile : IAsyncDisposable
    {
        private const string DatabaseEncKeyToAvoidUserMessTheFileWrongly = "LeaModMgrCacheFile";

        private readonly SQLiteAsyncConnection conn;

        public ModCacheFile(string databaseFilePath)
        {
            this.conn = new SQLiteAsyncConnection(new SQLiteConnectionString(databaseFilePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.Create | SQLiteOpenFlags.PrivateCache, true, DatabaseEncKeyToAvoidUserMessTheFileWrongly));
        }

        /// <summary>Initalize the database if it hasn't.</summary>
        public async Task Init()
        {
            await this.conn.EnableWriteAheadLoggingAsync();
            await this.conn.CreateTableAsync<FileModCacheData>();
        }

        /// <summary>Gets the file's cached data.</summary>
        /// <param name="relativeFilename">The relative filepath in mod package.</param>
        /// <returns>A <seealso cref="FileModCacheData"/> if the data exists. Otherwise, <see langword="null"/>.</returns>
        public async Task<FileModCacheData?> TryGetFileCacheData(string relativeFilename)
        {
            return await this.conn.FindWithQueryAsync<FileModCacheData>("SELECT * FROM FileModCacheData WHERE FilenameInMod=?", relativeFilename);
        }

        /// <summary>Clear the cache to clean slate.</summary>
        public Task ClearCache() => this.conn.DeleteAllAsync<FileModCacheData>();

        /// <summary>Gets the file's cached data or create a new one if it doesn't exist.</summary>
        /// <param name="relativeFilename">The relative filepath in mod package.</param>
        /// <param name="targetMD5">The MD5 checksum of targeted file to be modded. Can be <see langword="null"/>. The string should be all UPPERCASE.</param>
        /// <param name="fullpathToGetMetadata">The full filepath to the mod file on the machine. In case it's <see langword="null"/>, the path will be from the current mod package.</param>
        /// <param name="cancellationToken">The cancellation token to signal operation cancellation.</param>
        /// <returns>A <seealso cref="FileModCacheData"/> if cache exists or the cache is set successfully. Otherwise, <see langword="null"/>.</returns>
        public async Task<FileModCacheData?> GetOrSetFileCacheData(string relativeFilename, string? targetMD5 = null, string? fullpathToGetMetadata = null, CancellationToken cancellationToken = default)
        {
            return (await TryGetFileCacheData(relativeFilename)) ?? (await SetFileCacheData(relativeFilename, targetMD5, fullpathToGetMetadata, cancellationToken));
        }

        /// <summary>Sets the file's cached data.</summary>
        /// <param name="relativeFilename">The relative filepath in mod package.</param>
        /// <param name="targetMD5">The MD5 checksum of targeted file to be modded. Can be <see langword="null"/>. The string should be all UPPERCASE.</param>
        /// <param name="fullpathToGetMetadata">The full filepath to the mod file on the machine. In case it's <see langword="null"/>, the path will be from the current mod package.</param>
        /// <param name="cancellationToken">The cancellation token to signal operation cancellation.</param>
        /// <returns>The created <seealso cref="FileModCacheData"/> if the operation success. Otherwise, <see langword="null"/>.</returns>
        public async Task<FileModCacheData?> SetFileCacheData(string relativeFilename, string? targetMD5 = null, string? fullpathToGetMetadata = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(fullpathToGetMetadata))
            {
                fullpathToGetMetadata = Path.Combine(Path.GetDirectoryName(this.conn.DatabasePath) ?? string.Empty, relativeFilename);
            }
            var (time, moddedsize, moddedmd5) = await Task.Factory.StartNew<Tuple<DateTime, long, string>>(obj =>
            {
                if (obj is string str)
                {
                    var writetime = File.GetLastWriteTimeUtc(str);
                    using (var fs = File.OpenRead(str))
                        return new Tuple<DateTime, long, string>(writetime, fs.Length, HelperMethods.ComputeHashFromFile(fs));
                }
                return new Tuple<DateTime, long, string>(DateTime.MinValue, 0, string.Empty);
            }, fullpathToGetMetadata, cancellationToken);
            
            if (!cancellationToken.IsCancellationRequested)
            {
                var result = new FileModCacheData(relativeFilename, targetMD5, moddedmd5, time, moddedsize);
                if (await this.conn.InsertOrReplaceAsync(result) != 0)
                {
                    return result;
                }
            }
            return null;
        }

        /// <summary>Sets the file's cached data.</summary>
        /// <param name="data">The data to insert or replace existing..</param>
        /// <returns><see langword="true"/> if the operation success. Otherwise, <see langword="false"/>.</returns>
        public async Task<bool> SetFileCacheData(FileModCacheData data)
        {
            return (await this.conn.InsertOrReplaceAsync(data) != 0);
        }

        /// <summary>Attempts to close all underlying database connections and then close the database.</summary>
        /// <returns>An awaitable <seealso cref="ValueTask"/> which will completes when all attempts are finished.</returns>
        public ValueTask DisposeAsync() => new ValueTask(this.conn.CloseAsync());
    }
}

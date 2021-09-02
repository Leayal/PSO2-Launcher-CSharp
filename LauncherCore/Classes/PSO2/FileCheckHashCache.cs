using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using SQLite;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using System.IO;
using Leayal.SharedInterfaces;
using System.Threading;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public class FileCheckHashCache
    {
        private const int LatestVersion = 2;

        private static readonly ConcurrentDictionary<string, FileCheckHashCache> _connectionPool;

        static FileCheckHashCache()
        {
            _connectionPool = new ConcurrentDictionary<string, FileCheckHashCache>(StringComparer.OrdinalIgnoreCase);
        }

        public static FileCheckHashCache CreateOrOpen(string path)
        {
            return _connectionPool.AddOrUpdate(path, (key) => 
            {
                try
                {
                    var connection = new FileCheckHashCache(key);
                    connection.IncreaseRefCount();
                    _ = connection.Load();
                    return connection;
                }
                catch
                {
                    throw new DatabaseErrorException();
                }
            }, (key, existing) => 
            {
                if (existing.CancelScheduleClose())
                {
                    existing.IncreaseRefCount();
                    return existing;
                }
                else
                {
                    try
                    {
                        var connection = new FileCheckHashCache(key);
                        connection.IncreaseRefCount();
                        _ = connection.Load();
                        return connection;
                    }
                    catch
                    {
                        throw new DatabaseErrorException();
                    }
                }
            });
        }

        public static async void Close(FileCheckHashCache db)
        {
            if (_connectionPool.TryGetValue(db.filepath, out var connection))
            {
                if (connection.DecreaseRefCount() == 0)
                {
                    var canceltoken = await connection.ScheduleCloseAsync();

                    // Technically, CancellationToken.None has `CanBeCanceled` prop is false.
                    // But idk if it's changed in the future, so checking if it's not the None should be more accurate.
                    if (canceltoken != CancellationToken.None && canceltoken.CanBeCanceled && !canceltoken.IsCancellationRequested)
                    {
                        _connectionPool.TryRemove(db.filepath, out _);
                    }
                }
            }
        }

        public static async Task ForceCloseAll()
        {
            string[] keys = new string[_connectionPool.Keys.Count];
            if (keys.Length == 0) return;
            _connectionPool.Keys.CopyTo(keys, 0);
            for (int i = 0; i < keys.Length; i++)
            {
                if (_connectionPool.TryRemove(keys[i], out var connection))
                {
                    try
                    {
                        await connection.ForceClose();
                    }
                    catch { }
                }
            }
        }

        public static void ForceCloseAllSync()
        {
            string[] keys = new string[_connectionPool.Keys.Count];
            if (keys.Length == 0) return;
            _connectionPool.Keys.CopyTo(keys, 0);
            for (int i = 0; i < keys.Length; i++)
            {
                if (_connectionPool.TryRemove(keys[i], out var connection))
                {
                    try
                    {
                        connection.ForceCloseSync();
                    }
                    catch { }
                }
            }
        }

        private readonly string filepath;
        private readonly SQLiteAsyncConnection sqlConn;
        private Task t_load;
        private int flag_state;
        private CancellationTokenSource cancelSchedule;

        private int refCount;

        private FileCheckHashCache(string filepath)
        {
            this.cancelSchedule = null;
            this.filepath = filepath;
            this.refCount = 0;
            this.flag_state = 0;
            var connectionStr = new SQLiteConnectionString(filepath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.Create | SQLiteOpenFlags.PrivateCache, true, "leapso2ngshashtable");
            this.sqlConn = new SQLiteAsyncConnection(connectionStr);
        }

        private int IncreaseRefCount() => Interlocked.Increment(ref this.refCount);

        private int DecreaseRefCount() => Interlocked.Decrement(ref this.refCount);

        public async Task Load()
        {
            var state = Interlocked.CompareExchange(ref this.flag_state, 1, 0);
            if (state == 0)
            {
                this.t_load = this.Init();
            }
            else if (state == 3)
            {
                throw new ObjectDisposedException(nameof(FileCheckHashCache));
            }
            await this.t_load;
        }


        private async Task Init()
        {
            await this.sqlConn.EnableWriteAheadLoggingAsync();
            var versionTb = await this.sqlConn.CreateTableAsync<Versioning>();
            var oldRecordTb = await this.sqlConn.CreateTableAsync<PatchRecordItem>();
            if (versionTb == CreateTableResult.Created)
            {
                await this.sqlConn.InsertAsync(new Versioning() { TableName = "Ver", TableVersion = LatestVersion });
                if (oldRecordTb == CreateTableResult.Migrated)
                {
                    await this.sqlConn.DropTableAsync<PatchRecordItem>();
                    await this.sqlConn.CreateTableAsync<PatchRecordItem>();
                }
            }
            else
            {
                try
                {
                    var verRecordTb = await this.sqlConn.FindAsync<Versioning>("Ver");
                    if (verRecordTb == null)
                    {
                        await this.sqlConn.InsertAsync(new Versioning() { TableName = "Ver", TableVersion = LatestVersion });
                        await this.sqlConn.DropTableAsync<PatchRecordItem>();
                        await this.sqlConn.CreateTableAsync<PatchRecordItem>();
                    }
                    else if (verRecordTb.TableVersion != LatestVersion)
                    {
                        await this.Upgrading(verRecordTb.TableVersion);
                    }
                }
                catch (InvalidOperationException) // Why do you even call this as "Not found" exception
                {
                    await this.sqlConn.InsertAsync(new Versioning() { TableName = "Ver", TableVersion = LatestVersion });
                    await this.sqlConn.DropTableAsync<PatchRecordItem>();
                    await this.sqlConn.CreateTableAsync<PatchRecordItem>();
                }
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task Upgrading(int fromVersion)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (fromVersion < 1)
            {
                // Universe exploded
            }
            else
            {
                if (fromVersion == 1)
                {

                }
                if (fromVersion == LatestVersion)
                {
                    return;
                }
            }
        }

        public async Task<PatchRecordItem> GetPatchItem(string filename)
        {
            try
            {
                return await this.sqlConn.Table<PatchRecordItem>().FirstOrDefaultAsync(obj => obj.RemoteFilename == filename);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        public async Task<PatchRecordItem> SetPatchItem(PatchListItem item, DateTime lastModifiedTimeUTC)
        {
            var obj = new PatchRecordItem() { RemoteFilename = item.GetFilenameWithoutAffix(), FileSize = item.FileSize, MD5 = item.MD5, LastModifiedTimeUTC = lastModifiedTimeUTC };
            var result = await this.sqlConn.InsertOrReplaceAsync(obj);
            if (result != 0)
            {
                return obj;
            }
            else
            {
                return null;
            }
        }

        private bool CancelScheduleClose()
        {
            var state = Interlocked.CompareExchange(ref this.flag_state, 1, 2);
            if (state == 2)
            {
                this.cancelSchedule.Cancel();
                return true;
            }
            else if (state == 3)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private async Task<CancellationToken> ScheduleCloseAsync()
        {
            var state = Interlocked.CompareExchange(ref this.flag_state, 2, 1);
            if (state == 1)
            {
                this.cancelSchedule = new CancellationTokenSource();
                var token = this.cancelSchedule.Token;
                await Task.Delay(TimeSpan.FromSeconds(10), token).ContinueWith(async t =>
                {
                    if (!token.IsCancellationRequested && !t.IsCanceled)
                    {
                        if (Interlocked.CompareExchange(ref this.flag_state, 3, 2) == 2)
                        {
                            await this.ForceClose();
                        }
                    }
                    this.cancelSchedule?.Dispose();
                    this.cancelSchedule = null;
                }, token).Unwrap();
                return token;
            }
            return CancellationToken.None;
        }

        private async Task ForceClose()
        {
            await this.sqlConn.CloseAsync();
        }

        /// <summary>Only use when the application is exiting and that no database operation is on-going before closing.</summary>
        private void ForceCloseSync()
        {
            using (var connection = this.sqlConn.GetConnection())
            {
                if (connection.IsInTransaction)
                {
                    connection.Rollback();
                }
                connection.Close();
            }
        }

        public class DatabaseErrorException : Exception { }

        public class PatchRecordItem
        {
            [PrimaryKey, Unique, NotNull, MaxLength(2048)]
            public string RemoteFilename { get; set; }
            [MaxLength(32)]
            public string MD5 { get; set; }
            public long FileSize { get; set; }
            [NotNull]
            public DateTime LastModifiedTimeUTC { get; set; }
        }

        class Versioning
        {
            // Not sure whether I should do unique along with Primary
            [PrimaryKey, Unique, NotNull, MaxLength(256)]
            public string TableName { get; set; }
            [NotNull]
            public int TableVersion { get; set; }
        }
    }
}

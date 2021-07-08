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
                var state = Interlocked.CompareExchange(ref existing.flag_state, 0, 0);
                if (state == 3)
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
                else
                {
                    if (existing.IncreaseRefCount() == 1)
                    {
                        existing.CancelScheduleClose();
                    }
                    return existing;
                }
            });
        }

        public static async void Close(FileCheckHashCache db)
        {
            if (_connectionPool.TryGetValue(db.filepath, out var connection))
            {
                if (connection.DecreaseRefCount() == 0)
                {
                    await connection.ScheduleCloseAsync().ContinueWith(t =>
                    {
                        _connectionPool.TryRemove(db.filepath, out connection);
                    });
                }
            }
        }

        public static async Task ForceCloseAll()
        {
            string[] keys = new string[_connectionPool.Keys.Count];
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

        private const int LatestVersion = 1;

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
            var connectionStr = new SQLiteConnectionString(filepath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.Create, true, "leapso2ngshashtable");
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
                    var verRecordTb = await this.sqlConn.GetAsync<Versioning>("Ver");
                    if (verRecordTb.TableVersion != LatestVersion)
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

        private void CancelScheduleClose()
        {
            var state = Interlocked.CompareExchange(ref this.flag_state, 1, 2);
            if (state == 2)
            {
                this.cancelSchedule.Cancel();
            }
        }

        private async Task ScheduleCloseAsync()
        {
            var state = Interlocked.CompareExchange(ref this.flag_state, 2, 1);
            if (state == 1)
            {
                this.cancelSchedule = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var token = this.cancelSchedule.Token;
                await Task.Delay(TimeSpan.FromSeconds(30), token).ContinueWith(async t =>
                {
                    if (!t.IsCanceled)
                    {
                        if (Interlocked.CompareExchange(ref this.flag_state, 3, 2) == 2)
                        {
                            await this.ForceClose();
                        }
                    }
                }).Unwrap();
            }
        }

        private async Task ForceClose()
        {
            await this.sqlConn.CloseAsync();
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

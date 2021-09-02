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
    public class FileCheckHashCache : IAsyncDisposable
    {
        private const int LatestVersion = 2;

        private readonly SQLiteAsyncConnection sqlConn;
        private readonly Task t_load;
        private int state;

        public FileCheckHashCache(string filepath)
        {
            this.state = 0;
            var connectionStr = new SQLiteConnectionString(filepath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.Create | SQLiteOpenFlags.PrivateCache, true, "leapso2ngshashtable");
            this.sqlConn = new SQLiteAsyncConnection(connectionStr);

            this.t_load = this.Init();
            if (this.t_load.Status == TaskStatus.Created)
            {
                this.t_load.Start();
            }
        }

        public async Task Load()
        {
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
            switch (Interlocked.CompareExchange(ref this.state, -1, -1))
            {
                case 0:
                    await this.t_load;
                    try
                    {
                        return await this.sqlConn.Table<PatchRecordItem>().FirstOrDefaultAsync(obj => obj.RemoteFilename == filename);
                    }
                    catch (InvalidOperationException)
                    {
                        return null;
                    }
                case 1:
                    try
                    {
                        return await this.sqlConn.Table<PatchRecordItem>().FirstOrDefaultAsync(obj => obj.RemoteFilename == filename);
                    }
                    catch (InvalidOperationException)
                    {
                        return null;
                    }
                case 2:
                    throw new ObjectDisposedException(nameof(FileCheckHashCache));
                default:
                    throw new InvalidOperationException();
            }
        }

        public async Task<PatchRecordItem> SetPatchItem(PatchListItem item, DateTime lastModifiedTimeUTC)
        {
            var oldstate = Interlocked.CompareExchange(ref this.state, -1, -1);
            if (oldstate == 2)
            {
                throw new ObjectDisposedException(nameof(FileCheckHashCache));
            }
            else
            {
                if (oldstate >= 0 && oldstate < 2)
                {
                    if (oldstate == 0)
                    {
                        await this.t_load;
                    }
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
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            var oldstate = Interlocked.Exchange(ref this.state, 2);
            if (oldstate != 2)
            {
                if (oldstate == 0)
                {
                    await t_load;
                }
                await this.sqlConn.CloseAsync();
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

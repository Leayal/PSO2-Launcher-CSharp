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

        private readonly int concurrentlevel;
        private readonly SQLiteConnection sqlConn;
        private readonly Task t_write;
        private int state;

        private ConcurrentDictionary<string, PatchRecordItem> buffering;
        private readonly BlockingCollection<PatchRecordItem> writebuffer;

        public FileCheckHashCache(string filepath, in int concurrentLevel)
        {
            this.state = 0;
            this.buffering = null;
            this.concurrentlevel = Math.Min(Environment.ProcessorCount, concurrentLevel);
            this.writebuffer = new BlockingCollection<PatchRecordItem>(new ConcurrentQueue<PatchRecordItem>());
            var connectionStr = new SQLiteConnectionString(filepath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.Create | SQLiteOpenFlags.PrivateCache, true, "leapso2ngshashtable");
            this.sqlConn = new SQLiteConnection(connectionStr);
            this.t_write = Task.Factory.StartNew(this.PollingWrite, TaskCreationOptions.LongRunning).Unwrap();
        }

        private async Task PollingWrite()
        {
            while (!this.writebuffer.IsCompleted)
            {
                if (this.writebuffer.TryTake(out var item))
                {
                    this.sqlConn.BeginTransaction();
                    try
                    {
                        this.sqlConn.InsertOrReplace(item);
                        while (this.writebuffer.TryTake(out item))
                        {
                            this.sqlConn.InsertOrReplace(item);
                        }
                        this.sqlConn.Commit();
                    }
                    catch
                    {
                        this.sqlConn.Rollback();
                    }
                }
                else
                {
                    if (!this.writebuffer.IsAddingCompleted)
                    {
                        await Task.Delay(200);
                    }
                }
            }
        }

        public void Load()
        {
            if (Interlocked.CompareExchange(ref this.state, 1, 0) == 0)
            {
                this.sqlConn.EnableWriteAheadLogging();
                var versionTb = this.sqlConn.CreateTable<Versioning>();
                var oldRecordTb = this.sqlConn.CreateTable<PatchRecordItem>();
                if (versionTb == CreateTableResult.Created)
                {
                    this.sqlConn.Insert(new Versioning() { TableName = "Ver", TableVersion = LatestVersion });
                    if (oldRecordTb == CreateTableResult.Migrated)
                    {
                        this.sqlConn.DropTable<PatchRecordItem>();
                        this.sqlConn.CreateTable<PatchRecordItem>();
                    }
                }
                else
                {
                    try
                    {
                        var verRecordTb = this.sqlConn.Find<Versioning>("Ver");
                        if (verRecordTb == null)
                        {
                            this.sqlConn.Insert(new Versioning() { TableName = "Ver", TableVersion = LatestVersion });
                            this.sqlConn.DropTable<PatchRecordItem>();
                            this.sqlConn.CreateTable<PatchRecordItem>();
                        }
                        else if (verRecordTb.TableVersion != LatestVersion)
                        {
                            this.Upgrading(verRecordTb.TableVersion);
                        }
                    }
                    catch (InvalidOperationException) // Why do you even call this as "Not found" exception
                    {
                        this.sqlConn.InsertOrReplace(new Versioning() { TableName = "Ver", TableVersion = LatestVersion });
                        this.sqlConn.DropTable<PatchRecordItem>();
                        this.sqlConn.CreateTable<PatchRecordItem>();
                    }
                }

                // Buffering data into memory.
                var tb = this.sqlConn.Table<PatchRecordItem>();
                var count = tb.Count();
                if (count <= 0)
                {
                    this.buffering = new ConcurrentDictionary<string, PatchRecordItem>(this.concurrentlevel, 331, StringComparer.InvariantCultureIgnoreCase);
                }
                else
                {
                    this.buffering = new ConcurrentDictionary<string, PatchRecordItem>(this.concurrentlevel, count + 300, StringComparer.InvariantCultureIgnoreCase);
                }

                foreach (var item in tb.Deferred())
                {
                    this.buffering.TryAdd(item.RemoteFilename, item);
                }
            }
        }

        private void Upgrading(int fromVersion)
        {
            if (fromVersion < 1)
            {
                // Universe exploded
            }
            else
            {
                if (fromVersion == 1)
                {
                    this.sqlConn.InsertOrReplace(new Versioning() { TableName = "Ver", TableVersion = 2 });
                }
                if (fromVersion == LatestVersion)
                {
                    return;
                }
            }
        }

        public bool TryGetPatchItem(string filename, out PatchRecordItem item)
        {
            switch (Interlocked.CompareExchange(ref this.state, -1, -1))
            {
                case 0:
                    throw new InvalidOperationException("Please call Load() before using.");
                case 1:
                    return this.buffering.TryGetValue(filename, out item);
                case 2:
                    throw new ObjectDisposedException(nameof(FileCheckHashCache));
                default:
                    throw new InvalidOperationException();
            }
        }

        public PatchRecordItem SetPatchItem(in PatchListItem item, in DateTime lastModifiedTimeUTC)
        {
            switch (Interlocked.CompareExchange(ref this.state, -1, -1))
            {
                case 0:
                    throw new InvalidOperationException("Please call Load() before using.");
                case 1:
                    // this.buffering.AddOrUpdate(item.RemoteFilename, )
                    // var obj = new PatchRecordItem() { RemoteFilename = string.Create(item.GetSpanFilenameWithoutAffix().Length, item, (c, obj) => obj.GetSpanFilenameWithoutAffix().ToLowerInvariant(c)), FileSize = item.FileSize, MD5 = item.MD5, LastModifiedTimeUTC = lastModifiedTimeUTC };
                    return this.buffering.AddOrUpdate(item.RemoteFilename, (key, args) =>
                    {
                        var _item = args.Item1;
                        var result = new PatchRecordItem() { RemoteFilename = string.Create(_item.GetSpanFilenameWithoutAffix().Length, _item, (c, obj) => obj.GetSpanFilenameWithoutAffix().ToLowerInvariant(c)), FileSize = _item.FileSize, MD5 = _item.MD5, LastModifiedTimeUTC = args.Item2 };
                        args.Item3.Add(result);
                        return result;
                    }, (key, existing, args) =>
                    {
                        var _item = args.Item1;
                        var str = string.Create(_item.GetSpanFilenameWithoutAffix().Length, _item, (c, obj) => obj.GetSpanFilenameWithoutAffix().ToLowerInvariant(c));
                        if (!PatchRecordItem.IsEquals(existing, str, _item.MD5, in _item.FileSize, in args.Item2))
                        {
                            var result = new PatchRecordItem()
                            {
                                RemoteFilename = str,
                                FileSize = _item.FileSize,
                                MD5 = _item.MD5,
                                LastModifiedTimeUTC = args.Item2
                            };
                            args.Item3.Add(result);
                            return result;
                        }
                        else
                        {
                            return existing;
                        }
                    }, new ValueTuple<PatchListItem, DateTime, BlockingCollection<PatchRecordItem>>(item, lastModifiedTimeUTC, this.writebuffer));
                case 2:
                    throw new ObjectDisposedException(nameof(FileCheckHashCache));
                default:
                    throw new InvalidOperationException();
            }
        }

        public async ValueTask DisposeAsync()
        {
            var oldstate = Interlocked.Exchange(ref this.state, 2);
            if (oldstate != 2)
            {
                this.writebuffer.CompleteAdding();

                await this.t_write;

                this.sqlConn.ExecuteScalar<string>("PRAGMA optimize;", Array.Empty<object>());

                this.sqlConn.Close();
                this.sqlConn.Dispose();
            }
        }

        public class DatabaseErrorException : Exception { }

        public class PatchRecordItem : IEquatable<PatchRecordItem>
        {
            [PrimaryKey, Unique, NotNull, MaxLength(2048)]
            public string RemoteFilename { get; set; }
            [MaxLength(32)]
            public string MD5 { get; set; }
            public long FileSize { get; set; }
            [NotNull]
            public DateTime LastModifiedTimeUTC { get; set; }

            public override bool Equals(object obj)
            {
                if (obj is PatchRecordItem item)
                    return this.Equals(item);
                return base.Equals(obj);
            }

            public static bool IsEquals(PatchRecordItem item, string remotefilename, string md5, in long filesize, in DateTime lastmodified)
            {
                return (string.Equals(remotefilename, item.RemoteFilename, StringComparison.InvariantCultureIgnoreCase)
                   && string.Equals(md5, item.MD5, StringComparison.InvariantCultureIgnoreCase)
                   && filesize == item.FileSize
                   && lastmodified == item.LastModifiedTimeUTC);
            }

            public override int GetHashCode()
                => this.RemoteFilename.GetHashCode() ^ this.MD5.GetHashCode() ^ this.FileSize.GetHashCode() ^ this.LastModifiedTimeUTC.GetHashCode();

            public bool Equals(PatchRecordItem other)
            {
                return (string.Equals(other.RemoteFilename, this.RemoteFilename, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(other.MD5, this.MD5, StringComparison.InvariantCultureIgnoreCase)
                    && other.FileSize == this.FileSize
                    && other.LastModifiedTimeUTC == this.LastModifiedTimeUTC);
            }
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

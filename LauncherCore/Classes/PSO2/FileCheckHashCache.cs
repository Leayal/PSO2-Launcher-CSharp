using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using SQLite;
using CA = System.Diagnostics.CodeAnalysis;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public interface IFileCheckHashCache : IAsyncDisposable
    {
        void Load();
        bool TryGetPatchItem(string filename, [CA.NotNullWhen(true)] out PatchRecordItemValue? item);
        PatchRecordItemValue SetPatchItem(PatchListItem item, in DateTime lastModifiedTimeUTC);
    }

    public class DatabaseErrorException : Exception { }

    public class PatchRecordItemValue : IEquatable<PatchRecordItemValue>
    {
        // [PrimaryKey, Unique, NotNull, MaxLength(2048)]
        public readonly string RemoteFilename;
        // [MaxLength(32)]
        public readonly string MD5;
        public readonly long FileSize;
        // [NotNull]
        public readonly DateTime LastModifiedTimeUTC;

        public PatchRecordItemValue(string filename, in long filesize, string md5, in DateTime modifiedTimeUtc)
        {
            this.RemoteFilename = filename;
            this.FileSize = filesize;
            this.MD5 = md5;
            this.LastModifiedTimeUTC = modifiedTimeUtc;
        }

        public static bool IsEquals(PatchRecordItemValue item, string remotefilename, string md5, in long filesize, in DateTime lastmodified)
        {
            return (string.Equals(remotefilename, item.RemoteFilename, StringComparison.InvariantCultureIgnoreCase)
               && string.Equals(md5, item.MD5, StringComparison.InvariantCultureIgnoreCase)
               && filesize == item.FileSize
               && lastmodified == item.LastModifiedTimeUTC);
        }

        public override int GetHashCode()
        {
            var hc = new HashCode();
            hc.Add(this.RemoteFilename, StringComparer.OrdinalIgnoreCase);
            hc.Add(this.MD5);
            hc.Add(this.FileSize);
            hc.Add(this.LastModifiedTimeUTC);
            return hc.ToHashCode();
        }

        public override bool Equals(object? other)
        {
            if (other is PatchRecordItemValue item)
            {
                return this.Equals(item);
            }
            return false;
        }

        public bool Equals(PatchRecordItemValue? other)
        {
            if (other == null) return false;

            return (string.Equals(other.RemoteFilename, this.RemoteFilename, StringComparison.OrdinalIgnoreCase)
                && string.Equals(other.MD5, this.MD5, StringComparison.OrdinalIgnoreCase)
                && other.FileSize == this.FileSize
                && other.LastModifiedTimeUTC == this.LastModifiedTimeUTC);
        }
    }

    public class FileCheckHashCache : IFileCheckHashCache, IReadOnlyDictionary<string, PatchRecordItemValue>
    {
        private const int LatestVersion = 2;
        private const string Versioning_Str = "Ver";
        private const int Versioning_Length = 4; // Versioning_Str.Length + 1

        /// <summary>For backward-compatible now. Use constructor instead.</summary>
        /// <param name="cacheFilePath"></param>
        /// <param name="concurrentLevel"></param>
        /// <returns></returns>
        public static FileCheckHashCache Create(string cacheFilePath, in int concurrentLevel) => new FileCheckHashCache(cacheFilePath, in concurrentLevel);

        private readonly SQLiteConnection sqlConn;
        private readonly int concurrentlevel;
        private readonly Task t_write;
        
        private int state;
        private string _bufferedPSO2ClientVersion;

        protected ConcurrentDictionary<string, PatchRecordItemValue> buffering;
        private readonly BlockingCollection<PatchRecordItemValue> writebuffer;

        public IEnumerable<string> Keys => this.buffering.Keys;

        public IEnumerable<PatchRecordItemValue> Values => this.buffering.Values;

        [Obsolete("This should NOT be used within a loop due to performance reasons.", false)]
        public int Count => this.buffering.Count;

        public bool IsReadOnly => this.writebuffer.IsAddingCompleted;

        public PatchRecordItemValue this[string key] => this.buffering[key];

        public FileCheckHashCache(string filepath, in int concurrentLevel)
        {
            this.state = 0;
            this.buffering = null;
            this._bufferedPSO2ClientVersion = string.Empty;
            this.concurrentlevel = Math.Min(Environment.ProcessorCount, concurrentLevel);
            this.writebuffer = new BlockingCollection<PatchRecordItemValue>(new ConcurrentQueue<PatchRecordItemValue>());
            this.t_write = Task.Factory.StartNew(this.PollingWrite, TaskCreationOptions.LongRunning).Unwrap();

            var connectionStr = new SQLiteConnectionString(filepath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.NoMutex | SQLiteOpenFlags.Create | SQLiteOpenFlags.PrivateCache, true, "leapso2ngshashtable");
            this.sqlConn = new SQLiteConnection(connectionStr)
            {
                Trace = false,
                TimeExecution = false
            };
        }

        public void Load()
        {
            if (Interlocked.CompareExchange(ref this.state, 1, 0) == 0)
            {
                this.sqlConn.EnableWriteAheadLogging();
                
                var versionTb = this.sqlConn.CreateTable<Versioning>();
                var oldRecordTb = this.sqlConn.CreateTable<PatchRecordItem>();
                this.sqlConn.CreateTable<PSO2ClientVersion>();

                if (versionTb == CreateTableResult.Created)
                {
                    this.sqlConn.Insert(new Versioning() { TableVersion = LatestVersion });
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
                        var verRecordTb = this.sqlConn.Find<Versioning>(Versioning_Str);
                        if (verRecordTb == null)
                        {
                            this.sqlConn.Insert(new Versioning() { TableVersion = LatestVersion });
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
                        this.sqlConn.InsertOrReplace(new Versioning() { TableVersion = LatestVersion });
                        this.sqlConn.DropTable<PatchRecordItem>();
                        this.sqlConn.CreateTable<PatchRecordItem>();
                    }
                }

                // this.FetchAllRecords();
                this.FetchAllRecordsValueType();
            }
        }

        public void SetPSO2ClientVersion(string version)
        {
            Interlocked.Exchange<string>(ref this._bufferedPSO2ClientVersion, version);
        }

        public string GetPSO2ClientVersion() => this.GetPSO2ClientVersion(false);

        public string GetPSO2ClientVersion(bool fromBuffering)
        {
            if (fromBuffering)
            {
                return this._bufferedPSO2ClientVersion;
            }
            else
            {
                bool isTableExist = false;
                foreach (var table in this.sqlConn.TableMappings)
                {
                    if (string.Equals(table.TableName, "PSO2ClientVersion", StringComparison.OrdinalIgnoreCase))
                    {
                        isTableExist = true;
                        break;
                    }
                }

                if (isTableExist)
                {
                    try
                    {
                        var obj = this.sqlConn.Get<PSO2ClientVersion>(Versioning_Str);
                        return obj.ClientVersion ?? string.Empty;
                    }
                    catch { }
                }

                return string.Empty;
            }
        }

        private void Upgrading(int fromVersion)
        {
            if (fromVersion < 1)
            {
                fromVersion = 1;
            }
            if (fromVersion == 1)
            {
                this.sqlConn.InsertOrReplace(new Versioning() { TableVersion = 2 });
            }
            if (fromVersion == LatestVersion)
            {
                return;
            }
        }

        public bool TryGetPatchItem(string filename, [CA.NotNullWhen(true)] out PatchRecordItemValue? item)
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

        public PatchRecordItemValue SetPatchItem(PatchListItem item, in DateTime lastModifiedTimeUTC)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            switch (Interlocked.CompareExchange(ref this.state, -1, -1))
            {
                case 0:
                    throw new InvalidOperationException("Please call Load() before using.");
                case 1:
                    // this.buffering.AddOrUpdate(item.RemoteFilename, )
                    // var obj = new PatchRecordItem() { RemoteFilename = string.Create(item.GetSpanFilenameWithoutAffix().Length, item, (c, obj) => obj.GetSpanFilenameWithoutAffix().ToLowerInvariant(c)), FileSize = item.FileSize, MD5 = item.MD5, LastModifiedTimeUTC = lastModifiedTimeUTC };
                    return this.buffering.AddOrUpdate(item.GetFilename(), (key, args) =>
                    {
                        var _item = args.Item1;
                        var result = new PatchRecordItemValue(string.Create(_item.GetSpanFilenameWithoutAffix().Length, _item, (c, obj) => obj.GetSpanFilenameWithoutAffix().ToLowerInvariant(c)), in _item.FileSize, new string(_item.MD5.Span), in args.Item2);
                        args.Item3.Add(result);
                        return result;
                    }, (key, existing, args) =>
                    {
                        var _item = args.Item1;
                        var str = string.Create(_item.GetSpanFilenameWithoutAffix().Length, _item, (c, obj) => obj.GetSpanFilenameWithoutAffix().ToLowerInvariant(c));
                        if (!PatchRecordItemValue.IsEquals(existing, str, new string(_item.MD5.Span), in _item.FileSize, in args.Item2))
                        {
                            var result = new PatchRecordItemValue(str, in _item.FileSize, new string(_item.MD5.Span), in args.Item2);
                            args.Item3.Add(result);
                            return result;
                        }
                        else
                        {
                            return existing;
                        }
                    }, new ValueTuple<PatchListItem, DateTime, BlockingCollection<PatchRecordItemValue>>(item, lastModifiedTimeUTC, this.writebuffer));
                case 2:
                    throw new ObjectDisposedException(nameof(FileCheckHashCache));
                default:
                    throw new InvalidOperationException();
            }
        }

        public void MakeReadOnly()
        {
            this.writebuffer.CompleteAdding();
        }

        public async ValueTask DisposeAsync()
        {
            var oldstate = Interlocked.Exchange(ref this.state, 2);
            if (oldstate != 2)
            {
                // this.writebuffer.CompleteAdding();
                this.MakeReadOnly();
                bool hasError = false;

                try
                {
                    await this.t_write;
                }
                catch (Exception)
                {
                    hasError = true;
                }
                finally
                {
                    if (this.sqlConn.IsInTransaction)
                    {
                        if (hasError)
                            this.sqlConn.Rollback();
                        else
                            this.sqlConn.Commit();
                    }
                    this.sqlConn.ExecuteScalar<string>("PRAGMA optimize;", Array.Empty<object>());

                    this.buffering.Clear();

                    this.sqlConn.Close();
                    this.sqlConn.Dispose();

                    this.writebuffer.Dispose();
                }
            }
        }

        public bool ContainsKey(string key) => this.buffering.ContainsKey(key);

        public bool TryGetValue(string key, [CA.NotNullWhen(true)] out PatchRecordItemValue? value) => this.buffering.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<string, PatchRecordItemValue>> GetEnumerator() => this.buffering.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.buffering.GetEnumerator();

        private async Task PollingWrite()
        {
            var singular = new PatchRecordItem(); // Re-use this object to insert.

            while (!this.writebuffer.IsCompleted)
            {
                if (this.writebuffer.TryTake(out var item))
                {
                    // Has to redeclare local var before use again as "ConfigureAwait(false)" discard machine state (which including the local vars before the switching thread context)
                    // The old code has "conn" getting "NullReferenceException" at "conn.BeginTransaction()".
                    var _conn = this.sqlConn;
                    _conn.BeginTransaction();
                    try
                    {
                        singular.FileSize = item.FileSize;
                        singular.RemoteFilename = item.RemoteFilename;
                        singular.MD5 = item.MD5;
                        singular.LastModifiedTimeUTC = item.LastModifiedTimeUTC;
                        _conn.InsertOrReplace(singular);
                        while (this.writebuffer.TryTake(out item))
                        {
                            singular.FileSize = item.FileSize;
                            singular.RemoteFilename = item.RemoteFilename;
                            singular.MD5 = item.MD5;
                            singular.LastModifiedTimeUTC = item.LastModifiedTimeUTC;
                            _conn.InsertOrReplace(singular);
                        }
                        _conn.Commit();
                    }
                    catch
                    {
                        _conn.Rollback();
                    }
                }
                else
                {
                    if (!this.writebuffer.IsAddingCompleted)
                    {
                        await Task.Delay(200).ConfigureAwait(false);
                    }
                }
            }

            // Has to redeclare local var before use again as "ConfigureAwait(false)" discard machine state (which including the local vars before the switching thread context)
            var conn = this.sqlConn;
            var versionstring = Interlocked.Exchange<string>(ref this._bufferedPSO2ClientVersion, string.Empty);
            conn.BeginTransaction();
            try
            {
                var result = conn.CreateTable<PSO2ClientVersion>();
                if (result == CreateTableResult.Created)
                {
                    conn.Insert(new PSO2ClientVersion() { ClientVersion = versionstring });
                }
                else
                {
                    conn.InsertOrReplace(new PSO2ClientVersion() { ClientVersion = versionstring });
                }
                conn.Commit();
            }
            catch
            {
                conn.Rollback();
            }
        }

        private void FetchAllRecordsValueType()
        {
            // Buffering data into memory.

            // Live on edge.
            // Using low-level. LET'S GO!!!
            var count = this.sqlConn.ExecuteScalar<int>("SELECT COUNT(*) FROM PatchRecordItem");
            if (count <= 0)
            {
                this.buffering = new ConcurrentDictionary<string, PatchRecordItemValue>(this.concurrentlevel, 331, StringComparer.InvariantCultureIgnoreCase);
            }
            else
            {
                this.buffering = new ConcurrentDictionary<string, PatchRecordItemValue>(this.concurrentlevel, count + 300, StringComparer.InvariantCultureIgnoreCase);
            }

            using (var statementHandle = SQLite3.Prepare2(this.sqlConn.Handle, "SELECT RemoteFilename,FileSize,MD5,LastModifiedTimeUTC FROM PatchRecordItem"))
            {
                SQLite3.Result r;
                r = SQLite3.Step(statementHandle);
                string remoteFilename, md5;
                long filesize, tickCount;
                while (r == SQLite3.Result.Row)
                {
                    remoteFilename = SQLite3.ColumnString(statementHandle, 0);
                    filesize = SQLite3.ColumnInt64(statementHandle, 1);
                    md5 = SQLite3.ColumnString(statementHandle, 2);
                    tickCount = SQLite3.ColumnInt64(statementHandle, 3);
                    this.buffering.TryAdd(remoteFilename, new PatchRecordItemValue(remoteFilename, in filesize, md5, new DateTime(tickCount)));
                    r = SQLite3.Step(statementHandle);
                }
                if (r != SQLite3.Result.Done)
                {
                    this.buffering.Clear();
                    throw SQLiteException.New(r, SQLite3.GetErrmsg(this.sqlConn.Handle));
                }    
            }

            // As of 18th Dec 2022...Information below may not be true anymore in the future.
            // So check it by "Go to definition" in Visual Studio.
            // statementHandle.manual_close()

            // We no longer need to call Finalize ourselves as the Statement class inherits SafeHandle, in which will call sqlite3_finalize in ReleaseHandle().
            // So putting it in using block will take care of it.
            // SQLite3.Finalize(statementHandle);
        }

        class Versioning
        {
            [PrimaryKey, Unique, NotNull, MaxLength(Versioning_Length)]
            public string TableName { get; set; } = Versioning_Str;
            [NotNull]
            public int TableVersion { get; set; }
        }

        class PSO2ClientVersion
        {
            [PrimaryKey, Unique, NotNull, MaxLength(Versioning_Length), Collation("NOCASE")]
            public string TableName { get; set; } = Versioning_Str;
            [NotNull, MaxLength(128), Collation("NOCASE")]
            public string ClientVersion { get; set; } = string.Empty;
        }

        class PatchRecordItem
        {
            [PrimaryKey, Unique, NotNull, MaxLength(2048), Collation("NOCASE")]
            public string RemoteFilename { get; set; } = string.Empty;
            [MaxLength(32)]
            public string MD5 { get; set; } = string.Empty;
            public long FileSize { get; set; }
            [NotNull]
            public DateTime LastModifiedTimeUTC { get; set; }
        }
    }
}

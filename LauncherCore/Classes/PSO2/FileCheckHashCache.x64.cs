using SQLite.X64;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    class FileCheckHashCacheX64 : FileCheckHashCache
    {
        private readonly SQLiteConnection sqlConn;

        public FileCheckHashCacheX64(string filepath, in int concurrentLevel) : base(in concurrentLevel)
        {
            var connectionStr = new SQLiteConnectionString(filepath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.Create | SQLiteOpenFlags.PrivateCache, true, "leapso2ngshashtable");
            this.sqlConn = new SQLiteConnection(connectionStr);
        }

        protected override async Task PollingWrite(BlockingCollection<PatchRecordItemValue> writebuffer)
        {
            // INSERT OR REPLACE INTO PatchRecordItem (RemoteFilename,FileSize,MD5,LastModifiedTimeUTC) VALUES (?,?,?,?)
            //var statementHandle = SQLite3.Prepare2(this.sqlConn.Handle, "INSERT OR REPLACE INTO PatchRecordItem(RemoteFilename,FileSize,MD5,LastModifiedTimeUTC) VALUES (?,?,?,?)");   
            var singular = new PatchRecordItem(); // Re-use this object to insert.
            try
            {
                while (!writebuffer.IsCompleted)
                {
                    if (writebuffer.TryTake(out var item))
                    {
                        this.sqlConn.BeginTransaction();
                        try
                        {
                            singular.FileSize = item.FileSize;
                            singular.RemoteFilename = item.RemoteFilename;
                            singular.MD5 = item.MD5;
                            singular.LastModifiedTimeUTC = item.LastModifiedTimeUTC;
                            this.sqlConn.InsertOrReplace(singular);
                            while (writebuffer.TryTake(out item))
                            {
                                singular.FileSize = item.FileSize;
                                singular.RemoteFilename = item.RemoteFilename;
                                singular.MD5 = item.MD5;
                                singular.LastModifiedTimeUTC = item.LastModifiedTimeUTC;
                                this.sqlConn.InsertOrReplace(singular);
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
                        if (!writebuffer.IsAddingCompleted)
                        {
                            await Task.Delay(200).ConfigureAwait(false);
                        }
                    }
                }
            }
            finally
            {
                // SQLite3.Finalize(statementHandle);
            }
        }

        protected override void OnLoad()
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
        }

        protected override void FetchAllRecordsValueType()
        {
            // Buffering data into memory.

            // Live on edge.
            // Using low-level. LET'S GO!!!
            var count = this.ExecuteScalar<int>("SELECT COUNT(*) FROM PatchRecordItem");
            if (count <= 0)
            {
                this.buffering = new ConcurrentDictionary<string, PatchRecordItemValue>(this.concurrentlevel, 331, StringComparer.InvariantCultureIgnoreCase);
            }
            else
            {
                this.buffering = new ConcurrentDictionary<string, PatchRecordItemValue>(this.concurrentlevel, count + 300, StringComparer.InvariantCultureIgnoreCase);
            }

            var statementHandle = SQLite3.Prepare2(this.sqlConn.Handle, "SELECT RemoteFilename,FileSize,MD5,LastModifiedTimeUTC FROM PatchRecordItem");
            try
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
            finally
            {
                SQLite3.Finalize(statementHandle);
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

        protected override void CloseConnection() => this.sqlConn.Close();

        protected override void DisposeConnection() => this.sqlConn.Dispose();

        protected override T ExecuteScalar<T>(string cmd, params object[] args) => this.sqlConn.ExecuteScalar<T>(cmd, args);

        class Versioning
        {
            // Not sure whether I should do unique along with Primary
            [PrimaryKey, Unique, NotNull, MaxLength(256)]
            public string TableName { get; set; }
            [NotNull]
            public int TableVersion { get; set; }
        }

        class PatchRecordItem
        {
            [PrimaryKey, Unique, NotNull, MaxLength(2048), Collation("NOCASE")]
            public string RemoteFilename { get; set; }
            [MaxLength(32)]
            public string MD5 { get; set; }
            public long FileSize { get; set; }
            [NotNull]
            public DateTime LastModifiedTimeUTC { get; set; }
        }
    }
}

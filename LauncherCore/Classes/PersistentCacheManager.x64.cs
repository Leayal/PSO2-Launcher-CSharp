using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SQLite.X64;

namespace Leayal.PSO2Launcher.Core.Classes
{
    class PersistentCacheManagerX64 : PersistentCacheManager
    {
        private readonly SQLiteConnection conn;
        // private readonly SQLiteCommand cmd;

        public PersistentCacheManagerX64(string rootDirectory) : base(rootDirectory, false)
        {
            this.conn = new SQLiteConnection(new SQLiteConnectionString(Path.Combine(rootDirectory, dbName), SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.Create | SQLiteOpenFlags.PrivateCache, true, "leapersistcachedir"));

            this.InitHeadersDatabase();
        }

        protected override void InitHeadersDatabase()
        {
            this.conn.CreateTable<CacheHeaderEntry>();

            base.InitHeadersDatabase();
        }

        protected override async Task<JsonDocument?> FetchCacheHeader(string entryName, CancellationToken cancellationToken)
        {
            using (var lockObj = await this.EnterDatabaseLock(cancellationToken))
            {
                if (this.conn.Find<CacheHeaderEntry>(entryName) is CacheHeaderEntry header)
                {
                    return JsonDocument.Parse(header.Data, new JsonDocumentOptions() { CommentHandling = JsonCommentHandling.Skip });
                }
            }
            return null;
        }

        protected override async Task WriteCacheHeader(string entryName, ReadOnlyMemory<byte> entryHeaderData, CancellationToken cancellationToken)
        {
            using (var lockObj = await this.EnterDatabaseLock(cancellationToken))
            {
                var savepoint = this.conn.SaveTransactionPoint();
                try
                {
                    this.conn.InsertOrReplace(new CacheHeaderEntry() { EntryName = entryName, Data = entryHeaderData.ToArray() });
                    this.conn.Release(savepoint);
                }
                catch
                {
                    this.conn.RollbackTo(savepoint);
                    throw;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.conn.Dispose();
        }

        class CacheHeaderEntry
        {
            [PrimaryKey, Unique, NotNull, MaxLength(2048), Collation("NOCASE")]
            public string EntryName { get; set; }

            [NotNull]
            public byte[] Data { get; set; }
        }
    }
}

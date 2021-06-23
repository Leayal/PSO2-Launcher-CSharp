using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using SQLite;
using System;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    class FileCheckHashCache : IAsyncDisposable
    {
        private const int LatestVersion = 1;

        private readonly SQLiteAsyncConnection sqlConn;

        public FileCheckHashCache(string filepath)
        {
            var connectionStr = new SQLiteConnectionString(filepath, SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.Create, true, "leapso2ngshashtable");
            this.sqlConn = new SQLiteAsyncConnection(connectionStr);
        }

        public async Task Load()
        {
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
                return await this.sqlConn.GetAsync<PatchRecordItem>(filename);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        public async Task<bool> SetPatchItem(PatchListItem item, DateTime lastModifiedTimeUTC)
        {
            var result = await this.sqlConn.InsertOrReplaceAsync(new PatchRecordItem() { RemoteFilename = item.GetFilenameWithoutAffix(), FileSize = item.FileSize, MD5 = item.MD5, LastModifiedTimeUTC = lastModifiedTimeUTC });
            return (result != 0);
        }

        public async ValueTask DisposeAsync()
        {
            await this.sqlConn.CloseAsync();
        }

        public class PatchRecordItem
        {
            [PrimaryKey, Unique, NotNull]
            public string RemoteFilename { get; set; }
            public string MD5 { get; set; }
            public long FileSize { get; set; }
            [NotNull]
            public DateTime LastModifiedTimeUTC { get; set; }
        }

        class Versioning
        {
            // Not sure whether I should do unique along with Primary
            [PrimaryKey, Unique, NotNull]
            public string TableName { get; set; }
            [NotNull]
            public int TableVersion { get; set; }
        }
    }
}

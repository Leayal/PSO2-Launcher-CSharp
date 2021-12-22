using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public partial class GameClientUpdater
    {
        public event ProgressReportHandler ProgressReport;
        private void OnProgressReport(int TaskID, PatchListItem currentFile, in long currentProgress)
            => this.ProgressReport?.Invoke(TaskID, currentFile, in currentProgress);

        public event ProgressBeginHandler ProgressBegin;
        private void OnProgressBegin(int TaskID, PatchListItem currentFile, in long totalProgress)
            => this.ProgressBegin?.Invoke(TaskID, currentFile, in totalProgress);

        public event ProgressEndHandler ProgressEnd;
        private void OnProgressEnd(int TaskID, PatchListItem currentFile, in bool isSuccess)
            => this.ProgressEnd?.Invoke(TaskID, currentFile, in isSuccess);

        public event OperationBeginHandler OperationBegin;
        private void OnOperationBegin(int concurrentlevel)
        => this.OperationBegin?.Invoke(this, concurrentlevel);

        public event OperationCompletedHandler OperationCompleted;
        //private void OnClientOperationComplete1(string dir_pso2bin, GameClientSelection downloadMode, IReadOnlyCollection<PatchListItem> patchlist, IReadOnlyCollection<PatchListItem> needtodownload, IReadOnlyCollection<PatchListItem> successlist, IReadOnlyCollection<PatchListItem> failurelist, in PSO2Version ver, bool noError, CancellationToken cancellationToken)
        private void OnClientOperationComplete1(string dir_pso2bin, string? pso2tweaker_dirpath, GameClientSelection downloadMode, IReadOnlyCollection<PatchListItem> patchlist, IReadOnlyDictionary<PatchListItem, bool?> results, in PSO2Version ver, bool noError, CancellationToken cancellationToken)
        {
            // Everything is completed.
            // Write the version file out.

            try
            {
                if (noError && !cancellationToken.IsCancellationRequested && downloadMode != GameClientSelection.Always_Only && ver != default(PSO2Version))
                {
                    var localFilePath = Path.GetFullPath("version.ver", dir_pso2bin);
                    if (Directory.Exists(localFilePath))
                    {
                        Directory.Delete(localFilePath);
                    }
                    else if (File.Exists(localFilePath))
                    {
                        var attr = File.GetAttributes(localFilePath);
                        if (attr.HasFlag(FileAttributes.ReadOnly))
                        {
                            File.SetAttributes(localFilePath, attr & ~FileAttributes.ReadOnly);
                        }
                    }
                    File.WriteAllText(localFilePath, ver.ToString());

                    localFilePath = Path.GetFullPath(Path.Combine("SEGA", "PHANTASYSTARONLINE2", "_version.ver"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                    if (Directory.Exists(localFilePath))
                    {
                        Directory.Delete(localFilePath);
                    }
                    else if (File.Exists(localFilePath))
                    {
                        var attr = File.GetAttributes(localFilePath);
                        if (attr.HasFlag(FileAttributes.ReadOnly))
                        {
                            File.SetAttributes(localFilePath, attr & ~FileAttributes.ReadOnly);
                        }
                    }
                    File.WriteAllText(localFilePath, ver.ToString());

                    if (!string.IsNullOrWhiteSpace(pso2tweaker_dirpath))
                    {
                        var data_clientjson = Path.Combine(pso2tweaker_dirpath, "client.json");
                        if (File.Exists(data_clientjson))
                        {
                            var lookup = new Dictionary<ReadOnlyMemory<char>, PatchListItem>(results.Count, OrdinalIgnoreCaseMemoryStringComparer.Default);
                            foreach (var item in results)
                            {
                                lookup.Add(item.Key.GetMemoryFilenameWithoutAffix(), item.Key);
                            }
                            var newone = data_clientjson + "new";
                            using (var fs_data_clientjson = File.OpenRead(data_clientjson))
                            using (var doc = System.Text.Json.JsonDocument.Parse(fs_data_clientjson, new System.Text.Json.JsonDocumentOptions() { AllowTrailingCommas = true, CommentHandling = System.Text.Json.JsonCommentHandling.Skip }))
                            using (var fs_data_clientjson_new = File.Create(data_clientjson + "new"))
                            using (var writer = new System.Text.Json.Utf8JsonWriter(fs_data_clientjson_new, new System.Text.Json.JsonWriterOptions() { Indented = true }))
                            {
                                writer.WriteStartObject();
                                foreach (var prop in doc.RootElement.EnumerateObject())
                                {
                                    var mem = prop.Name.AsMemory();
                                    if (lookup.Remove(mem, out var item) && results.TryGetValue(item, out var isSuccess) && isSuccess == true)
                                    {
                                        writer.WriteString(mem.Span, item.MD5);
                                    }
                                    else
                                    {
                                        writer.WritePropertyName(mem.Span);
                                        prop.Value.WriteTo(writer);
                                    }
                                }

                                foreach (var prop in lookup)
                                {
                                    writer.WriteString(prop.Key.Span, prop.Value.MD5);
                                }

                                writer.WriteEndObject();
                                writer.Flush();
                            }
                            var attr = File.GetAttributes(data_clientjson);
                            if (attr.HasFlag(FileAttributes.ReadOnly))
                            {
                                File.SetAttributes(data_clientjson, attr & ~FileAttributes.ReadOnly);
                            }
                            File.SetAttributes(newone, attr);
                            File.Move(newone, data_clientjson, true);
                        }

                        var data_filestxt = Path.Combine(pso2tweaker_dirpath, "data_files.txt");
                        if (File.Exists(data_filestxt))
                        {
                            var attr = File.GetAttributes(data_filestxt);
                            if (attr.HasFlag(FileAttributes.ReadOnly))
                            {
                                File.SetAttributes(data_filestxt, attr & ~FileAttributes.ReadOnly);
                            }
                            using (var fs = File.Create(data_filestxt))
                            using (var sw = new StreamWriter(fs, new System.Text.UTF8Encoding(false)))
                            {
                                foreach (var item in patchlist)
                                {
                                    sw.WriteLine(Path.GetFullPath(item.GetFilenameWithoutAffix(), dir_pso2bin));
                                }
                                sw.Flush();
                            }
                            
                            if (attr.HasFlag(FileAttributes.ReadOnly))
                            {
                                File.SetAttributes(data_filestxt, attr);
                            }
                        }

                        var pso2tweakerconfig = new PSO2TweakerConfig();
                        if (pso2tweakerconfig.Load())
                        {
                            pso2tweakerconfig.ResetFanPatchVersion();
                            pso2tweakerconfig.Save();
                        }
                        // LatestWin32RebootFanPatchVersion
                    }
                }
            }
            finally
            {
                this.OperationCompleted?.Invoke(this, cancellationToken.IsCancellationRequested, patchlist, results);
            }
        }

        public event BackupFileFoundHandler BackupFileFound;
        private Task OnBackupFileFound(BackupFileFoundEventArgs e)
        {
            var callback = this.BackupFileFound;
            if (callback == null)
            {
                return Task.CompletedTask;
            }
            else
            {
                return callback.Invoke(this, e);
            }
        }

        public event FileCheckBeginHandler FileCheckBegin;
        private void OnFileCheckBegin(in int total) => this.FileCheckBegin?.Invoke(this, total);

        public event FileCheckReportHandler FileCheckReport;
        private void OnFileCheckReport(in int current) => this.FileCheckReport?.Invoke(this, in current);

        public event DownloadQueueAddedHandler DownloadQueueAdded;
        private void OnDownloadQueueAdded() => this.DownloadQueueAdded?.Invoke(this);

        public event FileCheckEndHandler FileCheckEnd;
        private void OnFileCheckEnd() => this.FileCheckEnd?.Invoke(this);

        public delegate void ProgressBeginHandler(int ConcurrentId, PatchListItem file, in long totalProgressValue);
        public delegate void ProgressReportHandler(int ConcurrentId, PatchListItem file, in long currentProgressValue);
        public delegate void ProgressEndHandler(int ConcurrentId, PatchListItem file, in bool isSuccess);

        public delegate void FileCheckEndHandler(GameClientUpdater sender);
        public delegate void FileCheckBeginHandler(GameClientUpdater sender, int total);
        public delegate void DownloadQueueAddedHandler(GameClientUpdater sender);
        public delegate void FileCheckReportHandler(GameClientUpdater sender, in int current);
        // public delegate void OperationCompletedHandler(GameClientUpdater sender, bool isCancelled, IReadOnlyCollection<PatchListItem> patchlist, IReadOnlyCollection<PatchListItem> download_required_list, IReadOnlyCollection<PatchListItem> successList, IReadOnlyCollection<PatchListItem> failureList);
        public delegate void OperationCompletedHandler(GameClientUpdater sender, bool isCancelled, IReadOnlyCollection<PatchListItem> patchlist, IReadOnlyDictionary<PatchListItem, bool?> download_result_list);
        public delegate void OperationBeginHandler(GameClientUpdater sender, int concurrentlevel);
        public delegate Task BackupFileFoundHandler(GameClientUpdater sender, BackupFileFoundEventArgs e);

        private delegate void DownloadFinishCallback(in DownloadItem item, in bool success);
        private delegate void InnerDownloadQueueAddCallback(in DownloadItem item);
            
        public class BackupFileFoundEventArgs : EventArgs
        {
            public readonly string Root;
            private IEnumerable<BackupRestoreItem> walking;
            private readonly bool doesReboot, doesClassic;

            public bool HasRebootBackup => this.doesReboot;
            public bool HasClassicBackup => this.doesClassic;

            /// <summary>
            /// <para>True to tell the <seealso cref="GameClientUpdater"/> that you have handled the backup restoring progress.</para>
            /// <para>False to let the <seealso cref="GameClientUpdater"/> handle the backup restoring progress.</para>
            /// <para>Null ignore the backup files.</para>
            /// </summary>
            /// <remarks>
            /// If true, you should handle your backup restoring within a background Task to avoid blocking UI Thread.
            /// </remarks>
            public bool? Handled { get; set; }

            public IEnumerable<BackupRestoreItem> Items
            {
                get
                {
                    if (this.walking == null)
                    {
                        this.walking = this.Walk();
                    }
                    return this.walking;
                }
            }

            private IEnumerable<BackupRestoreItem> Walk()
            {
                string currentDir, bakDir;
                if (this.doesReboot)
                {
                    currentDir = Path.GetFullPath(Path.Combine("data", "win32reboot"), this.Root);
                    bakDir = Path.Combine(currentDir, "backup");
                    foreach (var file in Directory.EnumerateFiles(bakDir, "*", SearchOption.AllDirectories))
                    {
                        var relativePath = Path.GetRelativePath(bakDir, file);
                        yield return new BackupRestoreItem(file, relativePath, Path.GetFullPath(relativePath, currentDir));
                    }
                }

                if (this.doesClassic)
                {
                    currentDir = Path.GetFullPath(Path.Combine("data", "win32"), this.Root);
                    bakDir = Path.Combine(currentDir, "backup");
                    foreach (var file in Directory.EnumerateFiles(bakDir, "*", SearchOption.TopDirectoryOnly))
                    {
                        var relativePath = Path.GetRelativePath(bakDir, file);
                        yield return new BackupRestoreItem(file, relativePath, Path.GetFullPath(relativePath, currentDir));
                    }
                }
            }

            public BackupFileFoundEventArgs(string pso2_bin, bool reboot, bool classic)
            {
                this.Handled = false;
                this.Root = pso2_bin;
                this.doesReboot = reboot;
                this.doesClassic = classic;
            }
        }

        public readonly struct BackupRestoreItem
        {
            public readonly string RelativePath;
            public readonly string BackupFileDestination;
            public readonly string BackupFileSourcePath;

            public BackupRestoreItem(string sourcePath, string relativePath, string to)
            {
                this.BackupFileSourcePath = sourcePath;
                this.RelativePath = relativePath;
                this.BackupFileDestination = to;
            }
        }

        class DownloadItem
        {
            public readonly PatchListItem PatchInfo;
            public readonly string Destination;
#nullable enable
            public readonly string? SymlinkTo;

            public DownloadItem(PatchListItem info, string dest, string? linkTo)
            {
                this.PatchInfo = info;
                this.Destination = dest;
                this.SymlinkTo = linkTo;
            }
#nullable restore
        }
    }
}

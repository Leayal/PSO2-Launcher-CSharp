using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.PSO2.UserConfig;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Leayal.Shared;

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
        private void OnOperationBegin(int concurrentlevel) => this.OperationBegin?.Invoke(this, concurrentlevel);

        public event OperationCompletedHandler OperationCompleted;
        //private void OnClientOperationComplete1(string dir_pso2bin, GameClientSelection downloadMode, IReadOnlyCollection<PatchListItem> patchlist, IReadOnlyCollection<PatchListItem> needtodownload, IReadOnlyCollection<PatchListItem> successlist, IReadOnlyCollection<PatchListItem> failurelist, in PSO2Version ver, bool noError, CancellationToken cancellationToken)
        private void OnClientOperationComplete1(string dir_pso2bin, string? pso2tweaker_dirpath, PSO2TweakerHashCache? tweakerhashcacheDump, GameClientSelection downloadMode, IReadOnlyCollection<PatchListItem> patchlist, IReadOnlyDictionary<PatchListItem, bool?> results, in PSO2Version ver, bool noError, CancellationToken cancellationToken)
        {
            // Everything is completed.
            // Write the version file out.

            try
            {
                if (noError && !cancellationToken.IsCancellationRequested && downloadMode != GameClientSelection.Always_Only && ver != default(PSO2Version))
                {
                    var versionStringRaw = ver.ToString();
                    var localFilePath = Path.GetFullPath("version.ver", dir_pso2bin);
                    if (Directory.Exists(localFilePath))
                    {
                        Directory.Delete(localFilePath);
                    }
                    else if (File.Exists(localFilePath))
                    {
                        var attr = File.GetAttributes(localFilePath);
                        if (attr.HasReadOnlyFlag())
                        {
                            File.SetAttributes(localFilePath, attr & ~FileAttributes.ReadOnly);
                        }
                    }
                    File.WriteAllText(localFilePath, versionStringRaw);

                    var pso2conf_dir = Path.GetFullPath(Path.Combine("SEGA", "PHANTASYSTARONLINE2"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                    var path_pso2conf = Path.GetFullPath("user.pso2", pso2conf_dir);
                    UserConfig conf;

                    static bool ReadjustPSO2UserConfig(UserConfig conf, GameClientSelection downloadMode) => (Windows.PSO2DeploymentWindow.AdjustPSO2UserConfig(conf, downloadMode) || AdjustPSO2UserConfig_FirstDownloadCheck(conf, downloadMode switch
                    {
                        GameClientSelection.NGS_Only => true,
                        GameClientSelection.NGS_AND_CLASSIC => true,
                        _ => false
                    }));

                    if (File.Exists(path_pso2conf))
                    {
                        conf = UserConfig.FromFile(path_pso2conf);
                        if (ReadjustPSO2UserConfig(conf, downloadMode))
                        {
                            conf.SaveAs(path_pso2conf);
                        }
                    }
                    else
                    {
                        conf = new UserConfig("Ini");
                        if (ReadjustPSO2UserConfig(conf, downloadMode))
                        {
                            if (!Directory.Exists(pso2conf_dir)) // Should be safe for symlink 
                            {
                                Directory.CreateDirectory(pso2conf_dir);
                            }
                            conf.SaveAs(path_pso2conf);
                        }
                    }

                    localFilePath = Path.GetFullPath("_version.ver", pso2conf_dir);
                    if (File.Exists(localFilePath))
                    {
                        var attr = File.GetAttributes(localFilePath);
                        if (attr.HasReadOnlyFlag())
                        {
                            File.SetAttributes(localFilePath, attr & ~FileAttributes.ReadOnly);
                        }
                        File.WriteAllText(localFilePath, versionStringRaw);
                    }
                    
                    if (!string.IsNullOrWhiteSpace(pso2tweaker_dirpath))
                    {
                        var pso2tweakerconfig = new PSO2TweakerConfig();
                        if (pso2tweakerconfig.Load())
                        {
                            var pso2tweakerconfig_pso2bin = pso2tweakerconfig.PSO2JPBinFolder;
                            if (!string.IsNullOrWhiteSpace(pso2tweakerconfig_pso2bin))
                            {
                                pso2tweakerconfig_pso2bin = Path.GetFullPath(pso2tweakerconfig_pso2bin);

                                // Check whether Tweaker is targeting the same pso2_bin this launcher is managing.
                                if (string.Equals(pso2tweakerconfig_pso2bin, dir_pso2bin, StringComparison.OrdinalIgnoreCase))
                                {
                                    pso2tweakerconfig.ResetFanPatchVersion();
                                    pso2tweakerconfig.PSO2JPRemoteVersion = versionStringRaw;

                                    if (tweakerhashcacheDump != null)
                                    {
                                        var lookup = new Dictionary<ReadOnlyMemory<char>, PatchListItem>(results.Count, OrdinalIgnoreCaseMemoryStringComparer.Default);
                                        foreach (var item in results)
                                        {
                                            if (item.Value == true)
                                            {
                                                tweakerhashcacheDump.WriteString(item.Key.GetSpanFilenameWithoutAffix(), item.Key.MD5);
                                            }
                                        }

                                        var attr = File.Exists(tweakerhashcacheDump.CachePath) ? File.GetAttributes(tweakerhashcacheDump.CachePath) : FileAttributes.Normal;
                                        if (attr.HasReadOnlyFlag())
                                        {
                                            File.SetAttributes(tweakerhashcacheDump.CachePath, attr & ~FileAttributes.ReadOnly);
                                        }
                                        tweakerhashcacheDump.Save();
                                        if (attr != FileAttributes.Normal)
                                        {
                                            File.SetAttributes(tweakerhashcacheDump.CachePath, attr);
                                        }
                                    }

                                    var data_filestxt = Path.Combine(pso2tweaker_dirpath, "data_files.txt");
                                    if (File.Exists(data_filestxt))
                                    {
                                        var attr = File.GetAttributes(data_filestxt);
                                        if (attr.HasReadOnlyFlag())
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

                                        if (attr.HasReadOnlyFlag())
                                        {
                                            File.SetAttributes(data_filestxt, attr);
                                        }
                                    }
                                    else
                                    {
                                        using (var fs = File.Create(data_filestxt))
                                        using (var sw = new StreamWriter(fs, new System.Text.UTF8Encoding(false)))
                                        {
                                            foreach (var item in patchlist)
                                            {
                                                sw.WriteLine(Path.GetFullPath(item.GetFilenameWithoutAffix(), dir_pso2bin));
                                            }
                                            sw.Flush();
                                        }
                                    }

                                    pso2tweakerconfig.Save();
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                this.OperationCompleted?.Invoke(this, dir_pso2bin, cancellationToken.IsCancellationRequested, patchlist, results);
            }
        }

        private static bool AdjustPSO2UserConfig_FirstDownloadCheck(UserConfig conf, bool value)
        {
            if (value)
            {
                if (!conf.TryGetProperty("FirstDownloadCheck", out var val_FirstDownloadCheck) || val_FirstDownloadCheck is not bool val || val != true)
                {
                    conf["FirstDownloadCheck"] = true;
                    return true;
                }
            }
            return false;
        }

        public event BackupFileFoundHandler BackupFileFound;
        private async Task OnBackupFileFound(BackupFileFoundEventArgs e)
        {
            var callback = this.BackupFileFound;
            if (callback != null)
            {
                var invokeList = callback.GetInvocationList();
                for (int i = 0; i < invokeList.Length; i++)
                {
                    if (invokeList[i] is BackupFileFoundHandler handler)
                    {
                        try
                        {
                            await handler.Invoke(this, e);
                        }
                        catch { }
                    }
                }
                // return callback.Invoke(this, e);
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
        public delegate void OperationCompletedHandler(GameClientUpdater sender, string pso2dir, bool isCancelled, IReadOnlyCollection<PatchListItem> patchlist, IReadOnlyDictionary<PatchListItem, bool?> download_result_list);
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
                int offset;
                if (this.doesReboot)
                {
                    currentDir = Path.GetFullPath(Path.Combine("data", "win32reboot"), this.Root);
                    bakDir = Path.Combine(currentDir, "backup");
                    offset = bakDir.Length + 1;
                    foreach (var file in Directory.EnumerateFiles(bakDir, "*", SearchOption.AllDirectories))
                    {
                        var relativePath = Path.GetRelativePath(bakDir, file);
                        yield return new BackupRestoreItem(file, file.AsMemory(offset), Path.GetFullPath(relativePath, currentDir));
                    }
                }

                if (this.doesClassic)
                {
                    currentDir = Path.GetFullPath(Path.Combine("data", "win32"), this.Root);
                    bakDir = Path.Combine(currentDir, "backup");
                    offset = bakDir.Length + 1;
                    foreach (var file in Directory.EnumerateFiles(bakDir, "*", SearchOption.TopDirectoryOnly))
                    {
                        var relativePath = Path.GetRelativePath(bakDir, file);
                        yield return new BackupRestoreItem(file, file.AsMemory(offset), Path.GetFullPath(relativePath, currentDir));
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

        public readonly struct BackupRestoreItem : IEquatable<BackupRestoreItem>
        {
            public readonly ReadOnlyMemory<char> RelativePath;
            public readonly string BackupFileSourcePath, BackupFileDestination;

            public BackupRestoreItem(string sourcePath, ReadOnlyMemory<char> relativePath, string to)
            {
                this.BackupFileSourcePath = sourcePath;
                this.RelativePath = relativePath;
                this.BackupFileDestination = to;
            }

            public override bool Equals([NotNullWhen(true)] object obj) => (obj is BackupRestoreItem item && this.Equals(item));

            public bool Equals(BackupRestoreItem other)
                => string.Equals(this.BackupFileSourcePath, other.BackupFileSourcePath, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

            public override int GetHashCode() => HashCode.Combine(this.RelativePath, this.BackupFileSourcePath);

            public override string ToString() => $"BackupRestoreItem: {this.RelativePath}";
        }

        sealed class DownloadItem : IEquatable<DownloadItem>
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

            public bool Equals(DownloadItem? other) => (other != null && other.PatchInfo.Equals(this.PatchInfo));

            public override bool Equals(object? obj) => (obj is DownloadItem item && this.Equals(item));

            public override int GetHashCode() => this.PatchInfo.GetHashCode();
#nullable restore
        }
    }
}

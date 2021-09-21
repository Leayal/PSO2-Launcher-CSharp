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
        private void OnProgressReport(PatchListItem currentFile, in long currentProgress)
            => this.ProgressReport?.Invoke(currentFile, in currentProgress);

        public event ProgressBeginHandler ProgressBegin;
        private void OnProgressBegin(PatchListItem currentFile, in long totalProgress)
            => this.ProgressBegin?.Invoke(currentFile, in totalProgress);

        public event ProgressEndHandler ProgressEnd;
        private void OnProgressEnd(PatchListItem currentFile, in bool isSuccess)
            => this.ProgressEnd?.Invoke(currentFile, in isSuccess);

        public event OperationCompletedHandler OperationCompleted;
        //private void OnClientOperationComplete1(string dir_pso2bin, GameClientSelection downloadMode, IReadOnlyCollection<PatchListItem> patchlist, IReadOnlyCollection<PatchListItem> needtodownload, IReadOnlyCollection<PatchListItem> successlist, IReadOnlyCollection<PatchListItem> failurelist, in PSO2Version ver, bool noError, CancellationToken cancellationToken)
        private void OnClientOperationComplete1(string dir_pso2bin, GameClientSelection downloadMode, IReadOnlyCollection<PatchListItem> patchlist, IReadOnlyDictionary<PatchListItem, bool?> results, in PSO2Version ver, bool noError, CancellationToken cancellationToken)
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
                    File.WriteAllText(localFilePath, ver.ToString());
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

        public delegate void ProgressBeginHandler(PatchListItem file, in long totalProgressValue);
        public delegate void ProgressReportHandler(PatchListItem file, in long currentProgressValue);
        public delegate void ProgressEndHandler(PatchListItem file, in bool isSuccess);

        public delegate void FileCheckEndHandler(GameClientUpdater sender);
        public delegate void FileCheckBeginHandler(GameClientUpdater sender, int total);
        public delegate void DownloadQueueAddedHandler(GameClientUpdater sender);
        public delegate void FileCheckReportHandler(GameClientUpdater sender, in int current);
        // public delegate void OperationCompletedHandler(GameClientUpdater sender, bool isCancelled, IReadOnlyCollection<PatchListItem> patchlist, IReadOnlyCollection<PatchListItem> download_required_list, IReadOnlyCollection<PatchListItem> successList, IReadOnlyCollection<PatchListItem> failureList);
        public delegate void OperationCompletedHandler(GameClientUpdater sender, bool isCancelled, IReadOnlyCollection<PatchListItem> patchlist, IReadOnlyDictionary<PatchListItem, bool?> download_result_list);
        public delegate Task BackupFileFoundHandler(GameClientUpdater sender, BackupFileFoundEventArgs e);

        private delegate void DownloadFinishCallback(in DownloadItem item, in bool success);
        private delegate void InnerDownloadQueueAddCallback(in DownloadItem item);
            
        public class BackupFileFoundEventArgs : EventArgs
        {
            private readonly string root;
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
                    currentDir = Path.GetFullPath(Path.Combine("data", "win32reboot"), this.root);
                    bakDir = Path.Combine(currentDir, "backup");
                    foreach (var file in Directory.EnumerateFiles(bakDir, "*", SearchOption.AllDirectories))
                    {
                        var relativePath = Path.GetRelativePath(bakDir, file);
                        yield return new BackupRestoreItem(file, relativePath, Path.GetFullPath(relativePath, currentDir));
                    }
                }

                if (this.doesClassic)
                {
                    currentDir = Path.GetFullPath(Path.Combine("data", "win32"), this.root);
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
                this.root = pso2_bin;
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
            public readonly string? SymlinkTo;

            public DownloadItem(PatchListItem info, string dest, string? linkTo)
            {
                this.PatchInfo = info;
                this.Destination = dest;
                this.SymlinkTo = linkTo;
            }
        }
    }
}

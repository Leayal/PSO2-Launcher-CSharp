using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.PSO2Launcher.Helper;
using Leayal.SharedInterfaces;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public partial class GameClientUpdater
    {
        // Task Thread: File checking
        // Task Thread(s): File download
        // Both or all must be working at the same time.
        // Progressive file hash cache. Which should reduce the risk of progress loss when application crash or computer shutdown due to any reasons (black out, BSOD).
        // Which also means the cancellation should follow along.

        private static readonly string Name_PatchRootInfo = "management_beta.txt";

        // private readonly string dir_pso2bin;
        // private readonly string? dir_classic_data, dir_reboot_data;
        private readonly PSO2HttpClient webclient;

        // Cache purposes
        private readonly ObjectShortCacheManager<object> lastKnownObjects;

        // File check
        private Task t_operation;

        // Threadings
        private int flag_operationStarted;

        // public string Path_PSO2BIN => this.dir_pso2bin;
        // public string? Path_PSO2ClassicData => this.dir_classic_data;
        // public string? Path_PSO2RebootData => this.dir_reboot_data;

        public int ConcurrentDownloadCount { get; set; }
        public int ThrottleFileCheckFactor { get; set; }

        // Snail mode (for when internet is extremely unreliable).
        public bool SnailMode { get; set; }

        public bool IsBusy => (Interlocked.CompareExchange(ref this.flag_operationStarted, -1, -1) != 0);

        public GameClientUpdater(PSO2HttpClient httpHandler)
        {
            this.t_operation = Task.CompletedTask;
            this.ConcurrentDownloadCount = 0;
            this.ThrottleFileCheckFactor = 0;
            // this.dir_pso2bin = Path.GetFullPath(whereIsThePSO2_BIN);
            // this.dir_classic_data = string.IsNullOrWhiteSpace(preference_classicWhere) ? null : Path.GetFullPath(preference_classicWhere);
            // this.dir_reboot_data = string.IsNullOrWhiteSpace(preference_rebootWhere) ? null : Path.GetFullPath(preference_rebootWhere);
            this.lastKnownObjects = new ObjectShortCacheManager<object>();
            this.webclient = httpHandler;
            this.SnailMode = false;
        }

        private Task<PatchRootInfo> InnerGetPatchRootAsync(CancellationToken cancellationToken)
            => this.InnerGetPatchRootAsync(false, cancellationToken);

        // Provide memory cache for the 'management_beta.txt' file for 30s.
        // Avoid requesting the file again and again within the time frame.
        // Force refresh if it's important to do so.
        private async Task<PatchRootInfo> InnerGetPatchRootAsync(bool forceRefresh, CancellationToken cancellationToken)
        {
            var obj = await this.lastKnownObjects.GetOrAdd(Name_PatchRootInfo, async () =>
            {
                return await this.webclient.GetPatchRootInfoAsync(cancellationToken);
            });

            return (PatchRootInfo)obj;
        }

        public Task<PSO2Version> GetRemoteVersionAsync(CancellationToken cancellationToken)
            => this.GetRemoteVersionAsync(null, cancellationToken);

        public async Task<PSO2Version> GetRemoteVersionAsync(PatchRootInfo? patchInfoRoot, CancellationToken cancellationToken)
        {
            if (patchInfoRoot == null)
            {
                patchInfoRoot = await this.InnerGetPatchRootAsync(false, cancellationToken);
            }
            return await this.webclient.GetPatchVersionAsync(patchInfoRoot, cancellationToken);
        }

        public async Task<bool> CheckForPSO2Updates(string dir_pso2bin, CancellationToken cancellationToken)
            => await this.CheckForPSO2Updates(dir_pso2bin, null, cancellationToken);

        public string GetLocalPSO2Version(string dir_pso2bin)
        {
            var versionFilePath = Path.GetFullPath("version.ver", dir_pso2bin);
            if (File.Exists(versionFilePath))
            {
                return QuickFile.ReadFirstLine(versionFilePath) ?? string.Empty;
            }
            else
            {
                return string.Empty;
            }
        }

        public async Task<bool> CheckForPSO2Updates(string dir_pso2bin, PSO2Version? remoteVer, CancellationToken cancellationToken)
        {
            string verString = this.GetLocalPSO2Version(dir_pso2bin);
            PSO2Version remoteVersion;
            if (remoteVer.HasValue)
            {
                remoteVersion = remoteVer.Value;
            }
            else
            {
                var patchInfoRoot = await this.InnerGetPatchRootAsync(true, cancellationToken); // Force refresh because we are CHECKING for newer version.
                remoteVersion = await this.GetRemoteVersionAsync(patchInfoRoot, cancellationToken);
            }

            if (!PSO2Version.TryParse(verString, out var localPSO2Ver) || localPSO2Ver != remoteVersion)
            {
                return true;
            }

            return false;
        }

#nullable enable
        public async Task ScanAndDownloadFilesAsync(string dir_pso2bin, GameClientSelection selection, FileScanFlags fScanReboot, FileScanFlags fScanClassic, bool fShouldScanForBackup, CancellationToken cancellationToken)
            => await this.ScanAndDownloadFilesAsync(dir_pso2bin, null, null, selection, fScanReboot, fScanClassic, fShouldScanForBackup, cancellationToken);

        public async Task ScanAndDownloadFilesAsync(string dir_pso2bin, string? pso2tweaker_binpath, GameClientSelection selection, FileScanFlags fScanReboot, FileScanFlags fScanClassic, bool fShouldScanForBackup, CancellationToken cancellationToken)
            => await this.ScanAndDownloadFilesAsync(dir_pso2bin, null, pso2tweaker_binpath, selection, fScanReboot, fScanClassic, fShouldScanForBackup, cancellationToken);

        public async Task ScanAndDownloadFilesAsync(string dir_pso2bin, string? dir_classic_data, string? pso2tweaker_dirpath, GameClientSelection selection, FileScanFlags fScanReboot, FileScanFlags fScanClassic, bool fShouldScanForBackup, CancellationToken cancellationToken)
        {
            if (fScanReboot == FileScanFlags.None)
            {
                throw new ArgumentOutOfRangeException(nameof(fScanReboot));
            }

            if (Interlocked.CompareExchange(ref this.flag_operationStarted, 1, 0) == 0)
            {
                this.t_operation = Task.Factory.StartNew(async () =>
                {
                    bool isOperationSuccess = false;
                    FileCheckHashCache? duhB = null;
                    PSO2Version ver = default;

                    PatchListMemory? patchlist = null;

                    ConcurrentDictionary<PatchListItem, bool?>? resultsOfDownloads = null;
                    // ConcurrentBag<PatchListItem> bag_needtodownload = new ConcurrentBag<PatchListItem>(), bag_success = new ConcurrentBag<PatchListItem>(), bag_failure = new ConcurrentBag<PatchListItem>();
                    var pendingFiles = new BlockingCollection<DownloadItem>();
                    PSO2TweakerHashCache? tweakerhashcacheDump = null;
                    try
                    {
                        var taskCount = this.ConcurrentDownloadCount;
                        if (taskCount == 0)
                        {
                            taskCount = RuntimeValues.GetProcessorCountAuto();
                        }

                        this.OnOperationBegin(taskCount);

                        var t_patchlist = this.InnerGetFilelistToScan(selection, cancellationToken);

                        // In case the folder is deleted after accepting the prompt but before the SQLite3 file is created (below).
                        // Or in case the directory doesn't existed in the first place.
                        // Create it to allow creating the SQLite3. Otherwise, "A part of the path to file doesn't exist" error may happen.
                        Directory.CreateDirectory(dir_pso2bin);

                        duhB = new FileCheckHashCache(Path.GetFullPath("leapso2launcher.CheckCache.dat", dir_pso2bin), taskCount + 1);
                        duhB.Load();

                        if (fShouldScanForBackup)
                        {
                            var ev_backup = await SearchForBackup(dir_pso2bin, selection, t_patchlist);
                            if (ev_backup != null)
                            {
                                var handled = ev_backup.Handled;
                                if (handled == false)
                                {
                                    var numOfFileRestored = RestoreBackups(ev_backup, in cancellationToken);
                                    this.BackupFileRestoreComplete?.Invoke(this, ev_backup, numOfFileRestored);
                                }
                                else if (handled == true)
                                {
                                    this.BackupFileRestoreComplete?.Invoke(this, ev_backup, -2);
                                }
                                else
                                {
                                    this.BackupFileRestoreComplete?.Invoke(this, ev_backup, -1);
                                }
                            }
                            else
                            {
                                this.BackupFileRestoreComplete?.Invoke(this, null, -1);
                            }
                        }

                        patchlist = await t_patchlist;

                        resultsOfDownloads = new ConcurrentDictionary<PatchListItem, bool?>(taskCount, patchlist.Count);
                        ver = await GetRemoteVersionAsync(patchlist.RootInfo, cancellationToken);
                        if (!string.IsNullOrWhiteSpace(pso2tweaker_dirpath) && Directory.Exists(pso2tweaker_dirpath))
                        {
                            var tweakerCachePath = Path.Combine(pso2tweaker_dirpath, "client.json");
                            tweakerhashcacheDump = new PSO2TweakerHashCache(tweakerCachePath);
                            tweakerhashcacheDump.Load();
                        }

                        if (patchlist.CanCount)
                        {
                            var itemCount = patchlist.Count;
                            if (itemCount == 0)
                            {
                                throw new EmptyPatchListException();
                            }
                            else
                            {
                                this.OnFileCheckBegin(itemCount);
                            }
                        }
                        else
                        {
                            this.OnFileCheckBegin(-1);
                        }

                        var scannerObj = new UglyWrapper_Obj_Scanner(this, pendingFiles, duhB, resultsOfDownloads, cancellationToken, tweakerhashcacheDump, dir_pso2bin, dir_classic_data, selection, fScanReboot, fScanClassic, patchlist);
                        var t_check = Task.Factory.StartNew(scannerObj.Work, cancellationToken, TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current ?? TaskScheduler.Default).Unwrap();

                        var tasks = new Task[taskCount];
                        for (int i = 0; i < taskCount; i++)
                        {
                            var wrapped = new UglyWrapper_Obj_Downloader(i, this, pendingFiles, duhB, resultsOfDownloads, cancellationToken, tweakerhashcacheDump);
                            tasks[i] = Task.Factory.StartNew(wrapped.Start, cancellationToken, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current ?? TaskScheduler.Default).Unwrap();
                        }

                        await Task.WhenAll(t_check, Task.WhenAll(tasks));

                        isOperationSuccess = true;
                    }
                    catch
                    {
                        if (resultsOfDownloads == null)
                        {
                            resultsOfDownloads = new ConcurrentDictionary<PatchListItem, bool?>(1, 0);
                        }
                        isOperationSuccess = false;
                        throw;
                    }
                    finally
                    {
                        if (pendingFiles != null)
                        {
                            pendingFiles.Dispose();
                            pendingFiles = null;
                        }
                        if (duhB != null)
                        {
                            duhB.SetPSO2ClientVersion(ver.ToString());
                            await duhB.DisposeAsync();
                            duhB = null;
                        }
                        if (resultsOfDownloads == null)
                        {
                            resultsOfDownloads = new ConcurrentDictionary<PatchListItem, bool?>(1, 0);
                        }
                        IReadOnlyCollection<PatchListItem> list_all;
                        if (patchlist == null)
                        {
                            list_all = new List<PatchListItem>(0);
                        }
                        else if (patchlist is PatchListMemory memorylist)
                        {
                            list_all = memorylist;
                        }
                        else
                        {
                            var list = new List<PatchListItem>(patchlist.Count);
                            list.AddRange(patchlist);
                            list_all = new ReadOnlyCollection<PatchListItem>(list);
                            patchlist = null;
                        }
                        if (Interlocked.CompareExchange(ref this.flag_operationStarted, 0, 1) == 1)
                        {
                            // this.OnClientOperationComplete1(dir_pso2bin, selection, list_all, bag_needtodownload, bag_success, bag_failure, ver, isOperationSuccess, cancellationToken);
                            this.OnClientOperationComplete1(dir_pso2bin, pso2tweaker_dirpath, tweakerhashcacheDump, selection, list_all, resultsOfDownloads, ver, isOperationSuccess, cancellationToken);
                        }

                        // For debugging purpose.
                        // Don't call GC.Collect because it won't help much.
                        // GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                        // GC.Collect(GC.MaxGeneration, GCCollectionMode.Default, true, true);
                    }
                }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current ?? TaskScheduler.Default).Unwrap();
            }

            await this.t_operation;
        }

        sealed class UglyWrapper_Obj_Scanner
        {
            private readonly GameClientUpdater thisRef;
            private readonly BlockingCollection<DownloadItem> pendingFiles;
            private readonly IFileCheckHashCache duhB;
            private readonly ConcurrentDictionary<PatchListItem, bool?> resultsOfDownloads;
            private readonly CancellationToken cancellationToken;
            private readonly PSO2TweakerHashCache? tweakerhashcacheDump;

            private readonly string dir_pso2bin;
            private readonly string? dir_classic_data;
            private readonly GameClientSelection selection;
            private readonly FileScanFlags fScanReboot, fScanClassic;
            private readonly PatchListMemory patchlist;

            public UglyWrapper_Obj_Scanner(GameClientUpdater updater, BlockingCollection<DownloadItem> collection, IFileCheckHashCache db, ConcurrentDictionary<PatchListItem, bool?> results, CancellationToken token, PSO2TweakerHashCache? tweakerhashcacheDump,
                string dir_pso2bin, string? dir_classic_data, GameClientSelection selection, FileScanFlags fScanReboot, FileScanFlags fScanClassic, PatchListMemory patchlist)
            {
                this.thisRef = updater;
                this.pendingFiles = collection;
                this.duhB = db;
                this.resultsOfDownloads = results;
                this.cancellationToken = token;
                this.tweakerhashcacheDump = tweakerhashcacheDump;
                this.dir_pso2bin = dir_pso2bin;
                this.dir_classic_data = dir_classic_data;
                this.selection = selection;
                this.fScanReboot = fScanReboot;
                this.fScanClassic = fScanClassic;
                this.patchlist = patchlist;
            }

            public async Task Work()
            {
                try
                {
                    await this.thisRef.InnerScanForFilesNeedToDownload(this.pendingFiles, this.dir_pso2bin, this.dir_classic_data, this.tweakerhashcacheDump, this.selection, this.fScanReboot, this.fScanClassic, this.duhB, this.patchlist, this._DownloadQueueAdd, this.cancellationToken);
                }
                finally
                {
                    this.pendingFiles.CompleteAdding();
                    this.thisRef.OnFileCheckEnd();
                }
            }

            private void _DownloadQueueAdd(in DownloadItem item)
            {
                // bag_needtodownload.Add(item.PatchInfo);
                this.resultsOfDownloads.TryAdd(item.PatchInfo, null);
                this.thisRef.OnDownloadQueueAdded();
            }
    }

        sealed class UglyWrapper_Obj_Downloader
        {
            private readonly int Id;
            private readonly GameClientUpdater thisRef;
            private readonly BlockingCollection<DownloadItem> pendingFiles;
            private readonly IFileCheckHashCache duhB;
            private readonly ConcurrentDictionary<PatchListItem, bool?> resultsOfDownloads;
            private readonly CancellationToken cancellationToken;
            private readonly PSO2TweakerHashCache? tweakerhashcacheDump;

            public UglyWrapper_Obj_Downloader(int id, GameClientUpdater updater, BlockingCollection<DownloadItem> collection, IFileCheckHashCache db, ConcurrentDictionary<PatchListItem, bool?> results, CancellationToken token, PSO2TweakerHashCache? tweakerhashcacheDump)
            {
                this.Id = id;
                this.thisRef = updater;
                this.pendingFiles = collection;
                this.duhB = db;
                this.resultsOfDownloads = results;
                this.cancellationToken = token;
                this.tweakerhashcacheDump = tweakerhashcacheDump;
            }

            public Task Start()
            {
                return thisRef.InnerDownloadSingleFile(this.Id, this.pendingFiles, this.duhB, this.OnDownloadFinishCallback, this.cancellationToken);
            }

            public void OnDownloadFinishCallback(in DownloadItem item, in bool success)
            {
                this.resultsOfDownloads.AddOrUpdate<bool?>(item.PatchInfo, (key, arg) => arg, (key, existing, arg) =>
                {
                    if (existing == null)
                    {
                        return arg;
                    }
                    else
                    {
                        return existing;
                    }
                }, success);

                if (success)
                {
                    this.tweakerhashcacheDump?.WriteString(item.PatchInfo.GetSpanFilenameWithoutAffix(), item.PatchInfo.MD5);
                }
                thisRef.OnProgressEnd(this.Id, item.PatchInfo, in success);
            }

            // pendingFiles, duhB, onDownloadFinishCallback, cancellationToken
        }
#nullable restore

        // private static string _prefix_data_classic = Path.Combine("data", "win32");
        // private static string _prefix_data_reboot = Path.Combine("data", "win32reboot");

#nullable enable
        /// <returns>Full path to a directory.</returns>
        private static string DetermineWhere(PatchListItem item, in string pso2bin, in string? classicData, out bool isLink)
        {
            if (!string.IsNullOrEmpty(classicData) && item.IsRebootData == false)
            {
                isLink = true;
                var filename = item.GetFilenameWithoutAffix();
                return Path.GetFullPath(filename, classicData);
            }
            else
            {
                isLink = false;
                var filename = item.GetFilenameWithoutAffix();
                return Path.GetFullPath(filename, pso2bin);
            }
            
            /*
            var normalized = PathStringComparer.Default.NormalizePath(in filename);
            if (item.IsDataFile)
            {
                var alwighilwagh = item.IsRebootData;
                if (alwighilwagh.HasValue)
                {
                    if (item.IsRebootData == false && !string.IsNullOrEmpty(classicData))
                    {
                        isLink = true;
                        return Path.GetFullPath(filename, classicData);
                    }
                    else if (item.IsRebootData == true && !string.IsNullOrEmpty(rebootData))
                    {
                        isLink = true;
                        return Path.GetFullPath(filename, rebootData);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(classicData) && normalized.StartsWith(_prefix_data_classic))
                    {
                        isLink = true;
                        return Path.GetFullPath(filename, classicData);
                    }
                    else if (!string.IsNullOrEmpty(rebootData) && normalized.StartsWith(_prefix_data_reboot))
                    {
                        isLink = true;
                        return Path.GetFullPath(filename, rebootData);
                    }
                }
            }
            isLink = false;
            return Path.GetFullPath(filename, pso2bin);
            */
        }
#nullable restore
    }
}

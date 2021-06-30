using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.PSO2Launcher.Helper;
using Leayal.SharedInterfaces;
using SymbolicLinkSupport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public class GameClientUpdater : AsyncDisposeObject
    {
        // Task Thread: File checking
        // Task Thread(s): File download
        // Both or all must be working at the same time.
        // Progressive file hash cache. Which should reduce the risk of progress loss when application crash or computer shutdown due to any reasons (black out, BSOD).
        // Which also means the cancellation should follow along.

        // ...  Welp

        private const string Name_PatchRootInfo = "management_beta.txt";

        private readonly string dir_pso2bin;
        private readonly string? dir_classic_data, dir_reboot_data;
        private readonly PSO2HttpClient webclient;

        // Cache purposes
        private readonly ObjectShortCacheManager<object> lastKnownObjects;
        private readonly FileCheckHashCache hashCacheDb;
        private PSO2Version? lastKnownRemoteVersion;

        // File check
        private Task t_fileCheckStarted, t_fileDownloadStarted;
        private long count_fileFailure, count_totalFiles;

        // Threadings
        private int flag_fileCheckStarted, flag_fileDownloadStarted, flag_operationCount;
        private readonly BlockingCollection<DownloadItem> pendingFiles;


        public int ConcurrentDownloadCount { get; set; }
        public int ThrottleFileCheckFactor { get; set; }

        // Snail mode (for when internet is extremely unreliable).
        public bool SnailMode { get; set; }

        public GameClientUpdater(string whereIsThePSO2_BIN, string? preference_classicWhere, string? preference_rebootWhere, string hashCheckCache, PSO2HttpClient httpHandler)
        {
            this.hashCacheDb = new FileCheckHashCache(hashCheckCache);
            this.count_fileFailure = 0;
            this.count_totalFiles = 0;
            this.lastKnownRemoteVersion = null;
            this.ConcurrentDownloadCount = 0;
            this.ThrottleFileCheckFactor = 0;
            this.flag_fileCheckStarted = 0;
            this.flag_fileDownloadStarted = 0;
            this.dir_pso2bin = Path.GetFullPath(whereIsThePSO2_BIN);
            this.dir_classic_data = string.IsNullOrWhiteSpace(preference_classicWhere) ? null : Path.GetFullPath(preference_classicWhere);
            this.dir_reboot_data = string.IsNullOrWhiteSpace(preference_rebootWhere) ? null : Path.GetFullPath(preference_rebootWhere);
            this.pendingFiles = new BlockingCollection<DownloadItem>();
            this.lastKnownObjects = new ObjectShortCacheManager<object>();
            // this.workingDirectory = whereIsThePSO2_BIN;
            this.webclient = httpHandler;
            this.SnailMode = false;
        }

        public Task Prepare() => this.hashCacheDb.Load();

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

        public async Task<bool> CheckForPSO2Updates(CancellationToken cancellationToken)
        {
            var versionFilePath = Path.GetFullPath("version.ver", this.dir_pso2bin);
            string verString;
            if (File.Exists(versionFilePath))
            {
                verString = QuickFile.ReadFirstLine(versionFilePath);
            }
            else
            {
                verString = string.Empty;
            }

            var patchInfoRoot = await this.InnerGetPatchRootAsync(true, cancellationToken); // Force refresh because we are CHECKING for newer version.
            var remoteVersion = await this.webclient.GetPatchVersionAsync(patchInfoRoot, cancellationToken);

            if (!PSO2Version.TrySafeParse(in verString, out var localPSO2Ver) || localPSO2Ver != remoteVersion)
            {
                this.lastKnownRemoteVersion = remoteVersion;
                return true;
            }

            return false;
        }

        public Task ScanForFilesNeedToDownload(GameClientSelection selection, FileScanFlags flags, CancellationToken cancellationToken)
        {
            if (flags == FileScanFlags.None)
            {
                throw new ArgumentOutOfRangeException(nameof(flags));
            }

            if (Interlocked.CompareExchange(ref this.flag_fileCheckStarted, 1, 0) == 0)
            {
                Interlocked.Increment(ref this.flag_operationCount);
                var checkTask = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        await this.InnerScanForFilesNeedToDownload(selection, flags, cancellationToken);
                    }
                    finally
                    {
                        this.pendingFiles.CompleteAdding();
                        this.OnFileCheckEnd();
                    }
                }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current ?? TaskScheduler.Default).Unwrap();

                this.t_fileCheckStarted = checkTask.ContinueWith(t =>
                {
                    if (Interlocked.CompareExchange(ref this.flag_fileCheckStarted, 0, 1) == 1)
                    {
                        if (Interlocked.Decrement(ref this.flag_operationCount) == 0)
                        {
                            this.OnClientOperationComplete1(cancellationToken);
                        }
                    }
                });
            }

            return this.t_fileCheckStarted;
        }

        private async Task InnerScanForFilesNeedToDownload(GameClientSelection selection, FileScanFlags flags, CancellationToken cancellationToken)
        {
            var factorSetting = this.ThrottleFileCheckFactor;
            int fileCheckThrottleFactor;
            if (factorSetting > 0)
            {
                fileCheckThrottleFactor = Convert.ToInt32(1000 / factorSetting);
                if (fileCheckThrottleFactor < 20)
                {
                    fileCheckThrottleFactor = 0;
                }
            }
            else
            {
                fileCheckThrottleFactor = 0;
            }

            // Acquire patch list.
            PatchListBase selectedList;
            var patchInfoRoot = await this.InnerGetPatchRootAsync(cancellationToken);
            this.lastKnownRemoteVersion = await this.webclient.GetPatchVersionAsync(patchInfoRoot, cancellationToken);
            var t_alwaysList = this.webclient.GetPatchListAlwaysAsync(patchInfoRoot, cancellationToken);
            switch (selection)
            {
                case GameClientSelection.NGS_AND_CLASSIC:
                    selectedList = await this.webclient.GetPatchListAllAsync(patchInfoRoot, cancellationToken);
                    break;

                case GameClientSelection.NGS_Prologue_Only:
                    selectedList = await this.webclient.GetPatchListNGSPrologueAsync(patchInfoRoot, cancellationToken);
                    break;

                case GameClientSelection.NGS_Only:
                    // Download both files at the same time.
                    var t_prologueOnly = this.webclient.GetPatchListNGSPrologueAsync(patchInfoRoot, cancellationToken);
                    var t_fullngs = this.webclient.GetPatchListNGSFullAsync(patchInfoRoot, cancellationToken);

                    await Task.WhenAll(t_prologueOnly, t_fullngs);

                    selectedList = PatchListBase.Create(await t_prologueOnly, await t_fullngs);

                    break;

                default:
                    // Universe exploded because you handed wrong value.
                    throw new ArgumentOutOfRangeException(nameof(selection));
            }

            var alwaysList = await t_alwaysList;
            var headacheMatterAgain = PatchListBase.Create(selectedList, alwaysList);

            if (headacheMatterAgain is PatchListMemory patchListMemory)
            {
                Interlocked.Exchange(ref this.count_totalFiles, patchListMemory.Count);
                this.OnFileCheckBegin(patchListMemory.Count);
            }
            else
            {
                Interlocked.Exchange(ref this.count_totalFiles, -1);
                this.OnFileCheckBegin(-1);
            }

            var duhB = this.hashCacheDb;

            // Begin file check

            // Maybe Enum.HasFlag() is better than this mess????

            int processedCount = 0;

            bool flag_cacheOnly = flags.HasFlag(FileScanFlags.CacheOnly),
                flag_forceRefresh = flags.HasFlag(FileScanFlags.ForceRefreshCache),
                flag_useMd5 = flags.HasFlag(FileScanFlags.MD5HashMismatch);

            if (flag_forceRefresh)
            {
                foreach (var patchItem in headacheMatterAgain)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var localFilename = patchItem.GetFilenameWithoutAffix();
                    string localFilePath = Path.GetFullPath(localFilename, this.dir_pso2bin);
                    // localFilePath = DetermineWhere(patchItem, this.dir_pso2bin, this.dir_classic_data, this.dir_reboot_data, out var isLink);
                    if (!File.Exists(localFilePath))
                    {
                        var linkTo = DetermineWhere(patchItem, this.dir_pso2bin, this.dir_classic_data, this.dir_reboot_data, out var isLink);
                        if (isLink)
                        {
                            this.pendingFiles.Add(new DownloadItem(patchItem, localFilePath, linkTo));
                        }
                        else
                        {
                            this.pendingFiles.Add(new DownloadItem(patchItem, localFilePath, null));
                        }
                    }
                    else
                    {
                        var localLastModifiedTimeUtc = File.GetLastWriteTimeUtc(localFilePath);
                        using (var fs = File.OpenRead(localFilePath))
                        {
                            var localFileLen = fs.Length;
                            if (localFileLen == patchItem.FileSize)
                            {
                                if (flag_useMd5)
                                {
                                    string localMd5 = MD5Hash.ComputeHashFromFile(fs);
                                    var cached = await duhB.GetPatchItem(localFilename);

                                    var bool_compareMD5 = string.Equals(localMd5, patchItem.MD5, StringComparison.OrdinalIgnoreCase);

                                    if (cached == null || cached.LastModifiedTimeUTC != localLastModifiedTimeUtc || cached.FileSize != localFileLen || !string.Equals(localMd5, cached.MD5, StringComparison.OrdinalIgnoreCase))
                                    {
                                        await duhB.SetPatchItem(new PatchListItem(null, patchItem.RemoteFilename, localFileLen, localMd5), localLastModifiedTimeUtc);
                                    }

                                    if (!bool_compareMD5)
                                    {
                                        var linkTo = DetermineWhere(patchItem, this.dir_pso2bin, this.dir_classic_data, this.dir_reboot_data, out var isLink);
                                        if (isLink)
                                        {
                                            this.pendingFiles.Add(new DownloadItem(patchItem, localFilePath, linkTo));
                                        }
                                        else
                                        {
                                            this.pendingFiles.Add(new DownloadItem(patchItem, localFilePath, null));
                                        }
                                        // this.pendingFiles.Add(patchItem);
                                    }
                                }
                            }
                            else
                            {
                                var linkTo = DetermineWhere(patchItem, this.dir_pso2bin, this.dir_classic_data, this.dir_reboot_data, out var isLink);
                                if (isLink)
                                {
                                    this.pendingFiles.Add(new DownloadItem(patchItem, localFilePath, linkTo));
                                }
                                else
                                {
                                    this.pendingFiles.Add(new DownloadItem(patchItem, localFilePath, null));
                                }
                            }
                        }
                    }
                    this.OnFileCheckReport(Interlocked.Increment(ref processedCount));
                    if (fileCheckThrottleFactor != 0)
                    {
                        await Task.Delay(fileCheckThrottleFactor);
                    }
                }
            }
            else if (flag_cacheOnly)
            {
                foreach (var patchItem in headacheMatterAgain)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var localFilename = patchItem.GetFilenameWithoutAffix();
                    // string localFilePath = DetermineWhere(patchItem, this.dir_pso2bin, this.dir_classic_data, this.dir_reboot_data);
                    var localFilePath = Path.GetFullPath(localFilename, this.dir_pso2bin);
                    var cachedHash = await duhB.GetPatchItem(localFilename);
                    // var localLastModifiedTimeUtc = File.GetLastWriteTimeUtc(localFilePath);

                    if (cachedHash == null)
                    {
                        var linkTo = DetermineWhere(patchItem, this.dir_pso2bin, this.dir_classic_data, this.dir_reboot_data, out var isLink);
                        if (isLink)
                        {
                            this.pendingFiles.Add(new DownloadItem(patchItem, localFilePath, linkTo));
                        }
                        else
                        {
                            this.pendingFiles.Add(new DownloadItem(patchItem, localFilePath, null));
                        }
                        // this.pendingFiles.Add(patchItem);
                    }
                    else
                    {
                        if (!string.Equals(cachedHash.MD5, patchItem.MD5, StringComparison.OrdinalIgnoreCase))
                        {
                            var linkTo = DetermineWhere(patchItem, this.dir_pso2bin, this.dir_classic_data, this.dir_reboot_data, out var isLink);
                            if (isLink)
                            {
                                this.pendingFiles.Add(new DownloadItem(patchItem, localFilePath, linkTo));
                            }
                            else
                            {
                                this.pendingFiles.Add(new DownloadItem(patchItem, localFilePath, null));
                            }
                        }
                    }

                    this.OnFileCheckReport(Interlocked.Increment(ref processedCount));
                    if (fileCheckThrottleFactor != 0)
                    {
                        await Task.Delay(fileCheckThrottleFactor);
                    }
                }
            }
            else
            {
                foreach (var patchItem in headacheMatterAgain)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var localFilename = patchItem.GetFilenameWithoutAffix();
                    var localFilePath = Path.GetFullPath(localFilename, this.dir_pso2bin);
                    // string localFilePath = DetermineWhere(patchItem, this.dir_pso2bin, this.dir_classic_data, this.dir_reboot_data);
                    if (!File.Exists(localFilePath))
                    {
                        string linkTo = DetermineWhere(patchItem, this.dir_pso2bin, this.dir_classic_data, this.dir_reboot_data, out var isLink);
                        if (isLink)
                        {
                            this.pendingFiles.Add(new DownloadItem(patchItem, localFilePath, linkTo));
                        }
                        else
                        {
                            this.pendingFiles.Add(new DownloadItem(patchItem, localFilePath, null));
                        }
                    }
                    else
                    {
                        var cachedHash = await duhB.GetPatchItem(localFilename);
                        var localLastModifiedTimeUtc = File.GetLastWriteTimeUtc(localFilePath);
                        using (var fs = File.OpenRead(localFilePath))
                        {
                            var localFileLen = fs.Length;
                            if (localFileLen == patchItem.FileSize)
                            {
                                if (flag_useMd5)
                                {
                                    string localMd5;
                                    if (cachedHash != null && cachedHash.FileSize == localFileLen && localLastModifiedTimeUtc == cachedHash.LastModifiedTimeUTC)
                                    {
                                        localMd5 = cachedHash.MD5;
                                    }
                                    else
                                    {
                                        localMd5 = MD5Hash.ComputeHashFromFile(fs);
                                        await duhB.SetPatchItem(new PatchListItem(null, patchItem.RemoteFilename, localFileLen, localMd5), localLastModifiedTimeUtc);
                                    }

                                    if (!string.Equals(localMd5, patchItem.MD5, StringComparison.OrdinalIgnoreCase))
                                    {
                                        string linkTo = DetermineWhere(patchItem, this.dir_pso2bin, this.dir_classic_data, this.dir_reboot_data, out var isLink);
                                        if (isLink)
                                        {
                                            this.pendingFiles.Add(new DownloadItem(patchItem, localFilePath, linkTo));
                                        }
                                        else
                                        {
                                            this.pendingFiles.Add(new DownloadItem(patchItem, localFilePath, null));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                string linkTo = DetermineWhere(patchItem, this.dir_pso2bin, this.dir_classic_data, this.dir_reboot_data, out var isLink);
                                if (isLink)
                                {
                                    this.pendingFiles.Add(new DownloadItem(patchItem, localFilePath, linkTo));
                                }
                                else
                                {
                                    this.pendingFiles.Add(new DownloadItem(patchItem, localFilePath, null));
                                }
                            }
                        }
                    }
                    this.OnFileCheckReport(Interlocked.Increment(ref processedCount));
                    if (fileCheckThrottleFactor != 0)
                    {
                        await Task.Delay(fileCheckThrottleFactor);
                    }
                }
            }
        }

        public Task StartDownloadFiles(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref this.flag_fileDownloadStarted, 1, 0) == 0)
            {
                Interlocked.Increment(ref this.flag_operationCount);
                var taskCount = this.ConcurrentDownloadCount;
                if (taskCount == 0)
                {
                    taskCount = RuntimeValues.GetProcessorCountAuto();
                }
                var tasks = new Task[taskCount];

                for (int i = 0; i < taskCount; i++)
                {
                    tasks[i] = Task.Factory.StartNew(async () =>
                    {
                        await this.InnerDownloadSingleFile(cancellationToken);
                    }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current ?? TaskScheduler.Default).Unwrap();
                }

                var allTasks = Task.WhenAll(tasks);

                this.t_fileDownloadStarted = allTasks.ContinueWith(t =>
                {
                    if (Interlocked.CompareExchange(ref this.flag_fileDownloadStarted, 0, 1) == 1)
                    {
                        if (Interlocked.Decrement(ref this.flag_operationCount) == 0)
                        {
                            this.OnClientOperationComplete1(cancellationToken);
                        }
                    }
                });
            }

            return this.t_fileDownloadStarted;
        }

        private async Task InnerDownloadSingleFile(CancellationToken cancellationToken)
        {
            // var downloadbuffer = new byte[4096];
            // var downloadbuffer = new byte[1024 * 1024]; // Increase buffer size to 1MB due to async's overhead.
            byte[] downloadbuffer;
            if (this.SnailMode)
            {
                downloadbuffer = new byte[4096]; // 4KB buffer.
            }
            else
            {
                downloadbuffer = new byte[1024 * 24]; // 24KB buffer.
            }
            var duhB = this.hashCacheDb;

            foreach (var downloadItem in this.pendingFiles.GetConsumingEnumerable())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var localFilePath = downloadItem.Destination;
                // var localFilePath = Path.GetFullPath(localFilename, this.workingDirectory);
                // var tmpFilename = localFilename + ".dtmp";
                var tmpFilePath = localFilePath + ".dtmp"; // Path.GetFullPath(tmpFilename, this.workingDirectory);

                // Check whether the launcher has the access right or able to create file at the destination
                bool isSuccess = false;

                var localStream = File.Create(tmpFilePath); // Sync it is
                try
                {
                    using (var response = await this.webclient.OpenForDownloadAsync(downloadItem.PatchInfo, cancellationToken))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            // Check if the response has content-length header.
                            long remoteSizeInBytes = -1, bytesDownloaded = 0;
                            var header = response.Content.Headers.ContentLength;
                            if (header.HasValue)
                            {
                                remoteSizeInBytes = header.Value;
                            }
                            else
                            {
                                remoteSizeInBytes = downloadItem.PatchInfo.FileSize;
                            }
                            using (var remoteStream = response.Content.ReadAsStream())
                            {
                                this.OnProgressBegin(downloadItem.PatchInfo, in remoteSizeInBytes);
                                if (remoteSizeInBytes == -1)
                                {
                                    // Download without knowing total size, until upstream get EOF.

                                    // Still need async to support cancellation faster.
                                    var byteRead = await remoteStream.ReadAsync(downloadbuffer, 0, downloadbuffer.Length, cancellationToken);
                                    while (byteRead > 0)
                                    {
                                        if (cancellationToken.IsCancellationRequested)
                                        {
                                            break;
                                        }
                                        localStream.Write(downloadbuffer, 0, byteRead);
                                        bytesDownloaded += byteRead;
                                        byteRead = await remoteStream.ReadAsync (downloadbuffer, 0, downloadbuffer.Length, cancellationToken);
                                    }
                                }
                                else
                                {
                                    // Download while reporting the download progress, until upstream get EOF.
                                    var byteRead = await remoteStream.ReadAsync(downloadbuffer, 0, downloadbuffer.Length, cancellationToken);
                                    while (byteRead > 0)
                                    {
                                        if (cancellationToken.IsCancellationRequested)
                                        {
                                            break;
                                        }

                                        localStream.Write(downloadbuffer, 0, byteRead);
                                        bytesDownloaded += byteRead;
                                        // Report progress here
                                        this.OnProgressReport(downloadItem.PatchInfo, in bytesDownloaded);
                                        byteRead = await remoteStream.ReadAsync(downloadbuffer, 0, downloadbuffer.Length, cancellationToken);
                                    }
                                }

                                localStream.Flush();
                                localStream.Position = 0;

                                // Final check
                                var downloadedMd5 = MD5Hash.ComputeHashFromFile(localStream);
                                if (downloadedMd5 == downloadItem.PatchInfo.MD5)
                                {
                                    isSuccess = true;
                                }
                            }
                        }
                        else
                        {
                            // Report failure and continue to another file.
                        }
                    }
                }
                finally
                {
                    localStream.Dispose();
                }

                if (isSuccess)
                {
                    var lastWrittenTimeUtc = File.GetLastWriteTimeUtc(localFilePath);
                    try
                    {
                        File.Move(tmpFilePath, localFilePath, true);
                        await duhB.SetPatchItem(downloadItem.PatchInfo, lastWrittenTimeUtc);
                    }
                    catch
                    {
                        File.Delete(tmpFilePath); // Won't throw error if file is not existed.
                        throw;
                    }
                }
                else
                {
                    Interlocked.Increment(ref this.count_fileFailure);
                    File.Delete(tmpFilePath);
                }

                this.OnProgressEnd(downloadItem.PatchInfo, in isSuccess);
            }
        }

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
        private void OnClientOperationComplete1(CancellationToken cancellationToken)
        {
            // Everything is completed.
            // Write the version file out.

            var failureCount = Interlocked.Exchange(ref this.count_fileFailure, 0);
            var totalCount = Interlocked.Exchange(ref this.count_totalFiles, 0);

            var ver = lastKnownRemoteVersion;
            lastKnownRemoteVersion = null;

            if (!cancellationToken.IsCancellationRequested && ver.HasValue)
            {
                var localFilePath = Path.GetFullPath("version.ver", this.dir_pso2bin);
                if (Directory.Exists(localFilePath))
                {
                    Directory.Delete(localFilePath);
                }
                File.WriteAllText(localFilePath, ver.Value.ToString());

            }

            this.OperationCompleted?.Invoke(this, totalCount, failureCount);
        }

        public event FileCheckBeginHandler FileCheckBegin;
        private void OnFileCheckBegin(in int total) => this.FileCheckBegin?.Invoke(this, total);

        public event FileCheckReportHandler FileCheckReport;
        private void OnFileCheckReport(in int current) => this.FileCheckReport?.Invoke(this, current);

        public event FileCheckEndHandler FileCheckEnd;
        private void OnFileCheckEnd() => this.FileCheckEnd?.Invoke(this);

        protected override async Task OnDisposeAsync()
        {
            this.pendingFiles?.Dispose();
            await this.hashCacheDb.DisposeAsync();
        }

        public delegate void ProgressBeginHandler(PatchListItem file, in long totalProgressValue);
        public delegate void ProgressReportHandler(PatchListItem file, in long currentProgressValue);
        public delegate void ProgressEndHandler(PatchListItem file, in bool isSuccess);

        public delegate void FileCheckEndHandler(GameClientUpdater sender);
        public delegate void FileCheckBeginHandler(GameClientUpdater sender, int total);
        public delegate void FileCheckReportHandler(GameClientUpdater sender, int current);
        public delegate void OperationCompletedHandler(GameClientUpdater sender, long howManyFileInTotal, long howManyFileFailure);


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

        private static string _prefix_data_classic = Path.Combine("data", "win32");
        private static string _prefix_data_reboot = Path.Combine("data", "win32reboot");

        /// <returns>Full path to a directory.</returns>
        private static string DetermineWhere(PatchListItem item, in string pso2bin, in string? classicData, in string? rebootData, out bool isLink)
        {
            var filename = item.GetFilenameWithoutAffix();
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
        }
    }
}

using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.PSO2Launcher.Helper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    class GameClientUpdater : IAsyncDisposable
    {
        // Task Thread: File checking
        // Task Thread(s): File download
        // Both or all must be working at the same time.
        // Progressive file hash cache. Which should reduce the risk of progress loss when application crash or computer shutdown due to any reasons (black out, BSOD).
        // Which also means the cancellation should follow along.

        // ...  Welp

        private const string Name_PatchRootInfo = "management_beta.txt";

        private readonly string workingDirectory;
        private readonly PSO2HttpClient webclient;

        // Cache purposes
        private readonly ObjectShortCacheManager<object> lastKnownObjects;
        private readonly FileCheckHashCache hashCacheDb;
        private PSO2Version? lastKnownRemoteVersion;

        // File check
        private readonly object synclock_pendingFiles;
        private Task t_fileCheckStarted, t_fileDownloadStarted;

        private long count_fileFailure, count_totalFiles;

        // Edgy stuffs: volatile
        private volatile bool flag_fileCheckStarted, flag_fileDownloadStarted;

        private BlockingCollection<PatchListItem> pendingFiles;

        public int ConcurrentDownloadCount { get; set; }

        // Snail mode (for when internet is extremely unreliable).

        public GameClientUpdater(string whereIsThePSO2_BIN, string hashCheckCache, PSO2HttpClient httpHandler)
        {
            this.hashCacheDb = new FileCheckHashCache(hashCheckCache);
            this.count_fileFailure = 0;
            this.count_totalFiles = 0;
            this.ConcurrentDownloadCount = Math.Min(Environment.ProcessorCount / 2, 4);
            this.synclock_pendingFiles = new object();
            this.flag_fileCheckStarted = false;
            this.flag_fileDownloadStarted = false;
            this.lastKnownObjects = new ObjectShortCacheManager<object>();
            this.workingDirectory = whereIsThePSO2_BIN;
            this.webclient = httpHandler;
        }

        public Task LoadLocalHashCheck()
        {
            return this.hashCacheDb.Load();
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

        public async Task<bool> CheckForPSO2Updates(CancellationToken cancellationToken)
        {
            var versionFilePath = Path.GetFullPath("version.ver", this.workingDirectory);
            var verString = QuickFile.ReadFirstLine(versionFilePath);

            var patchInfoRoot = await this.InnerGetPatchRootAsync(true, cancellationToken); // Force refresh because we are CHECKING for newer version.
            var remoteVersion = await this.webclient.GetPatchVersionAsync(patchInfoRoot, cancellationToken);

            if (!PSO2Version.TrySafeParse(in verString, out var localPSO2Ver) || localPSO2Ver != remoteVersion)
            {
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

            lock (this.synclock_pendingFiles)
            {
                if (this.flag_fileCheckStarted) return this.t_fileCheckStarted;
                this.flag_fileCheckStarted = true;

                if (this.pendingFiles == null)
                {
                    this.pendingFiles = new BlockingCollection<PatchListItem>();
                }
            }

            var checkTask = Task.Factory.StartNew(async () =>
            {
                try
                {
                    await this.InnerScanForFilesNeedToDownload(selection, flags, cancellationToken);
                }
                finally
                {
                    this.pendingFiles.CompleteAdding();
                }
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current ?? TaskScheduler.Default).Unwrap();

            this.t_fileCheckStarted = checkTask.ContinueWith(t =>
            {
                this.flag_fileCheckStarted = false;
                this.OnClientOperationComplete1();
            });

            return this.t_fileCheckStarted;
        }

        private async Task InnerScanForFilesNeedToDownload(GameClientSelection selection, FileScanFlags flags, CancellationToken cancellationToken)
        {
            // workingDirectory

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

            switch (flags)
            {
                case FileScanFlags.FileSizeMismatch | FileScanFlags.MD5HashMismatch:
                case FileScanFlags.MissingFilesOnly | FileScanFlags.FileSizeMismatch | FileScanFlags.MD5HashMismatch:
                    foreach (var patchItem in headacheMatterAgain)
                    {
                        var localFilename = patchItem.GetFilenameWithoutAffix();
                        var localFilePath = Path.GetFullPath(localFilename, this.workingDirectory);
                        if (!File.Exists(localFilePath))
                        {
                            this.pendingFiles.Add(patchItem);
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
                                        this.pendingFiles.Add(patchItem);
                                    }
                                }
                                else
                                {
                                    this.pendingFiles.Add(patchItem);
                                }
                            }
                        }
                        this.OnFileCheckReport(Interlocked.Increment(ref processedCount));
                    }
                    break;

                case FileScanFlags.FileSizeMismatch | FileScanFlags.MD5HashMismatch | FileScanFlags.ForceRefreshCache:
                case FileScanFlags.MissingFilesOnly | FileScanFlags.FileSizeMismatch | FileScanFlags.MD5HashMismatch | FileScanFlags.ForceRefreshCache:
                    foreach (var patchItem in headacheMatterAgain)
                    {
                        var localFilename = patchItem.GetFilenameWithoutAffix();
                        var localFilePath = Path.GetFullPath(localFilename, this.workingDirectory);
                        if (!File.Exists(localFilePath))
                        {
                            this.pendingFiles.Add(patchItem);
                        }
                        else
                        {
                            using (var fs = File.OpenRead(localFilePath))
                            {
                                if (fs.Length == patchItem.FileSize)
                                {
                                    string localMd5 = MD5Hash.ComputeHashFromFile(fs);
                                    if (!string.Equals(localMd5, patchItem.MD5, StringComparison.OrdinalIgnoreCase))
                                    {
                                        this.pendingFiles.Add(patchItem);
                                    }
                                    else
                                    {
                                        var localLastModifiedTimeUtc = File.GetLastWriteTimeUtc(localFilePath);
                                        await duhB.SetPatchItem(new PatchListItem(null, patchItem.RemoteFilename, patchItem.FileSize, localMd5), localLastModifiedTimeUtc);
                                    }
                                }
                                else
                                {
                                    this.pendingFiles.Add(patchItem);
                                }
                            }
                        }
                        this.OnFileCheckReport(Interlocked.Increment(ref processedCount));
                    }
                    break;

                case FileScanFlags.MissingFilesOnly:
                case FileScanFlags.MissingFilesOnly | FileScanFlags.ForceRefreshCache:
                    foreach (var patchItem in headacheMatterAgain)
                    {
                        var localFilename = patchItem.GetFilenameWithoutAffix();
                        var localFilePath = Path.GetFullPath(localFilename, this.workingDirectory);
                        if (!File.Exists(localFilePath))
                        {
                            this.pendingFiles.Add(patchItem);
                        }
                        else
                        {
                            var cached = await duhB.GetPatchItem(localFilename);
                            if (cached == null)
                            {
                                string localMd5;
                                var localLastModifiedTimeUtc = File.GetLastWriteTimeUtc(localFilePath);
                                using (var fs = File.OpenRead(localFilePath))
                                {
                                    localMd5 = MD5Hash.ComputeHashFromFile(fs);
                                }
                                await duhB.SetPatchItem(new PatchListItem(null, patchItem.RemoteFilename, patchItem.FileSize, localMd5), localLastModifiedTimeUtc);
                            }
                        }
                        this.OnFileCheckReport(Interlocked.Increment(ref processedCount));
                    }
                    break;

                case FileScanFlags.MissingFilesOnly | FileScanFlags.FileSizeMismatch | FileScanFlags.ForceRefreshCache:
                case FileScanFlags.MissingFilesOnly | FileScanFlags.FileSizeMismatch:
                case FileScanFlags.FileSizeMismatch | FileScanFlags.ForceRefreshCache:
                case FileScanFlags.FileSizeMismatch:
                    foreach (var patchItem in headacheMatterAgain)
                    {
                        var localFilename = patchItem.GetFilenameWithoutAffix();
                        var localFilePath = Path.GetFullPath(localFilename, this.workingDirectory);
                        if (!File.Exists(localFilePath))
                        {
                            this.pendingFiles.Add(patchItem);
                        }
                        else
                        {
                            using (var fs = File.OpenRead(localFilePath))
                            {
                                if (fs.Length != patchItem.FileSize)
                                {
                                    this.pendingFiles.Add(patchItem);
                                }
                                else
                                {
                                    var cached = await duhB.GetPatchItem(localFilename);
                                    if (cached == null)
                                    {
                                        var localLastModifiedTimeUtc = File.GetLastWriteTimeUtc(localFilePath);
                                        string localMd5 = MD5Hash.ComputeHashFromFile(fs);
                                        await duhB.SetPatchItem(new PatchListItem(null, patchItem.RemoteFilename, patchItem.FileSize, localMd5), localLastModifiedTimeUtc);
                                    }
                                }
                            }
                        }
                        this.OnFileCheckReport(Interlocked.Increment(ref processedCount));
                    }
                    break;

                case FileScanFlags.MissingFilesOnly | FileScanFlags.MD5HashMismatch:
                case FileScanFlags.MD5HashMismatch:
                    foreach (var patchItem in headacheMatterAgain)
                    {
                        var localFilename = patchItem.GetFilenameWithoutAffix();
                        var localFilePath = Path.GetFullPath(localFilename, this.workingDirectory);
                        if (!File.Exists(localFilePath))
                        {
                            this.pendingFiles.Add(patchItem);
                        }
                        else
                        {
                            var cachedHash = await duhB.GetPatchItem(localFilename);
                            var localLastModifiedTimeUtc = File.GetLastWriteTimeUtc(localFilePath);
                            using (var fs = File.OpenRead(localFilePath))
                            {
                                string localMd5;
                                if (cachedHash != null && cachedHash.FileSize == fs.Length && localLastModifiedTimeUtc == cachedHash.LastModifiedTimeUTC)
                                {
                                    localMd5 = cachedHash.MD5;
                                }
                                else
                                {
                                    localMd5 = MD5Hash.ComputeHashFromFile(fs);
                                    await duhB.SetPatchItem(new PatchListItem(null, patchItem.RemoteFilename, fs.Length, localMd5), localLastModifiedTimeUtc);
                                }
                                if (!string.Equals(localMd5, patchItem.MD5, StringComparison.OrdinalIgnoreCase))
                                {
                                    this.pendingFiles.Add(patchItem);
                                }
                            }
                        }
                        this.OnFileCheckReport(Interlocked.Increment(ref processedCount));
                    }
                    break;

                case FileScanFlags.MissingFilesOnly | FileScanFlags.MD5HashMismatch | FileScanFlags.ForceRefreshCache:
                case FileScanFlags.MD5HashMismatch | FileScanFlags.ForceRefreshCache:
                    foreach (var patchItem in headacheMatterAgain)
                    {
                        var localFilename = patchItem.GetFilenameWithoutAffix();
                        var localFilePath = Path.GetFullPath(localFilename, this.workingDirectory);
                        if (!File.Exists(localFilePath))
                        {
                            this.pendingFiles.Add(patchItem);
                        }
                        else
                        {
                            var localLastModifiedTimeUtc = File.GetLastWriteTimeUtc(localFilePath);
                            string localMd5;
                            long fileLen;
                            using (var fs = File.OpenRead(localFilePath))
                            {
                                fileLen = fs.Length;
                                localMd5 = await MD5Hash.ComputeHashFromFileAsync(fs);
                            }
                            await duhB.SetPatchItem(new PatchListItem(null, patchItem.RemoteFilename, fileLen, localMd5), localLastModifiedTimeUtc);
                            if (!string.Equals(localMd5, patchItem.MD5, StringComparison.OrdinalIgnoreCase))
                            {
                                this.pendingFiles.Add(patchItem);
                            }
                        }
                        this.OnFileCheckReport(Interlocked.Increment(ref processedCount));
                    }
                    break;
            }

            this.OnFileCheckEnd();
        }

        public Task StartDownloadFiles(CancellationToken cancellationToken)
        {
            lock (this.synclock_pendingFiles)
            {
                if (this.flag_fileDownloadStarted) return this.t_fileDownloadStarted;
                this.flag_fileDownloadStarted = true;

                if (this.pendingFiles == null)
                {
                    this.pendingFiles = new BlockingCollection<PatchListItem>();
                }
            }

            var taskCount = this.ConcurrentDownloadCount;
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
                this.flag_fileDownloadStarted = false;
                this.OnClientOperationComplete1();
            });

            return this.t_fileDownloadStarted;
        }

        private async Task InnerDownloadSingleFile(CancellationToken cancellationToken)
        {
            var downloadbuffer = new byte[4096];
            var duhB = this.hashCacheDb;

            foreach (var file in this.pendingFiles.GetConsumingEnumerable())
            {
                var localFilename = file.GetFilenameWithoutAffix();
                var localFilePath = Path.GetFullPath(localFilename, this.workingDirectory);
                var tmpFilename = localFilename + ".dtmp";
                var tmpFilePath = Path.GetFullPath(tmpFilename, this.workingDirectory);

                // Check whether the launcher has the access right or able to create file at the destination
                bool isSuccess = false;

                var localStream = File.Create(tmpFilePath); // Sync it is
                try
                {
                    using (var response = await this.webclient.OpenForDownloadAsync(file, cancellationToken))
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
                                remoteSizeInBytes = file.FileSize;
                            }
                            using (var remoteStream = response.Content.ReadAsStream())
                            {
                                this.OnProgressBegin(file, in remoteSizeInBytes);
                                if (remoteSizeInBytes == -1)
                                {
                                    // Download without knowing total size, until upstream get EOF.
                                    var byteRead = remoteStream.Read(downloadbuffer, 0, downloadbuffer.Length);
                                    while (byteRead > 0)
                                    {
                                        localStream.Write(downloadbuffer, 0, byteRead);
                                        bytesDownloaded += byteRead;
                                        byteRead = remoteStream.Read(downloadbuffer, 0, downloadbuffer.Length);
                                    }
                                }
                                else
                                {
                                    // Download while reporting the download progress, until upstream get EOF.
                                    var byteRead = remoteStream.Read(downloadbuffer, 0, downloadbuffer.Length);
                                    while (byteRead > 0)
                                    {
                                        localStream.Write(downloadbuffer, 0, byteRead);
                                        bytesDownloaded += byteRead;
                                        // Report progress here
                                        this.OnProgressReport(file, in bytesDownloaded);
                                        byteRead = remoteStream.Read(downloadbuffer, 0, downloadbuffer.Length);
                                    }
                                }

                                localStream.Flush();
                                localStream.Position = 0;

                                // Final check
                                var downloadedMd5 = MD5Hash.ComputeHashFromFile(localStream);
                                if (downloadedMd5 == file.MD5)
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
                        await duhB.SetPatchItem(file, lastWrittenTimeUtc);
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

                this.OnProgressEnd(file, in isSuccess);
            }
        }

        public event ProgressReportHandler ProgressReport;
        private void OnProgressReport(PatchListItem currentFile, in long currentProgress)
        {
            this.ProgressReport?.Invoke(currentFile, in currentProgress);
        }

        public event ProgressBeginHandler ProgressBegin;
        private void OnProgressBegin(PatchListItem currentFile, in long totalProgress)
        {
            this.ProgressBegin?.Invoke(currentFile, in totalProgress);
        }

        public event ProgressEndHandler ProgressEnd;
        private void OnProgressEnd(PatchListItem currentFile, in bool isSuccess)
        {
            this.ProgressEnd?.Invoke(currentFile, in isSuccess);
        }

        public event OperationCompletedHandler OperationCompleted;
        private void OnClientOperationComplete1()
        {
            if (!this.flag_fileCheckStarted && !this.flag_fileDownloadStarted)
            {
                // Everything is completed.
                // Write the version file out.

                var failureCount = Interlocked.Exchange(ref this.count_fileFailure, 0);
                var totalCount = Interlocked.Exchange(ref this.count_totalFiles, 0);

                if (lastKnownRemoteVersion.HasValue)
                {
                    File.WriteAllText(Path.GetFullPath("version.ver", this.workingDirectory), lastKnownRemoteVersion.Value.ToString());
                }

                this.OperationCompleted?.Invoke(this, totalCount, failureCount);
            }
        }

        public event FileCheckBeginHandler FileCheckBegin;
        private void OnFileCheckBegin(in int total) => this.FileCheckBegin?.Invoke(this, total);

        public event FileCheckReportHandler FileCheckReport;
        private void OnFileCheckReport(in int current) => this.FileCheckReport?.Invoke(this, current);

        public event FileCheckEndHandler FileCheckEnd;
        private void OnFileCheckEnd() => this.FileCheckEnd?.Invoke(this);

        public async ValueTask DisposeAsync()
        {
            await this.hashCacheDb.DisposeAsync();
            this.pendingFiles?.Dispose();
        }

        public delegate void ProgressBeginHandler(PatchListItem file, in long totalProgressValue);
        public delegate void ProgressReportHandler(PatchListItem file, in long currentProgressValue);
        public delegate void ProgressEndHandler(PatchListItem file, in bool isSuccess);

        public delegate void FileCheckEndHandler(GameClientUpdater sender);
        public delegate void FileCheckBeginHandler(GameClientUpdater sender, int total);
        public delegate void FileCheckReportHandler(GameClientUpdater sender, int current);
        public delegate void OperationCompletedHandler(GameClientUpdater sender, long howManyFileInTotal, long howManyFileFailure);
    }
}

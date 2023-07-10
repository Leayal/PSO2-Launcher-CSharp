using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.Shared;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Leayal.Shared.Windows;
using System.Runtime.CompilerServices;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
#nullable enable
    partial class GameClientUpdater
    {
        private static readonly string __Filename_HashTableRelativePath = "data/win32/d4455ebc2bef618f29106da7692ebc1a",
            __Filename_DlssBinaryFileRelativePath = "nvngx_dlss.dll";

        const int ScanBufferSize = 1024 * 16; // 16KB buffer
        private async Task<PatchListMemory> InnerGetFilelistToScan(GameClientSelection selection, CancellationToken cancellationToken)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void AddToPatchListTasks(Task<PatchListMemory> t, List<Task<PatchListMemory>> list)
            {
                if (t.Status == TaskStatus.Created)
                {
                    try
                    {
                        t.Start();
                    }
                    catch { }
                }
                list.Add(t);
            }

            // Acquire patch list.
            var patchInfoRoot = await this.InnerGetPatchRootAsync(cancellationToken);
            // var t_alwaysList = this.webclient.GetPatchListAlwaysAsync(patchInfoRoot, cancellationToken);
            var tasksOfLists = new List<Task<PatchListMemory>>(5);

            switch (selection)
            {
                case GameClientSelection.NGS_AND_CLASSIC:
                    AddToPatchListTasks(this.webclient.GetPatchListAllAsync(patchInfoRoot, cancellationToken), tasksOfLists);
                    AddToPatchListTasks(this.webclient.GetLauncherListAsync(patchInfoRoot, cancellationToken), tasksOfLists);
                    break;

                case GameClientSelection.NGS_Prologue_Only:
                    AddToPatchListTasks(this.webclient.GetPatchListNGSPrologueAsync(patchInfoRoot, cancellationToken), tasksOfLists);
                    AddToPatchListTasks(this.webclient.GetLauncherListAsync(patchInfoRoot, cancellationToken), tasksOfLists);
                    break;

                case GameClientSelection.Classic_Only:
                    AddToPatchListTasks(this.webclient.GetPatchListClassicAsync(patchInfoRoot, cancellationToken), tasksOfLists);
                    AddToPatchListTasks(this.webclient.GetLauncherListAsync(patchInfoRoot, cancellationToken), tasksOfLists);
                    break;

                case GameClientSelection.Always_Only:
                    AddToPatchListTasks(this.webclient.GetPatchListAlwaysAsync(patchInfoRoot, cancellationToken), tasksOfLists);
                    break;

                case GameClientSelection.NGS_Only:
                    // Download 3 files at the same time.
                    AddToPatchListTasks(this.webclient.GetPatchListNGSPrologueAsync(patchInfoRoot, cancellationToken), tasksOfLists);
                    AddToPatchListTasks(this.webclient.GetPatchListNGSFullAsync(patchInfoRoot, cancellationToken), tasksOfLists);
                    AddToPatchListTasks(this.webclient.GetLauncherListAsync(patchInfoRoot, cancellationToken), tasksOfLists);
                    break;

                default:
                    // Universe exploded because you handed wrong value.
                    throw new ArgumentOutOfRangeException(nameof(selection));
            }

            await Task.WhenAll(tasksOfLists);
            var arr_PatchListBase = new PatchListMemory[tasksOfLists.Count];
            for (int i = 0; i < arr_PatchListBase.Length; i++)
            {
                arr_PatchListBase[i] = await tasksOfLists[i];
            }
            return PatchListBase.Create(arr_PatchListBase);
        }

        private static int RestoreBackups(BackupFileFoundEventArgs e_onBackup, in CancellationToken cancellationToken)
        {
            int filecount = 0;
            if (e_onBackup.Handled == false)
            {
                string fullbakdirpath_reboot = Path.GetFullPath(Path.Combine("data", "win32reboot", "backup"), e_onBackup.Root),
                    fullbakdirpath_classic = Path.GetFullPath(Path.Combine("data", "win32", "backup"), e_onBackup.Root);
                string ? dest = null;
                byte[]? buffer = null;
                try
                {
                    foreach (var item in e_onBackup.Items)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                        filecount++;
                        dest = item.BackupFileDestination;
                        if (File.Exists(dest))
                        {
                            if (buffer == null)
                            {
                                buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(1024 * 128);
                            }

                            var linkTarget = SymbolicLink.FollowTarget(dest);
                            if (linkTarget != null)
                            {
                                dest = Path.GetFullPath(linkTarget);
                                if (!string.Equals(dest, item.BackupFileSourcePath, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (string.Equals(Path.GetPathRoot(dest), Path.GetPathRoot(item.BackupFileSourcePath), StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (File.Exists(item.BackupFileSourcePath))
                                        {
                                            var attr = File.GetAttributes(dest);
                                            File.SetAttributes(dest, attr & ~FileAttributes.ReadOnly);
                                            File.Move(item.BackupFileSourcePath, dest, true);
                                            if (attr != FileAttributes.Normal)
                                            {
                                                File.SetAttributes(dest, attr & ~FileAttributes.Directory);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            var attr = File.GetAttributes(dest);
                                            File.SetAttributes(dest, attr & ~FileAttributes.ReadOnly);
                                            using (var fs_dest = File.Create(dest))
                                            using (var fs_src = File.OpenRead(item.BackupFileSourcePath))
                                            {
                                                if (fs_src.Length != 0)
                                                {
                                                    int byteread = fs_src.Read(buffer, 0, buffer.Length);
                                                    while (byteread > 0)
                                                    {
                                                        if (cancellationToken.IsCancellationRequested)
                                                        {
                                                            break;
                                                        }
                                                        fs_dest.Write(buffer, 0, byteread);
                                                        byteread = fs_src.Read(buffer, 0, buffer.Length);
                                                    }
                                                }
                                            }
                                            File.Delete(item.BackupFileSourcePath);
                                            File.SetAttributes(dest, attr);
                                        }
                                        catch
                                        {
                                            if (File.Exists(item.BackupFileSourcePath))
                                            {
                                                var attr = File.GetAttributes(dest);
                                                File.SetAttributes(dest, attr & ~FileAttributes.ReadOnly);
                                                File.Move(item.BackupFileSourcePath, dest, true);
                                                if (attr != FileAttributes.Normal)
                                                {
                                                    File.SetAttributes(dest, attr & ~FileAttributes.Directory);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (File.Exists(item.BackupFileSourcePath))
                                {
                                    var attr = File.GetAttributes(dest);
                                    File.SetAttributes(dest, attr & ~FileAttributes.ReadOnly);
                                    File.Move(item.BackupFileSourcePath, dest, true);
                                    if (attr != FileAttributes.Normal)
                                    {
                                        File.SetAttributes(dest, attr & ~FileAttributes.Directory);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (File.Exists(item.BackupFileSourcePath))
                            {
                                var attr = FileAttributes.Normal;
                                if (Directory.Exists(dest))
                                {
                                    if (string.Equals(fullbakdirpath_reboot, dest, StringComparison.OrdinalIgnoreCase) || string.Equals(fullbakdirpath_classic, dest, StringComparison.OrdinalIgnoreCase))
                                    {
                                        File.Delete(item.BackupFileSourcePath);
                                        continue;
                                    }
                                    attr = File.GetAttributes(dest);
                                    File.SetAttributes(dest, attr & ~FileAttributes.ReadOnly);
                                    Directory.Delete(dest, true);
                                }
                                File.Move(item.BackupFileSourcePath, dest, true);
                            }
                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    if (string.IsNullOrWhiteSpace(dest))
                    {
                        throw;
                    }
                    else
                    {
                        throw new UnauthorizedAccessException(ex.Message + Environment.NewLine + "The path is: " + dest, ex);
                    }
                }
                finally
                {
                    if (buffer != null)
                    {
                        System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                    }
                }

                // This should clean up all empty directories created by backup.
                // However, it does not hurt if the empty directories still there.
                // So optional, silent the error since it's okay even if it fails.

                // Add "Directory.ResolveLinkTarget" so that in case the backup directory is actually a symlink. We will preserve it instead.
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static void TryDelEmptyBackupFolder(string path)
                {
                    if (Directory.Exists(path) && Directory.ResolveLinkTarget(path, false) == null && !DirectoryHelper.IsDirectoryNotEmpty(path))
                    {
                        try
                        {
                            Directory.Delete(path, true);
                        }
                        catch { }
                    }
                }

                TryDelEmptyBackupFolder(fullbakdirpath_reboot);
                TryDelEmptyBackupFolder(fullbakdirpath_classic);
            }
            return filecount;
        }

        private async Task<BackupFileFoundEventArgs?> SearchForBackup(string dir_pso2bin, GameClientSelection selection, Task<PatchListMemory> t_patchlist)
        {
            switch (selection)
            {
                case GameClientSelection.NGS_AND_CLASSIC:
                case GameClientSelection.NGS_Prologue_Only:
                case GameClientSelection.Classic_Only:
                case GameClientSelection.NGS_Only:
                    if (DirectoryHelper.IsDirectoryExistsAndNotEmpty(Path.Combine(dir_pso2bin, "data", "win32", "backup")) || DirectoryHelper.IsDirectoryExistsAndNotEmpty(Path.Combine(dir_pso2bin, "data", "win32reboot", "backup")))
                    {
                        var e_onBackup = new BackupFileFoundEventArgs(dir_pso2bin, selection, await t_patchlist);
                        await this.OnBackupFileFound(e_onBackup);
                        return e_onBackup;
                    }
                    else
                    {
                        return null;
                    }
                case GameClientSelection.Always_Only: // Explictly handle this by ignoring backups.
                    return null;
                default:
                    return null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private async Task InnerScanForFilesNeedToDownload(BlockingCollection<DownloadItem> pendingFiles, string dir_pso2bin, string? dir_classic_data, PSO2TweakerHashCache? tweakerHashCache, GameClientSelection selection, FileScanFlags fScanReboot, FileScanFlags fScanClassic, IFileCheckHashCache duhB, PatchListBase headacheMatterAgain, InnerDownloadQueueAddCallback onDownloadQueueAdd, CancellationToken cancellationToken)
        {
            if (fScanClassic == FileScanFlags.None)
            {
                fScanClassic = fScanReboot;
            }
            var factorSetting = this.ThrottleFileCheckFactor;
            int fileCheckThrottleFactor;
            if (factorSetting > 0)
            {
                fileCheckThrottleFactor = Convert.ToInt32(1000 / factorSetting);
                if (fileCheckThrottleFactor < 10)
                {
                    fileCheckThrottleFactor = 0;
                }
            }
            else
            {
                fileCheckThrottleFactor = 0;
            }

            // var t_alwaysList = this.webclient.GetPatchListAlwaysAsync(patchInfoRoot, cancellationToken);
            var tasksOfLists = new List<Task<PatchListMemory>>(4);
            // Begin file check

            /*
            static long GetFileSize(ref FileStream fs, string filename)
            {
                if (fs == null)
                {
                    fs = OpenToScan(filename);
                }
                return fs.Length;
            }

            static Task<string> GetFileMD5(ref FileStream fs, MD5 hashal, string filename, in CancellationToken cancellationToken)
            {
                if (fs == null)
                {
                    fs = OpenToScan(filename);
                }
                hashal.Initialize();
                return ___GetFileMD5(fs, hashal, cancellationToken);
            }

            static async Task<string> ___GetFileMD5(FileStream fs, MD5 hashal, CancellationToken cancellationToken)
            {
                var buffer = await hashal.ComputeHashAsync(fs, cancellationToken);
                return Convert.ToHexString(buffer);
            }
            */

            int processedFiles = 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool IsHashTableFile(PatchListItem item) => (MemoryExtensions.Equals(item.GetSpanFilenameWithoutAffix(), __Filename_HashTableRelativePath, StringComparison.OrdinalIgnoreCase) || MemoryExtensions.Equals(item.GetSpanFilenameWithoutAffix(), __Filename_HashTableRelativePath.AsSpan(11), StringComparison.OrdinalIgnoreCase));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool IsDlssBinaryFile(PatchListItem item) => (MemoryExtensions.Equals(item.GetSpanFilenameWithoutAffix(), __Filename_DlssBinaryFileRelativePath, StringComparison.OrdinalIgnoreCase));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void AddItemToQueue(BlockingCollection<DownloadItem> queue, InnerDownloadQueueAddCallback callback, PatchListItem patchItem, string localFilePath, string dir_pso2bin, string? dir_classic_data, CancellationToken cancellationToken)
            {
                var linkTo = DetermineWhere(patchItem, dir_pso2bin, dir_classic_data, out var isLink);
                var item = new DownloadItem(patchItem, localFilePath, isLink ? linkTo : null);
                queue.Add(item, cancellationToken);
                callback.Invoke(in item);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static byte[] BorrowBufferCertainSize(byte[]? buffer, int size)
            {
                if (buffer == null)
                {
                    buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(size);
                }
                else if (buffer.Length < size)
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                    buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(size);
                }

                return buffer;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static async Task<ReadOnlyMemory<byte>> Md5ComputeHash(IncrementalHash engine, FileStream stream, byte[] workingbuffer, CancellationToken cancellationToken, bool retainPosition = false)
            {
                long pos = -1;
                if (retainPosition && stream.CanSeek)
                {
                    pos = stream.Position;
                }
                int read;
                if (stream.IsAsync)
                {
                    while (!cancellationToken.IsCancellationRequested && (read = await stream.ReadAsync(workingbuffer, 0, workingbuffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
                    {
                        engine.AppendData(workingbuffer, 0, read);
                    }
                }
                else
                {
                    while (!cancellationToken.IsCancellationRequested && (read = stream.Read(workingbuffer, 0, workingbuffer.Length)) != 0)
                    {
                        engine.AppendData(workingbuffer, 0, read);
                    }
                }

                if (pos != -1)
                {
                    stream.Position = pos;
                }
                // Should use tryget instead?
                var hashlen = engine.GetHashAndReset(workingbuffer);
                return new ReadOnlyMemory<byte>(workingbuffer, 0, hashlen);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static FileStream OpenToScan(string localFilePath) => new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 0, true);

            using (var throttleWaiter = (fileCheckThrottleFactor == 0 ? null : new PeriodicTimerWithoutException(TimeSpan.FromMilliseconds(fileCheckThrottleFactor))))
            using (var md5engi = IncrementalHash.CreateHash(HashAlgorithmName.MD5))
            {
                // Reuse obj
                byte[]? scanBuffer = null;
                string hashBuffer = new string(char.MinValue, 32);
                try
                {
                    foreach (var patchItem in headacheMatterAgain)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        var flags = (patchItem.IsRebootData == false) ? fScanClassic : fScanReboot;
                        if ((flags & FileScanFlags.IgnoreHashTableFile) != 0 && IsHashTableFile(patchItem))
                        {
                            continue;
                        }
                        if ((flags & FileScanFlags.DoNotRedownloadNvidiaDlssBin) != 0 && IsDlssBinaryFile(patchItem))
                        {
                            if (File.Exists(Path.Join(dir_pso2bin, patchItem.GetSpanFilenameWithoutAffix())))
                                continue;
                        }
                        // data/win32/2b486d03bca4c2578f9e204b234f389b
                        if (flags == FileScanFlags.MissingFilesOnly)
                        {
                            var localFilename = patchItem.GetFilenameWithoutAffix();
                            string localFilePath = Path.GetFullPath(localFilename, dir_pso2bin);
                            if (!File.Exists(localFilePath))
                            {
                                AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, cancellationToken);
                            }
                        }
                        else
                        {
                            bool flag_cacheOnly = (flags & FileScanFlags.CacheOnly) != 0,
                                flag_forceRefresh = (flags & FileScanFlags.ForceRefreshCache) != 0,
                                flag_useFileSize = (flags & FileScanFlags.FileSizeMismatch) != 0,
                                flag_missingOnly = (flags & FileScanFlags.MissingFilesOnly) != 0,
                                flag_useMd5 = (flags & FileScanFlags.MD5HashMismatch) != 0;

                            if (flag_forceRefresh)
                            {
                                var localFilename = patchItem.GetFilenameWithoutAffix();
                                string localFilePath = Path.GetFullPath(localFilename, dir_pso2bin);
                                // localFilePath = DetermineWhere(patchItem, dir_pso2bin, dir_classic_data, dir_reboot_data, out var isLink);
                                if (!File.Exists(localFilePath))
                                {
                                    AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, cancellationToken);
                                }
                                else
                                {
                                    var localLastModifiedTimeUtc = File.GetLastWriteTimeUtc(localFilePath);

                                    using (var fs = OpenToScan(localFilePath))
                                    {
                                        var localFileLen = fs.Length;
                                        if (localFileLen == patchItem.FileSize)
                                        {
                                            if (flag_useMd5)
                                            {
                                                // Always equal????
                                                scanBuffer = BorrowBufferCertainSize(scanBuffer, ScanBufferSize);
                                                var rawHashBuffer = await Md5ComputeHash(md5engi, fs, scanBuffer, cancellationToken);
                                                var localMd5 = (HashHelper.TryWriteHashToHexString(hashBuffer, rawHashBuffer.Span) ? hashBuffer : Convert.ToHexString(rawHashBuffer.Span));
                                                var bool_compareMD5 = MemoryExtensions.Equals(localMd5, patchItem.MD5.Span, StringComparison.OrdinalIgnoreCase);
                                                duhB.SetPatchItem(new PatchListItem(null, patchItem.RemoteFilename, in localFileLen, localMd5.ToCharArray()), localLastModifiedTimeUtc);

                                                if (bool_compareMD5)
                                                {
                                                    tweakerHashCache?.WriteString(patchItem.GetSpanFilenameWithoutAffix(), localMd5);
                                                }
                                                else
                                                {
                                                    AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, cancellationToken);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, cancellationToken);
                                        }
                                    }
                                }
                                this.OnFileCheckReport(Interlocked.Increment(ref processedFiles));

                                if (throttleWaiter != null)
                                {
                                    await throttleWaiter.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
                                }
                            }
                            else if (flag_cacheOnly)
                            {
                                var localFilename = patchItem.GetFilenameWithoutAffix();
                                // string localFilePath = DetermineWhere(patchItem, dir_pso2bin, dir_classic_data);
                                var localFilePath = Path.GetFullPath(localFilename, dir_pso2bin);
                                var isInCache = duhB.TryGetPatchItem(localFilename, out var cachedHash);
                                // var localLastModifiedTimeUtc = File.GetLastWriteTimeUtc(localFilePath);

                                if (File.Exists(localFilePath))
                                {
                                    if (!isInCache)
                                    {
                                        using (var fs = OpenToScan(localFilePath))
                                        {
                                            fs.GetFileLastWriteTimeUTC(out var localLastModifiedTimeUtc);
                                            scanBuffer = BorrowBufferCertainSize(scanBuffer, ScanBufferSize);
                                            var rawHashBuffer = await Md5ComputeHash(md5engi, fs, scanBuffer, cancellationToken);
                                            var localMd5 = (HashHelper.TryWriteHashToHexString(hashBuffer, rawHashBuffer.Span, out var hashBuffer_length) ? hashBuffer.ToCharArray(0, hashBuffer_length / sizeof(char)).AsMemory() : Convert.ToHexString(rawHashBuffer.Span).AsMemory());
                                            var newCacheItem = new PatchListItem(null, patchItem.RemoteFilename, fs.Length, localMd5);
                                            cachedHash = duhB.SetPatchItem(newCacheItem, localLastModifiedTimeUtc);
                                            tweakerHashCache?.WriteString(newCacheItem.GetSpanFilenameWithoutAffix(), localMd5);
                                            isInCache = true;
                                        }
                                    }

                                    if (!MemoryExtensions.Equals(cachedHash.MD5, patchItem.MD5.Span, StringComparison.OrdinalIgnoreCase))
                                    {
                                        AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, cancellationToken);
                                    }
                                    else
                                    {
                                        tweakerHashCache?.WriteString(patchItem.GetSpanFilenameWithoutAffix(), patchItem.MD5);
                                    }
                                }
                                else
                                {
                                    AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, cancellationToken);
                                }

                                this.OnFileCheckReport(Interlocked.Increment(ref processedFiles));

                                if (throttleWaiter != null)
                                {
                                    await throttleWaiter.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                var localFilename = patchItem.GetFilenameWithoutAffix();
                                var localFilePath = Path.GetFullPath(localFilename, dir_pso2bin);
                                // string localFilePath = DetermineWhere(patchItem, dir_pso2bin, dir_classic_data);
                                if (!File.Exists(localFilePath))
                                {
                                    AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, cancellationToken);
                                }
                                else
                                {
                                    var isInCache = duhB.TryGetPatchItem(localFilename, out var cachedHash);
                                    var localLastModifiedTimeUtc = File.GetLastWriteTimeUtc(localFilePath);
                                    if (isInCache && localLastModifiedTimeUtc != cachedHash.LastModifiedTimeUTC)
                                    {
                                        isInCache = false;
                                    }
                                    FileStream? fs = null;
                                    try
                                    {
                                        if (flag_useFileSize)
                                        {
                                            fs = OpenToScan(localFilePath);
                                            if (flag_useMd5)
                                            {
                                                if (isInCache && localLastModifiedTimeUtc == cachedHash.LastModifiedTimeUTC && fs.Length == cachedHash.FileSize)
                                                {
                                                    if (!MemoryExtensions.Equals(patchItem.MD5.Span, cachedHash.MD5, StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, cancellationToken);
                                                    }
                                                    else
                                                    {
                                                        tweakerHashCache?.WriteString(patchItem.GetSpanFilenameWithoutAffix(), cachedHash.MD5);
                                                    }
                                                }
                                                else
                                                {
                                                    scanBuffer = BorrowBufferCertainSize(scanBuffer, ScanBufferSize);
                                                    var rawHashBuffer = await Md5ComputeHash(md5engi, fs, scanBuffer, cancellationToken);
                                                    var localMd5 = (HashHelper.TryWriteHashToHexString(hashBuffer, rawHashBuffer.Span) ? hashBuffer : Convert.ToHexString(rawHashBuffer.Span));
                                                    if (MemoryExtensions.Equals(patchItem.MD5.Span, localMd5, StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        duhB.SetPatchItem(patchItem, in localLastModifiedTimeUtc);
                                                        tweakerHashCache?.WriteString(patchItem.GetSpanFilenameWithoutAffix(), localMd5);
                                                    }
                                                    else
                                                    {
                                                        AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, cancellationToken);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (fs.Length != patchItem.FileSize)
                                                {
                                                    AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, cancellationToken);
                                                }
                                                else
                                                {
                                                    tweakerHashCache?.WriteString(patchItem.GetSpanFilenameWithoutAffix(), patchItem.MD5);
                                                }
                                            }
                                        }
                                        else if (flag_useMd5)
                                        {
                                            if (isInCache && localLastModifiedTimeUtc == cachedHash.LastModifiedTimeUTC)
                                            {
                                                if (!MemoryExtensions.Equals(patchItem.MD5.Span, cachedHash.MD5, StringComparison.OrdinalIgnoreCase))
                                                {
                                                    AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, cancellationToken);
                                                }
                                                else
                                                {
                                                    tweakerHashCache?.WriteString(patchItem.GetSpanFilenameWithoutAffix(), cachedHash.MD5);
                                                }
                                            }
                                            else
                                            {
                                                fs = OpenToScan(localFilePath);
                                                scanBuffer = BorrowBufferCertainSize(scanBuffer, ScanBufferSize);
                                                var rawHashBuffer = await Md5ComputeHash(md5engi, fs, scanBuffer, cancellationToken);
                                                var localMd5 = (HashHelper.TryWriteHashToHexString(hashBuffer, rawHashBuffer.Span) ? hashBuffer : Convert.ToHexString(rawHashBuffer.Span));
                                                if (MemoryExtensions.Equals(patchItem.MD5.Span, localMd5, StringComparison.OrdinalIgnoreCase))
                                                {
                                                    duhB.SetPatchItem(patchItem, in localLastModifiedTimeUtc);
                                                    tweakerHashCache?.WriteString(patchItem.GetSpanFilenameWithoutAffix(), localMd5);
                                                }
                                                else
                                                {
                                                    AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, cancellationToken);
                                                }
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        fs?.Dispose();
                                    }
                                }
                                this.OnFileCheckReport(Interlocked.Increment(ref processedFiles));

                                if (throttleWaiter != null)
                                {
                                    await throttleWaiter.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    if (scanBuffer != null)
                    {
                        System.Buffers.ArrayPool<byte>.Shared.Return(scanBuffer);
                    }
                }
            }
        }
    }
#nullable restore
}

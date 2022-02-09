using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.PSO2Launcher.Helper;
using Leayal.Shared;
using SymbolicLinkSupport;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
#nullable enable
    partial class GameClientUpdater
    {
        private async Task<PatchListMemory> InnerGetFilelistToScan(GameClientSelection selection, CancellationToken cancellationToken)
        {
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

        private static void RestoreBackups(BackupFileFoundEventArgs e_onBackup, in CancellationToken cancellationToken)
        {
            if (e_onBackup != null && e_onBackup.Handled == false)
            {
                string? dest = null;
                byte[]? buffer = null;
                try
                {
                    foreach (var item in e_onBackup.Items)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
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
                                    if ((e_onBackup.HasRebootBackup && string.Equals(Path.GetFullPath(Path.Combine("data", "win32reboot", "backup"), e_onBackup.Root), dest, StringComparison.OrdinalIgnoreCase))
                                        || e_onBackup.HasClassicBackup && string.Equals(Path.GetFullPath(Path.Combine("data", "win32", "backup"), e_onBackup.Root), dest, StringComparison.OrdinalIgnoreCase))
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
                if (e_onBackup.HasRebootBackup)
                {
                    var reboot = Path.GetFullPath(Path.Combine("data", "win32reboot", "backup"), e_onBackup.Root);
                    if (Directory.Exists(reboot) && !DirectoryHelper.IsDirectoryNotEmpty(reboot))
                    {
                        try
                        {
                            Directory.Delete(reboot, true);
                        }
                        catch { }
                    }
                }

                // Technically, this can't happen as classic files are all non-directory files.
                // But add it, just-in-case.
                // However, it does not hurt if the empty directories still there.
                // So optional, silent the error since it's okay even if it fails.
                if (e_onBackup.HasClassicBackup)
                {
                    var classic = Path.GetFullPath(Path.Combine("data", "win32", "backup"), e_onBackup.Root);
                    if (Directory.Exists(classic) && !DirectoryHelper.IsDirectoryNotEmpty(classic))
                    {
                        try
                        {
                            Directory.Delete(classic, true);
                        }
                        catch { }
                    }
                }
            }
        }

        private async Task<BackupFileFoundEventArgs?> SearchForBackup(string dir_pso2bin, GameClientSelection selection)
        {
            BackupFileFoundEventArgs? e_onBackup = null;
            bool bakExist_classic, bakExist_reboot;
            switch (selection)
            {
                case GameClientSelection.NGS_AND_CLASSIC:
                    bakExist_classic = DirectoryHelper.IsDirectoryExistsAndNotEmpty(Path.Combine(dir_pso2bin, "data", "win32", "backup"));
                    bakExist_reboot = DirectoryHelper.IsDirectoryExistsAndNotEmpty(Path.Combine(dir_pso2bin, "data", "win32reboot", "backup"));
                    if (bakExist_reboot || bakExist_classic)
                    {
                        e_onBackup = new BackupFileFoundEventArgs(dir_pso2bin, bakExist_reboot, bakExist_classic);
                        await this.OnBackupFileFound(e_onBackup);
                    }
                    break;

                case GameClientSelection.NGS_Prologue_Only:
                    bakExist_reboot = DirectoryHelper.IsDirectoryExistsAndNotEmpty(Path.Combine(dir_pso2bin, "data", "win32reboot", "backup"));
                    if (bakExist_reboot)
                    {
                        e_onBackup = new BackupFileFoundEventArgs(dir_pso2bin, bakExist_reboot, false);
                        await this.OnBackupFileFound(e_onBackup);
                    }
                    break;

                case GameClientSelection.Classic_Only:
                    bakExist_classic = DirectoryHelper.IsDirectoryExistsAndNotEmpty(Path.Combine(dir_pso2bin, "data", "win32", "backup"));
                    if (bakExist_classic)
                    {
                        e_onBackup = new BackupFileFoundEventArgs(dir_pso2bin, false, bakExist_classic);
                        await this.OnBackupFileFound(e_onBackup);
                    }
                    break;

                case GameClientSelection.Always_Only:
                    break;

                case GameClientSelection.NGS_Only:
                    bakExist_reboot = DirectoryHelper.IsDirectoryExistsAndNotEmpty(Path.Combine(dir_pso2bin, "data", "win32reboot", "backup"));
                    if (bakExist_reboot)
                    {
                        e_onBackup = new BackupFileFoundEventArgs(dir_pso2bin, bakExist_reboot, false);
                        await this.OnBackupFileFound(e_onBackup);
                    }
                    break;
            }

            return e_onBackup;
        }

        private async Task InnerScanForFilesNeedToDownload(BlockingCollection<DownloadItem> pendingFiles, string dir_pso2bin, string? dir_reboot_data, string? dir_classic_data, PSO2TweakerHashCache? tweakerHashCache, GameClientSelection selection, FileScanFlags flags, IFileCheckHashCache duhB, PatchListBase headacheMatterAgain, InnerDownloadQueueAddCallback onDownloadQueueAdd, CancellationToken cancellationToken)
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

            // var t_alwaysList = this.webclient.GetPatchListAlwaysAsync(patchInfoRoot, cancellationToken);
            var tasksOfLists = new List<Task<PatchListMemory>>(4);

            if (headacheMatterAgain is PatchListMemory patchListMemory)
            {
                this.OnFileCheckBegin(patchListMemory.Count);
            }
            else
            {
                this.OnFileCheckBegin(-1);
            }

            // Begin file check

            // Maybe Enum.HasFlag() is better than this mess????

            bool flag_cacheOnly = flags.HasFlag(FileScanFlags.CacheOnly),
                flag_forceRefresh = flags.HasFlag(FileScanFlags.ForceRefreshCache),
                flag_useFileSize = flags.HasFlag(FileScanFlags.FileSizeMismatch),
                flag_missingOnly = flags.HasFlag(FileScanFlags.MissingFilesOnly),
                flag_useMd5 = flags.HasFlag(FileScanFlags.MD5HashMismatch);

            /*
            static long GetFileSize(ref FileStream fs, string filename)
            {
                if (fs == null)
                {
                    fs = File.OpenRead(filename);
                }
                return fs.Length;
            }

            static Task<string> GetFileMD5(ref FileStream fs, MD5 hashal, string filename, in CancellationToken cancellationToken)
            {
                if (fs == null)
                {
                    fs = File.OpenRead(filename);
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
            static void AddItemToQueue(BlockingCollection<DownloadItem> queue, InnerDownloadQueueAddCallback callback, PatchListItem patchItem, string localFilePath, string dir_pso2bin, string? dir_classic_data, string? dir_reboot_data)
            {
                var linkTo = DetermineWhere(patchItem, dir_pso2bin, dir_classic_data, dir_reboot_data, out var isLink);
                var item = new DownloadItem(patchItem, localFilePath, isLink ? linkTo : null);
                queue.Add(item, CancellationToken.None);
                callback.Invoke(in item);
            }

            using (var md5engi = MD5.Create())
            {
                if (flags == FileScanFlags.MissingFilesOnly)
                {
                    foreach (var patchItem in headacheMatterAgain)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                        var localFilename = patchItem.GetFilenameWithoutAffix();
                        string localFilePath = Path.GetFullPath(localFilename, dir_pso2bin);
                        if (!File.Exists(localFilePath))
                        {
                            AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, dir_reboot_data);
                        }
                    }
                }
                else if (flag_forceRefresh)
                {
                    foreach (var patchItem in headacheMatterAgain)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        var localFilename = patchItem.GetFilenameWithoutAffix();
                        string localFilePath = Path.GetFullPath(localFilename, dir_pso2bin);
                        // localFilePath = DetermineWhere(patchItem, dir_pso2bin, dir_classic_data, dir_reboot_data, out var isLink);
                        if (!File.Exists(localFilePath))
                        {
                            AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, dir_reboot_data);
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
                                        var localMd5 = Convert.ToHexString(md5engi.ComputeHash(fs));

                                        var bool_compareMD5 = string.Equals(localMd5, patchItem.MD5, StringComparison.OrdinalIgnoreCase);
                                        duhB.SetPatchItem(new PatchListItem(null, patchItem.RemoteFilename, in localFileLen, localMd5), localLastModifiedTimeUtc);

                                        if (bool_compareMD5)
                                        {
                                            tweakerHashCache?.WriteString(patchItem.GetSpanFilenameWithoutAffix(), localMd5);
                                        }
                                        else
                                        {
                                            AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, dir_reboot_data);
                                        }
                                    }
                                }
                                else
                                {
                                    AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, dir_reboot_data);
                                }
                            }
                        }
                        this.OnFileCheckReport(Interlocked.Increment(ref processedFiles));
                        if (fileCheckThrottleFactor != 0)
                        {
                            await Task.Delay(fileCheckThrottleFactor, cancellationToken);
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
                        // string localFilePath = DetermineWhere(patchItem, dir_pso2bin, dir_classic_data, dir_reboot_data);
                        var localFilePath = Path.GetFullPath(localFilename, dir_pso2bin);
                        var isNotInCache = !duhB.TryGetPatchItem(localFilename, out var cachedHash);
                        // var localLastModifiedTimeUtc = File.GetLastWriteTimeUtc(localFilePath);

                        if (File.Exists(localFilePath))
                        {
                            if (isNotInCache)
                            {
                                var localLastModifiedTimeUtc = File.GetLastWriteTimeUtc(localFilePath);
                                using (var fs = File.OpenRead(localFilePath))
                                {
                                    var localMd5 = Convert.ToHexString(md5engi.ComputeHash(fs));
                                    var newCacheItem = new PatchListItem(null, patchItem.RemoteFilename, fs.Length, localMd5);
                                    cachedHash = duhB.SetPatchItem(newCacheItem, localLastModifiedTimeUtc);
                                    tweakerHashCache?.WriteString(newCacheItem.GetSpanFilenameWithoutAffix(), localMd5);
                                }
                            }

                            if (isNotInCache)
                            {
                                AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, dir_reboot_data);
                            }
                            else
                            {
                                if (!string.Equals(cachedHash.MD5, patchItem.MD5, StringComparison.OrdinalIgnoreCase))
                                {
                                    AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, dir_reboot_data);
                                }
                                else
                                {
                                    tweakerHashCache?.WriteString(patchItem.GetSpanFilenameWithoutAffix(), patchItem.MD5);
                                }
                            }
                        }
                        else
                        {
                            AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, dir_reboot_data);
                        }

                        this.OnFileCheckReport(Interlocked.Increment(ref processedFiles));
                        if (fileCheckThrottleFactor != 0)
                        {
                            await Task.Delay(fileCheckThrottleFactor, cancellationToken);
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
                        var localFilePath = Path.GetFullPath(localFilename, dir_pso2bin);
                        // string localFilePath = DetermineWhere(patchItem, dir_pso2bin, dir_classic_data, dir_reboot_data);
                        if (!File.Exists(localFilePath))
                        {
                            AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, dir_reboot_data);
                        }
                        else
                        {
                            var isInCache = duhB.TryGetPatchItem(localFilename, out var cachedHash);
                            var localLastModifiedTimeUtc = File.GetLastWriteTimeUtc(localFilePath);
                            if (isInCache && localLastModifiedTimeUtc != cachedHash.LastModifiedTimeUTC)
                            {
                            }
                            FileStream? fs = null;
                            try
                            {
                                if (flag_useFileSize)
                                {
                                    fs = File.OpenRead(localFilePath);
                                    if (flag_useMd5)
                                    {
                                        if (isInCache && localLastModifiedTimeUtc == cachedHash.LastModifiedTimeUTC && fs.Length == cachedHash.FileSize)
                                        {
                                            if (!string.Equals(patchItem.MD5, cachedHash.MD5, StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, dir_reboot_data);
                                            }
                                            else
                                            {
                                                tweakerHashCache?.WriteString(patchItem.GetSpanFilenameWithoutAffix(), cachedHash.MD5);
                                            }
                                        }
                                        else
                                        {
                                            var localMd5 = Convert.ToHexString(md5engi.ComputeHash(fs));
                                            if (string.Equals(patchItem.MD5, localMd5, StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                duhB.SetPatchItem(patchItem, in localLastModifiedTimeUtc);
                                                tweakerHashCache?.WriteString(patchItem.GetSpanFilenameWithoutAffix(), localMd5);
                                            }
                                            else
                                            {
                                                AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, dir_reboot_data);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (fs.Length != patchItem.FileSize)
                                        {
                                            AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, dir_reboot_data);
                                        }
                                        else
                                        {
                                            tweakerHashCache?.WriteString(patchItem.GetSpanFilenameWithoutAffix(), cachedHash.MD5);
                                        }
                                    }
                                }
                                else if (flag_useMd5)
                                {
                                    if (isInCache && localLastModifiedTimeUtc == cachedHash.LastModifiedTimeUTC)
                                    {
                                        if (!string.Equals(patchItem.MD5, cachedHash.MD5, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, dir_reboot_data);
                                        }
                                        else
                                        {
                                            tweakerHashCache?.WriteString(patchItem.GetSpanFilenameWithoutAffix(), cachedHash.MD5);
                                        }
                                    }
                                    else
                                    {
                                        fs = File.OpenRead(localFilePath);
                                        var localMd5 = Convert.ToHexString(md5engi.ComputeHash(fs));
                                        if (string.Equals(patchItem.MD5, localMd5, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            duhB.SetPatchItem(patchItem, in localLastModifiedTimeUtc);
                                            tweakerHashCache?.WriteString(patchItem.GetSpanFilenameWithoutAffix(), localMd5);
                                        }
                                        else
                                        {
                                            AddItemToQueue(pendingFiles, onDownloadQueueAdd, patchItem, localFilePath, dir_pso2bin, dir_classic_data, dir_reboot_data);
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
                        if (fileCheckThrottleFactor != 0)
                        {
                            await Task.Delay(fileCheckThrottleFactor, cancellationToken);
                        }
                    }
                }
                md5engi.Clear();
            }
        }
    }
#nullable restore
}

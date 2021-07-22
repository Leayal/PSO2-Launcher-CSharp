using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.PSO2Launcher.Helper;
using SymbolicLinkSupport;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    partial class GameClientUpdater
    {
        private async Task InnerScanForFilesNeedToDownload(BlockingCollection<DownloadItem> pendingFiles, GameClientSelection selection, FileScanFlags flags, FileCheckHashCache duhB, Action<int> onGetTotalFile, CancellationToken cancellationToken)
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
            var patchInfoRoot = await this.InnerGetPatchRootAsync(cancellationToken);
            // var t_alwaysList = this.webclient.GetPatchListAlwaysAsync(patchInfoRoot, cancellationToken);
            bool bakExist_classic = false;
            bool bakExist_reboot = false;
            Task<PatchListMemory> t_addition = null, t_fulllist;
            BackupFileFoundEventArgs e_onBackup = null;

            switch (selection)
            {
                case GameClientSelection.NGS_AND_CLASSIC:
                    t_fulllist = this.webclient.GetPatchListAllAsync(patchInfoRoot, cancellationToken);
                    if (t_fulllist.Status == TaskStatus.Created)
                    {
                        t_fulllist.Start();
                    }
                    bakExist_classic = IsDirectoryExistsAndNotEmpty(Path.Combine(this.dir_pso2bin, "data", "win32", "backup"));
                    bakExist_reboot = IsDirectoryExistsAndNotEmpty(Path.Combine(this.dir_pso2bin, "data", "win32reboot", "backup"));
                    if (bakExist_reboot || bakExist_classic)
                    {
                        e_onBackup = new BackupFileFoundEventArgs(this.dir_pso2bin, bakExist_reboot, bakExist_classic);
                        await this.OnBackupFileFound(e_onBackup);
                    }
                    break;

                case GameClientSelection.NGS_Prologue_Only:
                    t_fulllist = this.webclient.GetPatchListNGSPrologueAsync(patchInfoRoot, cancellationToken);
                    if (t_fulllist.Status == TaskStatus.Created)
                    {
                        t_fulllist.Start();
                    }
                    bakExist_reboot = IsDirectoryExistsAndNotEmpty(Path.Combine(this.dir_pso2bin, "data", "win32reboot", "backup"));
                    if (bakExist_reboot)
                    {
                        e_onBackup = new BackupFileFoundEventArgs(this.dir_pso2bin, bakExist_reboot, false);
                        await this.OnBackupFileFound(e_onBackup);
                    }
                    break;

                case GameClientSelection.Classic_Only:
                    t_fulllist = this.webclient.GetPatchListClassicAsync(patchInfoRoot, cancellationToken);
                    if (t_fulllist.Status == TaskStatus.Created)
                    {
                        t_fulllist.Start();
                    }
                    bakExist_classic = IsDirectoryExistsAndNotEmpty(Path.Combine(this.dir_pso2bin, "data", "win32", "backup"));
                    if (bakExist_classic)
                    {
                        e_onBackup = new BackupFileFoundEventArgs(this.dir_pso2bin, false, bakExist_classic);
                        await this.OnBackupFileFound(e_onBackup);
                    }
                    break;

                case GameClientSelection.Always_Only:
                    t_fulllist = this.webclient.GetPatchListAlwaysAsync(patchInfoRoot, cancellationToken);
                    if (t_fulllist.Status == TaskStatus.Created)
                    {
                        t_fulllist.Start();
                    }
                    break;

                case GameClientSelection.NGS_Only:
                    // Download both files at the same time.
                    t_addition = this.webclient.GetPatchListNGSPrologueAsync(patchInfoRoot, cancellationToken);
                    if (t_addition.Status == TaskStatus.Created)
                    {
                        t_addition.Start();
                    }
                    t_fulllist = this.webclient.GetPatchListNGSFullAsync(patchInfoRoot, cancellationToken);
                    if (t_fulllist.Status == TaskStatus.Created)
                    {
                        t_fulllist.Start();
                    }
                    bakExist_reboot = IsDirectoryExistsAndNotEmpty(Path.Combine(this.dir_pso2bin, "data", "win32reboot", "backup"));
                    if (bakExist_reboot)
                    {
                        e_onBackup = new BackupFileFoundEventArgs(this.dir_pso2bin, bakExist_reboot, false);
                        await this.OnBackupFileFound(e_onBackup);
                    }
                    break;

                default:
                    // Universe exploded because you handed wrong value.
                    throw new ArgumentOutOfRangeException(nameof(selection));
            }

            if (e_onBackup != null && e_onBackup.Handled == false)
            {
                string dest;
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
                                dest = linkTarget;
                                if (string.Equals(Path.GetPathRoot(dest), Path.GetPathRoot(item.BackupFileSourcePath), StringComparison.OrdinalIgnoreCase))
                                {
                                    File.Move(item.BackupFileSourcePath, dest, true);
                                }
                                else
                                {
                                    try
                                    {
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
                                    }
                                    catch
                                    {
                                        if (File.Exists(item.BackupFileSourcePath))
                                        {
                                            File.Move(item.BackupFileSourcePath, dest, true);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                File.Move(item.BackupFileSourcePath, dest, true);
                            }
                        }
                        else
                        {
                            File.Move(item.BackupFileSourcePath, dest, true);
                        }
                    }
                }
                finally
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            PatchListBase headacheMatterAgain;
            if (t_addition == null)
            {
                headacheMatterAgain = await t_fulllist;
            }
            else
            {
                await Task.WhenAll(t_addition, t_fulllist);
                headacheMatterAgain = PatchListBase.Create(await t_addition, await t_fulllist);
            }

            if (headacheMatterAgain is PatchListMemory patchListMemory)
            {
                onGetTotalFile?.Invoke(patchListMemory.Count);
                this.OnFileCheckBegin(patchListMemory.Count);
            }
            else
            {
                onGetTotalFile?.Invoke(-1);
                this.OnFileCheckBegin(-1);
            }

            await duhB.Load();

            // Begin file check

            // Maybe Enum.HasFlag() is better than this mess????

            int processedCount = 0;

            bool flag_cacheOnly = flags.HasFlag(FileScanFlags.CacheOnly),
                flag_forceRefresh = flags.HasFlag(FileScanFlags.ForceRefreshCache),
                flag_useFileSize = flags.HasFlag(FileScanFlags.FileSizeMismatch),
                flag_missingOnly = flags.HasFlag(FileScanFlags.MissingFilesOnly),
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
                            pendingFiles.Add(new DownloadItem(patchItem, localFilePath, linkTo));
                        }
                        else
                        {
                            pendingFiles.Add(new DownloadItem(patchItem, localFilePath, null));
                        }
                        this.OnDownloadQueueAdded(pendingFiles.Count);
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
                                            pendingFiles.Add(new DownloadItem(patchItem, localFilePath, linkTo));
                                        }
                                        else
                                        {
                                            pendingFiles.Add(new DownloadItem(patchItem, localFilePath, null));
                                        }
                                        this.OnDownloadQueueAdded(pendingFiles.Count);
                                        // this.pendingFiles.Add(patchItem);
                                    }
                                }
                            }
                            else
                            {
                                var linkTo = DetermineWhere(patchItem, this.dir_pso2bin, this.dir_classic_data, this.dir_reboot_data, out var isLink);
                                if (isLink)
                                {
                                    pendingFiles.Add(new DownloadItem(patchItem, localFilePath, linkTo));
                                }
                                else
                                {
                                    pendingFiles.Add(new DownloadItem(patchItem, localFilePath, null));
                                }
                                this.OnDownloadQueueAdded(pendingFiles.Count);
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

                    if (File.Exists(localFilePath))
                    {
                        if (cachedHash == null)
                        {
                            var localLastModifiedTimeUtc = File.GetLastWriteTimeUtc(localFilePath);
                            using (var fs = File.OpenRead(localFilePath))
                            {
                                var localMd5 = MD5Hash.ComputeHashFromFile(fs);
                                cachedHash = await duhB.SetPatchItem(new PatchListItem(null, patchItem.RemoteFilename, fs.Length, localMd5), localLastModifiedTimeUtc);
                            }
                        }

                        if (cachedHash == null)
                        {
                            var linkTo = DetermineWhere(patchItem, this.dir_pso2bin, this.dir_classic_data, this.dir_reboot_data, out var isLink);
                            if (isLink)
                            {
                                pendingFiles.Add(new DownloadItem(patchItem, localFilePath, linkTo));
                            }
                            else
                            {
                                pendingFiles.Add(new DownloadItem(patchItem, localFilePath, null));
                            }
                            this.OnDownloadQueueAdded(pendingFiles.Count);
                        }
                        else
                        {
                            if (!string.Equals(cachedHash.MD5, patchItem.MD5, StringComparison.OrdinalIgnoreCase))
                            {
                                var linkTo = DetermineWhere(patchItem, this.dir_pso2bin, this.dir_classic_data, this.dir_reboot_data, out var isLink);
                                if (isLink)
                                {
                                    pendingFiles.Add(new DownloadItem(patchItem, localFilePath, linkTo));
                                }
                                else
                                {
                                    pendingFiles.Add(new DownloadItem(patchItem, localFilePath, null));
                                }
                                this.OnDownloadQueueAdded(pendingFiles.Count);
                            }
                        }
                    }
                    else
                    {
                        var linkTo = DetermineWhere(patchItem, this.dir_pso2bin, this.dir_classic_data, this.dir_reboot_data, out var isLink);
                        if (isLink)
                        {
                            pendingFiles.Add(new DownloadItem(patchItem, localFilePath, linkTo));
                        }
                        else
                        {
                            pendingFiles.Add(new DownloadItem(patchItem, localFilePath, null));
                        }
                        this.OnDownloadQueueAdded(pendingFiles.Count);
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
                            pendingFiles.Add(new DownloadItem(patchItem, localFilePath, linkTo));
                        }
                        else
                        {
                            pendingFiles.Add(new DownloadItem(patchItem, localFilePath, null));
                        }
                        this.OnDownloadQueueAdded(pendingFiles.Count);
                    }
                    else
                    {
                        if (flag_useFileSize || flag_useMd5)
                        {
                            var cachedHash = await duhB.GetPatchItem(localFilename);
                            var localLastModifiedTimeUtc = File.GetLastWriteTimeUtc(localFilePath);
                            using (var fs = File.OpenRead(localFilePath))
                            {
                                var localFileLen = fs.Length;
                                if (!flag_useFileSize || localFileLen == patchItem.FileSize)
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
                                                pendingFiles.Add(new DownloadItem(patchItem, localFilePath, linkTo));
                                            }
                                            else
                                            {
                                                pendingFiles.Add(new DownloadItem(patchItem, localFilePath, null));
                                            }
                                            this.OnDownloadQueueAdded(pendingFiles.Count);
                                        }
                                    }
                                }
                                else
                                {
                                    string linkTo = DetermineWhere(patchItem, this.dir_pso2bin, this.dir_classic_data, this.dir_reboot_data, out var isLink);
                                    if (isLink)
                                    {
                                        pendingFiles.Add(new DownloadItem(patchItem, localFilePath, linkTo));
                                    }
                                    else
                                    {
                                        pendingFiles.Add(new DownloadItem(patchItem, localFilePath, null));
                                    }
                                    this.OnDownloadQueueAdded(pendingFiles.Count);
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
    }
}

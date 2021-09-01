﻿using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.PSO2Launcher.Helper;
using Leayal.SharedInterfaces;
using SymbolicLinkSupport;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public partial class GameClientUpdater : AsyncDisposeObject
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

        // File check
        private readonly string hashCheckCachePath;
        private Task t_operation;

        // Threadings
        private int flag_operationStarted;

        public string Path_PSO2BIN => this.dir_pso2bin;
        public string? Path_PSO2ClassicData => this.dir_classic_data;
        public string? Path_PSO2RebootData => this.dir_reboot_data;

        public int ConcurrentDownloadCount { get; set; }
        public int ThrottleFileCheckFactor { get; set; }

        // Snail mode (for when internet is extremely unreliable).
        public bool SnailMode { get; set; }

        public GameClientUpdater(string whereIsThePSO2_BIN, string? preference_classicWhere, string? preference_rebootWhere, string hashCheckCache, PSO2HttpClient httpHandler)
        {
            this.hashCheckCachePath = hashCheckCache;
            this.ConcurrentDownloadCount = 0;
            this.ThrottleFileCheckFactor = 0;
            this.dir_pso2bin = Path.GetFullPath(whereIsThePSO2_BIN);
            this.dir_classic_data = string.IsNullOrWhiteSpace(preference_classicWhere) ? null : Path.GetFullPath(preference_classicWhere);
            this.dir_reboot_data = string.IsNullOrWhiteSpace(preference_rebootWhere) ? null : Path.GetFullPath(preference_rebootWhere);
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

        public async Task<PSO2Version> GetRemoteVersionAsync(PatchRootInfo patchInfoRoot, CancellationToken cancellationToken)
        {
            if (patchInfoRoot == null)
            {
                patchInfoRoot = await this.InnerGetPatchRootAsync(false, cancellationToken);
            }
            return await this.webclient.GetPatchVersionAsync(patchInfoRoot, cancellationToken);
        }

        public Task<bool> CheckForPSO2Updates(CancellationToken cancellationToken)
            => this.CheckForPSO2Updates(null, cancellationToken);

        public async Task<bool> CheckForPSO2Updates(PSO2Version? remoteVer, CancellationToken cancellationToken)
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

            if (!PSO2Version.TrySafeParse(in verString, out var localPSO2Ver) || localPSO2Ver != remoteVersion)
            {
                return true;
            }

            return false;
        }

        public async Task ScanAndDownloadFilesAsync(GameClientSelection selection, FileScanFlags flags, CancellationToken cancellationToken)
        {
            if (flags == FileScanFlags.None)
            {
                throw new ArgumentOutOfRangeException(nameof(flags));
            }

            if (Interlocked.CompareExchange(ref this.flag_operationStarted, 1, 0) == 0)
            {
                this.t_operation = Task.Factory.StartNew(async () =>
                {
                    var pendingFiles = new BlockingCollection<DownloadItem>();
                    bool isOperationSuccess = false;
                    FileCheckHashCache duhB = null;
                    PSO2Version ver = default;

                    PatchListBase patchlist = null;

                    ConcurrentBag<PatchListItem> bag_needtodownload = new ConcurrentBag<PatchListItem>(),
                                                bag_success = new ConcurrentBag<PatchListItem>(),
                                                bag_failure = new ConcurrentBag<PatchListItem>();

                    try
                    {
                        duhB = FileCheckHashCache.CreateOrOpen(this.hashCheckCachePath);
                        ver = await GetRemoteVersionAsync(cancellationToken);
                        var t_check = Task.Factory.StartNew(async () =>
                        {
                            try
                            {
                                await this.InnerScanForFilesNeedToDownload(pendingFiles, selection, flags, duhB, newpatchlist =>
                                {
                                    Interlocked.CompareExchange<PatchListBase>(ref patchlist, newpatchlist, null);
                                }, item =>
                                {
                                    bag_needtodownload.Add(item.PatchInfo);
                                    this.OnDownloadQueueAdded();
                                }, cancellationToken);
                            }
                            finally
                            {
                                pendingFiles.CompleteAdding();
                                this.OnFileCheckEnd();
                            }
                        }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current ?? TaskScheduler.Default).Unwrap();

                        var t_download = Task.Factory.StartNew(async () =>
                        {
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
                                    await this.InnerDownloadSingleFile(pendingFiles, duhB, (item, success) =>
                                    {
                                        if (success)
                                        {
                                            bag_success.Add(item.PatchInfo);
                                        }
                                        else
                                        {
                                            bag_failure.Add(item.PatchInfo);
                                        }
                                        this.OnProgressEnd(item.PatchInfo, in success);
                                    }, cancellationToken);
                                }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current ?? TaskScheduler.Default).Unwrap();
                            }

                            await Task.WhenAll(tasks);
                        }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current ?? TaskScheduler.Default).Unwrap();

                        await Task.WhenAll(t_check, t_download);

                        isOperationSuccess = true;
                    }
                    catch
                    {
                        isOperationSuccess = false;
                        throw;
                    }
                    finally
                    {
                        if (duhB != null)
                        {
                            FileCheckHashCache.Close(duhB);
                        }
                        pendingFiles.Dispose();
                        IReadOnlyCollection<PatchListItem> list_all;
                        if (patchlist is PatchListMemory memorylist)
                        {
                            list_all = memorylist;
                        }
                        else
                        {
                            list_all = new List<PatchListItem>(patchlist);
                        }
                        this.OnClientOperationComplete1(selection, list_all, bag_needtodownload, bag_success, bag_failure, ver, isOperationSuccess, cancellationToken);
                        Interlocked.CompareExchange(ref this.flag_operationStarted, 0, 1);
                    }
                }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current ?? TaskScheduler.Default).Unwrap();
            }

            await this.t_operation;
        }

        private static string _prefix_data_classic = Path.Combine("data", "win32");
        private static string _prefix_data_reboot = Path.Combine("data", "win32reboot");

        /// <returns>Full path to a directory.</returns>
        private static string DetermineWhere(PatchListItem item, in string pso2bin, in string? classicData, in string? rebootData, out bool isLink)
        {
            isLink = false;
            var filename = item.GetFilenameWithoutAffix();
            return Path.GetFullPath(filename, pso2bin);
            
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

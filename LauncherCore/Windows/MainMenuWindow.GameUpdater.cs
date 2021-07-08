﻿using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.PSO2Launcher.Helper;
using Leayal.SharedInterfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class MainMenuWindow
    {
        private async void ButtonCheckForUpdate_Click(object sender, RoutedEventArgs e)
        {
            await StartGameClientUpdate(false);
        }

        private void TabGameClientUpdateProgressBar_UpdateCancelClicked(object sender, RoutedEventArgs e)
        {
            if (this.cancelSrc != null)
            {
                if (!this.cancelSrc.IsCancellationRequested)
                {
                    try
                    {
                        this.cancelSrc.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                        this.TabMainMenu.IsSelected = true;
                    }
                }
            }
        }

        private Task<IReadOnlyDictionary<PatchListItem, bool>> QuickCheckFiles(string pso2_bin, IEnumerable<PatchListItem> itemsToCheck)
        {
            return Task.Run<IReadOnlyDictionary<PatchListItem, bool>>(() =>
            {
                var result = new Dictionary<PatchListItem, bool>();
                string filename;
                foreach (var item in itemsToCheck)
                {
                    filename = Path.GetFullPath(item.GetFilenameWithoutAffix(), pso2_bin);
                    if (File.Exists(filename))
                    {
                        using (var fs = File.OpenRead(filename))
                        {
                            if (item.FileSize == 1 && string.Equals(MD5Hash.ComputeHashFromFile(fs), "", StringComparison.OrdinalIgnoreCase))
                            {
                                result.Add(item, true);
                            }
                            else
                            {
                                result.Add(item, false);
                            }
                        }
                    }
                    else
                    {
                        result.Add(item, false);
                    }
                }
                return result;
            });
        }

        private async void TabMainMenu_ButtonScanFixGameDataClicked(object sender, RoutedEventArgs e)
        {
            await StartGameClientUpdate(true);
        }

        private async Task StartGameClientUpdate(bool fixMode = false)
        {
            var dir_pso2bin = this.config_main.PSO2_BIN;
            if (this.pso2Updater == null || string.IsNullOrEmpty(dir_pso2bin))
            {
                if (MessageBox.Show(this, "You have not set the 'pso2_bin' directory.\r\nDo you want to set it now?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    this.TabMainMenu_ButtonManageGameDataClick(null, null);
                }
                return;
            }

            dir_pso2bin = Path.GetFullPath(dir_pso2bin);
            if (!Directory.Exists(dir_pso2bin))
            {
                if (MessageBox.Show(this, "The 'pso2_bin' directory doesn't exist.\r\nContinue anyway (may result in full game download)?\r\nPath: " + dir_pso2bin, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            if (fixMode)
            {
                if (MessageBox.Show(this, "Are you sure you want to begin the file check and repair?\r\n(If the download profile is 'Cache Only', it will use 'Balanced' profile instead to ensure the accuracy of file scan. Therefore, it may take longer time than an usual check for game client updates)", "Prompt", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            GameClientUpdater.OperationCompletedHandler completed = null;
            completed = (sender, cancelled, totalfiles, failedfiles) =>
            {
                this.pso2Updater.OperationCompleted -= completed;
                this.Dispatcher.BeginInvoke(new GameClientUpdater.OperationCompletedHandler((_sender, _cancelled, _totalfiles, _failedfiles) =>
                {
                    this.TabMainMenu.IsSelected = true;
                }), sender, cancelled, totalfiles, failedfiles);
            };
            this.pso2Updater.OperationCompleted += completed;

            CancellationTokenSource currentCancelSrc = null;
            try
            {
                var downloaderProfile = this.config_main.DownloaderProfile;
                var downloadType = this.config_main.DownloadSelection;
                this.TabGameClientUpdateProgressBar.IsIndetermined = true;
                this.TabGameClientUpdateProgressBar.IsSelected = true;
                currentCancelSrc = new CancellationTokenSource();
                this.cancelSrc?.Dispose();
                this.cancelSrc = currentCancelSrc;

                CancellationToken cancelToken = currentCancelSrc.Token;

                if (fixMode || await pso2Updater.CheckForPSO2Updates(cancelToken))
                {
                    this.TabGameClientUpdateProgressBar.SetProgressBarCount(pso2Updater.ConcurrentDownloadCount);

                    if (fixMode && downloaderProfile == FileScanFlags.CacheOnly)
                    {
                        // To ensure the accuracy of the fix. Don't use Cache Only.
                        downloaderProfile = FileScanFlags.Balanced;
                    }
                    var t_fileCheck = pso2Updater.ScanForFilesNeedToDownload(downloadType, downloaderProfile, cancelToken);
                    var t_downloading = pso2Updater.StartDownloadFiles(cancelToken);

                    await Task.WhenAll(t_fileCheck, t_downloading); // Wasting but it's not much.
                }
                else
                {
                    this.pso2Updater.OperationCompleted -= completed;
                    this.TabMainMenu.IsSelected = true;
                }
            }
            catch (FileCheckHashCache.DatabaseErrorException)
            {
                MessageBox.Show(this, "Error occured when opening database. Maybe you're clicking too fast. Please try again but slower.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private GameClientUpdater CreateGameClientUpdater(string directory, string? path_classic_data, string? path_reboot_data, PSO2HttpClient webclient)
        {
            var result = new GameClientUpdater(directory, path_classic_data, path_reboot_data, Path.GetFullPath("leapso2launcher.CheckCache.dat", directory), webclient);
            var throttleFileCheck = this.config_main.DownloaderCheckThrottle;

            var logicalCount = Environment.ProcessorCount;
            var num_concurrentCount = this.config_main.DownloaderConcurrentCount;
            if (num_concurrentCount > logicalCount)
            {
                num_concurrentCount = logicalCount;
            }
            else if (num_concurrentCount <= 0)
            {
                num_concurrentCount = RuntimeValues.GetProcessorCountAuto();
            }

            result.BackupFileFound += this.GameClientUpdater_BackupFileFound;

            result.ConcurrentDownloadCount = num_concurrentCount;
            result.ThrottleFileCheckFactor = throttleFileCheck;

            var bagFree = new ConcurrentBag<int>();
            ConcurrentDictionary<PatchListItem, int> dictionaryInUse = dictionaryInUse = new ConcurrentDictionary<PatchListItem, int>(result.ConcurrentDownloadCount, result.ConcurrentDownloadCount);
            for (int i = 0; i < result.ConcurrentDownloadCount; i++)
            {
                bagFree.Add(i);
            }

            result.ProgressReport += (PatchListItem file, in long value) =>
            {
                this.Dispatcher.BeginInvoke(new GameClientUpdater.ProgressReportHandler((PatchListItem _file, in long _value) =>
                {
                    if (dictionaryInUse.TryGetValue(file, out var index))
                    {
                        this.TabGameClientUpdateProgressBar.SetProgressValue(index, _value);
                    }
                }), file, value);
            };
            result.ProgressBegin += (PatchListItem file, in long maximum) =>
            {
                this.Dispatcher.Invoke(new GameClientUpdater.ProgressBeginHandler((PatchListItem _file, in long _maximum) =>
                {
                    if (bagFree.TryTake(out var index))
                    {
                        if (dictionaryInUse.TryAdd(_file, index))
                        {
                            this.TabGameClientUpdateProgressBar.SetProgressText(index, _file.GetFilenameWithoutAffix());
                            this.TabGameClientUpdateProgressBar.SetProgressMaximum(index, _maximum);
                        }
                    }
                }), file, maximum);
            };

            result.ProgressEnd += (PatchListItem file, in bool success) =>
            {
                this.Dispatcher.Invoke(new GameClientUpdater.ProgressEndHandler((PatchListItem _file, in bool _success) =>
                {
                    this.TabGameClientUpdateProgressBar.IncreaseDownloadedCount();
                    if (dictionaryInUse.TryRemove(_file, out var index))
                    {
                        this.TabGameClientUpdateProgressBar.SetProgressText(index, string.Empty);
                        bagFree.Add(index);
                    }
                }), file, success);
            };

            result.FileCheckBegin += (sender, totalfilecount) =>
            {
                this.Dispatcher.Invoke(new GameClientUpdater.FileCheckBeginHandler((_sender, _total) =>
                {
                    this.TabGameClientUpdateProgressBar.ResetDownloadCount();
                    this.TabGameClientUpdateProgressBar.TopProgressBar.Text = "Checking file";
                    this.TabGameClientUpdateProgressBar.TopProgressBar.progressbar.Value = 0;
                    this.TabGameClientUpdateProgressBar.TopProgressBar.ShowDetailedProgressPercentage = false;
                    if (_total == -1)
                    {
                        this.TabGameClientUpdateProgressBar.TopProgressBar.progressbar.Maximum = 100;
                    }
                    else
                    {
                        this.TabGameClientUpdateProgressBar.TopProgressBar.progressbar.Maximum = _total;
                        this.TabGameClientUpdateProgressBar.TopProgressBar.ShowDetailedProgressPercentage = true;
                        result.FileCheckReport += (sender, currentfilecount) =>
                        {
                            this.Dispatcher.BeginInvoke(new GameClientUpdater.FileCheckBeginHandler((_sender, _current) =>
                            {
                                this.TabGameClientUpdateProgressBar.TopProgressBar.progressbar.Value = _current;
                            }), sender, currentfilecount);
                        };
                    }
                    this.TabGameClientUpdateProgressBar.IsIndetermined = false;
                }), sender, totalfilecount);
            };

            result.DownloadQueueAdded += (GameClientUpdater sender, in int total) =>
            {
                this.Dispatcher.BeginInvoke((Action<int>)((_total) =>
                {
                    this.TabGameClientUpdateProgressBar.IncreaseNeedToDownloadCount();
                }), total);
            };

            result.FileCheckEnd += (sender) =>
            {
                this.Dispatcher.Invoke(new GameClientUpdater.FileCheckEndHandler((_sender) =>
                {
                    this.TabGameClientUpdateProgressBar.TopProgressBar.Text = "Checking completed. Waiting for downloads to complete.";
                    this.TabGameClientUpdateProgressBar.TopProgressBar.ShowDetailedProgressPercentage = false;
                }), sender);
            };
            return result;
        }

        private async Task GameClientUpdater_BackupFileFound(GameClientUpdater sender, GameClientUpdater.BackupFileFoundEventArgs e)
        {
            TaskCompletionSource<bool?> tsrc = new TaskCompletionSource<bool?>();
            _ = this.Dispatcher.BeginInvoke((Action)delegate
            {
                string msg;
                if (e.HasClassicBackup && e.HasRebootBackup)
                {
                    msg = "Found backup for classic and NGS files.\r\nDo you want to restore the backup?";
                }
                else
                {
                    if (e.HasClassicBackup)
                    {
                        msg = "Found backup for classic files.\r\nDo you want to restore the backup?";
                    }
                    else if (e.HasRebootBackup)
                    {
                        msg = "Found backup for NGS files.\r\nDo you want to restore the backup?";
                    }
                    else
                    {
                        msg = null;
                    }
                }
                if (msg != null)
                {
                    if (MessageBox.Show(this, msg, "Prompt", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        tsrc.SetResult(false);
                    }
                    else
                    {
                        tsrc.SetResult(null);
                    }
                }
                else
                {
                    tsrc.SetResult(false);
                }
            });
            e.Handled = await tsrc.Task;
        }
    }
}

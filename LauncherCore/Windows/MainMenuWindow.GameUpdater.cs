using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.PSO2Launcher.Core.UIElements;
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
            await StartGameClientUpdate(false, true);
        }

        private void TabGameClientUpdateProgressBar_UpdateCancelClicked(object sender, RoutedEventArgs e)
        {
            var cancelsrc = this.cancelSrc_gameupdater;
            if (cancelsrc != null)
            {
                try
                {
                    if (!cancelsrc.IsCancellationRequested)
                    {
                        cancelsrc.Cancel();
                    }
                }
                catch (ObjectDisposedException)
                {
                    this.TabMainMenu.IsSelected = true;
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

        private async void TabMainMenu_ButtonScanFixGameDataClicked(object sender, ButtonScanFixGameDataClickRoutedEventArgs e)
        {
            await StartGameClientUpdate(true, false, e.SelectedMode);
        }

        private async Task StartGameClientUpdate(bool fixMode = false, bool promptBeforeUpdate = false, GameClientSelection selection = GameClientSelection.Auto)
        {
            string dir_pso2bin = this.config_main.PSO2_BIN;
            if (string.IsNullOrEmpty(dir_pso2bin))
            {
                var aaa = new Prompt_PSO2BinIsNotSet();
                switch (aaa.ShowCustomDialog(this))
                {
                    case true:
                        this.TabMainMenu_ButtonInstallPSO2_Clicked(this.TabMainMenu, null);
                        break;
                    case false:
                        this.TabMainMenu_ButtonManageGameDataClick(this.TabMainMenu, null);
                        break;
                }
                /*
                if (MessageBox.Show(this, "You have not set the 'pso2_bin' directory.\r\nDo you want to set it now?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    this.TabMainMenu_ButtonManageGameDataClick(this.TabMainMenu, null);
                }
                */
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

            string dir_classic_data = this.config_main.PSO2Enabled_Classic ? this.config_main.PSO2Directory_Classic : null,
                dir_reboot_data = this.config_main.PSO2Enabled_Reboot ? this.config_main.PSO2Directory_Reboot : null;
            dir_classic_data = string.IsNullOrWhiteSpace(dir_classic_data) ? null : Path.GetFullPath(dir_classic_data, dir_pso2bin);
            dir_reboot_data = string.IsNullOrWhiteSpace(dir_reboot_data) ? null : Path.GetFullPath(dir_reboot_data, dir_pso2bin);

            var downloaderProfile = this.config_main.DownloaderProfile;
            var conf_DownloadType = this.config_main.DownloadSelection;
            GameClientSelection downloadType;
            switch (selection)
            {
                case GameClientSelection.Auto:
                    downloadType = conf_DownloadType;
                    break;
                case GameClientSelection.NGS_Only:
                    if (conf_DownloadType == GameClientSelection.NGS_Prologue_Only)
                    {
                        downloadType = GameClientSelection.NGS_Prologue_Only;
                    }
                    else
                    {
                        downloadType = GameClientSelection.NGS_Only;
                    }
                    break;
                default:
                    downloadType = selection;
                    break;
            }

            if (fixMode)
            {
                var sb = new StringBuilder("Are you sure you want to begin the file check and repair");
                switch (downloadType)
                {
                    case GameClientSelection.NGS_Prologue_Only:
                        sb.Append(" for the NGS Prologue files");
                        break;
                    case GameClientSelection.NGS_Only:
                        sb.Append(" for all NGS files");
                        break;
                    case GameClientSelection.Classic_Only:
                        sb.Append(" for all Classic files");
                        break;
                    case GameClientSelection.NGS_AND_CLASSIC:
                        sb.Append(" for all NGS and Classic files");
                        break;
                }
                sb.Append('?');

                if (selection == GameClientSelection.Classic_Only && conf_DownloadType != GameClientSelection.NGS_AND_CLASSIC)
                {
                    sb.AppendLine();
                    sb.Append("(Your setting has been set to ignore Classic files. However, you have selected scanning including Classic files. If you continue, your game client may become full NGS and Classic game)");
                }

                if (downloaderProfile == FileScanFlags.CacheOnly)
                {
                    sb.AppendLine();
                    sb.Append("(If the download profile is 'Cache Only', it will use 'Balanced' profile instead to ensure the accuracy of file scan. Therefore, it may take longer time than an usual check for game client updates)");
                }

                if (MessageBox.Show(this, sb.ToString(), "Prompt", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            GameClientUpdater.OperationCompletedHandler completed = null;
            completed = (sender, cancelled, patchlist, required_download, success_list, failure_list) =>
            {
                this.pso2Updater.OperationCompleted -= completed;
                this.Dispatcher.InvokeAsync(delegate
                {
                    this.TabMainMenu.IsSelected = true;
                });
            };
            this.pso2Updater.OperationCompleted += completed;

            CancellationTokenSource currentCancelSrc = null;
            try
            {
                this.TabGameClientUpdateProgressBar.IsIndetermined = true;
                this.TabGameClientUpdateProgressBar.IsSelected = true;
                currentCancelSrc = CancellationTokenSource.CreateLinkedTokenSource(this.cancelAllOperation.Token);
                this.cancelSrc_gameupdater?.Dispose();
                this.cancelSrc_gameupdater = currentCancelSrc;

                CancellationToken cancelToken = currentCancelSrc.Token;

                PSO2Version? ver;
                bool newVer = false;
                if (fixMode)
                {
                    ver = null;
                    newVer = true; // Force it to go ahead.
                }
                else
                {
                    await this.CreateNewParagraphInLog(writer =>
                    {
                        writer.Write("[GameUpdater] Checking for PSO2 game client updates...");
                    });
                    var version = await this.pso2Updater.GetRemoteVersionAsync(cancelToken);
                    ver = version;
                    newVer = await this.pso2Updater.CheckForPSO2Updates(dir_pso2bin, version, cancelToken);
                }
                

                if (newVer)
                {
                    if (promptBeforeUpdate)
                    {
                        string msg;
                        if (ver.HasValue)
                        {
                            msg = $"Launcher has found updates for PSO2 game client (v{ver.Value}).\r\nDo you want to perform update?";
                        }
                        else
                        {
                            msg = $"Launcher has found updates for PSO2 game client.\r\nDo you want to perform update?";
                        }
                        if (MessageBox.Show(this, msg, "Prompt", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                        {
                            currentCancelSrc.Dispose();
                            this.cancelSrc_gameupdater = null;
                            this.pso2Updater.OperationCompleted -= completed;
                            this.TabMainMenu.IsSelected = true;
                            return;
                        }
                    }
                    this.TabGameClientUpdateProgressBar.SetProgressBarCount(pso2Updater.ConcurrentDownloadCount);

                    if (fixMode && downloaderProfile == FileScanFlags.CacheOnly)
                    {
                        // To ensure the accuracy of the fix. Don't use Cache Only.
                        downloaderProfile = FileScanFlags.Balanced;
                    }
                    if (fixMode)
                    {
                        await this.CreateNewParagraphInLog(writer =>
                        {
                            writer.Write("[GameUpdater] Begin game client's files scanning and downloading...");
                        });
                    }
                    else
                    {
                        await this.CreateNewParagraphInLog(writer =>
                        {
                            writer.Write("[GameUpdater] Begin game client's updating progress...");
                        });
                    }
                    await this.pso2Updater.ScanAndDownloadFilesAsync(dir_pso2bin, dir_reboot_data, dir_classic_data, downloadType, downloaderProfile, cancelToken);
                }
                else
                {
                    currentCancelSrc.Dispose();
                    this.cancelSrc_gameupdater = null;
                    this.pso2Updater.OperationCompleted -= completed;
                    this.TabMainMenu.IsSelected = true;
                    if (!fixMode)
                    {
                        await this.CreateNewParagraphInLog(writer =>
                        {
                            writer.Write("[GameUpdater] PSO2 client is already up-to-date");
                        });
                    }
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (FileCheckHashCache.DatabaseErrorException)
            {
                MessageBox.Show(this, "Error occured when opening database. Maybe you're clicking too fast. Please try again but slower.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                currentCancelSrc?.Dispose();
                this.cancelSrc_gameupdater = null;
                this.pso2Updater.OperationCompleted -= completed;
                this.TabMainMenu.IsSelected = true;
            }
        }

        private GameClientUpdater CreateGameClientUpdater(PSO2HttpClient webclient)
        {
            var result = new GameClientUpdater(webclient);
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
            DebounceDispatcher[] debouncers = null;
            ConcurrentDictionary<PatchListItem, int> dictionaryInUse = dictionaryInUse = new ConcurrentDictionary<PatchListItem, int>(result.ConcurrentDownloadCount, result.ConcurrentDownloadCount);
            for (int i = 0; i < result.ConcurrentDownloadCount; i++)
            {
                bagFree.Add(i);
            }

            result.OperationCompleted += new GameClientUpdater.OperationCompletedHandler((sender, iscancelled, patchlist, required_download, success_list, failure_list) =>
            {
                this.Dispatcher.Invoke((Action)delegate
                {
                    if (debouncers != null)
                    {
                        var oldOnes = debouncers;
                        debouncers = null;
                        for (int i = 0; i < oldOnes.Length; i++)
                        {
                            oldOnes[i].Dispose();
                        }
                    }
                }, System.Windows.Threading.DispatcherPriority.Send);

                long totalsizedownloaded = 0L;
                foreach (var item in success_list)
                {
                    totalsizedownloaded += item.FileSize;
                }
                var totalsizedownloadedtext = Leayal.Shared.NumericHelper.ToHumanReadableFileSize(in totalsizedownloaded);

                _ = this.CreateNewParagraphInLog(writer =>
                {
                    if (iscancelled)
                    {
                        if (success_list.Count == 0)
                        {
                            writer.Write($"[GameUpdater] User cancelled the updating progress. No files downloaded before cancelled.");
                        }
                        else if (success_list.Count == 1)
                        {
                            writer.Write($"[GameUpdater] User cancelled the updating progress. Downloaded 1 file ({totalsizedownloadedtext}) before cancelled.");
                        }
                        else
                        {
                            writer.Write($"[GameUpdater] User cancelled the updating progress. Downloaded {success_list.Count} files ({totalsizedownloadedtext}) before cancelled.");
                        }
                    }
                    else
                    {
                        if (required_download.Count == 0)
                        {
                            writer.Write("[GameUpdater] PSO2 game client has all files updated. There are no files need to be downloaded.");
                        }
                        else
                        {
                            if (failure_list.Count != 0)
                            {
                                if (failure_list.Count == 1)
                                {
                                    writer.Write($"[GameUpdater] PSO2 game client has been updated (Downloaded {totalsizedownloadedtext}). However, there are 1 file which couldn't be downloaded");
                                }
                                else
                                {
                                    writer.Write($"[GameUpdater] PSO2 game client has been updated (Downloaded {totalsizedownloadedtext}). However, there are {failure_list.Count} files which couldn't be downloaded");
                                }
                            }
                            else
                            {
                                writer.Write($"[GameUpdater] PSO2 game client has been updated successfully (All files ({totalsizedownloadedtext}) downloaded)");
                            }
                        }
                    }
                });
            });

            result.ProgressReport += (PatchListItem file, in long value) =>
            {
                if (dictionaryInUse.TryGetValue(file, out var index))
                {
                    if (debouncers != null)
                    {
                        var val = value;
                        var debouncer = debouncers[index + 1];
                        debouncer.ThrottleEx(30, delegate
                        {
                            this.TabGameClientUpdateProgressBar.SetProgressValue(index, val);
                            /*
                            this.Dispatcher.BeginInvoke(new GameClientUpdater.ProgressReportHandler((PatchListItem _file, in long _value) =>
                            {
                                this.TabGameClientUpdateProgressBar.SetProgressValue(index, _value);
                            }), file, val);
                            */
                        });
                    }
                }
            };
            result.ProgressBegin += (PatchListItem file, in long maximum) =>
            {
                this.Dispatcher.Invoke(new GameClientUpdater.ProgressBeginHandler((PatchListItem _file, in long _maximum) =>
                {
                    if (bagFree.TryTake(out var index))
                    {
                        if (dictionaryInUse.TryAdd(_file, index))
                        {
                            // this.TabGameClientUpdateProgressBar.SetProgressValue(index, 0);
                            this.TabGameClientUpdateProgressBar.SetProgressText(index, _file.GetFilenameWithoutAffix());
                            this.TabGameClientUpdateProgressBar.SetProgressMaximum(index, _maximum);
                            this.TabGameClientUpdateProgressBar.SetProgressTextVisible(index, true);
                        }
                    }
                }), file, maximum);
            };

            result.ProgressEnd += (PatchListItem file, in bool success) =>
            {
                if (success)
                {
                    this.TabGameClientUpdateProgressBar.IncreaseDownloadedCount();
                    this.TabGameClientUpdateProgressBar.IncreaseDownloadedBytesCount(in file.FileSize);
                }
                this.Dispatcher.Invoke(new GameClientUpdater.ProgressEndHandler((PatchListItem _file, in bool _success) =>
                {
                    if (dictionaryInUse.TryRemove(_file, out var index))
                    {
                        debouncers[index].Stop();
                        this.TabGameClientUpdateProgressBar.SetProgressTextVisible(index, false);
                        this.TabGameClientUpdateProgressBar.SetProgressText(index, string.Empty);
                        this.TabGameClientUpdateProgressBar.SetProgressValue(index, 0);
                        bagFree.Add(index);
                    }
                }), file, success);
            };

            result.FileCheckBegin += (sender, totalfilecount) =>
            {
                this.Dispatcher.Invoke(new GameClientUpdater.FileCheckBeginHandler((_sender, _total) =>
                {
                    if (debouncers != null)
                    {
                        for (int i = 0; i < debouncers.Length; i++)
                        {
                            debouncers[i].Dispose();
                        }
                    }
                    debouncers = new DebounceDispatcher[result.ConcurrentDownloadCount + 1];
                    for (int i = 0; i < debouncers.Length; i++)
                    {
                        debouncers[i] = new DebounceDispatcher(this.Dispatcher);
                    }
                    this.TabGameClientUpdateProgressBar.ResetDownloadCount();
                    for (int index = 0; index < result.ConcurrentDownloadCount; index++)
                    {
                        this.TabGameClientUpdateProgressBar.SetProgressTextVisible(index, false);
                        this.TabGameClientUpdateProgressBar.SetProgressText(index, string.Empty);
                        this.TabGameClientUpdateProgressBar.SetProgressValue(index, 0);
                    }
                    this.TabGameClientUpdateProgressBar.TopProgressBar.Text = "Checking file";
                    this.TabGameClientUpdateProgressBar.TopProgressBar.ProgressBar.Value = 0;
                    this.TabGameClientUpdateProgressBar.TopProgressBar.ShowDetailedProgressPercentage = false;
                    if (_total == -1)
                    {
                        this.TabGameClientUpdateProgressBar.TopProgressBar.ProgressBar.Maximum = 100;
                    }
                    else
                    {
                        this.TabGameClientUpdateProgressBar.TopProgressBar.ProgressBar.Maximum = _total;
                        this.TabGameClientUpdateProgressBar.TopProgressBar.ShowDetailedProgressPercentage = true;
                        result.FileCheckReport += (sender, currentfilecount) =>
                        {
                            var debouncer = debouncers[0];
                            debouncer.Throttle(30, delegate
                            {
                                this.TabGameClientUpdateProgressBar.TopProgressBar.ProgressBar.Value = currentfilecount;
                                /*
                                this.Dispatcher.BeginInvoke(new GameClientUpdater.FileCheckBeginHandler((_sender, _current) =>
                                {
                                    this.TabGameClientUpdateProgressBar.TopProgressBar.ProgressBar.Value = _current;
                                }), sender, currentfilecount);
                                */
                            });
                            
                        };
                    }
                    this.TabGameClientUpdateProgressBar.IsIndetermined = false;
                }), sender, totalfilecount);
            };

            result.DownloadQueueAdded += (GameClientUpdater sender) =>
            {
                this.TabGameClientUpdateProgressBar.IncreaseNeedToDownloadCount();
                /*
                this.Dispatcher.InvokeAsync(delegate
                {
                    this.TabGameClientUpdateProgressBar.IncreaseNeedToDownloadCount();
                });
                */
            };

            result.FileCheckEnd += (sender) =>
            {
                this.Dispatcher.Invoke(new GameClientUpdater.FileCheckEndHandler((_sender) =>
                {
                    var debouncer = debouncers[0];
                    debouncer?.Stop();
                    this.TabGameClientUpdateProgressBar.TopProgressBar.Text = "Checking completed. Waiting for downloads to complete.";
                    // this.TabGameClientUpdateProgressBar.TopProgressBar.ShowDetailedProgressPercentage = false;
                    this.TabGameClientUpdateProgressBar.TopProgressBar.ProgressBar.Value = this.TabGameClientUpdateProgressBar.TopProgressBar.ProgressBar.Maximum;
                }), sender);
            };
            return result;
        }

        private async Task GameClientUpdater_BackupFileFound(GameClientUpdater sender, GameClientUpdater.BackupFileFoundEventArgs e)
        {
            TaskCompletionSource<bool?> tsrc = new TaskCompletionSource<bool?>();
            _ = this.Dispatcher.InvokeAsync(delegate
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

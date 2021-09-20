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
        private readonly ConcurrentDictionary<PatchListItem, int> gameupdater_dictionaryInUse;

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
            else
            {
                this.TabMainMenu.IsSelected = true;
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
                this.Dispatcher.TryInvoke(delegate
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
                    this.CreateNewParagraphInLog("[GameUpdater] Checking for PSO2 game client updates...");
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
                    // this.TabGameClientUpdateProgressBar.SetProgressBarCount(pso2Updater.ConcurrentDownloadCount);

                    if (fixMode && downloaderProfile == FileScanFlags.CacheOnly)
                    {
                        // To ensure the accuracy of the fix. Don't use Cache Only.
                        downloaderProfile = FileScanFlags.Balanced;
                    }
                    if (fixMode)
                    {
                        this.CreateNewParagraphInLog("[GameUpdater] Begin game client's files scanning and downloading...");
                    }
                    else
                    {
                        this.CreateNewParagraphInLog("[GameUpdater] Begin game client's updating progress...");
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
                        this.CreateNewParagraphInLog("[GameUpdater] PSO2 client is already up-to-date");
                    }
                }
            }
            catch (TaskCanceledException) { }
            catch (AggregateException ex)
            {
                foreach (var innerex in ex.InnerExceptions)
                {
                    if (innerex is TaskCanceledException || innerex is OperationCanceledException)
                    {
                        continue;
                    }
                    else
                    {
                        this.CreateNewParagraphInLog("[GameUpdater] An unknown error occured in operation. Error message: " + ex.Message);
                        MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                    }
                }
            }
            catch (FileCheckHashCache.DatabaseErrorException)
            {
                MessageBox.Show(this, "Error occured when opening database. Maybe you're clicking too fast. Please try again but slower.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                this.CreateNewParagraphInLog("[GameUpdater] An unknown error occured in operation. Error message: " + ex.Message);
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

        /// <remarks>Don't call this while the component is doing any operations.</remarks>
        private void RefreshGameUpdaterOptions() => this.RefreshGameUpdaterOptions(this.pso2Updater);

        /// <remarks>Don't call this while the component is doing any operations.</remarks>
        private void RefreshGameUpdaterOptions(GameClientUpdater updater)
        {
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

            updater.ConcurrentDownloadCount = num_concurrentCount;
            updater.ThrottleFileCheckFactor = throttleFileCheck;

            // this.TabGameClientUpdateProgressBar.SetProgressBarCount(num_concurrentCount);
        }

        private GameClientUpdater CreateGameClientUpdater(PSO2HttpClient webclient)
        {
            var result = new GameClientUpdater(webclient);

            this.RefreshGameUpdaterOptions(result);

            result.BackupFileFound += this.GameClientUpdater_BackupFileFound;
            result.OperationCompleted += this.GameUpdaterComponent_OperationCompleted;
            result.ProgressReport += this.GameUpdaterComponent_ProgressReport;
            result.ProgressBegin += this.GameUpdaterComponent_ProgressBegin;
            result.ProgressEnd += this.GameUpdaterComponent_ProgressEnd;
            result.FileCheckBegin += this.GameUpdaterComponent_FileCheckBegin;
            result.DownloadQueueAdded += this.GameUpdaterComponent_DownloadQueueAdded;
            result.FileCheckEnd += this.GameUpdaterComponent_FileCheckEnd;

            return result;
        }

        public void GameUpdaterComponent_ProgressReport(PatchListItem file, in long downloadedByteCount)
        {
            if (this.gameupdater_dictionaryInUse.TryGetValue(file, out var index))
            {
                this.TabGameClientUpdateProgressBar.SetSubProgressValue(index, Convert.ToDouble(downloadedByteCount));
            }
        }

        public void GameUpdaterComponent_ProgressBegin(PatchListItem file, in long byteCount)
        {

            if (this.TabGameClientUpdateProgressBar.Book_A_Slot(out var index, file.GetFilenameWithoutAffix(), Convert.ToDouble(byteCount)))
            {
                this.gameupdater_dictionaryInUse.AddOrUpdate(file, index, (key, existing) => index);
            }
        }

        public void GameUpdaterComponent_ProgressEnd(PatchListItem file, in bool success)
        {
            if (success)
            {
                this.TabGameClientUpdateProgressBar.IncreaseDownloadedCount(in file.FileSize);
            }
            if (this.gameupdater_dictionaryInUse.TryRemove(file, out var index))
            {
                this.TabGameClientUpdateProgressBar.ResetSubDownloadState(in index);
            }
        }

        private void GameUpdaterComponent_FileCheckBegin(GameClientUpdater sender, int total)
        {
            if (this.Dispatcher.CheckAccess())
            {
                this.gameupdater_dictionaryInUse.Clear();
                var tab = this.TabGameClientUpdateProgressBar;
                tab.SetProgressBarCount(sender.ConcurrentDownloadCount);
                tab.ResetAllSubDownloadState();
                // this.TabGameClientUpdateProgressBar.ResetMainProgressBarState();

                if (total == -1)
                {
                    tab.UpdateMainProgressBarState("Check file", 100d, false);
                    // Not needed but it would does nothing if the delegate is empty. Hence, safe.
                    sender.FileCheckReport -= this.GameUpdaterComponent_FileCheckReport;
                }
                else
                {
                    tab.UpdateMainProgressBarState("Check file", Convert.ToDouble(total), true);
                    sender.FileCheckReport += this.GameUpdaterComponent_FileCheckReport;
                }
                tab.IsIndetermined = false;
            }
            else
            {
                this.Dispatcher.Invoke(new Action<GameClientUpdater, int>(this.GameUpdaterComponent_FileCheckBegin), new object[] { sender, total });
            }
        }

        private void GameUpdaterComponent_FileCheckReport(GameClientUpdater sender, in int current)
        {
            this.TabGameClientUpdateProgressBar.SetMainProgressBarValue(Convert.ToDouble(current));
        }

        private void GameUpdaterComponent_DownloadQueueAdded(GameClientUpdater sender)
        {
            this.TabGameClientUpdateProgressBar.IncreaseNeedToDownloadCount();
        }

        private void GameUpdaterComponent_FileCheckEnd(GameClientUpdater sender)
        {
            sender.FileCheckReport -= this.GameUpdaterComponent_FileCheckReport;
            this.Dispatcher.TryInvokeSync(delegate
            {
                const double val = 1d;
                this.TabGameClientUpdateProgressBar.SetMainProgressBarValue(val);
                this.TabGameClientUpdateProgressBar.UpdateMainProgressBarState("Checking completed. Waiting for downloads to complete.", val, false);
            });
        }

        private void GameUpdaterComponent_OperationCompleted(GameClientUpdater sender, bool isCancelled, IReadOnlyCollection<PatchListItem> patchlist, IReadOnlyCollection<PatchListItem> download_required_list, IReadOnlyCollection<PatchListItem> successList, IReadOnlyCollection<PatchListItem> failureList)
        {
            long totalsizedownloaded = 0L;
            foreach (var item in successList)
            {
                totalsizedownloaded += item.FileSize;
            }
            var totalsizedownloadedtext = Shared.NumericHelper.ToHumanReadableFileSize(in totalsizedownloaded);

            var successCount = successList.Count;
            var requiredCount = download_required_list.Count;
            var failureCount = failureList.Count;

            Guid dialogguid;
            if (requiredCount == 0)
            {
                dialogguid = Guid.Empty;
            }
            else
            {
                // Very expensive
                HashSet<PatchListItem> lookup_success = new HashSet<PatchListItem>(successList), lookup_failure = new HashSet<PatchListItem>(failureList);
                var dictionary = new Dictionary<GameClientUpdateResultLogDialog.PatchListItemLogData, bool?>(download_required_list.Count);
                foreach (var itemrecord in download_required_list)
                {
                    if (lookup_success.Contains(itemrecord))
                    {
                        dictionary.Add(new GameClientUpdateResultLogDialog.PatchListItemLogData(itemrecord.GetFilenameWithoutAffix(), itemrecord.FileSize), true);
                    }
                    else if (lookup_failure.Contains(itemrecord))
                    {
                        dictionary.Add(new GameClientUpdateResultLogDialog.PatchListItemLogData(itemrecord.GetFilenameWithoutAffix(), itemrecord.FileSize), false);
                    }
                    else
                    {
                        dictionary.Add(new GameClientUpdateResultLogDialog.PatchListItemLogData(itemrecord.GetFilenameWithoutAffix(), itemrecord.FileSize), null);
                    }
                }
                dialogguid = (Guid)this.Dispatcher.Invoke(new Func<bool, int, IReadOnlyDictionary<GameClientUpdateResultLogDialog.PatchListItemLogData, bool?>, Guid>((_cancelled, patchlist_count, _scannedinfo) =>
                {
                    var factory = new GameClientUpdateResultLogDialogFactory(in _cancelled, in patchlist_count, _scannedinfo);
                    var result = factory.Id;
                    this.dialogReferenceByUUID.Add(result, factory);
                    return result;
                }), new object[] { isCancelled, patchlist.Count, (IReadOnlyDictionary<GameClientUpdateResultLogDialog.PatchListItemLogData, bool?>)dictionary });
            }

            Uri crafted;
            if (dialogguid != Guid.Empty)
            {
                if (!Uri.TryCreate(StaticResources.Url_ShowLogDialogFromGuid, dialogguid.ToString(), out crafted))
                {
                    this.Dispatcher.BeginInvoke(new Action<Guid>(id => this.dialogReferenceByUUID.Remove(id)), new object[] { dialogguid });
                    crafted = null;
                }
            }
            else
            {
                crafted = null;
            }

            string logtext;
            if (isCancelled)
            {
                switch (successCount)
                {
                    case 0:
                        logtext = $"[GameUpdater] User cancelled the updating progress. No files downloaded before cancelled.";
                        break;
                    case 1:
                        logtext = $"[GameUpdater] User cancelled the updating progress. Downloaded 1 file ({totalsizedownloadedtext}) before cancelled.";
                        break;
                    default:
                        logtext = $"[GameUpdater] User cancelled the updating progress. Downloaded {successList.Count} files ({totalsizedownloadedtext}) before cancelled.";
                        break;
                }
            }
            else
            {
                if (requiredCount == 0)
                {
                    logtext = "[GameUpdater] PSO2 game client has all files updated. There are no files need to be downloaded.";
                }
                else
                {
                    switch (failureCount)
                    {
                        case 0:
                            logtext = $"[GameUpdater] PSO2 game client has been updated successfully (All {successCount} files ({totalsizedownloadedtext}) downloaded)";
                            break;
                        case 1:
                            logtext = $"[GameUpdater] PSO2 game client has been updated (Downloaded {totalsizedownloadedtext}). However, there are 1 file which couldn't be downloaded";
                            break;
                        default:
                            logtext = $"[GameUpdater] PSO2 game client has been updated (Downloaded {totalsizedownloadedtext}). However, there are {failureCount} files which couldn't be downloaded";
                            break;
                    }
                }
            }

            if (crafted != null)
            {
                const string showDetail = " (Show details)";
                var urldefines = new Dictionary<RelativeLogPlacement, Uri>(1)
                {
                    { new RelativeLogPlacement(logtext.Length + 1, showDetail.Length - 1), crafted }
                };
                this.CreateNewParagraphFormatHyperlinksInLog(logtext + showDetail, urldefines);
            }
            else
            {
                this.CreateNewParagraphInLog(logtext);
            }
        }

        class GameClientUpdateResultLogDialogFactory : ILogDialogFactory
        {
            private readonly IReadOnlyDictionary<GameClientUpdateResultLogDialog.PatchListItemLogData, bool?> items;
            public readonly Guid Id;
            private readonly bool _cancel;
            private readonly int list_count;

            public GameClientUpdateResultLogDialogFactory(in bool iscancelled, in int patchlist_count, IReadOnlyDictionary<GameClientUpdateResultLogDialog.PatchListItemLogData, bool?> data)
            {
                this._cancel = iscancelled;
                this.list_count = patchlist_count;
                this.items = data;
                this.Id = Guid.NewGuid();
            }

            public Window CreateNew() => new GameClientUpdateResultLogDialog(in this.Id, in this._cancel, in this.list_count, items);
        }

        private async Task GameClientUpdater_BackupFileFound(GameClientUpdater sender, GameClientUpdater.BackupFileFoundEventArgs e)
        {
            if (this.Dispatcher.CheckAccess())
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
                        e.Handled = false;
                    }
                    else
                    {
                        e.Handled = null;
                    }
                }
                else
                {
                    e.Handled = false;
                }
            }
            else
            {
                TaskCompletionSource<bool?> tsrc = new();
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
}

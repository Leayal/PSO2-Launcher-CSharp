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
                if (Prompt_Generic.Show(this, "The 'pso2_bin' directory doesn't exist.\r\nContinue anyway (may result in full game download)?\r\nPath: " + dir_pso2bin, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            string? dir_classic_data = this.config_main.PSO2Enabled_Classic ? this.config_main.PSO2Directory_Classic : null,
                dir_pso2tweaker = this.config_main.PSO2Tweaker_CompatEnabled ? this.config_main.PSO2Tweaker_Bin_Path : null;
            dir_classic_data = string.IsNullOrWhiteSpace(dir_classic_data) ? null : Path.GetFullPath(dir_classic_data, dir_pso2bin);
            dir_pso2tweaker = string.IsNullOrWhiteSpace(dir_pso2tweaker) || !File.Exists(dir_pso2tweaker) ? null : Path.GetDirectoryName(dir_pso2tweaker);

            var downloaderProfile = this.config_main.DownloaderProfile;
            var downloaderProfileClassic = this.config_main.DownloaderProfileClassic;
            var conf_DownloadType = this.config_main.DownloadSelection;
            var conf_FileScannerConcurrentLevel = Math.Clamp(this.config_main.FileScannerConcurrentCount, 1, 16);
            bool shouldScanForBackups = (this.config_main.PSO2DataBackupBehavior != PSO2DataBackupBehavior.IgnoreAll);
            bool isDlssModAllowed = this.config_main.AllowNvidiaDlssModding;

            // if (!fixMode && this.config_main.AllowNvidiaDlssModding) downloaderProfile |= FileScanFlags.DoNotRedownloadNvidiaDlssBin;
            if (isDlssModAllowed) downloaderProfile |= FileScanFlags.DoNotRedownloadNvidiaDlssBin;

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

                if (downloaderProfile == FileScanFlags.CacheOnly || (downloaderProfileClassic == FileScanFlags.CacheOnly && (selection == GameClientSelection.NGS_AND_CLASSIC || selection == GameClientSelection.Classic_Only)))
                {
                    sb.AppendLine();
                    sb.Append("(If the download profile is 'Cache Only', it will be treated as 'Balanced' profile instead to ensure the accuracy of file scan. Therefore, it may take longer time than an usual check for game client updates)");
                }
                if (isDlssModAllowed)
                {
                    sb.AppendLine();
                    sb.Append("(You enabled \"Don't redownload Nvidia DLSS binary files if it's already existed\" option. Therefore, the Nvidia DLSS binary files will be excluded from the file scan)");
                }

                if (Prompt_Generic.Show(this, sb.ToString(), "Prompt", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            void completed(GameClientUpdater sender, string pso2dir, bool isCancelled, IReadOnlyCollection<PatchListItem> patchlist, IReadOnlyDictionary<PatchListItem, bool?> download_result_list)
            {
                this.pso2Updater.OperationCompleted -= completed;
                this.Dispatcher.TryInvoke(delegate
                {
                    this.TabMainMenu.IsSelected = true;
                });
            }
            this.pso2Updater.OperationCompleted += completed;

            CancellationTokenSource? currentCancelSrc = null;
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
                    this.CreateNewLineInConsoleLog("GameUpdater", "Checking for PSO2 game client updates...");
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
                            msg = $"Launcher has found updates for PSO2 game client (New Version: {ver.Value}).\r\nDo you want to perform update?";
                        }
                        else
                        {
                            msg = $"Launcher has found updates for PSO2 game client.\r\nDo you want to perform update?";
                        }
                        if (Prompt_Generic.Show(this, msg, "Prompt", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                        {
                            currentCancelSrc.Dispose();
                            this.cancelSrc_gameupdater = null;
                            this.pso2Updater.OperationCompleted -= completed;
                            this.TabMainMenu.IsSelected = true;
                            return;
                        }
                    }
                    // this.TabGameClientUpdateProgressBar.SetProgressBarCount(pso2Updater.ConcurrentDownloadCount);

                    if (fixMode)
                    {
                        // To ensure the accuracy of the fix. Don't use Cache Only.
                        if (downloaderProfile == FileScanFlags.CacheOnly)
                        {
                            downloaderProfile = FileScanFlags.Balanced;
                        }
                        if (downloaderProfileClassic == FileScanFlags.CacheOnly)
                        {
                            downloaderProfileClassic = FileScanFlags.Balanced;
                        }

                        this.CreateNewLineInConsoleLog("GameUpdater", "Begin game client's files scanning and downloading...");
                    }
                    else
                    {
                        this.CreateNewLineInConsoleLog("GameUpdater", "Begin game client's updating progress...");
                    }
                    if (!shouldScanForBackups)
                    {
                        this.CreateNewLineInConsoleLog("GameUpdater", "Ignores all backups file (based on user's setting in Launcher's Behavior)...");
                    }
                    await this.pso2Updater.ScanAndDownloadFilesAsync(dir_pso2bin, dir_classic_data, dir_pso2tweaker, conf_FileScannerConcurrentLevel, downloadType, downloaderProfile, downloaderProfileClassic, shouldScanForBackups, cancelToken);
                }
                else
                {
                    currentCancelSrc.Dispose();
                    this.cancelSrc_gameupdater = null;
                    this.pso2Updater.OperationCompleted -= completed;
                    this.TabMainMenu.IsSelected = true;
                    if (!fixMode)
                    {
                        this.CreateNewLineInConsoleLog("GameUpdater", "PSO2 client is already up-to-date");
                    }
                }
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
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
                        this.CreateNewErrorLineInConsoleLog("GameUpdater", "An unknown error occured in operation. Error message: " + ex.Message, null, ex);
                        Prompt_Generic.ShowError(this, ex);
                        break;
                    }
                }
            }
            catch (DatabaseErrorException)
            {
                Prompt_Generic.Show(this, "Error occured when opening database. Maybe you're clicking too fast. Please try again but slower.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (EmptyPatchListException ex)
            {
                this.CreateNewErrorLineInConsoleLog("GameUpdater", string.Empty, null, ex);
                Prompt_Generic.ShowError(this, ex);
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                this.CreateNewErrorLineInConsoleLog("GameUpdater", "An unknown error occured in operation. Error message: " + ex.Message, null, ex);
                Prompt_Generic.ShowError(this, ex);
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

            result.OperationBegin += this.GameClientUpdater_OperationBegin;
            result.BackupFileFound += this.GameClientUpdater_BackupFileFound;
            result.BackupFileRestoreComplete += this.GameClientUpdater_BackupFileRestoreComplete;
            result.OperationCompleted += this.GameUpdaterComponent_OperationCompleted;
            result.ProgressReport += this.GameUpdaterComponent_ProgressReport;
            result.ProgressBegin += this.GameUpdaterComponent_ProgressBegin;
            result.ProgressEnd += this.GameUpdaterComponent_ProgressEnd;
            result.FileCheckBegin += this.GameUpdaterComponent_FileCheckBegin;
            result.DownloadQueueAdded += this.GameUpdaterComponent_DownloadQueueAdded;
            result.FileCheckEnd += this.GameUpdaterComponent_FileCheckEnd;

            return result;
        }

        private void GameClientUpdater_OperationBegin(GameClientUpdater sender, int concurrentlevel)
        {
            var tab = this.TabGameClientUpdateProgressBar;
            if (tab.Dispatcher.CheckAccess())
            {
                tab.ResetMainProgressBarState();
                tab.SetProgressBarCount(concurrentlevel);
                tab.ResetAllSubDownloadState();
            }
            else
            {
                tab.Dispatcher.Invoke(new Action<GameClientUpdater, int>(this.GameClientUpdater_OperationBegin), new object[] { sender, concurrentlevel });
            }
        }

        public void GameUpdaterComponent_ProgressReport(int taskId, PatchListItem file, in long downloadedByteCount)
        {
            this.TabGameClientUpdateProgressBar.GetProgressController(taskId).SetProgress(downloadedByteCount);
        }

        public void GameUpdaterComponent_ProgressBegin(int taskId, PatchListItem file, in long byteCount)
        {
            this.TabGameClientUpdateProgressBar.GetProgressController(taskId).SetData(byteCount, 0, file.GetFilenameWithoutAffix(), true);
        }

        public void GameUpdaterComponent_ProgressEnd(int taskId, PatchListItem file, in bool success)
        {
            if (success)
            {
                this.TabGameClientUpdateProgressBar.IncreaseDownloadedCount(in file.FileSize);
            }
            this.TabGameClientUpdateProgressBar.GetProgressController(taskId).Reset();
        }

        private void GameUpdaterComponent_FileCheckBegin(GameClientUpdater sender, int total)
        {
            if (this.Dispatcher.CheckAccess())
            {
                var tab = this.TabGameClientUpdateProgressBar;
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

        //private void GameUpdaterComponent_OperationCompleted(GameClientUpdater sender, bool isCancelled, IReadOnlyCollection<PatchListItem> patchlist, IReadOnlyCollection<PatchListItem> download_required_list, IReadOnlyCollection<PatchListItem> successList, IReadOnlyCollection<PatchListItem> failureList)
        private async void GameUpdaterComponent_OperationCompleted(GameClientUpdater sender, string pso2dir, bool isCancelled, IReadOnlyCollection<PatchListItem> patchlist, IReadOnlyDictionary<PatchListItem, bool?> download_results_list)
        {
            long totalsizedownloaded = 0L;
            
            var requiredCount = download_results_list.Count;
            var successCount = 0;
            var failureCount = 0;

            foreach (var item in download_results_list)
            {
                var result = item.Value;
                if (result.HasValue)
                {
                    if (result.Value)
                    {
                        totalsizedownloaded += item.Key.FileSize;
                        successCount++;
                    }
                    else
                    {
                        failureCount++;
                    }
                }
            }
            var totalsizedownloadedtext = Shared.NumericHelper.ToHumanReadableFileSize(in totalsizedownloaded);

            Guid dialogguid;
            Uri? crafted;
            if (requiredCount == 0)
            {
                dialogguid = Guid.Empty;
                crafted = null;
            }
            else
            {
                var dictionary = new Dictionary<GameClientUpdateResultLogDialog.PatchListItemLogData, bool?>(requiredCount);
                foreach (var (itemrecord, value) in download_results_list)
                {
                    dictionary.Add(new GameClientUpdateResultLogDialog.PatchListItemLogData(itemrecord.GetFilenameWithoutAffix(), itemrecord.FileSize), value);
                }
                dialogguid = Guid.NewGuid();
                if (Uri.TryCreate(StaticResources.Url_ShowLogDialogFromGuid, dialogguid.ToString(), out crafted))
                {
                    var factory = new GameClientUpdateResultLogDialogFactory(pso2dir, in dialogguid, in isCancelled, patchlist.Count, (IReadOnlyDictionary<GameClientUpdateResultLogDialog.PatchListItemLogData, bool?>)dictionary);
                    this.dialogReferenceByUUID.Add(dialogguid, factory);
                }
                else
                {
                    dialogguid = Guid.Empty;
                    crafted = null;
                }
            }

            string logtext;
            if (isCancelled)
            {
                switch (successCount)
                {
                    case 0:
                        logtext = $"User cancelled the updating progress. No files downloaded before cancelled.";
                        break;
                    case 1:
                        logtext = $"User cancelled the updating progress. Downloaded 1 file ({totalsizedownloadedtext}) before cancelled.";
                        break;
                    default:
                        logtext = $"User cancelled the updating progress. Downloaded {successCount} files ({totalsizedownloadedtext}) before cancelled.";
                        break;
                }
            }
            else
            {
                if (requiredCount == 0)
                {
                    logtext = "PSO2 game client has all files updated. There are no files need to be downloaded.";
                }
                else
                {
                    switch (failureCount)
                    {
                        case 0:
                            logtext = $"PSO2 game client has been updated successfully (All {successCount} files ({totalsizedownloadedtext}) downloaded)";
                            break;
                        case 1:
                            logtext = $"PSO2 game client has been updated (Downloaded {totalsizedownloadedtext}). However, there are 1 file which couldn't be downloaded";
                            break;
                        default:
                            logtext = $"PSO2 game client has been updated (Downloaded {totalsizedownloadedtext}). However, there are {failureCount} files which couldn't be downloaded";
                            break;
                    }
                }
            }

            this.CreateNewLineInConsoleLog("GameUpdater", (console, writer, absoluteOffsetOfCurrentLine, arg) =>
            {
                var (myself, logtext, crafted) = arg;
                writer.Write(logtext);
                if (crafted != null)
                {
                    writer.Write(' ');
                    ConsoleLogHelper_WriteHyperLink(writer, "(Show details)", crafted, VisualLineLinkText_LinkClicked);
                }
            }, (this, logtext, crafted));

            var tab = this.TabGameClientUpdateProgressBar;
            if (tab.Dispatcher.CheckAccess())
            {
                this.TabGameClientUpdateProgressBar.ResetAllSubDownloadState();
            }
            else
            {
                await tab.Dispatcher.InvokeAsync(tab.ResetAllSubDownloadState);
            }
        }

        sealed class GameClientUpdateResultLogDialogFactory : ILogDialogFactory
        {
            private readonly IReadOnlyDictionary<GameClientUpdateResultLogDialog.PatchListItemLogData, bool?> items;
            private readonly Guid Id;
            private readonly bool _cancel;
            private readonly int list_count;
            private readonly string pso2dir;
            private readonly DateTime updateCompleteTime;

            public GameClientUpdateResultLogDialogFactory(string _pso2dir, in Guid id, in bool iscancelled, in int patchlist_count, IReadOnlyDictionary<GameClientUpdateResultLogDialog.PatchListItemLogData, bool?> data)
            {
                this.pso2dir = _pso2dir;
                this._cancel = iscancelled;
                this.list_count = patchlist_count;
                this.items = data;
                this.Id = id;
                this.updateCompleteTime = DateTime.Now;
            }

            public Window CreateNew() => new GameClientUpdateResultLogDialog(this.pso2dir, in this.Id, in this._cancel, in this.list_count, items, this.updateCompleteTime);
        }

        private async Task GameClientUpdater_BackupFileFound(GameClientUpdater sender, GameClientUpdater.BackupFileFoundEventArgs e)
        {
            var backupbehavior = this.config_main.PSO2DataBackupBehavior;

            static bool AcceptRestoreBackupWithoutAsking(MainMenuWindow window)
            {
                window.CreateNewLineInConsoleLog("GameUpdater", "Restoring backup files without asking user (based on user's setting in Launcher's Behavior)...");
                return false;
            }

            static bool? ShowPrompt(MainMenuWindow window, GameClientUpdater.BackupFileFoundEventArgs e)
            {
                window.CreateNewLineInConsoleLog("GameUpdater", "Backup data file found.");
                if (Prompt_PSO2DataBackupFound.Show(window, e, window.config_main) == MessageBoxResult.Yes)
                {
                    window.CreateNewLineInConsoleLog("GameUpdater", "User accepted to restore backup files.");
                    return false;
                }
                else
                {
                    window.CreateNewLineInConsoleLog("GameUpdater", "User declined. Ignoring all backup files.");
                    return null;
                }
            }

            e.Handled = backupbehavior switch
            {
                PSO2DataBackupBehavior.RestoreWithoutAsking => AcceptRestoreBackupWithoutAsking(this),
                PSO2DataBackupBehavior.IgnoreAll => null, // We will never reach this but let put it here to show intention.
                _ => (this.Dispatcher.CheckAccess() ? ShowPrompt(this, e) : await this.Dispatcher.InvokeAsync<bool?>(() => ShowPrompt(this, e)).Task.ConfigureAwait(false))
            };
        }

#nullable enable
        private void GameClientUpdater_BackupFileRestoreComplete(GameClientUpdater sender, GameClientUpdater.BackupFileFoundEventArgs? e, int numberOfBackupFiles)
        {
            if (e == null)
            {
                this.CreateNewLineInConsoleLog("GameUpdater", "Launcher found no backup files to restore.");
            }
            else if (numberOfBackupFiles >= 0)
            {
                if (numberOfBackupFiles == 0 || numberOfBackupFiles == 1)
                {
                    this.CreateNewLineInConsoleLog("GameUpdater", $"Restored {numberOfBackupFiles} backup file.");
                }
                else
                {
                    this.CreateNewLineInConsoleLog("GameUpdater", $"Restored {numberOfBackupFiles} backup files.");
                }
            }
        }
#nullable restore
    }
}

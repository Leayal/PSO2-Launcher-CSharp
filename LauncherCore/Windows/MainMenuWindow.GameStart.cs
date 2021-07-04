using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.PSO2Launcher.Core.UIElements;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class MainMenuWindow
    {
        private async void TabMainMenu_ButtonGameStartClick(object sender, RoutedEventArgs e)
        {
            if (sender is TabMainMenu tab)
            {
                tab.GameStartEnabled = false;
                CancellationTokenSource currentCancelSrc = null;
                try
                {
                    var dir_pso2bin = this.config_main.PSO2_BIN;
                    if (string.IsNullOrEmpty(dir_pso2bin))
                    {
                        if (MessageBox.Show(this, "You have not set the 'pso2_bin' directory.\r\nDo you want to set it now?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            this.TabMainMenu_ButtonManageGameDataClick(null, null);
                        }
                        return;
                    }
                    else
                    {
                        dir_pso2bin = Path.GetFullPath(dir_pso2bin);
                        var filename = Path.GetFullPath("pso2.exe", dir_pso2bin);
                        if (!Directory.Exists(dir_pso2bin))
                        {
                            MessageBox.Show(this, "The 'pso2_bin' directory doesn't exist.\r\nPath: " + dir_pso2bin, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        else if (!File.Exists(filename))
                        {
                            MessageBox.Show(this, "The file 'pso2.exe' doesn't exist.\r\nPath: " + filename, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        currentCancelSrc = new CancellationTokenSource();
                        this.cancelSrc?.Dispose();
                        this.cancelSrc = currentCancelSrc;
                        var cancelToken = currentCancelSrc.Token;

                        this.TabGameClientUpdateProgressBar.IsSelected = true;

                        var downloaderprofile = this.config_main.DownloaderProfile;


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
                        this.TabGameClientUpdateProgressBar.SetProgressBarCount(pso2Updater.ConcurrentDownloadCount);

                        var hasUpdate = await this.pso2Updater.CheckForPSO2Updates(cancelToken);
                        if (hasUpdate)
                        {
                            if (MessageBox.Show(this, "It seems like your client is not updated. Continue anyway?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            {
                                return;
                            }
                        }

                        var t1 = this.pso2Updater.ScanForFilesNeedToDownload(GameClientSelection.Always_Only, downloaderprofile, cancelToken);
                        var t2 = this.pso2Updater.StartDownloadFiles(cancelToken);
                        await Task.WhenAll(t1, t2);

                        if (!cancelToken.IsCancellationRequested)
                        {
                            using (var proc = new Process())
                            {
                                proc.StartInfo.UseShellExecute = true;
                                proc.StartInfo.Verb = "runas";
                                proc.StartInfo.FileName = filename;
                                proc.StartInfo.ArgumentList.Add("-reboot");
                                proc.StartInfo.ArgumentList.Add("-optimize");
                                proc.StartInfo.WorkingDirectory = dir_pso2bin;
                                proc.Start();
                            }
                        }
                    }
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
                {
                    // Silent it as user press "No" themselves.
                    // MessageBox.Show(this, ex.Message, "User cancelled", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    currentCancelSrc?.Dispose();
                    await this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        tab.GameStartEnabled = true;
                    }));
                }
            }
        }


        private async void TabMainMenu_LoginAndPlayClicked(object sender, RoutedEventArgs e)
        {
            if (sender is TabMainMenu tab)
            {
                tab.GameStartEnabled = false;
                CancellationTokenSource currentCancelSrc = null;
                try
                {
                    var dir_pso2bin = this.config_main.PSO2_BIN;
                    if (string.IsNullOrEmpty(dir_pso2bin))
                    {
                        if (MessageBox.Show(this, "You have not set the 'pso2_bin' directory.\r\nDo you want to set it now?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            this.TabMainMenu_ButtonManageGameDataClick(null, null);
                        }
                        return;
                    }
                    else
                    {
                        dir_pso2bin = Path.GetFullPath(dir_pso2bin);
                        var filename = Path.GetFullPath("pso2.exe", dir_pso2bin);
                        if (!Directory.Exists(dir_pso2bin))
                        {
                            MessageBox.Show(this, "The 'pso2_bin' directory doesn't exist.\r\nPath: " + dir_pso2bin, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        else if (!File.Exists(filename))
                        {
                            MessageBox.Show(this, "The file 'pso2.exe' doesn't exist.\r\nPath: " + filename, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (MessageBox.Show(this, "If you don't trust this. Please do not use this, instead, start the game without login.\r\nDo you really trust this?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                        {
                            return;
                        }

                        bool? loginContinue;
                        // bool isOKay = false;
                        PSO2LoginToken token = null;
                        using (var loginForm = new PSO2LoginDialog(this.pso2HttpClient))
                        {
                            loginForm.Owner = this;
                            loginContinue = loginForm.ShowDialog();
                            if (loginContinue == true)
                            {
                                token = loginForm.LoginToken;
                                if (token.RequireOTP)
                                {
                                    // Maybe I should stop here???
                                }
                            }
                        }   

                        if (token != null)
                        {
                            try
                            {
                                currentCancelSrc = new CancellationTokenSource();
                                this.cancelSrc?.Dispose();
                                this.cancelSrc = currentCancelSrc;
                                var cancelToken = currentCancelSrc.Token;

                                this.TabGameClientUpdateProgressBar.IsSelected = true;

                                var downloaderprofile = this.config_main.DownloaderProfile;


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
                                this.TabGameClientUpdateProgressBar.SetProgressBarCount(pso2Updater.ConcurrentDownloadCount);

                                var hasUpdate = await this.pso2Updater.CheckForPSO2Updates(cancelToken);
                                if (hasUpdate)
                                {
                                    if (MessageBox.Show(this, "It seems like your client is not updated. Continue anyway?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                                    {
                                        return;
                                    }
                                }

                                var t1 = this.pso2Updater.ScanForFilesNeedToDownload(GameClientSelection.Always_Only, downloaderprofile, cancelToken);
                                var t2 = this.pso2Updater.StartDownloadFiles(cancelToken);
                                await Task.WhenAll(t1, t2);

                                if (!cancelToken.IsCancellationRequested)
                                {
                                    using (var proc = new Process())
                                    {
                                        proc.StartInfo.UseShellExecute = true;
                                        proc.StartInfo.Verb = "runas";
                                        proc.StartInfo.FileName = filename;
                                        proc.StartInfo.ArgumentList.Add("-reboot");
                                        token.AppendToStartInfo(proc.StartInfo);
                                        proc.StartInfo.ArgumentList.Add("-optimize");
                                        proc.StartInfo.WorkingDirectory = dir_pso2bin;
                                        proc.Start();
                                    }
                                }
                            }
                            finally
                            {
                                token.Dispose();
                            }
                        }
                    }
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
                {
                    // Silent it as user press "No" themselves.
                    // MessageBox.Show(this, ex.Message, "User cancelled", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    currentCancelSrc?.Dispose();
                    await this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        tab.GameStartEnabled = true;
                    }));
                }
            }
        }
    }
}

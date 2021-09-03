using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.PSO2Launcher.Core.UIElements;
using Leayal.SharedInterfaces;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class MainMenuWindow
    {
        private SecureString? ss_id, ss_pw;

        private void TabMainMenu_ForgetLoginInfoClicked(object sender, RoutedEventArgs e)
            => this.ForgetSEGALogin();

        private void ForgetSEGALogin()
        {
            Interlocked.Exchange<SecureString>(ref this.ss_id, null)?.Dispose();
            Interlocked.Exchange<SecureString>(ref this.ss_pw, null)?.Dispose();
            this.TabMainMenu.ForgetLoginInfoEnabled = false;
        }

        private void TabMainMenu_DefaultGameStartStyleChanged(object sender, ChangeDefaultGameStartStyleEventArgs e)
        {
            this.config_main.DefaultGameStartStyle = e.SelectedStyle;
            this.config_main.Save();
        }

        private async void TabMainMenu_GameStartRequested(object sender, GameStartStyleEventArgs e)
        {
            GameStartStyle requestedStyle;
            if (e.SelectedStyle == GameStartStyle.Default)
            {
                requestedStyle = this.config_main.DefaultGameStartStyle;
            }
            else
            {
                requestedStyle = e.SelectedStyle;
            }

            if (sender is TabMainMenu tab)
            {
                tab.GameStartEnabled = false;
                CancellationTokenSource currentCancelSrc = null;
                try
                {
                    var dir_pso2bin = this.config_main.PSO2_BIN;
                    if (string.IsNullOrEmpty(dir_pso2bin))
                    {
                        var aaa = new Prompt_PSO2BinIsNotSet();
                        switch (aaa.ShowCustomDialog(this))
                        {
                            case true:
                                this.TabMainMenu_ButtonInstallPSO2_Clicked(tab, null);
                                break;
                            case false:
                                this.TabMainMenu_ButtonManageGameDataClick(tab, null);
                                break;
                        }
                        /*
                        if (MessageBox.Show(this, "You have not set the 'pso2_bin' directory.\r\nDo you want to set it now?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            this.TabMainMenu_ButtonManageGameDataClick(null, null);
                        }
                        */
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
                            MessageBox.Show(this, "The file 'pso2.exe' doesn't exist. Please download game's data files if you haven't done it.\r\nPath: " + filename, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        string dir_classic_data = this.config_main.PSO2Enabled_Classic ? this.config_main.PSO2Directory_Classic : null,
                            dir_reboot_data = this.config_main.PSO2Enabled_Reboot ? this.config_main.PSO2Directory_Reboot : null;
                        dir_classic_data = string.IsNullOrWhiteSpace(dir_classic_data) ? null : Path.GetFullPath(dir_classic_data, dir_pso2bin);
                        dir_reboot_data = string.IsNullOrWhiteSpace(dir_reboot_data) ? null : Path.GetFullPath(dir_reboot_data, dir_pso2bin);

                        var configFolderPath = Path.GetFullPath("config", RuntimeValues.RootDirectory);
                        var usernamePath = Path.Combine(configFolderPath, "SavedUsername.txt");
                        if (!File.Exists(usernamePath))
                        {
                            if (MessageBox.Show(this, "If you don't trust this. Please do not use this, instead, start the game without login.\r\nDo you really trust this?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            {
                                return;
                            }
                            File.Create(usernamePath)?.Dispose();
                        }

                        currentCancelSrc = CancellationTokenSource.CreateLinkedTokenSource(this.cancelAllOperation.Token);
                        var cancelToken = currentCancelSrc.Token;
                        // bool isOKay = false;
                        PSO2LoginToken token = null;

                        if (requestedStyle == GameStartStyle.StartWithToken)
                        {
                            if (this.ss_id == null || this.ss_pw == null)
                            {
                                this.ForgetSEGALogin();
                                SecureString username;
                                try
                                {
                                    if (File.Exists(usernamePath))
                                    {
                                        var data = File.ReadAllBytes(usernamePath);
                                        username = SecureStringHelper.Import(data);
                                        username.MakeReadOnly();
                                    }
                                    else
                                    {
                                        username = null;
                                    }
                                }
                                catch
                                {
                                    username = null;
                                }

                                using (var loginForm = new PSO2LoginDialog(this.config_main, this.pso2HttpClient, username, true))
                                {
                                    if (loginForm.ShowCustomDialog(this) == true)
                                    {
                                        token = loginForm.LoginToken;
                                        if (loginForm.checkbox_rememberusername.IsChecked == true)
                                        {
                                            using (var id = loginForm.GetUsername())
                                            {
                                                Directory.CreateDirectory(configFolderPath); // Totally unneccessary but did it anyway.
                                                using (var fs = File.Create(usernamePath))
                                                {
                                                    byte[] buffer = id.Export();
                                                    try
                                                    {
                                                        fs.Write(buffer, 0, buffer.Length);
                                                    }
                                                    finally
                                                    {
                                                        Array.Fill<byte>(buffer, 0);
                                                    }

                                                    fs.Flush();
                                                }
                                            }
                                        }
                                        if (loginForm.SelectedRememberOption == LoginPasswordRememberStyle.NonPersistentRemember)
                                        {
                                            this.ss_id = loginForm.GetUsername();
                                            this.ss_pw = loginForm.GetPassword();

                                            await this.TabMainMenu.Dispatcher.InvokeAsync(delegate
                                            {
                                                this.TabMainMenu.ForgetLoginInfoEnabled = true;
                                            });
                                        }
                                        if (token.RequireOTP)
                                        {
                                            // Maybe I should stop here???
                                        }
                                    }
                                }
                            }
                            else
                            {
                                await this.Dispatcher.InvokeAsync(delegate
                                {
                                    this.TabGameClientUpdateProgressBar.IsSelected = true;
                                });
                                token = await this.pso2HttpClient.LoginPSO2Async(this.ss_id, this.ss_pw, cancelToken);
                            }
                        }

                        if ((requestedStyle == GameStartStyle.StartWithToken && token != null) || requestedStyle == GameStartStyle.StartWithoutToken)
                        {
                            try
                            {
                                try
                                {
                                    this.cancelSrc_gameupdater?.Dispose();
                                }
                                catch { }
                                this.cancelSrc_gameupdater = currentCancelSrc;

                                if (this.TabGameClientUpdateProgressBar.IsSelected != true)
                                {
                                    this.TabGameClientUpdateProgressBar.IsSelected = true;
                                }

                                var checkUpdateBeforeLaunch = this.config_main.CheckForPSO2GameUpdateBeforeLaunchingGame;

                                GameClientUpdater.OperationCompletedHandler completed = null;
                                completed = (sender, cancelled, pathclist, requiredDownload, successList, failedList) =>
                                {
                                    this.pso2Updater.OperationCompleted -= completed;
                                    this.Dispatcher.InvokeAsync(delegate
                                    {
                                        this.TabMainMenu.IsSelected = true;
                                    });

                                };
                                this.pso2Updater.OperationCompleted += completed;
                                this.TabGameClientUpdateProgressBar.SetProgressBarCount(pso2Updater.ConcurrentDownloadCount);

                                if (checkUpdateBeforeLaunch)
                                {
                                    var hasUpdate = await this.pso2Updater.CheckForPSO2Updates(dir_pso2bin, cancelToken);
                                    if (hasUpdate)
                                    {
                                        if (MessageBox.Show(this, "It seems like your client is not updated. Continue anyway?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                                        {
                                            return;
                                        }
                                    }
                                }

                                await this.CreateNewParagraphInLog(writer =>
                                {
                                    writer.Write("[GameStart] Quick check executable files...");
                                });

                                // Safety reason => Balanced.
                                await this.pso2Updater.ScanAndDownloadFilesAsync(dir_pso2bin, dir_reboot_data, dir_classic_data, GameClientSelection.Always_Only, FileScanFlags.Balanced, cancelToken);

                                if (!cancelToken.IsCancellationRequested)
                                {
                                    using (var proc = new Process())
                                    {
                                        proc.StartInfo.UseShellExecute = true;
                                        proc.StartInfo.Verb = "runas";
                                        proc.StartInfo.FileName = filename;
                                        proc.StartInfo.ArgumentList.Add("-reboot");
                                        if (requestedStyle == GameStartStyle.StartWithToken && token != null)
                                        {
                                            token.AppendToStartInfo(proc.StartInfo);
                                        }
                                        proc.StartInfo.ArgumentList.Add("-optimize");
                                        proc.StartInfo.WorkingDirectory = dir_pso2bin;
                                        proc.Start();
                                        await this.CreateNewParagraphInLog(writer =>
                                        {
                                            writer.Write("[GameStart] Starting game...");
                                        });
                                    }
                                }
                            }
                            finally
                            {
                                token?.Dispose();
                            }
                        }
                    }
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
                {
                    // Silent it as user press "No" themselves.
                    // MessageBox.Show(this, ex.Message, "User cancelled", MessageBoxButton.OK, MessageBoxImage.Warning);
                    await this.CreateNewParagraphInLog(writer =>
                    {
                        writer.Write("[GameStart] User cancelled");
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    currentCancelSrc?.Dispose();
                    this.cancelSrc_gameupdater = null;
                    await this.Dispatcher.InvokeAsync(new Action(() =>
                    {
                        tab.GameStartEnabled = true;
                    }));
                }
            }
        }
    }
}

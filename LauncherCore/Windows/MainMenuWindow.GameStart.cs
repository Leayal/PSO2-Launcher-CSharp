using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.PSO2Launcher.Core.UIElements;
using Leayal.SharedInterfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
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
        {
            if (this.ss_id != null)
            {
                this.ss_id.Dispose();
                this.ss_id = null;
            }
            if (this.ss_pw != null)
            {
                this.ss_pw.Dispose();
                this.ss_pw = null;
            }
            this.TabMainMenu.ForgetLoginInfoEnabled = false;
        }

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

                        try
                        {
                            await this.pso2Updater.Prepare();
                        }
                        catch
                        {
                            await this.pso2Updater.Prepare();
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

                        currentCancelSrc = new CancellationTokenSource();
                        var cancelToken = currentCancelSrc.Token;
                        // bool isOKay = false;
                        PSO2LoginToken token = null;
                        if (this.ss_id == null || this.ss_pw == null)
                        {
                            this.ss_id?.Dispose();
                            this.ss_pw?.Dispose();
                            SecureString username;
                            try
                            {
                                if (File.Exists(usernamePath))
                                {
                                    using (var fs = File.OpenRead(usernamePath))
                                    {
                                        var bufferlength = fs.Length;
                                        if (bufferlength != 0 && bufferlength < (128 * 2))
                                        {
                                            var buffer = new byte[bufferlength];
                                            if (fs.Read(buffer, 0, buffer.Length) == buffer.Length)
                                            {
                                                for (int i = 0; i < buffer.Length; i++)
                                                {
                                                    buffer[i] ^= 0x55;
                                                }
                                                unsafe
                                                {
                                                    fixed (byte* b = buffer)
                                                    {
                                                        char* c = (char*)b;
                                                        username = new SecureString(c, buffer.Length / 2);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                username = null;
                                            }
                                        }
                                        else
                                        {
                                            username = null;
                                        }
                                    }
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
                            
                            using (var loginForm = new PSO2LoginDialog(this.pso2HttpClient, username))
                            {
                                loginForm.Owner = this;
                                if (loginForm.ShowDialog() == true)
                                {
                                    token = loginForm.LoginToken;
                                    if (loginForm.checkbox_rememberusername.IsChecked == true)
                                    {
                                        using (var id = loginForm.GetUsername())
                                        using (var fs = File.Create(usernamePath))
                                        {
                                            var buffer = new byte[id.Length * 2];
                                            id.UseAsString((in ReadOnlySpan<char> chars) =>
                                            {
                                                Encoding.Unicode.GetBytes(chars, buffer);
                                            });
                                            for (int i = 0; i < buffer.Length; i++)
                                            {
                                                buffer[i] ^= 0x55;
                                            }
                                            fs.Write(buffer, 0, buffer.Length);
                                        }
                                    }
                                    if (loginForm.SelectedRememberOption == PSO2LoginDialog.RememberOption.RememberLoginInfo)
                                    {
                                        this.ss_id = loginForm.GetUsername();
                                        this.ss_pw = loginForm.GetPassword();

                                        await this.TabMainMenu.Dispatcher.BeginInvoke((Action)delegate
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
                            this.TabGameClientUpdateProgressBar.IsSelected = true;
                            token = await this.pso2HttpClient.LoginPSO2Async(this.ss_id, this.ss_pw, cancelToken);
                        }

                        if (token != null)
                        {
                            try
                            {
                                this.cancelSrc?.Dispose();
                                this.cancelSrc = currentCancelSrc;

                                if (this.TabGameClientUpdateProgressBar.IsSelected != true)
                                {
                                    this.TabGameClientUpdateProgressBar.IsSelected = true;
                                }

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

                                try
                                {
                                    await this.pso2Updater.Prepare();
                                }
                                catch
                                {
                                    await this.pso2Updater.Prepare();
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

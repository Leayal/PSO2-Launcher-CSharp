using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.PSO2Launcher.Core.UIElements;
using Leayal.SharedInterfaces;
using Leayal.Shared;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
        private bool _isTweakerRunning;
#nullable enable
        private SecureString? ss_id, ss_pw;

        private void ForgetSEGALogin()
        {
            // Suppress it because we don't care whether it's null or not.
            // If it's already null, does nothing. If non-null, dispose it.
#pragma warning disable CS8601, CS8625
            Interlocked.Exchange<SecureString>(ref this.ss_id, null)?.Dispose();
            Interlocked.Exchange<SecureString>(ref this.ss_pw, null)?.Dispose();
#pragma warning restore CS8601, CS8625
            this.TabMainMenu.ForgetLoginInfoEnabled = false;
        }
#nullable restore

        private void TabMainMenu_ForgetLoginInfoClicked(object sender, RoutedEventArgs e)
            => this.ForgetSEGALogin();

        private void TabMainMenu_DefaultGameStartStyleChanged(object sender, ChangeDefaultGameStartStyleEventArgs e)
        {
            switch (e.SelectedStyle)
            {
                case GameStartStyle.StartWithPSO2Tweaker:
                    this.config_main.PSO2Tweaker_LaunchGameWithTweaker = true;
                    break;
                default:
                    this.config_main.PSO2Tweaker_LaunchGameWithTweaker = false;
                    this.config_main.DefaultGameStartStyle = e.SelectedStyle;
                    break;
            }
            
            this.config_main.Save();
        }

#nullable enable
        /// <returns>PSO2 process or NULL if not found.</returns>
        private static Task<Process?> TryFindPSO2Process(string? fullpath = null)
        {
            return Task.Factory.StartNew<Process?>(obj =>
            {
                Process? result = null;
                if (obj is string imagePath)
                {
                    var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(fullpath));
                    if (processes != null && processes.Length != 0)
                    {
                        if (!Path.IsPathFullyQualified(imagePath))
                        {
                            imagePath = Path.GetFullPath(imagePath);
                        }
                        for (int i = 0; i < processes.Length; i++)
                        {
                            if (string.Equals(processes[i].QueryFullProcessImageName(), imagePath, StringComparison.OrdinalIgnoreCase))
                            {
                                result = processes[i];
                            }
                            else
                            {
                                // Dispose/Release other handles which we don't need.
                                // We can wait for the runtime to cleanup automatically but I want the runtime to do it ASAP. Hence, calling Dispose.
                                processes[i].Dispose();
                            }
                        }
                    }
                }
                else
                {
                    var processes = Process.GetProcessesByName("pso2");
                    if (processes != null && processes.Length != 0)
                    {
                        // Dispose/Release other handles which we don't need.
                        for (int i = 1; i < processes.Length; i++)
                        {
                            // We can wait for the runtime to cleanup automatically but I want the runtime to do it ASAP. Hence, calling Dispose.
                            processes[i].Dispose();
                        }
                        result = processes[0];
                    }
                }
                return result;
            }, fullpath);
        }
#nullable restore

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

                        if (!Directory.Exists(dir_pso2bin))
                        {
                            Prompt_Generic.Show(this, "The 'pso2_bin' directory doesn't exist.\r\nPath: " + dir_pso2bin, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        var filename = Path.GetFullPath("pso2.exe", dir_pso2bin);
                        if (!File.Exists(filename))
                        {
                            Prompt_Generic.Show(this, "The file 'pso2.exe' doesn't exist. Please download game's data files if you haven't done it.\r\nPath: " + filename, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        bool isLaunchWithTweaker = (e.SelectedStyle == GameStartStyle.StartWithPSO2Tweaker);
                        string tweakerPath = this.config_main.PSO2Tweaker_Bin_Path;
                        if (isLaunchWithTweaker)
                        {
                            if (string.IsNullOrWhiteSpace(tweakerPath) || !File.Exists(tweakerPath))
                            {
                                Prompt_Generic.Show(this, "PSO2 Tweaker compatibility is enabled but you haven't specify the Tweaker's executable path.", "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                                return;
                            }
                        }
                        
                        using (var existingProcess = await TryFindPSO2Process(filename))
                        {
                            if (existingProcess != null)
                            {
                                if (UacHelper.IsCurrentProcessElevated)
                                {
                                    var windowhandle = existingProcess.MainWindowHandle;
                                    if (windowhandle == IntPtr.Zero)
                                    {
                                        try
                                        {
                                            string prompt_message;
                                            var elt = (DateTime.Now - existingProcess.StartTime);
                                            if ((DateTime.Now - existingProcess.StartTime) > TimeSpan.FromMinutes(5))
                                            {
                                                prompt_message = $"The game is already running but it seems to be stuck.{Environment.NewLine}Do you want to close the the existing game's process and start a new one?";
                                            }
                                            else
                                            {
                                                prompt_message = $"The game is already running but you should wait for a little bit before the window shows up.{Environment.NewLine}Are you sure you want to close the the existing game's process and start a new one?";
                                            }
                                            if (Prompt_Generic.Show(this, prompt_message, "Prompt", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                                            {
                                                if (!existingProcess.HasExited)
                                                {
                                                    existingProcess.Kill();
                                                }
                                            }
                                            else
                                            {
                                                if (!existingProcess.HasExited)
                                                {
                                                    windowhandle = existingProcess.MainWindowHandle;
                                                    if (UnmanagedWindowsHelper.SetForegroundWindow(windowhandle))
                                                    {
                                                        this.CreateNewParagraphInLog($"[GameStart] The game is already running. Giving the game's window focus...");
                                                    }
                                                }
                                                return;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            this.CreateNewParagraphInLog("[GameStart] Cannot terminate the current PSO2 process. Error message: " + ex.Message);
                                            Prompt_Generic.Show(this, "Cannot terminate the current PSO2 process.\r\nError Message: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        if (Prompt_Generic.Show(this, $"The game is already running.{Environment.NewLine}Are you sure you want to close the the existing game's process and start a new one?", "Prompt", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                                        {
                                            if (!existingProcess.HasExited)
                                            {
                                                existingProcess.Kill();
                                            }
                                        }
                                        else
                                        {
                                            try
                                            {
                                                if (!existingProcess.HasExited && UnmanagedWindowsHelper.SetForegroundWindow(windowhandle))
                                                {
                                                    this.CreateNewParagraphInLog($"[GameStart] The game is already running. Giving the game's window focus...");
                                                }
                                            }
                                            catch { }
                                            return;
                                        }
                                    }
                                }
                                else
                                {
                                    if (!existingProcess.HasExited)
                                    {
                                        try
                                        {
                                            var windowhandle = existingProcess.MainWindowHandle;
                                            if (windowhandle == IntPtr.Zero || !UnmanagedWindowsHelper.SetForegroundWindow(windowhandle))
                                            {
                                                this.CreateNewParagraphInLog($"[GameStart] The game is already running.");
                                                Prompt_Generic.Show(this, $"The game is already running.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                            }
                                            else
                                            {
                                                this.CreateNewParagraphInLog($"[GameStart] The game is already running. Giving the game's window focus...");
                                            }
                                        }
                                        catch
                                        {
                                            this.CreateNewParagraphInLog($"[GameStart] The game is already running.");
                                            Prompt_Generic.Show(this, $"The game is already running.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                        }
                                        return;
                                    }
                                }
                            }
                        }

                        string dir_classic_data = this.config_main.PSO2Enabled_Classic ? this.config_main.PSO2Directory_Classic : null,
                            dir_pso2tweaker = this.config_main.PSO2Tweaker_CompatEnabled ? this.config_main.PSO2Tweaker_Bin_Path : null;
                        dir_classic_data = string.IsNullOrWhiteSpace(dir_classic_data) ? null : Path.GetFullPath(dir_classic_data, dir_pso2bin);
                        dir_pso2tweaker = string.IsNullOrWhiteSpace(dir_pso2tweaker) || !File.Exists(dir_pso2tweaker) ? null : Path.GetDirectoryName(dir_pso2tweaker);

                        var configFolderPath = Path.GetFullPath("config", RuntimeValues.RootDirectory);
                        var usernamePath = Path.Combine(configFolderPath, "SavedUsername.txt");
                        if (e.SelectedStyle == GameStartStyle.StartWithToken && !File.Exists(usernamePath))
                        {
                            if (Prompt_Generic.Show(this, "If you don't trust this. Please do not use this, instead, start the game without login.\r\nDo you really trust this?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
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
                                    loginForm.AutoHideInTaskbarByOwnerIsVisible = true;
                                    if (loginForm.ShowCustomDialog(this) == true)
                                    {
                                        token = loginForm.LoginToken;
                                        if (loginForm.checkbox_rememberusername.IsChecked == true)
                                        {
                                            using (var id = loginForm.GetUsername())
                                            {
                                                Directory.CreateDirectory(configFolderPath); // Totally unneccessary but did it anyway.
                                                byte[] buffer = id.Export();
                                                var bufferlen = buffer.Length;
                                                try
                                                {
                                                    var isNewFile = !File.Exists(usernamePath);
                                                    using (var fshandle = File.OpenHandle(usernamePath, isNewFile ? FileMode.Create : FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, FileOptions.None, isNewFile ? bufferlen : 0))
                                                    using (var fs = new FileStream(fshandle, FileAccess.ReadWrite))
                                                    {
                                                        var fslen = fs.Length;
                                                        // All this to avoid unnecessary write....
                                                        if (isNewFile || fslen != bufferlen)
                                                        {
                                                            fs.Write(buffer, 0, buffer.Length);
                                                            fs.Flush();
                                                        }
                                                        else
                                                        {
                                                            var content = new byte[fslen];
                                                            if (fs.Read(content, 0, content.Length) != content.Length || !id.Equals(content, StringComparison.Ordinal))
                                                            {
                                                                fs.Position = 0;
                                                                fs.SetLength(bufferlen);
                                                                fs.Write(buffer, 0, buffer.Length);
                                                                fs.Flush();
                                                            }
                                                        }
                                                    }
                                                }
                                                finally
                                                {
                                                    Array.Clear(buffer);
                                                }
                                            }
                                        }
                                        if (loginForm.SelectedRememberOption == LoginPasswordRememberStyle.NonPersistentRemember)
                                        {
                                            this.ss_id = loginForm.GetUsername();
                                            this.ss_pw = loginForm.GetPassword();

                                            this.TabMainMenu.Dispatcher.TryInvoke(delegate
                                            {
                                                this.TabMainMenu.ForgetLoginInfoEnabled = true;
                                            });
                                        }
                                    }
                                }
                            }
                            else
                            {
                                this.Dispatcher.TryInvoke(delegate
                                {
                                    this.TabGameClientUpdateProgressBar.IsSelected = true;
                                });
                                var loginToken = await this.pso2HttpClient.LoginPSO2Async(this.ss_id, this.ss_pw, cancelToken);
                                if (loginToken.RequireOTP)
                                {
                                    for (int i = 1; i <= PSO2LoginDialog.OTP_RetryTimes_Safe; i++)
                                    {
                                        var otpDialog = new PSO2LoginOtpDialog() { Owner = this };
                                        if (i == 1)
                                        {
                                            otpDialog.DialogMessage = $"Your account has OTP-protection enabled.{Environment.NewLine}Please input OTP to login (Attempt 1/{PSO2LoginDialog.OTP_RetryTimes_Safe})";
                                        }
                                        else if (i == PSO2LoginDialog.OTP_RetryTimes_Safe)
                                        {
                                            otpDialog.DialogMessage = $"THIS IS THE LAST ATTEMPT, WRONG OTP FOR {PSO2LoginDialog.OTP_RetryTimes_Safe + 1} TIMES WILL TEMPORARILY LOCK YOUR ACCOUNT.{Environment.NewLine}Please input OTP to login (Attempt {PSO2LoginDialog.OTP_RetryTimes_Safe}/{PSO2LoginDialog.OTP_RetryTimes_Safe})";
                                        }
                                        else
                                        {
                                            otpDialog.DialogMessage = $"Please input OTP to login (Attempt {i}/{PSO2LoginDialog.OTP_RetryTimes_Safe})";
                                        }
                                        if (otpDialog.ShowCustomDialog(this) == true)
                                        {
                                            // var stuff = NormalizeOTP(_otp);
                                            if (await this.pso2HttpClient.AuthOTPAsync(loginToken, otpDialog.Otp, cancelToken).ConfigureAwait(true))
                                            {
                                                token = loginToken;

                                                otpDialog.ClearPassword();
                                                break;
                                            }
                                            else
                                            {
                                                Prompt_Generic.Show(this, $"- Please check your OTP device's date and time.{Environment.NewLine}- Verify if the OTP system is in sync with the the system.", "Wrong OTP", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                            }
                                        }
                                        else
                                        {
                                            loginToken.Dispose();
                                            break;
                                        }
                                        otpDialog.ClearPassword();
                                    }
                                }
                                else
                                {
                                    token = loginToken;
                                }
                            }
                        }

                        if ((requestedStyle == GameStartStyle.StartWithToken && token != null) || requestedStyle == GameStartStyle.StartWithoutToken || isLaunchWithTweaker)
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

                                void completed(object sender, string dir, bool isCancelled, IReadOnlyCollection<PatchListItem> patchlist, IReadOnlyDictionary<PatchListItem, bool?> results)
                                {
                                    this.pso2Updater.OperationCompleted -= completed;
                                    this.Dispatcher.TryInvoke(delegate
                                    {
                                        if (isLaunchWithTweaker)
                                        {
                                            this.TabGameClientUpdateProgressBar.IsIndetermined = true;
                                        }
                                        else
                                        {
                                            this.TabMainMenu.IsSelected = true;
                                        }
                                    });
                                }
                                this.pso2Updater.OperationCompleted += completed;
                                this.TabGameClientUpdateProgressBar.IsIndetermined = true;
                                this.TabGameClientUpdateProgressBar.ResetMainProgressBarState();
                                this.TabGameClientUpdateProgressBar.ResetAllSubDownloadState();
                                // this.TabGameClientUpdateProgressBar.SetProgressBarCount(pso2Updater.ConcurrentDownloadCount);

                                if (checkUpdateBeforeLaunch)
                                {
                                    var hasUpdate = await this.pso2Updater.CheckForPSO2Updates(dir_pso2bin, cancelToken);
                                    if (hasUpdate)
                                    {
                                        if (Prompt_Generic.Show(this, "It seems like your client is not updated. Continue anyway?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                                        {
                                            return;
                                        }
                                    }
                                }

                                this.CreateNewParagraphInLog("[GameStart] Quick check executable files...");

                                // Select "Balanced" for safety reason.
                                await this.pso2Updater.ScanAndDownloadFilesAsync(dir_pso2bin, dir_classic_data, dir_pso2tweaker, GameClientSelection.Always_Only, FileScanFlags.Balanced, FileScanFlags.CacheOnly, false, cancelToken);

                                if (!cancelToken.IsCancellationRequested)
                                {
                                    if (isLaunchWithTweaker)
                                    {
                                        var pso2tweakerconfig = new PSO2TweakerConfig();

                                        if (pso2tweakerconfig.Load())
                                        {
                                            this._isTweakerRunning = true;
                                            string tweakerpso2bin = string.IsNullOrWhiteSpace(pso2tweakerconfig.PSO2JPBinFolder) ? string.Empty : Path.GetFullPath(pso2tweakerconfig.PSO2JPBinFolder);
                                            bool differentpath = !string.Equals(tweakerpso2bin, dir_pso2bin, StringComparison.OrdinalIgnoreCase);
                                            string pso2tweakerpso2clientversionpath = Path.GetFullPath(Path.Combine("SEGA", "PHANTASYSTARONLINE2", "_version.ver"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                                            string pso2tweakerpso2clientversion = File.Exists(pso2tweakerpso2clientversionpath) ? File.ReadAllText(pso2tweakerpso2clientversionpath) : string.Empty;
                                            string pso2tweakercheckforgameupdate = pso2tweakerconfig.UpdateChecks;
                                            bool shouldSave = false;
                                            if (differentpath)
                                            {
                                                pso2tweakerconfig.PSO2JPBinFolder = dir_pso2bin;
                                                shouldSave = true;
                                            }
                                            bool differentUpdateChecks = !string.Equals(pso2tweakercheckforgameupdate, "Manual", StringComparison.Ordinal);
                                            if (differentUpdateChecks)
                                            {
                                                pso2tweakerconfig.UpdateChecks = "Manual";
                                                shouldSave = true;
                                            }
                                            if (shouldSave)
                                            {
                                                pso2tweakerconfig.Save();
                                                shouldSave = false;
                                            }
                                            var pso2versionlocal = pso2Updater.GetLocalPSO2Version(dir_pso2bin);
                                            bool differentversion = string.Equals(pso2tweakerpso2clientversion, pso2versionlocal, StringComparison.OrdinalIgnoreCase);
                                            if (differentversion)
                                            {
                                                File.WriteAllText(pso2tweakerpso2clientversionpath, pso2versionlocal);
                                            }
                                            this.CreateNewParagraphInLog("[Compatibility] PSO2 Tweaker's config has been patched.");
                                            try
                                            {
                                                using (var proc = new Process())
                                                {
                                                    proc.StartInfo.UseShellExecute = true;
                                                    proc.StartInfo.Verb = "runas";
                                                    proc.StartInfo.FileName = tweakerPath;
                                                    proc.StartInfo.ArgumentList.Add("-pso2jp");
                                                    proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(tweakerPath);
                                                    this.CreateNewParagraphInLog("[GameStart] Starting PSO2 Tweaker...");
                                                    proc.Start();
                                                    await proc.WaitForExitAsync(cancelToken);
                                                }
                                            }
                                            finally
                                            {
                                                if (differentpath && differentversion)
                                                {
                                                    File.WriteAllText(pso2tweakerpso2clientversionpath, pso2tweakerpso2clientversion);
                                                }
                                                if (pso2tweakerconfig.Load()) // Refresh everything.
                                                {
                                                    if (differentpath)
                                                    {
                                                        // In case the path setting is not changed AGAIN while Tweaker is running.
                                                        if (string.Equals(string.IsNullOrWhiteSpace(pso2tweakerconfig.PSO2JPBinFolder) ? string.Empty : Path.GetFullPath(pso2tweakerconfig.PSO2JPBinFolder), dir_pso2bin, StringComparison.OrdinalIgnoreCase))
                                                        {
                                                            // Revert it back.
                                                            pso2tweakerconfig.PSO2JPBinFolder = tweakerpso2bin;
                                                            shouldSave = true;
                                                        }
                                                    }
                                                    if (differentUpdateChecks)
                                                    {
                                                        pso2tweakerconfig.UpdateChecks = pso2tweakercheckforgameupdate;
                                                        shouldSave = true;
                                                    }
                                                }
                                                if (shouldSave)
                                                {
                                                    pso2tweakerconfig.Save();
                                                }
                                                this.CreateNewParagraphInLog("[Compatibility] PSO2 Tweaker's config has been restored.");
                                                this._isTweakerRunning = false;
                                                this.TabMainMenu.IsSelected = true;
                                            }
                                        }
                                    }
                                    else
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
                                            this.CreateNewParagraphInLog("[GameStart] Starting game...");
                                        }
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
                catch (PSO2LoginException loginEx)
                {
                    this.CreateNewParagraphInLog($"[GameStart] Fail to start game due to SEGA login issue. Error code: {loginEx.ErrorCode}.");
                    Prompt_Generic.ShowError(this, "Failed to login to PSO2.", $"Network Error (Code: {loginEx.ErrorCode})", loginEx);
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
                {
                    // Silent it as user press "No" themselves.
                    // Prompt_Generic.Show(this, ex.Message, "User cancelled", MessageBoxButton.OK, MessageBoxImage.Warning);
                    this.CreateNewParagraphInLog("[GameStart] User cancelled");
                }
                catch (TaskCanceledException)
                {
                    this.CreateNewParagraphInLog("[GameStart] User cancelled");
                }
                catch (EmptyPatchListException ex)
                {
                    this.CreateNewParagraphInLog("[GameUpdater] " + ex.Message);
                    Prompt_Generic.ShowError(this, ex);
                }
                catch (System.Net.Http.HttpRequestException ex)
                {
                    var errorCode = (ex.StatusCode.HasValue ? ex.StatusCode.Value.ToString() : "Unknown");
                    this.CreateNewParagraphInLog($"[GameStart] Fail to start game due to network problem. Error code: {errorCode}. Message: " + ex.Message);
                    Prompt_Generic.ShowError(this, ex, "Network Error (Code: " + errorCode + ")");
                }
                catch (System.Net.WebException ex)
                {
                    string errorCode;
                    if (ex.Response is System.Net.HttpWebResponse response)
                    {
                        errorCode = response.StatusDescription;
                    }
                    else
                    {
                        errorCode = ex.Status.ToString();
                    }
                    this.CreateNewParagraphInLog($"[GameStart] Fail to start game due to network problem. Error code: {errorCode}. Message: " + ex.Message);
                    Prompt_Generic.ShowError(this, ex, "Network Error (Code: " + errorCode + ")");
                }
                catch (DatabaseErrorException)
                {
                    this.CreateNewParagraphInLog("[GameUpdater] Error occured when opening file check cache database.");
                    Prompt_Generic.Show(this, "Error occured when opening database. Maybe you're clicking too fast. Please try again but slower.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex) when (!Debugger.IsAttached)
                {
                    this.CreateNewParagraphInLog("[GameStart] Fail to start game. Error message: " + ex.Message);
                    Prompt_Generic.ShowError(this, ex);
                }
                finally
                {
                    currentCancelSrc?.Dispose();
                    this.cancelSrc_gameupdater = null;
                    this.Dispatcher.TryInvoke(delegate { tab.GameStartEnabled = true; tab.IsSelected = true; });
                }
            }
        }
    }
}

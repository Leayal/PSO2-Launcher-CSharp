using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.PSO2Launcher.Core.UIElements;
using Leayal.SharedInterfaces;
using Leayal.Shared;
using Leayal.Shared.Windows;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Leayal.PSO2.UserConfig;
using System.Runtime.InteropServices;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class MainMenuWindow
    {
        private static readonly Lazy<Tuple<string, long>> lazy_PSO2EmptyIntegrityTableFileDoubleCheckInfo = new Lazy<Tuple<string, long>>(() =>
        {
            using (var contentStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Leayal.PSO2Launcher.Core.Resources.empty_d4455ebc2bef618f29106da7692ebc1a"))
            {
                return contentStream == null ?
                new Tuple<string, long>(string.Empty, 0) : new Tuple<string, long>(Helper.SHA1Hash.ComputeHashFromFile(contentStream), contentStream.Length);
            }
        });
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
        private async static Task<Process?> TryFindPSO2Process(string? fullpath = null)
        {
            return await Task.Factory.StartNew<Process?>(obj =>
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
                            if (string.Equals(processes[i].QueryFullProcessImageName(4096, true), imagePath, StringComparison.OrdinalIgnoreCase))
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

        private static Task<bool> ReplaceInGameIntegrityTableFile(string dir_pso2bin)
        {
            return Task.Factory.StartNew((obj) =>
            {
                if (obj is string pso2bin)
                {
                    byte[]? buffer = null;
                    try
                    {
                        var fullpath = Path.GetFullPath(Path.Combine("data", "win32", "d4455ebc2bef618f29106da7692ebc1a"), pso2bin);
                        var checksumFileExisted = File.Exists(fullpath);
                        bool shouldReplace = false;
                        if (checksumFileExisted)
                        {
                            using (var sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1))
                            using (var fs = new FileStream(fullpath, FileMode.Open, FileAccess.Read, FileShare.Read, 0, false))
                            {
                                var checkInfo = lazy_PSO2EmptyIntegrityTableFileDoubleCheckInfo.Value;
                                var requiredNumberOfBytes = (int)checkInfo.Item2;
                                if (buffer == null) buffer = ArrayPool<byte>.Shared.Rent(requiredNumberOfBytes + sha1.HashLengthInBytes + (sha1.HashLengthInBytes * sizeof(char) * 2) + 1);

                                if (fs.Length == checkInfo.Item2)
                                {
                                    static bool AppendHashData(IncrementalHash sha1, byte[] buffer, in int requiredNumberOfBytes)
                                    {
                                        sha1.AppendData(buffer, 0, requiredNumberOfBytes);
                                        return true;
                                    }

                                    if (fs.ReadEnsuredLength(buffer, 0, requiredNumberOfBytes) == requiredNumberOfBytes
                                    && AppendHashData(sha1, buffer, in requiredNumberOfBytes) && sha1.TryGetCurrentHash(buffer.AsSpan(requiredNumberOfBytes), out var hashSizeInBytes)
                                    && HashHelper.TryWriteHashToHexString(MemoryMarshal.Cast<byte, char>(buffer.AsSpan(requiredNumberOfBytes + hashSizeInBytes)), buffer.AsSpan(requiredNumberOfBytes, hashSizeInBytes), out var writtenCharactersInBytes)
                                    && !MemoryExtensions.Equals(MemoryMarshal.Cast<byte, char>(buffer.AsSpan(requiredNumberOfBytes + hashSizeInBytes, writtenCharactersInBytes)), checkInfo.Item1.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                    {
                                        shouldReplace = true;
                                    }
                                    // if (!string.Equals(Helper.SHA1Hash.ComputeHashFromFile(fs), checkInfo.Item1, StringComparison.OrdinalIgnoreCase)) shouldReplace = true;
                                }
                                else
                                {
                                    shouldReplace = true;
                                }
                            }
                        }
                        else
                        {
                            shouldReplace = true;
                        }
                        if (shouldReplace)
                        {
                            using (var contentStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Leayal.PSO2Launcher.Core.Resources.empty_d4455ebc2bef618f29106da7692ebc1a"))
                            {
                                if (contentStream != null)
                                {
                                    if (checksumFileExisted)
                                    {
                                        var dirName = Path.GetDirectoryName(fullpath.AsSpan());
                                        var backupDir = dirName.IsEmpty ? Path.GetFullPath(Path.Combine("data", "win32", "backup"), pso2bin) : Path.Join(dirName, "backup");
                                        if (!Directory.Exists(backupDir))
                                        {
                                            Directory.CreateDirectory(backupDir);
                                        }
                                        File.Move(fullpath, Path.Join(backupDir, Path.GetFileName(fullpath.AsSpan())), true);
                                    }

                                    var len = contentStream.Length;
                                    using (var dstFileHandle = File.OpenHandle(fullpath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, FileOptions.None, len))
                                    using (var dstFileStream = new FileStream(dstFileHandle, FileAccess.Write, 0, dstFileHandle.IsAsync))
                                    {
                                        int read;
                                        if (buffer == null) buffer = ArrayPool<byte>.Shared.Rent((int)len);
                                        while ((read = contentStream.Read(buffer, 0, buffer.Length)) != 0)
                                        {
                                            dstFileStream.Write(buffer, 0, read);
                                        }
                                        dstFileStream.Flush();
                                    }

                                    return true;
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (buffer != null)
                        {
                            ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                }
                return false;
            }, dir_pso2bin);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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
                CancellationTokenSource? currentCancelSrc = null;
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

                        var val_AntiCheatProgramSelection = this.config_main.AntiCheatProgramSelection;
                        if (DataManagerWindow.IsWellbiaXignCodeAllowed())
                        {
                            if (val_AntiCheatProgramSelection == GameStartWithAntiCheatProgram.Unspecified)
                            {
                                if (Prompt_Generic.Show(this, "You haven't selected which Anti-cheat program to be used yet."
                                    + Environment.NewLine + "Do you want to select now?"
                                    + Environment.NewLine + "If you select 'No', the launcher will abort launching game."
                                    + Environment.NewLine + "You must select before this launcher can start the game. THIS IS IMPORTANT CHANGES.", "Important setting", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes
                                    && this.ShowDataManagerWindowDialog(dialog => dialog.ShowFocusAnticheatSelection()))
                                {
                                    val_AntiCheatProgramSelection = this.config_main.AntiCheatProgramSelection;
                                }
                                else
                                {
                                    return;
                                }
                            }
                        }
                        var filename = Path.GetFullPath(val_AntiCheatProgramSelection switch
                        {
                            GameStartWithAntiCheatProgram.Wellbia_XignCode => Path.Join("sub", "pso2.exe"),
                            _ => "pso2.exe"
                        }, dir_pso2bin);
                        if (!File.Exists(filename))
                        {
                            Prompt_Generic.Show(this, string.Concat("The file ", val_AntiCheatProgramSelection switch
                            {
                                GameStartWithAntiCheatProgram.nProtect_GameGuard => "'pso2.exe' for nProtect GameGuard",
                                GameStartWithAntiCheatProgram.Wellbia_XignCode => "'pso2.exe' for Wellbia's XignCode",
                                _ => "pso2.exe"
                            }, " doesn't exist. Please download game's data files if you haven't done it, or switch to another anti-cheat program to see if it helps.", Environment.NewLine, "Path: ", filename), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                                                        this.CreateNewWarnLineInConsoleLog("GameStart", "The game is already running. Giving the game's window focus...");
                                                    }
                                                }
                                                return;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            this.CreateNewErrorLineInConsoleLog("GameStart", "Cannot terminate the current PSO2 process.", "Error", ex);
                                            // this.CreateNewParagraphInLog("[GameStart] Cannot terminate the current PSO2 process. Error message: " + ex.Message);
                                            Prompt_Generic.ShowError(this, $"Cannot terminate the current PSO2 process.{Environment.NewLine}Error Message: " + ex.Message, "Error", ex);
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
                                                    this.CreateNewWarnLineInConsoleLog("GameStart", "The game is already running. Giving the game's window focus...");
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
                                                this.CreateNewLineInConsoleLog("GameStart", "The game is already running.");
                                                Prompt_Generic.Show(this, "The game is already running.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                            }
                                            else
                                            {
                                                this.CreateNewLineInConsoleLog("GameStart", "The game is already running. Giving the game's window focus...");
                                            }
                                        }
                                        catch
                                        {
                                            this.CreateNewLineInConsoleLog("GameStart", "The game is already running.");
                                            Prompt_Generic.Show(this, "The game is already running.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                        }
                                        return;
                                    }
                                }
                            }
                        }

                        string? dir_classic_data = this.config_main.PSO2Enabled_Classic ? this.config_main.PSO2Directory_Classic : null,
                            dir_pso2tweaker = this.config_main.PSO2Tweaker_CompatEnabled ? this.config_main.PSO2Tweaker_Bin_Path : null;
                        dir_classic_data = string.IsNullOrWhiteSpace(dir_classic_data) ? null : Path.GetFullPath(dir_classic_data, dir_pso2bin);
                        dir_pso2tweaker = string.IsNullOrWhiteSpace(dir_pso2tweaker) || !File.Exists(dir_pso2tweaker) ? null : Path.GetDirectoryName(dir_pso2tweaker);

                        var configFolderPath = Path.GetFullPath("config", RuntimeValues.RootDirectory);
                        var usernamePath = Path.Combine(configFolderPath, "SavedUsername.txt");
                        if (e.SelectedStyle == GameStartStyle.StartWithToken && !File.Exists(usernamePath))
                        {
                            if (Prompt_Generic.Show(this, $"If you don't trust this. Please do not use this, instead, start the game without login.{Environment.NewLine}Do you really trust this?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            {
                                return;
                            }
                            File.Create(usernamePath)?.Dispose();
                        }

                        currentCancelSrc = CancellationTokenSource.CreateLinkedTokenSource(this.cancelAllOperation.Token);
                        var cancelToken = currentCancelSrc.Token;
                        // bool isOKay = false;
                        PSO2LoginToken? token = null;

                        if (requestedStyle == GameStartStyle.StartWithToken)
                        {
                            if (this.ss_id == null || this.ss_pw == null)
                            {
                                this.ForgetSEGALogin();
                                SecureString? username;
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
                                    var pso2RemoteVersion = await this.pso2Updater.GetRemoteVersionAsync(cancelToken);
                                    var hasUpdate = await this.pso2Updater.CheckForPSO2Updates(dir_pso2bin, pso2RemoteVersion, cancelToken);
                                    if (hasUpdate)
                                    {
                                        if (Prompt_Generic.Show(this, "It seems like your client is not updated. Continue anyway?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                                        {
                                            return;
                                        }
                                    }
                                }

                                if (this.config_main.LauncherCorrectPSO2DataDownloadSelectionWhenGameStart)
                                {
                                    var path_pso2conf = Path.GetFullPath(Path.Combine("SEGA", "PHANTASYSTARONLINE2", "user.pso2"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                                    var downloadMode = this.config_main.DownloadSelection;
                                    UserConfig conf;
                                    if (!File.Exists(path_pso2conf))
                                    {
                                        var folder = Path.GetDirectoryName(path_pso2conf);
                                        if (folder != null)
                                        {
                                            Directory.CreateDirectory(folder);
                                        }
                                        conf = new UserConfig("Ini");
                                    }
                                    else
                                    {
                                        conf = UserConfig.FromFile(path_pso2conf);
                                    }

                                    // For now, only adjust the "DataDownload" property in the config file.
                                    // Leave FirstDownloadCheck alone.
                                    if (PSO2DeploymentWindow.AdjustPSO2UserConfig(conf, downloadMode))
                                    {
                                        conf.SaveAs(path_pso2conf);
                                    }
                                    /*
                                    switch (downloadMode)
                                    {
                                        case GameClientSelection.NGS_Only:
                                        case GameClientSelection.NGS_Prologue_Only:
                                            if (GameClientUpdater.AdjustPSO2UserConfig_FirstDownloadCheck(conf, downloadMode) || PSO2DeploymentWindow.AdjustPSO2UserConfig(conf, downloadMode))
                                            {
                                                conf.SaveAs(path_pso2conf);
                                            }
                                            break;
                                        case GameClientSelection.NGS_AND_CLASSIC:
                                        case GameClientSelection.Classic_Only:
                                            // Quick check for client data files.
                                            if (string.Equals(this.pso2Updater.GetLocalPSO2Version(dir_pso2bin), pso2RemoteVersion.ToString(), StringComparison.OrdinalIgnoreCase))
                                            {
                                                var listOfClassicFiles = await this.pso2HttpClient.GetPatchListClassicAsync(cancelToken);
                                                bool isOkay = true;
                                                foreach (var item in listOfClassicFiles)
                                                {
                                                    if (!Leayal.Shared.Windows.Kernel32.FileExists(item.GetFilenameWithoutAffix()))
                                                    {
                                                        isOkay = false;
                                                    }
                                                }
                                                if (isOkay && (GameClientUpdater.AdjustPSO2UserConfig_FirstDownloadCheck(conf, downloadMode) || PSO2DeploymentWindow.AdjustPSO2UserConfig(conf, downloadMode)))
                                                {
                                                    conf.SaveAs(path_pso2conf);
                                                }
                                            }
                                            break;
                                    }
                                    */
                                }

                                if (!cancelToken.IsCancellationRequested)
                                {
                                    var ___willReplaceHashTable = this.config_main.LauncherDisableInGameFileIntegrityCheck;
                                    this.CreateNewLineInConsoleLog("GameStart", "Quick check executable files...");
                                    // Select "Balanced" for safety reason.
                                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                                    static FileScanFlags AdjustFileScanFlagsIfHashTableReplacing(FileScanFlags flags, bool willReplaceHashTable) => (willReplaceHashTable ? (flags | FileScanFlags.IgnoreHashTableFile) : flags);

                                    await this.pso2Updater.ScanAndDownloadFilesAsync(dir_pso2bin, dir_classic_data, dir_pso2tweaker, 1, GameClientSelection.Always_Only, AdjustFileScanFlagsIfHashTableReplacing(FileScanFlags.Balanced, ___willReplaceHashTable), AdjustFileScanFlagsIfHashTableReplacing(FileScanFlags.CacheOnly, ___willReplaceHashTable), false, cancelToken);
                                    if (___willReplaceHashTable)
                                    {
                                        var replaced = await ReplaceInGameIntegrityTableFile(dir_pso2bin);
                                        if (replaced)
                                        {
                                            // Sound cool a** but if SEGA does strict check or use another file to contains the integrity table, I guess this is busted.
                                            this.CreateNewLineInConsoleLog("GameStart", "The integrity table file has been replaced with an empty table one. Client mods are now possible without error while loading modded files.");
                                        }
                                    }

                                    if (isLaunchWithTweaker)
                                    {
                                        var pso2tweakerconfig = new PSO2TweakerConfig();

                                        if (pso2tweakerconfig.Load())
                                        {
                                            string ResStr_Manual = "Manual";

                                            this._isTweakerRunning = true;
                                            string tweakerpso2bin = string.IsNullOrWhiteSpace(pso2tweakerconfig.PSO2JPBinFolder) ? string.Empty : Path.GetFullPath(pso2tweakerconfig.PSO2JPBinFolder);
                                            
                                            // Values before patching
                                            string pso2tweakerpso2clientversionpath = Path.GetFullPath(Path.Combine("SEGA", "PHANTASYSTARONLINE2", "_version.ver"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)),
                                                pso2tweakerpso2clientversion = File.Exists(pso2tweakerpso2clientversionpath) ? File.ReadAllText(pso2tweakerpso2clientversionpath) : string.Empty,
                                                pso2tweakercheckforgameupdate = pso2tweakerconfig.UpdateChecks,
                                                pso2tweakerJPRemoteVersion = pso2tweakerconfig.PSO2JPRemoteVersion;

                                            bool shouldSave = false;

                                            // Correct the PSO2_BIN path.
                                            bool differentpath = !string.Equals(tweakerpso2bin, dir_pso2bin, StringComparison.OrdinalIgnoreCase);
                                            if (differentpath)
                                            {
                                                pso2tweakerconfig.PSO2JPBinFolder = dir_pso2bin;
                                                shouldSave = true;
                                            }

                                            // Disable PSO2 client update checker
                                            bool differentUpdateChecks = !string.Equals(pso2tweakercheckforgameupdate, ResStr_Manual, StringComparison.Ordinal);
                                            if (differentUpdateChecks)
                                            {
                                                pso2tweakerconfig.UpdateChecks = ResStr_Manual;
                                                shouldSave = true;
                                            }

                                            // Readjust the PSO2JPRemoteVersion to match latest version to avoid installation prompt.
                                            var pso2versionlocal = pso2Updater.GetLocalPSO2Version(dir_pso2bin);
                                            bool differentJPRemoteVersion = !string.Equals(pso2tweakerJPRemoteVersion, pso2versionlocal, StringComparison.OrdinalIgnoreCase);
                                            if (differentJPRemoteVersion)
                                            {
                                                pso2tweakerconfig.PSO2JPRemoteVersion = pso2versionlocal;
                                                shouldSave = true;
                                            }

                                            bool differentversion = string.Equals(pso2tweakerpso2clientversion, pso2versionlocal, StringComparison.OrdinalIgnoreCase);
                                            if (differentversion)
                                            {
                                                File.WriteAllText(pso2tweakerpso2clientversionpath, pso2versionlocal);
                                            }
                                            if (shouldSave)
                                            {
                                                pso2tweakerconfig.Save();
                                                shouldSave = false;
                                            }

                                            this.CreateNewLineInConsoleLog("Compatibility", "PSO2 Tweaker's config has been patched.");
                                            try
                                            {
                                                using (var proc = new Process())
                                                {
                                                    proc.StartInfo.UseShellExecute = true;
                                                    proc.StartInfo.Verb = "runas";
                                                    proc.StartInfo.FileName = tweakerPath;
                                                    proc.StartInfo.Arguments = "-pso2jp";
                                                    proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(tweakerPath);
                                                    this.CreateNewLineInConsoleLog("GameStart", "Starting PSO2 Tweaker...");
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

                                                            if (differentJPRemoteVersion)
                                                            {
                                                                pso2tweakerconfig.PSO2JPRemoteVersion = pso2tweakerJPRemoteVersion;
                                                            }
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
                                                this.CreateNewLineInConsoleLog("Compatibility", "PSO2 Tweaker's config has been restored.");
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
                                            this.CreateNewLineInConsoleLog("GameStart", val_AntiCheatProgramSelection switch
                                            {
                                                GameStartWithAntiCheatProgram.nProtect_GameGuard => "Starting game with nProtect GameGuard...",
                                                GameStartWithAntiCheatProgram.Wellbia_XignCode => "Starting game with Wellbia Whatever...",
                                                _ => "Starting game..."
                                            });
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
                    var title = $"Network Error (Code: {loginEx.ErrorCode})";
                    this.CreateNewErrorLineInConsoleLog("GameStart", $"Fail to start game due to SEGA login issue. Error code: {loginEx.ErrorCode}.", title, loginEx);
                    Prompt_Generic.ShowError(this, "Failed to login to PSO2.", title, loginEx);
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
                {
                    // Silent it as user press "No" themselves.
                    // Prompt_Generic.Show(this, ex.Message, "User cancelled", MessageBoxButton.OK, MessageBoxImage.Warning);
                    this.CreateNewLineInConsoleLog("GameStart", "User cancelled");
                }
                catch (TaskCanceledException)
                {
                    this.CreateNewLineInConsoleLog("GameStart", "User cancelled");
                }
                catch (EmptyPatchListException ex)
                {
                    this.CreateNewErrorLineInConsoleLog("GameUpdater", string.Empty, null, ex);
                    Prompt_Generic.ShowError(this, ex);
                }
                catch (System.Net.Http.HttpRequestException ex)
                {
                    var errorCode = (ex.StatusCode.HasValue ? ex.StatusCode.Value.ToString() : "Unknown");
                    var title = "Network Error (Code: " + errorCode + ")";
                    this.CreateNewErrorLineInConsoleLog("GameStart", $"Fail to start game due to network problem. Error code: {errorCode}. Message: " + ex.Message, title, ex);
                    Prompt_Generic.ShowError(this, ex, title);
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
                    var title = "Network Error (Code: " + errorCode + ")";
                    this.CreateNewErrorLineInConsoleLog("GameStart", $"Fail to start game due to network problem. Error code: {errorCode}. Message: " + ex.Message, title, ex);
                    Prompt_Generic.ShowError(this, ex, title);
                }
                catch (DatabaseErrorException ex)
                {
                    this.CreateNewErrorLineInConsoleLog("GameStart", "Error occured when opening file check cache database.", null, ex);
                    Prompt_Generic.Show(this, "Error occured when opening database. Maybe you're clicking too fast. Please try again but slower.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex) when (!Debugger.IsAttached)
                {
                    this.CreateNewErrorLineInConsoleLog("GameStart", "Fail to start game. Error message: " + ex.Message, null, ex);
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

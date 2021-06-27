using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using Leayal.SharedInterfaces;
using System.Reflection;
using System.ComponentModel;
using Leayal.PSO2Launcher.Core.UIElements;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainMenuWindow : MetroWindowEx
    {
        private readonly PSO2HttpClient pso2HttpClient;
        private GameClientUpdater pso2Updater;
        private CancellationTokenSource cancelSrc;
        private Classes.ConfigurationFile config_main = new Classes.ConfigurationFile(Path.GetFullPath(Path.Combine("config", "launcher.json"), RuntimeValues.RootDirectory));

        static MainMenuWindow()
        {
            // Hack IE to IE11.

            // HKEY_CURRENT_USER\SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_LOCALMACHINE_LOCKDOWN
            try
            {
                using (var hive = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(Path.Combine("SOFTWARE", "Microsoft", "Internet Explorer", "Main", "FeatureControl", "FEATURE_BROWSER_EMULATION"), true))
                {
                    if (hive != null)
                    {
                        string filename;
                        using (var proc = Process.GetCurrentProcess())
                        {
                            filename = Path.GetFileName(proc.MainModule.FileName);
                        }
                        if (hive.GetValue(filename) is int verNum)
                        {
                            if (verNum < 11001)
                            {
                                hive.SetValue(filename, 11001, Microsoft.Win32.RegistryValueKind.DWord);
                                hive.Flush();
                            }
                        }
                        else
                        {
                            hive.SetValue(filename, 11001, Microsoft.Win32.RegistryValueKind.DWord);
                            hive.Flush();
                        }
                    }
                }
            }
            catch
            {
                // Optional anyway.
            }
        }

        public MainMenuWindow()
        {
            this.pso2HttpClient = new PSO2HttpClient();
            this.config_main = new Classes.ConfigurationFile(Path.GetFullPath(Path.Combine("config", "launcher.json"), RuntimeValues.RootDirectory));
            if (File.Exists(this.config_main.Filename))
            {
                this.config_main.Load();
            }
            InitializeComponent();
            var str = this.config_main.PSO2_BIN;
            if (!string.IsNullOrEmpty(str))
            {
                pso2Updater = CreateGameClientUpdater(Path.GetFullPath(str), this.pso2HttpClient);
            }
            this.TabMainMenu.IsSelected = true;
        }

        private void ThisWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.config_main.LauncherLoadWebsiteAtStartup)
            {
                this.ButtonLoadLauncherWebView.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void ThisWindow_Closed(object sender, EventArgs e)
        {
            // this.config_main.Save();
        }

        private void LoadLauncherWebView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                // this.RemoveLogicalChild(btn);
                try
                {
                    var obj = AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap(
                        Path.GetFullPath(Path.Combine("bin", "WebViewCompat.dll"), RuntimeValues.RootDirectory),
                        "Leayal.WebViewCompat.WebViewCompatControl",
                        false,
                        BindingFlags.CreateInstance,
                        null,
                        new object[] { "PSO2Launcher" },
                        null,
                        null);
                    var webview = (IWebViewCompatControl)obj;
                    webview.Initialized += this.WebViewCompatControl_Initialized;
                    var grid = (Grid)this.Content;
                    grid.Children.Remove(btn);
                    var element = (Control)obj;
                    Grid.SetRow(element, 2);
                    element.Margin = new Thickness(1);
                    grid.Children.Add(element);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
            }
        }

        private void TabMainMenu_ButtonGameStartClick(object sender, RoutedEventArgs e)
        {
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

                    var data = new SharedInterfaces.Communication.BootstrapElevation();
                    data.Filename = filename;
                    data.WorkingDirectory = dir_pso2bin;
                    data.Arguments = " +0x33aca2b9 -reboot -optimize";
                    data.EnvironmentVars.Add("-pso2", "+0x01e3d1e9");
                    var exitCode = ProcessHelper.CreateProcessElevated(data);
                    switch (exitCode)
                    {
                        case 0:
                            // Do nothing
                            break;
                        case 740:
                            MessageBox.Show(this, "The current user doesn't have the privilege to create a process as Administrator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                        default:
                            MessageBox.Show(this, "Unknown error. Exit code: " + exitCode, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                    }
                }
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                MessageBox.Show(this, ex.Message, "User cancelled", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WebViewCompatControl_Initialized(object sender, EventArgs e)
        {
            if (sender is IWebViewCompatControl webview)
            {
                webview.NavigateTo(new Uri("https://launcher.pso2.jp/ngs/01/"));

                // Lock the view to the URL above.
                webview.Navigating += this.Webview_Navigating;
            }
        }

        private void Webview_Navigating(object sender, NavigatingEventArgs e)
        {
            if (sender is IWebViewCompatControl wvc)
            {
                e.Cancel = true;
                // Hackish. De-elevate starting Url.

                if (e.Uri.IsAbsoluteUri)
                {
                    try
                    {
                        Process.Start("explorer.exe", "\"" + e.Uri.AbsoluteUri + "\"").Dispose();
                    }
                    catch
                    {

                    }
                }
                else
                {
                    if (Uri.TryCreate(wvc.CurrentUrl, e.Uri.ToString(), out var absUri))
                    {
                        try
                        {
                            Process.Start("explorer.exe", "\"" + absUri.AbsoluteUri + "\"").Dispose();
                        }
                        catch
                        {

                        }
                    }
                }
            }
        }

        private async void TabMainMenu_ButtonManageGameDataClick(object sender, RoutedEventArgs e)
        {
            if (sender is TabMainMenu tab)
            {
                tab.ButtonManageGameDataClicked -= this.TabMainMenu_ButtonManageGameDataClick;
                try
                {
                    var dialog = new DataManagerWindow(this.config_main);
                    dialog.Owner = this;
                    if (dialog.ShowDialog() == true)
                    {
                        var str = this.config_main.PSO2_BIN;
                        if (!string.IsNullOrEmpty(str))
                        {
                            var oldUpdater = pso2Updater;
                            pso2Updater = CreateGameClientUpdater(Path.GetFullPath(str), this.pso2HttpClient);
                            this.RegistryDisposeObject(pso2Updater);
                            if (oldUpdater != null)
                            {
                                await oldUpdater.DisposeAsync();
                            }
                        }
                    }
                }
                finally
                {
                    tab.ButtonManageGameDataClicked += this.TabMainMenu_ButtonManageGameDataClick;
                }
            }
        }

        private async void ButtonCheckForUpdate_Click(object sender, RoutedEventArgs e)
        {
            // Run test
            var dir_pso2bin = this.config_main.PSO2_BIN;
            if (pso2Updater == null || string.IsNullOrEmpty(dir_pso2bin))
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
                if (!Directory.Exists(dir_pso2bin))
                {
                    if (MessageBox.Show(this, "The 'pso2_bin' directory doesn't exist.\r\nContinue anyway (may result in full game download)?.\r\nPath: " + dir_pso2bin, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
            }

            this.TabGameClientUpdateProgressBar.SetProgressBarCount(pso2Updater.ConcurrentDownloadCount);
            this.TabGameClientUpdateProgressBar.IsIndetermined = true;
            this.TabGameClientUpdateProgressBar.IsSelected = true;
            try
            {
                var downloaderProfile = this.config_main.DownloaderProfile;
                var downloadType = this.config_main.DownloadSelection;
                var t_loadLocalHashDb = pso2Updater.LoadLocalHashCheck();

                this.cancelSrc?.Dispose();
                this.cancelSrc = new CancellationTokenSource();

                CancellationToken cancelToken = this.cancelSrc.Token;

                if (await pso2Updater.CheckForPSO2Updates(cancelToken))
                {
                    try
                    {
                        await t_loadLocalHashDb;
                    }
                    catch 
                    {
                        try
                        {
                            await pso2Updater.LoadLocalHashCheck();
                        }
                        catch
                        {
                            throw new FileCheckHashCache.DatabaseErrorException();
                        }
                    }
                    var t_fileCheck = pso2Updater.ScanForFilesNeedToDownload(downloadType, downloaderProfile, cancelToken);
                    var t_downloading = pso2Updater.StartDownloadFiles(cancelToken);

                    await Task.WhenAll(t_fileCheck, t_downloading); // Wasting but it's not much.
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
            finally
            {
                await this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.TabMainMenu.IsSelected = true;
                }));
            }
        }

        private void TabGameClientUpdateProgressBar_UpdateCancelClicked(object sender, RoutedEventArgs e)
        {
            if (this.cancelSrc != null)
            {
                if (!this.cancelSrc.IsCancellationRequested)
                {
                    this.cancelSrc.Cancel();
                }
            }
        }

        private GameClientUpdater CreateGameClientUpdater(string directory, PSO2HttpClient webclient)
        {
            var result = new GameClientUpdater(directory, Path.GetFullPath("leapso2launcher.CheckCache.dat", directory), webclient);
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

            result.FileCheckEnd += (sender) =>
            {
                this.Dispatcher.Invoke(new GameClientUpdater.FileCheckEndHandler((_sender) =>
                {
                    this.TabGameClientUpdateProgressBar.TopProgressBar.Text = "Checking completed. Waiting for downloads to complete.";
                    this.TabGameClientUpdateProgressBar.TopProgressBar.ShowDetailedProgressPercentage = false;
                }), sender);
            };

            result.OperationCompleted += (sender, totalfiles, failedfiles) =>
            {
                this.Dispatcher.BeginInvoke(new GameClientUpdater.OperationCompletedHandler((_sender, _totalfiles, _failedfiles) =>
                {
                    this.TabMainMenu.IsSelected = true;
                }), sender, totalfiles, failedfiles);
            };
            return result;
        }

        #region | WindowsCommandButtons |
        private void WindowsCommandButtons_Close_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SystemCommands.CloseWindow(this);
        }

        private void WindowsCommandButtons_Maximize_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SystemCommands.MaximizeWindow(this);
        }

        private void WindowsCommandButtons_Restore_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SystemCommands.RestoreWindow(this);
        }

        private void WindowsCommandButtons_Minimize_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SystemCommands.MinimizeWindow(this);
        }
        #endregion
    }
}

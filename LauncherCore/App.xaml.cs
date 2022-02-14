using Leayal.SharedInterfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using ControlzEx.Theming;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.Diagnostics;
using Leayal.Shared;
using Leayal.Shared.Windows;
using Leayal.PSO2Launcher.Helper;
using System.Windows.Media.Imaging;
using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Toolbox.Windows;
using Leayal.PSO2Launcher.Toolbox;

namespace Leayal.PSO2Launcher.Core
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public new static App Current => ((App)(Application.Current));
        public static readonly BitmapSource DefaultAppIcon = BitmapSourceHelper.FromWin32Icon(BootstrapResources.ExecutableIcon);
        public readonly JSTClockTimer JSTClock;

        public readonly int BootstrapVersion;

        private bool isLightMode;
        public bool IsLightMode => this.isLightMode;

        private readonly Classes.ConfigurationFile config_main;
        private readonly System.Windows.Forms.Form? dummyForm;

        public App() : this(1, null) { }

        public App(int bootstrapversion, System.Windows.Forms.Form? dummyForm) : base()
        {
            this.dummyForm = dummyForm;
            this.BootstrapVersion = bootstrapversion;
            this.config_main = new Classes.ConfigurationFile(Path.GetFullPath(Path.Combine("config", "launcher.json"), RuntimeValues.RootDirectory));
            this.JSTClock = new JSTClockTimer();

            this.InitializeComponent();

            if (File.Exists(this.config_main.Filename))
            {
                this.config_main.Load();
            }

            var thememgr = ThemeManager.Current;
            thememgr.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            thememgr.ThemeChanged += this.Thememgr_ThemeChanged;

            this.RefreshThemeSetting();
        }

        public void RefreshThemeSetting()
        {
            var thememgr = ThemeManager.Current;
            if (this.config_main.SyncThemeWithOS)
            {
                // thememgr.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
                thememgr.SyncTheme(ThemeSyncMode.SyncWithAppMode);
            }
            else
            {
                // thememgr.ThemeSyncMode = ThemeSyncMode.DoNotSync;
                thememgr.SyncTheme(ThemeSyncMode.DoNotSync);
                this.ChangeThemeMode(this.config_main.ManualSelectedThemeIndex != 0);
            }
        }

#nullable enable
        public Window? GetModalOrNull() => this.GetModalOrNull(null);

        public Window? GetModalOrNull(Predicate<Window>? predicate)
        {
            if (System.Windows.Interop.ComponentDispatcher.IsThreadModal)
            {
                var collection = this.Windows;
                var count = collection.Count;
                if (predicate == null)
                {
                    for (int i = count - 1; i >= 0; i--)
                    {
                        if (collection[i] is MetroWindowEx windowex)
                        {
                            if (windowex.IsActive || windowex.IsVisible)
                            {
                                return windowex;
                            }
                        }
                    }
                }
                else
                {
                    for (int i = count - 1; i >= 0; i--)
                    {
                        if (predicate(collection[i]))
                        {
                            return collection[i];
                        }
                    }
                }
            }
            return null;
        }

        public Window? GetTopMostWindowOfThisAppOrNull() => this.GetTopMostWindowOfThisAppOrNull(null);

        public Window? GetTopMostWindowOfThisAppOrNull(Predicate<Window>? predicate)
        {
            var collection = this.Windows;
            var count = collection.Count;
            if (System.Windows.Interop.ComponentDispatcher.IsThreadModal)
            {
                if (predicate == null)
                {
                    for (int i = count - 1; i >= 0; i--)
                    {
                        if (collection[i] is MetroWindowEx windowex)
                        {
                            if (windowex.IsActive || windowex.IsVisible)
                            {
                                return windowex;
                            }
                        }
                    }
                }
                else
                {
                    for (int i = count - 1; i >= 0; i--)
                    {
                        if (predicate(collection[i]))
                        {
                            return collection[i];
                        }
                    }
                }
            }
            if (count != 0)
            {
                return collection[count - 1];
            }
            else
            {
                return null;
            }
        }
#nullable restore

        private void Thememgr_ThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            var thememgr = (sender as ThemeManager) ?? ThemeManager.Current;
            var mode = string.Equals(e.NewTheme.BaseColorScheme, ThemeManager.BaseColorLight, StringComparison.OrdinalIgnoreCase);
            if (this.isLightMode != mode)
            {
                this.isLightMode = mode;
                if (mode)
                {
                    thememgr.ChangeThemeColorScheme(this, "Blue");
                }
                else
                {
                    thememgr.ChangeThemeColorScheme(this, "Red");
                }
                foreach (var window in this.Windows)
                {
                    if (window is MetroWindowEx windowex)
                    {
                        windowex.RefreshTheme();
                    }
                }
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var mainmenuwindow = new Windows.MainMenuWindow(this.config_main);
            this.MainWindow = mainmenuwindow;

            // this.MainWindow.Show();
            bool starttotray = false;

            var args = e.Args;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--tray", StringComparison.OrdinalIgnoreCase))
                {
                    starttotray = true;
                    break;
                }
            }

            if (starttotray)
            {
                if (this.dummyForm is System.Windows.Forms.Form form)
                {
                    form.Hide();
                    form.ShowInTaskbar = false;
                    form.Dispose();
                }
                mainmenuwindow.ShowActivated = false;
                mainmenuwindow.ShowInTaskbar = false;
                mainmenuwindow.Visibility = Visibility.Hidden;
                mainmenuwindow.Show();
                mainmenuwindow.Visibility = Visibility.Visible;
                mainmenuwindow.IsMinimizedToTray = true;
                mainmenuwindow.ShowActivated = true;
            }
            else
            {
                mainmenuwindow.Show();
                mainmenuwindow.FirstShown += this.Mainmenuwindow_FirstShown;
            }
        }

        private void Mainmenuwindow_FirstShown(object sender, EventArgs e)
        {
            if (sender is Windows.MainMenuWindow window)
            {
                window.FirstShown -= this.Mainmenuwindow_FirstShown;
                window.Activate();
                if (this.dummyForm is System.Windows.Forms.Form form)
                {
                    form.Hide();
                    form.ShowInTaskbar = false;
                    form.Dispose();
                }
            }
        }

        public void ChangeThemeMode(bool isLightMode)
        {
            if (this.isLightMode != isLightMode)
            {
                // this.isLightMode = isLightMode;
                if (isLightMode)
                {
                    ThemeManager.Current.ChangeTheme(this, ThemeManager.BaseColorLight, "Blue");
                }
                else
                {
                    ThemeManager.Current.ChangeTheme(this, ThemeManager.BaseColorDark, "Red");
                }
            }
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var str = e.Exception.ToString();
            using (var sw = new StreamWriter(Path.Combine(RuntimeValues.RootDirectory, "unhandled_dispatcher_error_wpf.txt"), true, System.Text.Encoding.UTF8))
            {
                sw.WriteLine();
                sw.WriteLine();
                sw.WriteLine(str);
                sw.Flush();
            }
            var window = this.MainWindow;
            if (window != null)
            {
                MessageBox.Show(window, str, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(str, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            e.Handled = true;
        }

        private static void RestartWithArgs(ICollection<string> commandLineArgs)
        {
            ProcessStartInfo processStartInfo = new();
            processStartInfo.FileName = RuntimeValues.EntryExecutableFilename;
            if (commandLineArgs != null && commandLineArgs.Count != 0)
            {
                foreach (var arg in commandLineArgs)
                {
                    processStartInfo.ArgumentList.Add(arg);
                }
            }
            System.Windows.Forms.Application.Exit();
            Process.Start(processStartInfo)?.Dispose();
        }

        public void ExecuteCommandUrl(Uri url)
        {
            if (url != null && url.IsAbsoluteUri)
            {
                var urlstr = url.AbsoluteUri;
                if (string.Equals(urlstr, StaticResources.Url_ConfirmSelfUpdate.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                {
                    this.Dispatcher.InvokeAsync(delegate
                    {
                        var mainwindow = this.MainWindow;
                        if (mainwindow != null)
                        {
                            bool isInTray;
                            if (mainwindow is Windows.MainMenuWindow window)
                            {
                                isInTray = window.IsMinimizedToTray;
                            }
                            else
                            {
                                isInTray = false;
                            }

                            void mainformclosed(object sender, EventArgs e)
                            {
                                if (sender is Window w)
                                {
                                    w.Closed -= mainformclosed;
                                }

                                this.Shutdown();
                                var args = new List<string>(Environment.GetCommandLineArgs());
                                args.RemoveAt(0);
                                if (!args.Contains("--no-self-update-prompt"))
                                {
                                    args.Add("--no-self-update-prompt");
                                }
                                if (args.Contains("--no-self-update"))
                                {
                                    args.RemoveAll(x => string.Equals(x, "--no-self-update", StringComparison.OrdinalIgnoreCase));
                                }
                                if (isInTray)
                                {
                                    if (!args.Contains("--tray"))
                                    {
                                        args.Add("--tray");
                                    }
                                }
                                else
                                {
                                    if (args.Contains("--tray"))
                                    {
                                        args.RemoveAll(x => string.Equals(x, "--tray", StringComparison.OrdinalIgnoreCase));
                                    }
                                }
                                RestartWithArgs(args);
                            };

                            mainwindow.Closed += mainformclosed;
                            mainwindow.Close();
                        }
                        else
                        {
                            this.Shutdown();
                            var args = new List<string>(Environment.GetCommandLineArgs());
                            args.RemoveAt(0);
                            if (!args.Contains("--no-self-update-prompt"))
                            {
                                args.Add("--no-self-update-prompt");
                            }
                            if (args.Contains("--no-self-update"))
                            {
                                args.RemoveAll(x => string.Equals(x, "--no-self-update", StringComparison.OrdinalIgnoreCase));
                            }
                            if (args.Contains("--tray"))
                            {
                                args.RemoveAll(x => string.Equals(x, "--tray", StringComparison.OrdinalIgnoreCase));
                            }
                            RestartWithArgs(args);
                        }

                        // System.Windows.Forms.Application.Restart();
                    });
                }
                else if (string.Equals(urlstr, StaticResources.Url_ShowAuthor.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            WindowsExplorerHelper.OpenUrlWithDefaultBrowser("https://github.com/Leayal");
                        }
                        catch { }
                    });
                }
                else if (string.Equals(urlstr, StaticResources.Url_ShowSourceCodeGithub.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            WindowsExplorerHelper.OpenUrlWithDefaultBrowser("https://github.com/Leayal/PSO2-Launcher-CSharp");
                        }
                        catch { }
                    });
                }
                else if (string.Equals(urlstr, StaticResources.Url_ShowIssuesGithub.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            WindowsExplorerHelper.OpenUrlWithDefaultBrowser("https://github.com/Leayal/PSO2-Launcher-CSharp/issues");
                        }
                        catch { }
                    });
                }
                else if (string.Equals(urlstr, StaticResources.Url_OpenWebView2InstallerDownloadPage.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            WindowsExplorerHelper.OpenUrlWithDefaultBrowser("https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section");
                        }
                        catch { }
                    });
                }
                else if (string.Equals(urlstr, StaticResources.Url_DownloadWebView2BootstrapInstaller.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            WindowsExplorerHelper.OpenUrlWithDefaultBrowser("https://go.microsoft.com/fwlink/p/?LinkId=2124703");
                        }
                        catch { }
                    });
                }
                else if (string.Equals(urlstr, StaticResources.Url_IgnoreSelfUpdate.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                {
                    this.Dispatcher.InvokeAsync(delegate
                    {
                        if (this.MainWindow is Windows.MainMenuWindow window)
                        {
                            window.IsUpdateNotificationVisible = false;
                        }
                    });
                }
                else if (string.Equals(urlstr, StaticResources.Url_ShowPathInExplorer_SpecialFolder_JP_PSO2Config.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                {
                    Task.Run(() =>
                    {
                        var directory = Path.GetFullPath(Path.Combine("SEGA", "PHANTASYSTARONLINE2"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                        var filepath = Path.Combine(directory, "user.pso2");
                        try
                        {
                            if (File.Exists(filepath))
                            {
                                WindowsExplorerHelper.SelectPathInExplorer(filepath);
                            }
                            else if (Directory.Exists(directory))
                            {
                                WindowsExplorerHelper.ShowPathInExplorer(directory);
                            }
                        }
                        catch { }
                    });
                }
                else if (string.Equals(urlstr, StaticResources.Url_Toolbox_AlphaReactorCounter.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var window = new ToolboxWindow_AlphaReactorCount(DefaultAppIcon, this.JSTClock, false, this.config_main.LauncherUseClock);
                        window.Show();
                    }
                    catch (Exception ex)
                    {
                        Core.Windows.Prompt_Generic.ShowError(this.MainWindow, ex);
                    }
                }
                else if (string.Equals(urlstr, StaticResources.Url_ShowLatestGithubRelease.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            WindowsExplorerHelper.OpenUrlWithDefaultBrowser("https://github.com/Leayal/PSO2-Launcher-CSharp/releases/latest");
                        }
                        catch { }
                    });
                }
                else if (urlstr.StartsWith(StaticResources.Url_ShowLogDialogFromGuid.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                {
                    var dialogguide = Guid.Parse(urlstr.AsSpan(StaticResources.Url_ShowLogDialogFromGuid.AbsoluteUri.Length));
                    if (dialogguide != Guid.Empty)
                    {
                        this.Dispatcher.BeginInvoke(new Action<Guid>((dialog_id) =>
                        {
                            if (this.MainWindow is Windows.MainMenuWindow mainmenu)
                            {
                                mainmenu.ShowLogDialogFromGuid(dialog_id);
                            }
                        }), new object[] { dialogguide });
                    }
                }
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            ThemeManager.Current.ThemeChanged -= this.Thememgr_ThemeChanged;
            this.JSTClock.Dispose();
            ToolboxWindow_AlphaReactorCount.DisposeLogWatcherIfCreated();
            // Double check and close all database connections.
            // Optimally, this should does nothing because all databases have been finalized and closed.
            // SQLite.SQLiteAsyncConnection.ResetPool();
        }
    }
}

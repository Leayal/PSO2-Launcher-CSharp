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

namespace Leayal.PSO2Launcher.Core
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public new static App Current => ((App)(Application.Current));

        private bool isLightMode;

        public bool IsLightMode => this.isLightMode;

        private readonly Classes.ConfigurationFile config_main;

        public App() : base()
        {
            this.InitializeComponent();

            this.config_main = new Classes.ConfigurationFile(Path.GetFullPath(Path.Combine("config", "launcher.json"), RuntimeValues.RootDirectory));
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
                thememgr.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
                thememgr.SyncTheme();
            }
            else
            {
                thememgr.ThemeSyncMode = ThemeSyncMode.DoNotSync;
                thememgr.SyncTheme();
                this.ChangeThemeMode(this.config_main.ManualSelectedThemeIndex != 0);
            }
        }

        private void Thememgr_ThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            if (sender is ThemeManager thememgr)
            {
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
                        if (window is Windows.MetroWindowEx windowex)
                        {
                            windowex.RefreshTheme();
                        }
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
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
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
                        this.MainWindow?.Close();
                        this.Shutdown();
                        var args = new List<string>(Environment.GetCommandLineArgs());
                        args.RemoveAt(0);
                        if (!args.Contains("--no-self-update-prompt"))
                        {
                            args.Add("--no-self-update-prompt");
                        }
                        if (this.MainWindow is Windows.MainMenuWindow window)
                        {
                            if (window.IsMinimizedToTray)
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
                        }
                        RestartWithArgs(args);
                        // System.Windows.Forms.Application.Restart();
                    });
                }
                else if (string.Equals(urlstr, StaticResources.Url_IgnoreSelfUpdate.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                {
                    this.Dispatcher.InvokeAsync(delegate
                    {
                        if (this.MainWindow is Windows.MainMenuWindow window)
                        {
                            window.SelfUpdateNotification.Visibility = Visibility.Collapsed;
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
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            ThemeManager.Current.ThemeChanged -= this.Thememgr_ThemeChanged;
        }
    }
}

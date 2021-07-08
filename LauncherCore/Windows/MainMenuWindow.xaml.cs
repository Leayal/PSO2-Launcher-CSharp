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
using Leayal.PSO2Launcher.Helper;
using Leayal.PSO2Launcher.Core.Classes;
using System.Windows.Media.Imaging;
using System.Windows.Media;

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
        private readonly ConfigurationFile config_main;
        private readonly Lazy<BitmapSource?> lazybg_dark, lazybg_light;

        public MainMenuWindow()
        {
            this.ss_id = null;
            this.ss_pw = null;
            this.pso2HttpClient = new PSO2HttpClient();
            this.config_main = new Classes.ConfigurationFile(Path.GetFullPath(Path.Combine("config", "launcher.json"), RuntimeValues.RootDirectory));
            if (File.Exists(this.config_main.Filename))
            {
                this.config_main.Load();
            }
            this.lazybg_dark = new Lazy<BitmapSource?>(() => BitmapSourceHelper.FromEmbedResourcePath("Leayal.PSO2Launcher.Core.Resources._bgimg_dark.png"));
            this.lazybg_light = new Lazy<BitmapSource?>(() => BitmapSourceHelper.FromEmbedResourcePath("Leayal.PSO2Launcher.Core.Resources._bgimg_light.png"));
            InitializeComponent();
            string dir_root = this.config_main.PSO2_BIN,
                dir_classic_data = this.config_main.PSO2Enabled_Classic ? this.config_main.PSO2Directory_Classic : null,
                dir_reboot_data = this.config_main.PSO2Enabled_Reboot ? this.config_main.PSO2Directory_Reboot : null;
            if (!string.IsNullOrEmpty(dir_root))
            {
                dir_root = Path.GetFullPath(dir_root);
                dir_classic_data = string.IsNullOrWhiteSpace(dir_classic_data) ? null : Path.GetFullPath(dir_classic_data);
                dir_reboot_data = string.IsNullOrWhiteSpace(dir_reboot_data) ? null : Path.GetFullPath(dir_reboot_data);
                this.pso2Updater = CreateGameClientUpdater(dir_root, dir_classic_data, dir_reboot_data, this.pso2HttpClient);
            }
            this.TabMainMenu.IsSelected = true;
        }

        private async void ThisWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.RegistryDisposeObject(AsyncDisposeObject.CreateFrom(async delegate
            {
                await FileCheckHashCache.ForceCloseAll();
            }));
            
            try
            {
                if (App.Current.IsLightMode)
                {
                    this.BgImg.Source = lazybg_light.Value;
                }
                else
                {
                    this.BgImg.Source = lazybg_dark.Value;
                }
            }
            catch { }

            if (this.config_main.LauncherLoadWebsiteAtStartup)
            {
                this.ButtonLoadLauncherWebView.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }

            if (this.config_main.LauncherCheckForPSO2GameUpdateAtStartup)
            {
                await StartGameClientUpdate(false, this.config_main.LauncherCheckForPSO2GameUpdateAtStartupPrompt);
                // this.ButtonCheckForUpdate_Click(null, new RoutedEventArgs());
                // this.ButtonLoadLauncherWebView.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        protected override void OnThemeRefresh()
        {
            if (App.Current.IsLightMode)
            {
                this.BgImg.Source = lazybg_light.Value;
            }
            else
            {
                this.BgImg.Source = lazybg_dark.Value;
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
                    using (var hive = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(Path.Combine("SOFTWARE", "Microsoft", "Internet Explorer", "Main", "FeatureControl", "FEATURE_BROWSER_EMULATION"), true))
                    {
                        if (hive != null)
                        {
                            string filename = RuntimeValues.EntryExecutableFilename;
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
                try
                {
                    if (e.Uri.IsAbsoluteUri)
                    {
                        Process.Start("explorer.exe", "\"" + e.Uri.AbsoluteUri + "\"")?.Dispose();
                    }
                    else if (Uri.TryCreate(wvc.CurrentUrl, e.Uri.ToString(), out var absUri))
                    {
                        Process.Start("explorer.exe", "\"" + absUri.AbsoluteUri + "\"")?.Dispose();
                    }
                }
                catch { }
            }
        }

        private async void TabMainMenu_ButtonManageGameLauncherBehaviorClicked(object sender, RoutedEventArgs e)
        {
            if (sender is TabMainMenu tab)
            {
                tab.ButtonManageGameLauncherBehaviorClicked -= this.TabMainMenu_ButtonManageGameLauncherBehaviorClicked;
                try
                {
                    var dialog = new LauncherBehaviorManagerWindow(this.config_main);
                    dialog.Owner = this;

                    dialog.ShowDialog();
                }
                finally
                {
                    tab.ButtonManageGameLauncherBehaviorClicked += this.TabMainMenu_ButtonManageGameLauncherBehaviorClicked;
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
                        string dir_root = this.config_main.PSO2_BIN,
                            dir_classic_data = this.config_main.PSO2Enabled_Classic ? this.config_main.PSO2Directory_Classic : null,
                            dir_reboot_data = this.config_main.PSO2Enabled_Reboot ? this.config_main.PSO2Directory_Reboot : null;
                        if (string.IsNullOrEmpty(dir_root))
                        {
                            var oldUpdater = this.pso2Updater;
                            this.pso2Updater = null;
                            await oldUpdater.DisposeAsync();
                        }
                        else
                        {
                            dir_root = Path.GetFullPath(dir_root);
                            dir_classic_data = string.IsNullOrWhiteSpace(dir_classic_data) ? null : Path.GetFullPath(dir_classic_data);
                            dir_reboot_data = string.IsNullOrWhiteSpace(dir_reboot_data) ? null : Path.GetFullPath(dir_reboot_data);
                            var oldUpdater = this.pso2Updater;
                            if (oldUpdater == null)
                            {
                                this.pso2Updater = CreateGameClientUpdater(dir_root, dir_classic_data, dir_reboot_data, this.pso2HttpClient);
                                this.RegistryDisposeObject(this.pso2Updater);
                            }
                            else
                            {
                                if (!string.Equals(oldUpdater.Path_PSO2BIN, dir_root, StringComparison.OrdinalIgnoreCase) ||
                                    !string.Equals(oldUpdater.Path_PSO2RebootData, dir_reboot_data, StringComparison.OrdinalIgnoreCase) ||
                                    !string.Equals(oldUpdater.Path_PSO2ClassicData, dir_classic_data, StringComparison.OrdinalIgnoreCase))
                                {
                                    this.pso2Updater = CreateGameClientUpdater(dir_root, dir_classic_data, dir_reboot_data, this.pso2HttpClient);
                                    this.RegistryDisposeObject(this.pso2Updater);
                                    await oldUpdater.DisposeAsync();
                                }
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

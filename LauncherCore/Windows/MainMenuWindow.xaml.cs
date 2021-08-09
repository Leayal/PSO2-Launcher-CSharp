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
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Net.Http;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainMenuWindow : MetroWindowEx
    {
        private readonly HttpClient webclient;
        private readonly PSO2HttpClient pso2HttpClient;
        private GameClientUpdater pso2Updater;
        private CancellationTokenSource cancelSrc;
        private readonly ConfigurationFile config_main;
        private readonly Lazy<BitmapSource?> lazybg_dark, lazybg_light;
        private readonly Lazy<System.Windows.Forms.NotifyIcon> trayIcon;
        private readonly ToggleButton[] toggleButtons;
        private readonly Lazy<Task<BackgroundSelfUpdateChecker>> backgroundselfupdatechecker;

        public MainMenuWindow() : base()
        {
            this.ss_id = null;
            this.ss_pw = null;
            this.webclient = new HttpClient(new SocketsHttpHandler()
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = System.Net.DecompressionMethods.All,
                ConnectTimeout = TimeSpan.FromSeconds(30),
                UseProxy = false,
                UseCookies = false,
                Credentials = null,
                DefaultProxyCredentials = null
            }, true);
            this.pso2HttpClient = new PSO2HttpClient(this.webclient);
            this.backgroundselfupdatechecker = new Lazy<Task<BackgroundSelfUpdateChecker>>(() => Task.Run(() =>
            {
                var binDir = Path.Combine(RuntimeValues.RootDirectory, "bin");
                var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var removelen = binDir.Length + 1;

                if (Directory.Exists(binDir))
                {
                    foreach (var filename in Directory.EnumerateFiles(binDir, "*.dll", SearchOption.AllDirectories))
                    {
                        var sha1 = SHA1Hash.ComputeHashFromFile(filename);
                        dictionary.Add(filename.Remove(0, removelen), sha1);
                    }
                }
                var selfupdatecheck = new BackgroundSelfUpdateChecker(this.webclient, dictionary);
                selfupdatecheck.UpdateFound += this.OnSelfUpdateFound;
                return selfupdatecheck;
            }));

            this.config_main = new Classes.ConfigurationFile(Path.GetFullPath(Path.Combine("config", "launcher.json"), RuntimeValues.RootDirectory));
            if (File.Exists(this.config_main.Filename))
            {
                this.config_main.Load();
            }
            this.lazybg_dark = new Lazy<BitmapSource?>(() => BitmapSourceHelper.FromEmbedResourcePath("Leayal.PSO2Launcher.Core.Resources._bgimg_dark.png"));
            this.lazybg_light = new Lazy<BitmapSource?>(() => BitmapSourceHelper.FromEmbedResourcePath("Leayal.PSO2Launcher.Core.Resources._bgimg_light.png"));
            this.trayIcon = new Lazy<System.Windows.Forms.NotifyIcon>(CreateNotifyIcon);
            InitializeComponent();
            this.toggleButtons = new ToggleButton[] { this.ToggleBtn_PSO2News, this.ToggleBtn_RSSFeed, this.ToggleBtn_ConsoleLog };
            var pathlaststate_selectedtogglebuttons = Path.GetFullPath(Path.Combine("config", "state_togglebtns.txt"), RuntimeValues.RootDirectory);
            if (File.Exists(pathlaststate_selectedtogglebuttons))
            {
                var line = QuickFile.ReadFirstLine(pathlaststate_selectedtogglebuttons);
                foreach (var btn in this.toggleButtons)
                {
                    if (string.Equals(line, btn.Name, StringComparison.Ordinal))
                    {
                        btn.IsChecked = true;
                        break;
                    }
                }
            }
            _ = this.CreateNewParagraphInLog(writer =>
            {
                writer.Write($"[Lea] Welcome to PSO2 Launcher, which was made by Dramiel Leayal");
            }, false);
        }

        private void ThisWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.TabMainMenu.DefaultGameStartStyle = this.config_main.DefaultGameStartStyle;

            this.RegistryDisposeObject(AsyncDisposeObject.CreateFrom(async delegate
            {
                await FileCheckHashCache.ForceCloseAll();
                if (this.backgroundselfupdatechecker.IsValueCreated)
                {
                    var checker = await this.backgroundselfupdatechecker.Value;
                    checker.Dispose();
                }
            }));

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
            RSSFeedPresenter_Loaded();
        }

        protected override async void OnFirstShown(EventArgs e)
        {
            try
            {
                base.OnFirstShown(e);
            }
            catch
            {

            }

            if (this.config_main.LauncherLoadWebsiteAtStartup)
            {
                if (this.LauncherWebView.Child is Button btn)
                {
                    btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
            }

            if (this.config_main.LauncherCheckForPSO2GameUpdateAtStartup)
            {
                await StartGameClientUpdate(false, this.config_main.LauncherCheckForPSO2GameUpdateAtStartupPrompt);
            }

            if (this.config_main.LauncherCheckForSelfUpdates)
            {
                var selfchecker = await this.backgroundselfupdatechecker.Value;
                selfchecker.TickTime = TimeSpan.FromSeconds(this.config_main.LauncherCheckForSelfUpdates_IntervalHour);
                selfchecker.Start();
            }
        }

        protected override void OnThemeRefresh()
        {
            if (App.Current.IsLightMode)
            {
                this.BgImg.Source = lazybg_light.Value;
                _ = this.CreateNewParagraphInLog(writer =>
                {
                    writer.Write($"[ThemeManager] Detected Windows 10's theme change: Light Mode.");
                });
            }
            else
            {
                this.BgImg.Source = lazybg_dark.Value;
                _ = this.CreateNewParagraphInLog(writer =>
                {
                    writer.Write($"[ThemeManager] Detected Windows 10's theme change: Dark Mode.");
                });
            }
        }

        private void ThisWindow_Closed(object sender, EventArgs e)
        {
            // this.config_main.Save();
            if (this.trayIcon.IsValueCreated)
            {
                this.trayIcon.Value.Visible = false;
                this.trayIcon.Value.Dispose();
            }
            foreach (var btn in this.toggleButtons)
            {
                if (btn.IsChecked == true)
                {
                    var path = Path.GetFullPath(Path.Combine("config", "state_togglebtns.txt"), RuntimeValues.RootDirectory);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, btn.Name);
                    break;
                }
            }
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
                            string filename = Path.GetFileName(RuntimeValues.EntryExecutableFilename);
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
                    this.LauncherWebView.Child = (Control)obj;
                    _ = this.CreateNewParagraphInLog(writer =>
                    {
                        writer.Write("[WebView] PSO2's launcher news has been loaded.");
                    });
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
                webview.Navigated += this.Webview_Navigated;
                webview.NavigateTo(new Uri("https://launcher.pso2.jp/ngs/01/"));
            }
        }

        private void Webview_Navigated(object sender, NavigationEventArgs e)
        {
            if (sender is IWebViewCompatControl webview)
            {
                webview.Navigated -= this.Webview_Navigated;
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

                    if (dialog.ShowDialog() == true)
                    {
                        this.TabMainMenu.DefaultGameStartStyle = this.config_main.DefaultGameStartStyle;

                        if (this.config_main.LauncherCheckForSelfUpdates)
                        {
                            var isnewselfchecker = this.backgroundselfupdatechecker.IsValueCreated;
                            var selfchecker = await this.backgroundselfupdatechecker.Value;
                            if (!isnewselfchecker)
                            {
                                selfchecker.Stop();
                            }
                            selfchecker.TickTime = TimeSpan.FromHours(this.config_main.LauncherCheckForSelfUpdates_IntervalHour);
                            selfchecker.Start();
                        }
                        else
                        {
                            if (this.backgroundselfupdatechecker.IsValueCreated)
                            {
                                (await this.backgroundselfupdatechecker.Value).Stop();
                            }
                        }
                    }
                }
                finally
                {
                    tab.ButtonManageGameLauncherBehaviorClicked += this.TabMainMenu_ButtonManageGameLauncherBehaviorClicked;
                }
            }
        }

        private void TabMainMenu_ButtonPSO2GameOptionClicked(object sender, RoutedEventArgs e)
        {
            if (sender is TabMainMenu tab)
            {
                tab.ButtonPSO2GameOptionClicked -= this.TabMainMenu_ButtonPSO2GameOptionClicked;
                try
                {
                    var dialog = new PSO2UserConfigurationWindow();
                    dialog.Owner = this;

                    dialog.ShowDialog();
                }
                finally
                {
                    tab.ButtonPSO2GameOptionClicked += this.TabMainMenu_ButtonPSO2GameOptionClicked;
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

        private async Task CreateNewParagraphInLog(Action<ICSharpCode.AvalonEdit.Document.DocumentTextWriter> callback, bool newline = true, bool followLastLine = true)
        {
            if (this.ConsoleLog.CheckAccess())
            {
                var textlength = this.ConsoleLog.Document.TextLength;
                using (var writer = new ICSharpCode.AvalonEdit.Document.DocumentTextWriter(this.ConsoleLog.Document, textlength))
                {
                    // Is last line in view
                    bool isAlreadyInLastLineView = (followLastLine ? ((this.ConsoleLog.VerticalOffset + this.ConsoleLog.ViewportHeight) >= (this.ConsoleLog.ExtentHeight - 1d)) : false);

                    if (newline)
                    {
                        if (textlength != 0)
                        {
                            writer.WriteLine();
                        }
                    }
                    callback.Invoke(writer);
                    if (isAlreadyInLastLineView)
                    {
                        this.ConsoleLog.ScrollToEnd();
                    }
                }
            }
            else
            {
                TaskCompletionSource duh = new TaskCompletionSource();
                await this.ConsoleLog.Dispatcher.BeginInvoke(new _CreateNewParagraphInLog(this.CreateNewParagraphInLog2), new object[] { duh, callback, newline, followLastLine });
                await duh.Task;
            }
        }

        private void CreateNewParagraphInLog2(TaskCompletionSource tSrc, Action<ICSharpCode.AvalonEdit.Document.DocumentTextWriter> callback, bool newline, bool followLastLine)
        {
            try
            {
                var textlength = this.ConsoleLog.Document.TextLength;
                using (var writer = new ICSharpCode.AvalonEdit.Document.DocumentTextWriter(this.ConsoleLog.Document, textlength))
                {
                    bool isAlreadyInLastLineView = (followLastLine ? ((this.ConsoleLog.VerticalOffset + this.ConsoleLog.ViewportHeight) >= (this.ConsoleLog.ExtentHeight - 1d)) : false);
                    if (newline)
                    {
                        if (textlength != 0)
                        {
                            writer.WriteLine();
                        }
                    }
                    callback.Invoke(writer);
                    if (isAlreadyInLastLineView)
                    {
                        this.ConsoleLog.ScrollToEnd();
                    }
                }
                tSrc.SetResult();
            }
            catch (Exception ex)
            {
                tSrc.SetException(ex);
            }
        }

        private delegate void _CreateNewParagraphInLog(TaskCompletionSource tSrc, Action<ICSharpCode.AvalonEdit.Document.DocumentTextWriter> callback, bool newline, bool followLastLine);

        private void ConsoleLog_ContextMenuOpening(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu menu)
            {
                var consoleui = this.ConsoleLog;
                menu.PlacementTarget = consoleui;
                foreach (var item in menu.Items)
                {
                    if (item is MenuItem menuitem)
                    {
                        if (menuitem.Tag is string str)
                        {
                            // Hardcoded for now
                            if (string.Equals(str, "ConsoleLogMenuItemCopySelected", StringComparison.Ordinal))
                            {
                                if (consoleui.SelectionLength == 0)
                                {
                                    menuitem.Visibility = Visibility.Collapsed;
                                }
                                else
                                {
                                    menuitem.Visibility = Visibility.Visible;
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void ConsoleLogMenuItemCopySelected_Click(object sender, RoutedEventArgs e)
        {
            if (this.ConsoleLog.SelectionLength == 0) return;
            Clipboard.SetText(this.ConsoleLog.SelectedText, TextDataFormat.UnicodeText);
        }

        private void ConsoleLogMenuItemCopyAll_Click(object sender, RoutedEventArgs e)
        {
            var str = this.ConsoleLog.Text;
            if (!string.IsNullOrEmpty(str))
            {
                Clipboard.SetText(str, TextDataFormat.UnicodeText);
            }
        }

        private void ConsoleLogMenuItemClearAll_Click(object sender, RoutedEventArgs e)
        {
            this.ConsoleLog.Clear();
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

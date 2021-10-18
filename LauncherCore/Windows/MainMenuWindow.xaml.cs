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
using Leayal.Shared;
using System.Runtime;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainMenuWindow : MetroWindowEx
    {
        internal readonly HttpClient webclient;
        private readonly PSO2HttpClient pso2HttpClient;
        private readonly GameClientUpdater pso2Updater;
        private readonly CancellationTokenSource cancelAllOperation;
        private CancellationTokenSource cancelSrc_gameupdater;
        private readonly ConfigurationFile config_main;
#nullable enable
        private readonly Lazy<BitmapSource?> lazybg_dark, lazybg_light;
#nullable restore
        private readonly Lazy<System.Windows.Forms.NotifyIcon> trayIcon;
        private readonly ToggleButton[] toggleButtons;
        private readonly Lazy<Task<BackgroundSelfUpdateChecker>> backgroundselfupdatechecker;
        private readonly RSSFeedPresenter RSSFeedPresenter;
        // private readonly SimpleDispatcherQueue dispatcherqueue;
        
        public MainMenuWindow(ConfigurationFile conf) : base()
        {
            this.config_main = conf;
            this.ss_id = null;
            this.ss_pw = null;
            // this.dispatcherqueue = SimpleDispatcherQueue.CreateDefault(TimeSpan.FromMilliseconds(30), this.Dispatcher);
            // System.Net.Http.web
            this.webclient = new HttpClient(new SocketsHttpHandler()
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = System.Net.DecompressionMethods.All,
                ConnectTimeout = TimeSpan.FromSeconds(30),
#if DEBUGHTTPREQUEST
                UseProxy = true,
                Proxy = new System.Net.WebProxy(System.Net.IPAddress.Loopback.ToString(), 8866),
#else
                UseProxy = false,
                Proxy = null,
#endif
                EnableMultipleHttp2Connections = true,
                UseCookies = true,
                Credentials = null,
                DefaultProxyCredentials = null
            }, true);
            this.dialogReferenceByUUID = new Dictionary<Guid, ILogDialogFactory>();
            this.consolelog_hyperlinkparser = new CustomHyperlinkElementGenerator();
            this.consolelog_hyperlinkparser.LinkClicked += VisualLineLinkText_LinkClicked;
            this.RSSFeedPresenter = new RSSFeedPresenter(this.webclient);
            this.pso2HttpClient = new PSO2HttpClient(this.webclient);
            this.backgroundselfupdatechecker = new Lazy<Task<BackgroundSelfUpdateChecker>>(() => Task.Run(() =>
            {
                var binDir = Path.Combine(RuntimeValues.RootDirectory, "bin");
                var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var removelen = binDir.Length + 1;

                if (Directory.Exists(binDir))
                {
                    foreach (var filename in Directory.EnumerateFiles(binDir, "*.dll", SearchOption.TopDirectoryOnly))
                    {
                        var sha1 = SHA1Hash.ComputeHashFromFile(filename);
                        dictionary.Add(filename.Remove(0, removelen), sha1);
                    }
                }

                binDir = Path.Combine(RuntimeValues.RootDirectory, "bin", "plugins", "rss");
                if (Directory.Exists(binDir))
                {
                    foreach (var filename in Directory.EnumerateFiles(binDir, "*.dll", SearchOption.TopDirectoryOnly))
                    {
                        var sha1 = SHA1Hash.ComputeHashFromFile(filename);
                        dictionary.Add(filename.Remove(0, removelen), sha1);
                    }
                }

                dictionary.TrimExcess();
                var selfupdatecheck = new BackgroundSelfUpdateChecker(this.webclient, dictionary);
                selfupdatecheck.UpdateFound += this.OnSelfUpdateFound;
                return selfupdatecheck;
            }));
            this.pso2Updater = CreateGameClientUpdater(this.pso2HttpClient);
            /*
            this.config_main = new Classes.ConfigurationFile(Path.GetFullPath(Path.Combine("config", "launcher.json"), RuntimeValues.RootDirectory));
            if (File.Exists(this.config_main.Filename))
            {
                this.config_main.Load();
            }
            */
#nullable enable
            this.lazybg_dark = new Lazy<BitmapSource?>(() => BitmapSourceHelper.FromEmbedResourcePath("Leayal.PSO2Launcher.Core.Resources._bgimg_dark.png"));
            this.lazybg_light = new Lazy<BitmapSource?>(() => BitmapSourceHelper.FromEmbedResourcePath("Leayal.PSO2Launcher.Core.Resources._bgimg_light.png"));
#nullable restore
            this.trayIcon = new Lazy<System.Windows.Forms.NotifyIcon>(CreateNotifyIcon);

            this.cancelAllOperation = new CancellationTokenSource();

            InitializeComponent();

            this.ConsoleLog.Options.EnableHyperlinks = true;
            this.ConsoleLog.Options.EnableImeSupport = false;
            this.ConsoleLog.Options.EnableEmailHyperlinks = false;
            this.ConsoleLog.Options.EnableTextDragDrop = false;
            this.ConsoleLog.Options.HighlightCurrentLine = true;
            this.ConsoleLog.Options.RequireControlModifierForHyperlinkClick = false;

            this.ConsoleLog.TextArea.TextView.ElementGenerators.Add(this.consolelog_hyperlinkparser);
            this.ConsoleLog.TextArea.TextView.LineTransformers.Add(this.consolelog_hyperlinkparser.Colorizer);

            this.RSSFeedPresenterBorder.Child = this.RSSFeedPresenter;

            this.toggleButtons = new ToggleButton[] { this.ToggleBtn_PSO2News, this.ToggleBtn_RSSFeed, this.ToggleBtn_ConsoleLog };
            var pathlaststate_selectedtogglebuttons = Path.GetFullPath(Path.Combine("config", "state_togglebtns.txt"), RuntimeValues.RootDirectory);
            if (File.Exists(pathlaststate_selectedtogglebuttons))
            {
                var line = QuickFile.ReadFirstLine(pathlaststate_selectedtogglebuttons).AsSpan();
                if (!line.IsWhiteSpace())
                {
                    foreach (var btn in this.toggleButtons)
                    {
                        if (line.Equals(btn.Name, StringComparison.Ordinal))
                        {
                            btn.IsChecked = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                this.ToggleBtn_PSO2News.IsChecked = true;
            }

            const string StartingLine = "[Lea] Welcome to PSO2 Launcher, which was made by ",
                i_am_lea = "Dramiel Leayal",
                FollowingLline = ". The source code can be found on ",
                i_am_here = "Github";
            var str = StartingLine + i_am_lea + FollowingLline + i_am_here + '.';
            var placements = new Dictionary<RelativeLogPlacement, Uri>(2)
            {
                { new RelativeLogPlacement(StartingLine.Length, i_am_lea.Length), StaticResources.Url_ShowAuthor },
                { new RelativeLogPlacement(StartingLine.Length + i_am_lea.Length + FollowingLline.Length, i_am_here.Length), StaticResources.Url_ShowSourceCodeGithub }
            };
            this.CreateNewParagraphFormatHyperlinksInLog(str, placements, false);
            // this.CreateNewParagraphInLog("[Lea] Welcome to PSO2 Launcher, which was made by Dramiel Leayal", false);
        }

        private void ThisWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.TabMainMenu.DefaultGameStartStyle = this.config_main.DefaultGameStartStyle;
            this.TabMainMenu.IsSelected = true;
            RSSFeedPresenter_Loaded();
        }

        protected override async void OnFirstShown(EventArgs e)
        {
            if (this.config_main.LauncherCheckForSelfUpdates)
            {
                var selfchecker = await this.backgroundselfupdatechecker.Value;
                selfchecker.TickTime = TimeSpan.FromHours(this.config_main.LauncherCheckForSelfUpdates_IntervalHour);
                selfchecker.Start();
            }

            if (!this.config_main.LauncherLoadWebsiteAtStartup)
            {
                if (this.LauncherWebView.Child is Button btn)
                {
                    // btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    btn.Click += this.LoadLauncherWebView_Click;
                }
            }

            await this.OnEverythingIsDoneAndReadyToBeInteracted();
        }

        protected override void OnReady(EventArgs e)
        {
            if (this.config_main.LauncherLoadWebsiteAtStartup)
            {
                if (this.LauncherWebView.Child is Button btn)
                {
                    // btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    this.LoadLauncherWebView_Click(btn, null);
                }
            }
        }

        protected override void OnThemeRefresh()
        {
            if (App.Current.IsLightMode)
            {
                this.BgImg.Source = lazybg_light.Value;
                this.CreateNewParagraphInLog($"[ThemeManager] {(this.config_main.SyncThemeWithOS ? "Detected Windows 10's" : "User changed")} theme setting: Light Mode.");
                this.consolelog_hyperlinkparser.Colorizer.ForegroundBrush = Brushes.Blue;
            }
            else
            {
                this.BgImg.Source = lazybg_dark.Value;
                this.CreateNewParagraphInLog($"[ThemeManager] {(this.config_main.SyncThemeWithOS ? "Detected Windows 10's" : "User changed")} theme setting: Dark Mode.");
                this.consolelog_hyperlinkparser.Colorizer.ForegroundBrush = Brushes.Yellow;
            }
            this.ConsoleLog.TextArea.TextView.Redraw();
        }

        protected override async Task OnCleanupBeforeClosed()
        {
            this.CreateNewParagraphInLog("[System] Stopping all operations and cleaning up resources before closing and exiting launcher.");

            var listOfOperations = new List<Task>();

            Task t_stopClientUpdater;
            if (this.pso2Updater.IsBusy)
            {
                this.CreateNewParagraphInLog("[System] Detecting a time-consuming cleanup operation: Game client updating. This may take a while to gracefully stop and clean up. Please wait...");

                var tsrc = new TaskCompletionSource();
                t_stopClientUpdater = tsrc.Task;
                listOfOperations.Add(t_stopClientUpdater);

                GameClientUpdater.OperationCompletedHandler onfinialize = null;
                onfinialize = new GameClientUpdater.OperationCompletedHandler(delegate
                {
                    this.pso2Updater.OperationCompleted -= onfinialize;
                    this.CreateNewParagraphInLog("[System] Game client updating has been stopped gracefully.");
                    tsrc.TrySetResult();
                });
                this.pso2Updater.OperationCompleted += onfinialize;
            }
            else
            {
                t_stopClientUpdater = Task.CompletedTask;
            }

            listOfOperations.Add((new Func<Task>(async delegate
            {
                if (this.backgroundselfupdatechecker.IsValueCreated)
                {
                    var checker = await this.backgroundselfupdatechecker.Value;
                    checker.Dispose();
                }
            })).Invoke());

            this.cancelAllOperation.Cancel();
            this.webclient.CancelPendingRequests();

            // Wait until the everything is stopped within 10s, if not finished, consider timeout and force close anyway.
            if (listOfOperations.Count != 0)
            {
                await Task.WhenAny(Task.WhenAll(listOfOperations), Task.Delay(10000));
            }

            this.cancelAllOperation?.Dispose();
            this.cancelSrc_gameupdater?.Dispose();
            this.webclient.Dispose();

            if (this.trayIcon.IsValueCreated)
            {
                using (var trayicon = this.trayIcon.Value)
                {
                    trayicon.Visible = false;
                }
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

            // This is unnecessary as we don't really use the event CleanupBeforeClosed at all.
            // await base.OnCleanupBeforeClosed();
        }

        private async void LoadLauncherWebView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.Click -= this.LoadLauncherWebView_Click;
            }
            if (e != null)
            {
                e.Handled = true;
            }
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
                if (obj is IWebViewCompatControl webview)
                {
                    webview.Initialized += this.WebViewCompatControl_Initialized;
                    this.LauncherWebView.Child = (Control)obj;
                    this.CreateNewParagraphInLog("[WebView] PSO2's launcher news has been loaded.");
                }
                else
                {
                    if (obj is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    else if (obj is IAsyncDisposable asyncdisposable)
                    {
                        await asyncdisposable.DisposeAsync();
                    }
                    Prompt_Generic.Show(this, "Unknown error occurred when trying to load Web View.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Prompt_Generic.ShowError(this, ex);
            }
        }

        private readonly static Uri SEGALauncherNewsUrl = new("https://launcher.pso2.jp/ngs/01/");
        private void WebViewCompatControl_Initialized(object sender, EventArgs e)
        {
            if (sender is IWebViewCompatControl webview)
            {
                webview.Navigated += this.Webview_Navigated;
                webview.NavigateTo(SEGALauncherNewsUrl);
            }
        }

        private async Task OnEverythingIsDoneAndReadyToBeInteracted()
        {
            if (this.config_main.LauncherCheckForPSO2GameUpdateAtStartup)
            {
                await StartGameClientUpdate(false, this.config_main.LauncherCheckForPSO2GameUpdateAtStartupPrompt);
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
                        if (string.Equals(e.Uri.AbsoluteUri, SEGALauncherNewsUrl.AbsoluteUri, StringComparison.OrdinalIgnoreCase)) return;
                        WindowsExplorerHelper.OpenUrlWithDefaultBrowser(e.Uri.AbsoluteUri);
                    }
                    else if (Uri.TryCreate(wvc.CurrentUrl, e.Uri.ToString(), out var absUri))
                    {
                        if (string.Equals(absUri.AbsoluteUri, SEGALauncherNewsUrl.AbsoluteUri, StringComparison.OrdinalIgnoreCase)) return;
                        WindowsExplorerHelper.OpenUrlWithDefaultBrowser(absUri);
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
                    if (dialog.ShowCustomDialog(this) == true)
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
                    var dialog = new PSO2UserConfigurationWindow() { Owner = this };
                    dialog.ShowDialog();
                }
                finally
                {
                    tab.ButtonPSO2GameOptionClicked += this.TabMainMenu_ButtonPSO2GameOptionClicked;
                }
            }
        }

        private void TabMainMenu_ButtonManageGameDataClick(object sender, RoutedEventArgs e)
        {
            if (sender is TabMainMenu tab)
            {
                tab.ButtonManageGameDataClicked -= this.TabMainMenu_ButtonManageGameDataClick;
                try
                {
                    var dialog = new DataManagerWindow(this.config_main);
                    if (dialog.ShowCustomDialog(this) == true)
                    {
                        this.RefreshGameUpdaterOptions();
                    }
                }
                finally
                {
                    tab.ButtonManageGameDataClicked += this.TabMainMenu_ButtonManageGameDataClick;
                }
            }
        }

        

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

        private void TabMainMenu_ButtonManageLauncherThemingClicked(object sender, RoutedEventArgs e)
        {
            if (sender is TabMainMenu tab)
            {
                tab.ButtonManageLauncherThemingClicked -= this.TabMainMenu_ButtonManageLauncherThemingClicked;
                try
                {
                    var dialog = new LauncherThemingManagerWindow(this.config_main);
                    if (dialog.ShowCustomDialog(this) == true)
                    {
                        App.Current?.RefreshThemeSetting();
                    }
                }
                finally
                {
                    tab.ButtonManageLauncherThemingClicked += this.TabMainMenu_ButtonManageLauncherThemingClicked;
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

        private async void WindowsCommandButtons_InvokeGCFromUI_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            try
            {
                if (Prompt_Generic.Show(this, "Are you sure you want tell Garbarge Collector to clean up memory forcefully right now?" + Environment.NewLine + Environment.NewLine +
                    "This operation is not cancellable and you must wait until it's completed before doing anything else." + Environment.NewLine + Environment.NewLine +
                    "GC won't just just reduce the currently in-use memory down to a few MBs. It will only reclaim parts which are no longer in use and are available to be collected." + Environment.NewLine + Environment.NewLine +
                    "You may need to clean several times (about twice or 3 times) in order for the launcher to trim down the allocated memory." + Environment.NewLine +
                    "And you shouldn't clean memory up while the launcher is updating the PSO2 game client.", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    var controller = await MahApps.Metro.Controls.Dialogs.DialogManager.ShowProgressAsync(this, "Cleaning up memory", "Please wait...", isCancelable: false, settings: new MahApps.Metro.Controls.Dialogs.MetroDialogSettings() { OwnerCanCloseWithDialog = true, AnimateShow = false });
                    controller.SetIndeterminate();
                    // var dialog = await MahApps.Metro.Controls.Dialogs.DialogManager.GetCurrentDialogAsync<MahApps.Metro.Controls.Dialogs.ProgressDialog>(this);
                    try
                    {
                        await Task.Factory.StartNew(delegate
                        {
                            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                            GC.WaitForPendingFinalizers();
                        });
                        if (controller.IsOpen)
                        {
                            await controller.CloseAsync();
                        }
                    }
                    catch { }
                }
            }
            catch
            {

            }
        }

        private void WindowsCommandButtons_Minimize_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SystemCommands.MinimizeWindow(this);
        }

#endregion
    }
}

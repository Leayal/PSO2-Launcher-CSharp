using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using System.Threading;
using Leayal.SharedInterfaces;
using System.Reflection;
using Leayal.PSO2Launcher.Core.UIElements;
using Leayal.PSO2Launcher.Helper;
using Leayal.PSO2Launcher.Core.Classes;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Net.Http;
using Leayal.Shared;
using System.Runtime;
using Leayal.Shared.Windows;
using System.Windows.Threading;
using System.Runtime.Loader;
using System.Runtime.InteropServices;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainMenuWindow : MetroWindowEx
    {
        private static readonly Lazy<BitmapSource> lazybg_dark = new Lazy<BitmapSource?>(() => BitmapSourceHelper.FromEmbedResourcePath("Leayal.PSO2Launcher.Core.Resources._bgimg_dark.png")),
            lazybg_light = new Lazy<BitmapSource>(() => BitmapSourceHelper.FromEmbedResourcePath("Leayal.PSO2Launcher.Core.Resources._bgimg_light.png"));

        // UseClock
        public static readonly DependencyProperty UseClockProperty = DependencyProperty.Register("UseClock", typeof(bool), typeof(MainMenuWindow), new PropertyMetadata(false, (obj, e) =>
        {
            if (e.NewValue is bool b)
            {
                if (obj is MainMenuWindow window)
                {
                    if (b)
                    {
                        App.Current.JSTClock.Register(window.clockCallback);
                    }
                    else
                    {
                        App.Current.JSTClock.Unregister(window.clockCallback);
                    }
                    window.menuItem_TimeClock.Visible = b;
                }
                foreach (var w in App.Current.Windows)
                {
                    if (w is Toolbox.Windows.ToolboxWindow_AlphaReactorCount tbw)
                    {
                        tbw.IsClockVisible = b;
                    }
                }
            }
        }));
        private static readonly DependencyPropertyKey CurrentTimePropertyKey = DependencyProperty.RegisterReadOnly("CurrentTime", typeof(DateTime), typeof(MainMenuWindow), new PropertyMetadata(DateTime.MinValue, (obj, e) =>
        {
            if (obj is MainMenuWindow window && e.NewValue is DateTime d)
            {
                window.menuItem_TimeClock.Text = $"JST: {d}";
            }
        }));
        public static readonly DependencyProperty CurrentTimeProperty = CurrentTimePropertyKey.DependencyProperty;

        public DateTime CurrentTime => (DateTime)this.GetValue(CurrentTimeProperty);
        public bool UseClock
        {
            get => (bool)this.GetValue(UseClockProperty);
            set => this.SetValue(UseClockProperty, value);
        }

        private readonly DispatcherTimer timer_unloadWebBrowser;
        internal readonly HttpClient webclient;
        private readonly PSO2HttpClient pso2HttpClient;
        private readonly GameClientUpdater pso2Updater;
        private readonly CancellationTokenSource cancelAllOperation;
        private CancellationTokenSource cancelSrc_gameupdater;
        private readonly ConfigurationFile config_main;
        private readonly Lazy<System.Windows.Forms.NotifyIcon> trayIcon;
        private readonly ToggleButton[] toggleButtons;
        private readonly Lazy<Task<BackgroundSelfUpdateChecker>> backgroundselfupdatechecker;
        private readonly RSSFeedPresenter RSSFeedPresenter;
        private readonly Toolbox.ClockTickerCallback clockCallback;
        private readonly Action<DependencyPropertyKey, object> @delegateSetCurrentTime;
        private readonly object[] @delegateSetCurrentTime_params;
        private bool isWebBrowserLoaded;

        public MainMenuWindow(ConfigurationFile conf) : base()
        {
            this.isWebBrowserLoaded = false;
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
            this.RSSFeedPresenter.SelectedFeedChanged += this.RSSFeedPresenter_SelectedFeedChanged;
            this.pso2HttpClient = new PSO2HttpClient(this.webclient);
            this.backgroundselfupdatechecker = new Lazy<Task<BackgroundSelfUpdateChecker>>(() => Task.Run(() =>
            {
                var binDir = Path.GetFullPath("bin", RuntimeValues.RootDirectory);
                var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var removelen = binDir.Length + 1;

                static void AddToDictionary(Dictionary<string, string> d, string p, int o)
                {
                    if (Directory.Exists(p))
                    {
                        foreach (var filename in Directory.EnumerateFiles(p, "*.dll", SearchOption.TopDirectoryOnly))
                        {
                            var sha1 = SHA1Hash.ComputeHashFromFile(filename);
                            d.Add(filename.Remove(0, o), sha1);
                        }
                    }
                }

                AddToDictionary(dictionary, binDir, removelen);
                AddToDictionary(dictionary, Path.Combine(binDir, "plugins", "rss"), removelen);
                if (Environment.Is64BitProcess)
                {
                    AddToDictionary(dictionary, Path.Combine(binDir, "native-x64"), removelen);
                }
                else
                {
                    AddToDictionary(dictionary, Path.Combine(binDir, "native-x86"), removelen);
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
            this.trayIcon = new Lazy<System.Windows.Forms.NotifyIcon>(CreateNotifyIcon);

            this.cancelAllOperation = new CancellationTokenSource();

            this.@delegateSetCurrentTime = new Action<DependencyPropertyKey, object>(this.SetValue);
            this.delegateSetCurrentTime_params = new object[2] { CurrentTimePropertyKey, null };
            this.clockCallback = new Toolbox.ClockTickerCallback(this.OnClockTicked);

            InitializeComponent();

            this.Icon = App.DefaultAppIcon;

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
            // StartingLine + i_am_lea + FollowingLline + i_am_here + '.';
            this.CreateNewParagraphFormatHyperlinksInLog(string.Create<object>(StartingLine.Length + i_am_lea.Length + FollowingLline.Length + i_am_here.Length + 1, null, (c, _) =>
            {
                var s = c;
                StartingLine.CopyTo(s);
                s = s.Slice(StartingLine.Length);
                i_am_lea.CopyTo(s);
                s = s.Slice(i_am_lea.Length);
                FollowingLline.CopyTo(s);
                s = s.Slice(FollowingLline.Length);
                i_am_here.CopyTo(s);
                s = s.Slice(i_am_here.Length);
                // c[c.Length - 1] = '.';
                s[0] = '.';
            }), new Dictionary<RelativeLogPlacement, Uri>(2)
            {
                { new RelativeLogPlacement(StartingLine.Length, i_am_lea.Length), StaticResources.Url_ShowAuthor },
                { new RelativeLogPlacement(StartingLine.Length + i_am_lea.Length + FollowingLline.Length, i_am_here.Length), StaticResources.Url_ShowSourceCodeGithub }
            }, false);

            if (MemoryExtensions.Equals(Path.TrimEndingDirectorySeparator(RuntimeEnvironment.GetRuntimeDirectory().AsSpan()), Path.GetFullPath("dotnet", RuntimeValues.RootDirectory).AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                this.CreateNewParagraphInLog($"[System] Launcher is running in standalone environment (.NET Runtime version: {Environment.Version}). Launcher's bootstrap version: {System.Diagnostics.FileVersionInfo.GetVersionInfo(RuntimeValues.EntryExecutableFilename).FileVersion}.");
            }
            else
            {
                this.CreateNewParagraphInLog($"[System] Launcher is running in shared runtime environment (.NET Runtime version: {Environment.Version}). Launcher's bootstrap version: {System.Diagnostics.FileVersionInfo.GetVersionInfo(RuntimeValues.EntryExecutableFilename).FileVersion}.");
            }

            if (App.Current.BootstrapVersion < 4)
            {
                const string BootstrapVersionReminder = "[Launcher Updater] You are using an older version of the Launcher's bootstrap. It is recommended to update it.",
                    DowlodEtNow = "(Download it here)";
                this.CreateNewParagraphFormatHyperlinksInLog(string.Create<object>(BootstrapVersionReminder.Length + DowlodEtNow.Length + 1, null, (c, _) =>
                {
                    var s = c;
                    BootstrapVersionReminder.CopyTo(s);
                    s = s.Slice(BootstrapVersionReminder.Length);
                    s[0] = ' ';
                    s = s.Slice(1);
                    DowlodEtNow.CopyTo(s);
                }), new Dictionary<RelativeLogPlacement, Uri>(1)
                {
                    { new RelativeLogPlacement(BootstrapVersionReminder.Length + 1, DowlodEtNow.Length), StaticResources.Url_ShowLatestGithubRelease },
                });
            }

            this.timer_unloadWebBrowser = new DispatcherTimer(TimeSpan.FromSeconds(30), DispatcherPriority.Normal, this.Timer_UnloadWebBrowserControl, this.Dispatcher) { IsEnabled = false };
        }

        private void ThisWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.TabMainMenu.GameStartWithPSO2TweakerChecked = this.config_main.PSO2Tweaker_LaunchGameWithTweaker;
            this.TabMainMenu.GameStartWithPSO2TweakerEnabled = this.config_main.PSO2Tweaker_CompatEnabled;
            this.TabMainMenu.DefaultGameStartStyle = this.config_main.DefaultGameStartStyle;
            
            this.TabMainMenu.IsSelected = true;
            this.UseClock = this.config_main.LauncherUseClock;
            this.RSSFeedPresenter_Loaded();
        }

        protected override async void OnFirstShown(EventArgs e)
        {
            if (this.config_main.LauncherCheckForSelfUpdates)
            {
                var selfchecker = await this.backgroundselfupdatechecker.Value;
                selfchecker.TickTime = TimeSpan.FromHours(this.config_main.LauncherCheckForSelfUpdates_IntervalHour);
                selfchecker.Start();
            }

            base.OnFirstShown(e);

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

        private void OnClockTicked(in DateTime oldTime, in DateTime newTime)
        {
            this.@delegateSetCurrentTime_params[1] = newTime;
            this.Dispatcher.BeginInvoke(this.delegateSetCurrentTime, this.@delegateSetCurrentTime_params);
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

            if (this.LauncherWebView.Child is IWebViewCompatControl webview)
            {
                this.LauncherWebView.Child = null;
                webview.Dispose();
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
            var btn = sender as Button;
            if (btn != null)
            {
                btn.Click -= this.LoadLauncherWebView_Click;
            }
            if (e != null)
            {
                e.Handled = true;
            }
            // this.RemoveLogicalChild(btn);
            if (this.Dispatcher.HasShutdownStarted || this.Dispatcher.HasShutdownFinished) return;
            try
            {
                var filepath = Path.GetFullPath(Path.Combine("bin", "WebViewCompat.dll"), RuntimeValues.RootDirectory);
                Assembly asm;
                if (File.Exists(filepath))
                {
                    var context = AssemblyLoadContext.GetLoadContext(Assembly.GetAssembly(this.GetType()));
                    if (context == null)
                    {
                        asm = Assembly.LoadFrom(filepath);
                    }
                    else
                    {
                        asm = context.LoadFromNativeImagePath(filepath, filepath);
                    }
                }
                else
                {
                    asm = null;
                }
                if (asm == null)
                {
                    Prompt_Generic.Show(this, "Missing file 'WebViewCompat.dll' in 'bin' directory.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    var obj = asm.CreateInstance("Leayal.WebViewCompat.WebViewCompatControl", false, BindingFlags.CreateInstance, null, new object[] { "PSO2Launcher", this.config_main.UseWebView2IfAvailable }, null, null);
                    if (obj is IWebViewCompatControl webview)
                    {
                        webview.BrowserInitialized += this.WebViewCompatControl_Initialized;
                        this.LauncherWebView.Child = (UIElement)obj;
                        this.isWebBrowserLoaded = true;
                        if (webview.IsUsingWebView2)
                        {
                            this.CreateNewParagraphInLog($"[WebView] PSO2's launcher news has been loaded with WebView2 (Version: {webview.WebView2Version}).");
                        }
                        else
                        {
                            this.CreateNewParagraphInLog("[WebView] PSO2's launcher news has been loaded with Internet Explorer component.");
                        }
                    }
                    else
                    {
                        if (obj is IAsyncDisposable asyncdisposable)
                        {
                            await asyncdisposable.DisposeAsync();
                        }
                        else if (obj is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                        Prompt_Generic.Show(this, "Unknown error occurred when trying to load Web View.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (TypeLoadException ex)
            {
                if (btn != null)
                {
                    btn.Click += this.LoadLauncherWebView_Click;
                }
                Prompt_Generic.ShowError(this, new System.Windows.Documents.Inline[]
                {
                    new System.Windows.Documents.Run("An error has occurred while loading the necessary libraries for the web engine."),
                    new System.Windows.Documents.LineBreak(),
                    new System.Windows.Documents.Run("Please restart the launcher."),
                    new System.Windows.Documents.LineBreak(),
                    new System.Windows.Documents.Run("If the error still persists after restarting, please "),
                    new CommandHyperlink(new System.Windows.Documents.Run("create an issue report")) { NavigateUri = StaticResources.Url_ShowIssuesGithub },
                    new System.Windows.Documents.Run(" on Github."),
                }, "Error", ex, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (System.Runtime.InteropServices.COMException ex) when (((uint)ex.ErrorCode) == 0x80004005)
            {
                if (btn != null)
                {
                    btn.Click += this.LoadLauncherWebView_Click;
                }
                var lines = new List<System.Windows.Documents.Inline>();
                lines.Add(new System.Windows.Documents.Run("An error has occurred while initializing the Internet Explorer web engine."));
                lines.Add(new System.Windows.Documents.LineBreak());
                lines.Add(new System.Windows.Documents.Run("It seems like your operating system doesn't have both Internet Explorer's COM component and WebView2 runtime."));
                lines.Add(new System.Windows.Documents.LineBreak());
                lines.Add(new System.Windows.Documents.LineBreak());

                static void AAAAAAA(List<System.Windows.Documents.Inline> inlines)
                {
                    inlines.Add(new System.Windows.Documents.LineBreak());
                    inlines.Add(new System.Windows.Documents.Run("If the file can't be found or you want to get anew, you can "));
                    inlines.Add(new CommandHyperlink(new System.Windows.Documents.Run("go to the Microsoft's download page")) { NavigateUri = StaticResources.Url_OpenWebView2InstallerDownloadPage });
                    inlines.Add(new System.Windows.Documents.Run(" to download the installer and run it. "));
                    inlines.Add(new CommandHyperlink(new System.Windows.Documents.Run("(Or click here to download it directly)")) { NavigateUri = StaticResources.Url_DownloadWebView2BootstrapInstaller });
                }

                var pso2_bin = this.config_main.PSO2_BIN;
                if (!string.IsNullOrWhiteSpace(pso2_bin))
                {
                    var path_installerFromSEGA = Path.Combine(pso2_bin, "microsoftedgewebview2setup.exe");
                    if (File.Exists(path_installerFromSEGA))
                    {
                        lines.Add(new System.Windows.Documents.Run("Please run 'microsoftedgewebview2setup.exe' setup in the 'pso2_bin' directory of the game client to install WebView2 Runtime which this launcher can use. "));
                        lines.Add(new ShowLocalFileHyperlink(new System.Windows.Documents.Run("(Click here to show it in File Explorer)")) { NavigateUri = new Uri(path_installerFromSEGA) });
                        AAAAAAA(lines);
                    }
                    else
                    {
                        lines.Add(new System.Windows.Documents.Run("If you already downloaded the PSO2 client, please find and run 'microsoftedgewebview2setup.exe' setup in the 'pso2_bin' directory of the game client to install WebView2 Runtime which this launcher can use."));
                        AAAAAAA(lines);
                    }
                }
                else
                {
                    lines.Add(new System.Windows.Documents.Run("If you already downloaded the PSO2 client, please find and run 'microsoftedgewebview2setup.exe' setup in the 'pso2_bin' directory of the game client to install WebView2 Runtime which this launcher can use."));
                    AAAAAAA(lines);
                }
                Prompt_Generic.ShowError(this, lines, "Error", ex, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                if (btn != null)
                {
                    btn.Click += this.LoadLauncherWebView_Click;
                }
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
                    var dialog = new LauncherBehaviorManagerWindow(this.config_main, this.LauncherWebView.Child is IWebViewCompatControl);
                    if (dialog.ShowCustomDialog(this) == true)
                    {
                        this.TabMainMenu.DefaultGameStartStyle = this.config_main.DefaultGameStartStyle;
                        this.UseClock = this.config_main.LauncherUseClock;
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

                        if (this.config_main.PSO2Tweaker_CompatEnabled)
                        {
                            var tweakerexe = this.config_main.PSO2Tweaker_Bin_Path;
                            if (!string.IsNullOrWhiteSpace(tweakerexe) && File.Exists(tweakerexe))
                            {
                                tab.GameStartWithPSO2TweakerChecked = this.config_main.PSO2Tweaker_LaunchGameWithTweaker;
                                tab.GameStartWithPSO2TweakerEnabled = true;
                            }
                            else
                            {
                                tab.GameStartWithPSO2TweakerEnabled = false;
                            }
                            if (this.LauncherWebView.Child is IWebViewCompatControl webview)
                            {
                                if (this.config_main.UseWebView2IfAvailable != webview.IsUsingWebView2)
                                {
                                    this.LoadLauncherWebView_Click(null, null);
                                    webview.Dispose();
                                }
                            }
                        }
                        else
                        {
                            tab.GameStartWithPSO2TweakerEnabled = false;
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

        private void TabMainMenu_ButtonManageGameLauncherCompatibilityClicked(object sender, RoutedEventArgs e)
        {
            if (sender is TabMainMenu tab)
            {
                tab.ButtonManageGameLauncherCompatibilityClicked -= this.TabMainMenu_ButtonManageGameLauncherCompatibilityClicked;
                try
                {
                    var dialog = new LauncherCompatibilityWindow(this.config_main);
                    if (dialog.ShowCustomDialog(this) == true)
                    {
                        if (this.config_main.PSO2Tweaker_CompatEnabled)
                        {
                            var tweakerexe = this.config_main.PSO2Tweaker_Bin_Path;
                            if (!string.IsNullOrWhiteSpace(tweakerexe) && File.Exists(tweakerexe))
                            {
                                tab.GameStartWithPSO2TweakerEnabled = true;
                            }
                            else
                            {
                                tab.GameStartWithPSO2TweakerEnabled = false;
                            }
                        }
                        else
                        {
                            tab.GameStartWithPSO2TweakerEnabled = false;
                        }
                    }
                }
                finally
                {
                    tab.ButtonManageGameLauncherCompatibilityClicked += this.TabMainMenu_ButtonManageGameLauncherCompatibilityClicked;
                }
            }
        }

        private void Timer_UnloadWebBrowserControl(object sender, EventArgs e)
        {
            this.timer_unloadWebBrowser.Stop();
            // this.isWebBrowserLoaded = false;
            if (this.LauncherWebView.Child is IWebViewCompatControl webview)
            {
                this.LauncherWebView.Child = null;
                webview.Dispose();
            }
        }

        private void ThisWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this._isTweakerRunning
                && Prompt_Generic.Show(this, $"The launcher is currently managing PSO2 Tweaker.{Environment.NewLine}It is recommended to exit the PSO2 Tweaker before closing this launcher to avoid config corruption.{Environment.NewLine}Are you sure you still want to close the launcher before closing PSO2 Tweaker?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
        }

        private void LauncherWebView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.isWebBrowserLoaded)
            {
                var b = (bool)e.NewValue;
                if (b && !this.IsMinimizedToTray)
                {
                    this.timer_unloadWebBrowser.Stop();
                    if (!(this.LauncherWebView.Child is IWebViewCompatControl))
                    {
                        this.LoadLauncherWebView_Click(null, null);
                    }
                }
                else
                {
                    if (!this.timer_unloadWebBrowser.IsEnabled)
                    {
                        this.timer_unloadWebBrowser.Start();
                    }
                }
            }
        }

        private void ToggleBtn_Checked(object sender, RoutedEventArgs e)
        {
            if (this.toggleButtons != null)
            {
                foreach (var btn in this.toggleButtons)
                {
                    if (!btn.Equals(sender))
                    {
                        btn.IsChecked = false;
                    }
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
            if (sender is Button btn)
            {
                e.Handled = true;
                btn.Click -= this.WindowsCommandButtons_InvokeGCFromUI_Click;
                try
                {
                    if (Prompt_Generic.Show(this, "Are you sure you want tell Garbarge Collector to clean up memory forcefully right now?" + Environment.NewLine + Environment.NewLine +
                        "This operation is not cancellable and you must wait until it's completed before doing anything else." + Environment.NewLine + Environment.NewLine +
                        "GC won't just just reduce the currently in-use memory down to a few MBs. It will only reclaim parts which are no longer in use and are available to be collected." + Environment.NewLine + Environment.NewLine +
                        "You may need to clean several times (about twice or 3 times) in order for the launcher to trim down the allocated memory." + Environment.NewLine +
                        "And you shouldn't clean memory up while the launcher is updating the PSO2 game client.", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        bool hasWebBrowser;
                        if (this.LauncherWebView.Child is IWebViewCompatControl webBrowserControl)
                        {
                            hasWebBrowser = true;
                        }
                        else
                        {
                            hasWebBrowser = false;
                        }
                        var controller = await MahApps.Metro.Controls.Dialogs.DialogManager.ShowProgressAsync(this, "Cleaning up memory", "Please wait...", isCancelable: false, settings: new MahApps.Metro.Controls.Dialogs.MetroDialogSettings() { OwnerCanCloseWithDialog = true, AnimateShow = false });
                        bool isBrowserVisible;
                        if (hasWebBrowser && this.ToggleBtn_PSO2News.IsChecked == true)
                        {
                            isBrowserVisible = true;
                            this.ToggleBtn_PSO2News.IsChecked = false;
                        }
                        else
                        {
                            isBrowserVisible = false;
                        }
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
                        }
                        catch { }
                        finally
                        {
                            if (controller.IsOpen)
                            {
                                await controller.CloseAsync();
                            }
                            if (isBrowserVisible)
                            {
                                this.ToggleBtn_PSO2News.IsChecked = true;
                            }
                        }
                    }
                }
                catch
                {

                }
                finally
                {
                    btn.Click += this.WindowsCommandButtons_InvokeGCFromUI_Click;
                }
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

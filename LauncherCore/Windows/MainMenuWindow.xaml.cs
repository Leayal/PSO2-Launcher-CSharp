﻿using System;
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
using System.Runtime;
using Leayal.Shared.Windows;
using System.Windows.Threading;
using System.Runtime.Loader;
using System.Runtime.InteropServices;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.PSO2Launcher.Core.Classes.AvalonEdit;
using Leayal.Shared;
using System.Windows.Input;
using System.Runtime.CompilerServices;
using System.Collections.Frozen;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainMenuWindow : MetroWindowEx
    {
        private static readonly WeakLazy<BitmapSource> lazybg_dark = new WeakLazy<BitmapSource>(() => BitmapSourceHelper.FromEmbedResourcePath("Leayal.PSO2Launcher.Core.Resources._bgimg_dark.png")),
            lazybg_light = new WeakLazy<BitmapSource>(() => BitmapSourceHelper.FromEmbedResourcePath("Leayal.PSO2Launcher.Core.Resources._bgimg_light.png"));

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
                    if (w is Toolbox.Windows.ToolboxWindow_VendorItemPickupCount tbw)
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
        private CancellationTokenSource? cancelSrc_gameupdater;
        private readonly ConfigurationFile config_main;
        private readonly Lazy<System.Windows.Forms.NotifyIcon> trayIcon;
        private readonly ToggleButton[] toggleButtons;
        private readonly Lazy<Task<BackgroundSelfUpdateChecker>> backgroundselfupdatechecker;
        private readonly RSSFeedPresenter RSSFeedPresenter;
        private readonly Toolbox.ClockTickerCallback clockCallback;
        private readonly Action<DependencyPropertyKey, object> @delegateSetCurrentTime;
        private readonly object?[] @delegateSetCurrentTime_params;
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
            this.dialogReferenceByUUID = new Dictionary<Guid, ILogDialogHandler>();
            this.consolelog_hyperlinkparser = new CustomElementGenerator();
            this.consolelog_textcolorizer = new CustomColorTextTransformer();
            this.RSSFeedPresenter = new RSSFeedPresenter(this.webclient);
            this.RSSFeedPresenter.SelectedFeedChanged += this.RSSFeedPresenter_SelectedFeedChanged;
            this.pso2HttpClient = new PSO2HttpClient(this.webclient, Path.GetFullPath(Path.Combine("data", "cache", "leapso2client"), RuntimeValues.RootDirectory));
            this.backgroundselfupdatechecker = new Lazy<Task<BackgroundSelfUpdateChecker>>(this.SetupBackgroundSelfUpdateChecker);
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
            this.delegateSetCurrentTime_params = new object?[2] { CurrentTimePropertyKey, null };
            this.clockCallback = new Toolbox.ClockTickerCallback(this.OnClockTicked);

            InitializeComponent();

            this.Icon = App.DefaultAppIcon;

            this.ConsoleLog.Options.EnableHyperlinks = true;
            this.ConsoleLog.Options.EnableImeSupport = false;
            this.ConsoleLog.Options.EnableEmailHyperlinks = false;
            this.ConsoleLog.Options.EnableTextDragDrop = false;
            this.ConsoleLog.Options.HighlightCurrentLine = true;
            this.ConsoleLog.Options.RequireControlModifierForHyperlinkClick = false;

            this.consolelog_boldTypeface = new Typeface(this.ConsoleLog.FontFamily, this.ConsoleLog.FontStyle, FontWeights.Bold, this.ConsoleLog.FontStretch);

            this.ConsoleLog.TextArea.TextView.ElementGenerators.Add(this.consolelog_hyperlinkparser);
            this.ConsoleLog.TextArea.TextView.LineTransformers.Add(this.consolelog_textcolorizer);

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

            this.CreateNewLineInConsoleLog("Lea", (consoleLog, writer, absoluteOffsetOfDocumentLine, myself) =>
            {
                writer.Write("Welcome to PSO2 Launcher, which was made by ");
                myself.ConsoleLogHelper_WriteHyperLink(writer, "Dramiel Leayal", StaticResources.Url_ShowAuthor, VisualLineLinkText_LinkClicked);
                writer.Write(". The source code can be found on ");
                myself.ConsoleLogHelper_WriteHyperLink(writer, "Github", StaticResources.Url_ShowSourceCodeGithub, VisualLineLinkText_LinkClicked);
                writer.Write('.');
            }, this, false, false);

            if (MemoryExtensions.Equals(Path.TrimEndingDirectorySeparator(RuntimeEnvironment.GetRuntimeDirectory().AsSpan()), Path.GetFullPath("dotnet", RuntimeValues.RootDirectory).AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                this.CreateNewLineInConsoleLog("System", $"Launcher is running in standalone environment (.NET Runtime version: {Environment.Version}). Launcher's bootstrap version: {System.Diagnostics.FileVersionInfo.GetVersionInfo(RuntimeValues.EntryExecutableFilename).FileVersion}.");
            }
            else
            {
                this.CreateNewLineInConsoleLog("System", $"Launcher is running in shared runtime environment (.NET Runtime version: {Environment.Version}). Launcher's bootstrap version: {System.Diagnostics.FileVersionInfo.GetVersionInfo(RuntimeValues.EntryExecutableFilename).FileVersion}.");
            }

            if (UacHelper.IsCurrentProcessElevated)
            {
                this.CreateNewWarnLineInConsoleLog("System", "Launcher is elevated as Administrator. Unless you want to use launcher's functions which requires Administrator, it is not recommended for the launcher to be elevated as Admin.");
            }

            if (App.Current.BootstrapVersion < 6)
            {
                this.CreateNewLineInConsoleLog("Launcher Updater", (consoleLog, writer, absoluteOffsetOfDocumentLine, myself) =>
                {
                    var absoluteOffsetOfItem = writer.InsertionOffset;
                    var warn = "{WARN} You are using an older version of the Launcher's bootstrap. It is recommended to update it.";
                    writer.Write(warn);
                    myself.consolelog_textcolorizer.Add(new TextStaticTransformData(absoluteOffsetOfItem, warn.Length, Brushes.Gold, Brushes.DarkGoldenrod));
                    writer.Write(' ');
                    myself.ConsoleLogHelper_WriteHyperLink(writer, "(Download it here)", StaticResources.Url_ShowLatestGithubRelease, VisualLineLinkText_LinkClicked);
                }, this);
            }

            this.timer_unloadWebBrowser = new DispatcherTimer(TimeSpan.FromMinutes(5), DispatcherPriority.Normal, this.Timer_UnloadWebBrowserControl, this.Dispatcher) { IsEnabled = false };
        }

        private Task<BackgroundSelfUpdateChecker> SetupBackgroundSelfUpdateChecker() => Task.Run(this.SetupBackgroundSelfUpdateChecker2);

        private BackgroundSelfUpdateChecker SetupBackgroundSelfUpdateChecker2()
        {
            var binDir = Path.GetFullPath("bin", RuntimeValues.RootDirectory);
            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var removelen = binDir.Length + 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            // dictionary.TrimExcess();
            var selfupdatecheck = new BackgroundSelfUpdateChecker(this.cancelAllOperation.Token, this.webclient, dictionary.Count == 0 ? FrozenDictionary<string, string>.Empty : dictionary.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));
            selfupdatecheck.UpdateFound += this.OnSelfUpdateFound;
            return selfupdatecheck;
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
                this.CreateNewLineInConsoleLog("ThemeManager", $"{(this.config_main.SyncThemeWithOS ? "Detected Windows" : "User changed")} theme setting: Light Mode.");
            }
            else
            {
                this.BgImg.Source = lazybg_dark.Value;
                this.CreateNewLineInConsoleLog("ThemeManager", $"{(this.config_main.SyncThemeWithOS ? "Detected Windows" : "User changed")} theme setting: Dark Mode.");
            }
            this.ConsoleLog.TextArea.TextView.Redraw();
        }

        protected override async Task OnCleanupBeforeClosed()
        {
            this.CreateNewLineInConsoleLog("System", (console, writer, absoluteOffsetOfCurrentLine, myself) =>
            {
                writer.Write("Stopping all operations and cleaning up resources before closing and exiting launcher. ");
                var startOffset = writer.InsertionOffset;
                writer.Write("The launcher will automatically terminate itself anyway if the cleanup process lasts longer than 10 seconds. Please just wait for it...");
                var endOffset = writer.InsertionOffset;
                myself.consolelog_textcolorizer.Add(new TextStaticTransformData(startOffset, endOffset - startOffset, myself.consolelog_boldTypeface, Brushes.Gold, Brushes.DarkGoldenrod));
            }, this);

            var listOfOperations = new List<Task>();

            Task t_stopClientUpdater;
            if (this.pso2Updater.IsBusy)
            {
                this.CreateNewWarnLineInConsoleLog("System", "Detecting a time-consuming cleanup operation: Game client updating. This may take a while to gracefully stop and clean up. Please wait...");

                var tsrc = new TaskCompletionSource();
                t_stopClientUpdater = tsrc.Task;
                listOfOperations.Add(t_stopClientUpdater);

                void onfinialize(GameClientUpdater sender, string pso2dir, bool isCancelled, IReadOnlyCollection<PatchListItem> patchlist, IReadOnlyDictionary<PatchListItem, bool?> download_result_list)
                {
                    this.pso2Updater.OperationCompleted -= onfinialize;
                    this.CreateNewLineInConsoleLog("System", "Game client updating has been stopped gracefully.");
                    tsrc.TrySetResult();
                }
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
            this.pso2HttpClient.Dispose();

            // this.pso2HttpClient.Dispose() already call Dispose() on its referenced HttpClient.
            // this.webclient.Dispose();

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
                    var dir = Path.GetDirectoryName(path);
                    if (dir != null)
                    {
                        Directory.CreateDirectory(dir);
                    }
                    File.WriteAllText(path, btn.Name);
                    break;
                }
            }

            // This is unnecessary as we don't really use the event CleanupBeforeClosed at all.
            // await base.OnCleanupBeforeClosed();
        }

        private async void LoadLauncherWebView_Click(object? sender, RoutedEventArgs? e)
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
                Assembly? asm;
                if (File.Exists(filepath))
                {
                    var asm_currentType = Assembly.GetAssembly(this.GetType());
                    if (asm_currentType == null)
                    {
                        asm = Assembly.LoadFrom(filepath);
                    }
                    else
                    {
                        var context = AssemblyLoadContext.GetLoadContext(asm_currentType);
                        if (context == null)
                        {
                            asm = Assembly.LoadFrom(filepath);
                        }
                        else
                        {
                            asm = context.LoadFromNativeImagePath(filepath, filepath);
                        }
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
                            this.CreateNewLineInConsoleLog("WebView", $"PSO2's launcher news has been loaded with WebView2 (Version: {webview.WebView2Version}).");
                        }
                        else
                        {
                            this.CreateNewLineInConsoleLog("WebView", "PSO2's launcher news has been loaded with Internet Explorer component.");
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
                var lines = new System.Windows.Documents.Inline[]
                {
                    new System.Windows.Documents.Run("An error has occurred while loading the necessary libraries for the web engine."),
                    new System.Windows.Documents.LineBreak(),
                    new System.Windows.Documents.Run("Please restart the launcher."),
                    new System.Windows.Documents.LineBreak(),
                    new System.Windows.Documents.Run("If the error still persists after restarting, please "),
                    new CommandHyperlink(new System.Windows.Documents.Run("create an issue report")) { NavigateUri = StaticResources.Url_ShowIssuesGithub },
                    new System.Windows.Documents.Run(" on Github."),
                };
                this.CreateNewErrorLineInConsoleLog("WebEngine", lines, "Error", ex);
                Prompt_Generic.ShowError(this, lines, "Error", ex, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (COMException ex) when (((uint)ex.ErrorCode) == 0x80004005)
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
                this.CreateNewErrorLineInConsoleLog("WebEngine", lines, "Error", ex);
                Prompt_Generic.ShowError(this, lines, "Error", ex, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                if (btn != null)
                {
                    btn.Click += this.LoadLauncherWebView_Click;
                }
                this.CreateNewErrorLineInConsoleLog("GameUpdater", string.Empty, null, ex);
                Prompt_Generic.ShowError(this, ex);
            }
        }

        
        private void WebViewCompatControl_Initialized(object? sender, EventArgs e)
        {
            if (sender is IWebViewCompatControl webview)
            {
                if (webview.IsUsingWebView2)
                {
                    webview.Navigated += this.Webview_Navigated;
                    webview.NavigateTo(StaticResources.SEGALauncherNewsUrl);
                }
                else
                {
                    // Using IE which has known inconsistent COM issue when interop with the native component on user's machine.
                    // It may throw some kind of "wrong method signature" COM exception.
                    try
                    {
                        webview.Navigated += this.Webview_Navigated;
                        webview.NavigateTo(StaticResources.SEGALauncherNewsUrl);
                    }
                    catch (COMException ex) when (((uint)ex.ErrorCode) == 0x80004005)
                    {
                        var btn = new WeirdButton() { Content = "Click to load launcher web view" };
                        btn.Click += this.LoadLauncherWebView_Click;
                        this.LauncherWebView.Child = btn;
                        
                        var lines = new List<System.Windows.Documents.Inline>();
                        lines.Add(new System.Windows.Documents.Run("An error has occurred while initializing the Internet Explorer web engine."));
                        lines.Add(new System.Windows.Documents.LineBreak());
                        lines.Add(new System.Windows.Documents.Run("It seems like the Internet Explorer component on your operating system is not compatible with this launcher."));
                        lines.Add(new System.Windows.Documents.LineBreak());
                        lines.Add(new System.Windows.Documents.Run("If you still want to load the news page, please try the alternative by installing WebView2 Evergreen Runtime."));
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

                        webview.Dispose();
                    }
                }
            }
        }

        // Combine with WebViewCompatControl_Initialized:
        // The first navigated will be allow. Any subsequent navigations will open with default browser.
        // This is to allow the first navigation which goes to the launcher page.
        private void Webview_Navigated(object? sender, NavigationEventArgs e)
        {
            if (sender is IWebViewCompatControl webview)
            {
                webview.Navigated -= this.Webview_Navigated;
                webview.Navigating += this.Webview_Navigating;
            }
        }

        private async Task OnEverythingIsDoneAndReadyToBeInteracted()
        {
            if (this.config_main.LauncherCheckForPSO2GameUpdateAtStartup)
            {
                await StartGameClientUpdate(false, this.config_main.LauncherCheckForPSO2GameUpdateAtStartupPrompt);
            }
        }

        private void Webview_Navigating(object? sender, NavigatingEventArgs e)
        {
            if (sender is IWebViewCompatControl wvc)
            {
                e.Cancel = true;
                // Hackish. De-elevate starting Url.
                try
                {
                    if (e.Uri.IsAbsoluteUri)
                    {
                        if (string.Equals(e.Uri.AbsoluteUri, StaticResources.SEGALauncherNewsUrl.AbsoluteUri, StringComparison.OrdinalIgnoreCase)) return;
                        WindowsExplorerHelper.OpenUrlWithDefaultBrowser(e.Uri.AbsoluteUri);
                    }
                    else if (Uri.TryCreate(wvc.CurrentUrl, e.Uri.ToString(), out var absUri))
                    {
                        if (string.Equals(absUri.AbsoluteUri, StaticResources.SEGALauncherNewsUrl.AbsoluteUri, StringComparison.OrdinalIgnoreCase)) return;
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
                            var isExistedSelfChecker = this.backgroundselfupdatechecker.IsValueCreated;
                            var selfchecker = await this.backgroundselfupdatechecker.Value;
                            if (isExistedSelfChecker)
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

        private void TabMainMenu_ButtonManageGameDataClick(object sender, RoutedEventArgs? e)
        {
            if (sender is TabMainMenu tab)
            {
                tab.ButtonManageGameDataClicked -= this.TabMainMenu_ButtonManageGameDataClick;
                try
                {
                    this.ShowDataManagerWindowDialog();
                }
                finally
                {
                    tab.ButtonManageGameDataClicked += this.TabMainMenu_ButtonManageGameDataClick;
                }
            }
        }

        private bool ShowDataManagerWindowDialog(Action<DataManagerWindow>? callbackOnBeforeShown = null)
        {
            var dialog = new DataManagerWindow(this.config_main);
            if (callbackOnBeforeShown != null)
            {
                EventHandler? ev = null;
                ev = (sender, e) =>
                {
                    dialog.FirstShown -= ev;
                    callbackOnBeforeShown.Invoke(dialog);

                };
                dialog.FirstShown += ev;
            }
            
            if (dialog.ShowCustomDialog(this) == true)
            {
                this.RefreshGameUpdaterOptions();
                return true;
            }
            return false;
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

        private void Timer_UnloadWebBrowserControl(object? sender, EventArgs e)
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
            if ((this._isTweakerRunning && Prompt_Generic.Show(this, $"The launcher is currently managing PSO2 Tweaker.{Environment.NewLine}It is recommended to exit the PSO2 Tweaker before closing this launcher to avoid config corruption.{Environment.NewLine}Are you sure you still want to close the launcher before closing PSO2 Tweaker?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
                || (this.pso2Updater.IsBusy && Prompt_Generic.Show(this, $"The launcher is currently updating PSO2 client.{Environment.NewLine}Are you sure you still want to close the launcher before the operation is completed?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes))
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

        public void OpenDataOrganizerWindow()
        {
            var window = new DataOrganizerWindow(this.config_main, this.pso2HttpClient, cancelAllOperation.Token);
            window.Owner = this;
            window.ShowDialog();
        }

        public void OpenModsOrganizerWindow()
        {
            var window = new ModsOrganizerWindow(this.config_main, this.pso2HttpClient, cancelAllOperation.Token);
            window.Owner = this;
            window.ShowDialog();
        }

        #region | WindowsCommandButtons |
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

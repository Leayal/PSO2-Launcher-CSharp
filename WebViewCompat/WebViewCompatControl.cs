using System;
using System.ComponentModel;
using System.IO;
using WinForm = System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using System.Windows.Controls;
using StackOverflow;
using Microsoft.Web.WebView2.Core;
using Leayal.SharedInterfaces;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;

namespace Leayal.WebViewCompat
{
    [ToolboxItem(true)]
    public class WebViewCompatControl : ScrollViewer, IWebViewCompatControl
    {
        private const int _Width = 822, _Height = 670;
        public static string DefaultUserAgent { get; set; } = string.Empty;

        private WinForm.WebBrowser _fallbackControl;
        private WebView2 _webView2;
        private readonly string _userAgent;
        private readonly IntPtr loadedWebView2Core;
        private readonly string webview2runtime_directory;

        private bool isInit;
        private EventHandler _browserInitialized;
        public event EventHandler BrowserInitialized
        {
            add
            {
                this._browserInitialized += value;
                if (this.isInit)
                {
                    if (this.Dispatcher.CheckAccess())
                    {
                        value.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        this.Dispatcher.BeginInvoke(value, new object[] { this, EventArgs.Empty });
                    }
                }
            }
            remove => this._browserInitialized -= value;
        }

        public bool IsUsingWebView2 => (this._webView2 != null && !this._webView2.IsDisposed);

        public WebViewCompatControl() : this(DefaultUserAgent) { }

        public WebViewCompatControl(string userAgent) : this(userAgent, true) { }

        public WebViewCompatControl(string userAgent, bool useWebView2IfPossible) : base()
        {
            this._userAgent = userAgent;
            this.loadedWebView2Core = IntPtr.Zero;
            this.HorizontalAlignment = HorizontalAlignment.Center;
            this.VerticalAlignment = VerticalAlignment.Center;
            VirtualizingPanel.SetIsVirtualizing(this, true);
            VirtualizingPanel.SetScrollUnit(this, ScrollUnit.Pixel);
            VirtualizingPanel.SetVirtualizationMode(this, VirtualizationMode.Recycling);
            ScrollViewer.SetCanContentScroll(this, true);
            ScrollViewer.SetVerticalScrollBarVisibility(this, ScrollBarVisibility.Auto);
            this.isInit = false;

            if (useWebView2IfPossible && WebViewCompat.TryGetWebview2Runtime(out var webview2runtimedir) && NativeLibrary.TryLoad(Path.GetFullPath(Path.Combine("bin", Environment.Is64BitProcess ? "native-x64" : "native-x86", "WebView2Loader.dll"), RuntimeValues.RootDirectory), out var loaded))
            {
                var host = new WindowsFormsHostEx();
                this.Content = host;
                this.webview2runtime_directory = webview2runtimedir;
                try
                {
                    this.loadedWebView2Core = loaded;
                    //*
                    this._webView2 = new WebView2();
                    this._webView2.HandleCreated += this.OnWebView2WPFControlLoaded;
                    host.Child = this._webView2;
                    if (!this._webView2.Created)
                    {
                        this._webView2.CreateControl();
                    }
                    //*/
                }
                catch
                {
                    host.Child = null;
                    if (this._webView2 != null)
                    {
                        this._webView2.Dispose();
                        this._webView2 = null;
                    }
                    this.loadedWebView2Core = IntPtr.Zero;
                    NativeLibrary.Free(loaded);
                    SetIECompatVersion();
                    this._fallbackControl = this.CreateIE();
                    host.Child = this._fallbackControl;
                    if (!this._fallbackControl.Created)
                    {
                        this._fallbackControl.CreateControl();
                    }
                }
            }
            else
            {
                SetIECompatVersion();
                this._fallbackControl = this.CreateIE();
                this.Content = new WindowsFormsHostEx() { Child = this._fallbackControl };
                if (!this._fallbackControl.Created)
                {
                    this._fallbackControl.CreateControl();
                }
            }
        }

        private WinForm.WebBrowser CreateIE()
        {
            var wb = new WinForm.WebBrowser();
            wb.HandleCreated += this.FallbackControl_Initialized;
            wb.Navigating += this.FallbackControl_Navigating2;
            wb.DocumentCompleted += this.FallbackControl_DocumentCompleted;
            return wb;
        }

        public Uri CurrentUrl
        {
            get
            {
                if (this._webView2 != null)
                {
                    return this._webView2.Source;
                }
                else
                {
                    return this._fallbackControl.Url;
                }
            }
        }

        public event EventHandler<NavigationEventArgs> Navigated;
        private void FallbackControl_DocumentCompleted(object sender, WinForm.WebBrowserDocumentCompletedEventArgs e)
        {
            var ev = new NavigationEventArgs(e.Url);
            this.Navigated?.Invoke(this, ev);
        }
        
        //*
        private void Wv_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            NavigationEventArgs ev;
            if (sender is WebView2 wv)
            {
                ev = new NavigationEventArgs(wv.Source);
            }
            else if (sender is CoreWebView2 cwv)
            {
                ev = new NavigationEventArgs(new Uri(cwv.Source));
            }
            else
            {
                ev = null;
            }
            if (ev != null)
            {
                this.Navigated?.Invoke(this, ev);
            }
        }
        //*/

        public event EventHandler<NavigatingEventArgs> Navigating;
        
        //*
        private void Wv_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            var ev = new NavigatingEventArgs(new Uri(e.Uri));
            this.Navigating?.Invoke(this, ev);
            e.Cancel = ev.Cancel;
            // System.Windows.Navigation.NavigatingCancelEventArgs.
            // this.Navigating?.Invoke(this, new System.Windows.Navigation.NavigatingCancelEventArgs() { });
        }
        //*/

        private void FallbackControl_Navigating2(object sender, WinForm.WebBrowserNavigatingEventArgs e)
        {
            var ev = new NavigatingEventArgs(e.Url);
            this.Navigating?.Invoke(this, ev);
            e.Cancel = ev.Cancel;
        }

        private void FallbackControl_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            var ev = new NavigatingEventArgs(e.Uri);
            this.Navigating?.Invoke(this, ev);
            e.Cancel = ev.Cancel;
        }

        private void FallbackControl_Initialized(object sender, EventArgs e)
        {
            this._fallbackControl.Width = _Width;
            this._fallbackControl.Height = _Height;
            this._fallbackControl.ScrollBarsEnabled = false;
            this._fallbackControl.Dock = WinForm.DockStyle.Top;
            // this.OnInitialized(EventArgs.Empty);
            this._browserInitialized?.Invoke(this, EventArgs.Empty);
            this.isInit = true;
        }

        private void OnWebView2WPFControlLoaded(object sender, EventArgs e)
        {
            this._webView2.Width = _Width;
            this._webView2.Height = _Height;
            this._webView2.NavigationStarting += this.Wv_NavigationStarting;
            this._webView2.NavigationCompleted += this.Wv_NavigationCompleted;
            this.OnWebView2CoreInit(this.webview2runtime_directory, Path.GetFullPath(Path.Combine("data", "webview2"), RuntimeValues.RootDirectory));
        }
        
        //*
        private async void OnWebView2CoreInit(string browserExecutablePath, string userDataFolder)
        {
            // Seems like "--disable-breakpad" does nothing??
            // I can still see crashpad process running.
            try
            {
                var env = await CoreWebView2Environment.CreateAsync(browserExecutablePath, userDataFolder, new CoreWebView2EnvironmentOptions("--disable-breakpad"));
                await this._webView2.EnsureCoreWebView2Async(env);
                var core = this._webView2.CoreWebView2;
                var coresetting = core.Settings;
                coresetting.UserAgent = this._userAgent;
                coresetting.AreDevToolsEnabled = false;
                coresetting.AreHostObjectsAllowed = false;
                coresetting.IsGeneralAutofillEnabled = false;
                coresetting.IsPasswordAutosaveEnabled = false;
                coresetting.AreDefaultContextMenusEnabled = false;
                coresetting.AreBrowserAcceleratorKeysEnabled = false;
                // coresetting.AreDefaultScriptDialogsEnabled = false;
                coresetting.IsBuiltInErrorPageEnabled = false;
                coresetting.IsPinchZoomEnabled = false;
                coresetting.IsStatusBarEnabled = false;
                coresetting.IsSwipeNavigationEnabled = true;

                this._browserInitialized?.Invoke(this, EventArgs.Empty);
                this.isInit = true;
                // this.OnInitialized(EventArgs.Empty);
            }
            catch
            {
                if (this.Content is WindowsFormsHostEx host)
                {
                    host.Child = null;
                    if (this._webView2 != null)
                    {
                        this._webView2.Dispose();
                        this._webView2 = null;
                        if (this.loadedWebView2Core != IntPtr.Zero)
                        {
                            NativeLibrary.Free(this.loadedWebView2Core);
                        }
                    }
                    this._fallbackControl = this.CreateIE();
                    host.Child = this._fallbackControl;
                    if (!this._fallbackControl.Created)
                    {
                        this._fallbackControl.CreateControl();
                    }
                }
            }
        }

        // Document events here.
        private void Wv_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            // this._fallbackControl.Document.AttachEventHandler();
        }
        //*/

        private static System.Drawing.Size GetDocumentBodySize(WinForm.WebBrowser webBrowser)
        {
            int CurrentWidth, CurrentHeight, CurrentMinWidth = webBrowser.Width, CurrentMaxWidth = 0
                , CurrentMinHeight = webBrowser.Height, CurrentMaxHeight = 0;

            foreach (WinForm.HtmlElement webBrowserElement in webBrowser.Document.Body.All)
            {
                if ((CurrentWidth = Math.Max(webBrowserElement.ClientRectangle.Width, webBrowserElement.ScrollRectangle.Width)) > CurrentMaxWidth)
                    CurrentMaxWidth = CurrentWidth;

                if ((CurrentHeight = Math.Max(webBrowserElement.ClientRectangle.Height, webBrowserElement.ScrollRectangle.Height)) > CurrentMaxHeight)
                    CurrentMaxHeight = CurrentHeight;
            }

            return new System.Drawing.Size(CurrentMaxWidth > CurrentMinWidth ? CurrentMaxWidth : CurrentMinWidth, CurrentMaxHeight > CurrentMinHeight ? CurrentMaxHeight : CurrentMinHeight);
        }

        public void NavigateTo(Uri url)
        {
            if (this._webView2 != null)
            {
                this._webView2.Source = url;
            }
            else
            {
                this._fallbackControl.Navigate(url, null, null, "User-Agent: " + this._userAgent + "\r\n");
            }
        }

        public void Dispose()
        {
            if (this._webView2 != null)
            {
                this._webView2.Dispose();
                this._webView2 = null;
                if (this.loadedWebView2Core != IntPtr.Zero)
                {
                    NativeLibrary.Free(this.loadedWebView2Core);
                }
            }
            this._fallbackControl?.Dispose();
        }

        private static void SetIECompatVersion()
        {
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
        }
    }
}

using System;
using System.ComponentModel;
using System.IO;
using WinForm = System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Leayal.SharedInterfaces;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Leayal.Shared.Windows;

namespace Leayal.WebViewCompat
{
    [ToolboxItem(true)]
    public class WebViewCompatControl : Border, IWebViewCompatControl
    {
        private const int _Width = 825, _Height = 670;
        public static string DefaultUserAgent { get; set; } = string.Empty;

        private WinForm.WebBrowser? _fallbackControl;
        private WebView2Ex? _webView2;
        private readonly string _userAgent;
        private readonly IntPtr loadedWebView2Core;
        private readonly string webview2version;
        
        private bool isInit;
        private EventHandler? _browserInitialized;
        public event EventHandler BrowserInitialized
        {
            add
            {
                if (value == null) return;
                this._browserInitialized += value;
                if (this.isInit)
                {
                    if (this.Dispatcher.CheckAccess())
                    {
                        value?.Invoke(this, EventArgs.Empty);
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

        public string WebView2Version => this.webview2version;

        public WebViewCompatControl() : this(DefaultUserAgent) { }

        public WebViewCompatControl(string userAgent) : this(userAgent, true) { }

        public WebViewCompatControl(string userAgent, bool useWebView2IfPossible) : base()
        {
            this._userAgent = userAgent;
            this.loadedWebView2Core = IntPtr.Zero;
            this.isInit = false;

            if (useWebView2IfPossible && NativeLibrary.TryLoad(Path.GetFullPath(Path.Combine("bin", Environment.Is64BitProcess ? "native-x64" : "native-x86", "WebView2Loader.dll"), RuntimeValues.RootDirectory), out var loaded))
            {
                if (WebViewCompat.TryGetWebview2Runtime(out var _webview2version))
                {
                    this.webview2version = _webview2version;
                    var host = new WindowsFormsHostEx2();
                    this.Child = host;
                    try
                    {
                        this.loadedWebView2Core = loaded;
                        //*
                        this._webView2 = new WebView2Ex()
                        {
                            Anchor = WinForm.AnchorStyles.Top | WinForm.AnchorStyles.Bottom,
                            Location = System.Drawing.Point.Empty,
                            Width = _Width,
                            Height = _Height
                        };
                        this._webView2.NavigationStarting += this.Wv_NavigationStarting;
                        this._webView2.NavigationCompleted += this.Wv_NavigationCompleted;
                        // this._webView2.HandleCreated += this.OnWebView2WPFControlLoaded;
                        host.Child = this._webView2;
                        this.OnWebView2CoreInit();
                        // if (!this._webView2.Created) this._webView2.CreateControl();
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
                    this.loadedWebView2Core = IntPtr.Zero;
                    NativeLibrary.Free(loaded);
                    this.webview2version = string.Empty;
                    SetIECompatVersion();
                    this._fallbackControl = this.CreateIE();
                    var host = new WindowsFormsHostEx2() { Child = this._fallbackControl };
                    this.Child = host;
                    if (!this._fallbackControl.Created)
                    {
                        this._fallbackControl.CreateControl();
                    }
                }
            }
            else
            {
                this.webview2version = string.Empty;
                SetIECompatVersion();
                this._fallbackControl = this.CreateIE();
                var host = new WindowsFormsHostEx2() { Child = this._fallbackControl };
                this.Child = host;
                if (!this._fallbackControl.Created)
                {
                    this._fallbackControl.CreateControl();
                }
            }
        }

        private WinForm.WebBrowser CreateIE()
        {
            var wb = new WinForm.WebBrowser()
            {
                Anchor = WinForm.AnchorStyles.Top | WinForm.AnchorStyles.Bottom,
                Location = System.Drawing.Point.Empty
            };
            wb.HandleCreated += this.FallbackControl_Initialized;
            wb.Navigating += this.FallbackControl_Navigating2;
            wb.DocumentCompleted += this.FallbackControl_DocumentCompleted;
            return wb;
        }

        public Uri? CurrentUrl
        {
            get
            {
                if (this._webView2 != null)
                {
                    return this._webView2.Source;
                }
                else if (this._fallbackControl != null)
                {
                    return this._fallbackControl.Url;
                }
                else
                {
                    return null;
                }
            }
        }

        public event EventHandler<NavigationEventArgs> Navigated;
        private void FallbackControl_DocumentCompleted(object? sender, WinForm.WebBrowserDocumentCompletedEventArgs e)
        {
            var ev = new NavigationEventArgs(e.Url);
            this.Navigated?.Invoke(this, ev);
        }
        
        //*
        private void Wv_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            NavigationEventArgs? ev;
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
        private void Wv_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            var ev = new NavigatingEventArgs(new Uri(e.Uri));
            this.Navigating?.Invoke(this, ev);
            e.Cancel = ev.Cancel;
            // System.Windows.Navigation.NavigatingCancelEventArgs.
            // this.Navigating?.Invoke(this, new System.Windows.Navigation.NavigatingCancelEventArgs() { });
        }
        //*/

        private void FallbackControl_Navigating2(object? sender, WinForm.WebBrowserNavigatingEventArgs e)
        {
            var ev = new NavigatingEventArgs(e.Url);
            this.Navigating?.Invoke(this, ev);
            e.Cancel = ev.Cancel;
        }

        private void FallbackControl_Navigating(object? sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            var ev = new NavigatingEventArgs(e.Uri);
            this.Navigating?.Invoke(this, ev);
            e.Cancel = ev.Cancel;
        }

        private void FallbackControl_Initialized(object? sender, EventArgs e)
        {
            if (this._fallbackControl != null)
            {
                this._fallbackControl.Width = _Width;
                this._fallbackControl.Height = _Height;
                this._fallbackControl.ScrollBarsEnabled = false;
            }
            // this._fallbackControl.Dock = WinForm.DockStyle.Top;
            // this.OnInitialized(EventArgs.Empty);
            this._browserInitialized?.Invoke(this, EventArgs.Empty);
            this.isInit = true;
        }

        //*
        private async void OnWebView2CoreInit()
        {
            // Seems like "--disable-breakpad" does nothing??
            // I can still see crashpad process running.
            try
            {
                if (this._webView2 == null) throw new InvalidOperationException();
                await this._webView2.EnsureInitAsync();
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
                coresetting.IsBuiltInErrorPageEnabled = true;
                coresetting.IsPinchZoomEnabled = false;
                coresetting.IsStatusBarEnabled = false;
                coresetting.IsSwipeNavigationEnabled = false;

                this._browserInitialized?.Invoke(this, EventArgs.Empty);
                this.isInit = true;
                // this.OnInitialized(EventArgs.Empty);
            }
            catch
            {
                if (this.Child is WindowsFormsHostEx2 host)
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
                    SetIECompatVersion();
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
            if (this.Child is IDisposable cleanupableChildControl)
            {
                // CaptureMouseWheelWhenUnfocusedBehavior.SetIsEnabled(host, false);
                cleanupableChildControl.Dispose();
            }
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

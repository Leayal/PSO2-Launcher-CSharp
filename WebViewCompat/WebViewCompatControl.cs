using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using WinForm = System.Windows.Forms;
using System.Windows.Data;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Wpf;
using System.Windows.Forms.Integration;
using System.Windows.Controls;
using StackOverflow;

namespace Leayal.WebViewCompat
{
    [ToolboxItem(true)]
    public class WebViewCompatControl : ScrollViewer, IDisposable
    {
        public static string DefaultUserAgent { get; set; } = string.Empty;

        private readonly WinForm.WebBrowser _fallbackControl;
        private readonly WebView2 _webView2;
        private readonly string _userAgent;

        public WebViewCompatControl() : this(DefaultUserAgent) { }

        public WebViewCompatControl(string userAgent)
        {
            // ScrollViewer.SetCanContentScroll(this, true);
            // VirtualizingPanel.SetIsVirtualizing(this, true);
            // VirtualizingPanel.SetScrollUnit(this, ScrollUnit.Pixel);
            // VirtualizingPanel.SetVirtualizationMode(this, VirtualizationMode.Recycling);
            Panel panel = new DockPanel()
            {
                MinWidth = 825,
                MinHeight = 700
            };
            this._userAgent = userAgent;
            if (WebViewCompat.HasWebview2Runtime())
            {
                this._webView2 = new WebView2();
                this._webView2.CreationProperties = new CoreWebView2CreationProperties() { UserDataFolder = Path.GetFullPath("BrowserCache", AppDomain.CurrentDomain.BaseDirectory), Language = "" };
                this._webView2.CoreWebView2.Settings.UserAgent = this._userAgent;
                this._webView2.NavigationStarting += Wv_NavigationStarting;
                this._webView2.NavigationCompleted += Wv_NavigationCompleted;
                this._webView2.CoreWebView2InitializationCompleted += Wv_CoreWebView2InitializationCompleted;
                // this._webView2.WebMessageReceived += Wv_WebMessageReceived;
                this.Content = this._webView2;
            }
            else
            {
                var host = new WindowsFormsHostEx();
                host.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                host.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                this._fallbackControl = new WinForm.WebBrowser()
                {
                    Width = 825,
                    Height = 650
                };
                this._fallbackControl.ScrollBarsEnabled = false;
                // this._fallbackControl.Initialized += this.FallbackControl_Initialized;
                // this._fallbackControl.Loaded += this.FallbackControl_Initialized;
                // this._fallbackControl.Navigating += this.FallbackControl_Navigating;
                // this._fallbackControl.Navigated += this.FallbackControl_Navigated;
                this._fallbackControl.Dock = WinForm.DockStyle.Top;
                host.Loaded += this.FallbackControl_Initialized;
                // this._fallbackControl.HandleCreated += this.FallbackControl_Initialized;
                this._fallbackControl.Navigating += this.FallbackControl_Navigating2;
                this._fallbackControl.Navigated += this.FallbackControl_Navigated2;
                // this._fallbackControl.LoadCompleted += this.FallbackControl_LoadCompleted;
                // panel.Children.Add(this._fallbackControl);
                host.Child = this._fallbackControl;
                this.Content = host;
                if (!this._fallbackControl.Created)
                {
                    this._fallbackControl.CreateControl();
                }
            }
        }

        private void FallbackControl_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            // this._fallbackControl.Document
        }

        public event EventHandler<NavigationEventArgs> Navigated;
        private void FallbackControl_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            var ev = new NavigationEventArgs(e.Uri);
            this.Navigated?.Invoke(this, ev);
        }
        private void FallbackControl_Navigated2(object sender, WinForm.WebBrowserNavigatedEventArgs e)
        {
            var ev = new NavigationEventArgs(e.Url);
            this.Navigated?.Invoke(this, ev);
        }
        private void Wv_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            var ev = new NavigationEventArgs(((WebView2)sender).Source);
            this.Navigated?.Invoke(this, ev);
        }

        public event EventHandler<NavigatingEventArgs> Navigating;
        private void Wv_NavigationStarting(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
        {
            var ev = new NavigatingEventArgs(new Uri(e.Uri));
            this.Navigating?.Invoke(this, ev);
            e.Cancel = ev.Cancel;
            // System.Windows.Navigation.NavigatingCancelEventArgs.
            // this.Navigating?.Invoke(this, new System.Windows.Navigation.NavigatingCancelEventArgs() { });
        }
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
            /*
            if (!ev.Cancel)
            {
                if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New)
                {
                    return;
                    this._fallbackControl.Navigating -= this.FallbackControl_Navigating;
                    e.Cancel = true;
                    this._fallbackControl.Navigate(e.Uri, null, null, "User-Agent: " + this._userAgent + "\r\n");
                    this._fallbackControl.Navigating += this.FallbackControl_Navigating;
                }
                else
                {
                    e.WebRequest.Headers.Set("User-Agent", this._userAgent);
                    e.WebRequest.Proxy = new WebProxy("127.0.0.1", 8866);
                }
            }
            */
        }

        private void FallbackControl_Initialized(object sender, EventArgs e)
        {
            this.OnInitialized(EventArgs.Empty);
        }
        private void Wv_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            
            this.OnInitialized(EventArgs.Empty);
        }

        public class NavigationEventArgs : EventArgs
        {
            public Uri Uri { get; }

            public NavigationEventArgs(Uri uri)
            {
                this.Uri = uri;
            }
        }

        public class NavigatingEventArgs : NavigationEventArgs
        {
            public bool Cancel { get; set; }

            public NavigatingEventArgs(Uri uri) : base(uri)
            {
                this.Cancel = false;
            }
        }
        
        private void Wv_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {

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
            this._webView2?.Dispose();
            this._fallbackControl?.Dispose();
        }
    }
}

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
using Microsoft.Web.WebView2.Core;
using Leayal.SharedInterfaces;

namespace Leayal.WebViewCompat
{
    [ToolboxItem(true)]
    public class WebViewCompatControl : ScrollViewer, IWebViewCompatControl
    {
        public static string DefaultUserAgent { get; set; } = string.Empty;

        private readonly WinForm.WebBrowser _fallbackControl;
        private readonly WebView2 _webView2;
        private readonly string _userAgent;

        public WebViewCompatControl() : this(DefaultUserAgent) { }

        public WebViewCompatControl(string userAgent)
        {
            ScrollViewer.SetCanContentScroll(this, true);
            VirtualizingPanel.SetIsVirtualizing(this, true);
            VirtualizingPanel.SetScrollUnit(this, ScrollUnit.Pixel);
            VirtualizingPanel.SetVirtualizationMode(this, VirtualizationMode.Recycling);
            this._userAgent = userAgent;
            if (WebViewCompat.HasWebview2Runtime())
            {
                this._webView2 = new WebView2();
                this._webView2.CreationProperties = new CoreWebView2CreationProperties() { UserDataFolder = Path.GetFullPath("BrowserCache", RuntimeValues.RootDirectory), Language = "" };
                this._webView2.CoreWebView2.Settings.UserAgent = this._userAgent;
                // this._webView2.NavigationStarting += this.Wv_NavigationStarting;
                // this._webView2.NavigationCompleted += this.Wv_NavigationCompleted;

                //this._webView2. += this.FallbackControl_LoadCompleted;
                this._webView2.CoreWebView2InitializationCompleted += this.Wv_CoreWebView2InitializationCompleted;
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
                    Width = 820,
                    Height = 680
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
                // this._fallbackControl.Navigated += this.FallbackControl_Navigated2;
                this._fallbackControl.DocumentCompleted += this.FallbackControl_DocumentCompleted;
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
            this._webView2.CoreWebView2.NavigationStarting += this.Wv_NavigationStarting;
            this._webView2.CoreWebView2.NavigationCompleted += this.Wv_NavigationCompleted;
            this.OnInitialized(EventArgs.Empty);
        }
        

        // Document events here.
        private void Wv_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            // this._fallbackControl.Document.AttachEventHandler();
        }

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
            this._webView2?.Dispose();
            this._fallbackControl?.Dispose();
        }
    }
}

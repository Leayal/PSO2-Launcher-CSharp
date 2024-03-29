﻿using System;

namespace Leayal.SharedInterfaces
{
    public interface IWebViewCompatControl : IDisposable
    {
        public Uri CurrentUrl { get; }

        bool IsUsingWebView2 { get; }

        string WebView2Version { get; }

        event EventHandler BrowserInitialized;

        event EventHandler<NavigationEventArgs> Navigated;

        event EventHandler<NavigatingEventArgs> Navigating;

        void NavigateTo(Uri url);
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
}

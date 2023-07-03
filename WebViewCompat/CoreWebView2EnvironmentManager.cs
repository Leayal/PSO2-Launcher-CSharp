using Leayal.Shared;
using Leayal.SharedInterfaces;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.WebViewCompat
{
    public class CoreWebView2EnvironmentManager : IDisposable
    {
        public readonly static CoreWebView2EnvironmentManager DefaultInstance = new CoreWebView2EnvironmentManager();

        private int _refCount;
        private readonly WeakLazy<Task<CoreWebView2Environment>> _lazytask_env;
        private Task<CoreWebView2Environment>? _keep_alive;
        private bool disposedValue;

        public CoreWebView2EnvironmentManager()
        {
            this._keep_alive = null;
            this._lazytask_env = new WeakLazy<Task<CoreWebView2Environment>>(this.CreateTask);
            this._refCount = 0;
        }

        private Task<CoreWebView2Environment> CreateTask()
        {
            if (!WebViewCompat.TryGetWebview2Runtime(out var _webview2version))
            {
                _webview2version = null;
            }
            return CoreWebView2Environment.CreateAsync(null, Path.GetFullPath(Path.Combine("data", "webview2"), RuntimeValues.RootDirectory), new CoreWebView2EnvironmentOptions()
            {
                AdditionalBrowserArguments = "--disable-breakpad",
                AllowSingleSignOnUsingOSPrimaryAccount = false, // We don't really need any accounts or anything but the Browser's user-agent to view SEGA's launcher site.
                TargetCompatibleBrowserVersion = _webview2version, // For binary compatible with the given version in WebView2Loader.dll.
                ExclusiveUserDataFolderAccess = true, // For now, we're only the one using the folder anyway.
                IsCustomCrashReportingEnabled = true, // Enable custom reporting so that we stop WebView2's default reporting implementation from sending reports to MS server.
                EnableTrackingPrevention = true // Obvious
            });
        }

        public Task<CoreWebView2Environment> CreateEnvironmentInstance()
        {
            Interlocked.Increment(ref this._refCount);
            lock (this._lazytask_env)
            {
                this._keep_alive = this._lazytask_env.Value;
            }
            return this._keep_alive;
        }

        public void DestroyEnvironmentInstance()
        {
            var newVal = Interlocked.Decrement(ref this._refCount);
            if (newVal < 0)
            {
                Interlocked.Exchange(ref this._refCount, 0);
                newVal = 0;
            }
            if (newVal == 0)
            {
                this._keep_alive = null;
            }
        }

        public void Dispose() => this.DestroyEnvironmentInstance();
    }
}

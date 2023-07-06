using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Leayal.WebViewCompat
{
    class WebView2Ex : WebView2
    {
        private static readonly FieldInfo? field_coreWebView2Controller;
        // private static readonly PropertyInfo? prop_coreWebView2Controller;

        static WebView2Ex()
        {
            // CoreWebView2Controller
            var currentType = typeof(WebView2Ex);
            var currentBaseType = currentType.BaseType;
            if (currentBaseType?.Equals(typeof(WebView2)) == true)
            {
                field_coreWebView2Controller = currentBaseType.GetField("_coreWebView2Controller", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            }
            /* // This is for handling WPF-based WebView2. But we wrapped WinForm-based WebView2 instead so we never touched WPF-based one.
            else if (currentBaseType?.Equals(typeof(Microsoft.Web.WebView2.Wpf.WebView2)) == true)
            {
                prop_coreWebView2Controller = currentBaseType.GetProperty("CoreWebView2Controller", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty | BindingFlags.Public, null, typeof(CoreWebView2Controller), Array.Empty<Type>(), null);
            }
            */
        }

        private CoreWebView2Controller? controller;
        private bool isCustomWV2Environement;

        /// <summary>
        /// The underlying <seealso cref="Microsoft.Web.WebView2.Core.CoreWebView2Controller"/>.
        /// Use this property to perform more operations on the WebView2 content than is exposed on the WebView2. This value is <see langword="null"/> until
        /// it is initialized and the object itself has undefined behaviour once the control
        /// is disposed. You can force the underlying <seealso cref="Microsoft.Web.WebView2.Core.CoreWebView2Controller"/> to initialize via the
        /// <seealso cref="EnsureInitAsync(CoreWebView2Environment?, CoreWebView2ControllerOptions?)"/> method or <seealso cref="WebView2.EnsureCoreWebView2Async(CoreWebView2Environment)"/> method.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the calling thread isn't the thread which created this object (usually the UI thread). See <seealso cref="Control.InvokeRequired"/> for more info</exception>
        public CoreWebView2Controller? CoreWebView2Controller
        {
            get
            {
                _ = this.CoreWebView2;
                return this.controller;
            }
        }

        public WebView2Ex() : base()
        {
            this.isCustomWV2Environement = false;
            this.CoreWebView2InitializationCompleted += WebView2Ex_CoreWebView2InitializationCompleted;
        }

        private static void WebView2Ex_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (sender is WebView2Ex wv2ex)
            {
                wv2ex.CoreWebView2InitializationCompleted -= WebView2Ex_CoreWebView2InitializationCompleted;
                if (field_coreWebView2Controller != null)
                {
                    TypedReference reference = __makeref(wv2ex);
                    if (field_coreWebView2Controller.GetValueDirect(reference) is CoreWebView2Controller controller)
                    {
                        wv2ex.controller = controller;
                    }
                }
                /* // This is for handling WPF-based WebView2. But we wrapped WinForm-based WebView2 instead so we never touched WPF-based one.
                else if (prop_coreWebView2Controller != null)
                {
                    var getterInfo = prop_coreWebView2Controller.GetMethod;
                    if (getterInfo != null && Delegate.CreateDelegate(typeof(Func<CoreWebView2Controller>), wv2ex, getterInfo) is Func<CoreWebView2Controller> getter)
                    {
                        wv2ex.controller = getter.Invoke();
                    }
                }
                */
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);
            var core = this.CoreWebView2;
            if (core != null)
            {
                if (value)
                {
                    if (core.IsSuspended) core.Resume();
                }
                else
                {
                    core.TrySuspendAsync();
                }
            }
        }

        public async Task EnsureInitAsync(CoreWebView2Environment? env = null, CoreWebView2ControllerOptions? opts = null)
        {
            if (env == null)
            {
                env = await CoreWebView2EnvironmentManager.DefaultInstance.CreateEnvironmentInstance();
            }
            else
            {
                this.isCustomWV2Environement = true;
            }
            await this.EnsureCoreWebView2Async(env, opts);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var contr = Interlocked.Exchange(ref this.controller, null);
                // if (contr != null) contr.Close();
                if (!this.isCustomWV2Environement)
                {
                    CoreWebView2EnvironmentManager.DefaultInstance.DestroyEnvironmentInstance();
                }
                // this.controller?.Close();
            }
            base.Dispose(disposing);
        }
    }
}

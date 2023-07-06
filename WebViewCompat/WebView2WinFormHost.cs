using Leayal.Shared;
using Leayal.SharedInterfaces;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinFormsControl = System.Windows.Forms;

namespace Leayal.WebViewCompat
{
    class WebView2WinFormHost : WinFormsControl.Control
    {
        private readonly Task<CoreWebView2Environment> _task_env;
        private readonly TaskCompletionSource<CoreWebView2Controller> _taskSrc_controller;
        private readonly Task<CoreWebView2Controller> _task_controller;
        private CoreWebView2Controller? _controller;
        private int flag_task_env, flag_firstTimeShown;

        /// <summary>Gets the core back-end which backed the WebView2.</summary>
        /// <returns>A <seealso cref="CoreWebView2"/> which backed the current instance's WebView2.</returns>
        /// <exception cref="InvalidOperationException">The method was called before the instance is fully init. Consider using <seealso cref="WebView2ControllerCreated"/>, <seealso cref="WebView2Ready"/> or <seealso cref="WaitUntilCoreWebView2ControllerCreated"/>.</exception>
        public CoreWebView2 GetCoreWebView2()
        {
            var controller = this.GetCoreWebView2Controller();
            return controller.CoreWebView2;
        }

        /// <summary>Gets the controller which controls the WebView2 backend.</summary>
        /// <returns>A <seealso cref="CoreWebView2Controller"/> which controls the current instance's WebView2 backend.</returns>
        /// <exception cref="InvalidOperationException">The method was called before the instance is fully init. Consider using <seealso cref="WebView2ControllerCreated"/>, <seealso cref="WebView2Ready"/> or <seealso cref="WaitUntilCoreWebView2ControllerCreated"/>.</exception>
        public CoreWebView2Controller GetCoreWebView2Controller()
        {
            if (this._controller == null) throw new InvalidOperationException();
            return this._controller;
        }

        public async Task WaitUntilCoreWebView2ControllerCreated() => await this._task_controller;

        public WebView2WinFormHost(CoreWebView2EnvironmentManager manager) : base()
        {
            this.flag_task_env = 0;
            this.DoubleBuffered = false;
            this.SetStyle(WinFormsControl.ControlStyles.UserPaint | WinFormsControl.ControlStyles.Opaque | WinFormsControl.ControlStyles.AllPaintingInWmPaint, true);
            this._taskSrc_controller = new TaskCompletionSource<CoreWebView2Controller>();
            this._task_env = manager.CreateEnvironmentInstance();
            this._task_controller = this._taskSrc_controller.Task;
        }

        public WebView2WinFormHost() : this(CoreWebView2EnvironmentManager.DefaultInstance) { }

        // Async void is evil for debugging, but let's go.
        protected override void OnHandleCreated(EventArgs e)
        {
            if (Interlocked.CompareExchange(ref this.flag_task_env, 1, 0) == 0)
            {
                this._task_env.ContinueWith(this.AfterHandleCreatedFirstTime, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.NotOnCanceled);
            }
            else
            {
                this._task_controller.ContinueWith((t, myself) =>
                {
                    if (myself is WebView2WinFormHost host)
                    {
                        t.Result.ParentWindow = host.Handle;
                    }
                }, this);
            }
            base.OnHandleCreated(e);
        }

        protected override void DestroyHandle()
        {
            base.DestroyHandle();
        }

        private async void AfterHandleCreatedFirstTime(Task<CoreWebView2Environment> task)
        {
            if (!task.IsCompletedSuccessfully) throw new InvalidProgramException();
            if (this._controller == null)
            {
                var env = task.Result;
                var opts = env.CreateCoreWebView2ControllerOptions();
                opts.IsInPrivateModeEnabled = false;
                opts.ProfileName = "leapso2launcher";
                var controller = await env.CreateCoreWebView2ControllerAsync(this.Handle, opts);
                controller.AllowExternalDrop = false;
                controller.BoundsMode = CoreWebView2BoundsMode.UseRawPixels;
                controller.Bounds = this.ClientRectangle;
                controller.ShouldDetectMonitorScaleChanges = false;
                this._controller = controller;
                this._taskSrc_controller.TrySetResult(controller);
                this.OnWebView2ControllerCreated(EventArgs.Empty);
            }
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            var controller = this._controller;
            if (controller != null)
            {
                controller.Bounds = this.ClientRectangle;
            }
            base.OnClientSizeChanged(e);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Interlocked.CompareExchange(ref this.flag_firstTimeShown, 1, 0) == 0)
            {
                this._task_controller.ContinueWith((t, myself) =>
                {
                    if (myself is WebView2WinFormHost host)
                    {
                        host.WebView2Ready?.Invoke(host, EventArgs.Empty);
                    }
                }, this);
            }
        }

        public EventHandler? WebView2ControllerCreated;
        protected virtual void OnWebView2ControllerCreated(EventArgs e) => this.WebView2ControllerCreated?.Invoke(this, e);

        public EventHandler? WebView2Ready;
        protected virtual void OnWebView2Ready(EventArgs e) => this.WebView2Ready?.Invoke(this, e);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                this._taskSrc_controller.TrySetCanceled();
                this._controller?.Close();
            }
        }
    }
}

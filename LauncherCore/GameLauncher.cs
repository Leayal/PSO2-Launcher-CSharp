using Leayal.SharedInterfaces;
using Leayal.SharedInterfaces.Communication;
using System;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace Leayal.PSO2Launcher.Core
{
    public class GameLauncher : IWPFApp
    {
        // private App _app;
        
        public GameLauncher()
        {
            // var adminClient = new Leayal.PSO2Launcher.AdminProcess.AdminClient();
            // this.isLightMode = false;
        }

        public void Run(string[] args)
        {
            using (var mutex = new Mutex(true, "pso2lealauncher-v2", out var isNew))
            {
                if (isNew)
                {
                    try
                    {
                        using (var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "pso2lealauncher-v2-waiter"))
                        {
                            var app = new App();
                            var registered = ThreadPool.RegisterWaitForSingleObject(waitHandle, this.OnNextInstanceStarted, app, -1, false);
                            try
                            {
                                if (Debugger.IsAttached)
                                {
                                    Environment.ExitCode = app.Run();
                                }
                                else
                                {
                                    try
                                    {
                                        Environment.ExitCode = app.Run();
                                    }
                                    catch (Exception ex)
                                    {
                                        using (var sw = new StreamWriter(Path.Combine(RuntimeValues.RootDirectory, "unhandled_error_wpf2.txt"), true, System.Text.Encoding.UTF8))
                                        {
                                            sw.WriteLine();
                                            sw.WriteLine();
                                            sw.WriteLine(ex.ToString());
                                            sw.Flush();
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                registered.Unregister(null);
                            }
                        }
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
                else
                {
                    // There's already one in Game Updater. but this second layer is for safety purpose.
                    if (EventWaitHandle.TryOpenExisting("pso2lealauncher-v2-waiter", out var waitHandle))
                    {
                        using (waitHandle)
                        {
                            waitHandle.Set();
                        }
                    }
                }
            }
        }

        private void OnNextInstanceStarted(object? sender, bool timedOut)
        {
            if (sender is App app)
            {
                app.Dispatcher.InvokeAsync(delegate
                {
                    var modal = app.GetModalOrNull();
                    if (modal != null)
                    {
                        if (modal.WindowState == System.Windows.WindowState.Minimized)
                        {
                            System.Windows.SystemCommands.RestoreWindow(modal);
                        }
                        modal.Activate();
                    }
                    else
                    {
                        if (app.MainWindow is Windows.MainMenuWindow window)
                        {
                            if (window.IsMinimizedToTray)
                            {
                                window.IsMinimizedToTray = false;
                            }
                            else if (window.WindowState == System.Windows.WindowState.Minimized)
                            {
                                System.Windows.SystemCommands.RestoreWindow(window);
                            }
                            window.Activate();
                        }
                    }
                });
            }
        }
    }

    // Unused
    /*
    public class GameLauncher2 : WindowsFormsApplicationBase, IWPFApp
    {
        private App _app;

        public GameLauncher2() : base(AuthenticationMode.Windows)
        {
            // var adminClient = new Leayal.PSO2Launcher.AdminProcess.AdminClient();
            // this.isLightMode = false;
            this.IsSingleInstance = true;
            this.EnableVisualStyles = true;
            this.ShutdownStyle = ShutdownMode.AfterMainFormCloses;
        }

        protected override bool OnStartup(StartupEventArgs eventArgs)
        {
            this._app = new App();
            Environment.ExitCode = this._app.Run();
            return false;
        }

        protected override bool OnUnhandledException(Microsoft.VisualBasic.ApplicationServices.UnhandledExceptionEventArgs e)
        {
            using (var sw = new StreamWriter(Path.Combine(RuntimeValues.RootDirectory, "unhandled_error_wpf2.txt"), true, System.Text.Encoding.UTF8))
            {
                sw.WriteLine();
                sw.WriteLine();
                sw.WriteLine(e.Exception.ToString());
                sw.Flush();
            }
            return base.OnUnhandledException(e);
        }
    }
    */
}

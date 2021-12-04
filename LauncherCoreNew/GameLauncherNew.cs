using Leayal.SharedInterfaces;
using System;
using System.Diagnostics;
using System.IO;
using Leayal.PSO2Launcher.Interfaces;
using System.Windows.Forms;

namespace Leayal.PSO2Launcher.Core
{
#nullable enable
    public class GameLauncherNew : LauncherProgram
    {
        private readonly App _app;

        public GameLauncherNew(int bootstrapversion) : base(true, true)
        {
            // var adminClient = new Leayal.PSO2Launcher.AdminProcess.AdminClient();
            // this.isLightMode = false;
            this._app = new App(bootstrapversion);
        }

        protected override int OnExit()
        {
            this._app.Shutdown();
            return Environment.ExitCode;
        }

        protected override void OnFirstInstance(string[] args)
        {
            if (Debugger.IsAttached)
            {
                Environment.ExitCode = this._app.Run();
            }
            else
            {
                try
                {
                    Environment.ExitCode = this._app.Run();
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

        private void App_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (sender is App app)
            {
                app.LoadCompleted -= this.App_LoadCompleted;
            }
        }

        protected override void OnSubsequentInstance(string[] args)
        {
            if (this._app is App app)
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
#nullable restore
}

using Leayal.SharedInterfaces.Communication;
using System;
using ControlzEx.Theming;
using System.Runtime.Loader;
using System.Reflection;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using System.IO;
using Leayal.SharedInterfaces;
using Microsoft.Win32.SafeHandles;
using System.Security;
using System.Diagnostics;

namespace Leayal.PSO2Launcher.Core
{
    public class GameLauncher : IWPFApp
    {
        private readonly App _app;
        private bool isLightMode;
        
        public GameLauncher()
        {
            // var adminClient = new Leayal.PSO2Launcher.AdminProcess.AdminClient();
            // this.isLightMode = false;
            this._app = new App();
            ThemeManager.Current.SyncTheme(ThemeSyncMode.SyncWithAppMode);
            var themeInfo = ThemeManager.Current.DetectTheme(this._app);
            if (themeInfo == null)
            {
                // In case the assembly is isolated.
                // Currently enforce setting. Will do something about save/load later.
                ThemeManager.Current.ChangeTheme(this._app, ThemeManager.BaseColorDark, "Red");
                this.isLightMode = false;
            }
            else
            {
                this.isLightMode = ((themeInfo.BaseColorScheme) == ThemeManager.BaseColorLight);
            }
            // ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.DoNotSync;
        }

        public void Run(string[] args)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this._app.Run();
            }
            else
            {
                try
                {
                    this._app.Run();
                }
                catch (Exception ex)
                {
                    using (var sw = new StreamWriter(Path.Combine(RuntimeValues.RootDirectory, "unhandled_error_wpf.txt"), true, System.Text.Encoding.UTF8))
                    {
                        sw.WriteLine();
                        sw.WriteLine();
                        sw.WriteLine(ex.ToString());
                        sw.Flush();
                    }
                }
            }
        }

        public void ChangeThemeMode(bool isLightMode)
        {
            if (this.isLightMode != isLightMode)
            {
                this.isLightMode = isLightMode;
                if (isLightMode)
                {
                    ThemeManager.Current.ChangeTheme(this._app, ThemeManager.BaseColorLight, "Blue");
                }
                else
                {
                    ThemeManager.Current.ChangeTheme(this._app, ThemeManager.BaseColorDark, "Red");
                }
            }
        }
    }
}

using Leayal.SharedInterfaces.Communication;
using System;
using ControlzEx.Theming;
using System.Runtime.Loader;
using System.Reflection;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using System.IO;
using Leayal.SharedInterfaces;
using Microsoft.Win32.SafeHandles;

namespace Leayal.PSO2Launcher.Core
{
    public class GameLauncher : IWPFApp
    {
        private readonly App _app;
        private bool isLightMode;
        
        public GameLauncher()
        {
            SafeHandleZeroOrMinusOneIsInvalid lib = null;
            try
            {
                lib = FileCheckHashCache.InitSQLite3(Path.GetFullPath(Path.Combine("runtimes", "win-x64", "native", "e_sqlcipher.dll"), RuntimeValues.RootDirectory));
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
            finally
            {
                lib?.Close();
            }
        }

        public void Run(string[] args)
        {
            this._app.Run();
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

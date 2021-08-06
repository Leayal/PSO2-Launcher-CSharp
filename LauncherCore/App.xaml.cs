using Leayal.SharedInterfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using ControlzEx.Theming;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace Leayal.PSO2Launcher.Core
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public new static App Current => ((App)(Application.Current));

        private bool isLightMode;
        private readonly UserPreferenceChangingEventHandler preferenceChangingEventHandler;

        public bool IsLightMode => this.isLightMode;

        public App() : base()
        {
            this.preferenceChangingEventHandler = new UserPreferenceChangingEventHandler(this.SystemEvents_UserPreferenceChanging);
            this.InitializeComponent();
            this.ManuallySyncTheme();
            SystemEvents.UserPreferenceChanging += this.preferenceChangingEventHandler;
            // ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.DoNotSync;
        }

        private void SystemEvents_UserPreferenceChanging(object sender, UserPreferenceChangingEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General || e.Category == UserPreferenceCategory.Color || e.Category == UserPreferenceCategory.VisualStyle)
            {
                this.ManuallySyncTheme();
            }
        }

        public void ManuallySyncTheme()
        {
            bool hasChange = false;
            var thememgr = ThemeManager.Current;
            thememgr.SyncTheme(ThemeSyncMode.SyncWithAppMode | ThemeSyncMode.SyncWithAccent);
            var themeInfo = thememgr.DetectTheme(this);
            if (themeInfo == null)
            {
                // In case the assembly is isolated.
                // Currently enforce setting. Will do something about save/load later.
                thememgr.ChangeTheme(this, ThemeManager.BaseColorDark, "Red");
                var mode = false;
                hasChange = (this.isLightMode != mode);
                if (hasChange)
                {
                    this.isLightMode = mode;
                }
            }
            else
            {
                var mode = ((themeInfo.BaseColorScheme) == ThemeManager.BaseColorLight);
                hasChange = (this.isLightMode != mode);
                if (hasChange)
                {
                    this.isLightMode = mode;
                    if (mode)
                    {
                        thememgr.ChangeThemeColorScheme(this, "Blue");
                    }
                    else
                    {
                        thememgr.ChangeThemeColorScheme(this, "Red");
                    }
                }
            }

            if (hasChange)
            {
                foreach (var window in this.Windows)
                {
                    if (window is Windows.MetroWindowEx windowex)
                    {
                        windowex.RefreshTheme();
                    }
                }
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            this.MainWindow = new Windows.MainMenuWindow();
            this.MainWindow.Show();
        }

        public void ChangeThemeMode(bool isLightMode)
        {
            if (this.isLightMode != isLightMode)
            {
                this.isLightMode = isLightMode;
                if (isLightMode)
                {
                    ThemeManager.Current.ChangeTheme(this, ThemeManager.BaseColorLight, "Blue");
                }
                else
                {
                    ThemeManager.Current.ChangeTheme(this, ThemeManager.BaseColorDark, "Red");
                }
            }
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var str = e.Exception.ToString();
            using (var sw = new StreamWriter(Path.Combine(RuntimeValues.RootDirectory, "unhandled_dispatcher_error_wpf.txt"), true, System.Text.Encoding.UTF8))
            {
                sw.WriteLine();
                sw.WriteLine();
                sw.WriteLine(str);
                sw.Flush();
            }
            var window = this.MainWindow;
            if (window != null)
            {
                MessageBox.Show(window, str, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(str, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            e.Handled = true;
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            SystemEvents.UserPreferenceChanging -= this.preferenceChangingEventHandler;
        }
    }
}

using Leayal.SharedInterfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using ControlzEx.Theming;
using System.Threading.Tasks;
using System.Windows;

namespace Leayal.PSO2Launcher.Core
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public new static App Current => ((App)(Application.Current));

        private bool isLightMode;

        public bool IsLightMode => this.isLightMode;

        public App() : base()
        {
            this.InitializeComponent();
            ThemeManager.Current.SyncTheme(ThemeSyncMode.SyncWithAppMode);
            var themeInfo = ThemeManager.Current.DetectTheme(this);
            if (themeInfo == null)
            {
                // In case the assembly is isolated.
                // Currently enforce setting. Will do something about save/load later.
                ThemeManager.Current.ChangeTheme(this, ThemeManager.BaseColorDark, "Red");
                this.isLightMode = false;
            }
            else
            {
                this.isLightMode = ((themeInfo.BaseColorScheme) == ThemeManager.BaseColorLight);
            }
            // ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.DoNotSync;
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
    }
}

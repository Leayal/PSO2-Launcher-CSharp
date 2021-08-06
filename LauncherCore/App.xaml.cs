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

        public bool IsLightMode => this.isLightMode;

        public App() : base()
        {
            this.InitializeComponent();

            var thememgr = ThemeManager.Current;
            thememgr.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            thememgr.ThemeChanged += this.Thememgr_ThemeChanged;
            thememgr.SyncTheme();
        }

        private void Thememgr_ThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            if (sender is ThemeManager thememgr)
            {
                var mode = string.Equals(e.NewTheme.BaseColorScheme, ThemeManager.BaseColorLight, StringComparison.OrdinalIgnoreCase);
                if (this.isLightMode != mode)
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
                    foreach (var window in this.Windows)
                    {
                        if (window is Windows.MetroWindowEx windowex)
                        {
                            windowex.RefreshTheme();
                        }
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
            ThemeManager.Current.ThemeChanged -= this.Thememgr_ThemeChanged;
        }
    }
}

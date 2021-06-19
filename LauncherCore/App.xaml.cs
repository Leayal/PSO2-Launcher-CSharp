using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Leayal.PSO2Launcher.Core
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App() : base()
        {
            this.InitializeComponent();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            this.MainWindow = new Windows.MainMenuWindow();
            this.MainWindow.Show();
        }
    }
}

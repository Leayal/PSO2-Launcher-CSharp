using Leayal.PSO2.Installer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class MainMenuWindow
    {
        private void TabMainMenu_ButtonPSO2Troubleshooting_Clicked(object sender, RoutedEventArgs e)
        {
            var dialog = new PSO2TroubleshootingWindow(this.config_main)
            {
                Owner = this
            };
            if (dialog.ShowDialog() == true)
            {

            }
        }
    }
}

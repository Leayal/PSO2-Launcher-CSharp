using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Leayal.PSO2.Installer;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class MainMenuWindow
    {
        private void ButtonInstallPSO2_Clicked(object sender, RoutedEventArgs e)
        {
            var asadad = Requirements.HasDirectX11();
        }
    }
}

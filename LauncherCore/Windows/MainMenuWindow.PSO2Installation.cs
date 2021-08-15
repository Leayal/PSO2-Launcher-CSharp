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
            var hasDx11 = Requirements.HasDirectX11();
            var hasVC14_x86 = Requirements.GetVC14RedistVersion(false);
            var hasVC14_x64 = Requirements.GetVC14RedistVersion(true);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Leayal.PSO2.Installer;
using Leayal.PSO2Launcher.Core.UIElements;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class MainMenuWindow
    {
        private async void TabMainMenu_ButtonInstallPSO2_Clicked(object sender, RoutedEventArgs? e)
        {
            if (sender is TabMainMenu tab)
            {
                tab.ButtonInstallPSO2Clicked -= this.TabMainMenu_ButtonInstallPSO2_Clicked;
                try
                {
                    var dialog = new PSO2DeploymentWindow(this.pso2HttpClient);
                    var installation_result = dialog.ShowCustomDialog(this);
                    if (installation_result.HasValue)
                    {
                        this.config_main.PSO2_BIN = dialog.PSO2BinDirectory;
                        this.config_main.DownloadSelection = dialog.GameClientDownloadSelection;
                        this.config_main.DownloaderProfile = dialog.DownloaderProfileSelection;
                        this.config_main.DownloaderProfileClassic = dialog.DownloaderProfileClassicSelection;
                        this.config_main.Save();
                        this.RefreshGameUpdaterOptions();
                        if (installation_result.Value)
                        {
                            await StartGameClientUpdate(false, false);
                        }
                    }
                }
                finally
                {
                    tab.ButtonInstallPSO2Clicked += this.TabMainMenu_ButtonInstallPSO2_Clicked;
                }
            }
        }
    }
}

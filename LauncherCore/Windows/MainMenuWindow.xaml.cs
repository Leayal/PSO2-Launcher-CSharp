using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using System.Threading;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainMenuWindow : MetroWindowEx
    {
        private readonly PSO2HttpClient pso2HttpClient;

        public MainMenuWindow()
        {
            this.pso2HttpClient = new PSO2HttpClient();
            InitializeComponent();
        }

        private async void ButtonTest_Click(object sender, RoutedEventArgs e)
        {
            // Run test
            await using (var pso2Updater = new GameClientUpdater(@"G:\Phantasy Star Online 2\pso2_bin", @"G:\Phantasy Star Online 2\LeaCheckCacheV3.dat", this.pso2HttpClient))
            {
                pso2Updater.ProgressReport += this.PSO2Updater_ProgressReport;
                pso2Updater.ProgressBegin += this.PSO2Updater_ProgressBegin;
                pso2Updater.ProgressEnd += this.PSO2Updater_ProgressEnd;

                pso2Updater.OperationCompleted += this.PSO2Updater_OperationCompleted;

                CancellationToken cancelToken = CancellationToken.None;

                if ((await pso2Updater.CheckForPSO2Updates(cancelToken)) == true)
                {
                    var t_fileCheck = pso2Updater.ScanForFilesNeedToDownload(GameClientSelection.NGS_AND_CLASSIC, FileScanFlags.Balanced, cancelToken);
                    var t_downloading = pso2Updater.StartDownloadFiles(cancelToken);
                }
            }
        }

        private void PSO2Updater_OperationCompleted(GameClientUpdater sender, long howManyFileInTotal, long howManyFileFailure)
        {
            throw new NotImplementedException();
        }

        private void PSO2Updater_ProgressEnd(PatchListItem file, in bool isSuccess)
        {
            throw new NotImplementedException();
        }

        private void PSO2Updater_ProgressBegin(PatchListItem file, in long totalProgressValue)
        {
            throw new NotImplementedException();
        }

        private void PSO2Updater_ProgressReport(PatchListItem file, in long currentProgressValue)
        {
            throw new NotImplementedException();
        }

        #region | WindowsCommandButtons |
        private void WindowsCommandButtons_Close_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SystemCommands.CloseWindow(this);
        }

        private void WindowsCommandButtons_Maximize_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SystemCommands.MaximizeWindow(this);
        }

        private void WindowsCommandButtons_Restore_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SystemCommands.RestoreWindow(this);
        }

        private void WindowsCommandButtons_Minimize_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SystemCommands.MinimizeWindow(this);
        }
        #endregion
    }
}

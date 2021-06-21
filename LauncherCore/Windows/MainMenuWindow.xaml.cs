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

        private async void ButtonTest_Click(object sender, RoutedEventArgs e)
        {
            var rootInfo = await pso2HttpClient.GetPatchRootInfoAsync(CancellationToken.None);

            // Save HTTP requests. Re-use root.
            var str = await pso2HttpClient.GetPatchVersionAsync(rootInfo, CancellationToken.None);
            if (MessageBox.Show(this, str, "It's alive", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                var patchlist_reboot = await pso2HttpClient.GetPatchListNGSFullAsync(rootInfo, CancellationToken.None);
                var sb = new StringBuilder();
                if (patchlist_reboot.TryGetByFilename("pso2.exe", out var patchItemInfo))
                {
                    sb.AppendLine($"Main exe: {patchItemInfo.Filename} | {patchItemInfo.FileSize} | {patchItemInfo.MD5}");
                }
                sb.AppendLine($"Preview files:");
                foreach (var item in System.Linq.Enumerable.Take(patchlist_reboot, 10))
                {
                    if (item.PatchOrBase.HasValue)
                    {
                        char charPM = item.PatchOrBase.Value ? 'p' : 'm';
                        sb.AppendLine($"{item.Filename} | {item.FileSize} | {item.MD5} | {charPM}");
                    }
                    else
                    {
                        sb.AppendLine($"{item.Filename} | {item.FileSize} | {item.MD5}");
                    }
                }

                MessageBox.Show(this, sb.ToString(), "Preview Data", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}

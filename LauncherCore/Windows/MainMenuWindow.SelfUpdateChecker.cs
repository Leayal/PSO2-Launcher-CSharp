using System;
using System.Collections.Generic;
using System.IO;
using Leayal.PSO2Launcher.Core.Classes;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Windows;
using System.Windows.Documents;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class MainMenuWindow
    {
        private async Task OnSelfUpdateFound(BackgroundSelfUpdateChecker sender)
        {
            sender.Stop();
            await this.CreateNewParagraphInLog(writer =>
            {
                writer.Write("[Launcher Updater] An update for this PSO2 Launcher has been found.");
                this.SelfUpdateNotification.Visibility = Visibility.Visible;
            });
            /*
            if (this.SelfUpdateNotification.CheckAccess())
            {
                this.SelfUpdateNotification.Visibility = Visibility.Visible;
            }
            else
            {
                await this.SelfUpdateNotification.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.SelfUpdateNotification.Visibility = Visibility.Visible;
                }));
            }
            */
        }

        private void SelfUpdateNotificationHyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink link)
            {
                if (link.NavigateUri != null && link.NavigateUri.IsAbsoluteUri)
                {
                    if (string.Equals(link.NavigateUri.AbsoluteUri, "pso2lealauncher://selfupdatechecker/confirm", StringComparison.OrdinalIgnoreCase))
                    {
                        this.Close();
                        System.Windows.Forms.Application.Restart();
                        App.Current.Shutdown();
                    }
                    else if (string.Equals(link.NavigateUri.AbsoluteUri, "pso2lealauncher://selfupdatechecker/ignore", StringComparison.OrdinalIgnoreCase))
                    {
                        this.SelfUpdateNotification.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
    }
}

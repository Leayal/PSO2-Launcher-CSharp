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
        private async Task OnSelfUpdateFound(BackgroundSelfUpdateChecker sender, IReadOnlyList<string> files)
        {
            sender.Stop();
            await this.CreateNewParagraphInLog(writer =>
            {
                writer.Write("[Launcher Updater] An update for this PSO2 Launcher has been found. These files need to be updated: ");
                if (files != null)
                {
                    if (files.Count == 1)
                    {
                        writer.Write(files[0]);
                    }
                    else
                    {
                        writer.Write(files[0]);
                        for (int i = 1; i < files.Count; i++)
                        {
                            writer.Write(", ");
                            writer.Write(files[i]);
                        }
                    }
                }
                this.SelfUpdateNotification.Visibility = Visibility.Visible;
            });
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
                        App.Current.Shutdown();
                        System.Windows.Forms.Application.Restart();
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

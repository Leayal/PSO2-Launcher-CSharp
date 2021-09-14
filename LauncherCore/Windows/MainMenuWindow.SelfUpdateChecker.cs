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
            this.CreateNewParagraphInLog(writer =>
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
                if (this.trayIcon.IsValueCreated)
                {
                    var _trayicon = this.trayIcon.Value;
                    foreach (var item in _trayicon.ContextMenuStrip.Items)
                    {
                        if (item is System.Windows.Forms.ToolStripMenuItem menuitem)
                        {
                            if (menuitem.Tag is Uri uri && uri.Equals(StaticResources.Url_ConfirmSelfUpdate))
                            {
                                menuitem.Visible = true;
                            }
                        }
                    }
                }
            });
        }
    }
}

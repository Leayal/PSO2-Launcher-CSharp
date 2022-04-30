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
        private const string ConsoleLogUpdateNotification = "[Launcher Updater] An update for this PSO2 Launcher has been found.";
        private const string ConsoleLogUpdateNotificationWithFile = $"{ConsoleLogUpdateNotification} These files need to be updated: ";
        public static readonly DependencyProperty IsUpdateNotificationVisibleProperty = DependencyProperty.Register("IsUpdateNotificationVisible", typeof(bool), typeof(MainMenuWindow), new PropertyMetadata(false, (obj, e) =>
        {
            if (obj is MainMenuWindow window && e.NewValue is bool b)
            {
                if (window.trayIcon.IsValueCreated)
                {
                    var _trayicon = window.trayIcon.Value;
                    foreach (var item in _trayicon.ContextMenuStrip.Items)
                    {
                        if (item is System.Windows.Forms.ToolStripMenuItem menuitem)
                        {
                            if (menuitem.Tag is Uri uri && uri.Equals(StaticResources.Url_ConfirmSelfUpdate))
                            {
                                menuitem.Visible = b;
                            }
                        }
                    }
                }
            }
        }));
        public bool IsUpdateNotificationVisible
        {
            get => (bool)this.GetValue(IsUpdateNotificationVisibleProperty);
            set => this.SetValue(IsUpdateNotificationVisibleProperty, value);
        }

        private void InternalShowUpdateNotificationOnUi(string message)
        {
            this.CreateNewParagraphInLog(message);
            this.IsUpdateNotificationVisible = true;
            if (this.config_main.LauncherCheckForSelfUpdatesNotifyIfInTray && this.IsMinimizedToTray)
            {
                var ico = this.trayIcon.Value;
                ico.ShowBalloonTip(5000, string.Empty, message, System.Windows.Forms.ToolTipIcon.Info);
            }
        }

        private void OnSelfUpdateFound(BackgroundSelfUpdateChecker sender, IReadOnlyList<string> files)
        {
            sender.Stop();
            // var sb = new System.Text.StringBuilder()
            int len = 0;
            if (files != null)
            {
                if (files.Count == 1)
                {
                    len = files[0].Length;
                }
                else
                {
                    len = files[0].Length;
                    for (int i = 1; i < files.Count; i++)
                    {
                        len += (files[i].Length + 2);
                    }
                }
            }

            if (len == 0)
            {
                this.Dispatcher.BeginInvoke(this.InternalShowUpdateNotificationOnUi, new object[] { ConsoleLogUpdateNotification });
            }
            else
            {
                this.Dispatcher.BeginInvoke(this.InternalShowUpdateNotificationOnUi, new object[] { string.Create(ConsoleLogUpdateNotificationWithFile.Length + len, files, (c, list) =>
                {
                    ConsoleLogUpdateNotificationWithFile.CopyTo(c);
                    var workset = c.Slice(ConsoleLogUpdateNotificationWithFile.Length);
                    if (list.Count == 1)
                    {
                        list[0].CopyTo(workset);
                    }
                    else
                    {
                        var str = list[0];
                        str.CopyTo(workset);
                        workset = workset.Slice(str.Length);
                        for (int i = 1; i < list.Count; i++)
                        {
                            workset[0] = ',';
                            workset[1] = ' ';
                            workset = workset.Slice(2);
                            str = list[i];
                            str.CopyTo(workset);
                            workset = workset.Slice(str.Length);
                        }
                    }
                }) });
            }
        }
    }
}

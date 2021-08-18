using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class MainMenuWindow
    {
        public static readonly DependencyProperty IsMinimizedToTrayProperty = DependencyProperty.Register("IsMinimizedToTray", typeof(bool), typeof(MainMenuWindow), new UIPropertyMetadata(false, (obj, e) =>
        {
            if (obj is MainMenuWindow window)
            {
                bool b = (bool)e.NewValue;
                if (b)
                {
                    var ico = window.trayIcon.Value;
                    ico.Visible = true;
                    window.Hide();
                    window.ShowInTaskbar = false;
                }
                else
                {
                    window.ShowInTaskbar = true;
                    window.Show();
                    var ico = window.trayIcon.Value;
                    ico.Visible = false;
                }
            }
        }));
        public bool IsMinimizedToTray
        {
            get => (bool)this.GetValue(IsMinimizedToTrayProperty);
            set => this.SetValue(IsMinimizedToTrayProperty, value);
        }

        private NotifyIcon CreateNotifyIcon()
        {
            var ico = new NotifyIcon()
            {
                Visible = false,
                Icon = BootstrapResources.ExecutableIcon,
                Text = "PSO2Launcher"
            };
            ico.ShowBalloonTip(10000, "PSO2Launcher", "Double-click this icon in order to show the launcher again.", ToolTipIcon.Info);
            ico.DoubleClick += this.Ico_DoubleClick;
            var ico_contextmenu = new ContextMenuStrip();

            var typicalMenu = new ToolStripMenuItem("Quick menu");

            var typical_startGame = new ToolStripMenuItem("Start game");
            typical_startGame.Click += this.Typical_startGame_Click;
            var vals = Enum.GetValues<GameStartStyle>();
            for (int i = 0; i < vals.Length; i++)
            {
                var val = vals[i];
                string displayname;
                if (!EnumVisibleInOptionAttribute.TryGetIsVisible(val, out var isVisible) || isVisible)
                {
                    if (EnumDisplayNameAttribute.TryGetDisplayName(val, out var name))
                    {
                        displayname = name;
                    }
                    else
                    {
                        displayname = val.ToString();
                    }

                    var menuitem = new ToolStripMenuItem() { Text = displayname, Tag = val };
                    menuitem.Click += this.Typical_startGame_SubItemsClick;
                    typical_startGame.DropDownItems.Add(menuitem);
                }
            }

            var typical_checkforPSO2Updates = new ToolStripMenuItem("Check for PSO2 Updates");
            typical_checkforPSO2Updates.Click += this.Typical_checkforPSO2Updates_Click;

            typicalMenu.DropDownItems.Add(typical_checkforPSO2Updates);

            var menuitem_showLauncher = new ToolStripMenuItem("Show launcher") { Font = new System.Drawing.Font(typicalMenu.Font, System.Drawing.FontStyle.Bold) };
            menuitem_showLauncher.Tag = ico;
            menuitem_showLauncher.Click += this.Ico_DoubleClick;

            var menuitem_exit = new ToolStripMenuItem("Exit");
            menuitem_exit.Click += this.Menuitem_exit_Click;

            var tab = this.TabMainMenu;
            RoutedEventHandler _IsSelectedOrGameStartEnabledChanged = (_tab, ev) =>
            {
                bool isenabled = tab.IsSelected && tab.GameStartEnabled;
                typical_startGame.Enabled = isenabled;
                typical_checkforPSO2Updates.Enabled = isenabled;
                foreach (var item in typical_startGame.DropDownItems)
                {
                    if (item is ToolStripMenuItem dropitem)
                    {
                        dropitem.Enabled = isenabled;
                    }
                }
            };

            ico_contextmenu.Opening += (sender, e) =>
            {
                if (!e.Cancel)
                {
                    _IsSelectedOrGameStartEnabledChanged.Invoke(tab, null);
                    tab.GameStartEnabledChanged += _IsSelectedOrGameStartEnabledChanged;
                    tab.IsSelectedChanged += _IsSelectedOrGameStartEnabledChanged;
                }
            };
            ico_contextmenu.Closing += (sender, e) =>
            {
                if (!e.Cancel)
                {
                    tab.GameStartEnabledChanged -= _IsSelectedOrGameStartEnabledChanged;
                    tab.IsSelectedChanged -= _IsSelectedOrGameStartEnabledChanged;
                }
            };

            var hasupdatenotificationalready = (this.SelfUpdateNotification.Visibility == Visibility.Visible);
            var selfupdate_separator = new ToolStripSeparator()
            {
                Visible = hasupdatenotificationalready
            };
            var performRestartToSelfUpdate = new ToolStripMenuItem("Restart launcher to update launcher")
            {
                Visible = hasupdatenotificationalready
            };
            performRestartToSelfUpdate.Tag = new Uri("pso2lealauncher://selfupdatechecker/confirm");
            performRestartToSelfUpdate.VisibleChanged += (sender, e) =>
            {
                selfupdate_separator.Visible = performRestartToSelfUpdate.Visible;
            };
            performRestartToSelfUpdate.Click += this.PerformRestartToSelfUpdate_Click;

            ico_contextmenu.Items.AddRange(new ToolStripItem[] {
                menuitem_showLauncher,
                new ToolStripSeparator(),
                typical_startGame,
                typicalMenu,
                selfupdate_separator, // This is already a separator
                performRestartToSelfUpdate,
                new ToolStripSeparator(),
                menuitem_exit,
            });
            ico.ContextMenuStrip = ico_contextmenu;
            return ico;
        }

        private void PerformRestartToSelfUpdate_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuitem && menuitem.Tag is Uri uri && uri.IsAbsoluteUri)
            {
                this.ExecuteCommandUrl(uri);
            }
        }

        private void Typical_startGame_Click(object sender, EventArgs e)
        {
            if (this.TabMainMenu.IsSelected && this.TabMainMenu.GameStartEnabled)
            {
                this.TabMainMenu.TriggerButtonGameStart();
            }
        }

        private void Typical_checkforPSO2Updates_Click(object sender, EventArgs e)
        {
            // Unnecessary 'if' but it doesn't hurt to use it.
            if (this.TabMainMenu.IsSelected && this.TabMainMenu.GameStartEnabled)
            {
                this.TabMainMenu.TriggerButtonCheckForPSO2Update();
            }
        }

        private void Typical_startGame_SubItemsClick(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item && item.Tag is GameStartStyle style)
            {
                // Unnecessary 'if' but it doesn't hurt to use it.
                if (this.TabMainMenu.IsSelected && this.TabMainMenu.GameStartEnabled)
                {
                    this.TabMainMenu.RequestGameStart(style);
                }
            }
        }

        private void Menuitem_exit_Click(object sender, EventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void Ico_DoubleClick(object sender, EventArgs e)
        {
            this.IsMinimizedToTray = false;
        }

        private void WindowsCommandButtons_MinimizeToTray_Click(object sender, RoutedEventArgs e)
        {
            this.IsMinimizedToTray = true;
        }
    }
}

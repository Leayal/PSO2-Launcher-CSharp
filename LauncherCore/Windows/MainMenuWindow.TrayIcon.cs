﻿using Leayal.PSO2Launcher.Helper;
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
                    // this.WindowState = WindowState.Minimized;
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
            var typical_startGame_NoLogin = new ToolStripMenuItem("Without login");
            typical_startGame_NoLogin.Click += this.Typical_startGame_NoLogin_Click;
            var typical_startGame_Login = new ToolStripMenuItem("With login");
            typical_startGame_Login.Click += this.Typical_startGame_Login_Click;
            typical_startGame.DropDownItems.AddRange(new ToolStripItem[] { typical_startGame_NoLogin, typical_startGame_Login });

            var typical_checkforPSO2Updates = new ToolStripMenuItem("Check for PSO2 Updates");
            typical_checkforPSO2Updates.Click += this.Typical_checkforPSO2Updates_Click;

            typicalMenu.DropDownItems.AddRange(new ToolStripItem[] { typical_startGame, typical_checkforPSO2Updates });

            var menuitem_showLauncher = new ToolStripMenuItem("Show launcher") { Font = new System.Drawing.Font(typicalMenu.Font, System.Drawing.FontStyle.Bold) };
            menuitem_showLauncher.Tag = ico;
            menuitem_showLauncher.Click += this.Ico_DoubleClick;

            var menuitem_exit = new ToolStripMenuItem("Exit");
            menuitem_exit.Click += this.Menuitem_exit_Click;

            var tab = this.TabMainMenu;
            RoutedEventHandler _IsSelectedOrGameStartEnabledChanged = (_tab, ev) =>
            {
                bool istabselected = tab.IsSelected;
                typical_startGame.Enabled = istabselected && tab.GameStartEnabled;
                typical_checkforPSO2Updates.Enabled = istabselected && tab.GameStartEnabled;
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

            ico_contextmenu.Items.AddRange(new ToolStripItem[] {
                menuitem_showLauncher,
                new ToolStripSeparator(),
                typicalMenu,
                new ToolStripSeparator(),
                menuitem_exit,
            });
            ico.ContextMenuStrip = ico_contextmenu;
            return ico;
        }

        private void Typical_checkforPSO2Updates_Click(object sender, EventArgs e)
        {
            // Unnecessary 'if' but it doesn't hurt to use it.
            if (this.TabMainMenu.IsSelected && this.TabMainMenu.GameStartEnabled)
            {
                this.TabMainMenu.TriggerButtonCheckForPSO2Update();
            }
        }

        private void Typical_startGame_NoLogin_Click(object sender, EventArgs e)
        {
            // Unnecessary 'if' but it doesn't hurt to use it.
            if (this.TabMainMenu.IsSelected && this.TabMainMenu.GameStartEnabled)
            {
                this.TabMainMenu.TriggerButtonGameStart();
            }
        }

        private void Typical_startGame_Login_Click(object sender, EventArgs e)
        {
            // Unnecessary 'if' but it doesn't hurt to use it.
            if (this.TabMainMenu.IsSelected && this.TabMainMenu.GameStartEnabled)
            {
                this.TabMainMenu.TriggerMenuItemLoginAndPlay();
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

using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Helper;
using Leayal.SharedInterfaces;
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
        public static readonly DependencyProperty IsMinimizedToTrayProperty = DependencyProperty.Register("IsMinimizedToTray", typeof(bool), typeof(MainMenuWindow), new PropertyMetadata(false, (obj, e) =>
        {
            if (obj is MainMenuWindow window)
            {
                bool b = (bool)e.NewValue;
                if (b)
                {
                    if (window.isWebBrowserLoaded && !window.timer_unloadWebBrowser.IsEnabled)
                    {
                        window.timer_unloadWebBrowser.Start();
                    }
                    var ico = window.trayIcon.Value;
                    ico.Visible = true;
                    window.Hide();
                    window.ShowInTaskbar = false;
                }
                else
                {
                    if (window.isWebBrowserLoaded && window.LauncherWebView.IsVisible)
                    {
                        window.timer_unloadWebBrowser.Stop();
                        if (!(window.LauncherWebView.Child is IWebViewCompatControl))
                        {
                            window.LoadLauncherWebView_Click(null, null);
                        }
                    }
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

        private readonly ToolStripMenuItem menuItem_TimeClock = new ToolStripMenuItem() { Text = $"JST: {DateTime.MinValue}", Visible = false, Enabled = false };

        private NotifyIcon CreateNotifyIcon()
        {
            var ico = new NotifyIcon()
            {
                Visible = false,
                Icon = BootstrapResources.ExecutableIcon,
                Text = "PSO2LeaLauncher"
            };
            ico.ShowBalloonTip(10000, "PSO2LeaLauncher", "Double-click this icon in order to show the launcher again.", ToolTipIcon.Info);
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

                    var menuitem = new ToolStripMenuItem(displayname) { Tag = val };
                    menuitem.Click += this.Typical_startGame_SubItemsClick;
                    typical_startGame.DropDownItems.Add(menuitem);
                }
            }

            if (!EnumDisplayNameAttribute.TryGetDisplayName(GameStartStyle.StartWithPSO2Tweaker, out var displayname_launchTweaker))
            {
                displayname_launchTweaker = GameStartStyle.StartWithPSO2Tweaker.ToString();
            }
            var menuitem_launchTweaker = new ToolStripMenuItem(displayname_launchTweaker) { Tag = GameStartStyle.StartWithPSO2Tweaker, Visible = this.TabMainMenu.GameStartWithPSO2TweakerEnabled };
            menuitem_launchTweaker.Click += this.Typical_startGame_SubItemsClick;
            typical_startGame.DropDownItems.Add(menuitem_launchTweaker);

            var forgetSEGALogin = new ToolStripMenuItem("Forget remembered SEGA login");
            forgetSEGALogin.Click += this.MenuItemForgetSEGALogin_Click;
            typical_startGame.DropDownItems.Add(forgetSEGALogin);

            var typical_checkforPSO2Updates = new ToolStripMenuItem("Check for PSO2 Updates");
            typical_checkforPSO2Updates.Click += this.Typical_checkforPSO2Updates_Click;

            var typical_scanMissingDamaged = new ToolStripMenuItem("Scan for missing or damaged files");
            typical_scanMissingDamaged.Click += this.Typical_ScanForMissingDamaged_Click;
            typicalMenu.DropDownItems.AddRange(new[] { typical_checkforPSO2Updates, typical_scanMissingDamaged });

            var toolboxMenu = new ToolStripMenuItem("Toolbox");

            var toolbox_openPSO2AlphaReactorCounter = new ToolStripMenuItem("Open Alpha Reactor && Stellar Seed Counter")
            {
                Tag = StaticResources.Url_Toolbox_AlphaReactorCounter
            };
            toolbox_openPSO2AlphaReactorCounter.Click += this.MenuItem_UrlCommand_Click;
            toolboxMenu.DropDownItems.Add(toolbox_openPSO2AlphaReactorCounter);

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
                typical_scanMissingDamaged.Enabled = isenabled;
                menuitem_launchTweaker.Visible = this.TabMainMenu.GameStartWithPSO2TweakerEnabled;
                foreach (var item in typical_startGame.DropDownItems)
                {
                    if (item is ToolStripMenuItem dropitem)
                    {
                        dropitem.Enabled = isenabled;
                    }
                }
            }, _ForgetSegaLoginStateRefresh = (sender, e) =>
            {
                forgetSEGALogin.Enabled = tab.ForgetLoginInfoEnabled;
            };

            ico_contextmenu.Opening += (sender, e) =>
            {
                if (!e.Cancel)
                {
                    var modal = App.Current.GetModalOrNull();
                    if (modal != null)
                    {
                        e.Cancel = true;
                        if (modal.WindowState == WindowState.Minimized)
                        {
                            SystemCommands.RestoreWindow(modal);
                        }
                        modal.Activate();
                    }
                    else
                    {
                        _IsSelectedOrGameStartEnabledChanged.Invoke(tab, null);
                        _ForgetSegaLoginStateRefresh.Invoke(tab, null);
                        tab.GameStartEnabledChanged += _IsSelectedOrGameStartEnabledChanged;
                        tab.IsSelectedChanged += _IsSelectedOrGameStartEnabledChanged;
                        tab.ForgetLoginInfoEnabledChanged += _ForgetSegaLoginStateRefresh;
                    }
                }
            };
            ico_contextmenu.Closing += (sender, e) =>
            {
                if (!e.Cancel)
                {
                    tab.GameStartEnabledChanged -= _IsSelectedOrGameStartEnabledChanged;
                    tab.IsSelectedChanged -= _IsSelectedOrGameStartEnabledChanged;
                    tab.ForgetLoginInfoEnabledChanged -= _ForgetSegaLoginStateRefresh;
                }
            };

            var hasupdatenotificationalready = this.IsUpdateNotificationVisible;
            var selfupdate_separator = new ToolStripSeparator()
            {
                Visible = hasupdatenotificationalready
            };
            var performRestartToSelfUpdate = new ToolStripMenuItem("Restart launcher to perform update")
            {
                Visible = hasupdatenotificationalready,
                Tag = StaticResources.Url_ConfirmSelfUpdate
            };
            performRestartToSelfUpdate.VisibleChanged += (sender, e) =>
            {
                selfupdate_separator.Visible = performRestartToSelfUpdate.Visible;
            };
            performRestartToSelfUpdate.Click += this.MenuItem_UrlCommand_Click;

            ico_contextmenu.Items.AddRange(new ToolStripItem[] {
                menuitem_showLauncher,
                menuItem_TimeClock,
                new ToolStripSeparator(),
                typical_startGame,
                typicalMenu,
                toolboxMenu,
                selfupdate_separator, // This is already a separator
                performRestartToSelfUpdate,
                new ToolStripSeparator(),
                menuitem_exit,
            });
            ico.ContextMenuStrip = ico_contextmenu;
            return ico;
        }

        private void MenuItemForgetSEGALogin_Click(object sender, EventArgs e)
            => this.ForgetSEGALogin();

        private void MenuItem_UrlCommand_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuitem && menuitem.Tag is Uri uri && uri.IsAbsoluteUri)
            {
                App.Current.ExecuteCommandUrl(uri);
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

        private void Typical_ScanForMissingDamaged_Click(object sender, EventArgs e)
        {
            // Unnecessary 'if' but it doesn't hurt to use it.
            if (this.TabMainMenu.IsSelected && this.TabMainMenu.GameStartEnabled)
            {
                this.TabMainMenu.TriggerScanForMissingOrDamagedFiles(Classes.PSO2.GameClientSelection.Auto);
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
            var modal = App.Current.GetModalOrNull();
            if (modal != null)
            {
                if (modal.WindowState == WindowState.Minimized)
                {
                    SystemCommands.RestoreWindow(modal);
                }
                modal.Activate();
            }
            else
            {
                this.IsMinimizedToTray = false;
            }
        }

        private void WindowsCommandButtons_MinimizeToTray_Click(object sender, RoutedEventArgs e)
        {
            this.IsMinimizedToTray = true;
        }
    }
}

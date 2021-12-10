using Leayal.PSO2Launcher.Core.Classes;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Leayal.PSO2Launcher.Core.UIElements
{
    /// <summary>
    /// Interaction logic for TabMainMenu.xaml
    /// </summary>
    public partial class TabMainMenu : MetroTabItem
    {
        private static readonly Lazy<BitmapSource> ico_alphareactor = new Lazy<BitmapSource>(() => BitmapSourceHelper.FromEmbedResourcePath("Leayal.PSO2Launcher.Core.Resources.ico-item-AlphaReactor.png"));

        public static readonly RoutedEvent ButtonCheckForPSO2UpdateClickedEvent = EventManager.RegisterRoutedEvent("ButtonCheckForPSO2UpdateClicked", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TabMainMenu));
        public static readonly RoutedEvent ButtonManageGameDataClickedEvent = EventManager.RegisterRoutedEvent("ButtonManageGameDataClicked", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TabMainMenu));
        public static readonly RoutedEvent GameStartRequestedEvent = EventManager.RegisterRoutedEvent("GameStartRequested", RoutingStrategy.Direct, typeof(GameStartRequestEventHandler), typeof(TabMainMenu));
        public static readonly RoutedEvent ForgetLoginInfoClickedEvent = EventManager.RegisterRoutedEvent("ForgetLoginInfoClicked", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TabMainMenu));
        public static readonly RoutedEvent ButtonScanFixGameDataClickedEvent = EventManager.RegisterRoutedEvent("ButtonScanFixGameDataClicked", RoutingStrategy.Direct, typeof(ButtonScanFixGameDataClickRoutedEventHander), typeof(TabMainMenu));
        public static readonly RoutedEvent ButtonPSO2GameOptionClickedEvent = EventManager.RegisterRoutedEvent("ButtonPSO2GameOptionClicked", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TabMainMenu));
        public static readonly RoutedEvent ButtonManageGameLauncherBehaviorClickedEvent = EventManager.RegisterRoutedEvent("ButtonManageGameLauncherBehaviorClicked", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TabMainMenu));
        public static readonly RoutedEvent ButtonManageGameLauncherRSSFeedsClickedEvent = EventManager.RegisterRoutedEvent("ButtonManageGameLauncherRSSFeedsClicked", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TabMainMenu));
        public static readonly RoutedEvent ButtonInstallPSO2ClickedEvent = EventManager.RegisterRoutedEvent("ButtonInstallPSO2Clicked", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TabMainMenu));
        public static readonly RoutedEvent ButtonPSO2TroubleshootingClickedEvent = EventManager.RegisterRoutedEvent("ButtonPSO2TroubleshootingClicked", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TabMainMenu));
        public static readonly RoutedEvent ButtonManageLauncherThemingClickedEvent = EventManager.RegisterRoutedEvent("ButtonManageLauncherThemingClicked", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TabMainMenu));

        public static readonly DependencyProperty GameStartEnabledProperty = DependencyProperty.Register("GameStartEnabled", typeof(bool), typeof(TabMainMenu), new UIPropertyMetadata(true, (obj, e) =>
        {
            if (obj is TabMainMenu tab)
            {
                tab.RaiseEvent(new RoutedEventArgs(GameStartEnabledChangedEvent));
            }
        }));
        public static readonly DependencyProperty ForgetLoginInfoEnabledProperty = DependencyProperty.Register("ForgetLoginInfoEnabled", typeof(bool), typeof(TabMainMenu), new UIPropertyMetadata(false, (obj, val)=>
        {
            if (obj is TabMainMenu tab)
            {
                tab.MenuItemForgetSavedLogin.IsEnabled = (bool)(val.NewValue);
                tab.RaiseEvent(new RoutedEventArgs(ForgetLoginInfoEnabledChangedEvent));
            }
        }));
        public static readonly RoutedEvent ForgetLoginInfoEnabledChangedEvent = EventManager.RegisterRoutedEvent("ForgetLoginInfoEnabledChanged", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TabMainMenu));
        public static readonly RoutedEvent DefaultGameStartStyleChangedEvent = EventManager.RegisterRoutedEvent("DefaultGameStartStyleChanged", RoutingStrategy.Direct, typeof(ChangeDefaultGameStartStyleEventHandler), typeof(TabMainMenu));
        public event ChangeDefaultGameStartStyleEventHandler DefaultGameStartStyleChanged
        {
            add => this.AddHandler(DefaultGameStartStyleChangedEvent, value);
            remove => this.RemoveHandler(DefaultGameStartStyleChangedEvent, value);
        }
        public event RoutedEventHandler ForgetLoginInfoEnabledChanged
        {
            add => this.AddHandler(ForgetLoginInfoEnabledChangedEvent, value);
            remove => this.RemoveHandler(ForgetLoginInfoEnabledChangedEvent, value);
        }
        public static readonly DependencyProperty DefaultGameStartStyleProperty = DependencyProperty.Register("DefaultGameStartStyle", typeof(GameStartStyle), typeof(TabMainMenu), new UIPropertyMetadata(GameStartStyle.Default, (obj, e) =>
        {
            if (obj is TabMainMenu tab && e.NewValue is GameStartStyle style)
            {
                foreach (var subItem in tab.MenuItemChangeDefaultGameStartMethod.Items)
                {
                    if (subItem is MenuItem subMenuItem && subMenuItem.Tag is GameStartStyle tagstyle)
                    {
                        if (tagstyle == style)
                        {
                            subMenuItem.IsChecked = true;
                        }
                        else
                        {
                            subMenuItem.IsChecked = false;
                        }
                    }
                }
                tab.RaiseEvent(new ChangeDefaultGameStartStyleEventArgs(style, DefaultGameStartStyleChangedEvent));
            }
        }));
        public GameStartStyle DefaultGameStartStyle
        {
            get => (GameStartStyle)this.GetValue(DefaultGameStartStyleProperty);
            set => this.SetValue(DefaultGameStartStyleProperty, value);
        }
        public static readonly RoutedEvent GameStartEnabledChangedEvent = EventManager.RegisterRoutedEvent("GameStartEnabledChanged", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TabMainMenu));
        public event RoutedEventHandler GameStartEnabledChanged
        {
            add => this.AddHandler(GameStartEnabledChangedEvent, value);
            remove => this.RemoveHandler(GameStartEnabledChangedEvent, value);
        }
        public static readonly RoutedEvent IsSelectedChangedEvent = EventManager.RegisterRoutedEvent("IsSelectedChanged", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TabMainMenu));
        public event RoutedEventHandler IsSelectedChanged
        {
            add => this.AddHandler(IsSelectedChangedEvent, value);
            remove => this.RemoveHandler(IsSelectedChangedEvent, value);
        }

        public bool GameStartEnabled
        {
            get => (bool)this.GetValue(GameStartEnabledProperty);
            set => this.SetValue(GameStartEnabledProperty, value);
        }

        public bool ForgetLoginInfoEnabled
        {
            get => (bool)this.GetValue(ForgetLoginInfoEnabledProperty);
            set => this.SetValue(ForgetLoginInfoEnabledProperty, value);
        }

        private MenuItem MenuItemForgetSavedLogin;
        private MenuItem MenuItemChangeDefaultGameStartMethod;

        public TabMainMenu()
        {
            InitializeComponent();

            var contextMenu = this.ButtonGameStart.ContextMenu;
            this.MenuItemChangeDefaultGameStartMethod = new MenuItem() { Header = "Change default GameStart method" };

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

                    var menuitem = new MenuItem() { Header = displayname, Tag = val };
                    menuitem.Click += this.MenuItemChangeDefaultGameStartMethod_SubItemsClick;
                    this.MenuItemChangeDefaultGameStartMethod.Items.Add(menuitem);

                    menuitem = new MenuItem() { Header = displayname, Tag = val };
                    menuitem.Click += this.MenuItemSpecificGameStartRequest_Click;
                    contextMenu.Items.Add(menuitem);
                }
            }

            this.MenuItemForgetSavedLogin = new MenuItem() { Header = "Forget remembered SEGA login", IsEnabled = false };
            this.MenuItemForgetSavedLogin.Click += this.MenuItemForgetSavedLogin_Click;
            contextMenu.Items.Add(this.MenuItemForgetSavedLogin);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(this.MenuItemChangeDefaultGameStartMethod);

            this.DefaultGameStartStyle = GameStartStyle.StartWithoutToken;
        }

        private void MenuItemChangeDefaultGameStartMethod_SubItemsClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is GameStartStyle style)
            {
                this.DefaultGameStartStyle = style;
            }
        }

        public event RoutedEventHandler ButtonCheckForPSO2UpdateClicked
        {
            add { this.AddHandler(ButtonCheckForPSO2UpdateClickedEvent, value); }
            remove { this.RemoveHandler(ButtonCheckForPSO2UpdateClickedEvent, value); }
        }
        private void ButtonCheckForPSO2Update_Click(object sender, RoutedEventArgs e) => this.TriggerButtonCheckForPSO2Update();
        public void TriggerButtonCheckForPSO2Update() => this.RaiseEvent(new RoutedEventArgs(ButtonCheckForPSO2UpdateClickedEvent));
        public void TriggerScanForMissingOrDamagedFiles(Classes.PSO2.GameClientSelection selection) => this.RaiseEvent(new ButtonScanFixGameDataClickRoutedEventArgs(selection, ButtonScanFixGameDataClickedEvent));

        public event RoutedEventHandler ButtonManageGameDataClicked
        {
            add { this.AddHandler(ButtonManageGameDataClickedEvent, value); }
            remove { this.RemoveHandler(ButtonManageGameDataClickedEvent, value); }
        }

        private void ButtonManageGameData_Click(object sender, RoutedEventArgs e)
        {
            this.RaiseEvent(new RoutedEventArgs(ButtonManageGameDataClickedEvent));
        }

        private void ButtonGameStart_Click(object sender, RoutedEventArgs e) => this.TriggerButtonGameStart();
        public void TriggerButtonGameStart() => this.RequestGameStart(this.DefaultGameStartStyle);

        public event GameStartRequestEventHandler GameStartRequested
        {
            add { this.AddHandler(GameStartRequestedEvent, value); }
            remove { this.RemoveHandler(GameStartRequestedEvent, value); }
        }
        private void MenuItemSpecificGameStartRequest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is GameStartStyle style)
            {
                this.RequestGameStart(style);
            }
        }
        public void RequestGameStart(GameStartStyle style) => this.RaiseEvent(new GameStartStyleEventArgs(style, GameStartRequestedEvent));

        public event RoutedEventHandler ForgetLoginInfoClicked
        {
            add { this.AddHandler(ForgetLoginInfoClickedEvent, value); }
            remove { this.RemoveHandler(ForgetLoginInfoClickedEvent, value); }
        }
        private void MenuItemForgetSavedLogin_Click(object sender, RoutedEventArgs e)
            => this.RaiseEvent(new RoutedEventArgs(ForgetLoginInfoClickedEvent));

        private void WeirdButtonDropDownAble_Click(object sender, RoutedEventArgs e)
        {
            // It's okay, weird button is still a button
            if (sender is Button btn)
            {
                var ctm = btn.ContextMenu;
                if (ctm != null)
                {
                    ctm.PlacementTarget = sender as UIElement;
                    ctm.IsOpen = true;
                }
            }
        }

        public event ButtonScanFixGameDataClickRoutedEventHander ButtonScanFixGameDataClicked
        {
            add { this.AddHandler(ButtonScanFixGameDataClickedEvent, value); }
            remove { this.RemoveHandler(ButtonScanFixGameDataClickedEvent, value); }
        }
        private void ButtonScanFixGameData_Click(object sender, RoutedEventArgs e)
            => this.RaiseEvent(new ButtonScanFixGameDataClickRoutedEventArgs(Classes.PSO2.GameClientSelection.Auto, ButtonScanFixGameDataClickedEvent));
        private void ButtonScanFixGameData_NGSOnly_Click(object sender, RoutedEventArgs e)
            => this.RaiseEvent(new ButtonScanFixGameDataClickRoutedEventArgs(Classes.PSO2.GameClientSelection.NGS_Only, ButtonScanFixGameDataClickedEvent));
        private void ButtonScanFixGameData_ClassicOnly_Click(object sender, RoutedEventArgs e)
            => this.RaiseEvent(new ButtonScanFixGameDataClickRoutedEventArgs(Classes.PSO2.GameClientSelection.Classic_Only, ButtonScanFixGameDataClickedEvent));

        public event RoutedEventHandler ButtonPSO2GameOptionClicked
        {
            add { this.AddHandler(ButtonPSO2GameOptionClickedEvent, value); }
            remove { this.RemoveHandler(ButtonPSO2GameOptionClickedEvent, value); }
        }
        private void WeirdButtonPSO2GameOption_Click(object sender, RoutedEventArgs e)
            => this.RaiseEvent(new RoutedEventArgs(ButtonPSO2GameOptionClickedEvent));

        public event RoutedEventHandler ButtonManageGameLauncherBehaviorClicked
        {
            add { this.AddHandler(ButtonManageGameLauncherBehaviorClickedEvent, value); }
            remove { this.RemoveHandler(ButtonManageGameLauncherBehaviorClickedEvent, value); }
        }
        private void ButtonManageGameLauncherBehavior_Click(object sender, RoutedEventArgs e)
            => this.RaiseEvent(new RoutedEventArgs(ButtonManageGameLauncherBehaviorClickedEvent));

        public event RoutedEventHandler ButtonManageGameLauncherRSSFeedsClicked
        {
            add { this.AddHandler(ButtonManageGameLauncherRSSFeedsClickedEvent, value); }
            remove { this.RemoveHandler(ButtonManageGameLauncherRSSFeedsClickedEvent, value); }
        }
        private void ButtonManageGameLauncherRSSFeeds_Click(object sender, RoutedEventArgs e)
            => this.RaiseEvent(new RoutedEventArgs(ButtonManageGameLauncherRSSFeedsClickedEvent));

        private void MetroTabItem_SelectionChanged(object sender, RoutedEventArgs e)
            => this.RaiseEvent(new RoutedEventArgs(IsSelectedChangedEvent));

        public event RoutedEventHandler ButtonInstallPSO2Clicked
        {
            add => this.AddHandler(ButtonInstallPSO2ClickedEvent, value);
            remove => this.RemoveHandler(ButtonInstallPSO2ClickedEvent, value);
        }

        private void MenuItemInstallPSO2_Click(object sender, RoutedEventArgs e)
            => this.RaiseEvent(new RoutedEventArgs(ButtonInstallPSO2ClickedEvent));

        public event RoutedEventHandler ButtonPSO2TroubleshootingClicked
        {
            add => this.AddHandler(ButtonPSO2TroubleshootingClickedEvent, value);
            remove => this.RemoveHandler(ButtonPSO2TroubleshootingClickedEvent, value);
        }

        private void MenuItemPSO2Troubleshooting_Click(object sender, RoutedEventArgs e)
            => this.RaiseEvent(new RoutedEventArgs(ButtonPSO2TroubleshootingClickedEvent));

        public event RoutedEventHandler ButtonManageLauncherThemingClicked
        {
            add => this.AddHandler(ButtonManageLauncherThemingClickedEvent, value);
            remove => this.RemoveHandler(ButtonManageLauncherThemingClickedEvent, value);
        }

        private void ButtonManageGameLauncherTheming_Click(object sender, RoutedEventArgs e)
            => this.RaiseEvent(new RoutedEventArgs(ButtonManageLauncherThemingClickedEvent));

        private void MenuItem_AlphaReactorCounter_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                if (item.Icon is Image img)
                {
                    img.Source = ico_alphareactor.Value;
                }
                item.Tag = StaticResources.Url_Toolbox_AlphaReactorCounter;
            }
        }

        private void MenuItemToolBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is Uri url && url.IsAbsoluteUri)
            {
                App.Current.ExecuteCommandUrl(url);
            }
        }
    }
}

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
        public static readonly RoutedEvent ButtonCheckForPSO2UpdateClickedEvent = EventManager.RegisterRoutedEvent("ButtonCheckForPSO2UpdateClicked", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TabMainMenu));
        public static readonly RoutedEvent ButtonManageGameDataClickedEvent = EventManager.RegisterRoutedEvent("ButtonManageGameDataClicked", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TabMainMenu));
        public static readonly RoutedEvent ButtonGameStartClickedEvent = EventManager.RegisterRoutedEvent("ButtonGameStartClicked", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TabMainMenu));
        public static readonly RoutedEvent LoginAndPlayClickedEvent = EventManager.RegisterRoutedEvent("LoginAndPlayClicked", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TabMainMenu));

        public static readonly DependencyProperty GameStartEnabledProperty = DependencyProperty.Register("GameStartEnabled", typeof(bool), typeof(TabMainMenu), new UIPropertyMetadata(true));

        public bool GameStartEnabled
        {
            get => (bool)this.GetValue(GameStartEnabledProperty);
            set => this.SetValue(GameStartEnabledProperty, value);
        }

        public TabMainMenu()
        {
            InitializeComponent();
        }

        public event RoutedEventHandler ButtonCheckForPSO2UpdateClicked
        {
            add { this.AddHandler(ButtonCheckForPSO2UpdateClickedEvent, value); }
            remove { this.RemoveHandler(ButtonCheckForPSO2UpdateClickedEvent, value); }
        }

        private void ButtonCheckForPSO2Update_Click(object sender, RoutedEventArgs e)
        {
            this.RaiseEvent(new RoutedEventArgs(ButtonCheckForPSO2UpdateClickedEvent));
        }

        public event RoutedEventHandler ButtonManageGameDataClicked
        {
            add { this.AddHandler(ButtonManageGameDataClickedEvent, value); }
            remove { this.RemoveHandler(ButtonManageGameDataClickedEvent, value); }
        }

        private void ButtonManageGameData_Click(object sender, RoutedEventArgs e)
        {
            this.RaiseEvent(new RoutedEventArgs(ButtonManageGameDataClickedEvent));
        }

        public event RoutedEventHandler ButtonGameStartClicked
        {
            add { this.AddHandler(ButtonGameStartClickedEvent, value); }
            remove { this.RemoveHandler(ButtonGameStartClickedEvent, value); }
        }

        private void ButtonGameStart_Click(object sender, RoutedEventArgs e)
        {
            this.RaiseEvent(new RoutedEventArgs(ButtonGameStartClickedEvent));
        }

        public event RoutedEventHandler LoginAndPlayClicked
        {
            add { this.AddHandler(LoginAndPlayClickedEvent, value); }
            remove { this.RemoveHandler(LoginAndPlayClickedEvent, value); }
        }
        private void MenuItemLoginAndPlay_Click(object sender, RoutedEventArgs e)
        {
            this.RaiseEvent(new RoutedEventArgs(LoginAndPlayClickedEvent));
        }
    }
}

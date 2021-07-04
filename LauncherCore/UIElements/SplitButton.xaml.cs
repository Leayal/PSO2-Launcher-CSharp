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
    /// Interaction logic for SplitButton.xaml
    /// </summary>
    public partial class SplitButton : UserControl
    {
        public static readonly RoutedEvent DropDownClickEvent = EventManager.RegisterRoutedEvent("DropDownClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SplitButton));
        public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent("Click", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SplitButton));

        public SplitButton()
        {
            InitializeComponent();
        }

        private void ButtonDropDown_Click(object sender, RoutedEventArgs e)
        {
            var ev = new RoutedEventArgs(DropDownClickEvent);
            this.RaiseEvent(ev);
            if (!ev.Handled)
            {
                if (this.ContextMenu != null)
                {
                    this.ContextMenu.IsOpen = true;
                }
            }
        }
        
        public event RoutedEventHandler Click
        {
            add { this.AddHandler(ClickEvent, value); }
            remove { this.RemoveHandler(ClickEvent, value); }
        }

        public event RoutedEventHandler DropDownClicked
        {
            add { this.AddHandler(DropDownClickEvent, value); }
            remove { this.RemoveHandler(DropDownClickEvent, value); }
        }

        private void Button_Click(object sender, RoutedEventArgs e) => this.RaiseEvent(new RoutedEventArgs(ClickEvent));
    }
}

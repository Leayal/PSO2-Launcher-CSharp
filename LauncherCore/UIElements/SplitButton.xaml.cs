using MahApps.Metro.Controls;
using System;
using System.Collections;
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
    public partial class SplitButton : ContentControlEx
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
                    /*
                    var uielement = e.OriginalSource as UIElement;
                    if (uielement == null)
                    {
                        uielement = e.Source as UIElement;
                    }
                    if (uielement == null)
                    {
                        uielement = sender as UIElement;
                    }
                    */
                    if (sender is UIElement element)
                    {
                        this.ContextMenu.PlacementTarget = element;
                    }
                    else
                    {
                        this.ContextMenu.PlacementTarget = this;
                    }
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

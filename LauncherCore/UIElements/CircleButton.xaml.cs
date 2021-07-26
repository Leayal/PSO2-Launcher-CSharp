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
    /// Interaction logic for CircleButton.xaml
    /// </summary>
    public partial class CircleButton : Button
    {

        public CircleButton()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            this.RaiseEvent(new RoutedEventArgs(ClickEvent));
        }

        private void Carret_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }


    }
}

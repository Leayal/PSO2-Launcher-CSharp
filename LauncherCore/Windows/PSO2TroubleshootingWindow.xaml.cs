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
using System.Windows.Shapes;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for PSO2TroubleshootingWindow.xaml
    /// </summary>
    public partial class PSO2TroubleshootingWindow : MetroWindowEx
    {
        public PSO2TroubleshootingWindow()
        {
            InitializeComponent();
        }

        // Select category
        // -> Verify component(s) or environment(s) to filter answers
        // -> Asks what happened (Gets user select answer(s))
        // -> Filter fix suggestion(s)
        // -> Begin giving fix suggestion(s) or auto-fix (if user allows, but this probably requires admin privileges).

        // Hah!! Too much work.
        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }
    }
}

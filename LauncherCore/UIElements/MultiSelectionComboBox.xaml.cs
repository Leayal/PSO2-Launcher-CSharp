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
    /// Interaction logic for MultiSelectionComboBox.xaml
    /// </summary>
    public partial class MultiSelectionComboBox : UserControl
    {
        public static readonly ClientType[] ClientTypes = Enum.GetValues<ClientType>();

        public MultiSelectionComboBox()
        {
            InitializeComponent();

        }

        private void ContextMenu_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu menu)
            {
                menu.PlacementTarget = this;
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            }
        }

        private void ComboBoxClientTypeSelection_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                if (comboBox.ItemsSource == null)
                {
                    comboBox.ItemsSource = ClientTypes;
                    comboBox.SelectedIndex = 0;
                }
            }
        }

        public enum ClientType
        {
            Both,
            NGS,
            Classic
        }

        private void ComboBoxClientTypeSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}

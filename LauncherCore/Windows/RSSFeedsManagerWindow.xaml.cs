using Leayal.PSO2Launcher.RSS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Leayal.PSO2Launcher.Core.Classes.RSS;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for RSSFeedsManagerWindow.xaml
    /// </summary>
    public partial class RSSFeedsManagerWindow : MetroWindowEx
    {
        private readonly List<FeedChannelConfig> items;

        public RSSFeedsManagerWindow(ObservableCollection<RSSFeedHandler> handlers)
        {
            InitializeComponent();
            if (handlers != null && handlers.Count != 0)
            {
                items = new List<FeedChannelConfig>(handlers.Count);
                foreach (var item in handlers)
                {
                    items.Add(FeedChannelConfig.FromHandler(item));
                }
            }
            else
            {
                items = new List<FeedChannelConfig>();
            }
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonEdit_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {

        }


    }
}

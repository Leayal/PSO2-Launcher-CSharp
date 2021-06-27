using MahApps.Metro.Controls;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    /// <summary>
    /// Interaction logic for TabGameUpdateProgress.xaml
    /// </summary>
    public partial class TabGameUpdateProgress : MetroTabItem
    {
        private static readonly DependencyProperty IsIndeterminedProperty = DependencyProperty.Register("IsIndetermined", typeof(bool), typeof(TabGameUpdateProgress), new UIPropertyMetadata(false));

        public static readonly RoutedEvent UpdateCancelClickedEvent = EventManager.RegisterRoutedEvent("UpdateCancelClicked", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TabGameUpdateProgress));

        public bool IsIndetermined
        {
            get => (bool)this.GetValue(IsIndeterminedProperty);
            set => this.SetValue(IsIndeterminedProperty, value);
        }

        public event RoutedEventHandler UpdateCancelClicked
        {
            add { this.AddHandler(UpdateCancelClickedEvent, value); }
            remove { this.RemoveHandler(UpdateCancelClickedEvent, value); }
        }

        private readonly ObservableCollection<ExtendedProgressBar> indexing;

        public TabGameUpdateProgress()
        {
            this.indexing = new ObservableCollection<ExtendedProgressBar>();
            InitializeComponent();
            this.TopProgressBar.ShowDetailedProgressPercentage = true;
            this.DownloadFileTable.ItemsSource = this.indexing;
        }

        public void SetProgressBarCount(int count)
        {
            var currentHaving = this.indexing.Count;
            if (count != currentHaving)
            {
                this.indexing.Clear();
                for (int i = 0; i < count; i++)
                {
                    // this.DownloadFileTable.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40) });
                    var progressbar = new ExtendedProgressBar() { Margin = new Thickness(1) };
                    Grid.SetRow(progressbar, i);
                    this.indexing.Add(progressbar);
                }
            }
        }

        public void SetProgressText(int index, string text) => this.indexing[index].Text = text;
        public void SetProgressValue(int index, in double value) => this.indexing[index].ProgressBar.Value = value;
        public void SetProgressMaximum(int index, in double value) => this.indexing[index].ProgressBar.Maximum = value;

        private void ButtonCancel_Click(object sender, RoutedEventArgs e) => this.RaiseEvent(new RoutedEventArgs(UpdateCancelClickedEvent));
    }
}

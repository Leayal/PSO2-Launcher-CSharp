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
    /// Interaction logic for TabGameUpdateProgress.xaml
    /// </summary>
    public partial class TabGameUpdateProgress : MetroTabItem
    {
        private static readonly DependencyProperty IsIndeterminedProperty = DependencyProperty.Register("IsIndetermined", typeof(bool), typeof(TabGameUpdateProgress), new UIPropertyMetadata(false));

        public bool IsIndetermined
        {
            get => (bool)this.GetValue(IsIndeterminedProperty);
            set => this.SetValue(IsIndeterminedProperty, value);
        }

        private readonly List<ExtendedProgressBar> indexing;

        public TabGameUpdateProgress()
        {
            this.indexing = new List<ExtendedProgressBar>();
            InitializeComponent();
        }

        public void SetProgressBarCount(int count)
        {
            var currentHaving = this.DownloadFileTable.RowDefinitions.Count;
            if (count != currentHaving)
            {
                this.indexing.Clear();
                this.DownloadFileTable.RowDefinitions.Clear();
                this.DownloadFileTable.Children.Clear();
                for (int i = 0; i < count; i++)
                {
                    this.DownloadFileTable.RowDefinitions.Add(new RowDefinition());
                    var progressbar = new ExtendedProgressBar();
                    Grid.SetRow(progressbar, i);
                    this.indexing.Add(progressbar);
                    this.DownloadFileTable.Children.Add(progressbar);
                }
            }
        }



        // Not used
        public void SetProgressText(int index, string text) => this.indexing[index].Text = text;
        public void SetProgressValue(int index, in double value) => this.indexing[index].ProgressBar.Value = value;
        public void SetProgressMaximum(int index, in double value) => this.indexing[index].ProgressBar.Maximum = value;
    }
}

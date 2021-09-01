using Leayal.PSO2Launcher.Core.Classes;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
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
        public static readonly DependencyProperty IsIndeterminedProperty = DependencyProperty.Register("IsIndetermined", typeof(bool), typeof(TabGameUpdateProgress), new UIPropertyMetadata(false));

        public static readonly RoutedEvent UpdateCancelClickedEvent = EventManager.RegisterRoutedEvent("UpdateCancelClicked", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(TabGameUpdateProgress));

        private static readonly DependencyPropertyKey TotalFileNeedToDownloadPropertyKey = DependencyProperty.RegisterReadOnly("TotalFileNeedToDownload", typeof(int), typeof(TabGameUpdateProgress), new PropertyMetadata(0));
        public static readonly DependencyProperty TotalFileNeedToDownloadProperty = TotalFileNeedToDownloadPropertyKey.DependencyProperty;
        public int TotalFileNeedToDownload => (int)this.GetValue(TotalFileNeedToDownloadProperty);

        public static readonly DependencyPropertyKey TotalDownloadedPropertyKey = DependencyProperty.RegisterReadOnly("TotalDownloaded", typeof(int), typeof(TabGameUpdateProgress), new PropertyMetadata(0));
        public static readonly DependencyProperty TotalDownloadedProperty = TotalDownloadedPropertyKey.DependencyProperty;
        public int TotalDownloaded => (int)this.GetValue(TotalDownloadedProperty);

        public static readonly DependencyPropertyKey TotalDownloadedBytesPropertyKey = DependencyProperty.RegisterReadOnly("TotalDownloadedBytes", typeof(long), typeof(TabGameUpdateProgress), new PropertyMetadata(0L, (obj, val) =>
        {
            if (obj is TabGameUpdateProgress tab)
            {
                if (val.NewValue is long l || long.TryParse(val.NewValue.ToString(), System.Globalization.NumberStyles.Integer, null, out l))
                {
                    if (l == 0)
                    {
                        tab.SetValue(TotalDownloadedBytesTextPropertyKey, "0 B");
                    }
                    else
                    {
                        tab.SetValue(TotalDownloadedBytesTextPropertyKey, Leayal.Shared.NumericHelper.ToHumanReadableFileSize(in l));
                    }
                }
                else
                {
                    tab.SetValue(TotalDownloadedBytesTextPropertyKey, $"{val.NewValue} B");
                }
            }
        }));
        public static readonly DependencyPropertyKey TotalDownloadedBytesTextPropertyKey = DependencyProperty.RegisterReadOnly("TotalDownloadedBytesText", typeof(string), typeof(TabGameUpdateProgress), new PropertyMetadata("0 B"));
        public static readonly DependencyProperty TotalDownloadedBytesTextProperty = TotalDownloadedBytesTextPropertyKey.DependencyProperty;
        public string TotalDownloadedBytesText => (string)this.GetValue(TotalDownloadedBytesTextProperty);

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

        private readonly DebounceDispatcher debounceDispatcher1, debounceDispatcher2, debounceDispatcher3;
        private readonly ObservableCollection<ExtendedProgressBar> indexing;
        private int downloadedCount, totalDownloadCount;

        private long downloadedByteCount;

        public TabGameUpdateProgress()
        {
            this.downloadedByteCount = 0L;
            this.downloadedCount = 0;
            this.totalDownloadCount = 0;
            this.indexing = new ObservableCollection<ExtendedProgressBar>();
            this.debounceDispatcher1 = new DebounceDispatcher(this.Dispatcher);
            this.debounceDispatcher2 = new DebounceDispatcher(this.Dispatcher);
            this.debounceDispatcher3 = new DebounceDispatcher(this.Dispatcher);
            InitializeComponent();
            this.TopProgressBar.ShowDetailedProgressPercentage = true;
            this.DownloadFileTable.ItemsSource = this.indexing;
        }

        public int IncreaseDownloadedCount()
        {
            var num = Interlocked.Increment(ref this.downloadedCount);
            this.debounceDispatcher1.ThrottleEx(10, delegate
            {
                this.SetValue(TotalDownloadedPropertyKey, num);
            }, System.Windows.Threading.DispatcherPriority.Render);
            return num;
        }

        public long IncreaseDownloadedBytesCount(in long byteCount)
        {
            var num = Interlocked.Add(ref this.downloadedByteCount, byteCount);
            this.debounceDispatcher3.ThrottleEx(10, delegate
            {
                this.SetValue(TotalDownloadedBytesPropertyKey, num);
            }, System.Windows.Threading.DispatcherPriority.Render);
            return num;
        }

        public void IncreaseNeedToDownloadCount()
        {
            var num = Interlocked.Increment(ref this.totalDownloadCount);
            this.debounceDispatcher2.ThrottleEx(10, delegate
            {
                this.SetValue(TotalFileNeedToDownloadPropertyKey, num);
            }, System.Windows.Threading.DispatcherPriority.Render);
        }

        public void ResetDownloadCount()
        {
            Interlocked.Exchange(ref this.downloadedCount, 0);
            Interlocked.Exchange(ref this.totalDownloadCount, 0);
            Interlocked.Exchange(ref this.downloadedByteCount, 0L);
            this.SetValue(TotalDownloadedPropertyKey, 0);
            this.SetValue(TotalFileNeedToDownloadPropertyKey, 0);
            this.SetValue(TotalDownloadedBytesPropertyKey, 0L);
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
        public void SetProgressTextVisible(int index, bool value) => this.indexing[index].ShowProgressText = value;
        public void SetProgressValue(int index, in double value) => this.indexing[index].ProgressBar.Value = value;
        public void SetProgressMaximum(int index, in double value) => this.indexing[index].ProgressBar.Maximum = value;

        private void ThisSelf_Unselected(object sender, RoutedEventArgs e)
            => this.DispatchersStop();

        private void ThisSelf_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool b)
            {
                if (!b)
                {
                    this.DispatchersStop();
                }
            }
            else
            {
                this.DispatchersStop();
            }
        }

        private void DispatchersStop()
        {
            this.debounceDispatcher1?.Stop();
            this.debounceDispatcher2?.Stop();
            this.debounceDispatcher3?.Stop();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e) => this.RaiseEvent(new RoutedEventArgs(UpdateCancelClickedEvent));
    }
}

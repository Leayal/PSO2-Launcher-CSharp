using Leayal.PSO2Launcher.Core.Classes;
using MahApps.Metro.Controls;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private readonly SimpleDispatcherQueue debounceDispatcher;
        private DispatcherQueueItem? dispatcherQueueItem_IncreaseDownloadedCount, dispatcherQueueItem_IncreaseNeedToDownloadCount;
        private DispatcherQueueItem? dispatcherQueueItem_MainProgressValue;
        private readonly ObservableCollection<ExtendedProgressBar> indexing;
        private readonly List<ProgressController> controllers;

        private int downloadedCount, totalDownloadCount;
        private long downloadedByteCount;

        public TabGameUpdateProgress()
        {
            this.downloadedByteCount = 0L;
            this.downloadedCount = 0;
            this.totalDownloadCount = 0;
            this.controllers = new List<ProgressController>(Environment.ProcessorCount);
            this.indexing = new ObservableCollection<ExtendedProgressBar>();
            this.debounceDispatcher = SimpleDispatcherQueue.CreateDefault(TimeSpan.FromMilliseconds(15), System.Windows.Threading.DispatcherPriority.DataBind, this.Dispatcher);
            InitializeComponent();
            this.TopProgressBar.ShowDetailedProgressPercentage = true;
            this.DownloadFileTable.ItemsSource = this.indexing;
        }

        public void IncreaseDownloadedCount(in long byteCount)
        {
            Interlocked.Increment(ref this.downloadedCount);
            Interlocked.Add(ref this.downloadedByteCount, byteCount);

            // Only `this` is captured.
            var newitem = this.debounceDispatcher.RegisterToTick(delegate
            {
                this.SetValue(TotalDownloadedPropertyKey, Interlocked.CompareExchange(ref this.downloadedCount, 0, 0));
                this.SetValue(TotalDownloadedBytesPropertyKey, Interlocked.CompareExchange(ref this.downloadedByteCount, 0, 0));

                Interlocked.Exchange(ref this.dispatcherQueueItem_IncreaseDownloadedCount, null)?.Unregister();
            });
            var olditem = Interlocked.CompareExchange(ref this.dispatcherQueueItem_IncreaseDownloadedCount, newitem, null);
            if (olditem != null && olditem != newitem)
            {
                newitem.Unregister();
            }
        }

        public void IncreaseNeedToDownloadCount()
        {
            Interlocked.Increment(ref this.totalDownloadCount);

            // Only `this` is captured.
            var newitem = this.debounceDispatcher.RegisterToTick(delegate
            {
                this.SetValue(TotalFileNeedToDownloadPropertyKey, Interlocked.CompareExchange(ref this.totalDownloadCount, 0, 0));
                Interlocked.Exchange(ref this.dispatcherQueueItem_IncreaseNeedToDownloadCount, null)?.Unregister();
            });
            var olditem = Interlocked.CompareExchange(ref this.dispatcherQueueItem_IncreaseNeedToDownloadCount, newitem, null);
            if (olditem != null && olditem != newitem)
            {
                newitem.Unregister();
            }
        }

        public void ResetMainProgressBarState()
        {
            if (this.Dispatcher.CheckAccess())
            {
                this.TopProgressBar.ProgressBar.Value = 0d;
                this.UpdateMainProgressBarState("Checking file", 100d, false);
            }
            else
            {
                this.Dispatcher.Invoke(this.ResetMainProgressBarState);
            }
        }

        private delegate void _UpdateMainProgressBarState(string text, double maximum, bool showdetail);
        public void UpdateMainProgressBarState(string text, double maximum, bool showdetail)
        {
            if (this.Dispatcher.CheckAccess())
            {
                var topprogressbar = this.TopProgressBar;
                topprogressbar.Text = text;
                topprogressbar.ProgressBar.Maximum = maximum;
                topprogressbar.ShowDetailedProgressPercentage = showdetail;
            }
            else
            {
                this.Dispatcher.Invoke(new _UpdateMainProgressBarState(this.UpdateMainProgressBarState), new object[] { text, maximum, showdetail });
            }
        }

        public void SetMainProgressBarValue(double value)
        {
            if (this.Dispatcher.CheckAccess())
            {
                Interlocked.Exchange(ref this.dispatcherQueueItem_MainProgressValue, null)?.Unregister();
                this.TopProgressBar.ProgressBar.Value = value;
            }
            else
            {
                Interlocked.Exchange(ref this.dispatcherQueueItem_MainProgressValue, this.debounceDispatcher.RegisterToTick(new Action<ProgressBar, double>((_progressbar, val) =>
                {
                    _progressbar.Value = val;
                    // Interlocked.Exchange(ref this.dispatcherQueueItem_MainProgressValue, null)?.Unregister();
                }), this.TopProgressBar.ProgressBar, value))?.Unregister();
            }
        }

        public void ResetAllSubDownloadState()
        {
            Interlocked.Exchange(ref this.downloadedCount, 0);
            Interlocked.Exchange(ref this.totalDownloadCount, 0);
            Interlocked.Exchange(ref this.downloadedByteCount, 0L);
            this.SetValue(TotalDownloadedPropertyKey, 0);
            this.SetValue(TotalFileNeedToDownloadPropertyKey, 0);
            this.SetValue(TotalDownloadedBytesPropertyKey, 0L);

            var count = this.controllers.Count;
            for (int index = 0; index < count; index++)
            {
                this.controllers[index].Reset();
            }
        }

        public ProgressController GetProgressController(int index)
        {
            return this.controllers[index];
        }

        public void SetProgressBarCount(in int count)
        {
            var currentHaving = this.indexing.Count;
            if (count != currentHaving)
            {
                this.indexing.Clear();
                this.controllers.Clear();
                for (int i = 0; i < count; i++)
                {
                    // this.DownloadFileTable.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40) });
                    var progressbar = new ExtendedProgressBar() { Margin = new Thickness(1), Text = string.Empty, ShowDetailedProgressPercentage = true, ShowProgressText = false };
                    Grid.SetRow(progressbar, i);
                    this.indexing.Add(progressbar);
                    this.controllers.Add(new ProgressController(progressbar));
                }
            }
        }

        // public void SetProgressText(int index, string text) => this.indexing[index].Text = text;
        // public void SetProgressTextVisible(int index, bool value) => this.indexing[index].ShowProgressText = value;
        // public void SetProgressMaximum(int index, in double value) => this.indexing[index].ProgressBar.Maximum = value;

        private void ThisSelf_Unselected(object sender, RoutedEventArgs e)
        {
            // this.debounceDispatcher?.Stop();
        }

        private void ThisSelf_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                this.debounceDispatcher?.Start();
            }
            else
            {
                this.debounceDispatcher?.Stop();
            }
            // MahApps.Metro.Controls.Dialogs.ProgressDialogController a = new MahApps.Metro.Controls.Dialogs.ProgressDialogController();
        }

        public class ProgressController
        {
            private readonly ExtendedProgressBar progressbar;
            private long _value;
            private readonly DebounceDispatcher debouncer;

            public ProgressController(ExtendedProgressBar progressBar)
            {
                this.progressbar = progressBar;
                this.debouncer = new DebounceDispatcher(progressBar.Dispatcher);
                this.Reset();
            }

            public void Reset()
            {
                var disp = this.progressbar.Dispatcher;
                if (disp.CheckAccess())
                {
                    this.InnerReset();
                }
                else
                {
                    disp.Invoke(this.InnerReset);
                }
            }

            private void InnerReset()
            {
                this.debouncer.Stop();
                Interlocked.Exchange(ref this._value, 0);
                this.SetTextVislble(false);
                this.SetText(string.Empty);
                this.SetProgressLatest();
            }

            private static readonly Action<ProgressController, double> __setMax = (obj, val) => obj.progressbar.ProgressBar.Maximum = val;
            public void SetMaximum(double max)
            {
                if (this.progressbar.CheckAccess())
                {
                    __setMax(this, max);
                }
                else
                {
                    this.progressbar.ProgressBar.Dispatcher.Invoke(__setMax, new object[] { this, max });
                }
            }

            public void SetProgress(long value)
            {
                Interlocked.Exchange(ref this._value, value);
                // Interlocked.Add(ref this._value, value);
                this.debouncer.Throttle(30, this.SetProgressLatest);
            }

            private void SetProgressLatest()
            {
                this.progressbar.ProgressBar.Value = Interlocked.Read(ref this._value);
            }

            private static readonly Action<ProgressController, string> __setText = (obj, val) => obj.progressbar.Text = val;
            public void SetText(string text)
            {
                if (this.progressbar.CheckAccess())
                {
                    __setText(this, text);
                }
                else
                {
                    this.progressbar.Dispatcher.Invoke(__setText, new object[] { this, text });
                }
            }

            private static readonly Action<ProgressController, bool> __setTextVisible = (obj, val) => obj.progressbar.ShowProgressText = val;
            public void SetTextVislble(bool value)
            {
                if (this.progressbar.CheckAccess())
                {
                    __setTextVisible(this, value);
                }
                else
                {
                    this.progressbar.Dispatcher.Invoke(__setTextVisible, new object[] { this, value });
                }
            }

            private static readonly Action<ProgressController, long, long, string, bool> __setData = (obj, max, value, text, visibility) =>
            {
                obj.SetMaximum(max);
                obj.SetProgress(value);
                obj.SetText(text);
                obj.SetTextVislble(visibility);
            };
            public void SetData(long maximum, long value, string text, bool visibility)
            {
                if (this.progressbar.CheckAccess())
                {
                    __setData(this, maximum, value, text, visibility);
                }
                else
                {
                    this.progressbar.Dispatcher.Invoke(__setData, new object[] { this, maximum, value, text, visibility });
                }
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e) => this.RaiseEvent(new RoutedEventArgs(UpdateCancelClickedEvent));
    }
}

using Leayal.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Leayal.PSO2Launcher.Toolbox.Windows
{
    partial class ToolboxWindow_AlphaReactorCount
    {
        sealed class AccountData : DependencyObject, IEquatable<AccountData>
        {
            public readonly long AccountID;
            private readonly HashSet<string> names;
            private readonly StringBuilder sb;

            private static readonly DependencyPropertyKey NamePropertyKey = DependencyProperty.RegisterReadOnly("Name", typeof(string), typeof(AccountData), new PropertyMetadata(string.Empty));
            public static readonly DependencyProperty NameProperty = NamePropertyKey.DependencyProperty;

            public string Name => (string)this.GetValue(NameProperty);

            private static readonly DependencyPropertyKey NamesOnlyPropertyKey = DependencyProperty.RegisterReadOnly("NamesOnly", typeof(string), typeof(AccountData), new PropertyMetadata(string.Empty));
            public static readonly DependencyProperty NamesOnlyProperty = NamesOnlyPropertyKey.DependencyProperty;

            public string NamesOnly => (string)this.GetValue(NamesOnlyProperty);

            public long ID => this.AccountID;

            public static readonly DependencyProperty AlphaReactorCountProperty = DependencyProperty.Register("AlphaReactorCount", typeof(int), typeof(AccountData), new PropertyMetadata(0));
            public int AlphaReactorCount
            {
                get => (int)this.GetValue(AlphaReactorCountProperty);
                set => this.SetValue(AlphaReactorCountProperty, value);
            }

            public AccountData(long characterID)
            {
                this.sb = new StringBuilder();
                this.names = new HashSet<string>();
                this.AccountID = characterID;
            }

            public void AddName(string name)
            {
                if (this.names.Add(name))
                {
                    this.sb.Clear();
                    bool isFirst = true;
                    foreach (var n in this.names)
                    {
                        if (isFirst)
                        {
                            isFirst = false;
                            this.sb.Append(n);
                        }
                        else
                        {
                            this.sb.Append(", ").Append(n);
                        }
                    }
                    this.SetValue(NamesOnlyPropertyKey, this.sb.ToString());
                    this.sb.Append(" (Account ID: ").Append(this.AccountID).Append(')');
                    this.SetValue(NamePropertyKey, this.sb.ToString());
                }
            }

            public bool Equals(AccountData other) => this.AccountID == other.AccountID;
        }

        private void AccountSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems is not null && e.AddedItems.Count != 0)
            {
                this.AlphaCounter.DataContext = e.AddedItems[0];
            }
            else
            {
                this.AlphaCounter.DataContext = null;
            }
        }

        private void AccountSelector_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox box)
            {
                box.ItemsSource = this.characters;
            }
        }

        private static DateTime AdjustToGameResetTime(in DateTime datetime)
        {
            // Consider push the datetime before reset to become previous day.
            if ((TimeOnly.FromDateTime(datetime) < DailyResetTime))
            {
                return datetime.AddDays(-1);
            }
            else
            {
                return datetime;
            }
        }

        public sealed class LogCategories : IDisposable
        {
            private static readonly string LogDir = Path.GetFullPath(Path.Combine("SEGA", "PHANTASYSTARONLINE2", "log_ngs"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            private readonly DispatcherTimer ddActionLog, ddRewardLog;
            public readonly List<string> ActionLog, RewardLog;
            public readonly FileSystemWatcher watcher;
            private DateTime lastTick;

            private readonly System.Threading.Timer globalTimer;

            private int refcount;
            private readonly Task t_refresh;

            public LogCategories(Dispatcher dispatcher)
            {
                this.refcount = 0;
                this.lastTick = AdjustToGameResetTime(TimeZoneHelper.ConvertTimeToLocalJST(DateTime.Now));
                this.globalTimer = new(new TimerCallback(this.TimeTick), dispatcher, Timeout.Infinite, Timeout.Infinite);
                this.ActionLog = new List<string>();
                this.RewardLog = new List<string>();
                var ts = TimeSpan.FromMilliseconds(50);
                this.ddActionLog = new DispatcherTimer(ts, DispatcherPriority.Normal, this.OnNewActionLogFound, dispatcher) { IsEnabled = false };
                this.ddRewardLog = new DispatcherTimer(ts, DispatcherPriority.Normal, this.OnNewRewardLogFound, dispatcher) { IsEnabled = false };
                this.watcher = new FileSystemWatcher();
                this.watcher.BeginInit();
                Directory.CreateDirectory(LogDir);
                this.watcher.Path = LogDir;
                this.watcher.Filter = "*.txt";
                this.watcher.IncludeSubdirectories = false;
                this.watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.Size;
                this.watcher.Created += this.Watcher_FileCreated;
                this.watcher.Renamed += this.Watcher_Renamed;
                this.watcher.Deleted += this.Watcher_Deleted;
                this.watcher.EnableRaisingEvents = false;
                this.watcher.EndInit();
                this.t_refresh = this.Refresh();
            }

            private void Watcher_Renamed(object sender, RenamedEventArgs e)
            {
                var span = e.OldName.AsSpan();
                if (span.StartsWith("ActionLog", StringComparison.OrdinalIgnoreCase))
                {
                    lock (this.RewardLog)
                    {
                        this.ActionLog.Remove(e.OldFullPath);
                        this.ActionLog.Add(e.FullPath);
                        this.ActionLog.Sort(FileDateComparer.Default);
                    }
                    this.ddActionLog.Stop();
                    this.ddActionLog.Start();
                }
                else if (span.StartsWith("RewardLog", StringComparison.OrdinalIgnoreCase))
                {
                    lock (this.RewardLog)
                    {
                        this.RewardLog.Remove(e.OldFullPath);
                        this.RewardLog.Add(e.FullPath);
                        this.RewardLog.Sort(FileDateComparer.Default);
                    }
                    this.ddRewardLog.Stop();
                    this.ddRewardLog.Start();
                }
            }

            private void OnNewActionLogFound(object? sender, EventArgs e)
            {
                this.ddActionLog.Stop();
                this.NewFileFound?.Invoke(this, this.ActionLog);
            }
            private void OnNewRewardLogFound(object? sender, EventArgs e)
            {
                this.ddRewardLog.Stop();
                this.NewFileFound?.Invoke(this, this.RewardLog);
            }

            public async void StartWatching(Action<LogCategories, List<string>> callback, ClockTickedCallback clockCallback)
            {
                this.NewFileFound += callback;
                this.ClockTicked += clockCallback;
                if (Interlocked.Increment(ref this.refcount) == 1)
                {
                    Directory.CreateDirectory(this.watcher.Path);
                    await this.t_refresh;
                    bool isEnabled = Interlocked.CompareExchange(ref this.refcount, 0, -1) > 0;
                    this.watcher.EnableRaisingEvents = isEnabled;
                    if (isEnabled)
                    {
                        var d = DateTime.Now;
                        var a = TimeSpan.FromSeconds(1).Subtract(new TimeSpan(0, 0, 0, 0, d.Millisecond)).TotalMilliseconds;
                        this.globalTimer.Change((int)Math.Floor(a), 1000);
                    }
                }
                callback.Invoke(this, this.ActionLog);
                callback.Invoke(this, this.RewardLog);
            }

            public void StopWatching(Action<LogCategories, List<string>> callback, ClockTickedCallback clockCallback)
            {
                this.NewFileFound -= callback;
                this.ClockTicked -= clockCallback;
                if (Interlocked.Decrement(ref this.refcount) == 0)
                {
                    this.watcher.EnableRaisingEvents = false;
                    this.globalTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }

            public void Dispose()
            {
                this.ClockTicked = null;
                this.NewFileFound = null;
                this.globalTimer.Change(Timeout.Infinite, Timeout.Infinite);
                this.globalTimer.Dispose();
                this.watcher.EnableRaisingEvents = false;
                this.watcher.Dispose();
            }

            private void Watcher_Deleted(object sender, FileSystemEventArgs e)
            {
                var span = e.Name.AsSpan();
                if (span.StartsWith("ActionLog", StringComparison.OrdinalIgnoreCase))
                {
                    bool isremoved;
                    lock (this.RewardLog)
                    {
                        isremoved = this.ActionLog.Remove(e.FullPath);
                    }
                    if (isremoved)
                    {
                        this.ddActionLog.Stop();
                        this.ddActionLog.Start();
                    }
                }
                else if (span.StartsWith("RewardLog", StringComparison.OrdinalIgnoreCase))
                {
                    bool isremoved;
                    lock (this.RewardLog)
                    {
                        isremoved = this.RewardLog.Remove(e.FullPath);
                    }
                    if (isremoved)
                    {
                        this.ddRewardLog.Stop();
                        this.ddRewardLog.Start();
                    }
                }
            }

            private void TimeTick(object? obj)
            {
                var d = (Dispatcher)obj;
                var time = TimeZoneHelper.ConvertTimeToLocalJST(DateTime.Now);
                var isResettingTime = ((DateOnly.FromDateTime(time) == DateOnly.FromDateTime(this.lastTick)) && (TimeOnly.FromDateTime(this.lastTick) < DailyResetTime && TimeOnly.FromDateTime(time) > DailyResetTime));
                this.lastTick = time;
                // var timeonly = TimeOnly.FromDateTime(time);
                d.BeginInvoke(this.ClockTicked, new object[] { time, isResettingTime });
                // this.ClockTicked?.Invoke(in time, in isResettingTime);
            }

            public delegate void ClockTickedCallback(in DateTime currentTime, in bool isResetting);
            private Action<LogCategories, List<string>>? NewFileFound;
            private ClockTickedCallback? ClockTicked;

            private void Watcher_FileCreated(object sender, FileSystemEventArgs e)
            {
                var span = e.Name.AsSpan();
                if (span.StartsWith("ActionLog", StringComparison.OrdinalIgnoreCase))
                {
                    lock (this.ActionLog)
                    {
                        this.ActionLog.Add(e.FullPath);
                        this.ActionLog.Sort(FileDateComparer.Default);
                    }
                    this.ddActionLog.Stop();
                    this.ddActionLog.Start();
                }
                else if (span.StartsWith("RewardLog", StringComparison.OrdinalIgnoreCase))
                {
                    lock (this.RewardLog)
                    {
                        this.RewardLog.Add(e.FullPath);
                        this.RewardLog.Sort(FileDateComparer.Default);
                    }
                    this.ddRewardLog.Stop();
                    this.ddRewardLog.Start();
                }
            }

            private Task Refresh()
            {
                return Task.Run(() =>
                {
                    lock (this.ActionLog)
                    {
                        this.ActionLog.Clear();
                    }
                    lock (this.RewardLog)
                    {
                        this.RewardLog.Clear();
                    }

                    if (Directory.Exists(LogDir))
                    {
                        foreach (var file in Directory.EnumerateFiles(LogDir, "*.txt", SearchOption.TopDirectoryOnly))
                        {
                            var span = Path.GetFileNameWithoutExtension(file.AsSpan());
                            if (span.StartsWith("ActionLog", StringComparison.OrdinalIgnoreCase))
                            {
                                lock (this.ActionLog)
                                {
                                    this.ActionLog.Add(file);
                                }
                            }
                            else if (span.StartsWith("RewardLog", StringComparison.OrdinalIgnoreCase))
                            {
                                lock (this.RewardLog)
                                {
                                    this.RewardLog.Add(file);
                                }
                            }
                        }
                        lock (this.ActionLog)
                        {
                            this.ActionLog.Sort(FileDateComparer.Default);
                        }
                        lock (this.RewardLog)
                        {
                            this.RewardLog.Sort(FileDateComparer.Default);
                        }
                    }
                });
            }

            class FileDateComparer : IComparer<string>
            {
                public static readonly FileDateComparer Default = new FileDateComparer();
                public int Compare(string x, string y)
                {
                    var span_x = Path.GetFileNameWithoutExtension(x.AsSpan());
                    var span_y = Path.GetFileNameWithoutExtension(y.AsSpan());
                    int i_x = span_x.IndexOf("Log", StringComparison.OrdinalIgnoreCase);
                    int i_y = span_y.IndexOf("Log", StringComparison.OrdinalIgnoreCase);
                    if (i_x == -1 && i_y == -1) return 0;
                    else if (i_x == -1) return -1;
                    else if (i_y == -1) return 1;
                    span_x = span_x.Slice(i_x + 3, span_x.Length - i_x - 6);
                    span_y = span_y.Slice(i_y + 3, span_y.Length - i_y - 6);

                    // yyymmdd
                    var date_x = new DateOnly(int.Parse(span_x.Slice(0, 4)), int.Parse(span_x.Slice(4, 2)), int.Parse(span_x.Slice(6, 2)));
                    var date_y = new DateOnly(int.Parse(span_y.Slice(0, 4)), int.Parse(span_y.Slice(4, 2)), int.Parse(span_y.Slice(6, 2)));

                    return date_x.CompareTo(date_y);
                }
            }
        }
    }
}

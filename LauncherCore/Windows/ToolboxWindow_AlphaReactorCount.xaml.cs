using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Toolbox;
using Leayal.Shared;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for ToolboxWindow_AlphaReactorCount.xaml
    /// </summary>
    public partial class ToolboxWindow_AlphaReactorCount : MetroWindowEx
    {
        private static readonly TimeOnly DailyResetTime = new TimeOnly(4, 0);

        private static readonly DependencyPropertyKey CurrentDatePropertyKey = DependencyProperty.RegisterReadOnly("CurrentDate", typeof(DateOnly), typeof(ToolboxWindow_AlphaReactorCount), new PropertyMetadata(DateOnly.MinValue, (obj, e) =>
        {
            if (obj is ToolboxWindow_AlphaReactorCount window)
            {
                if (e.NewValue is DateOnly val)
                {
                    window.myToday = val;
                }
            }
        }));
        public static readonly DependencyProperty CurrentDateProperty = CurrentDatePropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey IsBeforeResetPropertyKey = DependencyProperty.RegisterReadOnly("IsBeforeReset", typeof(bool?), typeof(ToolboxWindow_AlphaReactorCount), new PropertyMetadata(null));
        public static readonly DependencyProperty IsBeforeResetProperty = IsBeforeResetPropertyKey.DependencyProperty;

        public DateOnly CurrentDate => (DateOnly)this.GetValue(CurrentDateProperty);
        public bool? IsBeforeReset => (bool?)this.GetValue(IsBeforeResetProperty);
        private DateOnly myToday;

        private PSO2LogAsyncReader? logreader;
        public static readonly Lazy<LogCategories> PSO2LogWatcher = new Lazy<LogCategories>(() => new LogCategories(App.Current.Dispatcher));
        private readonly ObservableCollection<CharacterData> characters;
        private readonly Dictionary<long, CharacterData> mapping;
        private readonly Action<LogCategories, List<string>> Logfiles_NewFileFound;

        public ToolboxWindow_AlphaReactorCount() : base()
        {
            this.mapping = new Dictionary<long, CharacterData>();
            this.characters = new ObservableCollection<CharacterData>();
            this.SetTime(TimeZoneHelper.ConvertTimeToCustom(DateTime.UtcNow));
            this.Logfiles_NewFileFound = new Action<LogCategories, List<string>>(this.Logfiles_OnNewFileFound);
            InitializeComponent();
            this.Icon = App.DefaultAppIcon;
        }

        protected override Task OnCleanupBeforeClosed()
        {
            var logfiles = PSO2LogWatcher.Value;
            logfiles.StopWatching(this.Logfiles_NewFileFound);
            try
            {
                this.CloseCurrentLog();
            }
            catch { }
            return Task.CompletedTask;
        }

        private void ThisSelf_Loaded(object sender, RoutedEventArgs e)
        {
            var logfiles = PSO2LogWatcher.Value;
            logfiles.StartWatching(this.Logfiles_NewFileFound);
            this.SelectNewestLog(logfiles.ActionLog);
        }

        private void Logfiles_OnNewFileFound(object sender, List<string> ev)
        {
            if (ev == PSO2LogWatcher.Value.ActionLog)
            {
                this.SelectNewestLog(ev);
            }
        }

        public void SelectNewestLog(List<string> logcategory) => this.SelectLog(logcategory, logcategory.Count - 1);

        public void SelectLog(List<string> logcategory, int index)
        {
            if (logcategory.Count == 0 && index < 0)
            {
                this.CloseCurrentLog();
            }
            else
            {
                var filepath = logcategory[index];
                if (this.logreader is not null && string.Equals(Path.IsPathFullyQualified(filepath) ? filepath : Path.GetFullPath(filepath), this.logreader.Fullpath, StringComparison.OrdinalIgnoreCase)) return;
                if (File.Exists(filepath))
                {
                    this.CloseCurrentLog();
                    this.RefreshDate();
                    this.logreader = new PSO2LogAsyncReader(filepath);
                    this.logreader.DataReceived += this.Logreader_DataReceived;
                    this.logreader.StartReceiving();
                }
            }
        }

        private void SetTime(DateTime datetime)
        {
            this.SetValue(CurrentDatePropertyKey, DateOnly.FromDateTime(datetime));
            this.SetValue(IsBeforeResetPropertyKey, TimeOnly.FromDateTime(datetime) < DailyResetTime);
        }

        public void RefreshDate()
        {
            var today = TimeZoneHelper.ConvertTimeToCustom(DateTime.UtcNow);
            if (this.Dispatcher.CheckAccess())
            {
                this.SetTime(today);
            }
            else
            {
                this.Dispatcher.Invoke(new Action<DateTime>(this.SetTime), new object[] { today });
            }
        }

        private void Logreader_DataReceived(PSO2LogAsyncReader arg1, LogReaderDataReceivedEventArgs arg2)
        {
            var datas = arg2.GetDatas();
            if (datas[2].Span.Equals("[Pickup]", StringComparison.OrdinalIgnoreCase) && (datas[5].Span.Equals("Alpha Reactor", StringComparison.OrdinalIgnoreCase) || datas[5].Span.Equals("アルファリアクター", StringComparison.OrdinalIgnoreCase)))
            {
                var dateonly = DateOnly.ParseExact(datas[0].Span.Slice(0, 10), "yyyy-MM-dd");
                var timeonly = TimeOnly.Parse(datas[0].Span.Slice(11));
                var localtime = new DateTime(dateonly.Year, dateonly.Month, dateonly.Day, timeonly.Hour, timeonly.Minute, timeonly.Second, DateTimeKind.Local);
                var customTime = TimeZoneHelper.ConvertTimeToCustom(localtime);
                dateonly = DateOnly.FromDateTime(customTime);
                timeonly = TimeOnly.FromDateTime(customTime);
                if ((dateonly == this.myToday && timeonly > DailyResetTime) || dateonly > this.myToday)
                {
                    this.RefreshDate();
                }
                if (dateonly == this.myToday && timeonly > DailyResetTime)
                {
                    this.IncreaseCharacterAlphaCount(long.Parse(datas[3].Span), new string(datas[4].Span));
                }
            }
            else
            {
                this.AddCharacter(long.Parse(datas[3].Span), new string(datas[4].Span));
            }
        }

        private void AddCharacter(long charId, string charName)
        {
            if (this.Dispatcher.CheckAccess())
            {
                if (!this.mapping.TryGetValue(charId, out var data))
                {
                    data = new CharacterData(charId);
                    this.mapping.Add(charId, data);
                    this.characters.Add(data);
                    if (this.characters.Count == 1)
                    {
                        this.AccountSelector.SelectedIndex = 0;
                    }
                }
                data.AddName(charName);
            }
            else
            {
                this.Dispatcher.Invoke(new Action<long, string>(this.AddCharacter), new object[] { charId, charName });
            }
        }

        private void IncreaseCharacterAlphaCount(long charId, string charName)
        {
            if (this.Dispatcher.CheckAccess())
            {
                if (!this.mapping.TryGetValue(charId, out var data))
                {
                    data = new CharacterData(charId);
                    this.mapping.Add(charId, data);
                    this.characters.Add(data);
                    if (this.characters.Count == 1)
                    {
                        this.AccountSelector.SelectedIndex = 0;
                    }
                }
                data.AddName(charName);
                data.AlphaReactorCount++;
            }
            else
            {
                this.Dispatcher.Invoke(new Action<long, string>(this.IncreaseCharacterAlphaCount), new object[] { charId, charName });
            }
        }

        public void CloseCurrentLog()
        {
            if (this.Dispatcher.CheckAccess())
            {
                this.characters.Clear();
                this.mapping.Clear();
                if (this.logreader is not null)
                {
                    this.logreader.DataReceived -= this.Logreader_DataReceived;
                    this.logreader.Dispose();
                }
            }
            else
            {
                this.Dispatcher.Invoke(this.CloseCurrentLog);
            }
        }

        sealed class CharacterData : DependencyObject, IEquatable<CharacterData>
        {
            public readonly long AccountID;
            private readonly HashSet<string> names;
            private readonly StringBuilder sb;

            private static readonly DependencyPropertyKey NamePropertyKey = DependencyProperty.RegisterReadOnly("Name", typeof(string), typeof(CharacterData), new PropertyMetadata(string.Empty));
            public static readonly DependencyProperty NameProperty = NamePropertyKey.DependencyProperty;

            public string Name => (string)this.GetValue(NameProperty);

            public static readonly DependencyProperty AlphaReactorCountProperty = DependencyProperty.Register("AlphaReactorCount", typeof(int), typeof(CharacterData), new PropertyMetadata(0));
            public int AlphaReactorCount
            {
                get => (int)this.GetValue(AlphaReactorCountProperty);
                set => this.SetValue(AlphaReactorCountProperty, value);
            }

            public CharacterData(long characterID)
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
                    this.sb.Append(" (Account ID: ").Append(this.AccountID).Append(')');
                    this.SetValue(NamePropertyKey, this.sb.ToString());
                }
            }

            public bool Equals(CharacterData other) => this.AccountID == other.AccountID;
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

        public sealed class LogCategories : IDisposable
        {
            private static readonly string LogDir = Path.GetFullPath(Path.Combine("SEGA", "PHANTASYSTARONLINE2", "log_ngs"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            private readonly DispatcherTimer ddActionLog, ddRewardLog;
            public readonly List<string> ActionLog, RewardLog;
            public readonly FileSystemWatcher watcher;

            private int refcount;
            private readonly Task t_refresh;

            public LogCategories(Dispatcher dispatcher)
            {
                this.refcount = 0;
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

            public async void StartWatching(Action<LogCategories, List<string>> callback)
            {
                if (Interlocked.Increment(ref this.refcount) == 1)
                {
                    Directory.CreateDirectory(this.watcher.Path);
                    this.NewFileFound += callback;
                    if (this.t_refresh.IsCompleted)
                    {
                        this.watcher.EnableRaisingEvents = true;
                    }
                    else
                    {
                        await this.t_refresh;
                        callback.Invoke(this, this.ActionLog);
                        callback.Invoke(this, this.RewardLog);
                        this.watcher.EnableRaisingEvents = (Interlocked.CompareExchange(ref this.refcount, 0, -1) > 0);
                    }

                }
            }

            public void StopWatching(Action<LogCategories, List<string>> callback)
            {
                this.NewFileFound -= callback;
                if (Interlocked.Decrement(ref this.refcount) == 0)
                {
                    this.watcher.EnableRaisingEvents = false;
                }
            }

            public void Dispose()
            {
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

            private Action<LogCategories, List<string>>? NewFileFound;

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

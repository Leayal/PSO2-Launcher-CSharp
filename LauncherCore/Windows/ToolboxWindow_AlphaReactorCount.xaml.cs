using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Toolbox;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for ToolboxWindow_AlphaReactorCount.xaml
    /// </summary>
    public partial class ToolboxWindow_AlphaReactorCount : MetroWindowEx
    {
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
        public DateOnly CurrentDate => (DateOnly)this.GetValue(CurrentDateProperty);
        private DateOnly myToday;

        private PSO2LogAsyncReader? logreader;
        public static readonly Lazy<LogCategories> PSO2LogWatcher = new Lazy<LogCategories>();
        private readonly ObservableCollection<CharacterData> characters;

        public ToolboxWindow_AlphaReactorCount() : base()
        {
            this.characters = new ObservableCollection<CharacterData>();
            this.myToday = DateOnly.FromDateTime(DateTime.Today);
            this.SetValue(CurrentDatePropertyKey, this.myToday);
            InitializeComponent();
        }

        protected override Task OnCleanupBeforeClosed()
        {
            var logfiles = PSO2LogWatcher.Value;
            logfiles.NewFileFound -= this.Logfiles_NewFileFound;
            logfiles.StopWatching();
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
            logfiles.StartWatching();
            this.SelectNewestLog(logfiles.ActionLog);
            logfiles.NewFileFound += this.Logfiles_NewFileFound;
        }

        private void Logfiles_NewFileFound(object sender, List<string> ev)
        {
            if (ev == PSO2LogWatcher.Value.ActionLog)
            {
                this.SelectNewestLog(ev);
            }
        }

        public void SelectNewestLog(List<string> logcategory) => this.SelectLog(logcategory, logcategory.Count - 1);

        public void SelectLog(List<string> logcategory, int index)
        {
            if (logcategory.Count == 0)
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

        public void RefreshDate()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (this.myToday != today)
            {
                if (this.Dispatcher.CheckAccess())
                {
                    this.SetValue(CurrentDatePropertyKey, today);
                }
                else
                {
                    this.Dispatcher.Invoke(new Action<DependencyProperty, object>(this.SetValue), new object[] { CurrentDatePropertyKey, today });
                }
            }
        }

        private void Logreader_DataReceived(PSO2LogAsyncReader arg1, LogReaderDataReceivedEventArgs arg2)
        {
            var datas = arg2.GetDatas();
            if (datas[2].Span.Equals("[Pickup]", StringComparison.OrdinalIgnoreCase) && (datas[5].Span.Equals("Alpha Reactor", StringComparison.OrdinalIgnoreCase) || datas[5].Span.Equals("アルファリアクター", StringComparison.OrdinalIgnoreCase)) && DateOnly.TryParse(datas[0].Span.Slice(0, 10), out var dateonly) && dateonly == this.myToday)
            {
                this.IncreaseCharacterAlphaCount(long.Parse(datas[3].Span), new string(datas[4].Span));
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
                var data = new CharacterData(charId, charName);
                var i = this.characters.IndexOf(data);
                if (i == -1)
                {
                    this.characters.Add(data);
                    if (this.characters.Count == 1)
                    {
                        this.CharacterSelector.SelectedIndex = 0;
                    }
                }
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
                var data = new CharacterData(charId, charName);
                var i = this.characters.IndexOf(data);
                if (i != -1)
                {
                    this.characters[i].AlphaReactorCount++;
                }
                else
                {
                    data.AlphaReactorCount++;
                    this.characters.Add(data);
                    if (this.characters.Count == 1)
                    {
                        this.CharacterSelector.SelectedIndex = 0;
                    }
                }
            }
            else
            {
                this.Dispatcher.Invoke(new Action<long, string>(this.IncreaseCharacterAlphaCount), new object[] { charId, charName });
            }
        }

        public void CloseCurrentLog()
        {
            if (this.logreader is not null)
            {
                if (this.Dispatcher.CheckAccess())
                {
                    this.characters.Clear();
                }
                else
                {
                    this.Dispatcher.Invoke(this.characters.Clear);
                }
                this.logreader.DataReceived -= this.Logreader_DataReceived;
                this.logreader.Dispose();
            }
        }

        sealed class CharacterData : DependencyObject, IEquatable<CharacterData>
        {
            public readonly string CharacterName;
            public readonly long CharacterID;

            public string Name => $"{this.CharacterName} (ID: {this.CharacterID})";

            public static readonly DependencyProperty AlphaReactorCountProperty = DependencyProperty.Register("AlphaReactorCount", typeof(int), typeof(CharacterData), new PropertyMetadata(0));
            public int AlphaReactorCount
            {
                get => (int)this.GetValue(AlphaReactorCountProperty);
                set => this.SetValue(AlphaReactorCountProperty, value);
            }

            public CharacterData(long characterID, string characterName)
            {
                this.CharacterID = characterID;
                this.CharacterName = characterName;
            }

            public bool Equals(CharacterData other) => (this.CharacterID == other.CharacterID && this.CharacterName == other.CharacterName);
        }

        public sealed class LogCategories : IDisposable
        {
            private static readonly string LogDir = Path.GetFullPath(Path.Combine("SEGA", "PHANTASYSTARONLINE2", "log_ngs"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            public readonly List<string> ActionLog, RewardLog;
            public readonly FileSystemWatcher watcher;

            private int refcount;

            public LogCategories()
            {
                this.refcount = 0;
                this.ActionLog = new List<string>();
                this.RewardLog = new List<string>();
                // this.RewardLog = new SortedSet<string>(FileDateComparer.Default);
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
            }

            private void Watcher_Renamed(object sender, RenamedEventArgs e)
            {
                // e.OldName
            }

            public void StartWatching()
            {
                if (Interlocked.Increment(ref this.refcount) == 1)
                {
                    Directory.CreateDirectory(this.watcher.Path);
                    this.watcher.EnableRaisingEvents = true;
                }
            }

            public void StopWatching()
            {
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
                    if (this.ActionLog.Remove(e.FullPath))
                    {
                        this.NewFileFound?.Invoke(this, this.ActionLog);
                    }
                }
                else if (span.StartsWith("RewardLog", StringComparison.OrdinalIgnoreCase))
                {
                    if (this.RewardLog.Remove(e.FullPath))
                    {
                        this.NewFileFound?.Invoke(this, this.RewardLog);
                    }
                }
            }

            public event Action<LogCategories, List<string>> NewFileFound;

            private void Watcher_FileCreated(object sender, FileSystemEventArgs e)
            {
                var span = e.Name.AsSpan();
                if (span.StartsWith("ActionLog", StringComparison.OrdinalIgnoreCase))
                {
                    this.ActionLog.Add(e.FullPath);
                    this.ActionLog.Sort(FileDateComparer.Default);
                    this.NewFileFound?.Invoke(this, this.ActionLog);
                }
                else if (span.StartsWith("RewardLog", StringComparison.OrdinalIgnoreCase))
                {
                    this.RewardLog.Add(e.FullPath);
                    this.RewardLog.Sort(FileDateComparer.Default);
                    this.NewFileFound?.Invoke(this, this.RewardLog);
                }
            }

            public async Task Refresh()
            {
                await Task.Run(() =>
                {
                    this.ActionLog.Clear();
                    this.RewardLog.Clear();
                    if (Directory.Exists(LogDir))
                    {
                        foreach (var file in Directory.EnumerateFiles(LogDir, "*.txt", SearchOption.TopDirectoryOnly))
                        {
                            var span = Path.GetFileNameWithoutExtension(file.AsSpan());
                            if (span.StartsWith("ActionLog", StringComparison.OrdinalIgnoreCase))
                            {
                                this.ActionLog.Add(file);
                            }
                            else if (span.StartsWith("RewardLog", StringComparison.OrdinalIgnoreCase))
                            {
                                this.RewardLog.Add(file);
                            }
                        }
                        this.ActionLog.Sort(FileDateComparer.Default);
                        this.RewardLog.Sort(FileDateComparer.Default);
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

        private void CharacterSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void CharacterSelector_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox box)
            {
                box.ItemsSource = this.characters;
            }
        }
    }
}

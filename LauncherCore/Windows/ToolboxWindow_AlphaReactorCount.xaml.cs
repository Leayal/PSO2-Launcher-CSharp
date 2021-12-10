using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
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
        private const char LogLineSplitter = '\t';
        
        private DateOnly myToday;

        private PSO2LogAsyncReader? logreader;
        private readonly LogCategories logfiles;
        private readonly ObservableCollection<CharacterData> characters;

        public ToolboxWindow_AlphaReactorCount() : base()
        {
            this.characters = new ObservableCollection<CharacterData>();
            this.logfiles = new LogCategories();
            InitializeComponent();
        }

        protected override async Task OnCleanupBeforeClosed()
        {
            await this.CloseCurrentLog();
        }

        private async void ThisSelf_Loaded(object sender, RoutedEventArgs e)
        {
            await this.logfiles.Refresh();
            await this.SelecNewestLog(this.logfiles.ActionLog);
            this.logfiles.NewFileFound += this.Logfiles_NewFileFound;
        }

        private async Task Logfiles_NewFileFound(object sender, List<string> ev)
        {
            if (ev == this.logfiles.ActionLog)
            {
                await this.SelecNewestLog(ev);
            }
        }

        public Task SelecNewestLog(List<string> logcategory) => this.SelectLog(logcategory, logcategory.Count - 1);

        public async Task SelectLog(List<string> logcategory, int index)
        {
            var filepath = logcategory[index];
            if (File.Exists(filepath))
            {
                await this.CloseCurrentLog();
                this.myToday = DateOnly.FromDateTime(DateTime.Today);
                this.logreader = new PSO2LogAsyncReader(new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous));
                this.logreader.DataReceived += this.Logreader_DataReceived;
                this.logreader.StartReceiving();
            }
        }

        private void Logreader_DataReceived(PSO2LogAsyncReader arg1, LogReaderDataReceivedEventArgs arg2)
        {
            var datas = arg2.GetDatas();
            if (datas[2].Span.Equals("[Pickup]", StringComparison.OrdinalIgnoreCase))
            {
                if (datas[5].Span.Equals("Alpha Reactor", StringComparison.OrdinalIgnoreCase) || datas[5].Span.Equals("アルファリアクター", StringComparison.OrdinalIgnoreCase))
                {
                    var dateonly = DateOnly.Parse(datas[0].Span.Slice(0, 10));
                    if (dateonly == this.myToday)
                    {
                        this.ProcessCharacterData(long.Parse(datas[3].Span), new string(datas[4].Span));
                    }
                }
            }
        }

        private void ProcessCharacterData(long charId, string charName)
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
                this.Dispatcher.Invoke(new Action<long, string>(this.ProcessCharacterData), new object[] { charId, charName });
            }
        }

        public async ValueTask CloseCurrentLog()
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
                await this.logreader.DisposeAsync();
                this.logreader.DataReceived -= this.Logreader_DataReceived;
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

        sealed class LogCategories
        {
            private static readonly string LogDir = Path.GetFullPath(Path.Combine("SEGA", "PHANTASYSTARONLINE2", "log_ngs"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            public readonly List<string> ActionLog, RewardLog;
            public readonly FileSystemWatcher watcher;

            public LogCategories()
            {
                this.ActionLog = new List<string>();
                this.RewardLog = new List<string>();
                this.watcher = new FileSystemWatcher();
                this.watcher.BeginInit();
                this.watcher.Path = LogDir;
                this.watcher.Filter = "*.txt";
                this.watcher.IncludeSubdirectories = false;
                this.watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.Size;
                this.watcher.Created += this.Watcher_FileCreated;
                this.watcher.Deleted += this.Watcher_Deleted;
                this.watcher.EnableRaisingEvents = true;
                this.watcher.EndInit();
            }

            private void Watcher_Deleted(object sender, FileSystemEventArgs e)
            {
                var span = Path.GetFileNameWithoutExtension(e.FullPath.AsSpan());
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

            public event Func<LogCategories, List<string>, Task> NewFileFound;

            private void Watcher_FileCreated(object sender, FileSystemEventArgs e)
            {
                var span = Path.GetFileNameWithoutExtension(e.FullPath.AsSpan());
                if (span.StartsWith("ActionLog", StringComparison.OrdinalIgnoreCase))
                {
                    this.ActionLog.Add(e.FullPath);
                    this.NewFileFound?.Invoke(this, this.ActionLog);
                }
                else if (span.StartsWith("RewardLog", StringComparison.OrdinalIgnoreCase))
                {
                    this.RewardLog.Add(e.FullPath);
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
                    span_x = span_x.Slice(i_x + 3, span_x.Length - i_x - 6);
                    int i_y = span_y.IndexOf("Log", StringComparison.OrdinalIgnoreCase);
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

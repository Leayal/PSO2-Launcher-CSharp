using Leayal.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Text.RegularExpressions;

namespace Leayal.PSO2Launcher.Toolbox.Windows
{
    sealed class LogCategories : ActivationBasedObject, IDisposable
    {
        private static readonly string LogDir = Path.GetFullPath(Path.Combine("SEGA", "PHANTASYSTARONLINE2", "log_ngs"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        private static readonly Regex r_logFiltering = new Regex(@"(\D*)Log(\d{8})_00\.txt", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly string ActionLog = "Action", ChatLog = "Chat", RewardLog = "Reward", StarGemLog = "StarGem", ScratchLog = "Scratch", SymbolChatLog = "SymbolChat";

        public readonly FileSystemWatcher watcher;
        private readonly Dictionary<string, Indexing> cache;
        private readonly Task t_refresh;
        private readonly Dispatcher _dispatcher;

        public LogCategories(Dispatcher dispatcher)
        {
            this._dispatcher = dispatcher;
            this.cache = new Dictionary<string, Indexing>(8, StringComparer.OrdinalIgnoreCase);
            var ts = TimeSpan.FromMilliseconds(50);
            this.watcher = new FileSystemWatcher();
            this.watcher.BeginInit();
            this.watcher.Path = string.Empty;
            this.watcher.Filter = "*.txt";
            this.watcher.IncludeSubdirectories = false;
            this.watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.Size;
            this.watcher.Created += this.Watcher_FileCreated;
            this.watcher.Renamed += this.Watcher_Renamed;
            this.watcher.Deleted += this.Watcher_Deleted;
            this.watcher.EnableRaisingEvents = false;
            this.watcher.EndInit();
            this.t_refresh = this.Refresh();
        }

        private static string DetermineType(in ReadOnlySpan<char> span)
        {
            if (span.Equals(ActionLog, StringComparison.OrdinalIgnoreCase)) return ActionLog;
            else if (span.Equals(ChatLog, StringComparison.OrdinalIgnoreCase)) return ChatLog;
            else if (span.Equals(RewardLog, StringComparison.OrdinalIgnoreCase)) return RewardLog;
            else if (span.Equals(StarGemLog, StringComparison.OrdinalIgnoreCase)) return StarGemLog;
            else if (span.Equals(ScratchLog, StringComparison.OrdinalIgnoreCase)) return ScratchLog;
            else if (span.Equals(SymbolChatLog, StringComparison.OrdinalIgnoreCase)) return SymbolChatLog;
            else return string.Empty;
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.OldName))
            {
                var match = r_logFiltering.Match(e.OldName);
                if (match.Success)
                {
                    var span_type = match.Groups[1].ValueSpan;
                    string otherend = DetermineType(in span_type);
                    Indexing? indexing;
                    lock (this.cache)
                    {
                        if (!this.cache.TryGetValue(otherend, out indexing))
                        {
                            indexing = new Indexing(otherend, this, OnNewLogFound, this._dispatcher);
                            this.cache.Add(otherend, indexing);
                        }
                    }
                    lock (indexing)
                    {
                        var list = indexing.Items;
                        list.Remove(e.OldFullPath);
                        list.Add(e.OldFullPath);
                        list.Sort(FileDateComparer.Default);
                        indexing.Timer.Stop();
                        indexing.Timer.Start();
                    }
                }
            }
        }

        private static void OnNewLogFound(object? sender, EventArgs e)
        {
            if (sender is DispatcherTimer timer)
            {
                timer.Stop();
                if (timer.Tag is Indexing indexing)
                {
                    var categories = indexing.Parent;
                    categories.NewFileFound?.Invoke(categories, new NewFileFoundEventArgs(indexing.Name));
                }
            }
        }

        public async void StartWatching(NewFileFoundEventHandler callback)
        {
            this.NewFileFound += callback;
            await this.t_refresh;
            callback.Invoke(this, new NewFileFoundEventArgs(null));
            this.RequestActive();
        }

        public void StopWatching(NewFileFoundEventHandler callback)
        {
            this.NewFileFound -= callback;
            this.RequestDeactive();
        }

        protected override void OnActivation()
        {
            this.watcher.Path = Directory.CreateDirectory(LogDir).FullName;
            this.watcher.EnableRaisingEvents = true;
        }

        protected override void OnDeactivation()
        {
            // Once started, keep running until launcher's exit.
            // this.watcher.EnableRaisingEvents = false;
        }

        public void Dispose()
        {
            this.NewFileFound = null;
            this.watcher.EnableRaisingEvents = false;
            this.watcher.Dispose();
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Name))
            {
                var match = r_logFiltering.Match(e.Name);
                if (match.Success)
                {
                    var span_type = match.Groups[1].ValueSpan;
                    string otherend = DetermineType(in span_type);
                    Indexing? indexing;
                    lock (this.cache)
                    {
                        if (!this.cache.TryGetValue(otherend, out indexing))
                        {
                            indexing = new Indexing(otherend, this, OnNewLogFound, this._dispatcher);
                            this.cache.Add(otherend, indexing);
                        }
                    }
                    lock (indexing)
                    {
                        var list = indexing.Items;
                        if (list.Remove(e.FullPath))
                        {
                            list.Sort(FileDateComparer.Default);
                            indexing.Timer.Stop();
                            indexing.Timer.Start();
                        }
                    }
                }
            }
        }

        private NewFileFoundEventHandler? NewFileFound;

        private void Watcher_FileCreated(object sender, FileSystemEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Name))
            {
                var match = r_logFiltering.Match(e.Name);
                if (match.Success)
                {
                    var span_type = match.Groups[1].ValueSpan;
                    string otherend = DetermineType(in span_type);
                    Indexing? indexing;
                    lock (this.cache)
                    {
                        if (!this.cache.TryGetValue(otherend, out indexing))
                        {
                            indexing = new Indexing(otherend, this, OnNewLogFound, this._dispatcher);
                            this.cache.Add(otherend, indexing);
                        }
                    }
                    lock (indexing)
                    {
                        var list = indexing.Items;
                        if (!list.Contains(e.FullPath))
                        {
                            list.Add(e.FullPath);
                            list.Sort(FileDateComparer.Default);
                            indexing.Timer.Stop();
                            indexing.Timer.Start();
                        }
                    }
                }
            }
        }

        /// <summary>Select a certain log file from the given category.</summary>
        /// <param name="categoryName">The name of log category to get the log file.</param>
        /// <param name="index">The index of the log file from the category. If negative value is given, select index relative from the end of the colleciton.</param>
        /// <returns>A string which is a full path to the log file. Or null if the given <paramref name="categoryName"/> has no log file.</returns>
        public string? SelectLog(string categoryName, int index = -1)
        {
            Indexing? indexing;
            lock (this.cache)
            {
                if (!this.cache.TryGetValue(categoryName, out indexing))
                {
                    return null;
                }
            }
            string filepath;
            lock (indexing)
            {
                var items = indexing.Items;
                var count = items.Count;
                if (index < 0)
                {
                    index = count + index;
                }
                if (count == 0 || index < 0 || index >= count)
                {
                    return null;
                }
                else
                {
                    filepath = indexing.Items[index];
                }
            }
            return filepath;
        }

        private Task Refresh()
        {
            return Task.Run(() =>
            {
                if (Directory.Exists(LogDir))
                {
                    lock (this.cache)
                    {
                        this.cache.Clear();
                    }
                    var list = new HashSet<Indexing>(6);
                    foreach (var file in Directory.EnumerateFiles(LogDir, "*.txt", SearchOption.TopDirectoryOnly))
                    {
                        var span = Path.GetFileName(file.AsSpan());
                        var match = r_logFiltering.Match(file, file.Length - span.Length, span.Length);
                        if (match.Success)
                        {
                            var span_type = match.Groups[1].ValueSpan;
                            string otherend = DetermineType(in span_type);
                            Indexing? indexing;
                            lock (this.cache)
                            {
                                if (!this.cache.TryGetValue(otherend, out indexing))
                                {
                                    indexing = new Indexing(otherend, this, OnNewLogFound, this._dispatcher);
                                    this.cache.Add(otherend, indexing);
                                }
                                list.Add(indexing);
                            }
                            lock (indexing)
                            {
                                var items = indexing.Items;
                                if (!items.Contains(file))
                                {
                                    items.Add(file);
                                }
                            }
                            // match.Groups[1]
                        }
                    }
                    lock (this.cache)
                    {
                        this.cache.TrimExcess();
                    }
                    foreach (var indexing in list)
                    {
                        lock (indexing)
                        {
                            var items = indexing.Items;
                            if (items.Count != 0)
                            {
                                items.Sort(FileDateComparer.Default);
                            }
                        }
                    }
                    list.Clear();
                }
            });
        }

        sealed class Indexing
        {
            public readonly LogCategories Parent;
            public readonly List<string> Items;
            public readonly DispatcherTimer Timer;
            public readonly string Name;

            public Indexing(string name, LogCategories parent, EventHandler eh, Dispatcher dispatcher)
            {
                this.Name = name;
                this.Parent = parent;
                this.Items = new List<string>();
                this.Timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Normal, eh, dispatcher) { IsEnabled = false, Tag = this };
            }
        }

        sealed class FileDateComparer : IComparer<string>
        {
            public static readonly FileDateComparer Default = new FileDateComparer();
            public int Compare(string? x, string? y)
            {
                bool isnull_x = x == null, isnull_y = y == null;
                if (!isnull_x && !isnull_y)
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

                    // yyyymmdd
                    var date_x = new DateOnly(int.Parse(span_x.Slice(0, 4)), int.Parse(span_x.Slice(4, 2)), int.Parse(span_x.Slice(6, 2)));
                    var date_y = new DateOnly(int.Parse(span_y.Slice(0, 4)), int.Parse(span_y.Slice(4, 2)), int.Parse(span_y.Slice(6, 2)));

                    return date_x.CompareTo(date_y);
                }
                else if (isnull_x && isnull_y)
                {
                    return 0;
                }
                else if (isnull_x)
                {
                    return -1;
                }
                else if (isnull_y)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}

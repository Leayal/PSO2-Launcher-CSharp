using Leayal.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Leayal.PSO2Launcher.Toolbox
{
    /// <summary>A class to categorize the log files.</summary>
    public sealed class LogCategories : IDisposable
    {
        /// <summary>A string which is a fully qualified path to the typical PSO2 NGS's log directory.</summary>
        public static readonly string DefaultLogDirectoryPath = Path.GetFullPath(Path.Combine("SEGA", "PHANTASYSTARONLINE2", "log_ngs"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

        /// <summary>Default implementation of <seealso cref="LogCategories"/>. Created by using <seealso cref="DefaultLogDirectoryPath"/>.</summary>
        public static readonly Lazy<LogCategories> Default = new Lazy<LogCategories>(() => new LogCategories(DefaultLogDirectoryPath));

        private static readonly Regex r_logFiltering = new Regex(@"(\D*)Log(\d{8})_00\.txt", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>The log category name.</summary>
        /// <remarks>
        /// <para>This can be used to check for which log is found when the <seealso cref="NewFileFoundEventHandler"/> occurs.</para>
        /// <para>This can be used to requests path to a log file of the category with the method <seealso cref="SelectLog(string, int)"/>.</para>
        /// </remarks>
        public static readonly string ActionLog = "Action", ChatLog = "Chat", RewardLog = "Reward", StarGemLog = "StarGem", ScratchLog = "Scratch", SymbolChatLog = "SymbolChat";

        private readonly string logDir;
        private readonly FileSystemWatcher watcher;
        private readonly Dictionary<string, Indexing> cache;
        private readonly Task t_refresh;

        /// <summary>Creates a new instance of this class.</summary>
        /// <param name="logDirectoryPath">The path to the log directory. It should be a fully qualified path.</param>
        public LogCategories(string logDirectoryPath)
        {
            this.logDir = logDirectoryPath;
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
            this.t_refresh = Task.Run(this.InternalRefresh);
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
                            indexing = new Indexing(otherend, this);
                            this.cache.Add(otherend, indexing);
                        }
                    }
                    lock (indexing)
                    {
                        var list = indexing.Items;
                        list.Remove(e.OldFullPath);
                        list.Add(e.OldFullPath);
                        list.Sort(FileDateComparer.Default);
                    }
                    this.OnNewLogFound(indexing.Name);
                }
            }
        }

        private void OnNewLogFound(string? categoryName)
        {
            this.NewFileFound?.Invoke(this, new NewFileFoundEventArgs(categoryName));
        }

        /// <summary>Register <paramref name="callback"/> handler from the event which would occurs when a new log file is found.</summary>
        /// <param name="callback">The handler to register</param>
        /// <remarks>When call this method for the first time, it will also initialize the <seealso cref="FileSystemWatcher"/> to watch for new log file.</remarks>
        public async void StartWatching(NewFileFoundEventHandler callback)
        {
            this.NewFileFound += callback;
            await this.t_refresh;
            callback.Invoke(this, new NewFileFoundEventArgs(null));
            if (!this.watcher.EnableRaisingEvents)
            {
                this.watcher.Path = Directory.CreateDirectory(this.logDir).FullName;
                this.watcher.EnableRaisingEvents = true;
            }
        }

        /// <summary>Unregister <paramref name="callback"/> handler from the event which would occurs when a new log file is found.</summary>
        /// <param name="callback">The handler to unregister</param>
        /// <remarks>
        /// <para>This method will not stop the <seealso cref="FileSystemWatcher"/> started by <seealso cref="StartWatching(NewFileFoundEventHandler)"/>.</para>
        /// <para>If you want to stop, consider calling <seealso cref="Dispose"/> when you no longer use this instance.</para>
        /// </remarks>
        public void StopWatching(NewFileFoundEventHandler callback)
        {
            this.NewFileFound -= callback;
        }

        /// <summary>Stops watching the log directory for new files and clean up all resources allocated by this instance.</summary>
        public void Dispose()
        {
            this.NewFileFound = null;
            // this.watcher.EnableRaisingEvents = false;
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
                            indexing = new Indexing(otherend, this);
                            this.cache.Add(otherend, indexing);
                        }
                    }
                    bool changed;
                    lock (indexing)
                    {
                        var list = indexing.Items;
                        changed = list.Remove(e.FullPath);
                        if (changed)
                        {
                            list.Sort(FileDateComparer.Default);
                        }
                    }
                    if (changed)
                    {
                        this.OnNewLogFound(indexing.Name);
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
                            indexing = new Indexing(otherend, this);
                            this.cache.Add(otherend, indexing);
                        }
                    }
                    bool changed;
                    lock (indexing)
                    {
                        var list = indexing.Items;
                        if (!list.Contains(e.FullPath))
                        {
                            list.Add(e.FullPath);
                            list.Sort(FileDateComparer.Default);
                            changed = true;
                        }
                        else
                        {
                            changed = false;
                        }
                    }
                    if (changed)
                    {
                        this.OnNewLogFound(indexing.Name);
                    }
                }
            }
        }

        /// <summary>Select a certain log file from the given category.</summary>
        /// <param name="categoryName">The name of log category to get the log file. If you pass null or an empty string, it will return unexpected result (unknown log outside of the known categories).</param>
        /// <param name="index">The index of the log file from the category. If negative value is given, select index relative from the end of the colleciton.</param>
        /// <returns>A string which is a full path to the log file. Or null if the given <paramref name="categoryName"/> has no log file.</returns>
        public string? SelectLog(string categoryName, int index = -1)
        {
            if (categoryName == null)
            {
                categoryName = string.Empty;
            }
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

        private void InternalRefresh()
        {
            if (Directory.Exists(this.logDir))
            {
                lock (this.cache)
                {
                    this.cache.Clear();
                }
                var list = new HashSet<Indexing>(6);
                foreach (var file in Directory.EnumerateFiles(this.logDir, "*.txt", SearchOption.TopDirectoryOnly))
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
                                indexing = new Indexing(otherend, this);
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
        }

        sealed class Indexing
        {
            public readonly LogCategories Parent;
            public readonly List<string> Items;
            public readonly string Name;

            public Indexing(string name, LogCategories parent)
            {
                this.Name = name;
                this.Parent = parent;
                this.Items = new List<string>();
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

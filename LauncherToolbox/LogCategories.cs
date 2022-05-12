using Leayal.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace Leayal.PSO2Launcher.Toolbox
{
    /// <summary>A class to categorize the log files.</summary>
    public sealed class LogCategories : IDisposable
    {
        /// <summary>A string which is a fully qualified path to the typical PSO2 NGS's log directory.</summary>
        public static readonly string DefaultLogDirectoryPath = Path.GetFullPath(Path.Combine("SEGA", "PHANTASYSTARONLINE2", "log_ngs"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

        // <summary>Default implementation of <seealso cref="LogCategories"/>. Created by using <seealso cref="DefaultLogDirectoryPath"/>.</summary>
        // public static readonly Lazy<LogCategories> Default = new Lazy<LogCategories>(() => new LogCategories(DefaultLogDirectoryPath));

        private static readonly Regex r_logFiltering = new Regex(@"(\D*)Log(\d{8})_00\.txt", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>The log category name.</summary>
        /// <remarks>
        /// <para>This can be used to check for which log is found when the <seealso cref="NewFileFoundEventHandler"/> occurs.</para>
        /// <para>This can be used to requests path to a log file of the category with the method <seealso cref="SelectLog(string, int)"/>.</para>
        /// </remarks>
        public static readonly string ActionLog = "Action", ChatLog = "Chat", RewardLog = "Reward", StarGemLog = "StarGem", ScratchLog = "Scratch", SymbolChatLog = "SymbolChat";

        private readonly string logDir;
        private readonly FileSystemWatcher watcher;
        private readonly ConcurrentDictionary<string, LazyIndexing> cache;
        private readonly Task t_refresh;

        private readonly BlockingCollection<BufferData> buffering;
        private readonly PeriodicTimer bufferFlusher;

        private bool disposed;

        /// <summary>Creates a new instance of this class.</summary>
        /// <param name="logDirectoryPath">The path to the log directory. It should be a fully qualified path.</param>
        public LogCategories(string logDirectoryPath)
        {
            this.disposed = false;
            this.logDir = logDirectoryPath;
            this.bufferFlusher = new PeriodicTimer(TimeSpan.FromSeconds(1));
            this.buffering = new BlockingCollection<BufferData>();
            this.cache = new ConcurrentDictionary<string, LazyIndexing>(StringComparer.OrdinalIgnoreCase);
            this.watcher = new FileSystemWatcher();
            this.watcher.BeginInit();
            this.watcher.Path = string.Empty;
            this.watcher.Filter = "*.txt";
            this.watcher.SynchronizingObject = null;
            this.watcher.IncludeSubdirectories = false;
            this.watcher.NotifyFilter = NotifyFilters.FileName;
            this.watcher.Created += this.Watcher_FileCreated;
            this.watcher.Renamed += this.Watcher_Renamed;
            this.watcher.Deleted += this.Watcher_Deleted;
            this.watcher.EnableRaisingEvents = false;
            this.watcher.EndInit();
            this.t_refresh = Task.Run(this.InternalRefresh);
        }

        private static string DetermineType(in ReadOnlySpan<char> span)
        {
            if (MemoryExtensions.Equals(span, ActionLog, StringComparison.OrdinalIgnoreCase)) return ActionLog;
            else if (MemoryExtensions.Equals(span, ChatLog, StringComparison.OrdinalIgnoreCase)) return ChatLog;
            else if (MemoryExtensions.Equals(span, RewardLog, StringComparison.OrdinalIgnoreCase)) return RewardLog;
            else if (MemoryExtensions.Equals(span, StarGemLog, StringComparison.OrdinalIgnoreCase)) return StarGemLog;
            else if (MemoryExtensions.Equals(span, ScratchLog, StringComparison.OrdinalIgnoreCase)) return ScratchLog;
            else if (MemoryExtensions.Equals(span, SymbolChatLog, StringComparison.OrdinalIgnoreCase)) return SymbolChatLog;
            else return string.Empty;
        }

        sealed class LazyIndexing
        {
            private readonly string category;
            private readonly LogCategories parent;
            // private readonly Lazy<Indexing> lazy;

            private bool _isvaluecreated;
            private Indexing _value;
            private object? lockobj;

            public bool IsValueCreated => this._isvaluecreated;
            public Indexing Value => LazyInitializer.EnsureInitialized(ref this._value, ref this._isvaluecreated, ref this.lockobj, this.Create);

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public LazyIndexing(string logCategory, LogCategories parent)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            {
                this.lockobj = null;
                this.category = logCategory;
                this.parent = parent;
                // this.lazy = new Lazy<Indexing>(this.Create);
            }

            private Indexing Create() => new Indexing(this.category, this.parent);
        }

        private LazyIndexing Lazy_Indexing(string logCategory)
        {
            return new LazyIndexing(logCategory, this);
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.OldName))
            {
                var match = r_logFiltering.Match(e.OldName);
                if (match.Success)
                {
                    var span_type = match.Groups[1].ValueSpan;
                    this.buffering.Add(new BufferDataRename(DetermineType(in span_type), e.OldFullPath, e.FullPath));

                    /*
                    string otherend = DetermineType(in span_type);
                    var indexing = this.cache.GetOrAdd(otherend, this.Lazy_Indexing).Value;
                    var needLock = !indexing.lockobj.IsWriteLockHeld;
                    try
                    {
                        if (needLock)
                        {
                            indexing.lockobj.EnterWriteLock();
                        }
                        var list = indexing.Items;
                        list.Remove(e.OldFullPath);
                        list.Add(e.FullPath, e.FullPath);
                    }
                    finally
                    {
                        if (needLock)
                        {
                            indexing.lockobj.ExitWriteLock();
                        }
                    }
                    */
                }
            }
        }

        private void OnNewLogFound(string? categoryName)
        {
            ThreadPool.QueueUserWorkItem(this._OnNewLogFound, categoryName);
        }

        private void _OnNewLogFound(object? categoryNameObj)
        {
            if (categoryNameObj is string categoryName)
            {
                this.NewFileFound?.Invoke(this, new NewFileFoundEventArgs(categoryName));
            }
        }

        /// <summary>Register <paramref name="callback"/> handler from the event which would occurs when a new log file is found.</summary>
        /// <param name="callback">The handler to register</param>
        /// <remarks>When call this method for the first time, it will also initialize the <seealso cref="FileSystemWatcher"/> to watch for new log file.</remarks>
        public async Task StartWatching(NewFileFoundEventHandler callback)
        {
            await this.t_refresh;
            callback.Invoke(this, new NewFileFoundEventArgs(null));
            this.NewFileFound += callback;
        }

        /// <summary>Unregister <paramref name="callback"/> handler from the event which would occurs when a new log file is found.</summary>
        /// <param name="callback">The handler to unregister</param>
        /// <remarks>
        /// <para>This method will not stop the <seealso cref="FileSystemWatcher"/> started by <seealso cref="StartWatching(NewFileFoundEventHandler)"/>.</para>
        /// <para>If you want to stop, consider calling <seealso cref="Dispose()"/> when you no longer use this instance.</para>
        /// </remarks>
        public void StopWatching(NewFileFoundEventHandler callback)
        {
            this.NewFileFound -= callback;
        }

        /// <summary>Stops watching the log directory for new files and clean up all resources allocated by this instance.</summary>
        public void Dispose()
        {
            if (this.disposed) return;
            this.disposed = true;
            GC.SuppressFinalize(this);
            this.Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            this.NewFileFound = null;
            this.watcher.EnableRaisingEvents = false;
            this.watcher.Dispose();

            this.bufferFlusher.Dispose();
            this.buffering.CompleteAdding();
            this.buffering.Dispose();

            var arr = this.cache.Values.ToArray();
            this.cache.Clear();
            if (arr != null && arr.Length != 0)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    if (arr[i].IsValueCreated)
                    {
                        arr[i].Value.Dispose();
                    }
                }
                Array.Clear(arr);
            }
        }

        /// <summary>Destructor</summary>
        ~LogCategories()
        {
            this.Dispose(false);
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Name))
            {
                var match = r_logFiltering.Match(e.Name);
                if (match.Success)
                {
                    var span_type = match.Groups[1].ValueSpan;
                    this.buffering.Add(new BufferData(DetermineType(in span_type), e.FullPath, BufferDataAction.Remove));

                    /*
                    string otherend = DetermineType(in span_type);
                    var indexing = this.cache.GetOrAdd(otherend, this.Lazy_Indexing).Value;
                    bool changed;
                    var needLock = !indexing.lockobj.IsWriteLockHeld;
                    try
                    {
                        if (needLock)
                        {
                            indexing.lockobj.EnterWriteLock();
                        }
                        changed = indexing.Items.Remove(e.FullPath);
                    }
                    finally
                    {
                        if (needLock)
                        {
                            indexing.lockobj.ExitWriteLock();
                        }
                    }
                    if (changed)
                    {
                        this.OnNewLogFound(indexing.Name);
                    }
                    */
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
                    this.buffering.Add(new BufferData(DetermineType(in span_type), e.FullPath, BufferDataAction.Add));

                    /*
                    string otherend = DetermineType(in span_type);
                    var indexing = this.cache.GetOrAdd(otherend, this.Lazy_Indexing).Value;
                    bool changed;
                    var needLock = !indexing.lockobj.IsWriteLockHeld;
                    try
                    {
                        if (needLock)
                        {
                            indexing.lockobj.EnterWriteLock();
                        }
                        var list = indexing.Items;
                        if (!list.ContainsValue(e.FullPath))
                        {
                            list.Add(e.FullPath, e.FullPath);
                            changed = true;
                        }
                        else
                        {
                            changed = false;
                        }
                    }
                    finally
                    {
                        if (needLock)
                        {
                            indexing.lockobj.ExitWriteLock();
                        }
                    }
                    if (changed)
                    {
                        this.OnNewLogFound(indexing.Name);
                    }
                    */
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
            LazyIndexing? lazyindexing;
            if (!this.cache.TryGetValue(categoryName, out lazyindexing) || !lazyindexing.IsValueCreated)
            {
                return null;
            }
            var indexing = lazyindexing.Value;
            string filepath;

            var lockobj = indexing.lockobj;
            bool needlock = !lockobj.IsReadLockHeld && !lockobj.IsUpgradeableReadLockHeld && !lockobj.IsWriteLockHeld;
            try
            {
                if (needlock)
                {
                    indexing.lockobj.EnterReadLock();
                }
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
                    filepath = indexing.Items.Values[index];
                }
            }
            finally
            {
                if (needlock)
                {
                    indexing.lockobj.ExitReadLock();
                }
            }
            return filepath;
        }

        private void InternalRefresh()
        {
            if (Directory.Exists(this.logDir))
            {
                this.cache.Clear();
                foreach (var file in Directory.EnumerateFiles(this.logDir, "*.txt", SearchOption.TopDirectoryOnly))
                {
                    var span = Path.GetFileName(file.AsSpan());
                    var match = r_logFiltering.Match(file, file.Length - span.Length, span.Length);
                    if (match.Success)
                    {
                        var span_type = match.Groups[1].ValueSpan;
                        string otherend = DetermineType(in span_type);
                        var indexing = this.cache.GetOrAdd(otherend, this.Lazy_Indexing).Value;

                        var items = indexing.Items;
                        if (!items.ContainsValue(file))
                        {
                            items.Add(file, file);
                        }

                        /*
                        var lockobj = indexing.lockobj;
                        bool needlock = !lockobj.IsWriteLockHeld;
                        try
                        {
                            if (needlock)
                            {
                                lockobj.EnterWriteLock();
                            }
                            var items = indexing.Items;
                            if (!items.ContainsValue(file))
                            {
                                items.Add(file, file);
                            }
                        }
                        finally
                        {
                            if (needlock)
                            {
                                lockobj.ExitWriteLock();
                            }
                        }
                        */
                    }
                }

                this.watcher.Path = Directory.CreateDirectory(this.logDir).FullName;
                this.watcher.EnableRaisingEvents = true;

                Task.Factory.StartNew(this.InnerFlushBufferData, TaskCreationOptions.LongRunning);
            }
        }

        private async Task InnerFlushBufferData()
        {
            var locks = new HashSet<ReaderWriterLockSlim>();
            var changedOnes = new HashSet<string>();
            try
            {
                while (!this.disposed && await this.bufferFlusher.WaitForNextTickAsync())
                {
                    try
                    {
                        while (!this.disposed && this.buffering.TryTake(out var data))
                        {
                            if (data.Action == BufferDataAction.Remove)
                            {
                                if (this.cache.TryGetValue(data.Category, out var lazyindexing) && lazyindexing.IsValueCreated)
                                {
                                    var indexing = lazyindexing.Value;
                                    var lockobj = indexing.lockobj;
                                    if (!lockobj.IsWriteLockHeld)
                                    {
                                        lockobj.EnterWriteLock();
                                        locks.Add(lockobj);
                                    }
                                    var items = indexing.Items;
                                    if (items.Values.Remove(data.Filename))
                                    {
                                        changedOnes.Add(data.Category);
                                    }
                                }
                            }
                            else if (data.Action == BufferDataAction.Add)
                            {
                                var indexing = this.cache.GetOrAdd(data.Category, this.Lazy_Indexing).Value;
                                var lockobj = indexing.lockobj;
                                if (!lockobj.IsWriteLockHeld)
                                {
                                    lockobj.EnterWriteLock();
                                    locks.Add(lockobj);
                                }
                                var items = indexing.Items;
                                if (!items.ContainsValue(data.Filename))
                                {
                                    items.Add(data.Filename, data.Filename);
                                    changedOnes.Add(data.Category);
                                }
                            }
                            else if (data.Action == BufferDataAction.Rename)
                            {
                                var indexing = this.cache.GetOrAdd(data.Category, this.Lazy_Indexing).Value;
                                var lockobj = indexing.lockobj;
                                if (!lockobj.IsWriteLockHeld)
                                {
                                    lockobj.EnterWriteLock();
                                    locks.Add(lockobj);
                                }
                                var items = indexing.Items;
                                if (!items.ContainsValue(data.Filename))
                                {
                                    items.Add(data.Filename, data.Filename);
                                    changedOnes.Add(data.Category);
                                }
                                if (data is BufferDataRename renamebuffer && items.Values.Remove(renamebuffer.OldFilename))
                                {
                                    changedOnes.Add(data.Category);
                                }
                            }
                        }
                    }
                    finally
                    {
                        ReleaseLocks(locks);
                        foreach (var changedOne in changedOnes)
                        {
                            this.OnNewLogFound(changedOne);
                        }
                        changedOnes.Clear();
                    }
                }
            }
            catch (ObjectDisposedException)
            {

            }
            finally
            {
                ReleaseLocks(locks);
            }

            static void ReleaseLocks(HashSet<ReaderWriterLockSlim> locks)
            {
                foreach (var item in locks)
                {
                    if (item.IsWriteLockHeld)
                    {
                        item.ExitWriteLock();
                    }
                }
                locks.Clear();
            }
        }

        sealed class Indexing : IDisposable
        {
            public readonly LogCategories Parent;
            public readonly SortedList<string, string> Items;
            public readonly string Name;
            public readonly ReaderWriterLockSlim lockobj;

            public Indexing(string name, LogCategories parent)
            {
                this.Name = name;
                this.Parent = parent;
                this.Items = new SortedList<string, string>(FileDateComparer.Default);
                this.lockobj = new ReaderWriterLockSlim();
            }

            public void Dispose()
            {
                this.lockobj.Dispose();
            }
        }

        private class BufferData
        {
            public readonly string Filename, Category;
            public readonly BufferDataAction Action;

            public BufferData(string type, string filename, BufferDataAction action)
            {
                this.Category = type;
                this.Filename = filename;
                this.Action = action;
            }
        }

        private class BufferDataRename : BufferData
        {
            public readonly string OldFilename;

            public BufferDataRename(string type, string oldFilename, string newFilename) : base(type, newFilename, BufferDataAction.Rename)
            {
                this.OldFilename = oldFilename;
            }
        }

        private enum BufferDataAction
        {
            Add,
            Remove,
            Rename
        }

        sealed class FileDateComparer : IComparer<string>, IComparer<ReadOnlyMemory<char>>
        {
            public static readonly FileDateComparer Default = new FileDateComparer();

            public int Compare(string? x, string? y)
            {
                bool isnull_x = x == null, isnull_y = y == null;
                if (!isnull_x && !isnull_y)
                {
                    return this.Compare(x.AsSpan(), y.AsSpan());
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

            public int Compare(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y) => this.Compare(x.Span, y.Span);

            public int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
            {
                bool isnull_x = x.IsEmpty, isnull_y = y.IsEmpty;
                if (!isnull_x && !isnull_y)
                {
                    var span_x = Path.GetFileNameWithoutExtension(x);
                    var span_y = Path.GetFileNameWithoutExtension(y);
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

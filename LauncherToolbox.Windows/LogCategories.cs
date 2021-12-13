using Leayal.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Leayal.PSO2Launcher.Toolbox.Windows
{
    sealed class LogCategories : ActivationBasedObject, IDisposable
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
            this.watcher.Path = string.Empty;
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
            this.NewFileFound += callback;
            bool deferred = !this.t_refresh.IsCompleted;
            await this.t_refresh;
            this.RequestActive();
            if (deferred)
            {
                callback.Invoke(this, this.ActionLog);
                callback.Invoke(this, this.RewardLog);
            }
        }

        public void StopWatching(Action<LogCategories, List<string>> callback)
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
            this.watcher.EnableRaisingEvents = false;
        }

        public void Dispose()
        {
            this.NewFileFound = null;
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

                    // yyymmdd
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

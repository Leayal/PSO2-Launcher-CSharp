using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.IO;

namespace Leayal.PSO2Launcher.Core.Classes
{
    class BackgroundSelfUpdateChecker : IDisposable
    {
        private static readonly SendOrPostCallback Post_OnUpdateFound = new SendOrPostCallback(OnUpdateFound);
        private CancellationTokenSource cancelSrc_BackgroundSelfUpdateChecker;
        private readonly SynchronizationContext syncContext;
        private DateTime lastchecktime;
        private readonly HttpClient webclient;
        private BackgroundSelfUpdateChecker c;

        private TimeSpan _ticktime;
        public TimeSpan TickTime
        {
            get => this._ticktime;
            set
            {
                var onesec = TimeSpan.FromSeconds(1);
                if (value < onesec)
                {
                    this._ticktime = onesec;
                }
                else
                {
                    this._ticktime = value;
                }
            }
        }

        private readonly IReadOnlyDictionary<string, string> files;

        public BackgroundSelfUpdateChecker(HttpClient client, Dictionary<string, string> filelist)
        {
            this._ticktime = TimeSpan.FromSeconds(1);
            this.files = filelist;
            this.syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
            this.lastchecktime = DateTime.Now;

            this.webclient = client;
        }

        public void Start()
        {
            this.syncContext.Post(async (obj) =>
            {
                var myself = (BackgroundSelfUpdateChecker)obj;
                if (myself.cancelSrc_BackgroundSelfUpdateChecker == null)
                {
                    var cancelsrc = new CancellationTokenSource();
                    myself.cancelSrc_BackgroundSelfUpdateChecker = cancelsrc;
                    try
                    {
                        var canceltoken = cancelsrc.Token;
                        canceltoken.Register(cancelsrc.Dispose);
                        try
                        {
                            myself.lastchecktime = await Task.Factory.StartNew<Task<DateTime>>(new Func<object, Task<DateTime>>(async (obj) =>
                            {
                                var copied = canceltoken;
                                DateTime datetime;
                                if (obj is DateTime lastcheck)
                                {
                                    datetime = lastcheck;
                                }
                                else
                                {
                                    datetime = DateTime.Now;
                                }
                                while (!copied.IsCancellationRequested)
                                {
                                    await Task.Delay(myself._ticktime, copied);
                                    datetime = DateTime.Now;

                                    var remotelist = await myself.GetFileList();
                                    var theonewhoneedupdate = myself.CheckUpdate(remotelist);

                                    if (theonewhoneedupdate != null && theonewhoneedupdate.Count != 0)
                                    {
                                        var taskCompletionSource = new TaskCompletionSource(null);
                                        myself.syncContext.Post(Post_OnUpdateFound, new UpdateFoundData(myself, taskCompletionSource, theonewhoneedupdate));
                                        await taskCompletionSource.Task;
                                    }
                                }
                                return datetime;
                            }), myself.lastchecktime, canceltoken, TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current ?? TaskScheduler.Default).Unwrap();
                        }
                        catch (TaskCanceledException)
                        {
                        }
                    }
                    finally
                    {
                        myself.cancelSrc_BackgroundSelfUpdateChecker = null;
                        cancelsrc.Dispose();
                    }
                }
            }, this);
        }

        private async Task<IReadOnlyDictionary<string, string>> GetFileList()
        {
            var data = await this.webclient.GetStringAsync("https://leayal.github.io/PSO2-Launcher-CSharp/publish/update.json");
            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (var document = JsonDocument.Parse(data))
            {
                var rootelement = document.RootElement;

                if (rootelement.TryGetProperty("critical-files", out var prop_criticalfiles))
                {
                    if (prop_criticalfiles.ValueKind == JsonValueKind.Object)
                    {
                        if (rootelement.TryGetProperty("root-url-critical", out var prop_rootUrl) && prop_rootUrl.ValueKind == JsonValueKind.String &&
                            Uri.TryCreate(prop_rootUrl.GetString(), UriKind.Absolute, out var rootUrl))
                        {
                            using (var objWalker = prop_criticalfiles.EnumerateObject())
                            {
                                while (objWalker.MoveNext())
                                {
                                    var item = objWalker.Current;
                                    if (item.Value.TryGetProperty("sha1", out var item_prop_sha1) && item_prop_sha1.ValueKind == JsonValueKind.String)
                                    {
                                        dictionary.Add(item.Name, item_prop_sha1.GetString());
                                    }
                                }
                            }
                        }
                    }
                }

                if (rootelement.TryGetProperty("files", out var prop_files))
                {
                    if (prop_files.ValueKind == JsonValueKind.Object)
                    {
                        if (rootelement.TryGetProperty("root-url", out var prop_rootUrl) && prop_rootUrl.ValueKind == JsonValueKind.String &&
                            Uri.TryCreate(prop_rootUrl.GetString(), UriKind.Absolute, out var rootUrl))
                        {
                            using (var objWalker = prop_files.EnumerateObject())
                            {
                                while (objWalker.MoveNext())
                                {
                                    var item = objWalker.Current;
                                    if (item.Value.TryGetProperty("sha1", out var item_prop_sha1) && item_prop_sha1.ValueKind == JsonValueKind.String)
                                    {
                                        var item_name = item.Name;
                                        if (!dictionary.ContainsKey(item_name))
                                        {
                                            dictionary.Add(item_name, item_prop_sha1.GetString());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return dictionary;
        }

        private List<string> CheckUpdate(IReadOnlyDictionary<string, string> checkingfiles)
        {
            var result = new List<string>(checkingfiles.Count);
            foreach (var item in checkingfiles)
            {
                if (!this.files.TryGetValue(item.Key, out var current_sha1) || !string.Equals(item.Value, current_sha1, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(item.Key);
                }
            }
            return result;
        }

        private static void OnUpdateFound(object obj)
        {
            if (obj is UpdateFoundData data)
            {
                var sender = data.Sender;
                var tasksrc = data.TaskSource;
                try
                {
                    sender.UpdateFound?.Invoke(sender, data.NeedUpdated);
                    tasksrc.SetResult();
                }
                catch (Exception ex)
                {
                    tasksrc.SetException(ex);
                }
            }
        }

        public event Func<BackgroundSelfUpdateChecker, IReadOnlyList<string>, Task> UpdateFound;

        class UpdateFoundData
        {
            public readonly BackgroundSelfUpdateChecker Sender;
            public readonly TaskCompletionSource TaskSource;
            public readonly List<string> NeedUpdated;

            public UpdateFoundData(BackgroundSelfUpdateChecker sender, TaskCompletionSource tasksrc, List<string> _needupdated)
            {
                this.Sender = sender;
                this.TaskSource = tasksrc;
                this.NeedUpdated = _needupdated;
            }
        }

        public void Stop() => this.syncContext.Post(this.InnerStop, null);

        private void InnerStop(object sender)
        {
            this.cancelSrc_BackgroundSelfUpdateChecker?.Cancel();
        }

        public void Dispose()
        {
            this.Stop();
        }
    }
}

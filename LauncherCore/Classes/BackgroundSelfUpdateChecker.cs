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
using System.Runtime.InteropServices;

namespace Leayal.PSO2Launcher.Core.Classes
{
    class BackgroundSelfUpdateChecker : IDisposable
    {
        private CancellationTokenSource cancelSrc_BackgroundSelfUpdateChecker;
        private DateTime lastchecktime;
        private readonly HttpClient webclient;
        private readonly static Architecture __arch = RuntimeInformation.ProcessArchitecture;
        private int _state;
        private readonly CancellationToken appExit;

        private TimeSpan _ticktime;
        public TimeSpan TickTime
        {
            get => this._ticktime;
            set
            {
                var tensec = TimeSpan.FromSeconds(10);
                if (value < tensec)
                {
                    value = tensec;
                }
                if (value != this._ticktime)
                {
                    this._ticktime = value;
                }
            }
        }

        private readonly IReadOnlyDictionary<string, string> files;

        public BackgroundSelfUpdateChecker(in CancellationToken cancellationToken, HttpClient client, Dictionary<string, string> filelist)
        {
            this.appExit = cancellationToken;
            this.files = filelist;
            this.lastchecktime = DateTime.Now;
            this.webclient = client;

            this._ticktime = TimeSpan.FromSeconds(1);
        }

        public async void Start()
        {
            if (Interlocked.CompareExchange(ref this._state, 1, 0) == 0)
            {
                var newCancel = CancellationTokenSource.CreateLinkedTokenSource(this.appExit);
                Interlocked.Exchange(ref this.cancelSrc_BackgroundSelfUpdateChecker, newCancel)?.Dispose();
                var cancelToken = newCancel.Token;

                if ((DateTime.Now - this.lastchecktime) >= this._ticktime)
                {
                    await this.TimerTicked().ConfigureAwait(false);
                }

                if (!cancelToken.IsCancellationRequested)
                {
                    using (var timer = new PeriodicTimerWithoutException(this._ticktime))
                    {
                        while (!cancelToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancelToken).ConfigureAwait(false))
                        {
                            await this.TimerTicked().ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        private async ValueTask TimerTicked()
        {
            this.lastchecktime = DateTime.Now;

            var remotelist = await this.GetFileList().ConfigureAwait(false);
            var theonewhoneedupdate = this.CheckUpdate(remotelist);

            if (theonewhoneedupdate != null && theonewhoneedupdate.Count != 0)
            {
                this.UpdateFound?.Invoke(this, theonewhoneedupdate);
            }
        }

        private async Task<IReadOnlyDictionary<string, string>> GetFileList()
        {
            var data = await this.webclient.GetStringAsync("https://leayal.github.io/PSO2-Launcher-CSharp/publish/v6/update.json").ConfigureAwait(false);
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
                                    var value = item.Value;
                                    if (value.TryGetProperty("cpu", out var item_cpu) && item_cpu.ValueKind == JsonValueKind.String)
                                    {
                                        if (string.Equals(item_cpu.GetString(), "x86", StringComparison.OrdinalIgnoreCase) && __arch != Architecture.X86)
                                        {
                                            continue;
                                        }
                                        else if (string.Equals(item_cpu.GetString(), "x64", StringComparison.OrdinalIgnoreCase) && __arch != Architecture.X64)
                                        {
                                            continue;
                                        }
                                        else if (string.Equals(item_cpu.GetString(), "arm64", StringComparison.OrdinalIgnoreCase) && __arch != Architecture.Arm64)
                                        {
                                            continue;
                                        }
                                        else if (string.Equals(item_cpu.GetString(), "arm", StringComparison.OrdinalIgnoreCase) && __arch != Architecture.Arm)
                                        {
                                            continue;
                                        }
                                    }
                                    if (value.TryGetProperty("sha1", out var item_prop_sha1) && item_prop_sha1.ValueKind == JsonValueKind.String)
                                    {
                                        var item_name = item.Name.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                                        if (item_name.IndexOf(Path.AltDirectorySeparatorChar) != -1)
                                        {
                                            item_name = item_name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                                        }
                                        dictionary.Add(item_name, item_prop_sha1.GetString());
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
                                    var value = item.Value;
                                    if (value.TryGetProperty("cpu", out var item_cpu) && item_cpu.ValueKind == JsonValueKind.String)
                                    {
                                        if (string.Equals(item_cpu.GetString(), "x86", StringComparison.OrdinalIgnoreCase) && __arch != Architecture.X86)
                                        {
                                            continue;
                                        }
                                        else if (string.Equals(item_cpu.GetString(), "x64", StringComparison.OrdinalIgnoreCase) && __arch != Architecture.X64)
                                        {
                                            continue;
                                        }
                                        else if (string.Equals(item_cpu.GetString(), "arm64", StringComparison.OrdinalIgnoreCase) && __arch != Architecture.Arm64)
                                        {
                                            continue;
                                        }
                                        else if (string.Equals(item_cpu.GetString(), "arm", StringComparison.OrdinalIgnoreCase) && __arch != Architecture.Arm)
                                        {
                                            continue;
                                        }
                                    }
                                    if (value.TryGetProperty("sha1", out var item_prop_sha1) && item_prop_sha1.ValueKind == JsonValueKind.String)
                                    {
                                        var item_name = item.Name.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                                        if (item_name.IndexOf(Path.AltDirectorySeparatorChar) != -1)
                                        {
                                            item_name = item_name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                                        }
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

        public event Action<BackgroundSelfUpdateChecker, IReadOnlyList<string>> UpdateFound;

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref this._state, 0, 1) == 1)
            {
                this.cancelSrc_BackgroundSelfUpdateChecker?.Cancel();
            }  
        }

        public void Dispose()
        {
            this.Stop();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Net.Http;
using System.Collections.ObjectModel;

namespace Leayal.PSO2Launcher.RSS
{
    public abstract class RSSFeed
    {
        protected readonly IRSSLoader Loader;
        private string displayname;
        private CancellationTokenSource cancelSrc;
        internal HttpClient webClient;

        protected readonly string WorkspaceDirectory;
        protected readonly string CacheDataDirectory;

        private int flag_fetch, flag_event;
        private volatile Task<List<RSSFeedItem>> t_fetch;
        protected HttpClient HttpClient => this.webClient;

        public string FeedDisplayName => this.displayname;
        public event RSSFeedDisplayNameChangedEventHandler DisplayNameChanged;
        protected void SetDisplayName(string displayname)
        {
            if (!string.Equals(this.displayname, displayname, StringComparison.Ordinal))
            {
                this.displayname = displayname;
                this.DisplayNameChanged?.Invoke(this, new RSSFeedDisplayNameChangedEventArgs(this.displayname));
            }
        }

        private Stream _displayImageStream;
        public Stream DisplayImageStream => this._displayImageStream;
        public event RSSFeedDisplayImageChangedEventHandler DisplayImageChanged;
        protected void SetDisplayImage(Stream imageStream)
        {
            this._displayImageStream = imageStream;
            this.DisplayImageChanged?.Invoke(this, new RSSFeedDisplayImageChangedEventArgs(imageStream));
        }

        protected RSSFeed(IRSSLoader loader, string uniquename)
        {
            this.t_fetch = null;
            this.flag_fetch = 0;
            this.flag_event = 0;
            this.WorkspaceDirectory = Path.GetFullPath(Path.Combine("rss", "data", uniquename), SharedInterfaces.RuntimeValues.RootDirectory);
            this.CacheDataDirectory = Path.Combine(this.WorkspaceDirectory, "cache");
            Directory.CreateDirectory(this.CacheDataDirectory);
            this.Loader = loader;
        }

        /// <summary>When overriden, this method should contain code to re-fetch, re-parse the RSS Feed(s).</summary>
        /// <remarks>This method is called when <seealso cref="Refresh(in DateTime)"/> is called.</remarks>
        /// <param name="datetime">The nearest <seealso cref="DateTime"/> object where the refresh is "needed"</param>
        protected abstract Task OnRefresh(DateTime dateTime);

        /// <summary>Force a feed refresh.</summary>
        /// <remarks>Calling this method within <seealso cref="OnRefresh(in DateTime)"/> will cause an infinite loop. Hence, app crash.</remarks>
        /// <param name="datetime">The nearest <seealso cref="DateTime"/> object where the refresh is "needed"</param>
        protected async Task Refresh(DateTime datetime)
        {
            await this.OnRefresh(datetime);
        }

        private const int BeaconTickMS = 500;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timespan"></param>
        protected void SetNextRefesh(TimeSpan timespan)
        {
            if (timespan == TimeSpan.Zero)
            {
                var cancel = this.cancelSrc;
                this.cancelSrc = null;
                cancel?.Cancel();
            }
            else
            {
                var src = new CancellationTokenSource();
                this.cancelSrc?.Cancel();
                this.cancelSrc = src;
                Task.Run(async () =>
                {
                    var total = Convert.ToInt64(timespan.TotalMilliseconds);
                    while (total > 0)
                    {
                        if (src.IsCancellationRequested)
                        {
                            break;
                        }
                        if (total >= BeaconTickMS)
                        {
                            total -= BeaconTickMS;
                            await Task.Delay(BeaconTickMS);
                        }
                        else
                        {
                            var convert = Convert.ToInt32(total);
                            total -= convert;
                            await Task.Delay(convert);
                        }
                    }
                    if (!src.IsCancellationRequested)
                    {
                        await this.Refresh(DateTime.Now);
                    }
                }, src.Token);
            }
        }

        public async Task Fetch()
        {
            var f_fetch = Interlocked.CompareExchange(ref this.flag_fetch, 1, 0);
            if (f_fetch == 0)
            {
                Interlocked.Exchange(ref this.flag_event, 1);
                this.t_fetch = InnerFetch();
                var result = await this.t_fetch;
                Interlocked.Exchange(ref this.flag_fetch, 0);
                this.FeedUpdated?.Invoke(this, new RSSFeedUpdatedEventArgs(result));
                Interlocked.Exchange(ref this.flag_event, 0);
            }
            else if (f_fetch == 1)
            {
                if (Interlocked.CompareExchange(ref this.flag_event, 1, 0) == 0)
                {
                    _ = this.t_fetch.ContinueWith(t =>
                    {
                        if (!t.IsCanceled && t.IsCompleted)
                        {
                            this.FeedUpdated?.Invoke(this, new RSSFeedUpdatedEventArgs(t.Result));
                            Interlocked.Exchange(ref this.flag_event, 0);
                        }
                    });
                }
                await this.t_fetch;
            }
            else
            {
                await this.t_fetch;
            }
        }

        private async Task<List<RSSFeedItem>> InnerFetch()
        {
            var fetched = await this.FetchFeed();
            if (fetched != null && fetched.Count != 0)
            {
                var list = new List<RSSFeedItem>(fetched.Count);
                foreach (var item in fetched)
                {
                    var feeditem = this.OnCreateRSSFeedItem(item.Key, item.Value);
                    list.Add(feeditem);
                }
                return list;
            }
            else
            {
                return null;
            }
        }

        public async Task<IReadOnlyList<RSSFeedItem>> GetPreviousFeedItems()
        {
            var t = this.t_fetch;
            if (t != null)
            {
                return await t;
            }
            else
            {
                return new ReadOnlyCollection<RSSFeedItem>(Array.Empty<RSSFeedItem>());
            }
        }

        public event RSSFeedUpdatedEventHandler FeedUpdated;

        /// <summary>When overriden, it should contain code to fetch a raw rss feed data from a remote source.</summary>
        protected abstract Task<IReadOnlyDictionary<string, Uri>> FetchFeed();

        protected virtual RSSFeedItem OnCreateRSSFeedItem(string displayName, Uri url)
        {
            return new GenericRSSFeedItem(this, displayName, url);
        }
    }
}

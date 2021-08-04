using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Net.Http;
using System.Collections.ObjectModel;
using System.Xml;
using System.Xml.XPath;

namespace Leayal.PSO2Launcher.RSS
{
    public abstract class RSSFeedHandler : IDisposable, IRSSFeedChannelParser, IRSSFeedItemCreator, IRSSFeedChannelDownloader
    {
        public static readonly RSSFeedHandler Default = new DefaultRSSFeedHandler(null);

        internal IRSSLoader loader;
        internal HttpClient webClient;

        private string displayname;
        private CancellationTokenSource cancelSrc;
        private readonly Uri _feedchannelUrl;

        protected readonly string WorkspaceDirectory;
        protected readonly string CacheDataDirectory;

        private int flag_fetch, flag_event;
        private Task<List<RSSFeedItem>> t_fetch;
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

        public RSSFeedHandler(Uri feedchannelUrl)
        {
            this._feedchannelUrl = feedchannelUrl;
            this.t_fetch = null;
            this.flag_fetch = 0;
            this.flag_event = 0;
            this.WorkspaceDirectory = Path.GetFullPath(Path.Combine("rss", "data", this.GetType().FullName), SharedInterfaces.RuntimeValues.RootDirectory);
            this.CacheDataDirectory = Path.Combine(this.WorkspaceDirectory, "cache");
            Directory.CreateDirectory(this.CacheDataDirectory);
        }

        /// <summary>When overriden, this method should contain code to re-fetch, re-parse the RSS Feed(s).</summary>
        /// <remarks>This method is called when <seealso cref="Refresh(in DateTime)"/> is called.</remarks>
        /// <param name="datetime">The nearest <seealso cref="DateTime"/> object where the refresh is "needed"</param>
        protected virtual async Task OnRefresh(DateTime dateTime)
        {
            await this.Fetch();
        }

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
            var data = await this.DownloadFeedChannel(this.HttpClient, this._feedchannelUrl);
            var fetched = await this.ParseFeedChannel(data);
            if (fetched != null && fetched.Count != 0)
            {
                var list = new List<RSSFeedItem>(fetched.Count);
                foreach (var item in fetched)
                {
                    var feeditem = this.CreateFeedItem(in item);
                    if (feeditem != null)
                    {
                        list.Add(feeditem);
                    }
                }
                return list;
            }
            else
            {
                return null;
            }
        }

        public virtual async Task<IReadOnlyList<RSSFeedItem>> GetPreviousFeedItems()
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

        public void Dispose()
        {
            this.SetNextRefesh(TimeSpan.Zero);
        }

        public Task<string> DownloadFeedChannel(HttpClient webclient, Uri feedchannelUrl)
            => this.OnDownloadFeedChannel(webclient, feedchannelUrl);

        protected abstract Task<string> OnDownloadFeedChannel(HttpClient webclient, Uri feedchannelUrl);

        public Task<IReadOnlyList<FeedItemData>> ParseFeedChannel(string data)
            => this.OnParseFeedChannel(data);

        protected abstract Task<IReadOnlyList<FeedItemData>> OnParseFeedChannel(string data);

        public event RSSFeedUpdatedEventHandler FeedUpdated;

        public RSSFeedItem CreateFeedItem(in FeedItemData feeditemdata)
            => this.OnCreateFeedItem(in feeditemdata);

        protected abstract RSSFeedItem OnCreateFeedItem(in FeedItemData feeditemdata);

        protected static IDictionary<string, string> GetNamespacesInScope(XmlNode xDoc)
        {
            IDictionary<string, string> AllNamespaces = new Dictionary<string, string>();
            IDictionary<string, string> localNamespaces;

            XmlNode temp = xDoc;
            XPathNavigator xNav;
            while (temp.ParentNode != null)
            {
                xNav = temp.CreateNavigator();
                localNamespaces = xNav.GetNamespacesInScope(XmlNamespaceScope.Local);
                foreach (var item in localNamespaces)
                {
                    if (!AllNamespaces.ContainsKey(item.Key))
                    {
                        AllNamespaces.Add(item);
                    }
                }
                temp = temp.ParentNode;
            }
            return AllNamespaces;
        }

        /// <summary>When overriden, contains code to determine whether the plugin can handle parsing this feed's data.</summary>
        /// <param name="url">The url to check for.</param>
        /// <returns>
        /// <para>True - The plugin can handle.</para>
        /// <para>False - The plugin can not handle.</para>
        /// </returns>
        public abstract bool CanHandleParseFeedData(Uri url);

        /// <summary>When overriden, contains code to determine whether the plugin can handle this feed's <seealso cref="RSSFeedItem"/> creation(s).</summary>
        /// <param name="url">The url to check for.</param>
        /// <returns>
        /// <para>True - The plugin can handle.</para>
        /// <para>False - The plugin can not handle.</para>
        /// </returns>
        public abstract bool CanHandleFeedItemCreation(Uri url);

        /// <summary>When overriden, contains code to determine whether the plugin can handle this feed.</summary>
        /// <param name="url">The url to check for.</param>
        /// <returns>
        /// <para>True - The plugin can handle.</para>
        /// <para>False - The plugin can not handle.</para>
        /// </returns>
        public abstract bool CanHandleDownloadChannel(Uri url);
    }
}

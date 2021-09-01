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

        public static bool IsDefault(RSSFeedHandler handler) => (handler is DefaultRSSFeedHandler);
        public static bool IsGeneric(RSSFeedHandler handler) => (handler is GenericRSSFeedHandler);

        internal RSSLoader loader;

        private string displayname;
        private CancellationTokenSource cancelSrc;
        private readonly Uri _feedchannelUrl;

        protected readonly string WorkspaceDirectory;
        protected readonly string CacheDataDirectory;
        protected readonly char DefaultRepresentativeCharacter;

        internal char GetDefaultRepresentativeCharacter() => this.DefaultRepresentativeCharacter;

        private int flag_fetch, flag_event, flag_pendingrefresh, flag_isinrefresh;
        private Task<List<RSSFeedItem>> t_fetch;
        protected HttpClient HttpClient => this.loader.webclient;

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

        private char _representativeCharacter;
        public char RepresentativeCharacter => this._representativeCharacter;
        private Stream _displayImageStream;
        public Stream DisplayImageStream => this._displayImageStream;
        public event RSSFeedDisplayImageChangedEventHandler DisplayImageChanged;
        protected void SetDisplayImage(char representativeCharacter, Stream imageStream)
        {
            this._representativeCharacter = representativeCharacter;
            this._displayImageStream = imageStream;
            this.DisplayImageChanged?.Invoke(this, new RSSFeedDisplayImageChangedEventArgs(representativeCharacter, imageStream));
        }

        protected void SetDisplayImage(Stream imageStream) => this.SetDisplayImage(this.DefaultRepresentativeCharacter, imageStream);

        protected void SetDisplayImage(char representativeCharacter)
        {
            this._representativeCharacter = representativeCharacter;
            this._displayImageStream?.Dispose();
            this._displayImageStream = null;
            this.DisplayImageChanged?.Invoke(this, new RSSFeedDisplayImageChangedEventArgs(representativeCharacter, null));
        }

        public Uri FeedChannelUrl => this._feedchannelUrl;

        public bool DeferRefresh { get; set; }

        internal RSSFeedHandler(Uri feedchannelUrl, bool createworkspace)
        {
            this._feedchannelUrl = feedchannelUrl;
            this.t_fetch = null;
            this.flag_fetch = 0;
            this.flag_event = 0;
            this.flag_isinrefresh = 0;
            this.flag_pendingrefresh = 0;
            this.DefaultRepresentativeCharacter = GetRepresentativeCharacterFromHostName(feedchannelUrl);

            this.SetDisplayImage(this.DefaultRepresentativeCharacter);

            if (createworkspace)
            {
                this.WorkspaceDirectory = Path.GetFullPath(Path.Combine("data", "rss", this.GetType().FullName, Shared.Sha1StringHelper.GenerateFromString(feedchannelUrl.IsAbsoluteUri ? feedchannelUrl.AbsoluteUri : feedchannelUrl.ToString())), SharedInterfaces.RuntimeValues.RootDirectory);
                this.CacheDataDirectory = Path.Combine(this.WorkspaceDirectory, "cache");
                Directory.CreateDirectory(this.CacheDataDirectory);
            }
            else
            {
                this.WorkspaceDirectory = null;
                this.CacheDataDirectory = null;
            }
        }

        public RSSFeedHandler(Uri feedchannelUrl) : this(feedchannelUrl, true) { }

        /// <summary>When overriden, this method should contain code to re-fetch, re-parse the RSS Feed(s).</summary>
        /// <remarks>This method is called when <seealso cref="Refresh(in DateTime)"/> is called.</remarks>
        /// <param name="dateTime">The nearest <seealso cref="DateTime"/> object where the refresh is "needed"</param>
        protected virtual async Task OnRefresh(DateTime dateTime)
        {
            if (Interlocked.CompareExchange(ref this.flag_isinrefresh, 1, 0) == 0)
            {
                await Task.Run(this.Fetch);
                Interlocked.Exchange(ref this.flag_isinrefresh, 0);
            }
        }

        /// <summary>Perform a refresh if there's pending one. Otherwise does nothing.</summary>
        public async Task Refresh()
        {
            if (Interlocked.CompareExchange(ref this.flag_pendingrefresh, 0, 1) == 1)
            {
                await this.OnRefresh(DateTime.Now);
            }
        }

        public event EventHandler DeferredRefreshReady;

        // private const int BeaconTickMS = (int.MaxValue - 1);

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
                this.cancelSrc?.Dispose();
                this.cancelSrc = src;
                var canceltoken = src.Token;
                Task.Factory.StartNew(async () =>
                {
                    var copiedtoken = canceltoken;
                    var total = Convert.ToInt64(timespan.TotalMilliseconds);
                    await Task.Delay(timespan, copiedtoken);
                    if (!copiedtoken.IsCancellationRequested)
                    {
                        bool deferred = this.DeferRefresh;
                        if (Interlocked.CompareExchange(ref this.flag_pendingrefresh, 1, 0) == 0)
                        {
                            if (deferred)
                            {
                                this.DeferredRefreshReady?.Invoke(this, EventArgs.Empty);
                            }
                            else
                            {
                                await this.Refresh();
                            }
                        }
                    }
                }, canceltoken, TaskCreationOptions.LongRunning, TaskScheduler.Current ?? TaskScheduler.Default);
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
            string data;

            while (true)
            {
                try
                {
                    data = await this.DownloadFeedChannel(this.HttpClient, this._feedchannelUrl).ConfigureAwait(false);
                    break;
                }
                catch (HttpRequestException)
                {
                    // Wait for 5 minutes before retry
                    await Task.Delay(TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                }
                catch (System.Net.WebException)
                {
                    // Wait for 5 minutes before retry
                    await Task.Delay(TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                }
                catch (Exception) { data = null; break; } // should include TaskCanceledException
            }

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

        protected static char GetRepresentativeCharacterFromHostName(Uri url)
        {
            if (url != null && url.IsAbsoluteUri && url.HostNameType == UriHostNameType.Dns)
            {
                const string worldwideweb = "www.";
                var str = url.Host;
                if (str.StartsWith(worldwideweb))
                {
                    return char.ToUpper(str[worldwideweb.Length]);
                }
                else
                {
                    return char.ToUpper(str[0]);
                }
            }
            else
            {
                return '?';
            }
        }
    }
}

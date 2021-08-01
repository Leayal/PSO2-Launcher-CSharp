using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Threading.Tasks;
using System.Timers;
using System.Net.Http;

namespace Leayal.PSO2Launcher.RSS
{
    public abstract class RSSFeed
    {
        protected readonly IRSSLoader Loader;
        private readonly Timer refreshtimer;
        private string displayname;
        private HttpClient webClient;

        protected readonly string WorkspaceDirectory;
        protected readonly string CacheDataDirectory;

        protected HttpClient HttpClient => this.webClient;

        private readonly List<RSSFeedItem> _feedItems;
        public IReadOnlyList<RSSFeedItem> FeedItems => this._feedItems;

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

        public event RSSFeedDisplayImageChangedEventHandler DisplayImageChanged;
        protected void SetDisplayImage(Stream imageStream)
        {
            if (!string.Equals(this.displayname, displayname, StringComparison.Ordinal))
            {
                this.DisplayImageChanged?.Invoke(this, new RSSFeedDisplayImageChangedEventArgs(imageStream));
            }
        }

        private RSSFeed()
        {
            this.refreshtimer = new Timer() { AutoReset = true, Enabled = false };
            this.refreshtimer.Elapsed += this.Refreshtimer_Elapsed;
        }

        protected RSSFeed(IRSSLoader loader, string uniquename) : base()
        {
            this.WorkspaceDirectory = Path.GetFullPath(Path.Combine("rss", uniquename), SharedInterfaces.RuntimeValues.RootDirectory);
            this.CacheDataDirectory = Path.Combine(this.WorkspaceDirectory, "cache");
            this.Loader = loader;
        }

        private async void Refreshtimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await this.Refresh(e.SignalTime);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timespan"></param>
        protected void SetRefeshTimer(TimeSpan timespan)
        {
            if (timespan == TimeSpan.Zero)
            {
                if (this.refreshtimer.Enabled)
                {
                    this.refreshtimer.Stop();
                }
                this.refreshtimer.Stop();
            }
            else
            {
                this.refreshtimer.Interval = timespan.TotalMilliseconds;
                if (!this.refreshtimer.Enabled)
                {
                    this.refreshtimer.Start();
                }
            }
        }

        public async Task Fetch()
        {
            var fetched = await this.FetchFeed();
            this._feedItems.Clear();
            if (fetched != null && fetched.Count != 0)
            {
                foreach (var item in fetched)
                {
                    var feeditem = this.OnCreateRSSFeedItem(item.Key, item.Value);
                    this._feedItems.Add(feeditem);
                }
            }
            this.FeedUpdated?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler FeedUpdated;

        /// <summary>When overriden, it should contain code to fetch a raw rss feed data from a remote source.</summary>
        protected abstract Task<IReadOnlyDictionary<string, Uri>> FetchFeed();

        protected virtual RSSFeedItem OnCreateRSSFeedItem(string displayName, Uri url)
        {
            return new GenericRSSFeedItem(this, displayName, url);
        }
    }
}

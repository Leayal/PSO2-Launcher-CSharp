using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.RSS
{
    public abstract class RSSFeedItem
    {
        private readonly RSSFeed feed;

        public virtual string DisplayName { get; }

        public virtual Uri Url { get; }

        protected RSSFeedItem(RSSFeed feed, string displayName, Uri url)
        {
            this.feed = feed;
            this.DisplayName = displayName;
            this.Url = url;
        }

        public event RSSFeedItemClickEventHandler Click;

        public void PerformClick() => this.OnClick();

        protected virtual void OnClick()
        {
            this.Click?.Invoke(this, new RSSFeedItemClickEventArgs(this.feed));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.RSS
{
    public abstract class RSSFeedItem
    {
        private readonly RSSFeedHandler feed;

        public virtual string DisplayName { get; }

        public virtual Uri Url { get; }

        public virtual string ShortDescription { get; }

        public virtual DateTime? PublishDate { get; }

        protected RSSFeedItem(RSSFeedHandler feed, string displayName, string shortdescription, Uri url, DateTime? publishdate)
        {
            this.feed = feed;
            this.DisplayName = displayName;
            this.Url = url;
            this.ShortDescription = shortdescription;
            this.PublishDate = publishdate;
        }

        public event RSSFeedItemClickEventHandler Click;

        public void PerformClick() => this.OnClick();

        protected virtual void OnClick()
        {
            this.Click?.Invoke(this, new RSSFeedItemClickEventArgs(this.feed));
        }
    }
}

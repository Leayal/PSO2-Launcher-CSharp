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

        /*
        private event RSSFeedItemClickEventHandler _click;

        // Avoid re-adding the same delegate
        public event RSSFeedItemClickEventHandler Click
        {
            add
            {
                if (Array.IndexOf(this._click.GetInvocationList(), value) == -1)
                {
                    this._click += value;
                }
            }
            remove
            {
                this._click -= value;
            }
        }
        */

        // This is extremely 
        public bool HasClickHandler(RSSFeedItemClickEventHandler handler)
        {
            var _delegate = this.Click;
            if (_delegate != null)
            {
                return (Array.IndexOf(_delegate.GetInvocationList(), handler) != -1);
            }
            else
            {
                return false;
            }
        }

        public event RSSFeedItemClickEventHandler Click;

        public void PerformClick() => this.OnClick();

        protected virtual void OnClick()
        {
            this.Click?.Invoke(this, new RSSFeedItemClickEventArgs(this.feed));
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.RSS
{
    public class RSSFeedDisplayNameChangedEventArgs : EventArgs
    {
        public string DisplayName { get; }

        public RSSFeedDisplayNameChangedEventArgs(string displayname) : base()
        {
            this.DisplayName = displayname;
        }
    }

    public delegate void RSSFeedDisplayNameChangedEventHandler(RSSFeed sender, RSSFeedDisplayNameChangedEventArgs e);

    public class RSSFeedDisplayImageChangedEventArgs : EventArgs
    {
        /// <summary>You willn need to call <seealso cref="Stream.Dispose"/> or wrap it within a "using" block.</summary>
        public Stream ImageContentStream { get; }

        public RSSFeedDisplayImageChangedEventArgs(Stream contentStream) : base()
        {
            this.ImageContentStream = contentStream;
        }
    }
    public delegate void RSSFeedDisplayImageChangedEventHandler(RSSFeed sender, RSSFeedDisplayImageChangedEventArgs e);

    public class RSSFeedItemClickEventArgs : EventArgs
    {
        public RSSFeed RSSFeed { get; }

        public RSSFeedItemClickEventArgs(RSSFeed feed) : base()
        {
            this.RSSFeed = feed;
        }
    }
    public delegate void RSSFeedItemClickEventHandler(RSSFeedItem sender, RSSFeedItemClickEventArgs e);

    public class RSSFeedUpdatedEventArgs : EventArgs
    {
        public IReadOnlyList<RSSFeedItem> Items { get; }

        public RSSFeedUpdatedEventArgs(IReadOnlyList<RSSFeedItem> items) : base()
        {
            this.Items = items;
        }
    }
    public delegate void RSSFeedUpdatedEventHandler(RSSFeed sender, RSSFeedUpdatedEventArgs e);
}

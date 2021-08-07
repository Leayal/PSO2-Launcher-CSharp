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

    public delegate void RSSFeedDisplayNameChangedEventHandler(RSSFeedHandler sender, RSSFeedDisplayNameChangedEventArgs e);

    public class RSSFeedDisplayImageChangedEventArgs : EventArgs
    {
        /// <summary>You willn need to call <seealso cref="Stream.Dispose"/> or wrap it within a "using" block.</summary>
        public Stream ImageContentStream { get; }

        public char RepresentativeCharacter { get; }

        public RSSFeedDisplayImageChangedEventArgs(char representativecharacter) : this(representativecharacter, null) { }

        public RSSFeedDisplayImageChangedEventArgs(char representativecharacter, Stream contentStream) : base()
        {
            this.RepresentativeCharacter = representativecharacter;
            this.ImageContentStream = contentStream;
        }
    }
    public delegate void RSSFeedDisplayImageChangedEventHandler(RSSFeedHandler sender, RSSFeedDisplayImageChangedEventArgs e);

    public class RSSFeedItemClickEventArgs : EventArgs
    {
        public RSSFeedHandler RSSFeed { get; }

        public RSSFeedItemClickEventArgs(RSSFeedHandler feed) : base()
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
    public delegate void RSSFeedUpdatedEventHandler(RSSFeedHandler sender, RSSFeedUpdatedEventArgs e);
}

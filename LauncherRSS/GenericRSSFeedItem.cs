using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.RSS
{
    class GenericRSSFeedItem : RSSFeedItem
    {
        public GenericRSSFeedItem(RSSFeed feed, string displayName, Uri url) : base(feed, displayName, url) { }
    }
}

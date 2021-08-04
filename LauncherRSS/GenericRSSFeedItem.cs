using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.RSS
{
    class GenericRSSFeedItem : RSSFeedItem
    {
        public GenericRSSFeedItem(RSSFeedHandler feed, string displayName, string shortdescription, Uri url, DateTime? publishDate) : base(feed, displayName, shortdescription, url, publishDate) { }
    }
}

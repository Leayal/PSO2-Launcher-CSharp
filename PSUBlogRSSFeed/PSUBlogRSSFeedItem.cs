using Leayal.PSO2Launcher.RSS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSUBlog
{
    class PSUBlogRSSFeedItem : RSSFeedItem
    {
        public PSUBlogRSSFeedItem(RSSFeedHandler handler, string item_display, string description, Uri url, DateTime? pubDate) : base(handler, item_display, description, url, pubDate) { }

        protected override void OnClick()
        {
            Task.Run(() =>
            {
                try
                {
                    Process.Start("explorer.exe", $"\"{this.Url.AbsoluteUri}\"")?.Dispose();
                }
                catch
                {

                }
            });
        }
    }
}

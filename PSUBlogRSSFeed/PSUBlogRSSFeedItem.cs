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
        public PSUBlogRSSFeedItem(string item_display, Uri url) : base(null, item_display, url) { }

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

using Leayal.PSO2Launcher.RSS;
using System;

namespace PSUBlog
{
    /// <summary>This is only an alias to Wordpress now.</summary>
    [SupportUriHost("www.bumped.org")]
    public class PSUBlogNGSRSSFeed : WordpressRSSFeed
    {
        // private static readonly Uri DefaultFeed = new Uri("https://www.bumped.org/phantasy/rss/");

        public PSUBlogNGSRSSFeed(Uri url) : base(url) { }
    }
}

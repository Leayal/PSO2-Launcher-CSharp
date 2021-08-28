using Leayal.PSO2Launcher.RSS;
using Leayal.PSO2Launcher.RSS.Handlers;
using System;

namespace PSUBlog
{
    /// <summary>This is only an alias to Wordpress now.</summary>
    /// <remarks><para>This code is not official from PSUBlog. It was written by Dramiel Leayal. If PSUBlog is not happy with this, please tell <i><b>Dramiel Leayal@8799</b></i> on Discord to remove this from the launcher.</para></remarks>
    [SupportUriHost("www.bumped.org")]
    public class PSUBlogNGSRSSFeed : WordpressRSSFeed
    {
        // private static readonly Uri DefaultFeed = new Uri("https://www.bumped.org/phantasy/rss/");

        public PSUBlogNGSRSSFeed(Uri url) : base(url) { }
    }
}

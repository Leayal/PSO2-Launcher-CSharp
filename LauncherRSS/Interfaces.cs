using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.RSS
{
    /// <summary>Represent for a class which will handle downloading a RSS Feed channel.</summary>
    /// <remarks>Usually you don't need to use this. Unless your RSS Feed has authorization or something out of ordinary.</remarks>
    public interface IRSSFeedChannelDownloader
    {
        /// <summary>Download data from the feed channel given by <paramref name="feedchannelUrl"/>.</summary>
        /// <param name="webclient">The <seealso cref="HttpClient"/> which can be used to download the data.</param>
        /// <param name="feedchannelUrl">The absolute url to the feed to send the HTTP request to.</param>
        /// <returns>Feed data in XML <seealso cref="string"/> format.</returns>
        Task<string> DownloadFeedChannel(HttpClient webclient, Uri feedchannelUrl);

        /// <summary>Determines whether the plugin can handle this feed.</summary>
        /// <param name="url">The url to check for.</param>
        /// <returns>
        /// <para>True - The plugin can handle.</para>
        /// <para>False - The plugin can not handle.</para>
        /// </returns>
        bool CanHandleDownloadChannel(Uri url);
    }

    /// <summary>Represent for a class which will handle parsing the downloaded data from a RSS Feed channel.</summary>
    public interface IRSSFeedChannelParser
    {
        /// <summary>Parse the downloaded data into <seealso cref="FeedItemData"/>(s).</summary>
        /// <param name="data">Feed data in XML <seealso cref="string"/> format.</param>
        /// <returns><seealso cref="FeedItemData"/>(s).</returns>
        Task<IReadOnlyList<FeedItemData>> ParseFeedChannel(string data);

        /// <summary>Determines whether the plugin can handle parsing this feed's data.</summary>
        /// <param name="url">The url to check for.</param>
        /// <returns>
        /// <para>True - The plugin can handle.</para>
        /// <para>False - The plugin can not handle.</para>
        /// </returns>
        bool CanHandleParseFeedData(Uri url);
    }

    /// <summary>Represent for a class which will handle creating <seealso cref="RSSFeedItem"/> from <seealso cref="FeedItemData"/>.</summary>
    public interface IRSSFeedItemCreator
    {
        /// <summary>Creates <seealso cref="RSSFeedItem"/> from <seealso cref="FeedItemData"/>.</summary>
        /// <param name="feeditemdata">The feed item's data information to create <seealso cref="RSSFeedItem"/>.</param>
        /// <returns>A <seealso cref="RSSFeedItem"/> or its derived class.</returns>
        RSSFeedItem CreateFeedItem(in FeedItemData feeditemdata);

        /// <summary>Determines whether the plugin can handle this feed's <seealso cref="RSSFeedItem"/> creation(s).</summary>
        /// <param name="url">The url to check for.</param>
        /// <returns>
        /// <para>True - The plugin can handle.</para>
        /// <para>False - The plugin can not handle.</para>
        /// </returns>
        bool CanHandleFeedItemCreation(Uri url);
    }

    /// <summary>Represent for a class which will handle sorting <seealso cref="RSSFeedItem"/>.</summary>
    public interface IRSSFeedItemSorter : IComparer<RSSFeedItem> { }
}

using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Leayal.SharedInterfaces;
using System.Net.Http;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using System.Xml.XPath;
using System.Text.RegularExpressions;
using System.Threading;

namespace Leayal.PSO2Launcher.RSS.Handlers
{
    public class WordpressRSSFeed : RSSFeedHandler
    {
        private static readonly Regex rg_cdata = new Regex(@"\<\!\[CDATA\[(.*)\]\]\>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex rg_removetags = new Regex(@"<\/?[\w\s]*>|<.+[\W]>.*?<.+[\W]>?", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private string cached_abs_url_icon;
        private readonly string IconPath;
        private readonly string IconHashPath;

        public WordpressRSSFeed(Uri url) : base(url)
        {
            this.IconPath = Path.Combine(this.CacheDataDirectory, "icon");
            this.IconHashPath = Path.Combine(this.CacheDataDirectory, "icon-hash");
            if (File.Exists(this.IconHashPath) && File.Exists(this.IconPath))
            {
                var fs = File.OpenRead(this.IconPath);
                var isnullFile = (fs.Length == 0L);
                if (isnullFile)
                {
                    fs.Dispose();
                    this.cached_abs_url_icon = null;
                }
                else
                {
                    this.cached_abs_url_icon = Leayal.PSO2Launcher.Helper.QuickFile.ReadFirstLine(this.IconHashPath);
                    if (string.IsNullOrWhiteSpace(this.cached_abs_url_icon))
                    {
                        this.cached_abs_url_icon = null;
                        fs.Dispose();
                    }
                    else
                    {
                        this.SetDisplayImage(fs);
                    }
                }
            }
            else
            {
                this.cached_abs_url_icon = null;
            }
        }

        protected override Task<IReadOnlyList<FeedItemData>> OnParseFeedChannel(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                this.SetNextRefesh(TimeSpan.FromMinutes(30));
                return Task.FromResult<IReadOnlyList<FeedItemData>>(null);
            }
            var reader = new XmlDocument();
            reader.LoadXml(data);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(reader.NameTable);
            foreach (var item in GetNamespacesInScope(reader.DocumentElement))
            {
                nsmgr.AddNamespace(item.Key, item.Value);
            }
            var element_channel = reader.DocumentElement.SelectSingleNode("channel");
            var listOfItem = new List<FeedItemData>();
            if (element_channel != null)
            {
                TimeSpan timerOffset = TimeSpan.FromHours(1);
                var sy_updatefrequency = element_channel.SelectSingleNode(@"sy:updateFrequency", nsmgr);
                if (sy_updatefrequency != null)
                {
                    var val_frequency = sy_updatefrequency.InnerText.AsSpan();
                    if (!val_frequency.IsEmpty && !val_frequency.IsWhiteSpace())
                    {
                        val_frequency = val_frequency.Trim();
                        if (int.TryParse(val_frequency, System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out var num))
                        {
                            var sy_updateperiod = element_channel.SelectSingleNode(@"sy:updatePeriod", nsmgr);
                            if (sy_updateperiod != null)
                            {
                                var val = sy_updateperiod.InnerText.AsSpan();
                                if (!val.IsEmpty && !val.IsWhiteSpace())
                                {
                                    val = val.Trim();
                                    if (val.Equals("hourly", StringComparison.OrdinalIgnoreCase))
                                    {
                                        timerOffset = TimeSpan.FromHours(num);
                                    }
                                    else if (val.Equals("daily", StringComparison.OrdinalIgnoreCase) || val.Equals("dayly", StringComparison.OrdinalIgnoreCase))
                                    {
                                        timerOffset = TimeSpan.FromDays(num);
                                    }
                                    else if (val.Equals("minutely", StringComparison.OrdinalIgnoreCase))
                                    {
                                        timerOffset = TimeSpan.FromMinutes(num);
                                    }
                                }
                            }
                        }
                    }
                }

                var title = element_channel.SelectSingleNode("title");
                if (title != null && !string.IsNullOrWhiteSpace(title.InnerText))
                {
                    this.SetDisplayName(title.InnerText.Trim());
                }

                var element_image = element_channel.SelectSingleNode("image");
                if (element_image != null)
                {
                    var element_url = element_image.SelectSingleNode("url");
                    if (element_url != null)
                    {
                        if (Uri.TryCreate(element_url.InnerText, UriKind.Absolute, out var uri))
                        {
                            var abs_url = uri.AbsoluteUri;
                            bool isNew = false;
                            lock (this)
                            {
                                isNew = !string.Equals(this.cached_abs_url_icon, abs_url, StringComparison.Ordinal);
                                if (isNew)
                                {
                                    this.cached_abs_url_icon = abs_url;
                                }
                            }
                            if (isNew)
                            {
                                _ = Task.Run(async delegate
                                {
                                    var cache_filename = Leayal.Shared.Sha1StringHelper.GenerateFromString(abs_url);
                                    try
                                    {
                                        using (var fs = File.Create(this.IconPath))
                                        using (var remotestream = await this.HttpClient.GetStreamAsync(uri))
                                        {
                                            if (remotestream != null)
                                            {
                                                var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(1024 * 32);
                                                try
                                                {
                                                    var readbyte = remotestream.Read(buffer, 0, buffer.Length);
                                                    while (readbyte > 0)
                                                    {
                                                        fs.Write(buffer, 0, readbyte);
                                                        readbyte = remotestream.Read(buffer, 0, buffer.Length);
                                                    }
                                                }
                                                finally
                                                {
                                                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                                                }
                                            }
                                            fs.Flush();
                                        }
                                        File.WriteAllText(this.IconHashPath, cache_filename);
                                        await Task.Delay(1);
                                        this.SetDisplayImage(File.OpenRead(this.IconPath)); // Who care about the file's lock
                                    }
                                    catch
                                    {

                                    }
                                });
                            }
                        }
                    }
                }

                // Begin feed parsing here.
                var items = element_channel.SelectNodes("item");
                if (items != null)
                {
                    foreach (XmlNode item in items)
                    {
                        var element_link = item.SelectSingleNode("link");
                        if (element_link != null)
                        {
                            string item_url = element_link.InnerText;
                            if (!string.IsNullOrWhiteSpace(item_url))
                            {
                                item_url = item_url.Trim();
                                string item_title;
                                var element_title = item.SelectSingleNode("title");
                                if (element_title == null)
                                {
                                    item_title = item_url;
                                }
                                else
                                {
                                    item_title = element_title.InnerText;
                                    if (string.IsNullOrWhiteSpace(item_title))
                                    {
                                        item_title = item_url;
                                    }
                                    else
                                    {
                                        item_title = item_title.Trim();
                                    }
                                }

                                string item_description;
                                var element_description = item.SelectSingleNode("description");
                                if (element_description == null)
                                {
                                    item_description = string.Empty;
                                }
                                else
                                {
                                    item_description = element_description.InnerText;
                                    if (string.IsNullOrWhiteSpace(item_description))
                                    {
                                        item_description = string.Empty;
                                    }
                                    else
                                    {
                                        item_description = item_description.Trim();
                                        if (rg_cdata.IsMatch(item_description))
                                        {
                                            var match = rg_cdata.Match(item_description);
                                            item_description = match.Groups[0].Value;
                                        }
                                        if (rg_removetags.IsMatch(item_description))
                                        {
                                            item_description = rg_removetags.Replace(item_description, string.Empty);
                                        }
                                    }
                                }

                                DateTime? item_pubdate;
                                var element_pubdate = item.SelectSingleNode("pubDate");
                                if (element_pubdate == null)
                                {
                                    item_pubdate = null;
                                }
                                else
                                {
                                    if (!string.IsNullOrWhiteSpace(element_pubdate.InnerText) && DateTime.TryParse(element_pubdate.InnerText, out var time))
                                    {
                                        item_pubdate = time;
                                    }
                                    else
                                    {
                                        item_pubdate = null;
                                    }
                                }

                                if (Uri.TryCreate(item_url, UriKind.Absolute, out _))
                                {
                                    listOfItem.Add(new FeedItemData(item_title, item_description, item_url, item_pubdate));
                                }
                            }
                        }
                    }
                }


                // Next tick.
                DateTime lastFetchTime;
                TimeSpan nextfetch;
                var lastbuilddate = element_channel.SelectSingleNode("lastBuildDate");
                if (lastbuilddate != null && !string.IsNullOrWhiteSpace(lastbuilddate.InnerText))
                {
                    var val_lastbuilddate = lastbuilddate.InnerText.AsSpan();
                    if (!val_lastbuilddate.IsEmpty && !val_lastbuilddate.IsWhiteSpace() && DateTime.TryParse(val_lastbuilddate, out lastFetchTime))
                    {
                        var added = lastFetchTime.Add(timerOffset);
                        var now = DateTime.Now;
                        while (added <= now)
                        {
                            added = added.Add(timerOffset);
                        }
                        nextfetch = added - now;
                    }
                    else
                    {
                        nextfetch = timerOffset;
                    }
                }
                else
                {
                    nextfetch = timerOffset;
                }
                this.SetNextRefesh(nextfetch);
            }

            return Task.FromResult<IReadOnlyList<FeedItemData>>(listOfItem);
        }

        protected override Task<string> OnDownloadFeedChannel(HttpClient webclient, Uri feedchannelUrl)
            => Default.DownloadFeedChannel(webclient, feedchannelUrl);

        protected override RSSFeedItem OnCreateFeedItem(in FeedItemData feeditemdata)
            => Default.CreateFeedItem(in feeditemdata);

        public override bool CanHandleParseFeedData(Uri url) => true;

        public override bool CanHandleFeedItemCreation(Uri url) => true;

        public override bool CanHandleDownloadChannel(Uri url) => true;
    }
}

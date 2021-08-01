using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Linq;
using Leayal.PSO2Launcher.RSS;
using Leayal.SharedInterfaces;
using System.Net.Http;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using System.Xml.XPath;

namespace PSUBlog
{
    /// <remarks><para>This code is not official from PSUBlog. It was written by Dramiel Leayal. If PSUBlog is not happy with this, please tell <i><b>Dramiel Leayal@8799</b></i> on Discord to remove this from the launcher.</para></remarks>
    public class PSUBlogNGSRSSFeed : RSSFeed
    {
        private static readonly Uri DefaultFeed = new Uri("https://www.bumped.org/phantasy/rss/");

        private bool isfirstfetch;
        private string cached_abs_url_icon;
        private readonly string IconPath;
        private readonly string IconHashPath;

        public PSUBlogNGSRSSFeed(IRSSLoader loader) : base(loader, typeof(PSUBlogNGSRSSFeed).FullName)
        {
            this.IconPath = Path.Combine(this.CacheDataDirectory, "icon");
            this.IconHashPath = Path.Combine(this.CacheDataDirectory, "icon-hash");
            if (File.Exists(this.IconHashPath))
            {
                this.cached_abs_url_icon = Leayal.PSO2Launcher.Helper.QuickFile.ReadFirstLine(this.IconHashPath);
                this.SetDisplayImage(File.OpenRead(this.IconPath));
            }
            else
            {
                this.cached_abs_url_icon = null;
            }
            this.isfirstfetch = true;
        }

        protected override async Task<IReadOnlyDictionary<string, Uri>> FetchFeed()
        {
            var data = await this.HttpClient.GetStringAsync(DefaultFeed);
            var reader = new XmlDocument();
            reader.LoadXml(data);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(reader.NameTable);
            foreach (var item in getNamespacesInScope(reader.DocumentElement))
            {
                nsmgr.AddNamespace(item.Key, item.Value);
            }
            var element_channel = reader.DocumentElement.SelectSingleNode("channel");
            var listOfItem = new Dictionary<string, Uri>();
            if (element_channel != null)
            {
                TimeSpan timerOffset = TimeSpan.Zero;
                var sy_updatefrequency = element_channel.SelectSingleNode(@"sy:updateFrequency", nsmgr);
                if (sy_updatefrequency != null)
                {
                    var val_frequency = sy_updatefrequency.InnerText;
                    if (!string.IsNullOrWhiteSpace(val_frequency))
                    {
                        val_frequency = val_frequency.Trim();
                        if (int.TryParse(val_frequency, System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out var num))
                        {
                            var sy_updateperiod = element_channel.SelectSingleNode(@"sy:updatePeriod", nsmgr);
                            if (sy_updateperiod != null)
                            {
                                var val = sy_updateperiod.InnerText;
                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    val = val.Trim();
                                    if (string.Equals(val, "hourly", StringComparison.OrdinalIgnoreCase))
                                    {
                                        timerOffset = TimeSpan.FromHours(num);
                                    }
                                    else if (string.Equals(val, "daily", StringComparison.OrdinalIgnoreCase) || string.Equals(val, "dayly", StringComparison.OrdinalIgnoreCase))
                                    {
                                        timerOffset = TimeSpan.FromDays(num);
                                    }
                                    else if (string.Equals(val, "minutely", StringComparison.OrdinalIgnoreCase))
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
                                    var cache_filename = Sha1String(abs_url);
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

                                if (Uri.TryCreate(item_url, UriKind.Absolute, out var uri))
                                {
                                    listOfItem.Add(item_title, uri);
                                }
                            }
                        }
                    }
                }


                // Next tick.
                if (isfirstfetch)
                {
                    isfirstfetch = false;
                    DateTime lastFetchTime;
                    TimeSpan nextfetch;
                    var lastbuilddate = element_channel.SelectSingleNode("lastBuildDate");
                    if (lastbuilddate != null && !string.IsNullOrWhiteSpace(lastbuilddate.InnerText))
                    {
                        lastFetchTime = DateTime.Parse(lastbuilddate.InnerText);
                        if (lastFetchTime.Add(timerOffset) > DateTime.Now)
                        {
                            // HOW!!!!?
                            nextfetch = TimeSpan.Zero;
                        }
                        else
                        {
                            nextfetch = lastFetchTime.Add(timerOffset) - DateTime.Now;
                        }
                    }
                    else
                    {
                        nextfetch = timerOffset;
                    }
                    this.SetNextRefesh(nextfetch);
                }
                else
                {
                    this.SetNextRefesh(timerOffset);
                }
            }

            return listOfItem;
        }

        static bool duh = true;

        static string Sha1String(in string str)
        {
            
            SHA1 sha1;
            if (duh)
            {
                try
                {
                    sha1 = new SHA1Managed();
                }
                catch (InvalidOperationException)
                {
                    duh = false;
                    sha1 = SHA1.Create();
                }
            }
            else
            {
                sha1 = SHA1.Create();
            }
            using (sha1)
            {
                return Convert.ToHexString(sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(str)));
            }
        }

        protected override async Task OnRefresh(DateTime dateTime)
        {
            await this.Fetch();
        }

        protected override RSSFeedItem OnCreateRSSFeedItem(string displayName, Uri url)
            => new PSUBlogRSSFeedItem(displayName, url);

        public static IDictionary<string, string> getNamespacesInScope(XmlNode xDoc)
        {
            IDictionary<string, string> AllNamespaces = new Dictionary<string, string>();
            IDictionary<string, string> localNamespaces;

            XmlNode temp = xDoc;
            XPathNavigator xNav;
            while (temp.ParentNode != null)
            {
                xNav = temp.CreateNavigator();
                localNamespaces = xNav.GetNamespacesInScope(XmlNamespaceScope.Local);
                foreach (var item in localNamespaces)
                {
                    if (!AllNamespaces.ContainsKey(item.Key))
                    {
                        AllNamespaces.Add(item);
                    }
                }
                temp = temp.ParentNode;
            }
            return AllNamespaces;
        }
    }
}

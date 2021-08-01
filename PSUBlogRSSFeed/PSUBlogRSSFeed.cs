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

namespace PSUBlog
{
    /// <remarks><para>This code is not official from PSUBlog. It was written by Dramiel Leayal. If PSUBlog is not happy with this, please tell <i><b>Dramiel Leayal@8799</b></i> on Discord to remove this from the launcher.</para></remarks>
    public class PSUBlogNGSRSSFeed : RSSFeed
    {
        private static readonly Uri DefaultFeed = new Uri("https://www.bumped.org/phantasy/rss/");

        private bool isfirstfetch;

        public PSUBlogNGSRSSFeed(IRSSLoader loader) : base(loader, typeof(PSUBlogNGSRSSFeed).FullName)
        {
            this.isfirstfetch = true;
        }

        protected override async Task<IReadOnlyDictionary<string, Uri>> FetchFeed()
        {
            var data = await this.HttpClient.GetStringAsync(DefaultFeed);
            var reader = XDocument.Parse(data);
            var element_channel = reader.Root.Element("channel");
            var listOfItem = new Dictionary<string, Uri>();
            if (element_channel != null)
            {
                TimeSpan timerOffset = TimeSpan.Zero;
                var sy_updatefrequency = element_channel.Element("sy:updateFrequency");
                if (sy_updatefrequency != null && !string.IsNullOrWhiteSpace(sy_updatefrequency.Value) && int.TryParse(sy_updatefrequency.Value, System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out var num))
                {
                    var sy_updateperiod = element_channel.Element("sy:updatePeriod");
                    if (sy_updateperiod != null)
                    {
                        var val = sy_updateperiod.Value;
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

                if (isfirstfetch)
                {
                    isfirstfetch = false;
                    DateTime lastFetchTime;
                    TimeSpan nextfetch;
                    var lastbuilddate = element_channel.Element("lastBuildDate");
                    if (lastbuilddate != null && !string.IsNullOrWhiteSpace(lastbuilddate.Value))
                    {
                        lastFetchTime = DateTime.Parse(lastbuilddate.Value);
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
                    this.SetRefeshTimer(nextfetch);
                }
                else
                {
                    this.SetRefeshTimer(timerOffset);
                }

                var title = element_channel.Element("title");
                if (title != null)
                {
                    this.SetDisplayName(title.Value);
                }

                var element_image = element_channel.Element("image");
                if (element_image != null)
                {
                    var element_url = element_image.Element("url");
                    if (element_url != null)
                    {
                        if (Uri.TryCreate(element_url.Value, UriKind.Absolute, out var uri))
                        {
                            var abs_url = uri.AbsoluteUri;
                            var cache_filename = Sha1String(abs_url);
                            var cache_filepath = Path.Combine(this.CacheDataDirectory, cache_filename);
                            if (File.Exists(cache_filepath))
                            {
                                this.SetDisplayImage(File.OpenRead(cache_filepath)); // Who care about the file's lock?
                            }
                            else
                            {
                                _ = Task.Run(async delegate
                                {
                                    try
                                    {
                                        using (var fs = File.Create(cache_filepath))
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
                                        await Task.Delay(1);
                                        this.SetDisplayImage(File.OpenRead(cache_filepath)); // Who care about the file's lock
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
                listOfItem.Add("", null);

                var items = element_channel.Elements("item");
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        var element_link = item.Element("link");
                        if (element_link != null)
                        {
                            string item_url = element_link.Value;
                            if (!string.IsNullOrWhiteSpace(item_url))
                            {
                                string item_title;
                                var element_title = item.Element("title");
                                if (element_title == null)
                                {
                                    item_title = item_url;
                                }
                                else
                                {
                                    item_title = element_title.Value;
                                }

                                if (Uri.TryCreate(item_url, UriKind.Absolute, out var uri))
                                {
                                    listOfItem.Add(item_title, uri);
                                }
                            }
                        }
                    }
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
    }
}

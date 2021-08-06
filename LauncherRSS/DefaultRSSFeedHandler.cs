using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Leayal.PSO2Launcher.RSS
{
    /// <summary></summary>
    class DefaultRSSFeedHandler : RSSFeedHandler
    {
        public DefaultRSSFeedHandler(Uri url) : base(url, false) { }

        public override bool CanHandleDownloadChannel(Uri url) => true;

        protected override async Task<string> OnDownloadFeedChannel(HttpClient webclient, Uri feedchannelUrl)
        {
            return await webclient.GetStringAsync(feedchannelUrl);
        }

        public override bool CanHandleParseFeedData(Uri url) => true;

        protected override Task<IReadOnlyList<FeedItemData>> OnParseFeedChannel(string data)
        {
            var reader = new XmlDocument();
            reader.LoadXml(data);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(reader.NameTable);
            XmlNode rssNode;
            if (reader.DocumentElement != null)
            {
                if (string.Equals(reader.DocumentElement.Name, "rss", StringComparison.OrdinalIgnoreCase))
                {
                    rssNode = reader.DocumentElement;
                }
                else
                {
                    rssNode = reader.DocumentElement.SelectSingleNode("rss");
                }
            }
            else
            {
                return Task.FromResult((IReadOnlyList<FeedItemData>)Array.Empty<FeedItemData>());
            }
            foreach (var item in GetNamespacesInScope(reader.DocumentElement))
            {
                nsmgr.AddNamespace(item.Key, item.Value);
            }
            var element_channel = rssNode.SelectSingleNode("channel");
            var listOfItem = new List<FeedItemData>();
            if (element_channel != null)
            {
                var title = element_channel.SelectSingleNode("title");
                if (title != null && !string.IsNullOrWhiteSpace(title.InnerText))
                {
                    this.SetDisplayName(title.InnerText.Trim());
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

                                listOfItem.Add(new FeedItemData(item_title, item_description, item_url, item_pubdate));
                            }
                        }
                    }
                }
            }

            return Task.FromResult((IReadOnlyList<FeedItemData>)listOfItem);
        }

        public override bool CanHandleFeedItemCreation(Uri url) => true;

        protected override RSSFeedItem OnCreateFeedItem(in FeedItemData feeditemdata)
        {
            if (Uri.TryCreate(feeditemdata.Link, UriKind.Absolute, out var uri))
            {
                return new GenericRSSFeedItem(this, feeditemdata.Title, feeditemdata.Description, uri, feeditemdata.PublishDate);
            }
            else
            {
                return null;
            }
        }
    }
}

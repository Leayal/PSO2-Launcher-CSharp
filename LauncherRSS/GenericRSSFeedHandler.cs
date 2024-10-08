﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Leayal.PSO2Launcher.RSS
{
    /// <summary></summary>
    public class GenericRSSFeedHandler : RSSFeedHandler
    {
        public GenericRSSFeedHandler(Uri feedchannelurl) : base(feedchannelurl) { }

        private readonly IRSSFeedChannelDownloader downloader;
        private readonly IRSSFeedChannelParser parser;
        private readonly IRSSFeedItemCreator itemmaker;

        public IRSSFeedChannelDownloader Downloader => this.downloader;
        public IRSSFeedChannelParser Parser => this.parser;
        public IRSSFeedItemCreator FeedItemCreator => this.itemmaker;

        public override bool CanHandleDownloadChannel(Uri url)
        {
            if (this.downloader == null) return false;
            return this.downloader.CanHandleDownloadChannel(url);
        }

        protected override Task<string> OnDownloadFeedChannel(HttpClient webclient, Uri feedchannelUrl)
            => this.downloader.DownloadFeedChannel(webclient, feedchannelUrl);

        public override bool CanHandleParseFeedData(Uri url)
        {
            if (this.parser == null) return false;
            return this.parser.CanHandleParseFeedData(url);
        }

        protected override Task<IReadOnlyList<FeedItemData>> OnParseFeedChannel(string data)
        {
            if (this.parser is DefaultRSSFeedHandler defaultOne)
            {
                return defaultOne.ParseFeedChannelWithInstance(this, data);
            }
            else
            {
                return this.parser.ParseFeedChannel(data);
            }
        }

        public override bool CanHandleFeedItemCreation(Uri url)
        {
            if (this.itemmaker == null) return false;
            return this.itemmaker.CanHandleFeedItemCreation(url);
        }

        protected override RSSFeedItem OnCreateFeedItem(in FeedItemData feeditemdata)
               => this.itemmaker.CreateFeedItem(in feeditemdata);

        public GenericRSSFeedHandler(Uri url, IRSSFeedChannelDownloader downloader, IRSSFeedChannelParser parser, IRSSFeedItemCreator creator) : base(url)
        {
            this.downloader = downloader;
            this.parser = parser;
            this.itemmaker = creator;
        }

        public GenericRSSFeedHandler(Uri url, IRSSFeedChannelDownloader downloader)
            : this(url, downloader, Default, Default) { }

        public GenericRSSFeedHandler(Uri url, IRSSFeedChannelParser parser)
            : this(url, Default, parser, Default) { }

        public GenericRSSFeedHandler(Uri url, IRSSFeedItemCreator creator)
            : this(url, Default, Default, creator) { }

        public GenericRSSFeedHandler(Uri url, IRSSFeedChannelDownloader downloader, IRSSFeedChannelParser parser)
           : this(url, downloader, parser, Default) { }

        public GenericRSSFeedHandler(Uri url, IRSSFeedChannelDownloader downloader, IRSSFeedItemCreator creator)
           : this(url, downloader, Default, creator) { }

        public GenericRSSFeedHandler(Uri url, IRSSFeedChannelParser parser, IRSSFeedItemCreator creator)
            : this(url, Default, parser, creator) { }
    }
}

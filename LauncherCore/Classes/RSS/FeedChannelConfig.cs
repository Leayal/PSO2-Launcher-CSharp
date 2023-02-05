using Leayal.PSO2Launcher.RSS;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Leayal.PSO2Launcher.Core.Classes.RSS
{
    public readonly struct FeedChannelConfig : IEquatable<FeedChannelConfig>
    {
        public readonly string FeedChannelUrl { get; init; }
        public readonly string BaseHandler { get; init; }
        public readonly string? DownloadHandler { get; init; }
        public readonly string? ParserHandler { get; init; }
        public readonly string? ItemCreatorHandler { get; init; }

        public readonly bool IsDeferredUpdate { get; init; }

        public static FeedChannelConfig FromData(string data)
        {
            using (var jsonDoc = JsonDocument.Parse(data))
            {
                return FromJson(jsonDoc);
            }
        }

        public static FeedChannelConfig FromHandler(RSSFeedHandler handler)
        {
            if (RSSFeedHandler.IsDefault(handler))
            {
                return new FeedChannelConfig()
                {
                    FeedChannelUrl = handler.FeedChannelUrl.IsAbsoluteUri ? handler.FeedChannelUrl.AbsoluteUri : handler.FeedChannelUrl.ToString(),
                    BaseHandler = "Default",
                    DownloadHandler = null,
                    ItemCreatorHandler = null,
                    ParserHandler = null,
                    IsDeferredUpdate = handler.DeferRefresh
                };
            }
            else if (handler is GenericRSSFeedHandler genericHandler)
            {
                var downloaderObj = genericHandler.Downloader;
                var parserObj = genericHandler.Parser;
                var itemcreatorObj = genericHandler.FeedItemCreator;
                string? downloader, itemcreator, parser;
                if (downloaderObj is RSSFeedHandler feedhandler_downloader && (RSSFeedHandler.IsDefault(feedhandler_downloader) || RSSFeedHandler.IsGeneric(feedhandler_downloader)))
                {
                    downloader = null;
                }
                else
                {
                    downloader = downloaderObj.GetType().FullName;
                }

                if (parserObj is RSSFeedHandler feedhandler_parser && (RSSFeedHandler.IsDefault(feedhandler_parser) || RSSFeedHandler.IsGeneric(feedhandler_parser)))
                {
                    parser = null;
                }
                else
                {
                    parser = parserObj.GetType().FullName;
                }

                if (itemcreatorObj is RSSFeedHandler feedhandler_itemcreator && (RSSFeedHandler.IsDefault(feedhandler_itemcreator) || RSSFeedHandler.IsGeneric(feedhandler_itemcreator)))
                {
                    itemcreator = null;
                }
                else
                {
                    itemcreator = itemcreatorObj.GetType().FullName;
                }

                return new FeedChannelConfig()
                {
                    FeedChannelUrl = handler.FeedChannelUrl.IsAbsoluteUri ? handler.FeedChannelUrl.AbsoluteUri : handler.FeedChannelUrl.ToString(),
                    BaseHandler = "Generic",
                    DownloadHandler = downloader,
                    ItemCreatorHandler = itemcreator,
                    ParserHandler = parser,
                    IsDeferredUpdate = handler.DeferRefresh
                };
            }
            else
            {
                return new FeedChannelConfig()
                {
                    FeedChannelUrl = handler.FeedChannelUrl.IsAbsoluteUri ? handler.FeedChannelUrl.AbsoluteUri : handler.FeedChannelUrl.ToString(),
                    BaseHandler = handler.GetType().FullName ?? "Generic",
                    DownloadHandler = null,
                    ItemCreatorHandler = null,
                    ParserHandler = null,
                    IsDeferredUpdate = handler.DeferRefresh
                };
            }
        }

        public void SaveTo(System.IO.Stream stream)
        {
            if (!stream.CanWrite)
            {
                throw new ArgumentException(nameof(stream));
            }
            else
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions() { Indented = true }))
                {
                    writer.WriteStartObject();
                    writer.WriteString("FeedChannelUrl", this.FeedChannelUrl);
                    writer.WriteString("BaseHandler", this.BaseHandler);
                    writer.WriteString("DownloadHandler", this.DownloadHandler);
                    writer.WriteString("ParserHandler", this.ParserHandler);
                    writer.WriteString("ItemCreatorHandler", this.ItemCreatorHandler);
                    writer.WriteBoolean("IsDeferredUpdate", this.IsDeferredUpdate);
                    writer.WriteEndObject();
                    writer.Flush();
                    stream.Flush();
                }
            }
        }

        public void SaveTo(string filename)
        {
            using (var fs = System.IO.File.Create(filename))
            {
                this.SaveTo(fs);
            }
        }

        private static FeedChannelConfig FromJson(JsonDocument jsonDoc)
        {
            var root = jsonDoc.RootElement;
            if (root.TryGetProperty("FeedChannelUrl", out var prop_FeedChannelUrl) && prop_FeedChannelUrl.ValueKind == JsonValueKind.String)
            {
                var channelUrl = prop_FeedChannelUrl.GetString();
                if (!string.IsNullOrWhiteSpace(channelUrl))
                {
                    string? val_BaseHandler, val_DownloadHandler, val_ParserHandler, val_ItemCreatorHandler;
                    bool deferredupdating = true;
                    if (root.TryGetProperty("BaseHandler", out var prop_BaseHandler) && prop_BaseHandler.ValueKind == JsonValueKind.String)
                    {
                        val_BaseHandler = prop_BaseHandler.GetString();
                    }
                    else
                    {
                        val_BaseHandler = null;
                    }
                    if (root.TryGetProperty("DownloadHandler", out var prop_DownloadHandler) && prop_DownloadHandler.ValueKind == JsonValueKind.String)
                    {
                        val_DownloadHandler = prop_DownloadHandler.GetString();
                    }
                    else
                    {
                        val_DownloadHandler = null;
                    }
                    if (root.TryGetProperty("ParserHandler", out var prop_ParserHandler) && prop_ParserHandler.ValueKind == JsonValueKind.String)
                    {
                        val_ParserHandler = prop_BaseHandler.GetString();
                    }
                    else
                    {
                        val_ParserHandler = null;
                    }
                    if (root.TryGetProperty("ItemCreatorHandler", out var prop_ItemCreatorHandler) && prop_ItemCreatorHandler.ValueKind == JsonValueKind.String)
                    {
                        val_ItemCreatorHandler = prop_BaseHandler.GetString();
                    }
                    else
                    {
                        val_ItemCreatorHandler = null;
                    }
                    if (root.TryGetProperty("IsDeferredUpdate", out var prop_IsDeferredUpdate) && prop_IsDeferredUpdate.ValueKind == JsonValueKind.False)
                    {
                        deferredupdating = false;
                    }

                    return new FeedChannelConfig() { FeedChannelUrl = channelUrl, BaseHandler = val_BaseHandler ?? "Default", DownloadHandler = val_DownloadHandler, ItemCreatorHandler = val_ItemCreatorHandler, ParserHandler = val_ParserHandler, IsDeferredUpdate = deferredupdating };
                }
            }
            return default;
        }

        public override int GetHashCode() => this.FeedChannelUrl.GetHashCode() ^ this.BaseHandler.GetHashCode();

        public override bool Equals(object? obj)
        {
            if (obj is FeedChannelConfig conf)
            {
                return this.Equals(conf);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(FeedChannelConfig other)
            => (string.Equals(this.FeedChannelUrl, other.FeedChannelUrl, StringComparison.Ordinal)
                && string.Equals(this.BaseHandler, other.BaseHandler, StringComparison.Ordinal));
    }
}

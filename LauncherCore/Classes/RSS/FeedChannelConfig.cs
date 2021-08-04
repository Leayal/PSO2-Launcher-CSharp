using System;
using System.Text.Json;

namespace Leayal.PSO2Launcher.Core.Classes.RSS
{
    readonly struct FeedChannelConfig
    {
        public readonly string FeedChannelUrl { get; init; }
        public readonly string BaseHandler { get; init; }
        public readonly string DownloadHandler { get; init; }
        public readonly string ParserHandler { get; init; }
        public readonly string ItemCreatorHandler { get; init; }

        public static FeedChannelConfig FromData(string data)
        {
            using (var jsonDoc = JsonDocument.Parse(data))
            {
                return FromFile(jsonDoc);
            }
        }

        public void SaveTo(string filename)
        {

        }

        private static FeedChannelConfig FromFile(JsonDocument jsonDoc)
        {
            var root = jsonDoc.RootElement;
            if (root.TryGetProperty("FeedChannelUrl", out var prop_FeedChannelUrl) && prop_FeedChannelUrl.ValueKind == JsonValueKind.String)
            {
                var channelUrl = prop_FeedChannelUrl.GetString();
                if (!string.IsNullOrWhiteSpace(channelUrl))
                {
                    string val_BaseHandler, val_DownloadHandler, val_ParserHandler, val_ItemCreatorHandler;
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

                    return new FeedChannelConfig() { FeedChannelUrl = channelUrl, BaseHandler = val_BaseHandler, DownloadHandler = val_DownloadHandler, ItemCreatorHandler = val_ItemCreatorHandler, ParserHandler = val_ParserHandler };
                }
            }
            return default;
        }
    }
}

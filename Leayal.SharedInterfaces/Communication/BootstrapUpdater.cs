using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Leayal.SharedInterfaces.Communication
{
    public class BootstrapUpdater_CheckForUpdates : RestartDataObj<BootstrapUpdater_CheckForUpdates>
    {
        public readonly Dictionary<string, UpdateItem> Items;
        public readonly bool RequireRestart;
        public readonly bool RequireReload;
        public readonly string RestartWithExe;

        public readonly Dictionary<string, string> RestartMoveItems;

        public BootstrapUpdater_CheckForUpdates()
            : this(null, false, false, null) { }

        public BootstrapUpdater_CheckForUpdates(Dictionary<string, UpdateItem> items, bool restart, bool reload, string restartWith)
            : this(items, restart, reload, restartWith, null) { }

        public BootstrapUpdater_CheckForUpdates(Dictionary<string, UpdateItem> items, bool restart, bool reload, string restartWith, Dictionary<string, string> restartMoveItems)
        {
            this.Items = items;
            this.RequireRestart = restart;
            this.RequireReload = reload;
            this.RestartWithExe = restartWith;
            this.RestartMoveItems = restartMoveItems;
        }

        public override void WriteJsonValueTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteBoolean("needRestart", this.RequireRestart);
            writer.WriteBoolean("needReload", this.RequireReload);
            writer.WriteString("restartExe", this.RestartWithExe);

            if (this.Items.Count == 0)
            {
                writer.WriteNull("items");
            }
            else
            {
                writer.WriteStartObject("items");

                foreach (var item in this.Items)
                {
                    var val = item.Value;
                    writer.WriteStartObject(item.Key);
                    writer.WriteString("displayName", val.DisplayName);
                    writer.WriteString("filename", val.LocalFilename);
                    writer.WriteString("url", val.DownloadUrl);
                    writer.WriteBoolean("archive", val.IsRemoteAnArchive);
                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }

            if (this.RestartMoveItems == null || this.RestartMoveItems.Count == 0)
            {
                writer.WriteNull("restartRenameItems");
            }
            else
            {
                writer.WriteStartObject("restartRenameItems");

                foreach (var item in this.RestartMoveItems)
                {
                    writer.WriteString(item.Key, item.Value);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        public override BootstrapUpdater_CheckForUpdates DeserializeJson(JsonElement rootElement)
        {
            var dictionary = new Dictionary<string, UpdateItem>(StringComparer.OrdinalIgnoreCase);

            if (rootElement.TryGetProperty("items", out var prop_items))
            {
                if (prop_items.ValueKind == JsonValueKind.Object)
                {
                    using (var objWalker = prop_items.EnumerateObject())
                    {
                        while (objWalker.MoveNext())
                        {
                            var current = objWalker.Current;
                            var currentValue = current.Value;
                            dictionary.Add(current.Name,
                                new UpdateItem(
                                    currentValue.GetProperty("filename").GetString(),
                                    currentValue.GetProperty("url").GetString(),
                                    currentValue.GetProperty("displayName").GetString(),
                                    currentValue.GetProperty("archive").GetBoolean()
                                )
                            );
                        }
                    }
                }
            }

            var dictionary_restartRename = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (rootElement.TryGetProperty("restartRenameItems", out var prop_restartRenameItems))
            {
                if (prop_restartRenameItems.ValueKind == JsonValueKind.Object)
                {
                    using (var objWalker = prop_restartRenameItems.EnumerateObject())
                    {
                        while (objWalker.MoveNext())
                        {
                            var current = objWalker.Current;
                            dictionary_restartRename.Add(current.Name, current.Value.GetString());
                        }
                    }
                }
            }
            if (dictionary_restartRename.Count == 0)
            {
                dictionary_restartRename = null;
            }

            return new BootstrapUpdater_CheckForUpdates(dictionary,
                rootElement.GetProperty("needRestart").GetBoolean(),
                rootElement.GetProperty("needReload").GetBoolean(),
                rootElement.GetProperty("restartExe").GetString(),
                dictionary_restartRename);
        }
    }

    public readonly struct UpdateItem
    {
        public readonly string LocalFilename;
        public readonly string DownloadUrl;
        public readonly string DisplayName;
        public readonly bool IsRemoteAnArchive;

        public UpdateItem(string localFilename, string url, string displayName) : this(localFilename, url, displayName, false) { }

        public UpdateItem(string localFilename, string url, string displayName, bool isArchive)
        {
            this.LocalFilename = localFilename;
            this.DownloadUrl = url;
            this.DisplayName = displayName;
            this.IsRemoteAnArchive = isArchive;
        }
    }

    public class BootstrapUpdaterException : Exception
    {

    }

    public interface IBootstrapUpdater
    {
        Task<BootstrapUpdater_CheckForUpdates> CheckForUpdatesAsync(string rootDirectory, string entryExecutableName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <returns>Null to cancel. True to proceed. False to ignore and continue.</returns>
        bool? DisplayUpdatePrompt(System.Windows.Forms.Form? parent);
    }
}

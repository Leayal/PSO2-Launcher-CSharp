using Leayal.PSO2Launcher.Helper;
using Leayal.SharedInterfaces.Communication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Updater
{
    public partial class BootstrapUpdater : IBootstrapUpdater, IBootstrapUpdater_v2
    {
        private async Task<BootstrapUpdater_CheckForUpdates> ParseFileList_1(JsonDocument document, string rootDirectory, string entryExecutableName)
        {
            var needtobeupdated = new Dictionary<string, UpdateItem>(StringComparer.OrdinalIgnoreCase);

            var rootelement = document.RootElement;

            if (rootelement.TryGetProperty("files", out var prop_files))
            {
                if (prop_files.ValueKind == JsonValueKind.Object)
                {
                    if (rootelement.TryGetProperty("root-url", out var prop_rootUrl) && prop_rootUrl.ValueKind == JsonValueKind.String &&
                        Uri.TryCreate(prop_rootUrl.GetString(), UriKind.Absolute, out var rootUrl))
                    {
                        using (var objWalker = prop_files.EnumerateObject())
                        {
                            while (objWalker.MoveNext())
                            {
                                var item_prop = objWalker.Current;
                                var displayName = item_prop.Name;
                                var prop_val = item_prop.Value;
                                if ((prop_val.TryGetProperty("sha1", out var item_prop_sha1) && item_prop_sha1.ValueKind == JsonValueKind.String))
                                {
                                    var localFilename = Path.GetFullPath(Path.Combine("bin", displayName), rootDirectory);
                                    var remotehash = item_prop_sha1.GetString();
                                    if (File.Exists(localFilename))
                                    {
                                        var hash = await SHA1Hash.ComputeHashFromFileAsync(localFilename);
                                        if (!string.Equals(hash, remotehash, StringComparison.OrdinalIgnoreCase))
                                        {
                                            bool isArchive = prop_val.TryGetProperty("archive", out var item_prop_archive) ? (item_prop_archive.ValueKind == JsonValueKind.True) : false;
                                            needtobeupdated.Add(displayName, new UpdateItem(localFilename, remotehash, (new Uri(rootUrl, displayName)).AbsoluteUri, displayName, isArchive));
                                        }
                                    }
                                    else
                                    {
                                        bool isArchive = prop_val.TryGetProperty("archive", out var item_prop_archive) ? (item_prop_archive.ValueKind == JsonValueKind.True) : false;
                                        needtobeupdated.Add(displayName, new UpdateItem(localFilename, remotehash, (new Uri(rootUrl, displayName)).AbsoluteUri, displayName, isArchive));
                                    }
                                }
                            }
                        }
                    }
                }
                return new BootstrapUpdater_CheckForUpdates(needtobeupdated, false, false, null);
            }
            else
            {
                throw new BootstrapUpdaterException();
            }
        }
    }
}

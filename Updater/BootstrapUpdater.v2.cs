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
        private async Task<BootstrapUpdater_CheckForUpdates> ParseFileList_2(JsonDocument document, string rootDirectory, string entryExecutableName)
        {
            var needtobeupdated = new Dictionary<string, UpdateItem>(StringComparer.OrdinalIgnoreCase);

            var rootelement = document.RootElement;

            if (rootelement.TryGetProperty("critical-files", out var prop_criticalfiles))
            {
                if (prop_criticalfiles.ValueKind == JsonValueKind.Object)
                {
                    if (rootelement.TryGetProperty("root-url-critical", out var prop_rootUrl) && prop_rootUrl.ValueKind == JsonValueKind.String &&
                        Uri.TryCreate(prop_rootUrl.GetString(), UriKind.Absolute, out var rootUrl))
                    {
                        using (var objWalker = prop_criticalfiles.EnumerateObject())
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
                                    long size;
                                    if (prop_val.TryGetProperty("size", out var item_prop_size) && item_prop_size.ValueKind == JsonValueKind.Number)
                                    {
                                        size = item_prop_size.GetInt64();
                                    }
                                    else
                                    {
                                        size = -1;
                                    }
                                    if (File.Exists(localFilename))
                                    {
                                        var hash = await SHA1Hash.ComputeHashFromFileAsync(localFilename);
                                        if (!string.Equals(hash, remotehash, StringComparison.OrdinalIgnoreCase))
                                        {
                                            bool isArchive = prop_val.TryGetProperty("archive", out var item_prop_archive) ? (item_prop_archive.ValueKind == JsonValueKind.True) : false;
                                            bool isEntry = prop_val.TryGetProperty("entry", out var item_prop_entry) ? (item_prop_entry.ValueKind == JsonValueKind.True) : false;
                                            needtobeupdated.Add(displayName, new UpdateItem_v2(localFilename, remotehash, (new Uri(rootUrl, displayName)).AbsoluteUri, displayName, isArchive, size, true, isEntry));
                                        }
                                    }
                                    else
                                    {
                                        bool isArchive = prop_val.TryGetProperty("archive", out var item_prop_archive) ? (item_prop_archive.ValueKind == JsonValueKind.True) : false;
                                        bool isEntry = prop_val.TryGetProperty("entry", out var item_prop_entry) ? (item_prop_entry.ValueKind == JsonValueKind.True) : false;
                                        needtobeupdated.Add(displayName, new UpdateItem_v2(localFilename, remotehash, (new Uri(rootUrl, displayName)).AbsoluteUri, displayName, isArchive, size, true, isEntry));
                                    }
                                }
                            }
                        }
                    }
                }
                return new BootstrapUpdater_CheckForUpdates(needtobeupdated, false, false, null);
            }

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
                                    long size;
                                    if (prop_val.TryGetProperty("size", out var item_prop_size) && item_prop_size.ValueKind == JsonValueKind.Number)
                                    {
                                        size = item_prop_size.GetInt64();
                                    }
                                    else
                                    {
                                        size = -1;
                                    }
                                    if (File.Exists(localFilename))
                                    {
                                        var hash = await SHA1Hash.ComputeHashFromFileAsync(localFilename);
                                        if (!string.Equals(hash, remotehash, StringComparison.OrdinalIgnoreCase))
                                        {
                                            bool isArchive = prop_val.TryGetProperty("archive", out var item_prop_archive) ? (item_prop_archive.ValueKind == JsonValueKind.True) : false;
                                            bool isEntry = prop_val.TryGetProperty("entry", out var item_prop_entry) ? (item_prop_entry.ValueKind == JsonValueKind.True) : false;
                                            needtobeupdated.Add(displayName, new UpdateItem_v2(localFilename, remotehash, (new Uri(rootUrl, displayName)).AbsoluteUri, displayName, isArchive, size, true, isEntry));
                                        }
                                    }
                                    else
                                    {
                                        bool isArchive = prop_val.TryGetProperty("archive", out var item_prop_archive) ? (item_prop_archive.ValueKind == JsonValueKind.True) : false;
                                        bool isEntry = prop_val.TryGetProperty("entry", out var item_prop_entry) ? (item_prop_entry.ValueKind == JsonValueKind.True) : false;
                                        needtobeupdated.Add(displayName, new UpdateItem_v2(localFilename, remotehash, (new Uri(rootUrl, displayName)).AbsoluteUri, displayName, isArchive, size, true, isEntry));
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

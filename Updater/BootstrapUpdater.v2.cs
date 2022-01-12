using Leayal.SharedInterfaces.Communication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Leayal.PSO2Launcher.Helper;

namespace Leayal.PSO2Launcher.Updater
{
    public partial class BootstrapUpdater : IBootstrapUpdater, IBootstrapUpdater_v2
    {
        private static async Task<bool> FileCheck_2(Dictionary<string, UpdateItem> needtobeupdated, Uri rootUrl, string displayName, JsonElement prop_val, string rootDirectory, bool iscritical, string entryExecutableName, Architecture arch)
        {
            if (prop_val.TryGetProperty("cpu", out var item_cpu) && item_cpu.ValueKind == JsonValueKind.String)
            {
                if (string.Equals(item_cpu.GetString(), "x86", StringComparison.OrdinalIgnoreCase) && arch != Architecture.X86)
                {
                    return false;
                }
                else if (string.Equals(item_cpu.GetString(), "x64", StringComparison.OrdinalIgnoreCase) && arch != Architecture.X64)
                {
                    return false;
                }
                else if (string.Equals(item_cpu.GetString(), "arm64", StringComparison.OrdinalIgnoreCase) && arch != Architecture.Arm64)
                {
                    return false;
                }
                else if (string.Equals(item_cpu.GetString(), "arm", StringComparison.OrdinalIgnoreCase) && arch != Architecture.Arm)
                {
                    return false;
                }
            }
            if (prop_val.TryGetProperty("sha1", out var item_prop_sha1) && item_prop_sha1.ValueKind == JsonValueKind.String)
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
                        needtobeupdated.Add(displayName, new UpdateItem_v2(localFilename, remotehash, (new Uri(rootUrl, displayName)).AbsoluteUri, displayName, isArchive, size, iscritical, isEntry));
                        return true;
                    }
                }
                else
                {
                    bool isArchive = prop_val.TryGetProperty("archive", out var item_prop_archive) ? (item_prop_archive.ValueKind == JsonValueKind.True) : false;
                    bool isEntry = prop_val.TryGetProperty("entry", out var item_prop_entry) ? (item_prop_entry.ValueKind == JsonValueKind.True) : false;
                    needtobeupdated.Add(displayName, new UpdateItem_v2(localFilename, remotehash, (new Uri(rootUrl, displayName)).AbsoluteUri, displayName, isArchive, size, iscritical, isEntry));
                    return true;
                }
            }
            return false;
        }

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
                                var item = objWalker.Current;
                                await FileCheck_2(needtobeupdated, rootUrl, item.Name, item.Value, rootDirectory, true, entryExecutableName, this._osArch);
                            }
                        }
                    }
                }
            }

            bool shouldReload = false;

            if (rootelement.TryGetProperty("files", out var prop_files))
            {
                if (prop_files.ValueKind == JsonValueKind.Object)
                {
                    if (rootelement.TryGetProperty("root-url", out var prop_rootUrl) && prop_rootUrl.ValueKind == JsonValueKind.String &&
                        Uri.TryCreate(prop_rootUrl.GetString(), UriKind.Absolute, out var rootUrl))
                    {
                        // This is no-good and roundabout way.
                        var avoidRecheck = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        if (prop_files.TryGetProperty(AssemblyFilenameOfMySelf, out var prop_thisself) && prop_thisself.ValueKind == JsonValueKind.Object)
                        {
                            avoidRecheck.Add(AssemblyFilenameOfMySelf);
                            if (await FileCheck_2(needtobeupdated, rootUrl, AssemblyFilenameOfMySelf, prop_thisself, rootDirectory, false, entryExecutableName, this._osArch))
                            {
                                shouldReload = true;
                            }
                        }
                        for (int i = 0; i < this.ReferencedAssemblyFilenameOfMySelf.Count; i++)
                        {
                            var asm_name = this.ReferencedAssemblyFilenameOfMySelf[i];
                            if (!string.IsNullOrEmpty(asm_name) && prop_files.TryGetProperty(asm_name, out var prop_depend) && prop_depend.ValueKind == JsonValueKind.Object)
                            {
                                if (avoidRecheck.Add(AssemblyFilenameOfMySelf))
                                {
                                    if (await FileCheck_2(needtobeupdated, rootUrl, AssemblyFilenameOfMySelf, prop_thisself, rootDirectory, false, entryExecutableName, this._osArch))
                                    {
                                        shouldReload = true;
                                    }
                                }
                            }
                        }

                        if (!shouldReload)
                        {
                            using (var objWalker = prop_files.EnumerateObject())
                            {
                                while (objWalker.MoveNext())
                                {
                                    var item = objWalker.Current;
                                    var asm_name = item.Name;
                                    if (avoidRecheck.Add(asm_name))
                                    {
                                        await FileCheck_2(needtobeupdated, rootUrl, asm_name, item.Value, rootDirectory, false, entryExecutableName, this._osArch);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                throw new BootstrapUpdaterException();
            }
            return new BootstrapUpdater_CheckForUpdates(needtobeupdated, false, shouldReload, null);
        }
    }
}

using Leayal.PSO2Launcher.Helper;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Net;
using System.Threading.Tasks;
// using System.Net.Http;
using System.Text.Json;
using System.Windows.Forms;
using Leayal.SharedInterfaces.Communication;

namespace Leayal.PSO2Launcher.Updater
{
    public class BootstrapUpdater : IBootstrapUpdater
    {
        public BootstrapUpdater()
        {
            
        }

        public async Task<BootstrapUpdater_CheckForUpdates> CheckForUpdatesAsync(string rootDirectory, string entryExecutableName)
        {
            // Fetch from internet a list then check for SHA-1.
            using (var wc = new WebClient())
            {
                using (var jsonStream = await wc.OpenReadTaskAsync("file:///E:/All%20Content/VB_Project/visual%20studio%202019/PSO2-Launcher-CSharp/Test/a.json"))
                using (var doc = await JsonDocument.ParseAsync(jsonStream))
                {
                    if (doc.RootElement.TryGetProperty("rep-version", out var prop_response_ver) && prop_response_ver.TryGetInt32(out var response_ver))
                    {
                        if (response_ver == 1)
                        {
                            return await this.ParseFileList_1(doc, rootDirectory, entryExecutableName);
                        }
                        else
                        {
                            // Latest kind of data
                            return await this.ParseFileList_1(doc, rootDirectory, entryExecutableName);
                        }
                    }
                    else
                    {
                        throw new BootstrapUpdaterException();
                    }
                }
            }
        }

        private async Task<BootstrapUpdater_CheckForUpdates> ParseFileList_1(JsonDocument document, string rootDirectory, string entryExecutableName)
        {
            var needtobeupdated = new Dictionary<string, UpdateItem>(StringComparer.OrdinalIgnoreCase);

            var rootelement = document.RootElement;

            if (rootelement.TryGetProperty("files", out var prop_files))
            {
                if (prop_files.ValueKind == JsonValueKind.Object)
                {
                    using (var objWalker = prop_files.EnumerateObject())
                    {
                        var currentNetAsm = Assembly.GetExecutingAssembly();
                        while (objWalker.MoveNext())
                        {
                            var item_prop = objWalker.Current;
                            var displayName = item_prop.Name;
                            var prop_val = item_prop.Value;
                            if ((prop_val.TryGetProperty("sha1", out var item_prop_sha1) && item_prop_sha1.ValueKind == JsonValueKind.String)
                                && (prop_val.TryGetProperty("url", out var item_prop_url) && item_prop_url.ValueKind == JsonValueKind.String))
                            {
                                var localFilename = Path.GetFullPath(Path.Combine("bin", displayName), rootDirectory);
                                var hash = await SHA1Hash.ComputeHashFromFileAsync(localFilename);
                                if (!string.Equals(hash, item_prop_sha1.GetString(), StringComparison.OrdinalIgnoreCase))
                                {
                                    bool isArchive = prop_val.TryGetProperty("archive", out var item_prop_archive) ? (item_prop_archive.ValueKind == JsonValueKind.True) : false;

                                    needtobeupdated.Add(displayName, new UpdateItem(localFilename, item_prop_url.GetString(), displayName, isArchive));
                                }
                            }
                        }
                    }
                }

                needtobeupdated.Clear(); // Simulate no new items.

                if (needtobeupdated.ContainsKey("PSO2Launcher.exe"))
                {
                    return new BootstrapUpdater_CheckForUpdates(needtobeupdated, true, false, entryExecutableName + ".tmp");
                }
                else
                {
                    var currentNetAsm = Assembly.GetExecutingAssembly();
                    if (needtobeupdated.ContainsKey($"{currentNetAsm.GetName().Name}.dll"))
                    {
                        return new BootstrapUpdater_CheckForUpdates(needtobeupdated, false, true, null);
                    }
                }

                return new BootstrapUpdater_CheckForUpdates(needtobeupdated, false, false, null);
            }
            else
            {
                throw new BootstrapUpdaterException();
            }
        }

        public bool? DisplayUpdatePrompt(Form? parent)
        {
            var placeholder = new Form();
            DialogResult result;
            if (parent == null)
            {
                result = placeholder.ShowDialog();
            }
            else
            {
                result = placeholder.ShowDialog(parent);
            }
            return result switch
            {
                DialogResult.OK => true,
                DialogResult.Cancel => false,
                _ => null
            };
        }
    }
}

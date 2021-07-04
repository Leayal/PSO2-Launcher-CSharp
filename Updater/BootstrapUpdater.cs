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
using Leayal.SharedInterfaces;

namespace Leayal.PSO2Launcher.Updater
{
    public class BootstrapUpdater : IBootstrapUpdater
    {
        private readonly WebClient wc;

        public BootstrapUpdater()
        {
            this.wc = new WebClient()
            {
                Proxy = null,
                CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore),
                Credentials = null,
                UseDefaultCredentials = false
            };
        }

        public event EventHandler<FileDownloadedEventArgs> FileDownloaded;
        public event EventHandler<StringEventArgs> StepChanged;

        public Task<BootstrapUpdater_CheckForUpdates> CheckForUpdatesAsync(string rootDirectory, string entryExecutableName)
        {
            // Fetch from internet a list then check for SHA-1.
            return Task.Run(async () =>
            {
                using (var jsonStream = await this.wc.OpenReadTaskAsync("https://leayal.github.io/PSO2-Launcher-CSharp/publish/update.json"))
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
            });
        }

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

        public bool? DisplayUpdatePrompt(Form? parent)
        {
            DialogResult result;
            if (parent == null)
            {
                result = MessageBox.Show("Found new version. Update the launcher?\r\nYes: Update [Recommended]\r\nNo: Continue using old version\r\nCancel: Exit", "Question", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            }
            else
            {
                result = MessageBox.Show(parent, "Found new version. Update the launcher?\r\nYes: Update [Recommended]\r\nNo: Continue using old version\r\nCancel: Exit", "Question", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            }
            return result switch
            {
                DialogResult.Yes => true,
                DialogResult.No => false,
                _ => null
            };
        }

        public Task<bool?> PerformUpdate(BootstrapUpdater_CheckForUpdates updateinfo)
        {
            return Task.Run<bool?>(async () =>
            {
                var e_filedownload = this.FileDownloaded;
                var e_step = this.StepChanged;

                string filepath, tmpFilename;
                foreach (var item in updateinfo.Items)
                {
                    filepath = item.Value.LocalFilename;
                    tmpFilename = filepath + ".dtmp";

                    e_step?.Invoke(this, new StringEventArgs($"Downloading '{item.Value.DisplayName}'"));
                    await this.wc.DownloadFileTaskAsync(item.Value.DownloadUrl, tmpFilename);

                    var hash_downloaded = SHA1Hash.ComputeHashFromFile(tmpFilename);
                    if (string.Equals(hash_downloaded, item.Value.SHA1Hash, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Move(tmpFilename, filepath, true);
                    }
                    else
                    {
                        throw new WebException();
                    }
                    e_filedownload?.Invoke(this, new FileDownloadedEventArgs(item.Value));
                }

                // Force restart anyway.
                return true;
            });
        }

        public void Dispose() => this.wc.Dispose();
    }
}

﻿using Leayal.PSO2Launcher.Helper;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
// using System.Net.Http;
using System.Text.Json;
using System.Windows.Forms;
using Leayal.SharedInterfaces.Communication;
using Leayal.SharedInterfaces;
using System.Runtime.Loader;
using System.Threading;
using System.Diagnostics;

namespace Leayal.PSO2Launcher.Updater
{
    public partial class BootstrapUpdater : IBootstrapUpdater, IBootstrapUpdater_v2
    {
        private readonly Assembly ThisAssembly;
        private readonly string AssemblyFilenameOfMySelf;
        private readonly string[] ReferencedAssemblyFilenameOfMySelf;

        private readonly HttpClient wc;
        private bool requireBootstrapUpdate, recommendBootstrapUpdate;
        private readonly AssemblyLoadContext? _loadedAssemblies;

        private readonly int bootstrapversion;

        private bool failToCheck;

        public BootstrapUpdater() : this(0, null) { }

        public BootstrapUpdater(int bootstrapversion, AssemblyLoadContext? loadedAssemblies)
        {
            if (EventWaitHandle.TryOpenExisting("pso2lealauncher-v2-waiter", out var waitHandle))
            {
                using (waitHandle)
                {
                    if (waitHandle.Set())
                    {
                        Application.Exit();
                    }
                }
            }
            this.ThisAssembly = Assembly.GetExecutingAssembly();
            this.AssemblyFilenameOfMySelf = $"{this.ThisAssembly.GetName().Name}.dll";
            var referenced = this.ThisAssembly.GetReferencedAssemblies();
            this.ReferencedAssemblyFilenameOfMySelf = new string[referenced.Length];
            for (int i = 0; i < referenced.Length; i++)
            {
                this.ReferencedAssemblyFilenameOfMySelf[i] = referenced[i].Name;
            }

            this.failToCheck = false;
            this.bootstrapversion = bootstrapversion;
            this.requireBootstrapUpdate = false;
            this.recommendBootstrapUpdate = false;
            this._loadedAssemblies = loadedAssemblies;
            this.wc = new HttpClient(new SocketsHttpHandler()
            {
                Proxy = null,
                UseProxy = false,
                EnableMultipleHttp2Connections = true,
                ConnectTimeout = TimeSpan.FromSeconds(10),
                DefaultProxyCredentials = null,
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.All,
                KeepAlivePingTimeout = TimeSpan.FromSeconds(5),
                AllowAutoRedirect = true,
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests
            });
            this.wc.DefaultRequestHeaders.Add("User-Agent", "PSO2LeaLauncher");
        }

        public event EventHandler<FileDownloadedEventArgs> FileDownloaded;
        public event Action<long> ProgressBarValueChanged;
        public event EventHandler<StringEventArgs> StepChanged;
        public event Action<long> ProgressBarMaximumChanged;

        public Task<BootstrapUpdater_CheckForUpdates> CheckForUpdatesAsync(string rootDirectory, string entryExecutableName)
        {
            // Fetch from internet a list then check for SHA-1.
            return Task.Run(async () =>
            {
                string jsonData;
                try
                {
#if DEBUG
                    jsonData = File.ReadAllText(Path.Combine(rootDirectory, @"..\docs\publish\update.json"));
#else
                    jsonData = await this.wc.GetStringAsync("https://leayal.github.io/PSO2-Launcher-CSharp/publish/update.json");
#endif
                }
                catch
                {
                    this.failToCheck = true;
                    return new BootstrapUpdater_CheckForUpdates(new Dictionary<string, UpdateItem>(StringComparer.OrdinalIgnoreCase)
                    {
                        { string.Empty, new UpdateItem(string.Empty, string.Empty, string.Empty, string.Empty) }
                    }, false, false, null);
                }

                if (string.IsNullOrWhiteSpace(jsonData))
                {
                    this.failToCheck = true;
                    return new BootstrapUpdater_CheckForUpdates(new Dictionary<string, UpdateItem>(StringComparer.OrdinalIgnoreCase)
                    {
                        { string.Empty, new UpdateItem(string.Empty, string.Empty, string.Empty, string.Empty) }
                    }, false, false, null);
                }

                using (var doc = JsonDocument.Parse(jsonData))
                {
                    if (doc.RootElement.TryGetProperty("rep-version", out var prop_response_ver) && prop_response_ver.TryGetInt32(out var response_ver))
                    {
                        if (response_ver == 1)
                        {
                            return await this.ParseFileList_1(doc, rootDirectory, entryExecutableName);
                        }
                        else if (response_ver == 2)
                        {
                            if (this.bootstrapversion < 1)
                            {
                                this.recommendBootstrapUpdate = true;
                                return await this.ParseFileList_1(doc, rootDirectory, entryExecutableName);
                            }
                            else
                            {
                                return await this.ParseFileList_2(doc, rootDirectory, entryExecutableName);
                            }
                        }
                        else
                        {
                            return await this.ParseFileList_2(doc, rootDirectory, entryExecutableName);
                        }
                    }
                    else
                    {
                        throw new BootstrapUpdaterException();
                    }
                }
            });
        }

        public bool? DisplayUpdatePrompt(Form? parent)
        {
            DialogResult result;
            if (this.failToCheck)
            {
                if (parent == null)
                {
                    result = MessageBox.Show("Failed to check for launcher updates.\r\nDo you want to continue anyway?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                }
                else
                {
                    result = MessageBox.Show(parent, "Failed to check for launcher updates.\r\nDo you want to continue anyway?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                }
                return result switch
                {
                    DialogResult.Yes => false,
                    _ => null
                };
            }
            else
            {
                if (this.recommendBootstrapUpdate)
                {
                    if (parent == null)
                    {
                        result = MessageBox.Show("Found new version. Update the launcher?\r\nYes: Update [Recommended]\r\nNo: Continue using old version\r\nCancel: Exit", "Question", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    }
                    else
                    {
                        result = MessageBox.Show(parent, "Found new version. Update the launcher?\r\nYes: Update [Recommended]\r\nNo: Continue using old version\r\nCancel: Exit", "Question", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    }
                }
                else
                {
                    if (parent == null)
                    {
                        result = MessageBox.Show("Found new version. Update the launcher?\r\nYes: Update [Recommended]\r\nNo: Continue using old version\r\nCancel: Exit", "Question", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    }
                    else
                    {
                        result = MessageBox.Show(parent, "Found new version. Update the launcher?\r\nYes: Update [Recommended]\r\nNo: Continue using old version\r\nCancel: Exit", "Question", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    }
                }
                return result switch
                {
                    DialogResult.Yes => true,
                    DialogResult.No => false,
                    _ => null
                };
            }
        }

        public Task<bool?> PerformUpdate(BootstrapUpdater_CheckForUpdates updateinfo)
        {
            return Task.Run<bool?>(async () =>
            {
                var e_filedownload = this.FileDownloaded;
                var e_step = this.StepChanged;
                var e_progressbarMax = this.ProgressBarMaximumChanged;
                var e_downloadProgress = this.ProgressBarValueChanged;
                byte[] buffer = new byte[1024 * 16];

                bool isNewBootstrap = this.bootstrapversion > 0;
                bool shouldRestart = isNewBootstrap ? false : true;
                bool shouldReload = updateinfo.RequireReload;

                string filepath, tmpFilename;
                e_progressbarMax?.Invoke(100);
                foreach (var item in updateinfo.Items)
                {
                    filepath = item.Value.LocalFilename;
                    tmpFilename = filepath + ".dtmp";

                    if (!shouldRestart && !shouldReload)
                    {
                        shouldReload = string.Equals(filepath, item.Key, StringComparison.OrdinalIgnoreCase);
                    }

                    e_step?.Invoke(this, new StringEventArgs($"Downloading '{item.Value.DisplayName}'"));

                    if (isNewBootstrap)
                    {
                        if (item.Value is UpdateItem_v2 itemv2)
                        {
                            if (itemv2.FileSize == -1)
                            {
                                await this.wc.DownloadFileTaskAsync(item.Value.DownloadUrl, tmpFilename);
                            }
                            else
                            {
                                // e_progressbarMax?.Invoke(itemv2.FileSize);
                                var request = new HttpRequestMessage(HttpMethod.Get, item.Value.DownloadUrl);
                                using (var response = await this.wc.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                                {
                                    response.EnsureSuccessStatusCode();
                                    using (var remoteStream = response.Content.ReadAsStream())
                                    {
                                        Directory.CreateDirectory(Path.GetDirectoryName(tmpFilename));
                                        using (var localStream = File.Create(tmpFilename))
                                        {
                                            long totalbyte = itemv2.FileSize;
                                            long bytetoDownload = totalbyte;
                                            long bytedownloaded = 0;
                                            int byteread = remoteStream.Read(buffer, 0, buffer.Length);
                                            while (byteread > 0 && bytetoDownload != 0)
                                            {
                                                localStream.Write(buffer, 0, byteread);
                                                bytedownloaded += byteread;
                                                bytetoDownload -= byteread;
                                                double val = bytedownloaded * 100 / totalbyte;
                                                e_downloadProgress?.Invoke(Convert.ToInt32(Math.Floor(val)));
                                                if (bytetoDownload > buffer.Length)
                                                {
                                                    byteread = remoteStream.Read(buffer, 0, buffer.Length);
                                                }
                                                else
                                                {
                                                    byteread = remoteStream.Read(buffer, 0, (int)bytetoDownload);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            await this.wc.DownloadFileTaskAsync(item.Value.DownloadUrl, tmpFilename);
                        }
                    }
                    else
                    {
                        await this.wc.DownloadFileTaskAsync(item.Value.DownloadUrl, tmpFilename);
                    }

                    var hash_downloaded = SHA1Hash.ComputeHashFromFile(tmpFilename);
                    if (string.Equals(hash_downloaded, item.Value.SHA1Hash, StringComparison.OrdinalIgnoreCase))
                    {
                        if (item.Value is UpdateItem_v2 itemv2)
                        {
                            if (itemv2.IsCritical)
                            {
                                shouldRestart = true;
                                if (itemv2.IsEntry)
                                {
                                    var entryAsm = Assembly.GetEntryAssembly();
                                    var location = entryAsm.Location;
                                    if (!string.IsNullOrEmpty(location))
                                    {
                                        location = Path.GetFullPath(location);
                                        File.Move(location, Path.ChangeExtension(location, ".bak"), true);
                                        File.Move(tmpFilename, location, true);
                                    }
                                    else
                                    {
                                        var dllName = Path.ChangeExtension(RuntimeValues.EntryExecutableFilename, ".dll");
                                        location = Path.GetFullPath(Path.Combine("dotnet", dllName), RuntimeValues.RootDirectory);
                                        if (!File.Exists(location))
                                        {
                                            location = Path.GetFullPath(dllName, RuntimeValues.RootDirectory);
                                        }
                                        if (File.Exists(location))
                                        {
                                            File.Move(location, Path.ChangeExtension(location, ".bak"), true);
                                            File.Move(tmpFilename, location, true);
                                        }
                                    }
                                }
                                else
                                {
                                    File.Move(filepath, Path.ChangeExtension(filepath, ".bak"), true);
                                    File.Move(tmpFilename, filepath, true);
                                }
                            }
                            else
                            {
                                File.Move(tmpFilename, filepath, true);
                            }
                        }
                        else
                        {
                            File.Move(tmpFilename, filepath, true);
                        }
                    }
                    else
                    {
                        throw new WebException("The downloaded file has been download incorrectly.");
                    }
                    e_filedownload?.Invoke(this, new FileDownloadedEventArgs(item.Value));
                }

                // This take preceded so shouldreload is totally wasted
                if (!shouldRestart)
                {
                    if (this._loadedAssemblies != null)
                    {
                        foreach (var loaded in this._loadedAssemblies.Assemblies)
                        {
                            if (updateinfo.Items.ContainsKey(loaded.GetName().Name + ".dll"))
                            {
                                shouldRestart = true;
                                break;
                            }
                        }
                        
                    }
                }

                if (shouldRestart)
                {
                    return true;
                }

                if (shouldReload)
                {
                    return false;
                }

                return null;
            });
        }

        public void Dispose() => this.wc.Dispose();
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Windows.Forms;
using Leayal.SharedInterfaces.Communication;
using Leayal.SharedInterfaces;
using System.Runtime.Loader;
using System.Threading;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using Leayal.PSO2Launcher.Helper;
using System.Diagnostics.CodeAnalysis;
using System.Security.Policy;

namespace Leayal.PSO2Launcher.Updater
{
    public partial class BootstrapUpdater : IBootstrapUpdater, IBootstrapUpdater_v2
    {
#nullable disable
        private readonly HttpClient wc;
        private readonly Assembly ThisAssembly;
        private readonly string AssemblyFilenameOfMySelf;
        private readonly IReadOnlyList<string> ReferencedAssemblyFilenameOfMySelf;
#nullable restore
        private readonly AssemblyLoadContext? _loadedAssemblies;
        private readonly Architecture _osArch;

        private bool recommendBootstrapUpdate, isRuntimeObsoleted; // requireBootstrapUpdate
        
        private readonly int bootstrapversion;
        private bool failToCheck;

        public BootstrapUpdater() : this(0, null) { }

        private readonly Form? bootstrapForm;

        public BootstrapUpdater(int bootstrapversion, AssemblyLoadContext? loadedAssemblies) : this(bootstrapversion, loadedAssemblies, null) { }

        public BootstrapUpdater(int bootstrapversion, AssemblyLoadContext? loadedAssemblies, Form? mainWindow)
        {
            if (bootstrapversion < 4)
            {
                if (EventWaitHandle.TryOpenExisting("pso2lealauncher-v2-waiter", out var waitHandle))
                {
                    using (waitHandle)
                    {
                        if (waitHandle.Set())
                        {
                            Application.Exit();
                            return;
                        }
                    }
                }
            }

            this.bootstrapForm = mainWindow;

            static void YeeeetMyself(Form? window)
            {
                if (window != null)
                {
                    window.Close();
                }
                else
                {
                    Application.Exit();
                }
                return;
            }

            static DialogResult ShowMsgBox(Form? owner, string message, string title, MessageBoxButtons button, MessageBoxIcon icon)
            {
                return owner == null ? MessageBox.Show(message, title, button, icon)
                : MessageBox.Show(owner, message, title, button, icon);
            }

            StringBuilder? sb = null;

            // If we're compiling targeting .NET Desktop Runtime older than 8.
#if !NET8_0_OR_GREATER
            Version runtimeVersion = Environment.Version,
                recommendedVersion = new Version(8, 0, 0);

            if (runtimeVersion < recommendedVersion)
            {
                // Upgrade your runtime is recommended. For security reasons.
                sb = new StringBuilder(768);
                sb.Append("You're still using .NET Desktop Runtime v").Append(runtimeVersion).Append('.')
                    .AppendLine()
                    .Append("It is recommended to redownload the launcher bootstrap to use .NET8 Desktop Runtime (v").Append(recommendedVersion).Append(" or newer).")
                    .AppendLine()
                    .AppendLine()
                    .Append("The launcher has abandoned supporting any runtimes before version ").Append(recommendedVersion).Append('.')
                    .AppendLine()
                    .Append("While you can still use the launcher without updating, you may miss out future bugfixes and improvements from launcher development.")
                    .AppendLine()
                    .AppendLine()
                    .AppendLine("Please go to the launcher's release page on Github and download the latest release package to update the launcher bootstrap which will target the newer .NET Desktop Runtime v8.")
                    .AppendLine()
                    .Append("[Yes: Open download page and exit launcher; No: Continue using launcher; Cancel: Exit launcher]");

                var informResult = ShowMsgBox(this.bootstrapForm, sb.ToString(), "Critical Information", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
                switch (informResult)
                {
                    case DialogResult.Yes:
                        var url = new Uri("https://leayal.github.io/PSO2-Launcher-CSharp/?page=downloads");
                        using (var proc = Process.Start(Path.GetFullPath("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.Windows)), $"\"{url.AbsoluteUri}\""))
                        {
                            proc?.WaitForExit(1000);
                        }
                        YeeeetMyself(mainWindow);
                        return;
                    case DialogResult.Cancel:
                        YeeeetMyself(mainWindow);
                        return;
                }
            }
#endif

            // In case the user uses `framework-dependent` type which follows the framework that is available on non-AMD64 OS.
            this._osArch = RuntimeInformation.OSArchitecture;
            if (this._osArch != Architecture.X64)
            {
                if (sb == null)
                {
                    // The whole thing below is around 270+ characters already.
                    sb = new StringBuilder(384);
                }
                else
                {
                    sb.Clear();
                }
                sb.Append("Phantasy Star Online 2: New Genesis (Japan) only has 64-bit (specifically, 'AMD64' or 'x86_64' architecture) game client.")
                    .AppendLine()
                    .AppendLine()
                    .Append("Your current operating system is ");
                switch (this._osArch)
                {
                    case Architecture.X86:
                        sb.Append("32-bit (or 'x86')");
                        break;
                    case Architecture.Arm:
                        sb.Append("ARM 32-bit (or 'ARM')");
                        break;
                    case Architecture.Arm64:
                        sb.Append("ARM 64-bit (or 'ARM64')");
                        break;
                    default:
                        sb.Append("'Unknown Architecture'");
                        break;
                }
                sb.Append(", which will not be able to run the game client.")
                    .AppendLine()
                    .AppendLine()
                    .Append("Are you sure you want to continue anyway?");
                var informResult = ShowMsgBox(this.bootstrapForm, sb.ToString(), "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (informResult != DialogResult.Yes)
                {
                    YeeeetMyself(mainWindow);
                    return;
                }
            }

            this.ThisAssembly = Assembly.GetExecutingAssembly();
            this.AssemblyFilenameOfMySelf = this.ThisAssembly.GetName().Name + ".dll";
            var referenced = this.ThisAssembly.GetReferencedAssemblies();
            if (referenced != null)
            {
                var l_referenced = new List<string>(referenced.Length);
                for (int i = 0; i < referenced.Length; i++)
                {
                    if (referenced[i] != null && referenced[i].Name is string v && !string.IsNullOrEmpty(v))
                    {
                        l_referenced.Add(v + ".dll");
                    }
                }
                this.ReferencedAssemblyFilenameOfMySelf = l_referenced;
            }
            else
            {
                this.ReferencedAssemblyFilenameOfMySelf = Array.Empty<string>();
            }

            this.failToCheck = false;
            this.bootstrapversion = bootstrapversion;
            // this.requireBootstrapUpdate = false;
            this.recommendBootstrapUpdate = false;
            this._loadedAssemblies = loadedAssemblies ?? AssemblyLoadContext.GetLoadContext(this.ThisAssembly);
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

        public event EventHandler<FileDownloadedEventArgs>? FileDownloaded;
        public event Action<long>? ProgressBarValueChanged;
        public event EventHandler<StringEventArgs>? StepChanged;
        public event Action<long>? ProgressBarMaximumChanged;

        public Task<BootstrapUpdater_CheckForUpdates> CheckForUpdatesAsync(string rootDirectory, string entryExecutableName)
        {
            // Fetch from internet a list then check for SHA-1.
            return Task.Run(async () =>
            {
                string jsonData;
                try
                {
#if DEBUG
                    jsonData = File.ReadAllText(Path.Combine(rootDirectory, @"..\docs\publish\v8\update.json"));
#else
                    jsonData = await this.wc.GetStringAsync(SharedCode.LauncherUpdateManifest);
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
                                var result_list = await this.ParseFileList_2(doc, rootDirectory, entryExecutableName);
                                if (result_list.Items.Count == 0)
                                {
                                    isRuntimeObsoleted = true;
                                    result_list.Items.Add(string.Empty, new UpdateItem(string.Empty, string.Empty, string.Empty, string.Empty));
                                }
                                return result_list;
                            }
                        }
                        else if (response_ver == 3)
                        {
                            var ver = Assembly.GetEntryAssembly()?.GetName()?.Version ?? new Version(3, 0);
                            if (ver.Major < 3)
                            {
                                if (this.bootstrapversion < 2)
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

#nullable enable
        public bool? DisplayUpdatePrompt(Form? parent)
        {
            DialogResult result;
            if (this.isRuntimeObsoleted)
            {
                result = MsgBoxShortHand(parent, $"The current release version of the launcher's bootstrap is obsolete.{Environment.NewLine}It is recommended to download the newer bootstrap release.{Environment.NewLine}{Environment.NewLine}Do you want to download now or keep using the current version?{Environment.NewLine}Yes = Open the download page{Environment.NewLine}No = Continue using this{Environment.NewLine}Cancel = Exit", "Question", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    Process.Start(Path.GetFullPath("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.Windows)), @"https://github.com/Leayal/PSO2-Launcher-CSharp/releases/latest")?.Dispose();
                }
                return result switch
                {
                    DialogResult.No => false,
                    _ => null
                };
            }
            else if (this.failToCheck)
            {
                result = MsgBoxShortHand(parent, $"Failed to check for launcher updates.{Environment.NewLine}Do you want to continue anyway?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                return result switch
                {
                    DialogResult.Yes => false,
                    _ => null
                };
            }
            else
            {
                bool updatewitoutpromp = false, ignoreupdate = false;
                foreach (var arg in Environment.GetCommandLineArgs())
                {
                    if (string.Equals(arg, "--no-self-update-prompt", StringComparison.OrdinalIgnoreCase))
                    {
                        updatewitoutpromp = true;
                    }
                    else if (string.Equals(arg, "--no-self-update", StringComparison.OrdinalIgnoreCase))
                    {
                        ignoreupdate = true;
                    }
                }
                if (ignoreupdate)
                {
                    result = DialogResult.No;
                }
                else if (updatewitoutpromp)
                {
                    result = DialogResult.Yes;
                }
                else if (this.recommendBootstrapUpdate)
                {
                    result = MsgBoxShortHand(parent, $"Found new bootstrap version. Update the launcher's bootstrap?{Environment.NewLine}Yes: Update [Recommended]{Environment.NewLine}No: Continue using old version{Environment.NewLine}Cancel: Exit", "Question", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                }
                else
                {
                    result = MsgBoxShortHand(parent, $"Found new version. Update the launcher?{Environment.NewLine}Yes: Update [Recommended]{Environment.NewLine}No: Continue using old version{Environment.NewLine}Cancel: Exit", "Question", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                }
                return result switch
                {
                    DialogResult.Yes => true,
                    DialogResult.No => false,
                    _ => null
                };
            }
        }

        private static DialogResult MsgBoxShortHand(Form? parent, string msg, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return (parent == null) ? MessageBox.Show(msg, title, buttons, icon) : MessageBox.Show(parent, msg, title, buttons, icon);
        }
#nullable restore

        public async Task<bool?> PerformUpdate(BootstrapUpdater_CheckForUpdates updateinfo)
        {
            return await Task.Run<bool?>(async () =>
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
                                        if (Path.GetDirectoryName(tmpFilename) is string dirPath && !string.IsNullOrEmpty(dirPath))
                                        {
                                            Directory.CreateDirectory(dirPath);
                                        }
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
                                    var location = Assembly.GetEntryAssembly()?.Location ?? string.Empty;
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

                if (shouldRestart || shouldReload)
                {
                    var args = new List<string>(Environment.GetCommandLineArgs());
                    args.RemoveAt(0);
                    if (!args.Contains("--no-self-update-prompt"))
                    {
                        args.Add("--no-self-update-prompt");
                    }
                    RestartWithArgs(args);
                    return true;
                }

                return null;
            });
        }

        public void Dispose() => this.wc.Dispose();

        private static void RestartWithArgs(ICollection<string> commandLineArgs)
        {
            if (AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()) is AssemblyLoadContext context
                && TryGetAssemblyName(context, "Leayal.SharedInterfaces", out var asm)
                && asm.GetType("Leayal.SharedInterfaces.Compatibility.CompatStockFunc", false, false) is Type t)
            {
                if (t.GetMethod("LauncherController_RestartWithArgs", new Type[] { typeof(IEnumerable<string>) }) is MethodInfo mi)
                {
                    var action = mi.CreateDelegate<Action<IEnumerable<string>>>();
                    action.Invoke(commandLineArgs);
                }
            }
            else
            {
                var processStartInfo = new ProcessStartInfo(RuntimeValues.EntryExecutableFilename);
                if (commandLineArgs != null && commandLineArgs.Count != 0)
                {
                    foreach (var arg in commandLineArgs)
                    {
                        processStartInfo.ArgumentList.Add(arg);
                    }
                }
                AppDomain.CurrentDomain.ProcessExit += new EventHandler((sender, args) =>
                {
                    Process.Start(processStartInfo)?.Dispose();
                });

                Application.Exit();
            }
        }

        private static bool TryGetAssemblyName(AssemblyLoadContext context, string name, [NotNullWhen(true)] out Assembly? assembly)
        {
            foreach (var asm in context.Assemblies)
            {
                if (asm != null && string.Equals(asm.GetName().Name ?? string.Empty, name, StringComparison.Ordinal))
                {
                    assembly = asm;
                    return true;
                }
            }

            assembly = null;
            return false;
        }
    }
}

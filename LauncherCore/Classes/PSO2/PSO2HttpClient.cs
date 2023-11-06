using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using System.Security;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Security.Cryptography;
using Leayal.Shared;
using Leayal.SharedInterfaces;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public class PSO2HttpClient : IDisposable
    {
        private static readonly int[] _hardcodedWhichAreUsuallyDeletedOnes;

        static PSO2HttpClient()
        {
            var names_hardcodedWhichAreUsuallyDeletedOne = new string[] {
                "data/win32/8608a2bc214fcffce7f3339ca6b290b4",
                "data/win32/32db82383bc3234605882b35bb5bca4d",
                "data/win32/c37b9bc003130afc0b0608a7191ee738",
                "data/win32/b2448e2023a4ceca7881d3acfaae96cd",
                "data/win32/74cdd2b68f9614e70dd0b67a80e4d723",
                "data/win32/b2effb45fc1161ef980bb45ab6611d79",
                "data/win32/d4455ebc2bef618f29106da7692ebc1a", // Likely to be the integrity table file to
                "data/win32/ffbff2ac5b7a7948961212cefd4d402c", // Likely to be the chat censorship file
                "data/win32/13b588fc47b0078d9d4623188a6ae440",
                "data/win32/9dc4e510eddebae273570fa5f5265eec",
                "data/win32/e7c15fb8c18091496b4ac8d5ee0511d6",
                "data/win32/00e5c66ef3b171161094f2b729121590",
                "data/win32/fd2c2ba34b4347d17585ed320858d775",
                "data/win32/711d974e5677f99b7f42acca71c9c2bc"
            };
            _hardcodedWhichAreUsuallyDeletedOnes = new int[names_hardcodedWhichAreUsuallyDeletedOne.Length];
            for (int i = 0; i < names_hardcodedWhichAreUsuallyDeletedOne.Length; i++)
            {
                _hardcodedWhichAreUsuallyDeletedOnes[i] = PathStringComparer.Default.GetHashCode(names_hardcodedWhichAreUsuallyDeletedOne[i]);
            }
            Array.Sort(_hardcodedWhichAreUsuallyDeletedOnes);
        }

        private readonly HttpClient client;
        private const string UA_AQUA_HTTP = "AQUA_HTTP",
            // PSO2Launcher = "PSO2Launcher",
            UA_PSO2_Launcher = "PSO2 Launcher",
            UA_pso2launcher = "pso2launcher",
            UA_WellbiaSite = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/119.0",
            Uri_WellbiaUninstaller = "https://wellbia.com/xuninstaller.zip";
        private readonly PersistentCacheManager? dataCache;

        // Need to add snail mode (for when internet is extremely unreliable).
        // Do it later.

        public PSO2HttpClient(HttpClient client) : this(client, string.Empty) { }

        public PSO2HttpClient(HttpClient client, string? cacheDirectory)
        {
            this.client = client;
            this.dataCache = (string.IsNullOrEmpty(cacheDirectory) ? null : PersistentCacheManager.Create(cacheDirectory));
        }

        #region | Convenient private methods |
        private static void SetUA_AQUA_HTTP(HttpRequestMessage request)
        {
            // request.Headers.UserAgent.Add(new ProductInfoHeaderValue(UA_AQUA_HTTP));
            request.Headers.Add("User-Agent", UA_AQUA_HTTP);
            request.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
            request.Headers.Pragma.ParseAdd("no-cache");
        }
                
        //private void SetUA_PSO2Launcher(HttpRequestMessage request)
        //{
        //    request.Headers.Add("User-Agent", UA_PSO2Launcher);
        //    request.Headers.Accept.ParseAdd("*/*");
        //    request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.7");
        //}

        private static void SetUA_pso2launcher(HttpRequestMessage request)
        {
            request.Headers.Add("User-Agent", UA_pso2launcher);
            // request.Headers.UserAgent.Add(new ProductInfoHeaderValue(UA_pso2launcher));
            request.Headers.Accept.ParseAdd("*/*");
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.7");
        }

        private static void SetUA_PSO2_Launcher(HttpRequestMessage request)
        {
            request.Headers.Add("User-Agent", UA_PSO2_Launcher);
            // request.Headers.UserAgent.Add(new ProductInfoHeaderValue(UA_PSO2Launcher));
        }
        #endregion

        #region | Simple public APIs |

#nullable enable
        public async Task<PSO2LoginToken> LoginPSO2Async(SecureString username, SecureString password, CancellationToken cancellationToken)
        {
            if (username == null)
            {
                throw new ArgumentNullException(nameof(username));
            }
            else if (username.Length == 0)
            {
                throw new ArgumentException(null, nameof(username));
            }
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }
            else if (password.Length == 0)
            {
                throw new ArgumentException(null, nameof(password));
            }
            var url = new Uri("https://auth.pso2.jp/auth/v1/auth");
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                SetUA_PSO2_Launcher(request);
                request.Headers.Host = url.Host;
                request.Headers.ConnectionClose = true;
                using (var content = new PSO2LoginContent(username, password))
                {
                    request.Content = content;

                    // Don't retry sending request. It may be considered as brute-force attack.
                    // Instead, let it throw naturally and then user can attempt another login by themselves (retry by themselves).

                    using (var response = await this.client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        using (var stream = response.Content.ReadAsStream(cancellationToken))
                        using (var doc = JsonDocument.Parse(stream))
                        {
                            var root = doc.RootElement;
                            if (root.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Number)
                            {
                                var code = result.GetInt32();
                                if (code == 0)
                                {
                                    return new PSO2LoginToken(in root);
                                }
                                else
                                {
                                    throw new PSO2LoginException(code);
                                }
                            }
                            else
                            {
                                throw new UnexpectedDataFormatException();
                            }
                        }
                    }
                }
            }
        }

        public async Task<bool> AuthOTPAsync(PSO2LoginToken loginToken, SecureString otp, CancellationToken cancellationToken)
        {
            if (loginToken == null)
            {
                throw new ArgumentNullException(nameof(loginToken));
            }
            if (otp == null)
            {
                throw new ArgumentNullException(nameof(otp));
            }
            else if (otp.Length == 0)
            {
                throw new ArgumentException(null, nameof(otp));
            }
            var url = new Uri("https://auth.pso2.jp/auth/v1/otpAuth");
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                SetUA_PSO2_Launcher(request);
                request.Headers.Host = url.Host;
                request.Headers.ConnectionClose = true;
                using (var content = new PSO2OtpAuthContent(loginToken, otp))
                {
                    request.Content = content;

                    // Don't retry sending request. It may be considered as brute-force attack.
                    // Instead, let it throw naturally and then user can attempt another login by themselves (retry by themselves).

                    using (var response = await this.client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        using (var stream = response.Content.ReadAsStream(cancellationToken))
                        using (var doc = JsonDocument.Parse(stream))
                        {
                            var root = doc.RootElement;
                            if (root.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Number)
                            {
                                var code = result.GetInt32();
                                if (code == 0)
                                {
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                throw new UnexpectedDataFormatException();
                            }
                        }
                    }
                }
            }
        }
#nullable restore

        public async Task<Stream> DownloadWellbiaUninstaller(CancellationToken cancellationToken)
        {
            var url = new Uri(Uri_WellbiaUninstaller);
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Host = url.Host;
                request.Headers.Add("User-Agent", UA_WellbiaSite);
                // request.Headers.UserAgent.Add(new ProductInfoHeaderValue(UA_pso2launcher));
                request.Headers.Accept.ParseAdd("*/*");
                request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.5");
                using (var response = await this.client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    var contentLen = response.Content.Headers.ContentLength;
                    if (contentLen.HasValue && contentLen.Value < (1024 * 1024 * 5))
                    {
                        var buffer = new byte[contentLen.Value];
                        int totalread = 0;
                        using (var repContent = await response.Content.ReadAsStreamAsync(cancellationToken))
                        {
                            int read = 0;
                            do
                            {
                                read = await repContent.ReadAsync(buffer, totalread, buffer.Length - totalread);
                                totalread += read;
                            }
                            while (read != 0 && totalread < buffer.Length);
                        }
                        return new MemoryStream(buffer, 0, totalread, false, true) { Position = 0 };
                    }
                    else
                    {
                        var filename = Path.GetFullPath(string.Concat("xuninstaller.".AsSpan(), DateTimeOffset.UtcNow.ToFileTime().ToString(System.Globalization.NumberFormatInfo.InvariantInfo).AsSpan(), Path.GetExtension(Uri_WellbiaUninstaller.AsSpan())), RuntimeValues.RootDirectory);
                        var local_fs = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 0, FileOptions.DeleteOnClose | FileOptions.Asynchronous);
                        using (var repContent = await response.Content.ReadAsStreamAsync(cancellationToken))
                        {
                            await repContent.CopyToAsync(local_fs, 4096, cancellationToken);
                            await local_fs.FlushAsync();
                        }
                        local_fs.Position = 0;
                        return local_fs;
                    }
                }
            }
        }

        public async Task<PatchRootInfo> GetPatchRootInfoAsync(CancellationToken cancellationToken)
        {
            // Why the official launcher request twice over the same thing within the same time frame..
            // Don't use 443, server doesn't listen that port.
            var url = new Uri("http://patch01.pso2gs.net/patch_prod/patches/management_beta.txt");

            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Host = url.Host;
                SetUA_pso2launcher(request);
                using (var response = await this.client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    var repContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    return new PatchRootInfo(repContent);
                }
            }
        }

        public Task<PSO2Version> GetPatchVersionAsync(CancellationToken cancellationToken) => this.GetPatchVersionAsync(null, cancellationToken);

#nullable enable
        public async Task<PSO2Version> GetPatchVersionAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
        {
            // Why the official launcher request twice over the same thing within the same time frame..
            // Don't use 443, server doesn't listen that port.

            PatchRootInfo patchRootInfo;
            if (rootInfo == null)
            {
                patchRootInfo = await this.GetPatchRootInfoAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                patchRootInfo = rootInfo;
            }
            Exception? netEx = null;
            string? str_PatchURL;
            if (patchRootInfo.TryGetPatchURL(out str_PatchURL))
            {
                try
                {
                    return await InnerGetPatchVersionAsync(str_PatchURL, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    netEx = ex;
                }
            }
            if (netEx != null)
            {
                if (patchRootInfo.TryGetBackupPatchURL(out str_PatchURL))
                {
                    try
                    {
                        return await InnerGetPatchVersionAsync(str_PatchURL, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        throw new AggregateException(netEx, ex);
                    }
                }
                else
                {
                    throw netEx;
                }
            }

            throw new UnexpectedDataFormatException();
        }

        public Task<PatchListMemory> GetPatchListAllAsync(CancellationToken cancellationToken)
            => this.GetPatchListAllAsync(null, cancellationToken);

        public Task<PatchListMemory> GetPatchListClassicAsync(CancellationToken cancellationToken)
            => this.GetPatchListClassicAsync(null, cancellationToken);

        public Task<PatchListMemory> GetPatchListAlwaysAsync(CancellationToken cancellationToken)
            => this.GetPatchListAlwaysAsync(null, cancellationToken);

        public Task<PatchListMemory> GetPatchListNGSPrologueAsync(CancellationToken cancellationToken)
            => this.GetPatchListNGSPrologueAsync(null, cancellationToken);

        public Task<PatchListMemory> GetPatchListNGSFullAsync(CancellationToken cancellationToken)
            => this.GetPatchListNGSFullAsync(null, cancellationToken);

        public Task<PatchListMemory> GetLauncherListAsync(CancellationToken cancellationToken)
            => this.GetLauncherListAsync(null, cancellationToken);

        public Task<PatchListMemory> GetPatchListAvatarAsync(CancellationToken cancellationToken)
            => this.GetPatchListAvatarAsync(null, cancellationToken);

        /// <remarks>Read more from <seealso cref="GetPatchListRegionsAsync(PatchRootInfo?, CancellationToken)"/></remarks>
        public Task<PatchListMemory> GetPatchListRegionsAsync(CancellationToken cancellationToken)
            => this.GetPatchListRegionsAsync(null, cancellationToken);

        public async Task<PatchListMemory> GetPatchListAllAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
        {
            // var t_all = InnerGetPatchListAsync(rootInfo, "patchlist_all.txt", null, cancellationToken);
            if (rootInfo == null)
            {
                rootInfo = await this.GetPatchRootInfoAsync(cancellationToken);
            }
            var t_classic = this.GetPatchListClassicAsync(rootInfo, cancellationToken).ConfigureAwait(false);
            var t_ngs = this.InnerGetPatchListAsync(rootInfo, "patchlist_reboot.txt", true, cancellationToken).ConfigureAwait(false);

            var ngs = await t_ngs;
            var therest = await t_classic;
            var dictionary = new Dictionary<string, PatchListItem>(ngs.Count + therest.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var item in ngs) dictionary.Add(item.GetFilenameWithoutAffix(), item);
            foreach (var item in therest) dictionary.TryAdd(item.GetFilenameWithoutAffix(), item);
            
            return new PatchListMemory(rootInfo, null, dictionary);
        }

        /// <remarks>I may be wrong. This seems to be a random things SEGA put in-time to pre-download files for any coming major updates.</remarks>
        public Task<PatchListMemory> GetPatchListRegionsAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
        {
            /* Log:
             * - 18th May 2022: patchlist_region1st.txt -> composed both win32 and win32reboot datas. Purpose: unknown but seems to be aelio. Note: ?? June 2022 is Kvaris (third region, aka Reboot Tundra) update.
             */

            // Currently there's only one region so we will just forward the function calls.
            return this.InnerGetPatchListAsync(rootInfo, "patchlist_region1st.txt", true, cancellationToken);
        }

        public Task<PatchListMemory> GetPatchListAlwaysAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
            => this.InnerGetPatchListAsync(rootInfo, "patchlist_always.txt", null, cancellationToken);

        public Task<PatchListMemory> GetPatchListClassicAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
            => this.InnerGetPatchListAsync(rootInfo, "patchlist_classic.txt", false, cancellationToken);

        public Task<PatchListMemory> GetPatchListNGSPrologueAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
            => this.InnerGetPatchListAsync(rootInfo, "patchlist_prologue.txt", true, cancellationToken);

        public Task<PatchListMemory> GetPatchListNGSFullAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
            => this.InnerGetPatchListAsync(rootInfo, "patchlist_reboot.txt", true, cancellationToken);

    public Task<PatchListMemory> GetLauncherListAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
            => this.InnerGetPatchListAsync(rootInfo, "launcherlist.txt", null, cancellationToken);

        public Task<PatchListMemory> GetPatchListAvatarAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
            => this.InnerGetPatchListAsync(rootInfo, "patchlist_avatar.txt", true, cancellationToken);
#nullable restore
        #endregion

        #region | Advanced public APIs |
        // Need to be able to open stream (to download or handle resources from SEGA's server directly)

        private static bool IsTweakerDeletedFiles(PatchListItem item)
            => (Array.IndexOf<int>(_hardcodedWhichAreUsuallyDeletedOnes, PathStringComparer.Default.GetHashCode(item.GetSpanFilenameWithoutAffix())) != -1);

        public async Task<HttpResponseMessage> OpenForDownloadAsync(PatchListItem file, CancellationToken cancellationToken)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            else if (file.RemoteFilename.IsEmpty)
            {
                throw new ArgumentException(nameof(file));
            }
            else if (file.Origin == null)
            {
                throw new InvalidOperationException(nameof(file));
            }

            if (this.dataCache != null && IsTweakerDeletedFiles(file))
            {
                var versionRoot = await this.GetPatchVersionAsync(file.Origin.RootInfo, cancellationToken).ConfigureAwait(false);
                var stream = await this.dataCache.Fetch(string.Create(file.GetSpanFilenameWithoutAffix().Length, file, (c, arg) =>
                {
                    arg.GetSpanFilenameWithoutAffix().CopyTo(c);
                    for (int i = 0; i < c.Length; i++)
                    {
                        if (c[i] == Path.DirectorySeparatorChar || c[i] == Path.AltDirectorySeparatorChar)
                        {
                            c[i] = '_';
                        }
                    }
                }), this.InnerGetDownload_Create, InnerGetDownload_Verify, new ValueTuple<PatchRootInfo, bool?, Uri, PSO2Version>(file.Origin.RootInfo, file.IsRebootData, file.GetDownloadUrl(), versionRoot), cancellationToken).ConfigureAwait(false);
                var rep = new HttpResponseMessage(HttpStatusCode.OK);
                var kontent = new StreamContent(stream);
                kontent.Headers.ContentLength = stream.Length;
                rep.Content = kontent;
                return rep;
            }
            else
            {
                return await this.OpenForDownloadAsync(file.GetDownloadUrl(false), cancellationToken).ConfigureAwait(false);
            }
        }

        // Manual URL
        private async Task<HttpResponseMessage> OpenForDownloadAsync(Uri filename, CancellationToken cancellationToken)
        {
            if (!filename.IsAbsoluteUri)
            {
                throw new ArgumentException(null, nameof(filename));
            }

            HttpResponseMessage? response = null;
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, filename))
                {
                    SetUA_AQUA_HTTP(request);
                    request.Headers.Host = filename.Host;
                    response = await this.client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    return response;
                }
            }
            catch
            {
                response?.Dispose();
                throw;
            }
        }

        // Support deferred here. Why? Because this is open source. So there maybe someone who want deferred or enumerating kind of reading.

        #endregion

        #region | Private or inner methods |

#nullable enable
        private async Task<PatchListMemory> InnerGetPatchListAsync(PatchRootInfo? rootInfo, string filelistFilename, bool? isReboot, CancellationToken cancellationToken)
        {
            PatchRootInfo patchRootInfo;
            if (rootInfo == null)
            {
                patchRootInfo = await this.GetPatchRootInfoAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                patchRootInfo = rootInfo;
            }
            Exception? netEx = null;
            string? str_PatchURL;
            if (patchRootInfo.TryGetPatchURL(out str_PatchURL))
            {
                try
                {
                    return await InnerGetPatchListAsync2(patchRootInfo, str_PatchURL, isReboot, filelistFilename, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    netEx = ex;
                }
            }
            if (netEx != null)
            {
                if (patchRootInfo.TryGetBackupPatchURL(out str_PatchURL))
                {
                    try
                    {
                        return await InnerGetPatchListAsync2(patchRootInfo, str_PatchURL, isReboot, filelistFilename, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        throw new AggregateException(netEx, ex);
                    }
                }
                else
                {
                    throw netEx;
                }
            }

            throw new UnexpectedDataFormatException();
        }

        private static async Task<bool> InnerGetDownload_Verify(string entryName, JsonDocument header, Stream cacheContent, ValueTuple<PatchRootInfo, bool?, Uri, PSO2Version> arg, CancellationToken cancellationToken)
        {
            if (header.RootElement.TryGetProperty("source", out var prop_src) && prop_src.ValueKind == JsonValueKind.String
                && header.RootElement.TryGetProperty("version", out var prop_ver) && prop_ver.ValueKind == JsonValueKind.String && PSO2Version.TryParse(prop_ver.GetString() ?? string.Empty, out var localVer)
                && header.RootElement.TryGetProperty("sha1", out var prop_sha1) && prop_sha1.ValueKind == JsonValueKind.String)
            {
                var src = prop_src.GetString();
                var sha1 = prop_sha1.GetString();
                if (!string.IsNullOrWhiteSpace(src) && string.Equals(src, arg.Item3.AbsoluteUri, StringComparison.Ordinal) && arg.Item4.Equals(localVer))
                {
                    using (var shaEngi = IncrementalHash.CreateHash(HashAlgorithmName.SHA1))
                    {
                        var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(1024 * 32);
                        try
                        {
                            var readcount = await cacheContent.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                            while (!cancellationToken.IsCancellationRequested && readcount != 0)
                            {
                                shaEngi.AppendData(buffer, 0, readcount);
                                readcount = await cacheContent.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                            }

                            ReadOnlyMemory<byte> hash;
                            Memory<byte> therest;
                            if (shaEngi.TryGetCurrentHash(buffer, out var hashsize))
                            {
                                hash = new ReadOnlyMemory<byte>(buffer, 0, hashsize);
                                therest = new Memory<byte>(buffer, hashsize, buffer.Length - hashsize);
                            }
                            else
                            {
                                hash = shaEngi.GetCurrentHash();
                                therest = new Memory<byte>(buffer);
                            }
                            if (HashHelper.TryWriteHashToHexString(MemoryMarshal.Cast<byte, char>(therest.Span), hash.Span, out var writtenBytes))
                            {
                                if (MemoryExtensions.Equals(MemoryMarshal.Cast<byte, char>(therest.Slice(0, writtenBytes).Span), sha1, StringComparison.OrdinalIgnoreCase))
                                {
                                    return true;
                                }
                            }
                            else if (string.Equals(Convert.ToHexString(hash.Span), sha1, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                        finally
                        {
                            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                }
            }
            return false;
        }

        // This include too many insanity within just to avoid allocations.
        private async Task<bool> InnerGetDownload_Create(string entryName, Utf8JsonWriter headerWriter, ValueTuple<PatchRootInfo, bool?, Uri, PSO2Version> arg, Stream entryStream, CancellationToken cancellationToken)
        {
            bool result;
            using (var shaEngi = IncrementalHash.CreateHash(HashAlgorithmName.SHA1))
            using (var request = new HttpRequestMessage(HttpMethod.Get, arg.Item3))
            {
                request.Headers.Host = arg.Item3.Host;
                SetUA_AQUA_HTTP(request);
                using (var response = await this.client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = response.Content.ReadAsStream(cancellationToken))
                    {
                        // Allocate once and use it for all ops
                        var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(1024 * 32);
                        try
                        {
                            int read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                            result = (read != 0);
                            if (result)
                            {
                                while (read != 0)
                                {
                                    var readBuffer = buffer.AsMemory(0, read);
                                    
                                    var t_write = entryStream.WriteAsync(readBuffer, cancellationToken).ConfigureAwait(false);
                                    shaEngi.AppendData(readBuffer.Span);
                                    await t_write;

                                    read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                                }
                                entryStream.Flush();

                                headerWriter.WriteString("source", arg.Item3.AbsoluteUri);
                                headerWriter.WriteString("version", arg.Item4.ToString());

                                // Try fetch the hash into the existing buffer.
                                if (shaEngi.TryGetCurrentHash(buffer, out var hashSize))
                                {
                                    // Should always success, tbh

                                    // Slice and use the first slice to store raw hash (in bytes)
                                    var span_hash = buffer.AsMemory(0, hashSize);

                                    // Slice again and use the second slice to the hex characters of the hash
                                    var span_hex = buffer.AsMemory(hashSize);

                                    // Take the raw hash from the first slice and encode it into hex string and store it to the second slice.
                                    if (HashHelper.TryWriteHashToHexString(MemoryMarshal.Cast<byte, char>(span_hex.Span), span_hash.Span, out var byteCount))
                                    {
                                        headerWriter.WriteString("sha1", MemoryMarshal.Cast<byte, char>(span_hex.Slice(0, byteCount).Span));
                                    }
                                    else
                                    {
                                        // If we can't reuse the buffer, fallback to allocation.
                                        headerWriter.WriteString("sha1", Convert.ToHexString(shaEngi.GetCurrentHash()));
                                    }
                                }
                                else
                                {
                                    // If we can't reuse the buffer, fallback to allocation.
                                    headerWriter.WriteString("sha1", Convert.ToHexString(shaEngi.GetCurrentHash()));
                                }
                            }
                        }
                        finally
                        {
                            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                }
            }
            return result;
        }

        private async Task<PatchListMemory> InnerGetPatchListAsync2(PatchRootInfo rootInfo, string patchBaseUrl, bool? isReboot, string filelistFilename, CancellationToken cancellationToken)
        {
            var baseUri = new Uri(patchBaseUrl);
            var filelistUrl = new Uri(baseUri, filelistFilename);
            static async Task<PatchListMemory> GetFromRemote(HttpClient client, PatchRootInfo rootInfo, Uri filelistUrl, bool? isReboot, string filelistFilename, CancellationToken cancellationToken)
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, filelistUrl))
                {
                    request.Headers.Host = filelistUrl.Host;
                    SetUA_AQUA_HTTP(request);
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        using (var contentstream = response.Content.ReadAsStream(cancellationToken))
                        using (var tr = new StreamReader(contentstream))
                        using (var parser = new PatchListDeferred(rootInfo, isReboot, tr, false))
                        {
                            return parser.ToMemory();
                        }
                    }
                }
            }

            if (this.dataCache != null)
            {
                var versionRoot = await this.GetPatchVersionAsync(rootInfo, cancellationToken).ConfigureAwait(false);
                using (var stream = await this.dataCache.Fetch(filelistFilename, this.InnerGetDownload_Create, InnerGetDownload_Verify, new ValueTuple<PatchRootInfo, bool?, Uri, PSO2Version>(rootInfo, isReboot, filelistUrl, versionRoot), cancellationToken).ConfigureAwait(false))
                {
                    if (stream == null)
                    {
                        return await GetFromRemote(this.client, rootInfo, filelistUrl, isReboot, filelistFilename, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        using (var tr = new StreamReader(stream))
                        using (var parser = new PatchListDeferred(rootInfo, isReboot, tr, false))
                        {
                            return parser.ToMemory();
                        }
                    }
                }
                
            }
            else
            {
                return await GetFromRemote(this.client, rootInfo, filelistUrl, isReboot, filelistFilename, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<bool> InnerGetPatchVersionAsyncVerification(string entryName, JsonDocument header, Stream cacheContent, Uri arg, CancellationToken cancellationToken)
        {
            if (header.RootElement.TryGetProperty("source", out var prop_src) && prop_src.ValueKind == JsonValueKind.String
                && header.RootElement.TryGetProperty("timestamp", out var prop_timestamp) && prop_timestamp.ValueKind == JsonValueKind.Number
                && header.RootElement.TryGetProperty("sha1", out var prop_sha1) && prop_sha1.ValueKind == JsonValueKind.String)
            {
                var src = prop_src.GetString();
                var sha1 = prop_sha1.GetString();
                var offset = DateTimeOffset.FromUnixTimeSeconds(prop_timestamp.GetInt64());
                if (!string.IsNullOrWhiteSpace(src) && string.Equals(src, arg.AbsoluteUri, StringComparison.Ordinal) && ((DateTimeOffset.UtcNow - offset) < TimeSpan.FromMinutes(5)))
                {
                    using (var shaEngi = IncrementalHash.CreateHash(HashAlgorithmName.SHA1))
                    {
                        var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(1024 * 16);
                        try
                        {
                            var readcount = await cacheContent.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                            while (!cancellationToken.IsCancellationRequested && readcount != 0)
                            {
                                shaEngi.AppendData(buffer, 0, readcount);
                                readcount = await cacheContent.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                            }

                            ReadOnlyMemory<byte> hash;
                            Memory<byte> therest;
                            if (shaEngi.TryGetCurrentHash(buffer, out var hashsize))
                            {
                                hash = new ReadOnlyMemory<byte>(buffer, 0, hashsize);
                                therest = new Memory<byte>(buffer, hashsize, buffer.Length - hashsize);
                            }
                            else
                            {
                                hash = shaEngi.GetCurrentHash();
                                therest = new Memory<byte>(buffer);
                            }
                            if (HashHelper.TryWriteHashToHexString(MemoryMarshal.Cast<byte, char>(therest.Span), hash.Span, out var writtenBytes))
                            {
                                if (MemoryExtensions.Equals(MemoryMarshal.Cast<byte, char>(therest.Slice(0, writtenBytes).Span), sha1, StringComparison.OrdinalIgnoreCase))
                                {
                                    return true;
                                }
                            }
                            else if (string.Equals(Convert.ToHexString(hash.Span), sha1, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                        finally
                        {
                            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                }
            }
            return false;
        }

        private async Task<bool> InnerGetPatchVersionAsyncFetchCreation(string entryName, Utf8JsonWriter headerWriter, Uri arg, Stream entryStream, CancellationToken cancellationToken)
        {
            bool result;
            using (var shaEngi = IncrementalHash.CreateHash(HashAlgorithmName.SHA1))
            using (var request = new HttpRequestMessage(HttpMethod.Get, arg))
            {
                request.Headers.Host = arg.Host;
                SetUA_AQUA_HTTP(request);
                using (var response = await this.client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = response.Content.ReadAsStream(cancellationToken))
                    {
                        // Allocate once and use it for all ops
                        var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(1024 * 16);
                        try
                        {
                            int read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                            result = (read != 0);
                            if (result)
                            {
                                while (read != 0)
                                {
                                    var readBuffer = buffer.AsMemory(0, read);

                                    var t_write = entryStream.WriteAsync(readBuffer, cancellationToken).ConfigureAwait(false);
                                    shaEngi.AppendData(readBuffer.Span);
                                    await t_write;

                                    read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                                }
                                entryStream.Flush();

                                headerWriter.WriteString("source", arg.AbsoluteUri);
                                headerWriter.WriteNumber("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                                // Try fetch the hash into the existing buffer.
                                if (shaEngi.TryGetCurrentHash(buffer, out var hashSize))
                                {
                                    // Should always success, tbh

                                    // Slice and use the first slice to store raw hash (in bytes)
                                    var span_hash = buffer.AsMemory(0, hashSize);

                                    // Slice again and use the second slice to the hex characters of the hash
                                    var span_hex = buffer.AsMemory(hashSize);

                                    // Take the raw hash from the first slice and encode it into hex string and store it to the second slice.
                                    if (HashHelper.TryWriteHashToHexString(MemoryMarshal.Cast<byte, char>(span_hex.Span), span_hash.Span, out var byteCount))
                                    {
                                        headerWriter.WriteString("sha1", MemoryMarshal.Cast<byte, char>(span_hex.Slice(0, byteCount).Span));
                                    }
                                    else
                                    {
                                        // If we can't reuse the buffer, fallback to allocation.
                                        headerWriter.WriteString("sha1", Convert.ToHexString(shaEngi.GetCurrentHash()));
                                    }
                                }
                                else
                                {
                                    // If we can't reuse the buffer, fallback to allocation.
                                    headerWriter.WriteString("sha1", Convert.ToHexString(shaEngi.GetCurrentHash()));
                                }
                            }
                        }
                        finally
                        {
                            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                }
            }
            return result;
        }

        private async Task<PSO2Version> InnerGetPatchVersionAsync(string patchUrl, CancellationToken cancellationToken)
        {
            static PSO2Version ReadPSO2VersionFromStream(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    throw new UnexpectedDataFormatException();
                }

                if (PSO2Version.TryParse(raw, out var result))
                {
                    return result;
                }
                else
                {
                    throw new UnexpectedDataFormatException();
                }
            }

            var baseUri = new Uri(patchUrl);
            var requestUri = new Uri(baseUri, "version.ver");

            static async Task<PSO2Version> FulfillFromRemote(HttpClient client, Uri requestUri, CancellationToken cancellationToken)
            {
               
                using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
                {
                    request.Headers.Host = requestUri.Host;
                    SetUA_AQUA_HTTP(request);
                    // By default it complete with buffering with HttpCompletionOption.ResponseContentRead
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                        return ReadPSO2VersionFromStream(raw);
                    }
                }
            }

            if (this.dataCache != null)
            {
                using (var stream = await this.dataCache.Fetch("version.ver", this.InnerGetPatchVersionAsyncFetchCreation, InnerGetPatchVersionAsyncVerification, requestUri, cancellationToken).ConfigureAwait(false))
                {
                    if (stream == null)
                    {
                        return await FulfillFromRemote(this.client, requestUri, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        using (var tr = new StreamReader(stream))
                        {
                            return ReadPSO2VersionFromStream(tr.ReadLine());
                        }
                    }
                }
            }
            else
            {
                return await FulfillFromRemote(this.client, requestUri, cancellationToken).ConfigureAwait(false);
            }
        }
#nullable restore

        #endregion

        public void Dispose()
        {
            this.client.Dispose();
            this.dataCache?.Dispose();
        }
    }
}

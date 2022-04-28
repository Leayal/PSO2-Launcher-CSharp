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

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public class PSO2HttpClient : IDisposable
    {
        private readonly HttpClient client;
        private const string UA_AQUA_HTTP = "AQUA_HTTP";
        // private const string UA_PSO2Launcher = "PSO2Launcher";
        private const string UA_PSO2_Launcher = "PSO2 Launcher";
        private const string UA_pso2launcher = "pso2launcher";
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
            if (patchRootInfo.TryGetPatchURL(out var str_PatchURL))
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

        public async Task<PatchListMemory> GetPatchListAllAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
        {
            // var t_all = InnerGetPatchListAsync(rootInfo, "patchlist_all.txt", null, cancellationToken);
            var t_classic = this.GetPatchListClassicAsync(rootInfo, cancellationToken);
            var t_ngs = this.GetPatchListNGSFullAsync(rootInfo, cancellationToken);

            var ngs = await t_ngs;
            var therest = await t_classic;
            var dictionary = new Dictionary<string, PatchListItem>(therest.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var item in ngs) dictionary.Add(item.GetFilenameWithoutAffix(), item);

            foreach (var item in therest) dictionary.TryAdd(item.GetFilenameWithoutAffix(), item);

            return new PatchListMemory(rootInfo, null, dictionary);
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
#nullable restore
        #endregion

        #region | Advanced public APIs |
        // Need to be able to open stream (to download or handle resources from SEGA's server directly)

        public Task<HttpResponseMessage> OpenForDownloadAsync(in PatchListItem file, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(file.RemoteFilename))
            {
                throw new ArgumentNullException(nameof(file));
            }
            else if (file.Origin == null)
            {
                throw new InvalidOperationException(nameof(file));
            }

            return this.OpenForDownloadAsync(file.GetDownloadUrl(false), cancellationToken);
        }

        // Manual URL
        public async Task<HttpResponseMessage> OpenForDownloadAsync(Uri filename, CancellationToken cancellationToken)
        {
            if (!filename.IsAbsoluteUri)
            {
                throw new ArgumentException(null, nameof(filename));
            }

            HttpResponseMessage response = null;
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
                patchRootInfo = await this.GetPatchRootInfoAsync(cancellationToken);
            }
            else
            {
                patchRootInfo = rootInfo;
            }
            Exception? netEx = null;
            if (patchRootInfo.TryGetPatchURL(out var str_PatchURL))
            {
                try
                {
                    return await InnerGetPatchListAsync2(patchRootInfo, str_PatchURL, isReboot, filelistFilename, cancellationToken);
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
                        return await InnerGetPatchListAsync2(patchRootInfo, str_PatchURL, isReboot, filelistFilename, cancellationToken);
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

        private static async Task<bool> InnerGetPatchListAsyncVerification(string entryName, JsonDocument header, Stream cacheContent, ValueTuple<PatchRootInfo, bool?, Uri, PSO2Version> arg, CancellationToken cancellationToken)
        {
            if (header.RootElement.TryGetProperty("source", out var prop_src) && prop_src.ValueKind == JsonValueKind.String
                && header.RootElement.TryGetProperty("version", out var prop_ver) && prop_ver.ValueKind == JsonValueKind.String && PSO2Version.TryParse(prop_ver.GetString(), out var localVer)
                && header.RootElement.TryGetProperty("sha1", out var prop_sha1) && prop_sha1.ValueKind == JsonValueKind.String)
            {
                var src = prop_src.GetString();
                var sha1 = prop_sha1.GetString();
                if (!string.IsNullOrWhiteSpace(src) && string.Equals(src, arg.Item3.AbsoluteUri, StringComparison.Ordinal) && arg.Item4.Equals(localVer))
                {
                    var contentSha1 = await Helper.SHA1Hash.ComputeHashFromFileAsync(cacheContent, cancellationToken).ConfigureAwait(false);
                    if (string.Equals(contentSha1, sha1, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task<bool> InnerGetPatchListAsyncFetchCreation(string entryName, Utf8JsonWriter headerWriter, ValueTuple<PatchRootInfo, bool?, Uri, PSO2Version> arg, Stream entryStream, CancellationToken cancellationToken)
        {
            bool result;
            using (var request = new HttpRequestMessage(HttpMethod.Get, arg.Item3))
            {
                request.Headers.Host = arg.Item3.Host;
                SetUA_AQUA_HTTP(request);
                using (var response = await this.client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = response.Content.ReadAsStream(cancellationToken))
                    {
                        await stream.CopyToAsync(entryStream, 4096, cancellationToken).ConfigureAwait(false);
                        entryStream.Flush();
                        result = (entryStream.Length != 0);
                    }
                }
            }
            using (var shaEngi = SHA1.Create())
            {
                if (result)
                {
                    headerWriter.WriteString("source", arg.Item3.AbsoluteUri);
                    headerWriter.WriteString("version", arg.Item4.ToString());
                    entryStream.Position = 0;
                    headerWriter.WriteString("sha1", Convert.ToHexString(await shaEngi.ComputeHashAsync(entryStream, cancellationToken)));
                }
            }
            return result;
        }

        private async Task<PatchListMemory> InnerGetPatchListAsync2(PatchRootInfo rootInfo, string patchBaseUrl, bool? isReboot, string filelistFilename, CancellationToken cancellationToken)
        {
            var baseUri = new Uri(patchBaseUrl);
            var filelistUrl = new Uri(baseUri, filelistFilename);

            if (this.dataCache != null)
            {
                var versionRoot = await this.GetPatchVersionAsync(rootInfo, cancellationToken).ConfigureAwait(false);
                using (var stream = await this.dataCache.Fetch(filelistFilename, this.InnerGetPatchListAsyncFetchCreation, InnerGetPatchListAsyncVerification, new ValueTuple<PatchRootInfo, bool?, Uri, PSO2Version>(rootInfo, isReboot, filelistUrl, versionRoot), cancellationToken).ConfigureAwait(false))
                {
                    if (stream == null)
                    {
                        using (var request = new HttpRequestMessage(HttpMethod.Get, filelistUrl))
                        {
                            request.Headers.Host = baseUri.Host;
                            SetUA_AQUA_HTTP(request);
                            using (var response = await this.client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
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
                using (var request = new HttpRequestMessage(HttpMethod.Get, filelistUrl))
                {
                    request.Headers.Host = baseUri.Host;
                    SetUA_AQUA_HTTP(request);
                    using (var response = await this.client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        using (var stream = response.Content.ReadAsStream(cancellationToken))
                        using (var tr = new StreamReader(stream))
                        using (var parser = new PatchListDeferred(rootInfo, isReboot, tr, false))
                        {
                            return parser.ToMemory();
                        }
                    }
                }
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
                    var contentSha1 = await Helper.SHA1Hash.ComputeHashFromFileAsync(cacheContent, cancellationToken).ConfigureAwait(false);
                    if (string.Equals(contentSha1, sha1, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task<bool> InnerGetPatchVersionAsyncFetchCreation(string entryName, Utf8JsonWriter headerWriter, Uri arg, Stream entryStream, CancellationToken cancellationToken)
        {
            bool result;
            using (var request = new HttpRequestMessage(HttpMethod.Get, arg))
            {
                request.Headers.Host = arg.Host;
                SetUA_AQUA_HTTP(request);
                using (var response = await this.client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = response.Content.ReadAsStream(cancellationToken))
                    {
                        await stream.CopyToAsync(entryStream, 4096, cancellationToken).ConfigureAwait(false);
                        entryStream.Flush();
                        result = (entryStream.Length != 0);
                    }
                }
            }
            using (var shaEngi = SHA1.Create())
            {
                if (result)
                {
                    headerWriter.WriteString("source", arg.AbsoluteUri);
                    headerWriter.WriteNumber("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    entryStream.Position = 0;
                    headerWriter.WriteString("sha1", Convert.ToHexString(await shaEngi.ComputeHashAsync(entryStream, cancellationToken)));
                }
            }
            return result;
        }

        private async Task<PSO2Version> InnerGetPatchVersionAsync(string patchUrl, CancellationToken cancellationToken)
        {
            var baseUri = new Uri(patchUrl);
            var requestUri = new Uri(baseUri, "version.ver");

            if (this.dataCache != null)
            {
                using (var stream = await this.dataCache.Fetch("version.ver", this.InnerGetPatchVersionAsyncFetchCreation, InnerGetPatchVersionAsyncVerification, requestUri, cancellationToken).ConfigureAwait(false))
                {
                    if (stream == null)
                    {
                        using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
                        {
                            request.Headers.Host = baseUri.Host;
                            SetUA_AQUA_HTTP(request);
                            // By default it complete with buffering with HttpCompletionOption.ResponseContentRead
                            using (var response = await this.client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false))
                            {
                                response.EnsureSuccessStatusCode();
                                var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                                if (string.IsNullOrWhiteSpace(raw))
                                {
                                    throw new UnexpectedDataFormatException();
                                }

                                if (PSO2Version.TrySafeParse(in raw, out var result))
                                {
                                    return result;
                                }
                                else
                                {
                                    throw new UnexpectedDataFormatException();
                                }
                            }
                        }
                    }
                    else
                    {
                        using (var tr = new StreamReader(stream))
                        {
                            var raw = tr.ReadLine();
                            if (string.IsNullOrWhiteSpace(raw))
                            {
                                throw new UnexpectedDataFormatException();
                            }

                            if (PSO2Version.TrySafeParse(in raw, out var result))
                            {
                                return result;
                            }
                            else
                            {
                                throw new UnexpectedDataFormatException();
                            }
                        }
                    }
                }
            }
            else
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
                {
                    request.Headers.Host = baseUri.Host;
                    SetUA_AQUA_HTTP(request);
                    // By default it complete with buffering with HttpCompletionOption.ResponseContentRead
                    using (var response = await this.client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                        if (string.IsNullOrWhiteSpace(raw))
                        {
                            throw new UnexpectedDataFormatException();
                        }

                        if (PSO2Version.TrySafeParse(in raw, out var result))
                        {
                            return result;
                        }
                        else
                        {
                            throw new UnexpectedDataFormatException();
                        }
                    }
                }
            }
        }
#nullable restore

        #endregion

        // Not sure we're gonna use this?
        public void Dispose()
        {
            this.client.Dispose();
            this.dataCache?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

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

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public class PSO2HttpClient : IDisposable
    {
        private readonly HttpClient client;
        private const string UA_AQUA_HTTP = "AQUA_HTTP";
        private const string UA_PSO2Launcher = "PSO2Launcher";
        private const string UA_PSO2_Launcher = "PSO2 Launcher";
        private const string UA_pso2launcher = "pso2launcher";

        // Need to add snail mode (for when internet is extremely unreliable).
        // Do it later.


        public PSO2HttpClient()
        {
            this.client = new HttpClient(new SocketsHttpHandler()
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.All,
                ConnectTimeout = TimeSpan.FromSeconds(30),
                UseProxy = false,
                UseCookies = false,
                Credentials = null,
                DefaultProxyCredentials = null
            }, true);
        }

        #region | Convenient private methods |
        private void SetUA_AQUA_HTTP(HttpRequestMessage request)
        {
            // request.Headers.UserAgent.Add(new ProductInfoHeaderValue(UA_AQUA_HTTP));
            request.Headers.Add("User-Agent", UA_AQUA_HTTP);
            request.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
            request.Headers.Pragma.ParseAdd("no-cache");
        }

        private void SetUA_PSO2Launcher(HttpRequestMessage request)
        {
            request.Headers.Add("User-Agent", UA_PSO2Launcher);
            // request.Headers.UserAgent.Add(new ProductInfoHeaderValue(UA_PSO2Launcher));
            request.Headers.Accept.ParseAdd("*/*");
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.7");
        }

        private void SetUA_pso2launcher(HttpRequestMessage request)
        {
            request.Headers.Add("User-Agent", UA_pso2launcher);
            // request.Headers.UserAgent.Add(new ProductInfoHeaderValue(UA_pso2launcher));
            request.Headers.Accept.ParseAdd("*/*");
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.7");
        }

        private void SetUA_PSO2_Launcher(HttpRequestMessage request)
        {
            request.Headers.Add("User-Agent", UA_PSO2_Launcher);
            // request.Headers.UserAgent.Add(new ProductInfoHeaderValue(UA_PSO2Launcher));
        }
        #endregion

        #region | Simple public APIs |

        public async Task<PSO2LoginToken> LoginPSO2Async(SecureString username, SecureString password, CancellationToken cancellationToken)
        {
            var url = new Uri("https://auth.pso2.jp/auth/v1/auth");
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            SetUA_PSO2_Launcher(request);
            request.Headers.Host = url.Host;
            using (var content = new PSO2LoginContent(username, password))
            {
                request.Content = content;
                using (var response = await this.client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = response.Content.ReadAsStream())
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

        /// <summary>This class is sh**.</summary>
        class PSO2LoginContent : HttpContent
        {
            private static readonly byte[] b_1 = Encoding.UTF8.GetBytes("{\"id\":\"");
            private static readonly byte[] b_2 = Encoding.UTF8.GetBytes("\",\"password\":\"");
            private static readonly byte[] b_3 = Encoding.UTF8.GetBytes("\"}");
            private SecureString username;
            private SecureString password;

            private long? computedLength;

            public PSO2LoginContent(SecureString username, SecureString pw) : base()
            {
                this.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
                this.computedLength = null;
                this.username = username;
                this.password = pw;
            }

            // {"id":"","password":""}
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                stream.Write(b_1);
                this.username.EncodeTo(Encoding.UTF8, stream, out _, out var b_id);
                stream.Write(b_2);
                this.password.EncodeTo(Encoding.UTF8, stream, out _, out var b_pw);
                stream.Write(b_3);

                if (!computedLength.HasValue)
                {
                    computedLength = b_1.Length + b_2.Length + b_3.Length + b_id + b_pw;
                }

                return Task.CompletedTask;
            }

            protected override bool TryComputeLength(out long length)
            {
                if (!computedLength.HasValue)
                {
                    var bufferLen = Math.Max(this.username.Length, this.password.Length) * 2;
                    var buffer = new byte[bufferLen];
                    try
                    {
                        using (var mem = new MemoryStream(buffer, true))
                        {
                            mem.Position = 0;
                            this.username.EncodeTo(Encoding.UTF8, mem, out _, out var b_id);
                            mem.Position = 0;
                            this.password.EncodeTo(Encoding.UTF8, mem, out _, out var b_pw);
                            computedLength = b_1.Length + b_2.Length + b_3.Length + b_id + b_pw;
                        }
                    }
                    finally
                    {
                        Array.Fill(buffer, (byte)0);
                    }
                }
                length = computedLength.Value;
                return true;
            }

            protected override void Dispose(bool disposing)
            {
                this.username = null;
                this.password = null;
                base.Dispose(disposing);
            }
        }


        public async Task<PatchRootInfo> GetPatchRootInfoAsync(CancellationToken cancellationToken)
        {
            // Why the official launcher request twice over the same thing within the same time frame..
            // Don't use 443, server doesn't listen that port.
            var url = new Uri("http://patch01.pso2gs.net/patch_prod/patches/management_beta.txt");
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Host = url.Host;
            SetUA_pso2launcher(request);
            using (var response = await this.client.SendAsync(request, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                var repContent = await response.Content.ReadAsStringAsync(cancellationToken);

                return new PatchRootInfo(in repContent);
            }
        }

        public Task<PSO2Version> GetPatchVersionAsync(CancellationToken cancellationToken) => this.GetPatchVersionAsync(null, cancellationToken);

        public async Task<PSO2Version> GetPatchVersionAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
        {
            // Why the official launcher request twice over the same thing within the same time frame..
            // Don't use 443, server doesn't listen that port.

            PatchRootInfo patchRootInfo;
            if (rootInfo == null)
            {
                patchRootInfo = await this.GetPatchRootInfoAsync(cancellationToken);
            }
            else
            {
                patchRootInfo = rootInfo;
            }
            Exception netEx = null;
            string str_PatchURL; // Be clarify
            if (patchRootInfo.TryGetPatchURL(out str_PatchURL))
            {
                try
                {
                    return await InnerGetPatchVersionAsync(str_PatchURL, cancellationToken);
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
                        return await InnerGetPatchVersionAsync(str_PatchURL, cancellationToken);
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

        public async Task<PatchListMemory> GetPatchListAllAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
        {
            var t_all = InnerGetPatchListAsync(rootInfo, "patchlist_all.txt", null, cancellationToken);
            var t_ngs = GetPatchListNGSFullAsync(rootInfo, cancellationToken);

            var ngs = await t_ngs;
            var therest = await t_all;
            var dictionary = new Dictionary<string, PatchListItem>(therest.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var item in ngs) dictionary.Add(item.GetFilenameWithoutAffix(), item);

            foreach (var item in therest) dictionary.TryAdd(item.GetFilenameWithoutAffix(), item);

            return new PatchListMemory(rootInfo, null, dictionary);
        }

        public Task<PatchListMemory> GetPatchListAlwaysAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
            => InnerGetPatchListAsync(rootInfo, "patchlist_always.txt", null, cancellationToken);

        public Task<PatchListMemory> GetPatchListClassicAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
            => InnerGetPatchListAsync(rootInfo, "patchlist_classic.txt", false, cancellationToken);

        public Task<PatchListMemory> GetPatchListNGSPrologueAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
            => InnerGetPatchListAsync(rootInfo, "patchlist_prologue.txt", true, cancellationToken);

        public Task<PatchListMemory> GetPatchListNGSFullAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
            => InnerGetPatchListAsync(rootInfo, "patchlist_reboot.txt", true, cancellationToken);
        #endregion

        #region | Advanced public APIs |
        // Need to be able to open stream (to download or handle resources from SEGA's server directly)

        public async Task<HttpResponseMessage> OpenForDownloadAsync(PatchListItem file, CancellationToken cancellationToken)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            try
            {
                return await this.OpenForDownloadAsync(file.GetDownloadUrl(false), cancellationToken);
            }
            catch (Exception ex) when (ex is WebException || ex is HttpRequestException)
            {
                try
                {
                    return await this.OpenForDownloadAsync(file.GetDownloadUrl(true), cancellationToken);
                }
                catch (Exception ex2) when (ex2 is WebException || ex2 is HttpRequestException)
                {
#pragma warning disable CA2200 // Rethrow to preserve stack details
                    throw ex; // Should be the same failure in case it Net exception
#pragma warning restore CA2200 // Rethrow to preserve stack details
                }
            }
        }

        // Manual URL
        public async Task<HttpResponseMessage> OpenForDownloadAsync(Uri filename, CancellationToken cancellationToken)
        {
            if (!filename.IsAbsoluteUri)
            {
                throw new ArgumentException(nameof(filename));
            }
            var request = new HttpRequestMessage(HttpMethod.Get, filename);
            SetUA_AQUA_HTTP(request);
            request.Headers.Host = filename.Host;

            HttpResponseMessage response = null;
            try
            {
                response = await this.client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                return response;
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
            Exception netEx = null;
            string str_PatchURL; // Be clarify
            if (patchRootInfo.TryGetPatchURL(out str_PatchURL))
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

        private async Task<PatchListMemory> InnerGetPatchListAsync2(PatchRootInfo rootInfo, string patchBaseUrl, bool? isReboot, string filelistFilename, CancellationToken cancellationToken)
        {
            var baseUri = new Uri(patchBaseUrl);
            var filelistUrl = new Uri(baseUri, filelistFilename);

            var request = new HttpRequestMessage(HttpMethod.Get, filelistUrl);
            request.Headers.Host = baseUri.Host;
            SetUA_AQUA_HTTP(request);

            using (var response = await this.client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                using (var stream = response.Content.ReadAsStream()) // I thought there was only Async ops.
                using (var sr = new StreamReader(stream))
                using (var patchlistReader = new PatchListDeferred(rootInfo, isReboot, sr, false))
                {
                    return patchlistReader.ToMemory();
                }
            }
        }

        private async Task<PSO2Version> InnerGetPatchVersionAsync(string patchUrl, CancellationToken cancellationToken)
        {
            var baseUri = new Uri(patchUrl);
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, "version.ver"));
            request.Headers.Host = baseUri.Host;
            SetUA_AQUA_HTTP(request);

            // By default it complete with buffering with HttpCompletionOption.ResponseContentRead
            using (var response = await this.client.SendAsync(request, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                var raw = await response.Content.ReadAsStringAsync();
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

        #endregion

        // Not sure we're gonna use this?
        public void Dispose()
        {
            this.client.Dispose();
        }
    }
}

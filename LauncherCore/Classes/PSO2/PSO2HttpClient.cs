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

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    class PSO2HttpClient : IDisposable
    {
        private readonly HttpClient client;
        private const string UA_AQUA_HTTP = "AQUA_HTTP";
        private const string UA_PSO2Launcher = "PSO2Launcher";
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
        #endregion

        #region | Simple public APIs |
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

        public Task<PatchListMemory> GetPatchListNGSPrologueAsync(CancellationToken cancellationToken)
            => this.GetPatchListNGSPrologueAsync(null, cancellationToken);

        public Task<PatchListMemory> GetPatchListNGSFullAsync(CancellationToken cancellationToken)
            => this.GetPatchListNGSFullAsync(null, cancellationToken);

        public Task<PatchListMemory> GetPatchListAllAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
            => InnerGetPatchListAsync(rootInfo, "patchlist_all.txt", cancellationToken);

        public Task<PatchListMemory> GetPatchListAlwaysAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
            => InnerGetPatchListAsync(rootInfo, "patchlist_always.txt", cancellationToken);

        public Task<PatchListMemory> GetPatchListNGSPrologueAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
            => InnerGetPatchListAsync(rootInfo, "patchlist_prologue.txt", cancellationToken);

        public Task<PatchListMemory> GetPatchListNGSFullAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
            => InnerGetPatchListAsync(rootInfo, "patchlist_reboot.txt", cancellationToken);
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
                response = await this.client.SendAsync(request, cancellationToken);
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

        private async Task<PatchListMemory> InnerGetPatchListAsync(PatchRootInfo? rootInfo, string filelistFilename, CancellationToken cancellationToken)
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
                    return await InnerGetPatchListAsync2(patchRootInfo, str_PatchURL, filelistFilename, cancellationToken);
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
                        return await InnerGetPatchListAsync2(patchRootInfo, str_PatchURL, filelistFilename, cancellationToken);
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

        private async Task<PatchListMemory> InnerGetPatchListAsync2(PatchRootInfo rootInfo, string patchBaseUrl, string filelistFilename, CancellationToken cancellationToken)
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
                using (var patchlistReader = new PatchListDeferred(rootInfo, sr, false))
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

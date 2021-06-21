using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    class PSO2HttpClient : IDisposable
    {
        private readonly HttpClient client;
        private const string UA_AQUA_HTTP = "AQUA_HTTP";
        private const string UA_PSO2Launcher = "PSO2Launcher";
        private const string UA_pso2launcher = "pso2launcher";

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

        public Task<string> GetPatchVersionAsync(CancellationToken cancellationToken) => this.GetPatchVersionAsync(null, cancellationToken);

        public async Task<string> GetPatchVersionAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
        {
            // Why the official launcher request twice over the same thing within the same time frame..
            // Don't use 443, server doesn't listen that port.
            
            PatchRootInfo patchRootInfo = await this.GetPatchRootInfoAsync(cancellationToken);
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

        public Task<PatchListMemory> GetPatchListNGSPrologueAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
            => InnerGetPatchListAsync(rootInfo, "patchlist_prologue.txt", cancellationToken);

        public Task<PatchListMemory> GetPatchListNGSFullAsync(PatchRootInfo? rootInfo, CancellationToken cancellationToken)
            => InnerGetPatchListAsync(rootInfo, "patchlist_reboot.txt", cancellationToken);
        #endregion

        #region | Advanced public APIs |
        // Need to be able to open stream (to download or handle resources from SEGA's server directly)

        // Support deferred here. Why? Because this is open source. So there maybe someone who want deferred or enumerating kind of reading.

        #endregion

        #region | Private or inner methods |

        private async Task<PatchListMemory> InnerGetPatchListAsync(PatchRootInfo? rootInfo, string filelistFilename, CancellationToken cancellationToken)
        {
            PatchRootInfo patchRootInfo = await this.GetPatchRootInfoAsync(cancellationToken);
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
                    return await InnerGetPatchListAsync2(str_PatchURL, filelistFilename, cancellationToken);
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
                        return await InnerGetPatchListAsync2(str_PatchURL, filelistFilename, cancellationToken);
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

        private async Task<PatchListMemory> InnerGetPatchListAsync2(string patchBaseUrl, string filelistFilename, CancellationToken cancellationToken)
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
                using (var patchlistReader = new PatchListDeferred(sr, false))
                {
                    return patchlistReader.ToMemory();
                }
            }
        }

        private async Task<string> InnerGetPatchVersionAsync(string patchUrl, CancellationToken cancellationToken)
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
                return raw.Trim(); // For safety, trim it
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

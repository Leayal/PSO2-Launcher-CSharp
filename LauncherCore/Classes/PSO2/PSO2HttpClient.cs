using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

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

        private void Set_AQUA_HTTP(HttpRequestMessage request)
        {
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue(UA_AQUA_HTTP));
            request.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
            request.Headers.Pragma.ParseAdd("no-cache");
        }

        private void Set_PSO2Launcher(HttpRequestMessage request)
        {
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue(UA_PSO2Launcher));
            request.Headers.Accept.ParseAdd("*/*");
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.7");
        }

        private void Set_pso2launcher(HttpRequestMessage request)
        {
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue(UA_pso2launcher));
            request.Headers.Accept.ParseAdd("*/*");
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.7");
        }

        // Not sure we're gonna use this?
        public void Dispose()
        {
            this.client.Dispose();
        }
    }
}

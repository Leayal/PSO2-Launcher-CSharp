using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Updater
{
    class WebClientEx : WebClient
    {
        protected override WebResponse GetWebResponse(WebRequest request)
        {
            if (request is HttpWebRequest httpRequest)
            {
                if (httpRequest.AutomaticDecompression != DecompressionMethods.All)
                {
                    httpRequest.AutomaticDecompression = DecompressionMethods.All;
                }
            }
            return base.GetWebResponse(request);
        }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            if (request is HttpWebRequest httpRequest)
            {
                if (httpRequest.AutomaticDecompression != DecompressionMethods.All)
                {
                    httpRequest.AutomaticDecompression = DecompressionMethods.All;
                }
            }
            return base.GetWebResponse(request, result);
        }
    }
}

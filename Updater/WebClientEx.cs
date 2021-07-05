using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Updater
{
    class WebClientEx : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request is HttpWebRequest httpRequest)
            {
                if (httpRequest.AutomaticDecompression != DecompressionMethods.All)
                {
                    httpRequest.AutomaticDecompression = DecompressionMethods.All;
                }
            }
            return request;
        }
    }
}

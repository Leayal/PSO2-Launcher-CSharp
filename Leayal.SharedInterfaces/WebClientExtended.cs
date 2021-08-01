using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.SharedInterfaces
{
    public class WebClientExtended : WebClient
    {
        public WebClientExtended() : base()
        {
            this.Encoding = Encoding.UTF8;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request is HttpWebRequest httprequest)
            {
                if (string.Equals(httprequest.Method, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    httprequest.AutomaticDecompression = DecompressionMethods.All;
                }
            }
            return request;
        }
    }
}

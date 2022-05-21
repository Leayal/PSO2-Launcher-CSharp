using System;
using System.Net;
using System.Text;

namespace Leayal.SharedInterfaces
{

#pragma warning disable SYSLIB0014 // Type or member is obsolete
    [Obsolete("This class is not recommended to use. Please use System.Net.Http.HttpClient instead.", false)]
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
#pragma warning restore SYSLIB0014 // Type or member is obsolete
}

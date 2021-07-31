using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Updater
{
    static class HttpClientExtension
    {
        public static async Task DownloadFileTaskAsync(this HttpClient client, string url, string destination)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            using (var fs = File.Create(destination))
            {
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = response.Content.ReadAsStream())
                    {
                        var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(1024 * 32);
                        try
                        {
                            var readbyte = stream.Read(buffer, 0, buffer.Length);
                            while (readbyte > 0)
                            {
                                fs.Write(buffer, 0, readbyte);
                                readbyte = stream.Read(buffer, 0, buffer.Length);
                            }
                        }
                        finally
                        {
                            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                }
            }
        }
    }
}

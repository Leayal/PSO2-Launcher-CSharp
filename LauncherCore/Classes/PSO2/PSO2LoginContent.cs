using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    /// <summary>This class is sh**.</summary>
    sealed class PSO2LoginContent : HttpContent
    {
        private static readonly byte[] b_1 = Encoding.UTF8.GetBytes("{\"id\":\"");
        private static readonly byte[] b_2 = Encoding.UTF8.GetBytes("\",\"password\":\"");
        private static readonly byte[] b_3 = Encoding.UTF8.GetBytes("\"}");

        private readonly bool _keepSecureStringAlive;
        private SecureString? username, password;

        private long? computedLength;

        public PSO2LoginContent(SecureString username, SecureString pw, bool keepSecureStringAlive = true) : base()
        {
            this._keepSecureStringAlive = keepSecureStringAlive;
            this.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
            this.computedLength = null;
            this.username = username;
            this.password = pw;
        }

        // {"id":"","password":""}
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            if (this.username == null || this.password == null) throw new InvalidOperationException();
            stream.Write(b_1);
            this.username.EncodeTo(stream, out var b_id);
            stream.Write(b_2);
            this.password.EncodeTo(stream, out var b_pw);
            stream.Write(b_3);

            if (!computedLength.HasValue)
            {
                computedLength = b_1.Length + b_2.Length + b_3.Length + b_id + b_pw;
            }

            return Task.CompletedTask;
        }

        protected override bool TryComputeLength(out long length)
        {
            if (this.username == null || this.password == null) throw new InvalidOperationException();
            if (!computedLength.HasValue)
            {
                computedLength = Convert.ToInt64(b_1.Length + b_2.Length + b_3.Length + this.username.GetByteCount(Encoding.UTF8) + this.password.GetByteCount(Encoding.UTF8));
            }
            length = computedLength.Value;
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (!this._keepSecureStringAlive)
            {
                this.username?.Dispose();
                this.password?.Dispose();
            }
            this.username = null;
            this.password = null;
            base.Dispose(disposing);
        }
    }
}

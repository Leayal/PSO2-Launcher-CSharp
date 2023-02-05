using System;
using System.IO;
using System.Text.Json;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    class PSO2OtpAuthContent : HttpContent
    {
        private readonly PSO2LoginToken loginToken;
        private readonly SecureString otp;

        private long? computedLength;

        public PSO2OtpAuthContent(PSO2LoginToken loginInfo, SecureString otp) : base()
        {
            this.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
            this.computedLength = null;
            this.loginToken = loginInfo;
            this.otp = otp;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            this.computedLength = this.EncodeTo(stream);
            return Task.CompletedTask;
        }

        private long EncodeTo(Stream stream)
        {
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions() { Indented = false }))
            {
                writer.WriteStartObject();

                writer.WriteString("userId", loginToken.UserId);
                writer.WriteString("token", loginToken.Token);
                this.otp.Reveal((in ReadOnlySpan<char> buffer, Utf8JsonWriter w) =>
                {
                    if (buffer.Contains(' '))
                    {
                        var trimmed = buffer.Trim();
                        if (trimmed.Contains(' '))
                        {
                            var buffer2 = new Span<char>(new char[trimmed.Length]);
                            int index = 0;
                            for(int i = 0; i < trimmed.Length; i++)
                            {
                                if (trimmed[i] != ' ')
                                {
                                    buffer2[index++] = trimmed[i];
                                }
                            }
                            buffer2 = buffer2.Slice(0, index);
                            w.WriteString("otp", buffer2);
                            buffer2.Fill(char.MinValue);
                        }
                        else
                        {
                            w.WriteString("otp", trimmed);
                        }
                    }
                    else
                    {
                        w.WriteString("otp", buffer);
                    }
                }, writer);

                writer.WriteEndObject();
                writer.Flush();

                return writer.BytesCommitted + writer.BytesPending;
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            if (!this.computedLength.HasValue)
            {
                using (var memStream = new MemoryStream())
                {
                    this.computedLength = this.EncodeTo(memStream);

                    if (memStream.TryGetBuffer(out var segment))
                    {
                        new Span<byte>(segment.Array, segment.Offset, segment.Count).Fill(0);
                    }
                    else
                    {
                        memStream.Position = 0;
                        for (int i = 0; i < memStream.Length; i++)
                        {
                            memStream.WriteByte(0);
                        }
                    }
                }
            }
            length = this.computedLength.Value;
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            this.otp?.Dispose();
            base.Dispose(disposing);
        }
    }
}

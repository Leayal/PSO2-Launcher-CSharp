using System;
using System.Collections.Generic;
using System.Buffers;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace Leayal.PSO2Launcher.AdminProcess
{
    public class Response
    {
        private bool _isSuccess;
        public bool IsSuccess => this._isSuccess;
        private string _message;
        public string Message => this._message;
        private int _hresult;
        public int HResult => this._hresult;

        public Response() { }

        public Response(bool isSuccess, string message, int hresult) 
        {
            this._isSuccess = isSuccess;
            this._message = message;
            this._hresult = hresult;
        }

        public ReadOnlyMemory<byte> Encode()
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartObject();
                writer.WriteBoolean("success", this.IsSuccess);
                writer.WriteString("message", this.Message);
                writer.WriteNumber("hresult", this.HResult);
                writer.WritePropertyName("data");
                this.EncodeData(writer);
                writer.WriteEndObject();
                writer.Flush();
            }

            return buffer.WrittenMemory;
        }

        protected virtual void EncodeData(Utf8JsonWriter writer)
        {
            writer.WriteNullValue();
        }

        public bool Decode(ReadOnlyMemory<byte> raw)
        {
            using (var document = JsonDocument.Parse(raw))
            {
                var root = document.RootElement;
                if (root.TryGetProperty("success", out var prop_success) && prop_success.ValueKind == JsonValueKind.True)
                {
                    this._isSuccess = true;
                }
                else
                {
                    this._isSuccess = false;
                }
                if (root.TryGetProperty("message", out var prop_msg) && prop_msg.ValueKind == JsonValueKind.String)
                {
                    this._message = prop_msg.GetString();
                }
                else
                {
                    this._isSuccess = false;
                }
                if (root.TryGetProperty("hresult", out var prop_hresult) && prop_hresult.ValueKind == JsonValueKind.Number)
                {
                    this._hresult = prop_hresult.GetInt32();
                }
                else
                {
                    this._hresult = prop_hresult.GetInt32();
                }
                if (root.TryGetProperty("data", out var prop_data))
                {
                    this.DecodeData(in prop_data);
                }
            }
            return false;
        }

        protected virtual void DecodeData(in JsonElement element)
        {
        }
    }
}

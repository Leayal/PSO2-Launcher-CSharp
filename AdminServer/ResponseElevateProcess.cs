using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.AdminProcess
{
    public class ResponseElevateProcess : Response
    {
        private int? _exitCode;
        public int? ExitCode => this._exitCode;

        public ResponseElevateProcess() : base() 
        {
            this._exitCode = null;
        }

        public ResponseElevateProcess(bool success, string message, int hresult, int? exitCode) : base(success, message, hresult) 
        {
            this._exitCode = exitCode;
        }

        protected override void DecodeData(in JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object) return;
            if (element.TryGetProperty("exitcode", out var prop_exitcode) && prop_exitcode.ValueKind == JsonValueKind.Number)
            {
                this._exitCode = prop_exitcode.GetInt32();
            }
            else
            {
                this._exitCode = null;
            }
        }

        protected override void EncodeData(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            if (this._exitCode.HasValue)
            {
                writer.WriteNumber("exitcode", this._exitCode.Value);
            }
            else
            {
                writer.WriteNull("exitcode");
            }
            writer.WriteEndObject();
        }
    }
}

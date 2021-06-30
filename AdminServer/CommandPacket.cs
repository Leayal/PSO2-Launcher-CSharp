using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Buffers;

namespace Leayal.PSO2Launcher.AdminProcess
{
    public abstract class CommandPacket
    {
        private readonly string commandname;
        protected CommandPacket(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                throw new ArgumentNullException(nameof(commandName));
            }
            this.commandname = commandName;
        }

        public ReadOnlyMemory<byte> Encode()
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartObject();
                writer.WriteString("name", this.commandname);
                writer.WritePropertyName("data");
                this.WriteDataToJson(writer);
                writer.WriteEndObject();
                writer.Flush();
            }

            return buffer.WrittenMemory;
        }

        public bool Decode(ReadOnlyMemory<byte> raw)
        {
            using (var document = JsonDocument.Parse(raw))
            {
                var root = document.RootElement;
                if (root.TryGetProperty("name", out var prop_commandName) && prop_commandName.ValueKind == JsonValueKind.String)
                {
                    if (string.Equals(prop_commandName.GetString(), this.commandname, StringComparison.Ordinal))
                    {
                        if (root.TryGetProperty("data", out var prop_data))
                        {
                            this.DecodeData(in prop_data);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public abstract void WriteDataToJson(Utf8JsonWriter writer);

        public abstract void DecodeData(in JsonElement element);
    }
}

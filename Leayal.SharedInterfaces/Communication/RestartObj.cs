using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Leayal.SharedInterfaces.Communication
{
    public class RestartObj<T> where T : RestartDataObj<T>, new()
    {
        public readonly T DataObj;
        public readonly string ParentFilename;
        public readonly List<string> ParentParams;

        public RestartObj(T obj, string filename, List<string> processparams)
        {
            this.DataObj = obj;
            this.ParentFilename = filename;
            this.ParentParams = processparams;
        }

        public RestartObj(T obj, string filename) : this(obj, filename, null) { }

        public ReadOnlyMemory<byte> SerializeJson()
        {
            var result = new System.Buffers.ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(result))
            {
                writer.WriteStartObject();

                writer.WriteString("parentFilename", this.ParentFilename);

                if (this.ParentParams == null)
                {
                    writer.WriteNull("parentParams");
                }
                else
                {
                    writer.WriteStartArray("parentParams");
                    for (int i = 0; i < this.ParentParams.Count; i++)
                    {
                        writer.WriteStringValue(this.ParentParams[i]);
                    }
                    writer.WriteEndArray();
                }
                writer.WritePropertyName("data");
                this.DataObj.WriteJsonValueTo(writer);

                writer.WriteEndObject();

                writer.Flush();
            }

            return result.WrittenMemory;
        }

        public static RestartObj<T> DeserializeJson(ReadOnlyMemory<byte> data)
        {
            using (var doc = JsonDocument.Parse(data))
            {
                T dataObj = null;
                var rootElement = doc.RootElement;
                if (rootElement.TryGetProperty("data", out var prop_data))
                {
                    var parser = new T();
                    dataObj = parser.DeserializeJson(prop_data);
                }

                List<string> paramList = null;
                if (rootElement.TryGetProperty("parentParams", out var prop_params) && prop_params.ValueKind == JsonValueKind.Array)
                {
                    paramList = new List<string>();
                    using (var arrayWalker = prop_params.EnumerateArray())
                    {
                        while (arrayWalker.MoveNext())
                        {
                            var str = arrayWalker.Current.GetString();
                            if (!string.IsNullOrEmpty(str))
                            {
                                paramList.Add(str);
                            }
                        }
                    }
                }
                return new RestartObj<T>(dataObj, rootElement.GetProperty("parentFilename").GetString(), paramList);
            }
        }
    }

    public abstract class RestartDataObj<T>
    {
        public virtual ReadOnlyMemory<byte> SerializeJson()
        {
            var mem = new System.Buffers.ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(mem))
            {
                this.WriteJsonValueTo(writer);
                writer.Flush();
            }
            return mem.WrittenMemory;
        }

        public abstract void WriteJsonValueTo(Utf8JsonWriter writer);

        public abstract T DeserializeJson(JsonElement element);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Communication
{
    class RestartObj<T> where T : RestartDataObj
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

        public ReadOnlyMemory<byte> SerializeJson()
        {
            var result = new System.Buffers.ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(result))
            {
                writer.WriteStartObject();

                writer.WriteString("parentFilename", this.ParentFilename);

                writer.WriteStartArray("parentParams");
                for (int i = 0; i < this.ParentParams.Count; i++)
                {
                    writer.WriteStringValue(this.ParentParams[i]);
                }
                writer.WriteEndArray();
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
                    var type = typeof(T).GetType();
                    var method = type.GetMethod("DeserializeJson", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    if (method != null)
                    {
                        dataObj = method.Invoke(null, new object[] { prop_data }) as T;
                    }
                }

                var paramList = new List<string>();
                if (rootElement.TryGetProperty("parentParams", out var prop_params) && prop_params.ValueKind == JsonValueKind.Array)
                {
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

    public abstract class RestartDataObj
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
    }
}

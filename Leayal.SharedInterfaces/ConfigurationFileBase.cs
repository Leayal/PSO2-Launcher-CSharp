using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Leayal.SharedInterfaces
{
    public abstract class ConfigurationFileBase
    {
        protected readonly Dictionary<string, object> keyValuePairs;

        protected ConfigurationFileBase()
        {
            this.keyValuePairs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        protected void Set(string key, object value)
        {
            lock (this.keyValuePairs)
            {
                this.keyValuePairs[key] = value;
            }
        }

        protected bool TryGetRaw(string key, out JsonElement value)
        {
            object obj;
            bool result;
            lock (this.keyValuePairs)
            {
                result = this.keyValuePairs.TryGetValue(key, out obj);
            }
            if (obj is JsonElement element)
            {
                value = element;
            }
            else
            {
                value = default;
            }
            return result;
        }

        protected bool TryGet(string key, out object value)
        {
            bool result;
            lock (this.keyValuePairs)
            {
                result = this.keyValuePairs.TryGetValue(key, out value);
            }
            return result;
        }

        protected void SaveTo(Stream stream) => this.SaveTo(stream, Encoding.UTF8);

        protected void SaveTo(Stream stream, Encoding encoding)
        {
            if (!stream.CanWrite)
            {
                throw new ArgumentException(nameof(stream));
            }
            stream.Write(JsonSerializer.SerializeToUtf8Bytes(keyValuePairs));
        }

        protected void Load(Stream stream) => this.Load(stream, Encoding.UTF8);

        protected void Load(Stream stream, Encoding encoding)
        {
            if (!stream.CanRead)
            {
                throw new ArgumentException(nameof(stream));
            }
            using (var reader = new StreamReader(stream, encoding))
            {
                var str = reader.ReadToEnd();
                var dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(str);
                this.keyValuePairs.Clear();
                foreach (var item in dictionary)
                {
                    this.keyValuePairs.Add(item.Key, item.Value);
                }
            }
        }
    }
}

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
        protected readonly Dictionary<string, ValueWrap> keyValuePairs;

        protected ConfigurationFileBase()
        {
            this.keyValuePairs = new Dictionary<string, ValueWrap>(StringComparer.OrdinalIgnoreCase);
        }

        protected void Set(string key, string value)
        {   
            lock (this.keyValuePairs)
            {
                this.keyValuePairs[key] = new ValueWrap(value);
            }
        }

        protected void Set(string key, int value)
        {
            lock (this.keyValuePairs)
            {
                this.keyValuePairs[key] = new ValueWrap(value);
            }
        }

        protected void Set(string key, bool value)
        {
            lock (this.keyValuePairs)
            {
                this.keyValuePairs[key] = new ValueWrap(value);
            }
        }

        protected bool TryGetRaw(string key, out ValueWrap value)
        {
            bool result;
            lock (this.keyValuePairs)
            {
                result = this.keyValuePairs.TryGetValue(key, out value);
            }
            return result;
        }

        public class ValueWrap
        {
            public JsonValueKind ValueKind { get; }

            public object Value { get; }

            public ValueWrap(int value)
            {
                this.Value = value;
                this.ValueKind = JsonValueKind.Number;
            }

            public ValueWrap(string value)
            {
                this.Value = value;
                this.ValueKind = JsonValueKind.String;
            }

            public ValueWrap(bool value)
            {
                this.Value = value;
                if (value)
                {
                    this.ValueKind = JsonValueKind.True;
                }
                else
                {
                    this.ValueKind = JsonValueKind.False;
                }
            }
        }

        protected void SaveTo(Stream stream)
        {
            if (!stream.CanWrite)
            {
                throw new ArgumentException(nameof(stream));
            }
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions() { Indented = true }))
            {
                writer.WriteStartObject();
                foreach (var item in this.keyValuePairs)
                {
                    switch (item.Value.ValueKind)
                    {
                        case JsonValueKind.Null:
                            writer.WriteNull(item.Key);
                            break;
                        case JsonValueKind.String:
                            writer.WriteString(item.Key, (string)(item.Value.Value));
                            break;
                        case JsonValueKind.Number:
                            writer.WriteNumber(item.Key, (int)(item.Value.Value));
                            break;
                        case JsonValueKind.True:
                            writer.WriteBoolean(item.Key, true);
                            break;
                        case JsonValueKind.False:
                            writer.WriteBoolean(item.Key, false);
                            break;
                    }
                }
                writer.WriteEndObject();
            }
        }

        protected bool Load(Stream stream)
        {
            if (!stream.CanRead)
            {
                throw new ArgumentException(nameof(stream));
            }
            try
            {
                using (var jsonDocument = JsonDocument.Parse(stream))
                {
                    using (var walker = jsonDocument.RootElement.EnumerateObject())
                    {
                        this.keyValuePairs.Clear();
                        while (walker.MoveNext())
                        {
                            var element = walker.Current;
                            var value = element.Value;
                            switch (value.ValueKind)
                            {
                                case JsonValueKind.String:
                                    this.keyValuePairs.Add(element.Name, new ValueWrap(value.GetString()));
                                    break;
                                case JsonValueKind.Number:
                                    this.keyValuePairs.Add(element.Name, new ValueWrap(value.GetInt32()));
                                    break;
                                case JsonValueKind.True:
                                    this.keyValuePairs.Add(element.Name, new ValueWrap(true));
                                    break;
                                case JsonValueKind.False:
                                    this.keyValuePairs.Add(element.Name, new ValueWrap(false));
                                    break;
                            }
                        }
                    }
                }
                return true;
            }
            catch
            {
                // Corrupted config file
                return false;
            }
        }
    }
}

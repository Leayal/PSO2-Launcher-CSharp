using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.ObjectModel;

namespace Leayal.SharedInterfaces
{
    public abstract class ConfigurationFileBase
    {
        protected readonly Dictionary<string, ValueWrap> keyValuePairs;

        protected ConfigurationFileBase() : this(StringComparer.OrdinalIgnoreCase) { }

        protected ConfigurationFileBase(IEqualityComparer<string?> comparer)
        {
            this.keyValuePairs = new Dictionary<string, ValueWrap>(comparer);
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

        protected void SetNull(string key)
        {
            lock (this.keyValuePairs)
            {
                this.keyValuePairs[key] = new ValueWrap();
            }
        }

        /// <summary>Creates a save of all values which can be used to restore later with <seealso cref="LoadSavedValues(ConfigurationFileSave, bool)"/>.</summary>
        /// <returns>A save which can be restored with <seealso cref="LoadSavedValues(ConfigurationFileSave, bool)"/>.</returns>
        /// <remarks>This save can only be used with this instance.</remarks>
        public ConfigurationFileSave SaveCurrentValues()
        {
            ConfigurationFileSave result;
            lock (this.keyValuePairs)
            {
                result = new ConfigurationFileSave(this);
            }
            return result;
        }

        /// <summary>Restore saved values from a <seealso cref="ConfigurationFileSave"/> created by this instance.</summary>
        /// <param name="saved">The saved values to restore</param>
        /// <param name="clearBeforeRestore">A boolean determines whether to clear all existing values before restoring. Set false to keep any values which aren't in the saved values.</param>
        /// <exception cref="InvalidOperationException">Throws when trying to restore a save which was created by other instance.</exception>
        public void LoadSavedValues(ConfigurationFileSave saved, bool clearBeforeRestore = true)
        {
            if (!object.ReferenceEquals(saved.source, this)) throw new InvalidOperationException();
            lock (this.keyValuePairs)
            {
                if (clearBeforeRestore)
                {
                    this.keyValuePairs.Clear();
                }
                foreach (var item in saved.saved)
                {
                    this.keyValuePairs.Add(item.Key, item.Value);
                }
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

        public sealed class ConfigurationFileSave
        {
            internal readonly ConfigurationFileBase source;
            internal readonly Dictionary<string, ValueWrap> saved;

            internal ConfigurationFileSave(ConfigurationFileBase src)
            {
                this.source = src;
                this.saved = new Dictionary<string, ValueWrap>(src.keyValuePairs, src.keyValuePairs.Comparer);
            }
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

            public ValueWrap()
            {
                this.Value = null;
                this.ValueKind = JsonValueKind.Null;
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
                                case JsonValueKind.Null:
                                        this.keyValuePairs.Add(element.Name, new ValueWrap());
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Leayal.SharedInterfaces
{
    public abstract class ConfigurationFileBase
    {
        protected readonly Dictionary<string, ValueWrap> keyValuePairs;

        protected ConfigurationFileBase() : this(StringComparer.OrdinalIgnoreCase) { }

        protected ConfigurationFileBase(IEqualityComparer<string> comparer)
        {
            this.keyValuePairs = new Dictionary<string, ValueWrap>(comparer);
        }

        protected void Set(string key, string value)
        {   
            lock (this.keyValuePairs)
            {
                this.keyValuePairs[key] = ValueWrap.Create(value);
            }
        }

        protected void Set(string key, ReadOnlyMemory<char> value)
        {
            lock (this.keyValuePairs)
            {
                this.keyValuePairs[key] = ValueWrap.Create(value);
            }
        }

        protected void Set(string key, bool value)
        {
            lock (this.keyValuePairs)
            {
                this.keyValuePairs[key] = ValueWrap.Create(value);
            }
        }

        protected void Set<T>(string key, T value) where T : struct, IConvertible
        {
            lock (this.keyValuePairs)
            {
                switch (Type.GetTypeCode(value.GetType()))
                {
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                    case TypeCode.Single:
                        this.keyValuePairs[key] = ValueWrap.CreateNumber(value);
                        break;
                    default:
                        this.keyValuePairs[key] = ValueWrap.Create(value);
                        break;
                }
            }
        }

        protected void SetNull(string key)
        {
            lock (this.keyValuePairs)
            {
                this.keyValuePairs[key] = ValueWrap.Null;
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

        public abstract class ValueWrap
        {
            public static readonly ValueWrap Null = new ValueWrapGeneric<object?>(null, JsonValueKind.Null);
            public static readonly ValueWrap EmptyString = new ValueWrapString(string.Empty);

            public JsonValueKind ValueKind { get; protected set; }

            public static ValueWrap CreateNumber<T>(T value) where T : struct, IConvertible => new ValueWrapNumber<T>(value);
            public static ValueWrap Create<T>(T value) => new ValueWrapGeneric<T>(value, JsonValueKind.Undefined);

            public static ValueWrap Create(int value) => CreateNumber(value);
            public static ValueWrap Create(long value) => CreateNumber(value);
            public static ValueWrap Create(short value) => CreateNumber(value);
            public static ValueWrap Create(byte value) => CreateNumber(value);
            public static ValueWrap Create(uint value) => CreateNumber(value);
            public static ValueWrap Create(ulong value) => CreateNumber(value);
            public static ValueWrap Create(ushort value) => CreateNumber(value);
            public static ValueWrap Create(sbyte value) => CreateNumber(value);
            public static ValueWrap Create(float value) => CreateNumber(value);
            public static ValueWrap Create(double value) => CreateNumber(value);
            public static ValueWrap Create(decimal value) => CreateNumber(value);

            public static ValueWrap Create(scoped in JsonElement value) => new ValueWrapRaw(in value);

            public static ValueWrap Create(string value) => (string.IsNullOrEmpty(value) ? EmptyString : new ValueWrapString(value));

            public static ValueWrap Create(ReadOnlyMemory<char> value) => (value.IsEmpty ? EmptyString : new ValueWrapStringSegment(value));

            public static ValueWrap Create(bool value) => new ValueWrapBoolean(value);

            public T GetValue<T>() where T : IConvertible
            {
                if (this is ValueWrapGeneric<T> val) return val.Value;
                return (T)Convert.ChangeType(this.GenericValue, typeof(T));
            }

            public bool TryGetValue<T>([NotNullWhen(true)] out T value)
            {
                if (this is ValueWrapGeneric<T> val)
                {
                    value = val.Value;
                    return true;
                }
                else
                {
                    value = default;
                    return false;
                }
            }

            public abstract void WriteTo(Utf8JsonWriter writer);

            protected abstract object? GenericValue { get; }

            // The below is a hardcoded mess

            sealed class ValueWrapRaw: ValueWrap
            {
                private readonly JsonElement rawData;

                public ValueWrapRaw(in JsonElement value)
                {
                    this.rawData = value;
                    this.ValueKind = JsonValueKind.Undefined;
                }

                protected override object GenericValue => this.rawData;

                public override void WriteTo(Utf8JsonWriter writer) => this.rawData.WriteTo(writer);
            }

            class ValueWrapGeneric<T> : ValueWrap
            {
                public T Value { get; protected set; }

                public ValueWrapGeneric(T value, JsonValueKind kind)
                {
                    this.Value = value;
                    this.ValueKind = kind;
                }

                protected override object? GenericValue => this.Value;

                public override void WriteTo(Utf8JsonWriter writer)
                {
                    switch (this.ValueKind)
                    {
                        case JsonValueKind.Null:
                            writer.WriteNullValue();
                            break;
                        default:
                            if (this.Value is null) writer.WriteNullValue();
                            else writer.WriteRawValue(JsonSerializer.Serialize(this.Value));
                            break;
                    }
                }
            }

            sealed class ValueWrapBoolean : ValueWrapGeneric<bool>
            {
                public ValueWrapBoolean(bool value) : base(value, value ? JsonValueKind.True : JsonValueKind.False) { }

                public override void WriteTo(Utf8JsonWriter writer) => writer.WriteBooleanValue(this.Value);
            }

            sealed class ValueWrapString : ValueWrapGeneric<string>
            {
                public ValueWrapString(string value) : base(value, JsonValueKind.String) { }

                public override void WriteTo(Utf8JsonWriter writer) => writer.WriteStringValue(this.Value);
            }

            sealed class ValueWrapStringSegment : ValueWrapGeneric<ReadOnlyMemory<char>>
            {
                public ValueWrapStringSegment(ReadOnlyMemory<char> value) : base(value, JsonValueKind.String) { }

                public override void WriteTo(Utf8JsonWriter writer) => writer.WriteStringValue(this.Value.Span);
            }

            sealed class ValueWrapNumber<T> : ValueWrap where T : struct, IConvertible
            {
                private readonly T _value;
                public T Value => this._value;

                public ValueWrapNumber(T value)
                {
                    this._value = value;
                    this.ValueKind = JsonValueKind.Number;
                }

                protected override object GenericValue => this._value;

                public override void WriteTo(Utf8JsonWriter writer)
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    static TTo QuickCast<TTo>(scoped ref readonly T value) => Unsafe.As<T, TTo>(ref Unsafe.AsRef(in value));

                    switch (Type.GetTypeCode(this._value.GetType()))
                    {
                        case TypeCode.Byte:
                            writer.WriteNumberValue(QuickCast<byte>(in this._value));
                            break;
                        case TypeCode.SByte:
                            writer.WriteNumberValue(QuickCast<sbyte>(in this._value));
                            break;
                        case TypeCode.UInt16:
                            writer.WriteNumberValue(QuickCast<ushort>(in this._value));
                            break;
                        case TypeCode.UInt32:
                            writer.WriteNumberValue(QuickCast<uint>(in this._value));
                            break;
                        case TypeCode.UInt64:
                            writer.WriteNumberValue(QuickCast<ulong>(in this._value));
                            break;
                        case TypeCode.Int16:
                            writer.WriteNumberValue(QuickCast<short>(in this._value));
                            break;
                        case TypeCode.Int32:
                            writer.WriteNumberValue(QuickCast<int>(in this._value));
                            break;
                        case TypeCode.Int64:
                            writer.WriteNumberValue(QuickCast<long>(in this._value));
                            break;
                        case TypeCode.Decimal:
                            writer.WriteNumberValue(QuickCast<decimal>(in this._value));
                            break;
                        case TypeCode.Double:
                            writer.WriteNumberValue(QuickCast<double>(in this._value));
                            break;
                        case TypeCode.Single:
                            writer.WriteNumberValue(QuickCast<float>(in this._value));
                            break;
                        default:
                            throw new NotSupportedException();
                    }
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
                    writer.WritePropertyName(item.Key);
                    item.Value.WriteTo(writer);
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
                                    this.keyValuePairs.Add(element.Name, ValueWrap.Create(value.GetString()));
                                    break;
                                case JsonValueKind.Number:
                                    {
                                        // Yes, another hardcoded mess
                                        if (value.TryGetByte(out var byteValue)) this.keyValuePairs.Add(element.Name, ValueWrap.Create(byteValue));
                                        else if (value.TryGetSByte(out var sbyteValue)) this.keyValuePairs.Add(element.Name, ValueWrap.Create(sbyteValue));
                                        else if (value.TryGetInt16(out var shortValue)) this.keyValuePairs.Add(element.Name, ValueWrap.Create(shortValue));
                                        else if (value.TryGetUInt16(out var ushortValue)) this.keyValuePairs.Add(element.Name, ValueWrap.Create(ushortValue));
                                        else if (value.TryGetInt32(out var intValue)) this.keyValuePairs.Add(element.Name, ValueWrap.Create(intValue));
                                        else if (value.TryGetUInt32(out var uintValue)) this.keyValuePairs.Add(element.Name, ValueWrap.Create(uintValue));
                                        else if (value.TryGetInt64(out var longValue)) this.keyValuePairs.Add(element.Name, ValueWrap.Create(longValue));
                                        else if (value.TryGetUInt64(out var ulongValue)) this.keyValuePairs.Add(element.Name, ValueWrap.Create(ulongValue));
                                        else if (value.TryGetSingle(out var floatValue)) this.keyValuePairs.Add(element.Name, ValueWrap.Create(floatValue));
                                        else if (value.TryGetDouble(out var doubleValue)) this.keyValuePairs.Add(element.Name, ValueWrap.Create(doubleValue));
                                        else if (value.TryGetDecimal(out var decimalValue)) this.keyValuePairs.Add(element.Name, ValueWrap.Create(decimalValue));
                                    }
                                    break;
                                case JsonValueKind.True:
                                    this.keyValuePairs.Add(element.Name, ValueWrap.Create(true));
                                    break;
                                case JsonValueKind.False:
                                    this.keyValuePairs.Add(element.Name, ValueWrap.Create(false));
                                    break;
                                case JsonValueKind.Null:
                                    this.keyValuePairs.Add(element.Name, ValueWrap.Null);
                                    break;
                                default:
                                    this.keyValuePairs.Add(element.Name, ValueWrap.Create(value.Clone()));
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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Leayal.PSO2.UserConfig
{
    /// <summary>
    /// Primitive class to contains a token in the configuration file.
    /// </summary>
    /// <remarks>
    /// <para>A token can contain one or multiple values. A token value can be either one of the following:</para>
    /// <para>A number with fraction which is <seealso cref="double"/> (<seealso cref="float"/>, <seealso cref="decimal"/> will be convert to <seealso cref="double"/>).</para>
    /// <para>A number which is <seealso cref="long"/> (<seealso cref="int"/>, <seealso cref="short"/>, <seealso cref="byte"/> will be convert to <seealso cref="long"/>).</para>
    /// <para>A <seealso cref="string"/>.</para>
    /// <para>A <seealso cref="bool"/>.</para>
    /// <para>A <see cref="ReadOnlyMemory{char}">ReadOnlyMemory&lt;char&gt;</see> contains raw data value(s).</para>
    /// <para>A <seealso cref="ConfigToken"/> itself which contains more even values. (This means a token can be nested)</para>
    /// </remarks>
    public class ConfigToken
    {
        private static readonly string levelJump = new string(' ', 4);

        private readonly Dictionary<ReadOnlyMemory<char>, object> values;

        public ConfigToken()
        {
            this.values = new Dictionary<ReadOnlyMemory<char>, object>(KeyNameComparer.Default);
        }

        /// <summary>Gets property value with the given property name.</summary>
        /// <param name="propertyName">The property name to fetch.</param>
        /// <returns><seealso cref="null"/> if the property is not found. Otherwise the value of the property.</returns>
        /// <remarks>Setting <seealso cref="null"/> a property is equivalent to remove that property.</remarks>
        public object this[in ReadOnlyMemory<char> propertyName]
        {
            get
            {
                if (this.values.TryGetValue(propertyName, out var val))
                {
                    return val;
                }
                return null;
            }
            set
            {
                if (value == null)
                {
                    this.RemoveProperty(in propertyName);
                }
                else
                {
                    this.values[propertyName] = value;
                }
            }
        }

        // liahwgliawhglaiwgh
        public object this[string propertyName]
        {
            get => this[propertyName.AsMemory()];
            set => this[propertyName.AsMemory()] = value;
        }

        public bool RemoveProperty(string propertyName) => this.RemoveProperty(propertyName.AsMemory());

        public bool RemoveProperty(in ReadOnlyMemory<char> propertyName) => this.values.Remove(propertyName);

        public bool TryGetProperty(string propertyName, [NotNullWhen(true)] out object? value) => this.TryGetProperty(propertyName.AsMemory(), out value);

        public bool TryGetProperty(in ReadOnlyMemory<char> propertyName, [NotNullWhen(true)] out object? value) => this.values.TryGetValue(propertyName, out value);

        public void WriteValueTo(StringBuilder sb) => WriteValueTo(sb, 0);

        protected virtual void WriteValueTo(StringBuilder sb, int depth)
        {
            const string s_equal = " = ";
            sb.Append('{');
            sb.AppendLine();
            object val;
            foreach (var item in this.values)
            {
                val = item.Value;
                if (val != null)
                {
                    if (val is ConfigToken token)
                    {
                        if (token.values.Count != 0)
                        {
                            PrintIndent(sb, depth);
                            sb.Append(item.Key).Append(s_equal);
                            token.WriteValueTo(sb, depth + 1);
                            sb.Append(',').AppendLine();
                        }
                    }
                    else
                    {
                        PrintIndent(sb, depth);
                        if (val is bool b)
                        {
                            // Hardcode for boolean. This is better than trying to write val.ToString().ToLower() as it will alloc another 2 strings.
                            sb.Append(item.Key).Append(s_equal);
                            (b ? sb.Append("true,") : sb.Append("false,")).AppendLine();
                            /*
                            if (b)
                            {
                                sb.Append("true,");
                            }
                            else
                            {
                                sb.Append($"false,");
                            }
                            sb.AppendLine();
                            */
                        }
                        else if (val is double d)
                        {
                            sb.Append(item.Key).Append(s_equal);
                            (IsLong(in d) ? sb.Append(Convert.ToInt64(d)) : sb.Append(d)).Append(',').AppendLine();
                            /*
                            if (IsLong(in d))
                            {
                                sb.AppendLine($"{item.Key} = {Convert.ToInt64(d)},");
                            }
                            else
                            {
                                sb.AppendLine($"{item.Key} = {d},");
                            }
                            */
                        }
                        else if (val is string wstr)
                        {
                            sb.Append(item.Key).Append(s_equal).Append('"').Append(wstr).AppendLine("\",");
                            // sb.AppendLine($"{item.Key} = \"{wstr}\",");
                        }
                        else if (val is ReadOnlyMemory<char> c)
                        {
                            sb.Append(item.Key).Append(s_equal).Append(c).Append(',').AppendLine();
                            // sb.Append($"{item.Key} = ").Append(c).Append(',').AppendLine();
                        }
                        else
                        {
                            sb.Append(item.Key).Append(s_equal).Append(val).Append(',').AppendLine();
                            // sb.AppendLine($"{item.Key} = {val},");
                        }
                    }
                }
            }
            PrintIndent(sb, depth - 1);
            sb.Append('}');
        }

        private static bool IsLong(in double number) => Math.Abs(number % 1) <= (Double.Epsilon * 100);

        private static void PrintIndent(StringBuilder sb, int depth)
        {
            if (depth == 0) return;
            for (int i = 1; i <= depth; i++)
                sb.Append(levelJump);
        }

        public ConfigToken CreateOrSelect(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path));

            var found = this;
            foreach (var name in Shared.PathHelper.Walk(path))
            {
                if (found.TryGetProperty(name, out var val))
                {
                    if (val is ConfigToken token)
                    {
                        found = token;
                    }
                    else
                    {
                        throw new System.IO.InvalidDataException();
                    }
                }
                else
                {
                    var created = new ConfigToken();
                    found[name] = created;
                    found = created;
                }
            }
            return found;
        }

        class KeyNameComparer : IComparer<ReadOnlyMemory<char>>, IEqualityComparer<ReadOnlyMemory<char>>
        {
            public static readonly KeyNameComparer Default = new KeyNameComparer();

            public int Compare(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y) => x.Span.CompareTo(y.Span, StringComparison.OrdinalIgnoreCase);

            public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y) => x.Span.Equals(y.Span, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode(ReadOnlyMemory<char> obj)
            {
                if (obj.Length == 0) return 0;
                var span = obj.Span;
                int hashcode = span[0].GetHashCode();
                if (obj.Length == 1)
                {
                    return hashcode;
                }

                for (int i = 1; i < span.Length; i++)
                {
                    hashcode ^= span[i].GetHashCode();
                }
                return hashcode;
            }
        }
    }
}

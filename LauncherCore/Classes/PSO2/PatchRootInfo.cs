using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public readonly struct PatchRootInfo : IReadOnlyDictionary<string, PatchRootInfoValue>, IEquatable<PatchRootInfo>
    {
        private readonly FrozenDictionary<string, PatchRootInfoValue> content;

        public PatchRootInfo(string data)
        {
            var items = new Dictionary<string, PatchRootInfoValue>();

            using (var reader = new StringReader(data))
            {
                var line = reader.ReadLine();
                string key;
                int indexOfEqual;

                while (line != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        indexOfEqual = line.IndexOf('=');
                        if (indexOfEqual != -1)
                        {
                            key = line.Substring(0, indexOfEqual);
                            // value = line.Substring(indexOfEqual + 1);

                            items.Add(key, new PatchRootInfoValue(line.AsMemory(indexOfEqual + 1)));
                        }
                    }

                    line = reader.ReadLine();
                }
            }

            this.content = FrozenDictionary.ToFrozenDictionary(items);
            items.Clear();
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null) return false;
            else if (obj is PatchRootInfo info) return this.Equals(info);
            else return false;
        }

        public bool Equals(PatchRootInfo? info)
        {
            if (!info.HasValue) return false;
            return this.Equals(info.Value);
        }

        public bool Equals(PatchRootInfo info)
        {
            var count = this.content.Count;
            if (count != info.content.Count) return false;

            foreach (var myOwnItem in this.content)
            {
                ref readonly var comparand = ref info.content.GetValueRefOrNullRef(myOwnItem.Key);
                if (Unsafe.IsNullRef(in comparand)) return false;
                if (!MemoryExtensions.SequenceEqual(myOwnItem.Value.RawValue.Span, comparand.RawValue.Span)) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hc = new HashCode();
            var comparer = this.content.Comparer;
            foreach (var myOwnItem in this.content)
            {
                hc.Add(myOwnItem.Key, comparer);
                hc.Add(myOwnItem.Value);
            }
            return hc.ToHashCode();
        }

        public readonly PatchRootInfoValue this[string key] => this.content[key];

        public readonly IEnumerable<string> Keys => this.content.Keys;

        public readonly IEnumerable<PatchRootInfoValue> Values => this.content.Values;

        public readonly int Count => this.content.Count;

        public readonly bool ContainsKey(string key) => this.content.ContainsKey(key);

        public readonly IEnumerator<KeyValuePair<string, PatchRootInfoValue>> GetEnumerator() => this.content.GetEnumerator();

        public readonly bool TryGetValue(string key, out PatchRootInfoValue value)
        {
            // value = Unsafe.AsRef(in this.content.GetValueRefOrNullRef(key));
            ref readonly var result = ref this.content.GetValueRefOrNullRef(key);
            if (Unsafe.IsNullRef(in result))
            {
                value = Unsafe.NullRef<PatchRootInfoValue>(); // To workaround the uninit
                return false;
            }
            else
            {
                value = result;
                return true;
            }
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)content).GetEnumerator();
        }

        private readonly bool TryGetStringValue(string key, [NotNullWhen(true)] out string? value)
        {
            if (this.TryGetValue(key, out var info))
            {
                value = info.GetString();
                return true;
            }
            value = null;
            return false;
        }

        public readonly bool TryGetMasterURL([NotNullWhen(true)] out string? value) => this.TryGetStringValue("MasterURL", out value);

        public readonly bool TryGetPatchURL([NotNullWhen(true)] out string? value) => this.TryGetStringValue("PatchURL", out value);

        public readonly bool TryGetBackupMasterURL([NotNullWhen(true)] out string? value) => this.TryGetStringValue("BackupMasterURL", out value);

        public readonly bool TryGetBackupPatchURL([NotNullWhen(true)] out string? value) => this.TryGetStringValue("BackupPatchURL", out value);

        public readonly bool TryGetMasterURL(out PatchRootInfoValue value) => this.TryGetValue("MasterURL", out value);

        public readonly bool TryGetPatchURL(out PatchRootInfoValue value) => this.TryGetValue("PatchURL", out value);

        public readonly bool TryGetBackupMasterURL(out PatchRootInfoValue value) => this.TryGetValue("BackupMasterURL", out value);

        public readonly bool TryGetBackupPatchURL(out PatchRootInfoValue value) => this.TryGetValue("BackupPatchURL", out value);

        public static bool operator ==(PatchRootInfo left, PatchRootInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PatchRootInfo left, PatchRootInfo right)
        {
            return !(left == right);
        }
    }
}

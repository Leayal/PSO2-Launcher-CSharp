using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public class PatchRootInfo : IReadOnlyDictionary<string, PatchRootInfoValue>, IDisposable
    {
        private readonly Dictionary<string, PatchRootInfoValue> content;

        public PatchRootInfo(string data)
        {
            this.content = new Dictionary<string, PatchRootInfoValue>();

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

                            this.content.Add(key, new PatchRootInfoValue(line.AsMemory(indexOfEqual + 1)));
                        }
                    }

                    line = reader.ReadLine();
                }
            }
        }

        public PatchRootInfoValue this[string key] => this.content[key];

        public IEnumerable<string> Keys => ((IReadOnlyDictionary<string, PatchRootInfoValue>)content).Keys;

        public IEnumerable<PatchRootInfoValue> Values => ((IReadOnlyDictionary<string, PatchRootInfoValue>)content).Values;

        public int Count => this.content.Count;

        public bool ContainsKey(string key) => this.content.ContainsKey(key);

        public IEnumerator<KeyValuePair<string, PatchRootInfoValue>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, PatchRootInfoValue>>)content).GetEnumerator();
        }

        public bool TryGetValue(string key, out PatchRootInfoValue value) => this.content.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)content).GetEnumerator();
        }

        private bool TryGetStringValue(string key, [MaybeNullWhen(false)] out string value)
        {
            if (this.content.TryGetValue(key, out var info))
            {
                value = info.GetString();
                return true;
            }
            value = null;
            return false;
        }

        public bool TryGetMasterURL([MaybeNullWhen(false)] out string value) => this.TryGetStringValue("MasterURL", out value);

        public bool TryGetPatchURL([MaybeNullWhen(false)] out string value) => this.TryGetStringValue("PatchURL", out value);

        public bool TryGetBackupMasterURL([MaybeNullWhen(false)] out string value) => this.TryGetStringValue("BackupMasterURL", out value);

        public bool TryGetBackupPatchURL([MaybeNullWhen(false)] out string value) => this.TryGetStringValue("BackupPatchURL", out value);

        public bool TryGetMasterURL(out PatchRootInfoValue value) => this.TryGetValue("MasterURL", out value);

        public bool TryGetPatchURL(out PatchRootInfoValue value) => this.TryGetValue("PatchURL", out value);

        public bool TryGetBackupMasterURL(out PatchRootInfoValue value) => this.TryGetValue("BackupMasterURL", out value);

        public bool TryGetBackupPatchURL(out PatchRootInfoValue value) => this.TryGetValue("BackupPatchURL", out value);

        public void Dispose() => this.content.Clear();
    }
}

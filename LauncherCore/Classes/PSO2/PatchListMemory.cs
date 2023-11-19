using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public class PatchListMemory : PatchListBase, IReadOnlyCollection<PatchListItem>
    {
        private readonly FrozenDictionary<string, PatchListItem> _items;

        public override bool CanCount => true;

        public override int Count => this._items.Count;

        public PatchListMemory(PatchRootInfo rootInfo, bool? isReboot, IReadOnlyDictionary<string, PatchListItem> items) : base(rootInfo, isReboot)
        {
            this._items = FrozenDictionary.ToFrozenDictionary(items, PathStringComparer.Default);
        }

        public PatchListMemory(PatchRootInfo rootInfo, bool? isReboot) : base(rootInfo, isReboot)
        {
            this._items = FrozenDictionary<string, PatchListItem>.Empty;
        }

        protected override IEnumerator<PatchListItem> CreateEnumerator()
        {
            var items = this._items.Values;
            var count = items.Length;
            for (int i = 0; i < count; i++) yield return items.ItemRef(i);
        }

        public override bool TryGetByFilenameExact(string filename, [NotNullWhen(true)] out PatchListItem? value) => this._items.TryGetValue(filename, out value);

        protected override void CopyTo(IDictionary<string, PatchListItem> items, bool clearBeforeCopy)
        {
            if (clearBeforeCopy)
            {
                items.Clear();
            }
            foreach (var item in this._items)
            {
                items.Add(item.Key, item.Value);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this._items is IDictionary<string, PatchListItem> dict)
                {
                    dict.Clear();
                    if (dict is Dictionary<string, PatchListItem> dict2)
                    {
                        dict2.TrimExcess(0);
                    }
                }
            }
        }
    }
}

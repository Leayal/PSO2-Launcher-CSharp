using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public sealed class PatchListMemory : PatchListBase, IReadOnlyCollection<PatchListItem>, IReadOnlyDictionary<string, PatchListItem>
    {
        private readonly FrozenDictionary<string, PatchListItem> _items;

        public override bool CanCount => true;

        public override int Count => this._items.Count;

        public ImmutableArray<string> Keys => this._items.Keys;

        public ImmutableArray<PatchListItem> Values => this._items.Values;

        IEnumerable<string> IReadOnlyDictionary<string, PatchListItem>.Keys => this._items.Keys;

        IEnumerable<PatchListItem> IReadOnlyDictionary<string, PatchListItem>.Values => this._items.Values;

        public PatchListItem this[string key] => this._items[key];

        public PatchListMemory(PatchRootInfo? rootInfo, bool? isReboot, IReadOnlyDictionary<string, PatchListItem> items) : base(rootInfo, isReboot)
        {
            this._items = FrozenDictionary.ToFrozenDictionary(items, PathStringComparer.Default);
        }

        public PatchListMemory(PatchRootInfo? rootInfo, bool? isReboot) : base(rootInfo, isReboot)
        {
            this._items = FrozenDictionary<string, PatchListItem>.Empty;
        }

        protected override IEnumerator<PatchListItem> CreateEnumerator() => new Enumerator(this._items.Values);        

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

        public bool ContainsKey(string key)
        {
            return ((IReadOnlyDictionary<string, PatchListItem>)_items).ContainsKey(key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out PatchListItem value)
        {
            return ((IReadOnlyDictionary<string, PatchListItem>)_items).TryGetValue(key, out value);
        }

        IEnumerator<KeyValuePair<string, PatchListItem>> IEnumerable<KeyValuePair<string, PatchListItem>>.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, PatchListItem>>)_items).GetEnumerator();
        }

        sealed class Enumerator : IEnumerator<PatchListItem>
        {
            /// <summary>The span being enumerated.</summary>
            private readonly ImmutableArray<PatchListItem> _items;
            /// <summary>The next index to yield.</summary>
            private int _index;

            /// <summary>Initialize the enumerator.</summary>
            /// <param name="span">The span to enumerate.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(ImmutableArray<PatchListItem> items)
            {
                _items = items;
                _index = -1;
            }

            /// <summary>Advances the enumerator to the next element of the span.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                int index = _index + 1;
                if (index < _items.Length)
                {
                    _index = index;
                    return true;
                }

                return false;
            }

            public void Reset() => _index = -1;

            public void Dispose() { }

            /// <summary>Gets the element at the current position of the enumerator.</summary>
            public ref readonly PatchListItem Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _items.ItemRef(_index);
            }

            PatchListItem IEnumerator<PatchListItem>.Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.Current;
            }

            object IEnumerator.Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.Current;
            }
        }
    }
}

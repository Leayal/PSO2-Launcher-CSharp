using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    /// <summary>Provides instance for forward-only parsing patchlist file.</summary>
    /// <remarks>Mainly used for building <see cref="PatchListMemory"/> or enumerating.</remarks>
    public class PatchListDeferred : PatchListBase
    {
        private readonly TextReader tr;
        private readonly bool keepOpen;
        
        public PatchListDeferred(PatchRootInfo rootInfo, bool? isReboot, TextReader reader) : this(rootInfo, isReboot, reader, false) { }

        public PatchListDeferred(PatchRootInfo rootInfo, bool? isReboot, TextReader reader, bool keepOpen) : base(rootInfo, isReboot)
        {
            this.keepOpen = keepOpen;
            this.tr = reader;
        }

        public override bool CanCount => false;

        public override int Count => throw new NotSupportedException();

        protected override IEnumerator<PatchListItem> CreateEnumerator() => new PatchListItemWalker(this);

        class PatchListItemWalker : IEnumerator<PatchListItem>
        {
            private readonly PatchListBase parent;
            private readonly TextReader tr;
            private PatchListItem currentItem = PatchListItem.Empty;

            public PatchListItemWalker(PatchListDeferred parent)
            {
                this.parent = parent;
                this.tr = parent.tr;
            }

            public PatchListItem Current => this.currentItem;

            object? IEnumerator.Current => this.currentItem;

            public void Dispose()
            {
                this.currentItem = PatchListItem.Empty;
            }

            [MemberNotNullWhen(true, nameof(currentItem))]
            public bool MoveNext()
            {
                var currentLine = this.tr.ReadLine();
                if (currentLine != null)
                {
                    this.currentItem = PatchListItem.Parse(this.parent, currentLine);
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                // Does nothing
            }
        }

        /// <remarks>What do you expect? It's deferred reading. It's best suit for forward-only read operation.</remarks>
        public override bool TryGetByFilename(string filename, out PatchListItem value)
        {
            throw new NotSupportedException();
        }

        public override bool TryGetByFilenameExact(string filename, out PatchListItem value)
        {
            throw new NotSupportedException();
        }

        protected override void CopyTo(IDictionary<string, PatchListItem> items, bool clearBeforeCopy)
        {
            if (clearBeforeCopy)
            {
                items.Clear();
            }
            foreach (var item in this)
            {
                items.Add(item.GetFilenameWithoutAffix(), item);
            }
        }

        public PatchListMemory ToMemory()
        {
            var items = new Dictionary<string, PatchListItem>(StringComparer.OrdinalIgnoreCase);
            this.CopyTo(items, false);
            return new PatchListMemory(this.RootInfo, this.IsReboot, items);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.keepOpen)
                {
                    this.tr.Dispose();
                }
            }
        }
    }
}

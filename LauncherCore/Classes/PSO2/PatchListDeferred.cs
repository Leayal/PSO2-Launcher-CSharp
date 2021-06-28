using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    /// <summary>Provides instance for forward-only parsing patchlist file.</summary>
    /// <remarks>Mainly used for building <see cref="PatchListMemory"/> or enumerating.</remarks>
    public class PatchListDeferred : PatchListBase, IDisposable
    {
        private readonly TextReader tr;
        private readonly bool keepOpen;

        public PatchListDeferred(PatchRootInfo rootInfo, bool? isReboot, TextReader textReader) : this(rootInfo, isReboot, textReader, false) { }

        public PatchListDeferred(PatchRootInfo rootInfo, bool? isReboot, TextReader textReader, bool keepOpen) : base(rootInfo, isReboot)
        {
            this.keepOpen = keepOpen;
            this.tr = textReader;
        }

        public override IEnumerator<PatchListItem> GetEnumerator() => new PatchListItemWalker(this);

        protected override IEnumerator CreateEnumerator() => new PatchListItemWalker(this);

        class PatchListItemWalker : IEnumerator<PatchListItem>
        {
            private readonly PatchListDeferred parent;
            private PatchListItem currentItem;

            public PatchListItemWalker(PatchListDeferred parent)
            {
                this.parent = parent;
            }

            public PatchListItem Current => this.currentItem;

            object IEnumerator.Current => this.currentItem;

            public void Dispose()
            {
                // Does nothing
            }

            public bool MoveNext()
            {
                var currentLine = this.parent.tr.ReadLine();
                if (currentLine != null)
                {
                    this.currentItem = PatchListItem.Parse(this.parent, in currentLine);
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
        public override bool TryGetByFilename(in string filename, out PatchListItem value)
        {
            throw new NotSupportedException();
        }

        public override bool TryGetByFilenameExact(in string filename, out PatchListItem value)
        {
            throw new NotSupportedException();
        }

        protected override void CopyTo(Dictionary<string, PatchListItem> items) => this.InnerCopyTo(items);

        private void InnerCopyTo(Dictionary<string, PatchListItem> items)
        {
            items.Clear();
            foreach (var item in this)
            {
                items.Add(item.GetFilenameWithoutAffix(), item);
            }
        }

        public PatchListMemory ToMemory()
        {
            var items = new Dictionary<string, PatchListItem>(StringComparer.OrdinalIgnoreCase);
            this.InnerCopyTo(items);
            return new PatchListMemory(this.RootInfo, this.IsReboot, items);
        }

        public void Dispose()
        {
            if (!this.keepOpen)
            {
                this.tr.Dispose();
            }
        }
    }
}

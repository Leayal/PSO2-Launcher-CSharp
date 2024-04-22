using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    /// <summary>Provides instance for parsing patchlist file from a cached memory/buffer.</summary>
    /// <remarks>For seeking, <seealso cref="MemoryStream"/> must has its internal buffer visible.</remarks>
    public sealed class PatchListSeekableDeferred : PatchListDeferred
    {
        private readonly MemoryStream mem;
        private readonly bool keepOpen;
        private int _count;

        public PatchListSeekableDeferred(PatchRootInfo rootInfo, bool? isReboot, MemoryStream dataBuffer) : this(rootInfo, isReboot, dataBuffer, false) { }

        public PatchListSeekableDeferred(PatchRootInfo rootInfo, bool? isReboot, MemoryStream dataBuffer, bool keepOpen) : base(rootInfo, isReboot, TextReader.Null, true)
        {
            this._count = -1;
            this.keepOpen = keepOpen;
            this.mem = dataBuffer;
        }

        public override bool CanCount => true;

        public override int Count
        {
            get
            {
                if (this._count == -1)
                {
                    this._count = 0;
                    if (mem.TryGetBuffer(out var segment) && segment.Array != null)
                    {
                        using (var view = new MemoryStream(segment.Array, segment.Offset, segment.Count, false))
                        using (var sr = new StreamReader(view))
                        {
                            while (sr.ReadLine() != null)
                            {
                                this._count++;
                            }
                        }
                    }
                    else
                    {
                        this.mem.Position = 0;
                        using (var sr = new StreamReader(this.mem, leaveOpen: true))
                        {
                            while (sr.ReadLine() != null)
                            {
                                this._count++;
                            }
                        }
                        this.mem.Position = 0;
                    }
                }
                return this._count;
            }
        }

        protected override IEnumerator<PatchListItem> CreateEnumerator() => new PatchListItemWalker(this);

        class PatchListItemWalker : IEnumerator<PatchListItem>
        {
            private readonly PatchListBase parent;
            private readonly MemoryStream? view;
            private readonly StreamReader tr;
            private PatchListItem currentItem;

            public PatchListItemWalker(PatchListSeekableDeferred parent)
            {
                this.parent = parent;
                if (parent.mem.TryGetBuffer(out var segment) && segment.Array != null)
                {
                    this.view = new MemoryStream(segment.Array, segment.Offset, segment.Count, false);
                    this.view.Position = 0;
                    this.tr = new StreamReader(this.view, leaveOpen: false);
                }
                else
                {
                    this.view = null;
                    this.tr = new StreamReader(parent.mem, leaveOpen: true);
                }
            }

            public PatchListItem Current => this.currentItem;

            object IEnumerator.Current => this.currentItem;

            public void Dispose() => this.tr.Dispose();

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
                if (this.view == null)
                {
                    throw new NotSupportedException();
                }
                else
                {
                    this.view.Position = 0;
                }
                // Does nothing
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.keepOpen)
                {
                    this.mem.Dispose();
                }
            }
        }
    }
}

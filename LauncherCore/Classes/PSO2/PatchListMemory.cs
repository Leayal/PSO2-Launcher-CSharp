using System;
using System.Collections.Generic;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public class PatchListMemory : PatchListBase
    {
        private readonly Dictionary<string, PatchListItem> _items;

        public int Count => this._items.Count;

        public PatchListMemory(PatchRootInfo rootInfo, bool? isReboot, IDictionary<string, PatchListItem> items) : base(rootInfo, isReboot)
        {
            this._items = new Dictionary<string, PatchListItem>(items, StringComparer.OrdinalIgnoreCase);
        }

        public PatchListMemory(PatchRootInfo rootInfo, bool? isReboot) : base(rootInfo, isReboot)
        {
            this._items = new Dictionary<string, PatchListItem>(StringComparer.OrdinalIgnoreCase);
        }

        internal PatchListMemory(PatchRootInfo rootInfo, bool? isReboot, Dictionary<string, PatchListItem> items) : base(rootInfo, isReboot)
        {
            this._items = items;
        }

        public override IEnumerator<PatchListItem> GetEnumerator() => this._items.Values.GetEnumerator();

        protected override IEnumerator CreateEnumerator() => this._items.Values.GetEnumerator();

        public override bool TryGetByFilenameExact(in string filename, out PatchListItem value) => this._items.TryGetValue(filename, out value);

        protected override void CopyTo(Dictionary<string, PatchListItem> items)
        {
            items.Clear();
            foreach (var item in this._items)
            {
                items.Add(item.Key, item.Value);
            }
        }
    }
}

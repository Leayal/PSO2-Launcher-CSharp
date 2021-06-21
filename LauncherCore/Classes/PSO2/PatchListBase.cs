using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    abstract class PatchListBase : IEnumerable<PatchListItem>, IEnumerable
    {
        public abstract IEnumerator<PatchListItem> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.CreateEnumerator();

        protected abstract IEnumerator CreateEnumerator();

        public virtual bool TryGetByFilename(in string filename, out PatchListItem value)
        {
            if (this.TryGetByFilenameExact(in filename, out value))
            {
                return true;
            }
            else
            {
                var affixedName = filename + ".pat";
                return this.TryGetByFilenameExact(in affixedName, out value);
            }
        }

        public abstract bool TryGetByFilenameExact(in string filename, out PatchListItem value);

        protected abstract void CopyTo(Dictionary<string, PatchListItem> items);

        /// <summary>Merge all the patchlists into one and return the merged patchlist.</summary>
        /// <returns>Return a patchlist which has all the items from given <paramref name="patchlists"/>.</returns>
        public static PatchListBase Create(params PatchListBase[] patchlists)
        {
            if (patchlists == null)
            {
                throw new ArgumentNullException(nameof(patchlists));
            }
            else if (patchlists.Length == 0)
            {
                throw new ArgumentException(nameof(patchlists));
            }

            if (patchlists.Length == 1)
            {
                return patchlists[0];
            }
            else
            {
                var items = new Dictionary<string, PatchListItem>(StringComparer.OrdinalIgnoreCase);
                for (int i = 1; i < patchlists.Length; i++)
                {
                    foreach (var item in patchlists[i])
                    {
                        if (items.TryGetValue(item.Filename, out var value))
                        {
                            if (!value.PatchOrBase.HasValue && !item.PatchOrBase.HasValue)
                            {
                                items[item.Filename] = item;
                            }
                            else if (value.PatchOrBase.HasValue && !item.PatchOrBase.HasValue)
                            {
                                items[item.Filename] = item;
                            }
                            else if (!value.PatchOrBase.HasValue && item.PatchOrBase.HasValue)
                            {
                                // Keep current existing.
                            }
                            else
                            {
                                if (item.PatchOrBase == true && value.PatchOrBase == true)
                                {
                                    items[item.Filename] = item;
                                }
                                else if (item.PatchOrBase == false && value.PatchOrBase == true)
                                {
                                    // Keep current existing.
                                }
                                else if (item.PatchOrBase == true && value.PatchOrBase == false)
                                {
                                    items[item.Filename] = item;
                                }
                                // Get whichever is `p`
                            }
                        }
                        else
                        {
                            items.Add(item.Filename, item);
                        }
                    }
                }
                return new PatchListMemory(items);
            }
        }
    }
}

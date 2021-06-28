using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public abstract class PatchListBase : IEnumerable<PatchListItem>, IEnumerable
    {
        public readonly PatchRootInfo RootInfo;
        public readonly bool? IsReboot;

        protected PatchListBase(PatchRootInfo rootInfo, bool? isReboot)
        {
            if (rootInfo == null)
            {
                throw new ArgumentNullException(nameof(rootInfo));
            }
            this.IsReboot = isReboot;
            this.RootInfo = rootInfo;
        }

        public abstract IEnumerator<PatchListItem> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.CreateEnumerator();

        protected abstract IEnumerator CreateEnumerator();

        public virtual bool TryGetByFilename(in string filename, out PatchListItem value)
        {
            if (filename.EndsWith(PatchListItem.AffixFilename))
            {
                if (this.TryGetByFilenameExact(in filename, out value))
                {
                    return true;
                }
                else
                {
                    return this.TryGetByFilenameExact(PatchListItem.GetFilenameWithoutAffix(filename), out value);
                }
            }
            else
            {
                if (this.TryGetByFilenameExact(in filename, out value))
                {
                    return true;
                }
                else
                {
                    var affixedName = filename + PatchListItem.AffixFilename;
                    return this.TryGetByFilenameExact(in affixedName, out value);
                }
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
                PatchRootInfo comparand = null;
                var items = new Dictionary<string, PatchListItem>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < patchlists.Length; i++)
                {
                    if (comparand == null)
                    {
                        comparand = patchlists[i].RootInfo;
                    }
                    else if (comparand != patchlists[i].RootInfo)
                    {
                        throw new InvalidOperationException();
                    }
                    foreach (var item in patchlists[i])
                    {
                        var filenameWithoutAffix = item.GetFilenameWithoutAffix();
                        if (items.TryGetValue(filenameWithoutAffix, out var value))
                        {
                            if (!value.PatchOrBase.HasValue && !item.PatchOrBase.HasValue)
                            {
                                items[filenameWithoutAffix] = item;
                            }
                            else if (value.PatchOrBase.HasValue && !item.PatchOrBase.HasValue)
                            {
                                items[filenameWithoutAffix] = item;
                            }
                            else if (!value.PatchOrBase.HasValue && item.PatchOrBase.HasValue)
                            {
                                // Keep current existing.
                            }
                            else
                            {
                                if (item.PatchOrBase == true && value.PatchOrBase == true)
                                {
                                    items[filenameWithoutAffix] = item;
                                }
                                else if (item.PatchOrBase == false && value.PatchOrBase == true)
                                {
                                    // Keep current existing.
                                }
                                else if (item.PatchOrBase == true && value.PatchOrBase == false)
                                {
                                    items[filenameWithoutAffix] = item;
                                }
                                // Get whichever is `p`
                            }
                        }
                        else
                        {
                            items.Add(filenameWithoutAffix, item);
                        }
                    }
                }
                return new PatchListMemory(comparand, items);
            }
        }
    }
}

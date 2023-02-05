using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2
{
    public abstract class PatchListBase : IEnumerable<PatchListItem>, IEnumerable, IReadOnlyCollection<PatchListItem>, IDisposable
    {
        public readonly PatchRootInfo RootInfo;

        /// <summary>True = NGS/Reboot. False = Classic. Null = Unspecific.</summary>
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

        public abstract bool CanCount { get; }

        public abstract int Count { get; }

        public IEnumerator<PatchListItem> GetEnumerator() => this.CreateEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.CreateEnumerator();

        protected abstract IEnumerator<PatchListItem> CreateEnumerator();

        public virtual bool TryGetByFilename(string filename, [MaybeNullWhen(false)] out PatchListItem value)
        {
            if (filename.EndsWith(PatchListItem.AffixFilename))
            {
                if (this.TryGetByFilenameExact(filename, out value))
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
                if (this.TryGetByFilenameExact(filename, out value))
                {
                    return true;
                }
                else
                {
                    var affixedName = filename + PatchListItem.AffixFilename;
                    return this.TryGetByFilenameExact(affixedName, out value);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PatchListBase()
        {
            this.Dispose(false);
        }

        public abstract bool TryGetByFilenameExact(string filename, [NotNullWhen(true)] out PatchListItem value);

        protected abstract void CopyTo(Dictionary<string, PatchListItem> items, bool clearBeforeCopy);

        /// <summary>Merge all the patchlists into one and return the merged patchlist.</summary>
        /// <returns>Return a patchlist which has all the items from given <paramref name="patchlists"/>.</returns>
        public static PatchListMemory Create(params PatchListBase[] patchlists)
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
                if (patchlists[0] is PatchListMemory mem)
                {
                    return mem;
                }
                else
                {
                    var list = patchlists[0];
                    Dictionary<string, PatchListItem> items;
                    if (list.CanCount)
                    {
                        items = new Dictionary<string, PatchListItem>(list.Count + 1, StringComparer.OrdinalIgnoreCase);
                    }
                    else
                    {
                        items = new Dictionary<string, PatchListItem>(StringComparer.OrdinalIgnoreCase);
                    }
                    list.CopyTo(items, false);
                    return new PatchListMemory(list.RootInfo, list.IsReboot, items);
                }
            }
            else
            {
                PatchRootInfo? comparand = null;
                var items = new Dictionary<string, PatchListItem>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < patchlists.Length; i++)
                {
                    var patchlist = patchlists[i];
                    if (comparand == null)
                    {
                        comparand = patchlist.RootInfo;
                    }
                    else if (comparand != patchlist.RootInfo)
                    {
                        throw new InvalidOperationException();
                    }
                    if (patchlist.CanCount)
                    {
                        items.EnsureCapacity(items.Count + patchlist.Count);
                    }
                    foreach (var item in patchlist)
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
                return new PatchListMemory(comparand, null, items);
            }
        }
    }
}

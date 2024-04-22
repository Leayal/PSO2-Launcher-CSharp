using Leayal.PSO2.Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Leayal.PSO2Launcher.Core.Windows
{
    public sealed partial class ModsOrganizerWindow
    {
        public sealed class ModDataFileListBinding : INotifyCollectionChanged,INotifyPropertyChanged, IReadOnlyList<ModDataFile>//, IReadOnlyDictionary<string, ModDataFile>
        {
            private readonly ObservableCollection<ModDataFile> _files; // For head count purpose and present checker
            private readonly List<ModDataFile> _indexing; // For mod list order

            public int Count => this._files.Count;

            public ModDataFile this[int index] => throw new NotImplementedException();

            public event NotifyCollectionChangedEventHandler? CollectionChanged;
            public event PropertyChangedEventHandler? PropertyChanged;

            private void OnCollectionChanged_Reset()
            {
                OnPropertyChanged();
                var view = CollectionViewSource.GetDefaultView(this._files);
                this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            /// <summary>Called when the collection has changed.</summary>
            /// <param name="action">The action.</param>
            /// <param name="changedItem">The changed item's key.</param>
            private void OnCollectionChanged_SingleItem(NotifyCollectionChangedAction action, string changedItem)
            {
                OnPropertyChanged();
                this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItem, 0));
            }

            /// <summary>Called when the collection has changed.</summary>
            /// <param name="action">The action.</param>
            /// <param name="newItem">The new item.</param>
            /// <param name="oldItem">The old item.</param>
            private void OnCollectionChanged_Replace(KeyValuePair<string, ModDataFile> newItem, KeyValuePair<string, ModDataFile> oldItem, int itemIndex = 0)
            {
                OnPropertyChanged();
                this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem, itemIndex));
            }

            /// <summary>Called when the collection has changed.</summary>
            /// <param name="action">The action.</param>
            /// <param name="newItems">The new items' key.</param>
            private void OnCollectionChanged(NotifyCollectionChangedAction action, IList newItems)
            {
                OnPropertyChanged();
                this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItems, 0));
            }

            /// <summary>Called when the collection has changed.</summary>
            /// <param name="action">The action.</param>
            /// <param name="newItems">The removed item's key.</param>
            private void OnCollectionChanged_Remove(string removedItem, int itemIndex = 0)
            {
                OnPropertyChanged();
                this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem, itemIndex));
            }

            private void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            private void OnPropertyChanged()
            {
                this.OnPropertyChanged("Count");
                this.OnPropertyChanged("Item[]");
                // this.OnPropertyChanged("Keys");
                // this.OnPropertyChanged("Values");
            }

            public IEnumerator<ModDataFile> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>A binding between the UI and a <seealso cref="ModPackage"/> object.</summary>
        public sealed class ModPackageBindingObject : DependencyObject
        {

        }

        /// <summary>Represents a file that is going to be used for replacing game's data file.</summary>
        public sealed class ModDataFile : DependencyObject
        {
            private ModPackage? overrideOrder_TargetedOne;
            private HashSet<ModPackage>? overlappedModPackages;

            /// <summary>The relative path to the game client's data file (relative from pso2.exe's directory).</summary>
            public string FilePath { get; } = string.Empty;

            public bool IsOverlappedByMultipleModPackage => (this.overlappedModPackages?.Count != 0 ?  true : false);

            public bool IsOverridden => (this.IsOverlappedByMultipleModPackage && this.overrideOrder_TargetedOne == null);

            private void SetOrderOverride(ModPackage package)
            {
                ArgumentNullException.ThrowIfNull(package);
                if (this.overlappedModPackages == null || !this.overlappedModPackages.Contains(package)) return;

                this.overrideOrder_TargetedOne = package;

                this.OnProperty_OverlappedMods_Changed();
            }

            public void AddOverlappedModPackage(ModPackage package)
            {
                ArgumentNullException.ThrowIfNull(package);

                if (this.overlappedModPackages == null) this.overlappedModPackages = new HashSet<ModPackage>();
                
                if (this.overlappedModPackages.Add(package))
                {
                    this.OnProperty_OverlappedMods_Changed();
                }
            }

            public void RemoveOverlappedModPackage(ModPackage package)
            {
                ArgumentNullException.ThrowIfNull(package);

                if (this.overlappedModPackages == null) return;

                if (this.overlappedModPackages.Remove(package))
                {
                    Interlocked.CompareExchange(ref this.overrideOrder_TargetedOne, null, package);
                    this.OnProperty_OverlappedMods_Changed();
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            private void OnProperty_OverlappedMods_Changed()
            {
                this.OnPropertyChanged(nameof(IsOverlappedByMultipleModPackage));
                this.OnPropertyChanged(nameof(IsOverridden));
            }

            // public override bool Equals(object? obj) => (obj is ModDataFile modDataFile ? this.Equals(modDataFile) : false);

            protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
            {
                base.OnPropertyChanged(e);
            }

            public bool Equals(ModDataFile? modDataFile)
                => (modDataFile == null) ? false : StringComparer.OrdinalIgnoreCase.Equals(this.FilePath, modDataFile.FilePath);
        }
    }
}

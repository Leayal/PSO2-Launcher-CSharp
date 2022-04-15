using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using Leayal.Shared.Windows;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.SharedInterfaces;
using System.Windows.Controls;
using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Core.UIElements;
using System.Windows.Data;
using System.ComponentModel;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for LauncherThemingManagerWindow.xaml
    /// </summary>
    public partial class DataOrganizerWindow : MetroWindowEx
    {
        public static readonly DependencyProperty CustomizationFileListProperty = DependencyProperty.Register("CustomizationFileList", typeof(ICollectionView), typeof(DataOrganizerWindow));

        public static readonly DataAction[] DataActions = { DataAction.DoNothing, DataAction.Delete, DataAction.Move, DataAction.MoveAndSymlink };

        public static readonly EnumComboBox.ValueDOM<FilterType>[] FilterTypes = System.Linq.Enumerable.ToArray(EnumComboBox.EnumToDictionary<FilterType>().Values);

        public ICollectionView CustomizationFileList
        {
            get => (ICollectionView)this.GetValue(CustomizationFileListProperty);
            set => this.SetValue(CustomizationFileListProperty, value);
        }

        private readonly Classes.ConfigurationFile _config;
        private FilterType _currentFilterType;
        private string _currentFilterString;

        public DataOrganizerWindow(Classes.ConfigurationFile conf)
        {
            this._config = conf;
            InitializeComponent();
        }

        private void ThisSelf_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            using (var rootInfo = new PatchRootInfo(Path.Combine(RuntimeValues.RootDirectory, "management_beta.txt")))
            using (var tr = new StreamReader(Path.Combine(RuntimeValues.RootDirectory, "patchlist_all.txt")))
            using (var deferred = new PatchListDeferred(rootInfo, null, tr))
            {
                var list = deferred.CanCount ? new List<CustomizationFileListItem>(deferred.Count) : new List<CustomizationFileListItem>();
                foreach (var item in deferred)
                {
                    list.Add(new CustomizationFileListItem() { RemoteFilename = item.GetFilenameWithoutAffix() });
                }
                this.CustomizationFileList = CollectionViewSource.GetDefaultView(list);
            }
#endif
        }

        public void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ButtonSelectDeletePSO2ClassicPreset_Click(object sender, RoutedEventArgs e)
        {
            this.tabCustomizePreset.IsSelected = true;
        }

        private void Item_ActionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cbox && cbox.DataContext is CustomizationFileListItem item)
            {
                item.SelectedAction = (DataAction)cbox.SelectedValue;
            }
        }

        class CustomizationFileListItem : DependencyObject
        {
            public static readonly DependencyPropertyKey HasActionSettingsPropertyKey = DependencyProperty.RegisterReadOnly("HasActionSettings", typeof(bool), typeof(CustomizationFileListItem), new PropertyMetadata(false));
            public static readonly DependencyProperty HasActionSettingsProperty = HasActionSettingsPropertyKey.DependencyProperty;
            public static readonly DependencyProperty DataActionProperty = DependencyProperty.Register("DataAction", typeof(DataAction), typeof(CustomizationFileListItem), new PropertyMetadata(DataAction.DoNothing, (obj, e) =>
            {
                if (obj is CustomizationFileListItem item)
                {
                    switch (item.SelectedAction)
                    {
                        case DataAction.MoveAndSymlink:
                        case DataAction.Move:
                            item.SetValue(HasActionSettingsPropertyKey, true);
                            break;
                        default:
                            item.SetValue(HasActionSettingsPropertyKey, false);
                            break;
                    }
                }
            }));

            public string RemoteFilename { get; init; }

            public DataAction SelectedAction
            {
                get => (DataAction)this.GetValue(DataActionProperty);
                set => this.SetValue(DataActionProperty, value);
            }

            public bool HasActionSettings => (bool)this.GetValue(HasActionSettingsProperty);
        }

        public enum FilterType
        {
            [EnumDisplayName("Filter by name")]
            Name,
            [EnumDisplayName("Filter by size (bigger than)")]
            SizeBigger,
            [EnumDisplayName("Filter by size (smaller than)")]
            SizeSmaller,
            [EnumDisplayName("Filter by client type")]
            ClientType
        }

        public enum DataAction
        {
            DoNothing,
            Delete,
            Move,
            MoveAndSymlink
        }

        private void FilterByString_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                this._currentFilterString = tb.Text;
                this.ReFiltering();
            }
        }

        private void FilterType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count != 0 && e.AddedItems[0] is EnumComboBox.ValueDOM<FilterType> val)
            {
                this._currentFilterType = val.Value;
                this.ReFiltering();
            }
        }

        private void ReFiltering()
        {
            if (this.CustomizationFileList != null)
            {
                switch (this._currentFilterType)
                {
                    case FilterType.Name:
                        this.CustomizationFileList.Filter = this.Filtering_ByName;
                        break;
                    default:
                        this.CustomizationFileList.Filter = null;
                        break;
                }
            }
        }

        private bool Filtering_ByName(object obj)
        {
            if (obj is CustomizationFileListItem item)
            {
                if (string.IsNullOrEmpty(this._currentFilterString))
                {
                    return true;
                }
                else
                {
                    return item.RemoteFilename.Contains(this._currentFilterString);
                }
            }
            return false;
        }
    }
}

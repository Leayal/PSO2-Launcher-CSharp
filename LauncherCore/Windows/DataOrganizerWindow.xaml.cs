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

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for LauncherThemingManagerWindow.xaml
    /// </summary>
    public partial class DataOrganizerWindow : MetroWindowEx
    {
        public static readonly DependencyProperty CustomizationFileListProperty = DependencyProperty.Register("CustomizationFileList", typeof(IEnumerable), typeof(DataOrganizerWindow));

        public static readonly DataAction[] DataActions = { DataAction.DoNothing, DataAction.Delete, DataAction.Move, DataAction.MoveAndSymlink };

        public static readonly EnumComboBox.ValueDOM<FilterType>[] FilterTypes = System.Linq.Enumerable.ToArray(EnumComboBox.EnumToDictionary<FilterType>().Values);

        public IEnumerable CustomizationFileList
        {
            get => (IEnumerable)this.GetValue(CustomizationFileListProperty);
            set => this.SetValue(CustomizationFileListProperty, value);
        }

        private readonly Classes.ConfigurationFile _config;

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
                this.CustomizationFileList = list;
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
    }
}

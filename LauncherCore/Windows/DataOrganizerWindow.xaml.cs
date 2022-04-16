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
        
        public static readonly DataAction[] DataActions = (StaticResources.IsCurrentProcessAdmin ? new DataAction[] { DataAction.DoNothing, DataAction.Delete, DataAction.Move, DataAction.MoveAndSymlink }
                                                                                                                                        : new DataAction[] { DataAction.DoNothing, DataAction.Delete, DataAction.Move });

        public ICollectionView CustomizationFileList
        {
            get => (ICollectionView)this.GetValue(CustomizationFileListProperty);
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
                    list.Add(new CustomizationFileListItem()
                    {
                        RelativeFilename = item.GetFilenameWithoutAffix(),
                        FileSize = item.FileSize,
                        ClientType = DataOrganizeFilteringBox.ClientType.Both
                    });
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

        public class CustomizationFileListItem : DependencyObject
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

            public string RelativeFilename { get; init; }
            public long FileSize { get; init; }
            public DataOrganizeFilteringBox.ClientType ClientType  { get; init; }

            public DataAction SelectedAction
            {
                get => (DataAction)this.GetValue(DataActionProperty);
                set => this.SetValue(DataActionProperty, value);
            }

            public bool HasActionSettings => (bool)this.GetValue(HasActionSettingsProperty);
        }

        public enum DataAction
        {
            DoNothing,
            Delete,
            Move,
            MoveAndSymlink
        }

        private void DataOrganizeFilteringBox_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}

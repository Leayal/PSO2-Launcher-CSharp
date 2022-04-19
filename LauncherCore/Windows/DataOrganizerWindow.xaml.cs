using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using Leayal.Shared.Windows;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.SharedInterfaces;
using System.Windows.Documents;
using System.Windows.Controls;
using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Core.UIElements;
using System.Windows.Data;
using System.ComponentModel;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using Microsoft.Win32;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for LauncherThemingManagerWindow.xaml
    /// </summary>
    public partial class DataOrganizerWindow : MetroWindowEx
    {
        private static readonly DependencyPropertyKey HasBulkActionSettingsPropertyKey = DependencyProperty.RegisterReadOnly("HasBulkActionSettings", typeof(bool), typeof(DataOrganizerWindow), new PropertyMetadata(false));
        public static readonly DependencyProperty HasBulkActionSettingsProperty = HasBulkActionSettingsPropertyKey.DependencyProperty;
        public static readonly DependencyProperty BulkDataActionProperty = DependencyProperty.Register("BulkDataAction", typeof(DataAction), typeof(DataOrganizerWindow), new PropertyMetadata(DataAction.DoNothing, (obj, e) =>
        {
            if (obj is DataOrganizerWindow window)
            {
                switch ((DataAction)e.NewValue)
                {
                    case DataAction.MoveAndSymlink:
                    case DataAction.Move:
                        window.SetValue(HasBulkActionSettingsPropertyKey, true);
                        break;
                    default:
                        window.SetValue(HasBulkActionSettingsPropertyKey, false);
                        break;
                }
            }
        }));
        public static readonly DependencyProperty CustomizationFileListProperty = DependencyProperty.Register("CustomizationFileList", typeof(ICollectionView), typeof(DataOrganizerWindow));
        
        public static readonly DataAction[] DataActions = (StaticResources.IsCurrentProcessAdmin ? new DataAction[] { DataAction.DoNothing, DataAction.Delete, DataAction.Move, DataAction.MoveAndSymlink }
                                                                                                                                        : new DataAction[] { DataAction.DoNothing, DataAction.Delete, DataAction.Move });

        public ICollectionView CustomizationFileList
        {
            get => (ICollectionView)this.GetValue(CustomizationFileListProperty);
            set => this.SetValue(CustomizationFileListProperty, value);
        }

        public bool HasBulkActionSettings => (bool)this.GetValue(HasBulkActionSettingsProperty);

        public DataAction BulkDataAction
        {
            get => (DataAction)this.GetValue(BulkDataActionProperty);
            set => this.SetValue(BulkDataActionProperty, value);
        }

        private readonly Lazy<SaveFileDialog> _folderBrowserDialog;
        private readonly Classes.ConfigurationFile _config;

        public DataOrganizerWindow(Classes.ConfigurationFile conf) : base()
        {
            this._config = conf;
            this._folderBrowserDialog = new Lazy<SaveFileDialog>();
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

        private void ButtonBulkBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select folder that will contains all the selected files";
                fbd.ShowNewFolderButton = true;
                fbd.UseDescriptionForTitle = true;
                if (fbd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    this.BulkTextBox.Text = fbd.SelectedPath;
                }
            }
        }

        private void ButtonBulkApply_Click(object sender, RoutedEventArgs e)
        {

            bool hasActionSetting = this.HasBulkActionSettings;
            string tbText = hasActionSetting ? this.BulkTextBox.Text : string.Empty;
            if (hasActionSetting && string.IsNullOrEmpty(tbText))
            {
                return;
            }
            var action = this.BulkDataAction;
            var inlines = new List<Inline>(hasActionSetting ? 6 : 4)
            {
                new Run("Are you sure to change the actions of all selected files to this settings?"),
                new LineBreak(),
                new Run("New action settings:"),
                new Run(action.ToString()),
            };
            if (hasActionSetting)
            {
                inlines.Add(new Run("->"));
                inlines.Add(new Run(tbText));
            }
            
            if (Prompt_Generic.Show(this, inlines, "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (var item in this.CustomizationFileList.SourceCollection)
                {
                    if (item is CustomizationFileListItem listitem && listitem.IsChecked)
                    {
                        listitem.SelectedAction = action;
                        if (hasActionSetting)
                        {
                            listitem.TextBoxValue = tbText;
                        }
                    }
                }
            }
        }

        private void ItemSelectionBox_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is DataGridCell cell)
            {
                cell.IsEditing = true;
                // if (cell.Content is CheckBox cb) { }
            }
        }

        private void ItemSelectionBox_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is DataGridCell cell)
            {
                cell.IsEditing = false;
            }
        }

        private void ItemSelectionBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is DataGridCell cell)
            {
                cell.IsEditing = false;
            }
        }

        private void PresetCustomization_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Space)
            {
                var itemList = this.PresetCustomization.SelectedItems;
                if (itemList.Count != 0)
                {
                    e.Handled = true;
                    for (int i = 0; i < itemList.Count; i++)
                    {
                        if (itemList[i] is CustomizationFileListItem item)
                        {
                            item.IsChecked = !item.IsChecked;
                        }
                    }
                }
            }
        }

        private void MenuItemBulkSelectInView_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

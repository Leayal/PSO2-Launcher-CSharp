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
using System.Threading;
using System.Threading.Tasks;
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
        const string PresetDeletePSO2Classic = "deletePSO2Classic";
        const string PresetDeletePSO2Classic_FetchPatchList = PresetDeletePSO2Classic + "-fetchpatchlist";
        private static readonly char[] trimEndPath = { '*', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
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

        private readonly Lazy<SaveFileDialog> _SaveFileDialog;
        private readonly ConfigurationFile _config;
        private readonly PSO2HttpClient pso2HttpClient;
        private readonly CancellationTokenSource _cancelAllOps;
        private readonly Lazy<Task<PatchListMemory>> lazy_PatchListAll;

        private Func<object, Task<bool>>? _ProceedCalback;

        public DataOrganizerWindow(ConfigurationFile conf, PSO2HttpClient pso2HttpClient, in CancellationToken cancellationToken) : base()
        {
            this._cancelAllOps = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            this.pso2HttpClient = pso2HttpClient;
            this._ProceedCalback = null;
            this._config = conf;
            this._SaveFileDialog = new Lazy<SaveFileDialog>(delegate
            {
                return new SaveFileDialog()
                {
                    CheckFileExists = false,
                    AddExtension = false,
                    CheckPathExists = true,
                    CreatePrompt = false,
                    OverwritePrompt = true,
                    ValidateNames = true
                };
            });
            this.lazy_PatchListAll = new Lazy<Task<PatchListMemory>>(this.InitLazy_PatchListAll);
            InitializeComponent();
        }

        private Task<PatchListMemory> InitLazy_PatchListAll() => this.GetPatchListAllAsync(this._cancelAllOps.Token);

        private void ThisSelf_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public async void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            this.tabProgressRing.IsSelected = true;
            try
            {
                var nextAct = Interlocked.Exchange(ref this._ProceedCalback, null);
                if (nextAct != null)
                {
                    if (await nextAct.Invoke(this.tabCustomizePreset.Tag))
                    {
                        this.tabCustomizeAction.IsSelected = true;
                    }
                    else
                    {
                        this.tabCustomizePreset.IsSelected = true;
                    }
                }
                else
                {
                    this.tabCustomizeAction.IsSelected = true;
                }
            }
            catch (Exception ex)
            {
                this.tabPresetSelection.IsSelected = true;
                Prompt_Generic.ShowError(this, ex);
            }
        }

        private async Task<bool> ButtonSelectNoPreset_Proceed(object? obj)
        {
            using (var patchlist = await this.lazy_PatchListAll.Value)
            {
                var list = patchlist.CanCount ? new List<CustomizationFileListItem>(patchlist.Count) : new List<CustomizationFileListItem>();
                foreach (var item in patchlist)
                {
                    list.Add(new CustomizationFileListItem()
                    {
                        RelativeFilename = string.Create(item.GetSpanFilenameWithoutAffix().Length, item, (c, obj) =>
                        {
                            obj.GetSpanFilenameWithoutAffix().CopyTo(c);
                            var iOfSlash = c.IndexOf(Path.AltDirectorySeparatorChar);
                            if (iOfSlash != -1)
                            {
                                for (int i = iOfSlash; i < c.Length; i++)
                                {
                                    if (c[i] == Path.AltDirectorySeparatorChar)
                                    {
                                        c[i] = Path.DirectorySeparatorChar;
                                    }
                                }
                            }
                        }),
                        FileSize = item.FileSize,
                        ClientType = item.IsRebootData switch
                        {
                            true => DataOrganizeFilteringBox.ClientType.NGS,
                            false => DataOrganizeFilteringBox.ClientType.Classic,
                            _ => DataOrganizeFilteringBox.ClientType.Both
                        },
                        SelectedAction = DataAction.DoNothing
                    });
                }
                this.CustomizationFileList = CollectionViewSource.GetDefaultView(list);

                return true;
            }
        }

        private void ButtonSelectNoPreset_Click(object sender, RoutedEventArgs e)
        {
            _ = this.lazy_PatchListAll.Value;
            this.tabCustomizePresetContent.Children.Clear();
            this.tabCustomizePresetContent.RowDefinitions.Clear();
            this.tabCustomizePresetContent.ColumnDefinitions.Clear();
            var text = new TextBlock() { TextWrapping = TextWrapping.Wrap };
            text.Inlines.AddRange(new Inline[]
            {
                new Run("You will be able to customize the action preset on next step and make any changes you desired."),
                new LineBreak(),
                new Run("When you're done customizing or you don't want to make any changes, press 'Start actions' on next step to begin the process."),
                new LineBreak(),
                new Run("Press 'Proceed' button below to continue to next step.")
            });
            this.tabCustomizePresetContent.Children.Add(text);

            this._ProceedCalback = this.ButtonSelectNoPreset_Proceed;

            this.tabCustomizePreset.IsSelected = true;
        }

        private async Task<bool> ButtonSelectDeletePSO2ClassicPreset_Proceed(object? obj)
        {
            using (var patchlist = await this.lazy_PatchListAll.Value)
            {
                var list = patchlist.CanCount ? new List<CustomizationFileListItem>(patchlist.Count) : new List<CustomizationFileListItem>();
                foreach (var item in patchlist)
                {
                    var type = item.IsRebootData switch
                    {
                        true => DataOrganizeFilteringBox.ClientType.NGS,
                        false => DataOrganizeFilteringBox.ClientType.Classic,
                        _ => DataOrganizeFilteringBox.ClientType.Both
                    };
                    list.Add(new CustomizationFileListItem()
                    {
                        RelativeFilename = string.Create(item.GetSpanFilenameWithoutAffix().Length, item, (c, obj) =>
                        {
                            obj.GetSpanFilenameWithoutAffix().CopyTo(c);
                            var iOfSlash = c.IndexOf(Path.AltDirectorySeparatorChar);
                            if (iOfSlash != -1)
                            {
                                for (int i = iOfSlash; i < c.Length; i++)
                                {
                                    if (c[i] == Path.AltDirectorySeparatorChar)
                                    {
                                        c[i] = Path.DirectorySeparatorChar;
                                    }
                                }
                            }
                        }),
                        FileSize = item.FileSize,
                        ClientType = type,
                        SelectedAction = ((type == DataOrganizeFilteringBox.ClientType.Classic) ? DataAction.Delete : DataAction.DoNothing)
                    });
                }
                this.CustomizationFileList = CollectionViewSource.GetDefaultView(list);

                return true;
            }
        }

        private void ButtonSelectDeletePSO2ClassicPreset_Click(object sender, RoutedEventArgs e)
        {
            _ = this.lazy_PatchListAll.Value;
            this.tabCustomizePresetContent.Children.Clear();
            this.tabCustomizePresetContent.RowDefinitions.Clear();
            this.tabCustomizePresetContent.ColumnDefinitions.Clear();
            var text = new TextBlock() { TextWrapping = TextWrapping.Wrap };
            text.Inlines.AddRange(new Inline[]
            {
                new Run("All data files, which are only being used by PSO2 Classic (or PSO2 OG), are set to be deleted on the next step."),
                new LineBreak(),
                new Run("You will be able to customize the action preset on next step and make any changes you desired."),
                new LineBreak(),
                new Run("When you're done customizing or you don't want to make any changes, press 'Start actions' on next step to begin the process."),
                new LineBreak(),
                new Run("Press 'Proceed' button below to continue to next step.")
            });
            this.tabCustomizePresetContent.Children.Add(text);

            this._ProceedCalback = this.ButtonSelectDeletePSO2ClassicPreset_Proceed;

            this.tabCustomizePreset.IsSelected = true;
        }

        private async Task<bool> ButtonSelectMoveClassicCreateSymlinkPreset_Proceed(object? obj)
        {
            if (obj is TextBox tb)
            {
                string dstDir = tb.Text;
                if (string.IsNullOrEmpty(dstDir))
                {
                    Prompt_Generic.Show(this, "The destination directory shouldn't be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                else
                {
                    using (var patchlist = await this.lazy_PatchListAll.Value)
                    {
                        var list = patchlist.CanCount ? new List<CustomizationFileListItem>(patchlist.Count) : new List<CustomizationFileListItem>();
                        foreach (var item in patchlist)
                        {
                            var type = item.IsRebootData switch
                            {
                                true => DataOrganizeFilteringBox.ClientType.NGS,
                                false => DataOrganizeFilteringBox.ClientType.Classic,
                                _ => DataOrganizeFilteringBox.ClientType.Both
                            };
                            var relativeFilename = string.Create(item.GetSpanFilenameWithoutAffix().Length, item, (c, obj) =>
                            {
                                obj.GetSpanFilenameWithoutAffix().CopyTo(c);
                                var iOfSlash = c.IndexOf(Path.AltDirectorySeparatorChar);
                                if (iOfSlash != -1)
                                {
                                    for (int i = iOfSlash; i < c.Length; i++)
                                    {
                                        if (c[i] == Path.AltDirectorySeparatorChar)
                                        {
                                            c[i] = Path.DirectorySeparatorChar;
                                        }
                                    }
                                }
                            });
                            list.Add(new CustomizationFileListItem()
                            {
                                RelativeFilename = relativeFilename,
                                FileSize = item.FileSize,
                                ClientType = type,
                                SelectedAction = ((type == DataOrganizeFilteringBox.ClientType.Classic) ? DataAction.MoveAndSymlink : DataAction.DoNothing),
                                TextBoxValue = ((type == DataOrganizeFilteringBox.ClientType.Classic) ? Path.GetFullPath(relativeFilename, dstDir) : string.Empty)
                            });
                        }
                        this.CustomizationFileList = CollectionViewSource.GetDefaultView(list);
                    }

                    return true;
                }
            }
            else
            {
                Prompt_Generic.Show(this, "The one who wrote this launcher is dum dum at the moment. Please report this and he/she will know what to do right away.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void ButtonSelectMoveClassicCreateSymlinkPreset_Click(object sender, RoutedEventArgs e)
        {
            if (StaticResources.IsCurrentProcessAdmin)
            {
                _ = this.lazy_PatchListAll.Value;
                this.tabCustomizePresetContent.Children.Clear();
                this.tabCustomizePresetContent.RowDefinitions.Clear();
                this.tabCustomizePresetContent.ColumnDefinitions.Clear();

                this.tabCustomizePresetContent.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                this.tabCustomizePresetContent.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                this.tabCustomizePresetContent.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                this.tabCustomizePresetContent.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                this.tabCustomizePresetContent.ColumnDefinitions.Add(new ColumnDefinition());
                this.tabCustomizePresetContent.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

                var text1 = new TextBlock() { Text = "Browse for the destination directory that the classic data files will be moved to:" };
                Grid.SetColumnSpan(text1, 3);
                this.tabCustomizePresetContent.Children.Add(text1);

                var text2 = new TextBlock() { Text = "Destination directory", VerticalAlignment = VerticalAlignment.Center };
                Grid.SetRow(text2, 1);
                this.tabCustomizePresetContent.Children.Add(text2);
                var tb = new TextBox();
                Grid.SetRow(tb, 1);
                Grid.SetColumn(tb, 1);
                this.tabCustomizePresetContent.Children.Add(tb);
                this.tabCustomizePreset.Tag = tb;
                var browse = new Button() { Content = new TextBlock() { Text = "..." }, Tag = tb };
                browse.Click += this.CustomizePresetContentBrowse_Click;
                Grid.SetRow(browse, 1);
                Grid.SetColumn(browse, 2);
                this.tabCustomizePresetContent.Children.Add(browse);

                var text3 = new TextBlock() { TextWrapping = TextWrapping.Wrap };
                Grid.SetRow(text3, 2);
                Grid.SetColumnSpan(text3, 3);
                text3.Inlines.AddRange(new Inline[]
                {
                    new Run("All data files, which are only being used by PSO2 Classic (or PSO2 OG), are set to be moved and replaced with symlink to the specified destination above on the next step."),
                    new LineBreak(),
                    new Run("You will be able to customize the action preset on next step and make any changes you desired."),
                    new LineBreak(),
                    new Run("When you're done customizing or you don't want to make any changes, press 'Start actions' on next step to begin the process."),
                    new LineBreak(),
                    new Run("Press 'Proceed' button below to continue to next step.")
                });
                this.tabCustomizePresetContent.Children.Add(text3);

                this._ProceedCalback = this.ButtonSelectMoveClassicCreateSymlinkPreset_Proceed;

                this.tabCustomizePreset.IsSelected = true;
            }
            else
            {
                Prompt_Generic.Show(this, "Creating Symlink is not possible for non-admin processes. Please rerun the launcher as Administration to use this option.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CustomizePresetContentBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TextBox tb)
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Select destination folder that the files will be moved to";
                    fbd.ShowNewFolderButton = true;
                    fbd.UseDescriptionForTitle = true;
                    if (fbd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    {
                        tb.Text = fbd.SelectedPath;
                    }
                }
            }
        }

        private async Task<PatchListMemory> GetPatchListAllAsync(CancellationToken cancellationToken)
        {
            using (var patchroot = await this.pso2HttpClient.GetPatchRootInfoAsync(cancellationToken).ConfigureAwait(false))
            {
                return await this.pso2HttpClient.GetPatchListAllAsync(patchroot, cancellationToken).ConfigureAwait(false);
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this._cancelAllOps.Cancel();
        }

        private async void ButtonStartActions_Click(object sender, RoutedEventArgs e)
        {
            this.tabProgressRing.IsSelected = true;
            try
            {
                if (this.CustomizationFileList?.SourceCollection is List<CustomizationFileListItem> list)
                {
                    this.tabActionProgress.IsSelected = true;
                    var result = await Task.Factory.StartNew(this.StartAction, list, TaskCreationOptions.LongRunning);
                    if (result)
                    {
                        Prompt_Generic.Show(this, "Everything is done nicely.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.CustomDialogResult = true;
                        this.DialogResult = true;
                    }
                    else if (!this._cancelAllOps.IsCancellationRequested)
                    {
                        Prompt_Generic.Show(this, "Something went wrong in the progress.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }  
            }
            catch (Exception ex)
            {
                Prompt_Generic.ShowError(this, ex);
            }
        }

        private void SetProgressBarMax(double value)
        {
            this.ActionProgress_Value.ProgressBar.Value = 0;
            this.ActionProgress_Value.ProgressBar.Maximum = value;
        }

        private void SetProgressBarComplete()
        {
            this.ActionProgress_Value.ProgressBar.Value = this.ActionProgress_Value.ProgressBar.Maximum;
            this.ActionProgress_Value.Text = "Task completed";
        }

        private readonly struct ActionProgressReport
        {
            private readonly double Value;
            private readonly string Text;
            private readonly ExtendedProgressBar _progressbar;

            public ActionProgressReport(in double value, string text, ExtendedProgressBar progressbar)
            {
                this.Value = value;
                this.Text = text;
                this._progressbar = progressbar;
            }

            public void Invoke()
            {
                _progressbar.Text = this.Text;
                _progressbar.ProgressBar.Value = this.Value;
            }
        }

        private bool StartAction(object? obj)
        {
            var pso2dir = this._config.PSO2_BIN;
            var dispatcher = this.Dispatcher;
            using (var debouncer = new DebounceDispatcher(dispatcher))
            {
                if (obj is List<CustomizationFileListItem> list && list.Count != 0)
                {
                    double value = 0;
                    dispatcher.Invoke(this.SetProgressBarMax, Convert.ToDouble(list.Count));
                    foreach (var item in list)
                    {
                        value++;
                        if (this._cancelAllOps.IsCancellationRequested)
                        {
                            return false;
                        }
                        var action = item.SelectedAction;
                        if (action == DataAction.Delete)
                        {
                            var a = new ActionProgressReport(value, $"Deleting '{item.RelativeFilename}'", this.ActionProgress_Value);
                            debouncer.ThrottleEx(30, a.Invoke);
                            var delPath = Path.GetFullPath(item.RelativeFilename, pso2dir);
                            if (File.Exists(delPath))
                            {
                                File.Delete(delPath);
                            }
                        }
                        else if (action == DataAction.Move || action == DataAction.MoveAndSymlink)
                        {
                            var a = new ActionProgressReport(value, (action == DataAction.MoveAndSymlink) ? $"Move & Symlink '{item.RelativeFilename}'" : $"Moving '{item.RelativeFilename}'", this.ActionProgress_Value);
                            debouncer.ThrottleEx(30, a.Invoke);
                            string srcMove = Path.GetFullPath(item.RelativeFilename, pso2dir),
                                    dstMove = Path.GetFullPath(item.TextBoxValue);
                            Directory.CreateDirectory(Path.GetDirectoryName(dstMove));
                            var symlinkInfo = File.ResolveLinkTarget(srcMove, true);
                            if (symlinkInfo == null)
                            {
                                File.Move(srcMove, dstMove, true);
                                if (action == DataAction.MoveAndSymlink)
                                {
                                    File.CreateSymbolicLink(srcMove, dstMove);
                                }
                            }
                            else
                            {
                                var realsrcMove = symlinkInfo.FullName;
                                if (!string.Equals(realsrcMove, dstMove, StringComparison.OrdinalIgnoreCase))
                                {
                                    File.Move(realsrcMove, dstMove, true);
                                    if (action == DataAction.MoveAndSymlink)
                                    {
                                        File.Delete(srcMove);
                                        File.CreateSymbolicLink(srcMove, dstMove);
                                    }
                                }
                            }
                        }
                    }
                    dispatcher.InvokeAsync(this.SetProgressBarComplete);
                    return true;
                }
                else
                {
                    return false;
                }
            }
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
            var spantbText = tbText.AsSpan();
            bool keptDirectoryStructure;
            string pathDst;
            if (spantbText.Length > 2 && spantbText.EndsWith("**", StringComparison.OrdinalIgnoreCase) && (spantbText[spantbText.Length - 3] == Path.DirectorySeparatorChar || spantbText[spantbText.Length - 3] == Path.AltDirectorySeparatorChar))
            {
                keptDirectoryStructure = true;
                pathDst = Path.GetFullPath(Path.TrimEndingDirectorySeparator(tbText.Substring(0, tbText.Length - 3)));
            }
            else if (spantbText.Length > 1 && spantbText[spantbText.Length - 1] == '*' && (spantbText[spantbText.Length - 2] == Path.DirectorySeparatorChar || spantbText[spantbText.Length - 2] == Path.AltDirectorySeparatorChar))
            {
                keptDirectoryStructure = false;
                pathDst = Path.GetFullPath(Path.TrimEndingDirectorySeparator(tbText.Substring(0, tbText.Length - 2)));
            }
            else
            {
                keptDirectoryStructure = true;
                pathDst = Path.GetFullPath(tbText.TrimEnd(trimEndPath));
            }
            var action = this.BulkDataAction;
            var inlines = new List<Inline>(hasActionSetting ? 14 : 4)
            {
                new Run("Are you sure to change the actions of all selected files to this settings?"),
                new LineBreak(),
                new Run("New action settings: "),
                new Run(action.ToString()),
            };
            if (hasActionSetting)
            {
                if (keptDirectoryStructure)
                {
                    inlines.Add(new Run(" -> " + tbText + " (Maintain directory structures)"));
                }
                else
                {
                    inlines.Add(new Run(" -> " + tbText));
                }
                inlines.Add(new LineBreak());
                inlines.Add(new Run("Explanation: "));
                if (keptDirectoryStructure)
                {
                    inlines.Add(new Run($"'Maintain directory structures' means all relative paths, which includes directory/folder, will be maintained when moving to '{pathDst}'."));

                    inlines.Add(new LineBreak());
                    inlines.Add(new Run("For example:"));
                    inlines.Add(new LineBreak());
                    inlines.Add(new Run("- File without folder/directory: 'pso2.exe' -> 'pso2.exe'"));
                    inlines.Add(new LineBreak());
                    inlines.Add(new Run($"- Filename with folder(s): 'data/win32/000a686a27ade4d971ac5e27a664a5a3' -> '{pathDst}\\data\\win32\\000a686a27ade4d971ac5e27a664a5a3'"));
                }
                else
                {
                    inlines.Add(new Run($"All files' path will be flattened and moved, which may result in confliction(s) of files with same name in different folders, to '{pathDst}'."));

                    inlines.Add(new LineBreak());
                    inlines.Add(new Run("For example:"));
                    inlines.Add(new LineBreak());
                    inlines.Add(new Run("- File without folder/directory: 'pso2.exe' -> 'pso2.exe'"));
                    inlines.Add(new LineBreak());
                    inlines.Add(new Run($"- Filename with folder(s): 'data/win32/000a686a27ade4d971ac5e27a664a5a3' -> '{pathDst}\\000a686a27ade4d971ac5e27a664a5a3'"));
                }
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
                            listitem.TextBoxValue = Path.GetFullPath(keptDirectoryStructure ? listitem.RelativeFilename : Path.GetFileName(listitem.RelativeFilename), pathDst);
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

        private void BuilkSelectAll(IEnumerable list)
        {
            if (list != null)
            {
                foreach (var obj in list)
                {
                    if (obj is CustomizationFileListItem item)
                    {
                        item.IsChecked = true;
                    }
                }
            }
        }

        private void BuilkDeselectAll(IEnumerable list)
        {
            if (list != null)
            {
                foreach (var obj in list)
                {
                    if (obj is CustomizationFileListItem item)
                    {
                        item.IsChecked = false;
                    }
                }
            }
        }

        private void MenuItemBulkSelectInView_Click(object sender, RoutedEventArgs e)
            => this.BuilkSelectAll(this.CustomizationFileList);

        private void MenuItemBulkDeselectInView_Click(object sender, RoutedEventArgs e)
            => this.BuilkDeselectAll(this.CustomizationFileList);

        private void MenuItemBulkSelectAll_Click(object sender, RoutedEventArgs e)
        => this.BuilkSelectAll(this.CustomizationFileList?.SourceCollection);

        private void MenuItemBulkDeselectAll_Click(object sender, RoutedEventArgs e)
            => this.BuilkDeselectAll(this.CustomizationFileList?.SourceCollection);

        private void ButtonBulkSelect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var ctm = btn.ContextMenu;
                if (ctm != null)
                {
                    ctm.PlacementTarget = btn;
                    ctm.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                    ctm.IsOpen = true;
                }
            }
        }

        private void ButtonItemBrowseLocation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CustomizationFileListItem item)
            {
                var fsd = this._SaveFileDialog.Value;
                fsd.Title = $"Specify location to put file '{item.RelativeFilename}' to";
                fsd.FileName = Path.GetFileName(item.RelativeFilename);
                if (fsd.ShowDialog(this) == true)
                {
                    item.TextBoxValue = fsd.FileName;
                }
            }
        }
    }
}

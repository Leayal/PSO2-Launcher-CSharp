using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Leayal.Shared.Windows;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.ComponentModel;
using System.Windows.Data;
using Leayal.PSO2Launcher.Core.UIElements;
using Leayal.Shared;
using System.Text.Json;
using System.Windows.Documents;
using System.Text.Encodings.Web;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for UpdateResultLogDialog.xaml
    /// </summary>
    public partial class GameClientUpdateResultLogDialog : MetroWindowEx
    {
        private const string Text_StatusCancelled = "Operation cancelled", Text_StatusCompleted = "Operation completed";
        private readonly string _pso2dir;
        private readonly DateTime updateCompleteTime;

        public Guid ResultGuid { get; }

        public GameClientUpdateResultLogDialog(string pso2dir, in Guid id, in bool cancel, in int patchlist_count, IReadOnlyDictionary<PatchListItemLogData, bool?> datalist, in DateTime updateCompleteTime) : base()
        {
            this.updateCompleteTime = updateCompleteTime;
            this._pso2dir = pso2dir;
            this.ResultGuid = id;
            InitializeComponent();
            this.WindowCloseIsDefaultedCancel = true;

            Collection<PatchListItemLogData> obCollection_success = new(), obCollection_failure = new(), obCollection_cancelled = new();
            foreach (var data in datalist)
            {
                var val = data.Value;
                if (val.HasValue)
                {
                    if (val.Value)
                    {
                        obCollection_success.Add(data.Key);
                    }
                    else
                    {
                        obCollection_failure.Add(data.Key);
                    }
                }
                else
                {
                    obCollection_cancelled.Add(data.Key);
                }
            }
            int count_success = obCollection_success.Count, count_failure = obCollection_failure.Count, count_cancelled = obCollection_cancelled.Count;
            if (count_success == 0)
            {
                this.ListOfSuccessItems.ItemsSource = null;
            }
            else
            {
                this.ListOfSuccessItems.ItemsSource = CollectionViewSource.GetDefaultView(obCollection_success);
                this.CreateCM(this.ListOfSuccessItems);
            }
            if (count_failure == 0)
            {
                this.ListOfFailureItems.ItemsSource = null;
            }
            else
            {
                this.ListOfFailureItems.ItemsSource = CollectionViewSource.GetDefaultView(obCollection_failure);
                this.CreateCM(this.ListOfFailureItems);
            }
            if (count_cancelled == 0)
            {
                this.ListOfCancelledItems.ItemsSource = null;
            }
            else
            {
                this.ListOfCancelledItems.ItemsSource = CollectionViewSource.GetDefaultView(obCollection_cancelled);
                this.CreateCM(this.ListOfCancelledItems);
            }

            this.OverviewPanel.DataContext = new Eyy()
            {
                Status = cancel ? Text_StatusCancelled : Text_StatusCompleted,
                FileCount = patchlist_count,
                CountScanned = datalist.Count,
                CountSuccess = count_success,
                CountFailure = count_failure,
                CountCancelled = count_cancelled
            };
        }

        private void CreateCM(ListBox ctrl)
        {
            ctrl.ContextMenuOpening += Ctrl_ContextMenuOpening;
            var result = new ContextMenu();

            var item = new MenuItem() { Header = new TextBlock() { Text = "Copy relative path" }, Tag = ctrl };
            item.Click += this.ItemCopyRelativePath_Click;
            result.Items.Add(item);

            item = new MenuItem() { Header = new TextBlock() { Text = "Copy full path" }, Tag = ctrl };
            item.Click += this.ItemCopyFullPath_Click;
            result.Items.Add(item);

            result.Items.Add(new Separator());

            item = new MenuItem() { Header = new TextBlock() { Text = "Copy file" }, Tag = ctrl };
            item.Click += this.ItemFileCopy_Click;
            result.Items.Add(item);

            item = new MenuItem() { Header = new TextBlock() { Text = "Cut file" }, Tag = ctrl };
            item.Click += this.ItemFileCut_Click;
            result.Items.Add(item);

            result.Items.Add(new Separator());

            item = new MenuItem() { Header = new TextBlock() { Text = "Show file in file explorer" }, Tag = ctrl };
            item.Click += this.ItemShowItem_Click;
            result.Items.Add(item);

            ctrl.ContextMenu = result;
        }

        private void ItemCopyRelativePath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menu && menu.Tag is ListBox listbox)
            {
                var items = listbox.SelectedItems;
                switch (items.Count)
                {
                    case 0:
                        return;
                    case 1:
                        if (items[0] is PatchListItemLogData item)
                        {
                            Clipboard.Clear();
                            Clipboard.SetText(item.Name, TextDataFormat.UnicodeText);
                            Clipboard.Flush();
                        }
                        return;
                    default:
                        var sb = new StringBuilder();
                        bool first = true;
                        foreach (var _item in items)
                        {
                            if (_item is PatchListItemLogData __item)
                            {
                                if (first)
                                {
                                    first = false;
                                }
                                else
                                {
                                    sb.AppendLine();
                                }
                                sb.Append(__item.Name);
                            }
                        }
                        Clipboard.Clear();
                        Clipboard.SetText(sb.ToString(), TextDataFormat.UnicodeText);
                        Clipboard.Flush();
                        return;
                }
            }
        }

        private void ItemCopyFullPath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menu && menu.Tag is ListBox listbox)
            {
                var items = listbox.SelectedItems;
                switch (items.Count)
                {
                    case 0:
                        return;
                    case 1:
                        if (items[0] is PatchListItemLogData item)
                        {
                            Clipboard.Clear();
                            Clipboard.SetText(Path.GetFullPath(item.Name, this._pso2dir), TextDataFormat.UnicodeText);
                            Clipboard.Flush();
                        }
                        return;
                    default:
                        var sb = new StringBuilder();
                        bool first = true;
                        foreach (var _item in items)
                        {
                            if (_item is PatchListItemLogData __item)
                            {
                                if (first)
                                {
                                    first = false;
                                }
                                else
                                {
                                    sb.AppendLine();
                                }
                                sb.Append(Path.GetFullPath(__item.Name, this._pso2dir));
                            }
                        }
                        Clipboard.Clear();
                        Clipboard.SetText(sb.ToString(), TextDataFormat.UnicodeText);
                        Clipboard.Flush();
                        return;
                }
            }
        }

        private void ItemShowItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menu && menu.Tag is ListBox listbox)
            {
                var items = listbox.SelectedItems;
                switch (items.Count)
                {
                    case 0:
                        return;
                    default:
                        if (items[0] is PatchListItemLogData item)
                        {
                            var fullpath = Path.GetFullPath(item.Name, this._pso2dir);
                            if (FileSystem.PathExists(fullpath))
                            {
                                WindowsExplorerHelper.SelectPathInExplorer(fullpath);
                            }
                        }
                        return;
                }
            }
        }

        private void ItemFileCopy_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menu && menu.Tag is ListBox listbox)
            {
                var items = listbox.SelectedItems;
                var count = items.Count;
                switch (count)
                {
                    case 0:
                        return;
                    case 1:
                        if (items[0] is PatchListItemLogData item)
                        {
                            var path = Path.GetFullPath(item.Name, this._pso2dir);
                            if (File.Exists(path))
                            {
                                Clipboard.Clear();
                                ClipboardHelper.PutFilesToClipboard(new System.Collections.Specialized.StringCollection()
                                {
                                    path
                                }, false);
                            }
                            else
                            {
                                Prompt_Generic.Show(this, "The file is no longer existed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        return;
                    default:
                        var list = new System.Collections.Specialized.StringCollection();
                        bool warnMissingFiles = false;
                        foreach (var _item in items)
                        {
                            if (_item is PatchListItemLogData __item)
                            {
                                list.Add(Path.GetFullPath(__item.Name, this._pso2dir));
                            }
                            else if (!warnMissingFiles)
                            {
                                warnMissingFiles = true;
                            }
                        }
                        Clipboard.Clear();
                        ClipboardHelper.PutFilesToClipboard(list, false);

                        if (warnMissingFiles)
                        {
                            Prompt_Generic.Show(this, "There are some file which are no longer existed. Ignored cutting those missing files.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                }
            }
        }

        private void ItemFileCut_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menu && menu.Tag is ListBox listbox)
            {
                var items = listbox.SelectedItems;
                var count = items.Count;
                switch (count)
                {
                    case 0:
                        return;
                    case 1:
                        if (items[0] is PatchListItemLogData item)
                        {
                            var path = Path.GetFullPath(item.Name, this._pso2dir);
                            if (File.Exists(path))
                            {
                                Clipboard.Clear();
                                ClipboardHelper.PutFilesToClipboard(new System.Collections.Specialized.StringCollection()
                                {
                                    path
                                }, true);
                            }
                            else
                            {
                                Prompt_Generic.Show(this, "The file is no longer existed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        return;
                    default:
                        var list = new System.Collections.Specialized.StringCollection();
                        bool warnMissingFiles = false;
                        foreach (var _item in items)
                        {
                            if (_item is PatchListItemLogData __item)
                            {
                                var path = Path.GetFullPath(__item.Name, this._pso2dir);
                                if (File.Exists(path))
                                {
                                    list.Add(path);
                                }
                                else if (!warnMissingFiles)
                                {
                                    warnMissingFiles = true;
                                }
                            }
                        }
                        Clipboard.Clear();
                        ClipboardHelper.PutFilesToClipboard(list, true);

                        if (warnMissingFiles)
                        {
                            Prompt_Generic.Show(this, "There are some file which are no longer existed. Ignored cutting those missing files.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        return;
                }
            }
        }

        private static void Ctrl_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is ListBox listbox)
            {
                var menu = listbox.ContextMenu;
                if (menu == null) return;
                if (listbox.SelectedItem is PatchListItemLogData item)
                {
                    menu.Tag = item;
                }
                else
                {
                    e.Handled = true;
                }
            }
        }

        class Eyy
        {
            public string Status { get; init; }
            public int FileCount { get; init; }
            public int CountScanned { get; init; }
            public int CountSuccess { get; init; }
            public int CountFailure { get; init; }
            public int CountCancelled { get; init; }

            public Eyy()
            {
                // Was to workaround the compiler's warning about can't leave non-nullable string type to be null.
                this.Status = string.Empty;
            }
        }

        private void MetroAnimatedTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => ConvenientEventHandlers.TabControl_SelectionChanged_PreventSelectingNothing(sender, e);

        private static readonly string CharLookout = "?#*[]";
        private void TextBoxDelayedTextChange_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBoxDelayedTextChange textbox && textbox.Tag is ICollectionView view)
            {
                var filterPattern = textbox.Text;
                var filterPatternSpan = filterPattern.AsSpan();
                if (filterPatternSpan.IsEmpty)
                {
                    view.Filter = null;
                }
                else if (filterPatternSpan.IndexOfAny(CharLookout.AsSpan()) != -1)
                {
                    view.Filter = StringHelper.MakePredicate_MatchByPattern<PatchListItemLogData>(filterPattern, false);
                }
                else
                {
                    view.Filter = StringHelper.MakePredicate_ContainsLiteral<PatchListItemLogData>(filterPattern.AsMemory(), false);
                }
            }
        }

        private void ButtonExportToJSON_Click(object sender, RoutedEventArgs e)
        {
            using (var sfd = new System.Windows.Forms.SaveFileDialog()
            {
                AddExtension = true,
                CheckFileExists = false,
                CheckPathExists = true,
                AutoUpgradeEnabled = true,
                CreatePrompt = false,
                FileName = "PSO2UpdateResult_" + this.updateCompleteTime.ToString("yyyy-MM-dd_hh-mm-ss"),
                Title = "Select export destination",
                DereferenceLinks = true,
                Filter = "JavaScript Object Notation With Comment|*.jsonc|JavaScript Object Notation|*.json",
                OverwritePrompt = true,
            })
            {
                if (sfd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    var dst = sfd.FileName;
                    bool allowComment = sfd.FilterIndex == 1; // dst.EndsWith(".jsonc");
                    using (var fs = File.Create(dst))
                    using (var jsWriter = new Utf8JsonWriter(fs, new JsonWriterOptions() { Indented = true  }))
                    {
                        jsWriter.WriteStartObject();
                        if (allowComment) jsWriter.WriteCommentValue("The FileTime (NOT system time) expressed in UTC, indicates when the update has been done regardless success or not");
                        jsWriter.WriteNumber("completeAt", this.updateCompleteTime.ToFileTimeUtc());
                        if (allowComment) jsWriter.WriteCommentValue("The path to the folder which contained PSO2 game client when this update happened");
                        jsWriter.WriteString("path_pso2bin", this._pso2dir);

                        static void WriteArray(Utf8JsonWriter jsWriter, ListBox listbox, string propertyName, string comment)
                        {
                            if (listbox.ItemsSource is ICollectionView view && view.SourceCollection is Collection<PatchListItemLogData> collection)
                            {
                                if (comment.Length != 0) jsWriter.WriteCommentValue(comment);
                                jsWriter.WritePropertyName(propertyName);
                                jsWriter.WriteStartArray();
                                foreach (var item in collection)
                                    jsWriter.WriteStringValue(item.Name);
                                jsWriter.WriteEndArray();
                            }
                        }
                        WriteArray(jsWriter, this.ListOfSuccessItems, "success", allowComment ? "List of successfully downloaded items" : string.Empty);
                        WriteArray(jsWriter, this.ListOfFailureItems, "failed", allowComment ? "List of failed downloaded items" : string.Empty);
                        WriteArray(jsWriter, this.ListOfCancelledItems, "cancelled", allowComment ? "List of items which hasn't even begin downloading" : string.Empty);
                        jsWriter.WriteEndObject();
                        jsWriter.Flush();
                    }
                    Prompt_Generic.Show(this, new Inline[]
                    { 
                        new Run("The result has been exported successfully."),
                        new LineBreak(),
                        new ShowLocalFileHyperlink(new Run("(Show the exported file in File Explorer)")) { NavigateUri = new Uri(dst) }
                    }, "Export result", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        public class PatchListItemLogData : IEquatable<PatchListItemLogData>, StringHelper.IStringComparable
        {
            public string Size { get; }
            public string Name { get; }

            public PatchListItemLogData(string name, long size)
            {
                this.Size = Shared.NumericHelper.ToHumanReadableFileSize(in size);
                this.Name = name;
            }

            public bool Equals(PatchListItemLogData? other) => (other != null && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase));

            public override bool Equals(object? obj) => (obj is PatchListItemLogData item && this.Equals(item));

            public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(this.Name);

            public ReadOnlyMemory<char> GetComparableStringRegion() => this.Name.AsMemory();
        }
    }
}

using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for UpdateResultLogDialog.xaml
    /// </summary>
    public partial class GameClientUpdateResultLogDialog : MetroWindowEx
    {
        private const string Text_NoSuccess = "(No successfully downloaded files in the list)",
            Text_NoFailures = "(No download failures in the list)",
            Text_NoCancelled = "(No download cancellation in the list)",
            Text_StatusCancelled = "Operation cancelled",
            Text_StatusCompleted = "Operation completed";

        public Guid ResultGuid { get; }

        public GameClientUpdateResultLogDialog(in Guid id, in bool cancel, in int patchlist_count, IReadOnlyDictionary<PatchListItemLogData, bool?> datalist) : base()
        {
            this.ResultGuid = id;
            InitializeComponent();
            this.WindowCloseIsDefaultedCancel = true;

            RowDefinitionCollection row_success = this.ListOfSuccessItems.RowDefinitions,
                row_failure = this.ListOfFailureItems.RowDefinitions,
                row_cancel = this.ListOfCancelledItems.RowDefinitions;
            UIElementCollection items_success = this.ListOfSuccessItems.Children,
                items_failure = this.ListOfFailureItems.Children,
                items_cancel = this.ListOfCancelledItems.Children;
            int index_success = 0, index_failure = 0, index_cancelled = 0;

            foreach (var data in datalist)
            {
                var item = data.Key;
                if (data.Value.HasValue)
                {
                    if (data.Value.Value)
                    {
                        row_success.Add(new RowDefinition() { Height = GridLength.Auto });
                        var tbox = new TextBox() { BorderThickness = new Thickness(0), BorderBrush = null, Text = item.Name, IsReadOnly = true, IsReadOnlyCaretVisible = true, IsUndoEnabled = false, IsInactiveSelectionHighlightEnabled = false, TextAlignment = TextAlignment.Left, HorizontalAlignment = HorizontalAlignment.Left };
                        Grid.SetRow(tbox, index_success);
                        items_success.Add(tbox);
                        var tblock = new TextBlock() { Text = Shared.NumericHelper.ToHumanReadableFileSize(in item.Size), TextAlignment = TextAlignment.Right, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
                        Grid.SetRow(tblock, index_success);
                        Grid.SetColumn(tblock, 1);
                        items_success.Add(tblock);
                        index_success++;
                    }
                    else
                    {
                        row_failure.Add(new RowDefinition() { Height = GridLength.Auto });
                        var tbox = new TextBox() { BorderThickness = new Thickness(0), BorderBrush = null, Text = item.Name, IsReadOnly = true, IsReadOnlyCaretVisible = true, IsUndoEnabled = false, IsInactiveSelectionHighlightEnabled = false, TextAlignment = TextAlignment.Left, HorizontalAlignment = HorizontalAlignment.Left };
                        Grid.SetRow(tbox, index_failure);
                        items_failure.Add(tbox);
                        var tblock = new TextBlock() { Text = Shared.NumericHelper.ToHumanReadableFileSize(in item.Size), TextAlignment = TextAlignment.Right, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
                        Grid.SetRow(tblock, index_failure);
                        Grid.SetColumn(tblock, 1);
                        items_failure.Add(tblock);
                        index_failure++;
                    }
                }
                else
                {
                    row_cancel.Add(new RowDefinition() { Height = GridLength.Auto });
                    var tbox = new TextBox() { BorderThickness = new Thickness(0), BorderBrush = null, Text = item.Name, IsReadOnly = true, IsReadOnlyCaretVisible = true, IsUndoEnabled = false, IsInactiveSelectionHighlightEnabled = false, TextAlignment = TextAlignment.Left, HorizontalAlignment = HorizontalAlignment.Left };
                    Grid.SetRow(tbox, index_cancelled);
                    items_cancel.Add(tbox);
                    var tblock = new TextBlock() { Text = Shared.NumericHelper.ToHumanReadableFileSize(in item.Size), TextAlignment = TextAlignment.Right, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
                    Grid.SetRow(tblock, index_cancelled);
                    Grid.SetColumn(tblock, 1);
                    items_cancel.Add(tblock);
                    index_cancelled++;
                }
            }
            if (index_success == 0)
            {
                row_success.Add(new RowDefinition());
                var tblock = new TextBlock() { Text = Text_NoSuccess, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumnSpan(tblock, 2);
                items_success.Add(tblock);
            }
            if (index_failure == 0)
            {
                row_failure.Add(new RowDefinition());
                var tblock = new TextBlock() { Text = Text_NoFailures, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumnSpan(tblock, 2);
                items_failure.Add(tblock);
            }
            if (index_cancelled == 0)
            {
                row_cancel.Add(new RowDefinition());
                var tblock = new TextBlock() { Text = Text_NoCancelled, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumnSpan(tblock, 2);
                items_cancel.Add(tblock);
            }

            this.OverviewPanel.DataContext = new Eyy()
            {
                Status = cancel ? Text_StatusCompleted : Text_StatusCancelled,
                FileCount = patchlist_count,
                CountScanned = datalist.Count,
                CountSuccess = index_success,
                CountFailure = index_failure,
                CountCancelled = index_cancelled
            };
        }

        class Eyy
        {
            public string Status { get; init; }
            public int FileCount { get; init; }
            public int CountScanned { get; init; }
            public int CountSuccess { get; init; }
            public int CountFailure { get; init; }
            public int CountCancelled { get; init; }
        }

        private void MetroAnimatedTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems == null || e.AddedItems.Count == 0)
            {
                if (e.RemovedItems[0] is MetroTabItem tab)
                {
                    e.Handled = true;
                    tab.IsSelected = true;
                }
            }
        }

        public readonly struct PatchListItemLogData
        {
            public readonly long Size;
            public readonly string Name;

            public PatchListItemLogData(string name, long size)
            {
                this.Size = size;
                this.Name = name;
            }
        }
    }
}

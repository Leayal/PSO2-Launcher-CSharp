using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Leayal.Shared.Windows;
using System.Collections.ObjectModel;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for UpdateResultLogDialog.xaml
    /// </summary>
    public partial class GameClientUpdateResultLogDialog : MetroWindowEx
    {
        private const string Text_StatusCancelled = "Operation cancelled", Text_StatusCompleted = "Operation completed";

        public Guid ResultGuid { get; }

        public GameClientUpdateResultLogDialog(in Guid id, in bool cancel, in int patchlist_count, IReadOnlyDictionary<PatchListItemLogData, bool?> datalist) : base()
        {
            this.ResultGuid = id;
            InitializeComponent();
            this.WindowCloseIsDefaultedCancel = true;

            Collection<PatchListItemLogData> obCollection_success = new(), obCollection_failure = new(), obCollection_cancelled = new();

            int index_success = 0, index_failure = 0, index_cancelled = 0;

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
            if (obCollection_success.Count == 0)
            {
                this.ListOfSuccessItems.ItemsSource = null;
            }
            else
            {
                this.ListOfSuccessItems.ItemsSource = obCollection_success;
            }
            if (obCollection_failure.Count == 0)
            {
                this.ListOfFailureItems.ItemsSource = null;
            }
            else
            {
                this.ListOfFailureItems.ItemsSource = obCollection_failure;
            }
            if (obCollection_cancelled.Count == 0)
            {
                this.ListOfCancelledItems.ItemsSource = null;
            }
            else
            {
                this.ListOfCancelledItems.ItemsSource = obCollection_cancelled;
            }

            this.OverviewPanel.DataContext = new Eyy()
            {
                Status = cancel ? Text_StatusCancelled : Text_StatusCompleted,
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

        public class PatchListItemLogData
        {
            public string Size { get; }
            public string Name { get; }

            public PatchListItemLogData(string name, long size)
            {
                this.Size = Leayal.Shared.NumericHelper.ToHumanReadableFileSize(in size);
                this.Name = name;
            }
        }
    }
}

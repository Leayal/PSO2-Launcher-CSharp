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

        public Guid ResultGuid { get; }

        public GameClientUpdateResultLogDialog(in Guid id, IEnumerable<PatchListItemLogData> success, IEnumerable<PatchListItemLogData> failures) : base()
        {
            this.ResultGuid = id;
            InitializeComponent();

            this.WindowCloseIsDefaultedCancel = true;

            var row = this.ListOfSuccess.RowDefinitions;
            var children = this.ListOfSuccess.Children;
            row.Clear();
            int index = 0;
            foreach (var item in success)
            {
                row.Add(new RowDefinition() { Height = GridLength.Auto });
                var tbox = new TextBox() { BorderThickness = new Thickness(0), BorderBrush = null, Text = item.Name, IsReadOnly = true, IsReadOnlyCaretVisible = true, IsUndoEnabled = false, IsInactiveSelectionHighlightEnabled = false, TextAlignment = TextAlignment.Left, HorizontalAlignment = HorizontalAlignment.Left };
                Grid.SetRow(tbox, index);
                children.Add(tbox);
                var tblock = new TextBlock() { Text = Shared.NumericHelper.ToHumanReadableFileSize(in item.Size), TextAlignment = TextAlignment.Right, HorizontalAlignment = HorizontalAlignment.Right };
                Grid.SetRow(tblock, index);
                Grid.SetColumn(tblock, 1);
                children.Add(tblock);
                index++;
            }

            row = this.ListOfFailure.RowDefinitions;
            children = this.ListOfFailure.Children;
            row.Clear();
            index = 0;
            foreach (var item in failures)
            {
                row.Add(new RowDefinition() { Height = GridLength.Auto });
                var tbox = new TextBox() { BorderThickness = new Thickness(0), BorderBrush = null, Text = item.Name, IsReadOnly = true, IsReadOnlyCaretVisible = true, IsUndoEnabled = false, IsInactiveSelectionHighlightEnabled = false, TextAlignment = TextAlignment.Left, HorizontalAlignment = HorizontalAlignment.Left };
                Grid.SetRow(tbox, index);
                children.Add(tbox);
                var tblock = new TextBlock() { Text = Shared.NumericHelper.ToHumanReadableFileSize(in item.Size), TextAlignment = TextAlignment.Right, HorizontalAlignment = HorizontalAlignment.Right };
                Grid.SetRow(tblock, index);
                Grid.SetColumn(tblock, 1);
                children.Add(tblock);
                index++;
            }
        }
    }
}

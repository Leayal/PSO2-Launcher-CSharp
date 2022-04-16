using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using CustomizationFileListItem = Leayal.PSO2Launcher.Core.Windows.DataOrganizerWindow.CustomizationFileListItem;
namespace Leayal.PSO2Launcher.Core.UIElements
{
    /// <summary>
    /// Interaction logic for DataOrganizeFilteringBox.xaml
    /// </summary>
    public partial class DataOrganizeFilteringBox : UserControl
    {
        public static readonly ClientType[] ClientTypes = Enum.GetValues<ClientType>();
        public static readonly SizeComparisonType[] SizeComparisonTypes = Enum.GetValues<SizeComparisonType>();
        public static readonly SizeComparisonScale[] SizeComparisonScales = Enum.GetValues<SizeComparisonScale>();

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(ICollectionView), typeof(DataOrganizeFilteringBox), new PropertyMetadata(null, (obj, e) =>
        {
            if (obj is DataOrganizeFilteringBox box)
            {
                box._currentView = (ICollectionView)e.NewValue;
                if (e.OldValue is ICollectionView view && view.CanFilter)
                {
                    view.Filter = null;
                }
                box.ReFiltering();
            }
        }));

        private static readonly DependencyPropertyKey FilterDisplayStringPropertyKey = DependencyProperty.RegisterReadOnly("FilterDisplayString", typeof(string), typeof(DataOrganizeFilteringBox), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty FilterDisplayStringProperty = FilterDisplayStringPropertyKey.DependencyProperty;

        private ClientType _currentClientType;
        private string _currentName, _currentFileSizeRaw;
        private long _currentFileSize;
        private SizeComparisonType _currentSizeComparisonType;
        private SizeComparisonScale _currentSizeComparisonScale;
        private ICollectionView _currentView;
        private bool hasFilter_name, hasFilter_clientType, hasFilter_fileSize;

        public ICollectionView ItemsSource
        {
            get => (ICollectionView)this.GetValue(ItemsSourceProperty);
            set => this.SetValue(ItemsSourceProperty, value);
        }

        public string FilterDisplayString => (string)this.GetValue(FilterDisplayStringProperty);

        public DataOrganizeFilteringBox() : base()
        {
            this.hasFilter_name = this.hasFilter_clientType = this.hasFilter_fileSize = false;
            this._currentView = null;
            this._currentClientType = ClientType.Both;
            InitializeComponent();
        }

        private void ContextMenu_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu menu)
            {
                menu.PlacementTarget = this;
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            }
        }

        private void ComboBoxClientTypeSelection_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                if (comboBox.ItemsSource == null)
                {
                    comboBox.ItemsSource = ClientTypes;
                    comboBox.SelectedIndex = 0;
                }
            }
        }

        private void ComboBoxClientTypeSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count != 0 && e.AddedItems[0] is ClientType type)
            {
                this._currentClientType = type;
            }
        }
        private void ComboBoxSizeComparisionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count != 0 && e.AddedItems[0] is SizeComparisonType type)
            {
                this._currentSizeComparisonType = type;
            }
        }

        private void ComboBoxSizeComparisionScale_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count != 0 && e.AddedItems[0] is SizeComparisonScale scale)
            {
                this._currentSizeComparisonScale = scale;
                if (long.TryParse(this._currentFileSizeRaw, out long value))
                {
                    this._currentFileSize = scale switch
                    {
                        SizeComparisonScale.B => value,
                        SizeComparisonScale.KB => value * 1024,
                        SizeComparisonScale.MB => value * 1024 * 1024,
                        SizeComparisonScale.GB => value * 1024 * 1024 * 1024,
                        _ => 0
                    };
                }
            }
        }

        private void TextBoxFilterByName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                this._currentName = textBox.Text;
            }
        }

        private void TextBoxFilterBySize_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var t = textBox.Text;
                if (string.IsNullOrWhiteSpace(t))
                {
                    e.Handled = true;
                    textBox.Text = "0";
                    textBox.CaretIndex = 1;
                }
                else if (long.TryParse(t, out long value))
                {
                    this._currentFileSizeRaw = t;

                    this._currentFileSize = this._currentSizeComparisonScale switch
                    {
                        SizeComparisonScale.B => value,
                        SizeComparisonScale.KB => value * 1024,
                        SizeComparisonScale.MB => value * 1024 * 1024,
                        SizeComparisonScale.GB => value * 1024 * 1024 * 1024,
                        _ => 0
                    };
                }
            }
        }

        private void ReFiltering()
        {
            var current = this._currentView;
            if (current != null && current.CanFilter)
            {
                int count_filter = 0;
                bool hasFilter_Name = (this.hasFilter_name && !string.IsNullOrEmpty(this._currentName));

                if (hasFilter_Name) count_filter++;
                if (this.hasFilter_clientType) count_filter++;
                if (this.hasFilter_fileSize) count_filter++;

                if (count_filter == 0)
                {
                    this.SetValue(FilterDisplayStringPropertyKey, string.Empty);
                    current.Filter = null;
                }
                else if (count_filter == 1)
                {
                    if (hasFilter_Name)
                    {
                        this.SetValue(FilterDisplayStringPropertyKey, "Name: " + this._currentName);
                        current.Filter = this.FilterOnlyByName;
                    }
                    else if (this.hasFilter_fileSize)
                    {
                        this.SetValue(FilterDisplayStringPropertyKey, $"Size: {this._currentSizeComparisonType} than {this._currentFileSizeRaw}{this._currentSizeComparisonScale}");
                        current.Filter = this.FilterOnlyByFileSize;
                    }
                    else if (this.hasFilter_clientType)
                    {
                        this.SetValue(FilterDisplayStringPropertyKey, "Client: " + this._currentClientType.ToString());
                        current.Filter = this.FilterOnlyByClientType;
                    }
                    else
                    {
                        this.SetValue(FilterDisplayStringPropertyKey, string.Empty);
                        current.Filter = null;
                    }
                }
                else
                {
                    var sb = new StringBuilder();
                    if (hasFilter_Name)
                    {
                        sb.Append("Name: ").Append(this._currentName);
                    }
                    if (this.hasFilter_fileSize)
                    {
                        if (sb.Length != 0)
                        {
                            sb.Append("; ");
                        }
                        sb.Append("Size: ").Append(this._currentSizeComparisonType.ToString()).Append(" than ").Append(this._currentFileSizeRaw).Append(this._currentSizeComparisonScale.ToString());
                    }
                    if (this.hasFilter_clientType)
                    {
                        if (sb.Length != 0)
                        {
                            sb.Append("; ");
                        }
                        sb.Append("Client: ").Append(this._currentClientType.ToString());
                    }
                    this.SetValue(FilterDisplayStringPropertyKey, sb.ToString());
                    current.Filter = this.MultipleFilters;
                }
            }
        }

        private bool FilterOnlyByName(object? obj)
        {
            if (obj is CustomizationFileListItem item)
            {
                return this.FilterOnlyByName(item);
            }
            return false;
        }

        private bool FilterOnlyByName(CustomizationFileListItem item)
            => item.RelativeFilename.Contains(this._currentName, StringComparison.OrdinalIgnoreCase);

        private bool FilterOnlyByFileSize(object? obj)
        {
            if (obj is CustomizationFileListItem item)
            {
                return this.FilterOnlyByFileSize(item);
            }
            return false;
        }

        private bool FilterOnlyByFileSize(CustomizationFileListItem item)
            => (this._currentFileSize == 0 || this._currentSizeComparisonType switch
            {
                SizeComparisonType.Smaller => item.FileSize <= this._currentFileSize,
                SizeComparisonType.Bigger => item.FileSize >= this._currentFileSize,
                _ => false
            });

        private bool FilterOnlyByClientType(object? obj)
        {
            if (obj is CustomizationFileListItem item)
            {
                return this.FilterOnlyByClientType(item);
            }
            return false;
        }

        private bool FilterOnlyByClientType(CustomizationFileListItem item)
            => (item.ClientType == ClientType.Both || this._currentClientType == ClientType.Both || item.ClientType == this._currentClientType);

        private bool MultipleFilters(object? obj)
        {
            if (obj is CustomizationFileListItem item)
            {
                bool hasFilter_Name = (this.hasFilter_name && !string.IsNullOrEmpty(this._currentName));

                if (hasFilter_Name && !this.FilterOnlyByName(item)) return false;
                if (this.hasFilter_fileSize && !this.FilterOnlyByFileSize(item)) return false;
                if (this.hasFilter_clientType && !this.FilterOnlyByClientType(item)) return false;

                return true;
            }
            return false;
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            this.ReFiltering();
        }

        private void ToggleFilterByName_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                this.hasFilter_name = (cb.IsChecked == true);
            }
        }

        private void TextBoxDelayedTextChange_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var span = e.Text.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (!char.IsDigit(span[i]))
                {
                    e.Handled = true;
                }
            }
        }

        private void ToggleFilterByClientType_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                this.hasFilter_clientType = (cb.IsChecked == true);
            }
        }

        private void ToggleFilterByFileSize_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                this.hasFilter_fileSize = (cb.IsChecked == true);
            }
        }

        private void MenuItemClose_Click(object sender, RoutedEventArgs e)
        {
            var ctm = this.ContextMenu;
            if (ctm != null)
            {
                ctm.IsOpen = false;
            }
        }
    }
}

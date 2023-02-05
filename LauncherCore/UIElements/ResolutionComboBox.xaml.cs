using Leayal.PSO2Launcher.Core.Classes.PSO2.GameConfig;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Leayal.PSO2Launcher.Core.UIElements
{
    /// <summary>
    /// Interaction logic for ResolutionComboBox.xaml
    /// </summary>
    public partial class ResolutionComboBox : ContentControl
    {
        private static readonly ScreenResolution _CustomOneResolution = new ScreenResolution(0, 0);
        private static readonly ResolutionDOM _CustomOneDom = new ResolutionDOM(_CustomOneResolution);

        public static readonly RoutedEvent SelectedValueChangedEvent = EventManager.RegisterRoutedEvent("SelectedValueChanged", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(ResolutionComboBox));
        public event RoutedEventHandler SelectedValueChanged
        {
            add => this.AddHandler(SelectedValueChangedEvent, value);
            remove => this.RemoveHandler(SelectedValueChangedEvent, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IEnumerable<ScreenResolution>), typeof(ResolutionComboBox), new PropertyMetadata(null, (obj, e) =>
        {
            if (obj is ResolutionComboBox combobox)
            {
                combobox.SetItems();
            }
        }));

        private static readonly DependencyPropertyKey IsCustomResolutionPropertyKey = DependencyProperty.RegisterReadOnly("IsCustomResolution", typeof(bool), typeof(ResolutionComboBox), new PropertyMetadata(false));
        public static readonly DependencyProperty IsCustomResolutionProperty = IsCustomResolutionPropertyKey.DependencyProperty;

        public IEnumerable<ScreenResolution> ItemsSource
        {
            get => (IEnumerable<ScreenResolution>)this.GetValue(ItemsSourceProperty);
            set => this.SetValue(ItemsSourceProperty, value);
        }
        
        public bool IsCustomResolution => (bool)this.GetValue(IsCustomResolutionProperty);

        public static readonly DependencyProperty CustomWidthProperty = DependencyProperty.Register("CustomWidth", typeof(int), typeof(ResolutionComboBox), new UIPropertyMetadata(0, (obj, e) =>
        {
            if (obj is ResolutionComboBox box)
            {
                if (e.NewValue is int num)
                {
                    box.CustomWidthBox.Text = num.ToString();
                }
            }
        }));
        public static readonly DependencyProperty CustomHeightProperty = DependencyProperty.Register("CustomHeight", typeof(int), typeof(ResolutionComboBox), new UIPropertyMetadata(0, (obj, e) =>
        {
            if (obj is ResolutionComboBox box)
            {
                if (e.NewValue is int num)
                {
                    box.CustomHeightBox.Text = num.ToString();
                }
            }
        }));
        public int CustomWidth
        {
            get => (int)this.GetValue(CustomWidthProperty);
            set => this.SetValue(CustomWidthProperty, value);
        }
        public int CustomHeight
        {
            get => (int)this.GetValue(CustomHeightProperty);
            set => this.SetValue(CustomHeightProperty, value);
        }

        private readonly ObservableCollection<ResolutionDOM> resCollection;

        public ResolutionComboBox() : base()
        {
            this.resCollection = new ObservableCollection<ResolutionDOM>();
            InitializeComponent();
            this.SelectionBox.ItemsSource = this.resCollection;
        }

        private void SetItems()
        {
            // Create a copy and append "Custom"
            var source = this.ItemsSource;
            if (source == null)
            {
                this.SelectionBox.ItemsSource = null;
            }
            else
            {
                var selectedItem = this.SelectionBox.SelectedItem as ResolutionDOM;
                this.resCollection.Clear();
                foreach (var item in source)
                {
                    if (!item.IsEmpty)
                    {
                        var dom = new ResolutionDOM(item);
                        this.resCollection.Add(dom);
                        if (selectedItem != null && selectedItem.Equals(dom))
                        {
                            selectedItem = dom;
                        }
                    }
                }
                this.resCollection.Add(_CustomOneDom);
                if (selectedItem == null)
                {
                    this.SelectionBox.SelectedIndex = 0;
                }
                else
                {
                    this.SelectionBox.SelectedItem = selectedItem;
                }
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = this.SelectionBox.SelectedItem;
            if (selected != null && selected is ResolutionDOM dom)
            {
                if (dom.Value.IsEmpty)
                {
                    this.SetValue(IsCustomResolutionPropertyKey, true);
                }
                else
                {
                    this.CustomWidth = dom.Value.Width;
                    this.CustomHeight = dom.Value.Height;
                    this.SetValue(IsCustomResolutionPropertyKey, false);
                }
            }
            else
            {
                this.SetValue(IsCustomResolutionPropertyKey, false);
            }
            this.RaiseEvent(new RoutedEventArgs(SelectedValueChangedEvent));
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var span = e.Text.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (!char.IsDigit(span[i]))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        public ScreenResolution SelectedResolution
        {
            get
            {
                var selected = this.SelectionBox.SelectedItem;
                if (selected != null && selected is ResolutionDOM dom)
                {
                    var val = dom.Value;
                    if (val.IsEmpty)
                    {
                        return new ScreenResolution(this.CustomWidth, this.CustomHeight);
                    }
                    else
                    {
                        return val;
                    }
                }
                else
                {
                    return default;
                }
            }
            set
            {
                var source = this.SelectionBox.ItemsSource;
                if (source != null)
                {
                    foreach (var duh in source)
                    {
                        if (duh is ResolutionDOM dom)
                        {
                            if (dom.Value.Equals(value))
                            {
                                this.SelectionBox.SelectedItem = dom;
                                return;
                            }
                        }
                    }
                }
                this.SelectionBox.SelectedItem = _CustomOneDom;
                this.CustomWidth = value.Width;
                this.CustomHeight = value.Height;
            }
        }

        class ResolutionDOM
        {
            public string DisplayName { get; }
            public ScreenResolution Value { get; }

            public ResolutionDOM(ScreenResolution res)
            {
                this.Value = res;
                if (res.IsEmpty)
                {
                    this.DisplayName = "Custom resolution";
                }
                else
                {
                    string displayName;
                    switch (res.Ratio)
                    {
                        case KnownRatio._16_9:
                            displayName = $"{res.Width}x{res.Height} (16:9)";
                            break;
                        case KnownRatio._16_10:
                            displayName = $"{res.Width}x{res.Height} (16:10)";
                            break;
                        case KnownRatio._4_3:
                            displayName = $"{res.Width}x{res.Height} (4:3)";
                            break;
                        default:
                            displayName = $"{res.Width}x{res.Height}";
                            break;
                    }
                    this.DisplayName = displayName;
                }
            }

            public override bool Equals(object? obj)
            {
                if (obj is ResolutionDOM dom)
                {
                    return this.Equals(dom);
                }
                else
                {
                    return false;
                }
            }

            public bool Equals(ResolutionDOM? obj)
            {
                if (obj == null) return false;
                return this.Value.Equals(obj.Value);
            }

            public override int GetHashCode() => this.Value.GetHashCode();

            public override string ToString() => this.DisplayName;
        }

        private void CustomWidthBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(this.CustomWidthBox.Text, out var num))
            {
                this.CustomWidth = num;
                this.RaiseEvent(new RoutedEventArgs(SelectedValueChangedEvent));
            }
        }

        private void CustomHeightBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(this.CustomHeightBox.Text, out var num))
            {
                this.CustomHeight = num;
                this.RaiseEvent(new RoutedEventArgs(SelectedValueChangedEvent));
            }
        }
    }
}

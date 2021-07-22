using Leayal.PSO2Launcher.Core.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// Interaction logic for WeirdSlider.xaml
    /// </summary>
    public partial class WeirdSlider : UserControl
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(int), typeof(WeirdSlider), new UIPropertyMetadata(0, (obj, val) =>
        {
            if (obj is WeirdSlider slider)
            {
                // EnumDisplayNameAttribute
                // var t = val.NewValue.GetType();
                slider.RefreshState();
                slider.RaiseEvent(new RoutedPropertyChangedEventArgs<int>((int)val.OldValue, (int)val.NewValue, ValueChangedEvent));
            }
        }));
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(int), typeof(WeirdSlider), new UIPropertyMetadata(1, (obj, val) =>
        {
            if (obj is WeirdSlider slider)
            {
                var aaaa = slider.Name;
                var num = (int)val.NewValue;
                var min = slider.Minimum;
                if (num < min)
                {
                    throw new ArgumentOutOfRangeException();
                }
                var count = num + 1;
                if (count != slider.Indicator.ColumnDefinitions.Count)
                {
                    slider.Indicator.ColumnDefinitions.Clear();
                    for (int i = min; i < count; i++)
                    {
                        slider.Indicator.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                    }
                }
                if (slider.Value > num)
                {
                    slider.Value = num;
                }
                else
                {
                    slider.RefreshState();
                }
            }
        }));
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(int), typeof(WeirdSlider), new UIPropertyMetadata(0, (obj, val) =>
        {
            if (obj is WeirdSlider slider)
            {
                var num = (int)val.NewValue;
                if (num < slider.Maximum)
                {
                    throw new ArgumentOutOfRangeException();
                }
                if (slider.Value < num)
                {
                    slider.Value = num;
                }
                else
                {
                    slider.RefreshState();
                }
            }
        }));

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IReadOnlyDictionary<int, string>), typeof(WeirdSlider), new UIPropertyMetadata(null, (obj, val) =>
        {
            if (obj is WeirdSlider slider)
            {
                var olditems = val.OldValue as IReadOnlyDictionary<int, string>;
                var newitems = val.NewValue as IReadOnlyDictionary<int, string>;
                if (olditems == null)
                {
                    if (newitems != null)
                    {
                        slider.Minimum = 0;
                        slider.Maximum = newitems.Count - 1;
                        slider.Value = 0;
                    }
                }
                else
                {
                    if (newitems != null)
                    {
                        slider.Minimum = 0;
                        slider.Maximum = newitems.Count - 1;
                    }
                    else
                    {
                        slider.Minimum = 0;
                        slider.Maximum = 1;
                    }
                    slider.Value = 0;
                }
                slider.RefreshState();
            }
        }));

        private static readonly DependencyPropertyKey ValueTextPropertyKey = DependencyProperty.RegisterReadOnly("ValueText", typeof(string), typeof(WeirdSlider), new UIPropertyMetadata(string.Empty));
        public static readonly DependencyProperty ValueTextProperty = ValueTextPropertyKey.DependencyProperty;

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Direct, typeof(RoutedPropertyChangedEventHandler<int>), typeof(WeirdSlider));

        public event RoutedPropertyChangedEventHandler<int> ValueChanged
        {
            add => this.AddHandler(ValueChangedEvent, value);
            remove => this.RemoveHandler(ValueChangedEvent, value);
        }

        public int Value
        {
            get => (int)this.GetValue(ValueProperty);
            set => this.SetValue(ValueProperty, value);
        }

        public int Maximum
        {
            get => (int)this.GetValue(MaximumProperty);
            set => this.SetValue(MaximumProperty, value);
        }

        public int Minimum
        {
            get => (int)this.GetValue(MinimumProperty);
            set => this.SetValue(MinimumProperty, value);
        }

        public IReadOnlyDictionary<int, string> ItemsSource
        {
            get => (IReadOnlyDictionary<int, string>)this.GetValue(ItemsSourceProperty);
            set => this.SetValue(ItemsSourceProperty, value);
        }

        public string ValueText => (string)this.GetValue(ValueTextProperty);

        public WeirdSlider()
        {
            InitializeComponent();
        }

        private void WeirdButtonNext_Click(object sender, RoutedEventArgs e)
        {
            var val = this.Value;
            if (val < this.Maximum)
            {
                this.Value = val + 1;
                this.RefreshState();
            }
        }

        private void WeirdButtonPrevious_Click(object sender, RoutedEventArgs e)
        {
            var val = this.Value;
            if (val > this.Minimum)
            {
                this.Value = val - 1;
                this.RefreshState();
            }
        }

        private void RefreshState()
        {
            var current = this.Value;
            var src = this.ItemsSource;
            if (src != null)
            {
                if (!src.TryGetValue(current, out var name))
                {
                    name = current.ToString();
                }
                this.SetValue(ValueTextPropertyKey, name);
            }
            else
            {
                this.SetValue(ValueTextPropertyKey, current.ToString());
            }
            Grid.SetColumn(this.IndicatorValue, current);
        }
    }
}

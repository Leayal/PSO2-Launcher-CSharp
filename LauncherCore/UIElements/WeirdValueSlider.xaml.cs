using Leayal.PSO2Launcher.Core.Classes;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    /// <summary>
    /// Interaction logic for WeirdValueSlider.xaml
    /// </summary>
    public partial class WeirdValueSlider : UserControl
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(int), typeof(WeirdValueSlider), new UIPropertyMetadata(0, (obj, e) =>
        {
            if (obj is WeirdValueSlider slider)
            {
                slider.RaiseEvent(new RoutedPropertyChangedEventArgs<int>((int)e.OldValue, (int)e.NewValue, ValueChangedEvent));
            }
        }));
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Direct, typeof(RoutedPropertyChangedEventHandler<int>), typeof(WeirdValueSlider));
        private static readonly DoubleToIntConverter _doubleToIntConverter = new DoubleToIntConverter();

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

        public WeirdValueSlider()
        {
            InitializeComponent();

            this.SetBinding(ValueProperty, new Binding("Value") { Source = this.slider, Mode = BindingMode.TwoWay, Converter = _doubleToIntConverter });
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

        private void WeirdButtonNext_Click(object sender, RoutedEventArgs e)
        {
            var val = this.slider.Value;
            if (val < this.slider.Maximum)
            {
                this.slider.Value = val + 1;
            }
        }

        private void WeirdButtonPrevious_Click(object sender, RoutedEventArgs e)
        {
            var val = this.slider.Value;
            if (val > this.slider.Minimum)
            {
                this.slider.Value = val - 1;
            }
        }


        // Text="{Binding ElementName=slider,Path=Value,Mode=TwoWay,Converter={StaticResource NumberToStringConverter}}"

        bool flag_dontDOIT = false;
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!flag_dontDOIT)
            {
                flag_dontDOIT = true;
                try
                {
                    this.textbox.Text = Convert.ToInt32(e.NewValue).ToString();
                }
                finally
                {
                    flag_dontDOIT = false;
                }
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!flag_dontDOIT)
            {
                // Useless check but it also cast.
                if (sender is TextBox tb)
                {
                    flag_dontDOIT = true;
                    try
                    {
                        var currentVal = Convert.ToInt32(this.slider.Value);
                        var str = tb.Text;
                        int checkNum = Convert.ToInt32(this.slider.Minimum);
                        if (string.IsNullOrWhiteSpace(str))
                        {
                            e.Handled = true;
                            tb.Text = checkNum.ToString();
                            tb.CaretIndex = tb.Text.Length;
                            if (currentVal != checkNum)
                            {
                                this.slider.Value = checkNum;
                            }
                        }
                        else if (int.TryParse(str, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var num))
                        {
                            // Invalid values will not be accepted.
                            if (num < checkNum)
                            {
                                e.Handled = true;
                                tb.Text = checkNum.ToString();
                                tb.CaretIndex = tb.Text.Length;
                                if (currentVal != num)
                                {
                                    this.slider.Value = num;
                                }
                            }
                            else
                            {
                                checkNum = Convert.ToInt32(this.slider.Maximum);
                                if (num > checkNum)
                                {
                                    e.Handled = true;
                                    tb.Text = checkNum.ToString();
                                    tb.CaretIndex = tb.Text.Length;
                                    if (currentVal != num)
                                    {
                                        this.slider.Value = num;
                                    }
                                }
                                else
                                {
                                    if (currentVal != num)
                                    {
                                        this.slider.Value = num;
                                    }
                                }
                            }
                        }
                        else
                        {
                            var newVal = Convert.ToInt32(str);
                            if (currentVal != newVal)
                            {
                                this.slider.Value = newVal;
                            }
                        }
                    }
                    finally
                    {
                        flag_dontDOIT = false;
                    }
                }
            }
        }
    }
}

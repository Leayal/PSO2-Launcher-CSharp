using System;
using System.Globalization;
using System.Windows.Data;

namespace Leayal.PSO2Launcher.Core.Classes
{
    sealed class DoubleToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt32(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value);
        }
    }
}

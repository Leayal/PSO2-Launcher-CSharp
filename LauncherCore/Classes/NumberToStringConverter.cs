using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Leayal.PSO2Launcher.Core.Classes
{
    public sealed class NumberToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int num)
            {
                return num.ToString();
            }
            else if (value is double d)
            {
                return d.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                if (targetType == typeof(int))
                {
                    if (int.TryParse(s, out var num))
                    {
                        return num;
                    }
                }
                if (targetType == typeof(double))
                {
                    if (double.TryParse(s, out var num))
                    {
                        return num;
                    }
                }
            }

            return 0;
        }
    }

    public class StringToNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                if (targetType == typeof(int))
                {
                    if (int.TryParse(s, out var num))
                    {
                        return num;
                    }
                }
                if (targetType == typeof(double))
                {
                    if (double.TryParse(s, out var num))
                    {
                        return num;
                    }
                }
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int num)
            {
                return num.ToString();
            }
            else if (value is double d)
            {
                return d.ToString();
            }
            return string.Empty;
        }
    }
}
